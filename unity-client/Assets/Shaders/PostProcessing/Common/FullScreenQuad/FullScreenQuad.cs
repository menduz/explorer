namespace UnityEngine.Rendering.LWRP
{
    public class FullScreenQuad : ScriptableRendererFeature
    {
        [System.Serializable]
        public class FullScreenQuadSettings
        {
            public RenderPassEvent renderPassEvent;
            public Material material;
        }

        public FullScreenQuadSettings settings = new FullScreenQuadSettings();
        FullScreenQuadPass m_RenderQuadPass;

        public override void Create()
        {
            m_RenderQuadPass = new FullScreenQuadPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.material != null)
                renderer.EnqueuePass(m_RenderQuadPass);
        }
    }
}
