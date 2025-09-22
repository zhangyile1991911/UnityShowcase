#ifndef LYGIA_WRAP_H_
#define LYGIA_WRAP_H_

#include "Assets/lygia/sdf/circleSDF.hlsl"
void circleSDF_half(in float2 st,out half sdf)
{
    sdf = 0;
    sdf = circleSDF(st);
}

void circleSDF_float(in float2 st,out float sdf)
{
    sdf = 0;
    sdf = circleSDF(st);
}

void VortexUV_float(float2 UV, float2 Center, float Strength, float Radius, out float2 OutUV)
{
    float2 delta = UV - Center;
    float distance = length(delta);
    float angle = atan2(delta.y, delta.x);
    
    if (distance < Radius)
    {
        float rotation = Strength * (1.0 - distance / Radius);
        angle += rotation;
        delta = float2(cos(angle), sin(angle)) * distance;
    }
    
    OutUV = Center + delta;
}

void VortexUV_half(float2 UV, float2 Center, float Strength, float Radius,out half2 OutUV)
{
    float2 result = float2(0.0, 0.0);
    VortexUV_float(UV,Center,Strength,Radius,result);
    OutUV = half2(result);
}

void SmoothVortex_float(
    float2 UV,
    float2 Center,
    float Strength,
    float Radius,
    float Time,
    float RotationSpeed,
    out float2 OutUV,
    out float VortexMask
)
{
    float2 delta = UV - Center;
    float distance = length(delta);
    
    // 1. 计算平滑的影响掩码（避免硬边界）
    float innerRadius = Radius * 0.8;
    float outerRadius = Radius * 1.2;
    VortexMask = 1.0 - smoothstep(innerRadius, outerRadius, distance);
    
    // 2. 无限旋转处理（使用相位累积）
    if (VortexMask > 0.001)
    {
        float originalAngle = atan2(delta.y, delta.x);
        
        // 基础漩涡强度（距离相关）
        float distanceFactor = 1.0 - saturate(distance / Radius);
        float vortexPower = Strength * distanceFactor;
        
        // 无限时间旋转（使用周期函数避免数值问题）
        float infiniteRotation = RotationSpeed * Time * 2.0 * 3.14159;
        
        // 组合旋转角度
        float totalRotation = vortexPower + infiniteRotation;
        
        // 3. 应用平滑旋转
        float newAngle = originalAngle + totalRotation;
        
        // 4. 重新计算坐标（保持距离不变）
        delta = float2(cos(newAngle), sin(newAngle)) * distance;
        
        // 5. 可选：添加径向缩放效果增强平滑度
        float scale = 1.0 - VortexMask * 0.2 * sin(Time + distance * 5.0);
        delta *= scale;
    }
    
    OutUV = Center + delta;
}
#endif