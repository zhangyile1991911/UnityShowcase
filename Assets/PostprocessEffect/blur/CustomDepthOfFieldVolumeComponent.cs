using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
    public ClampedFloatParameter nearClipDistance = new ClampedFloatParameter(1f, 0f, 100f);
    public ClampedFloatParameter farClipDistance = new ClampedFloatParameter(1f, 0f, 1000f);

    public bool IsActive()
    {
        return mode.value == CustomDepthOfFieldMode.On;
    }
}

public enum CustomDepthOfFieldMode
{
    Off = 0,
    On = 1,
}
[Serializable]
public sealed class CustomDepthOfFieldModeParameter : VolumeParameter<CustomDepthOfFieldMode>
{
    public CustomDepthOfFieldModeParameter(CustomDepthOfFieldMode value, bool overrideState = false):base(value,overrideState){}
}