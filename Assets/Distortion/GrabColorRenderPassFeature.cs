using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class GrabColorRenderPassFeature : ScriptableRendererFeature
{
    class GrabColorRenderPass : ScriptableRenderPass
    {
        private ProfilingSampler m_profilingSampler = new ProfilingSampler(nameof(GrabColorRenderPass));
        private readonly int m_CameraTransparentTextureID = Shader.PropertyToID("_CameraTransparentTexture");
        private RTHandle m_InputHandle;
        private RTHandle m_OutputHandle;
        private const string k_OutputName = "_BlitColorTexture";
        private Material m_Material;

        public GrabColorRenderPass(RenderPassEvent evt,Material mat)
        {
            m_Material = mat;
            renderPassEvent = evt;
        }

        #region Unity兼容模式

        // public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        // {
        //     var desc = cameraTextureDescriptor;
        //     desc.depthBufferBits = 0;
        //     desc.msaaSamples = 1;
        //     RenderingUtils.ReAllocateIfNeeded(ref m_OutputHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_OutputName );
        //     ConfigureTarget(m_OutputHandle);
        // }
        //
        // public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        // {
        //     // Set camera color as the input
        //     m_InputHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
        //
        //     CommandBuffer cmd = CommandBufferPool.Get();
        //     using (new ProfilingScope(cmd, m_profilingSampler))
        //     {
        //         // Blit the input RTHandle to the output one
        //         Blitter.BlitCameraTexture(cmd, m_InputHandle, m_OutputHandle, m_Material, 0);
        //
        //         // Make the output texture available for the shaders in the scene
        //         cmd.SetGlobalTexture(m_CameraTransparentTextureID, m_OutputHandle.nameID);
        //     }
        //     context.ExecuteCommandBuffer(cmd);
        //     cmd.Clear();
        //     CommandBufferPool.Release(cmd);
        // }
        #endregion

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.cameraType != CameraType.Game) return;

            //根据当前相机参数 创建RenderTexture
            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            RenderingUtils.ReAllocateHandleIfNeeded(ref m_OutputHandle, desc, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: k_OutputName);

            //设置全局共享
            Shader.SetGlobalTexture(m_CameraTransparentTextureID,m_OutputHandle);
            
            TextureHandle source = resourceData.activeColorTexture;
            TextureHandle destination = renderGraph.ImportTexture(m_OutputHandle);

            if (!source.IsValid() || !destination.IsValid())
                return;
            
            RenderGraphUtils.BlitMaterialParameters parameters = new (source,destination,Blitter.GetBlitMaterial(TextureDimension.Tex2D),0);
            renderGraph.AddBlitPass(parameters,"CopyTransparentResult");
            // resourceData.cameraColor = destination;
        }

        public void Dispose()
        {
            m_InputHandle?.Release();
            m_OutputHandle?.Release();
        }
    }

    GrabColorRenderPass m_GrabColorePass;
    
    public Material blitMaterial;
    /// <inheritdoc/>
    public override void Create()
    {
        m_GrabColorePass = new GrabColorRenderPass(RenderPassEvent.AfterRenderingTransparents,blitMaterial);
        
        m_GrabColorePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(m_GrabColorePass); 
        }
    }

    protected override void Dispose(bool disposing)
    {
        m_GrabColorePass?.Dispose();
        m_GrabColorePass = null;
    }
}
