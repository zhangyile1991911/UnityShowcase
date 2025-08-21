using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public enum CustomDepthOfFieldMode
{
    Off = 0,
    On = 1,
}

public enum FarBlurMode
{
    BoxBlur = 0,
    RadialBlur = 1,
}

public enum NearBlurMode
{
    BoxBlur = 0,
    GaussianBlur = 1,
    NoiseBlur = 2,
    RadialBlur = 3,
}

[Serializable]
public sealed class CustomDepthOfFieldModeParameter : VolumeParameter<CustomDepthOfFieldMode>
{
    public CustomDepthOfFieldModeParameter(CustomDepthOfFieldMode value, bool overrideState = false):base(value,overrideState){}
}

[Serializable]
public sealed class CustomDepthOfFieldFarBlurModeParameter : VolumeParameter<FarBlurMode>
{
    public CustomDepthOfFieldFarBlurModeParameter(FarBlurMode value, bool overrideState = false):base(value,overrideState){}
}

[Serializable]
public sealed class CustomDepthOfFieldNearBlurModeParameter : VolumeParameter<NearBlurMode>
{
    public CustomDepthOfFieldNearBlurModeParameter(NearBlurMode value, bool overrideState = false):base(value,overrideState){}
}

[VolumeComponentMenu("Post-processing Custom/CustomDepthOfField")]
[VolumeRequiresRendererFeatures(typeof(CustomDepthOfFieldRendererFeature))]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
public sealed class CustomDepthOfFieldVolumeComponent : VolumeComponent, IPostProcessComponent
{
    public CustomDepthOfFieldVolumeComponent()
    {
        displayName = "CustomDepthOfField";
    }

    public CustomDepthOfFieldModeParameter mode = new CustomDepthOfFieldModeParameter(CustomDepthOfFieldMode.Off);
    public ClampedFloatParameter farBlurFactor = new ClampedFloatParameter(20f, 0f, 50f,true);
    public ClampedFloatParameter farClipDistance = new ClampedFloatParameter(1f, 5f, 50f,true);
    public CustomDepthOfFieldFarBlurModeParameter farBlurMode = new CustomDepthOfFieldFarBlurModeParameter(FarBlurMode.BoxBlur);

    public CustomDepthOfFieldModeParameter nearMode = new CustomDepthOfFieldModeParameter(CustomDepthOfFieldMode.Off);
    public ClampedFloatParameter nearClipDistance = new ClampedFloatParameter(1f, 0f, 10f,true);
    public ClampedFloatParameter nearBlurFactor = new ClampedFloatParameter(20f, 0f, 50f,true);
    public CustomDepthOfFieldNearBlurModeParameter nearBlurMode = new CustomDepthOfFieldNearBlurModeParameter(NearBlurMode.NoiseBlur);
    
    public ClampedFloatParameter maxFarDistance = new ClampedFloatParameter(100f, 0f, 1000f,true);
    public bool IsActive()
    {
        return mode.value == CustomDepthOfFieldMode.On;
    }
}