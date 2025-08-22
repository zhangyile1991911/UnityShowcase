Shader "Hidden/Custom/PostProcessing/FogEffect"
{
    HLSLINCLUDE
    #pragma target 3.5
    
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    #include "Assets/lygia/generative/pnoise.hlsl"
    float4 _FogColor;
    float _FogDensity;
    float _FogStart;
    float _FogEnd;
    float _FogHeight;
    float _FogFalloff;
    float _NoiseScale;
    float _NoiseIntensity;
    float4 _NoiseVector;
    float _MaxFogBlendFactor;
    float3 _RealWorldSpaceCameraPos;
    
    // 简单的噪声函数
    float noise(float2 p)
    {
        return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
    }
    
    struct FogVaryings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        // float3 positionVS : TEXCOORD1;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    //顶点着色器
    FogVaryings FogVert(Attributes input)
    {
        FogVaryings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        
        float4 ClipPosition = GetFullScreenTriangleVertexPosition(input.vertexID);
        float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);

        // float4 ndcPosition = float4(ClipPosition.xy,0.0,1.0f);
        // float4 viewPosition = mul(unity_CameraInvProjection, ndcPosition);
        // viewPosition.xyz /= viewPosition.w; // 透视除法
        // output.positionVS = viewPosition.xyz;
        
        output.positionCS = ClipPosition;
        output.texcoord   =  uv;
        return output;
        
    }
    //片段着色器
    half4 FragFog(FogVaryings input) : SV_Target
    {
        // 采样原始颜色
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
        half4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
        
        // 从深度纹理获取线性深度
        float depth = SampleSceneDepth(uv);
        //转换到摄像机的裁剪空间0.3~1000
        float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
        
        //重建世界位置
        //在PostProcess处理流程中 不能直接使用_WorldSpaceCameraPos
        //后处理摄像机“接管”：为了对这张纹理应用全屏效果（如Bloom，色调调整，景深等）
        //Unity的渲染管线会创建一个虚拟的、用于渲染后处理的摄像机。这个摄像机：通常位于世界原点 (0, 0, 0)。
        //它的朝向是固定的（例如沿着Z轴）。使用正交投影，以确保它能完美地、1:1地渲染整个屏幕四边形。
        float3 worldPos = ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
        // 计算基础雾效
        float fogFactor = saturate((linearDepth - _FogStart) / (_FogEnd - _FogStart));
        fogFactor = 1.0 - exp(-_FogDensity * fogFactor);
        
        // 高度衰减
        //指数函数 heightFactor值是 < 1
        //所以 _FogFallOff > 0 的时候heightFactor逐渐减小
        //_FogFalloff < 0 的时候heightFactor逐渐变大
        
        float heightFactor = saturate(worldPos.y / _FogHeight);
        heightFactor = pow(heightFactor, _FogFalloff);
        fogFactor *= (1.0 - heightFactor);

        float3 posdistort = worldPos + _NoiseVector.xyz;
        float noiseValue = pnoise(posdistort,float3(10,10,10));
        noiseValue = (noiseValue + 1) * 0.5;
        // return half4(noiseValue,noiseValue,noiseValue,noiseValue);
        // // 添加噪声扰动
        // float2 noiseUV = worldPos.xz * _NoiseScale +  _NoiseVector;
        // float noiseValue = noise(noiseUV) * _NoiseIntensity;
        // fogFactor *= noiseValue;
        // return half4(fogFactor,fogFactor,fogFactor,1);
        
        half4 fogColor =_FogColor * (fogFactor + noiseValue * 0.05);
        // return fogColor;
        
        // 混合雾颜色和场景颜色
        float f = saturate((linearDepth - _FogStart) / (_FogEnd - _FogStart));
        f = min(f,_MaxFogBlendFactor);
        half4 finalColor = lerp(sceneColor, fogColor , f);
        return finalColor;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Depth of Field Fog"
            HLSLPROGRAM
                #pragma vertex FogVert
                #pragma fragment FragFog
            ENDHLSL
        }
    }
    
}