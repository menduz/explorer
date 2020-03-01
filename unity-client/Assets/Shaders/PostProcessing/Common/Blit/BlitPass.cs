
namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Copy the given color buffer to the given destination color buffer.
    ///
    /// You can use this pass to copy a color buffer to the destination,
    /// so you can use it later in rendering. For example, you can copy
    /// the opaque texture to use it for distortion effects.
    /// </summary>
    internal class BlitPass : ScriptableRenderPass
    {
        public enum RenderTarget
        {
            Color,
            RenderTexture,
        }

        public Material blitMaterial = null;
        public int blitShaderPassIndex = 0;
        public FilterMode filterMode { get; set; }

        private RenderTargetIdentifier source { get; set; }
        private RenderTargetHandle destination { get; set; }

        RenderTargetHandle m_TemporaryColorTexture;
        RenderTargetHandle m_TemporaryTexture;
        string m_ProfilerTag;

        Material createdMaterial;
        Blit.BlitSettings settings;
        /// <summary>
        /// Create the CopyColorPass
        /// </summary>
        public BlitPass(Blit.BlitSettings settings, string tag)
        {
            this.settings = settings;
            m_ProfilerTag = tag;

            if (settings.destination == LWRP.Blit.Target.Color)
                m_TemporaryColorTexture.Init("_TemporaryColorTexture");
            else if (settings.destination == LWRP.Blit.Target.Texture)
                m_TemporaryTexture.Init(settings.textureId);


            if (settings.blitMaterial == null)
            {
                if (createdMaterial == null)
                    createdMaterial = new Material(Shader.Find("Hidden/BlitCopy"));

                blitMaterial = createdMaterial;
            }
            else
            {
                blitMaterial = settings.blitMaterial;
            }

            blitShaderPassIndex = settings.blitMaterialPassIndex;
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination)
        {
            this.source = source;
            this.destination = destination;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;
            opaqueDesc.width = (int)(opaqueDesc.width * settings.renderScale);
            opaqueDesc.height = (int)(opaqueDesc.height * settings.renderScale);

            // Can't read and write to same color target, create a temp render target to blit. 
            if (destination == RenderTargetHandle.CameraTarget)
            {
                cmd.GetTemporaryRT(m_TemporaryColorTexture.id, opaqueDesc, filterMode);
                Blit(cmd, source, m_TemporaryColorTexture.Identifier(), blitMaterial, blitShaderPassIndex);
                Blit(cmd, m_TemporaryColorTexture.Identifier(), source);
            }
            else
            {
                cmd.GetTemporaryRT(m_TemporaryTexture.id, opaqueDesc, filterMode);
                Blit(cmd, source, m_TemporaryTexture.Identifier(), blitMaterial, blitShaderPassIndex);
                cmd.ReleaseTemporaryRT(m_TemporaryTexture.id);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (destination == RenderTargetHandle.CameraTarget)
                cmd.ReleaseTemporaryRT(m_TemporaryColorTexture.id);
        }
    }
}
