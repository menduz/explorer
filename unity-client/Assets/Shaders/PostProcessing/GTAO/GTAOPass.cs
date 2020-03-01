
namespace UnityEngine.Rendering.LWRP
{
    internal class GTAOPass : ScriptableRenderPass
    {

        Material blitMaterial = null;
        public FilterMode filterMode { get; set; }

        private RenderTargetIdentifier source { get; set; }
        private RenderTargetHandle destination { get; set; }

        string m_ProfilerTag;

        GTAO.GTAOSettings settings;
        /// <summary>
        /// Create the CopyColorPass
        /// </summary>
        public GTAOPass(GTAO.GTAOSettings settings, string tag)
        {
            this.settings = settings;
            m_ProfilerTag = tag;

            if (blitMaterial == null)
            {
                blitMaterial = new Material(Shader.Find("Hidden/GTAO"));
            }

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
            opaqueDesc.sRGB = false;
            opaqueDesc.dimension = TextureDimension.Tex2D;

            cmd.GetTemporaryRT(destination.id, opaqueDesc, filterMode);

            Camera camera = renderingData.cameraData.camera;

            cmd.SetGlobalTexture("_NoiseTexture", settings.noiseTexture);
            //cmd.SetGlobalMatrix("_InverseView", camera.cameraToWorldMatrix);
            //cmd.SetGlobalTexture("_MainTex", settings.checkerTexture);

            Blit(cmd, source, destination.Identifier(), blitMaterial);
            //Blit(cmd, destination.Identifier(), source);


            //cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            //cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, blitMaterial);
            //cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(destination.id);
        }
    }
}
