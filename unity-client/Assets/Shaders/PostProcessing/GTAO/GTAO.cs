namespace UnityEngine.Rendering.LWRP
{
    public class GTAO : ScriptableRendererFeature
    {
        [System.Serializable]
        public class GTAOSettings
        {
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingPrePasses;
            public string textureId = "_BlitPassTexture";
            public float renderScale = 1;
            public Texture2D noiseTexture;
            public Texture2D checkerTexture;
        }

        public GTAOSettings settings = new GTAOSettings();
        RenderTargetHandle m_RenderTextureHandle;

        GTAOPass blitPass;

        public override void Create()
        {
            blitPass = new GTAOPass(settings, name);
            blitPass.filterMode = FilterMode.Trilinear;
            m_RenderTextureHandle.Init(settings.textureId);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var src = renderer.cameraColorTarget;
            var dest = m_RenderTextureHandle;
            blitPass.Setup(src, dest);
            renderer.EnqueuePass(blitPass);
        }
    }
}
