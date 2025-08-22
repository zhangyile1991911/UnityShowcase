using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class PS_FogRendererFeature : ScriptableRendererFeature
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
        m_Material = new Material(Shader.Find("Hidden/Custom/PostProcessing/FogEffect"));
        if(m_Material)
            m_FullScreenPass = new CustomPostRenderPass(name, m_Material);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Material == null || m_FullScreenPass == null)
            return;
        
        if (renderingData.cameraData.cameraType == CameraType.Preview || renderingData.cameraData.cameraType == CameraType.Reflection)
            return;
        
        PS_FogVolumeComponent myVolume = VolumeManager.instance.stack?.GetComponent<PS_FogVolumeComponent>();
        if (myVolume == null || !myVolume.IsActive())
            return;

        m_FullScreenPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        m_FullScreenPass.ConfigureInput(ScriptableRenderPassInput.None);

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

        // The material used to render the post-processing effect
        private Material m_Material;

        // The handle to the temporary color copy texture (only used in the non-render graph path)
        private RTHandle m_CopiedColor;

        // The property block used to set additional properties for the material
        private static MaterialPropertyBlock s_SharedPropertyBlock = new MaterialPropertyBlock();

        // This constant is meant to showcase how to create a copy color pass that is needed for most post-processing effects
        private static readonly bool kSampleActiveColor = true;

        // This constant is meant to showcase how you can add dept-stencil support to your main pass
        private static readonly bool kBindDepthStencilAttachment = false;

        // Creating some shader properties in advance as this is slightly more efficient than referencing them by string
        private static readonly int kBlitTexturePropertyId = Shader.PropertyToID("_BlitTexture");
        private static readonly int kBlitScaleBiasPropertyId = Shader.PropertyToID("_BlitScaleBias");

        #endregion

        public CustomPostRenderPass(string passName, Material material)
        {
            profilingSampler = new ProfilingSampler(passName);
            m_Material = material;
            
            requiresIntermediateTexture = kSampleActiveColor;
        }
        

        #region PASS_RENDER_GRAPH_PATH
        
        private static readonly int kFogColorPropertyId = Shader.PropertyToID("_FogColor");
        private static readonly int kFogDensityPropertyId = Shader.PropertyToID("_FogDensity");
        private static readonly int kFogStartPropertyId = Shader.PropertyToID("_FogStart");
        private static readonly int kFogEndPropertyId = Shader.PropertyToID("_FogEnd");
        private static readonly int kFogHeightPropertyId = Shader.PropertyToID("_FogHeight");
        private static readonly int kFogFalloffPropertyId = Shader.PropertyToID("_FogFalloff");
        private static readonly int kNoiseVectorPropertyId = Shader.PropertyToID("_NoiseVector");
        private static readonly int kMaxFogBlendFactor = Shader.PropertyToID("_MaxFogBlendFactor");
        private static readonly int kRealWorldSpaceCameraPos = Shader.PropertyToID("_RealWorldSpaceCameraPos");
        
        private class MainPassData
        {
            public Material material;
            public TextureHandle inputTexture;
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            
            if(cameraData.isSceneViewCamera)return;
            var stack = VolumeManager.instance.stack;
            var customDepthOfFogParam = stack.GetComponent<PS_FogVolumeComponent>();
            
            using (var builder = renderGraph.AddRasterRenderPass<MainPassData>(passName, out var passData, profilingSampler))
            {
                passData.material = m_Material;
                
                m_Material.SetColor(kFogColorPropertyId,customDepthOfFogParam.fogColor.value);
                m_Material.SetFloat(kFogDensityPropertyId,customDepthOfFogParam.FogDensity.value);
                m_Material.SetFloat(kFogStartPropertyId,customDepthOfFogParam.FogStart.value);
                m_Material.SetFloat(kFogEndPropertyId,customDepthOfFogParam.FogEnd.value);
                m_Material.SetFloat(kFogHeightPropertyId,customDepthOfFogParam.FogHeight.value);
                m_Material.SetFloat(kFogFalloffPropertyId,customDepthOfFogParam.FogFalloff.value);
                m_Material.SetVector(kNoiseVectorPropertyId,customDepthOfFogParam.NoiseVector.value);
                m_Material.SetFloat(kMaxFogBlendFactor,customDepthOfFogParam.MaxFogBlendFactor.value);
                m_Material.SetVector(kRealWorldSpaceCameraPos,cameraData.worldSpaceCameraPos);
                Debug.Log($"cameraData.worldSpaceCameraPos = {cameraData.worldSpaceCameraPos}");
                TextureHandle destination;
                
                var cameraColorDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
                cameraColorDesc.name = "_CameraColorCustomPostProcessing";
                cameraColorDesc.clearBuffer = false;
                
                //注意事项
                //这里不要直接去修改原本CameraColor中的颜色数据
                //在下次渲染时候,原始场景颜色会叠加上一次Fog效果
                //导致多次渲染后 整个画面都会变成白色
                destination = renderGraph.CreateTexture(cameraColorDesc);
                passData.inputTexture = resourcesData.cameraColor;
                
                builder.UseTexture(passData.inputTexture, AccessFlags.Read);
                
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                
                // builder.SetRenderAttachmentDepth(resourcesData.activeDepthTexture, AccessFlags.Read);

                builder.SetRenderFunc((MainPassData data, RasterGraphContext context) =>
                {
                    s_SharedPropertyBlock.SetTexture(kBlitTexturePropertyId,data.inputTexture);
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1, s_SharedPropertyBlock);
                });
                resourcesData.cameraColor = destination;
            }
        }

        public void Dispose()
        {
            
        }
        #endregion
    }
}
