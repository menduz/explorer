namespace UnityEngine.Rendering.LWRP
{
    public class FullScreenQuadPass : ScriptableRenderPass
    {
        string m_ProfilerTag = "DrawFullScreenPass";

        FullScreenQuad.FullScreenQuadSettings m_Settings;
        RenderTargetHandle m_SourceTextureHandler;

        public FullScreenQuadPass(FullScreenQuad.FullScreenQuadSettings settings)
        {
            renderPassEvent = settings.renderPassEvent;
            m_Settings = settings;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get(m_ProfilerTag);

            Camera camera = renderingData.cameraData.camera;

            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Settings.material);
            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
