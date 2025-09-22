#ifndef HALFTONE_MAINLIGHT_H
#define HALFTONE_MAINLIGHT_H

#if !defined(SHADERGRAPH_PREVIEW)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#endif

void HalfToneMainLight_half(out half3 Direction)
{
    #if defined(SHADERGRAPH_PREVIEW)
    //在ShaderGraph中 默认给定一个向上的光照方向
    Direction = half3(0.0f, 1.0f, 0.0f);
    #else
    Light light = GetMainLight();
    Direction = light.direction;
    #endif
}

void HalfToneMainLight_float(out float3 Direction)
{
    #if defined(SHADERGRAPH_PREVIEW)
    //在ShaderGraph中 默认给定一个向上的光照方向
    Direction = half3(0.0f, 1.0f, 0.0f);
    #else
    Light light = GetMainLight();
    Direction = half3(light.direction);
    #endif
}


#endif