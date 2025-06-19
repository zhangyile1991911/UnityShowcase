using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;


public sealed class CustomDepthOfFieldRendererFeature : ScriptableRendererFeature
{
    #region FEATURE_FIELDS
    
    [SerializeField]
    [HideInInspector]
    private Material m_Material;
    
    private CustomPostRenderPass m_FullScreenPass;

    #endregion

    #region FEATURE_METHODS

    public override void Create()
    {
        m_Material = new Material(Shader.Find("Hidden/CustomDepthOfField"));
        if(m_Material)
            m_FullScreenPass = new CustomPostRenderPass(name, m_Material);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Material == null || m_FullScreenPass == null)
            return;
        
        if (renderingData.cameraData.cameraType == CameraType.Preview || renderingData.cameraData.cameraType == CameraType.Reflection)
            return;

        
        CustomDepthOfFieldVolumeComponent myVolume = VolumeManager.instance.stack?.GetComponent<CustomDepthOfFieldVolumeComponent>();
        if (myVolume == null || !myVolume.IsActive())
            return;

       
        m_FullScreenPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        
        m_FullScreenPass.ConfigureInput(ScriptableRenderPassInput.Depth);

        renderer.EnqueuePass(m_FullScreenPass);
    }

    protected override void Dispose(bool disposing)
    {
        m_FullScreenPass.Dispose();
    }

    #endregion

    private class CustomPostRenderPass : ScriptableRenderPass
    {
        #region PASS_FIELDS
        
        private Material m_Material;
        private readonly GraphicsFormat m_DepthOfFieldFormat;
        // The property block used to set additional properties for the material
        private static MaterialPropertyBlock s_SharedPropertyBlock = new MaterialPropertyBlock();
        
        
        private static readonly int kBlitTexturePropertyId = Shader.PropertyToID("_BlitTexture");
        private static readonly int kBlitScaleBiasPropertyId = Shader.PropertyToID("_BlitScaleBias");
        private static readonly int _AreaFieldParams = Shader.PropertyToID("_AreaFieldParams");
        private static readonly int _SeparationTexture = Shader.PropertyToID("_SeparationTexture");
        private static readonly int _NearBlurTexture = Shader.PropertyToID("_NearBlurTexture");
        private static readonly int _FarBlurTexture = Shader.PropertyToID("_FarBlurTexture");
        public static readonly int _SourceSize = Shader.PropertyToID("_SourceSize");

        #endregion

        public CustomPostRenderPass(string passName, Material material)
        {
            profilingSampler = new ProfilingSampler(passName);
            m_Material = material;
            
            requiresIntermediateTexture = true;
            
            if (SystemInfo.IsFormatSupported(GraphicsFormat.R16_UNorm, GraphicsFormatUsage.Blend))
                m_DepthOfFieldFormat = GraphicsFormat.R16_UNorm;
            else if (SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, GraphicsFormatUsage.Blend))
                m_DepthOfFieldFormat = GraphicsFormat.R16_SFloat;
            else // Expect CoC banding
                m_DepthOfFieldFormat = GraphicsFormat.R8_UNorm;
        }

        #region PASS_RENDER_GRAPH_PATH
        
        private class MainPassData
        {
            public Material material;
            public TextureHandle inputTexture;
            public TextureHandle separationTexture;
            public TextureHandle nearBlurTexture;
            public TextureHandle farBlurTexture;
        }
        
        const int k_FirstCustomDepthOfField = 0;
        const int k_BlurNearDepthOfField = 1;
        const int k_BlurFarDepthOfField = 2;
        const int k_BlurDoFPassComposite = 3;

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            

            if (cameraData.isSceneViewCamera) return;
            var stack = VolumeManager.instance.stack;
            var customDepthOfFieldParam = stack.GetComponent<CustomDepthOfFieldVolumeComponent>();
            Debug.Log($"m_CustomDepthOfField.nearClipDistance = ${customDepthOfFieldParam.nearClipDistance.value}");
            Debug.Log($"m_CustomDepthOfField.farClipDistance = ${customDepthOfFieldParam.farClipDistance.value}");
            
            var curDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
            Debug.Log($"_SourceSize = {curDesc.width}x{curDesc.height} ");
            m_Material.SetVector(_SourceSize, new Vector4(curDesc.width, curDesc.height, 1.0f/curDesc.width, 1.0f/curDesc.height) );
            m_Material.SetVector(_AreaFieldParams,new Vector2(customDepthOfFieldParam.nearClipDistance.value,customDepthOfFieldParam.farClipDistance.value));
            
            //step 1 根据深度图 分割出需要模糊的部分和不需要模糊的部分
            TextureHandle SeparationTexture;
            
            curDesc.name = "_CameraColorCustomPostProcessing";
            curDesc.clearBuffer = false;
            SeparationTexture = renderGraph.CreateTexture(curDesc);
            m_Material.SetVector(_AreaFieldParams,new Vector2(customDepthOfFieldParam.nearClipDistance.value,customDepthOfFieldParam.farClipDistance.value));
            
            var bmp1 = new RenderGraphUtils.BlitMaterialParameters(resourcesData.cameraColor, SeparationTexture,
                m_Material, k_FirstCustomDepthOfField);
            renderGraph.AddBlitPass(bmp1, "Depth Of Field Separate");
            Debug.Log($"Depth Of Field Separate is Finished");
            
            //step2 近景模糊
            TextureHandle nearBlurResult;
            nearBlurResult = renderGraph.CreateTexture(curDesc);
            var bmp2 = new RenderGraphUtils.BlitMaterialParameters(resourcesData.cameraColor, nearBlurResult,
                m_Material, k_BlurNearDepthOfField);
            renderGraph.AddBlitPass(bmp2, "Depth Of Field Blur Near");
            Debug.Log($"Depth Of Field Blur Near is Finished");
            
            //step3 远景模糊
            TextureHandle farBlurResult;
            farBlurResult = renderGraph.CreateTexture(curDesc);
            var bmp3 = new RenderGraphUtils.BlitMaterialParameters(resourcesData.cameraColor, farBlurResult,
                m_Material, k_BlurFarDepthOfField);
            renderGraph.AddBlitPass(bmp3, "third CDOF far blur");
            Debug.Log($"Depth Of Field Blur Far is Finished");
            
            using (var builder = renderGraph.AddRasterRenderPass<MainPassData>(passName, out var passData, profilingSampler))
            {
                passData.material = m_Material;

                TextureHandle destination;
                
                var cameraColorDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
                cameraColorDesc.name = "_CameraColorCustomPostProcessing";
                cameraColorDesc.clearBuffer = false;

                destination = renderGraph.CreateTexture(cameraColorDesc);
                passData.inputTexture = resourcesData.cameraColor;
                passData.separationTexture = SeparationTexture;
                passData.nearBlurTexture = nearBlurResult;
                passData.farBlurTexture = farBlurResult;
                
                builder.UseTexture(passData.inputTexture, AccessFlags.Read);
                builder.UseTexture(passData.separationTexture, AccessFlags.Read);
                builder.UseTexture(passData.nearBlurTexture, AccessFlags.Read);
                builder.UseTexture(passData.farBlurTexture, AccessFlags.Read);
                
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourcesData.activeDepthTexture, AccessFlags.Write);
                builder.SetRenderFunc((MainPassData data, RasterGraphContext context) =>
                {
                    s_SharedPropertyBlock.Clear();
                    if (data.inputTexture.IsValid())
                    {
                        s_SharedPropertyBlock.SetTexture(kBlitTexturePropertyId, data.inputTexture);
                        s_SharedPropertyBlock.SetTexture(_SeparationTexture,data.separationTexture);
                        s_SharedPropertyBlock.SetTexture(_NearBlurTexture,data.nearBlurTexture);
                        s_SharedPropertyBlock.SetTexture(_FarBlurTexture,data.farBlurTexture);
                    }
                    s_SharedPropertyBlock.SetVector(kBlitScaleBiasPropertyId, new Vector4(1, 1, 0, 0));
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, k_BlurDoFPassComposite, MeshTopology.Triangles, 3, 1, s_SharedPropertyBlock);
                });
                resourcesData.cameraColor = destination;
            }
        }

        #endregion
        
        public void Dispose()
        {
        }
    }
}
