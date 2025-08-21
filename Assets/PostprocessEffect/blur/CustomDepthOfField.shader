Shader "Hidden/Custom/CustomDepthOfField"
{
    HLSLINCLUDE

        #pragma target 3.5

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        TEXTURE2D_X(_SeparationTexture);
        TEXTURE2D_X(_NearBlurTexture);
        TEXTURE2D_X(_FarBlurTexture);
        
        float _nearBlurFactor;
        float _farBlurFactor;
        float2 _AreaFieldParams;
        float4 _SourceSize;
        float _MaxFarDistance;
        #define NearBlurEnd   _AreaFieldParams.x
        #define FarBlurStart   _AreaFieldParams.y


        half4 FragSeparate(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            //根据屏幕的宽高放大UV坐标
            float depth = SampleSceneDepth(uv);
            
            //最小值：等于相机的近裁剪面距离（如 Near=0.3，则最小返回 0.3）
		    //最大值：等于相机的远裁剪面距离（如 Far=1000，则最大返回 1000）
            depth = LinearEyeDepth(depth, _ZBufferParams);
           
            // 假定 现在near = 5 far = 15
            // 比5小的地方是红色 比15大的地方是红色
            float r = 0;
            float g = 0;
            float b = 0;
            
            if (depth+_ProjectionParams.y < NearBlurEnd)
            {
                r = saturate(depth / NearBlurEnd);
                g = 1;
            }
            else if (depth >= FarBlurStart)
            {
                r = saturate((depth - FarBlurStart) / (_MaxFarDistance - FarBlurStart));
                b = 1;
            }
            
            return half4(r,g,b,1);
        }

        #include "Assets/lygia/filter/boxBlur.hlsl"
        #include "Assets/lygia/filter/noiseBlur.hlsl"
        #include "Assets/lygia/filter/gaussianBlur.hlsl"
        #include "Assets/lygia/filter/radialBlur.hlsl"
        
        half4 NearBlur(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            return noiseBlur(_BlitTexture,uv,_SourceSize.zw,_nearBlurFactor);
        }

        
        half4 FarBlur(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            // return gaussianBlur(_BlitTexture,uv,_SourceSize.zw,_farBlurFactor);
            return boxBlur(_BlitTexture,uv,_SourceSize.zw,_farBlurFactor);
            // return noiseBlur(_BlitTexture,uv,_SourceSize.zw,_farBlurFactor);
            // float2 dir = uv - float2(.5f,0.85f);
            // dir = normalize(dir);
            // return radialBlur(_BlitTexture,uv,_SourceSize.zw*dir,_farBlurFactor);
        }

        half4 FragRadialBlur(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            float2 dir = uv - float2(.5f,0.85f);
            dir = normalize(dir);
            return radialBlur(_BlitTexture,uv,_SourceSize.zw*dir,_farBlurFactor);
        }

        half4 FragComposite(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

            half4 nearColor = LOAD_TEXTURE2D_X(_NearBlurTexture, _SourceSize.xy * uv);
            half4 farColor = LOAD_TEXTURE2D_X(_FarBlurTexture, _SourceSize.xy * uv);
            half4 baseColor = LOAD_TEXTURE2D_X(_BlitTexture, _SourceSize.xy * uv);
            half4 sep = SAMPLE_TEXTURE2D_X(_SeparationTexture, sampler_LinearClamp,uv);
            
            UNITY_BRANCH
            if (sep.g > 0.5)
            {
                half blend = sqrt(sep.r * TWO_PI);
                nearColor = nearColor * saturate(blend);
                nearColor.rgb = nearColor.rgb + baseColor.rgb * saturate(1.0 - blend);
            }

            UNITY_BRANCH
            if (sep.b > 0.5)
            {
                half blend = sqrt(sep.r * TWO_PI);
                farColor = farColor * saturate(blend);
                farColor.rgb = farColor.rgb + baseColor.rgb * saturate(1.0 - blend);
                // farColor.rgb = farColor.rgb * sep.r + baseColor.rgb * saturate(1.0 - sep.r);
            }
            // return sep;
            //rgb
            //只输出 没有被影响的部分
            // return (1 - sep.g - sep.b) * baseColor;
            //只输出 前景
            // return sep.g * nearColor;
            //只输出 后景
            // return sep.b * farColor;
           return nearColor * sep.g + farColor * sep.b + (1 - sep.g - sep.b) * baseColor;
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Depth Of Field Separate"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragSeparate
            ENDHLSL
        }

        Pass
        {
            Name "Depth Of Field Blur Near"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment NearBlur
                
            ENDHLSL
        }

        Pass
        {
            Name "Depth Of Field Blur Far"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FarBlur
            ENDHLSL
        }

        Pass
        {
            Name "Depth Of Field Composite"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragComposite
            ENDHLSL
        }

        Pass
        {
            Name "Depth Of Field Radial Blur"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragRadialBlur
            ENDHLSL
        }
    }
}
