using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(SSAOSimpleRenderer), PostProcessEvent.AfterStack, "DCL/SSAO-Simple")]
public sealed class SSAOSimple : PostProcessEffectSettings
{
    public FloatParameter intensity = new FloatParameter { value = 1 };
    public FloatParameter radius = new FloatParameter { value = 5 };
    public ColorParameter occlusionColor = new ColorParameter { value = Color.black };
    public FloatParameter noiseAmount = new FloatParameter { value = 0.001f };
    public FloatParameter downScaling = new FloatParameter { value = 1 };
    public FloatParameter blurAmount = new FloatParameter { value = 3 };
    public FloatParameter blurDownscaling = new FloatParameter { value = 1 };
    public IntParameter blurPasses = new IntParameter { value = 1 };
    public BoolParameter debugMode = new BoolParameter { value = false };
}

public sealed class SSAOSimpleRenderer : PostProcessEffectRenderer<SSAOSimple>
{
    const float _MinDepthDefaultValue = 1.5f;
    const float _SelfShadowReductionFactorDefaultValue = 0.4f;
    const float _GaussianCenterDefaultValue = 0.4f;
    const float _GaussianWidthDefaultValue = 5;

    public void GetTemporaryRT(PostProcessRenderContext context, int shaderId, int width, int height)
    {
        context.command.GetTemporaryRT(shaderId, new RenderTextureDescriptor
        {
            width = (int)width,
            height = (int)height,
            colorFormat = RenderTextureFormat.RFloat,
            depthBufferBits = 0,
            volumeDepth = 1,
            autoGenerateMips = false,
            msaaSamples = 1,
            enableRandomWrite = false,
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
            sRGB = false
        }, FilterMode.Bilinear);
    }

    public enum BlitPass
    {
        PRESENT = 0,
        BLUR_H = 1,
        BLUR_V = 2
    }

    static int _BlurTexture;
    static int _BlurTexture2;
    static int _AmbientOcclusionTexture;


    static int _NoiseResolution;
    static int _MinDepth;
    static int _Radius;
    static int _NoiseAmount;
    static int _SelfShadowReductionFactor;
    static int _GaussianCenter;
    static int _GaussianWidth;

    static int _BlurAmount;
    static int _Intensity;
    static int _DebugMode;
    static int _Color;

    static Shader shaderSsaoSimple;
    static Shader shaderSsaoSimpleBlit;

    public override void Init()
    {
        base.Init();

        shaderSsaoSimple = Shader.Find("Hidden/Custom/SSAO-Simple");
        shaderSsaoSimpleBlit = Shader.Find("Hidden/Custom/SSAO-Simple-Blit");

        _BlurTexture = Shader.PropertyToID("_BlurTexture");
        _BlurTexture2 = Shader.PropertyToID("_BlurTexture2");
        _AmbientOcclusionTexture = Shader.PropertyToID("_AmbientOcclusionTexture");

        _NoiseResolution = Shader.PropertyToID("_NoiseResolution");
        _MinDepth = Shader.PropertyToID("_MinDepth");
        _Radius = Shader.PropertyToID("_Radius");
        _NoiseAmount = Shader.PropertyToID("_NoiseAmount");
        _SelfShadowReductionFactor = Shader.PropertyToID("_SelfShadowReductionFactor");
        _GaussianCenter = Shader.PropertyToID("_GaussianCenter");
        _GaussianWidth = Shader.PropertyToID("_GaussianWidth");

        _BlurAmount = Shader.PropertyToID("_BlurAmount");
        _Intensity = Shader.PropertyToID("_Intensity");
        _DebugMode = Shader.PropertyToID("_DebugMode");
        _Color = Shader.PropertyToID("_Color");
    }


    public override void Render(PostProcessRenderContext context)
    {
        var ssaoSheet = context.propertySheets.Get(shaderSsaoSimple);
        float width = context.width / settings.downScaling;
        float height = context.height / settings.downScaling;

        ssaoSheet.properties.SetVector(_NoiseResolution, new Vector4(width, height, 0, 0));
        ssaoSheet.properties.SetFloat(_MinDepth, _MinDepthDefaultValue);
        ssaoSheet.properties.SetFloat(_Radius, settings.radius);
        ssaoSheet.properties.SetFloat(_NoiseAmount, settings.noiseAmount);
        ssaoSheet.properties.SetFloat(_SelfShadowReductionFactor, _SelfShadowReductionFactorDefaultValue);
        ssaoSheet.properties.SetFloat(_GaussianCenter, _GaussianCenterDefaultValue);
        ssaoSheet.properties.SetFloat(_GaussianWidth, _GaussianWidthDefaultValue);

        var ambientOcclusionTextureId = new UnityEngine.Rendering.RenderTargetIdentifier(_AmbientOcclusionTexture);

        GetTemporaryRT(context, _AmbientOcclusionTexture, (int)width, (int)height);

        context.command.BlitFullscreenTriangle(context.source, ambientOcclusionTextureId, ssaoSheet, 0);

        var blitSheet = context.propertySheets.Get(shaderSsaoSimpleBlit);
        blitSheet.properties.SetVector(_NoiseResolution, new Vector4(width / settings.blurDownscaling, height / settings.blurDownscaling, 0, 0));
        blitSheet.properties.SetFloat(_BlurAmount, settings.blurAmount);
        blitSheet.properties.SetFloat(_Intensity, settings.intensity);
        blitSheet.properties.SetFloat(_DebugMode, settings.debugMode ? 1 : 0);
        blitSheet.properties.SetFloat(_GaussianCenter, _GaussianCenterDefaultValue);
        blitSheet.properties.SetColor(_Color, settings.occlusionColor);

        ApplyBlur(context, ambientOcclusionTextureId, blitSheet);

        context.command.BlitFullscreenTriangle(context.source, context.destination, blitSheet, (int)BlitPass.PRESENT);

        context.command.ReleaseTemporaryRT(_AmbientOcclusionTexture);
        context.command.ReleaseTemporaryRT(_BlurTexture);

        if (settings.blurPasses > 1)
            context.command.ReleaseTemporaryRT(_BlurTexture2);
    }

    private void ApplyBlur(PostProcessRenderContext context, UnityEngine.Rendering.RenderTargetIdentifier ambientOcclusionTextureId, PropertySheet blitSheet)
    {
        float width = context.width / settings.downScaling;
        float height = context.height / settings.downScaling;

        var blurTextureId = new UnityEngine.Rendering.RenderTargetIdentifier(_BlurTexture);
        var blurTextureId2 = new UnityEngine.Rendering.RenderTargetIdentifier(_BlurTexture2);

        GetTemporaryRT(context, _BlurTexture, (int)(width / settings.blurDownscaling), (int)(height / settings.blurDownscaling));
        GetTemporaryRT(context, _BlurTexture2, (int)(width / settings.blurDownscaling), (int)(height / settings.blurDownscaling));

        context.command.BlitFullscreenTriangle(ambientOcclusionTextureId, blurTextureId, blitSheet, (int)BlitPass.BLUR_H);

        for (int i = 0; i < settings.blurPasses; i++)
        {
            if (i > 0)
                context.command.BlitFullscreenTriangle(blurTextureId2, blurTextureId, blitSheet, (int)BlitPass.BLUR_H);

            context.command.BlitFullscreenTriangle(blurTextureId, blurTextureId2, blitSheet, (int)BlitPass.BLUR_V);
        }
    }
}
