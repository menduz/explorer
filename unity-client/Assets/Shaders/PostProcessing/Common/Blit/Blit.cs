namespace UnityEngine.Rendering.LWRP
{
    public class Blit : ScriptableRendererFeature
    {
        [System.Serializable]
        public class BlitSettings
        {
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

            public Material blitMaterial = null;
            public int blitMaterialPassIndex = -1;
            public Target destination = Target.Color;
            public string textureId = "_BlitPassTexture";
            public float renderScale = 1;
        }

        public enum Target
        {
            Color,
            Texture
        }

        public BlitSettings settings = new BlitSettings();
        RenderTargetHandle m_RenderTextureHandle;

        BlitPass blitPass;

        public override void Create()
        {
            int passIndex = settings.blitMaterial != null ? settings.blitMaterial.passCount - 1 : 0;
            settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, passIndex);

            blitPass = new BlitPass(settings, name);
            blitPass.filterMode = FilterMode.Trilinear;
            m_RenderTextureHandle.Init(settings.textureId);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var src = renderer.cameraColorTarget;
            var dest = (settings.destination == Target.Color) ? RenderTargetHandle.CameraTarget : m_RenderTextureHandle;

            blitPass.Setup(src, dest);
            renderer.EnqueuePass(blitPass);
        }
    }
}
