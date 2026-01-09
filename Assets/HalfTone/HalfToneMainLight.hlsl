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

void SSSMainLight_half(out half3 Direction,out half Intensity)
{
    #if defined(SHADERGRAPH_PREVIEW)
    Direction = half3(0.5f, 0.5f, 0.0f);
    Intensity = 1.0f;
    #else
    Light light = GetMainLight();
    Direction = light.direction;
    Intensity = length(light.color);    
    #endif
}

void SSSMainLight_float(out float3 Direction,out float Intensity)
{
    #if defined(SHADERGRAPH_PREVIEW)
    Direction = float3(0.5f, 0.5f, 0.0f);
    Intensity = 1.0f;
    #else
    Light light = GetMainLight();
    Direction = light.direction;
    Intensity = length(light.color);    
    #endif
}

void ZeldaMainLight_float(float3 WorldPos,out float3 Direction,out float3 Color,out float DistanceAtten,out float ShadowAtten)
{
#if SHADERGRAPH_PREVIEW
    Direction = float3(0.5f, 0.5f, 0.0f);
    Color = float3(1.0f, 1.0f, 1.0f);
    DistanceAtten = 1.0f;
    ShadowAtten = 1.0f;
#else
    #if SHADOWS_SCREEN
        float4 clipPos = TransformWorldToHClip(WorldPos);
        float4 shadowCoord = ComputeScreenPos(clipPos);
    #else
        float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    #endif
    Light mainLight = GetMainLight(shadowCoord);
    Direction = mainLight.direction;
    Color = mainLight.color;
    DistanceAtten = mainLight.distanceAttenuation;
    ShadowAtten = mainLight.shadowAttenuation;
#endif
}

void ZeldaMainLight_half(float3 WorldPos,out half3 Direction,out half3 Color,out half DistanceAtten,out half ShadowAtten)
{
    #if SHADERGRAPH_PREVIEW
    Direction = half3(0.5f, 0.5f, 0.0f);
    Color = half3(1.0f, 1.0f, 1.0f);
    DistanceAtten = 1.0f;
    ShadowAtten = 1.0f;
    #else
    #if SHADOWS_SCREEN
    float4 clipPos = TransformWorldToHClip(WorldPos);
    float4 shadowCoord = ComputeScreenPos(clipPos);
    #else
    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    #endif
    Light mainLight = GetMainLight(shadowCoord);
    Direction = mainLight.direction;
    Color = mainLight.color;
    DistanceAtten = mainLight.distanceAttenuation;
    ShadowAtten = mainLight.shadowAttenuation;
    #endif
}
#endif