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
        m_Material = new Material(Shader.Find("Hidden/Custom/CustomDepthOfField"));
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
        
        private static MaterialPropertyBlock s_SharedPropertyBlock = new MaterialPropertyBlock();
        
        
        private static readonly int kBlitTexturePropertyId = Shader.PropertyToID("_BlitTexture");
        private static readonly int kBlitScaleBiasPropertyId = Shader.PropertyToID("_BlitScaleBias");
        private static readonly int _AreaFieldParams = Shader.PropertyToID("_AreaFieldParams");
        private static readonly int _SeparationTexture = Shader.PropertyToID("_SeparationTexture");
        private static readonly int _NearBlurTexture = Shader.PropertyToID("_NearBlurTexture");
        private static readonly int _FarBlurTexture = Shader.PropertyToID("_FarBlurTexture");
        public static readonly int _SourceSize = Shader.PropertyToID("_SourceSize");
        private static readonly int _nearBlurFactor = Shader.PropertyToID("_nearBlurFactor");
        private static readonly int _farBlurFactor = Shader.PropertyToID("_farBlurFactor");
        private static readonly int _MaxFarDistance = Shader.PropertyToID("_MaxFarDistance");

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
        const int k_RadialBlur = 4;
        const int k_BoxBlur = 5;
        const int k_GaussianBlur = 6;
        const int k_NoiseBlur = 7;

        TextureHandle SeparationTexture;
        TextureHandle nearBlurResult;
        TextureHandle farBlurResult;
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            
            if (cameraData.isSceneViewCamera) return;
            var stack = VolumeManager.instance.stack;
            var customDepthOfFieldParam = stack.GetComponent<CustomDepthOfFieldVolumeComponent>();
            
            if (customDepthOfFieldParam.nearMode == CustomDepthOfFieldMode.On)
            {
                m_Material.SetFloat(_nearBlurFactor, customDepthOfFieldParam.nearBlurFactor.value);    
            }
            else
            {
                m_Material.SetFloat(_nearBlurFactor, 0);
            }
            
            m_Material.SetFloat(_farBlurFactor, customDepthOfFieldParam.farBlurFactor.value);
            m_Material.SetFloat(_MaxFarDistance,customDepthOfFieldParam.maxFarDistance.value);
            
            //step 1 根据深度图 分割出需要模糊的部分和不需要模糊的部分
            var curDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
            curDesc.name = "_CameraColorCustomPostProcessing";
            curDesc.clearBuffer = false;
            renderGraph.CreateTextureIfInvalid(curDesc,ref SeparationTexture);
            m_Material.SetVector(_AreaFieldParams,new Vector2(customDepthOfFieldParam.nearClipDistance.value,customDepthOfFieldParam.farClipDistance.value));
            m_Material.SetVector(_SourceSize, new Vector4(curDesc.width, curDesc.height, 1f/curDesc.width, 1f/curDesc.height));
            var bmp1 = new RenderGraphUtils.BlitMaterialParameters(resourcesData.cameraColor, SeparationTexture,
                m_Material, k_FirstCustomDepthOfField);
            renderGraph.AddBlitPass(bmp1, "Depth Of Field Separate");
            
            //step2 近景模糊
            renderGraph.CreateTextureIfInvalid(curDesc,ref nearBlurResult);
            if (customDepthOfFieldParam.nearMode == CustomDepthOfFieldMode.On)
            {
                var bmp2 = new RenderGraphUtils.BlitMaterialParameters(resourcesData.cameraColor, nearBlurResult,
                    m_Material, k_BlurNearDepthOfField);
                renderGraph.AddBlitPass(bmp2, "Depth Of Field Blur Near");
            }
            
            //step3 远景模糊
            renderGraph.CreateTextureIfInvalid(curDesc,ref farBlurResult);
            if (customDepthOfFieldParam.farBlurMode == FarBlurMode.BoxBlur)
            {
                var bmp3 = new RenderGraphUtils.BlitMaterialParameters(resourcesData.cameraColor, farBlurResult,
                    m_Material, k_BlurFarDepthOfField);
                renderGraph.AddBlitPass(bmp3, "third CDOF far blur");    
            }
            else
            {
                var bmp3 = new RenderGraphUtils.BlitMaterialParameters(resourcesData.cameraColor, farBlurResult,
                    m_Material, k_RadialBlur);
                renderGraph.AddBlitPass(bmp3, "third CDOF Radial blur");
            }
              using (var builder = renderGraph.AddRasterRenderPass<MainPassData>(passName, out var passData, profilingSampler))
              {
                    //step4 融合模糊结果
                    passData.material = m_Material;
                    
                    TextureHandle destination;
                    
                    var cameraColorDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
                    cameraColorDesc.name = "_CameraColorCustomPostProcessing";
                    cameraColorDesc.clearBuffer = false;
                
                    destination = renderGraph.CreateTexture(cameraColorDesc);
                    passData.inputTexture = resourcesData.cameraColor;
                    passData.separationTexture = SeparationTexture;
                    if (customDepthOfFieldParam.nearMode == CustomDepthOfFieldMode.On)
                    {
                        passData.nearBlurTexture = nearBlurResult;    
                    }
                    else
                    {
                        passData.nearBlurTexture = resourcesData.cameraColor;
                    }
                    passData.farBlurTexture = farBlurResult;
                         
                    builder.UseTexture(passData.inputTexture, AccessFlags.Read);
                    builder.UseTexture(passData.separationTexture, AccessFlags.Read);
                    builder.UseTexture(passData.nearBlurTexture, AccessFlags.Read);
                    builder.UseTexture(passData.farBlurTexture, AccessFlags.Read);
                    
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    
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
