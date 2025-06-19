Shader "Hidden/CustomDepthOfField"
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
        
        float2 _AreaFieldParams;
        float4 _SourceSize;
        #define NearBlurEnd   _AreaFieldParams.x
        #define FarBlurStart   _AreaFieldParams.y


        half4 FragSeparate(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            //根据屏幕的宽高放大UV坐标
            float depth = LOAD_TEXTURE2D_X(_CameraDepthTexture, _SourceSize.xy * uv).x;
            
            //最小值：等于相机的近裁剪面距离（如 Near=0.3，则最小返回 0.3）
		    //最大值：等于相机的远裁剪面距离（如 Far=1000，则最大返回 1000）
            depth = LinearEyeDepth(depth, _ZBufferParams);
           
            // 假定 现在near = 5 far = 15
            // 比5小的地方是红色 比15大的地方是红色
            float r = 0;
            float g = 0;
            float b = 0;
            if (depth < NearBlurEnd)
            {
                r = saturate((depth - _ProjectionParams.y) / (NearBlurEnd - _ProjectionParams.y));
                g = 1;
            }
            else if (depth > FarBlurStart)
            {
                r = saturate((depth - FarBlurStart) / (100 - FarBlurStart));
                b = 1;
            }
            
            return half4(r,g,b,1);
        }

        #include "Assets/lygia/filter/boxBlur.hlsl"
        #include "Assets/lygia/filter/noiseBlur.hlsl"
        
        half4 NearBlur(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            return noiseBlur(_BlitTexture,uv,_SourceSize.zw,20);
        }

        
        half4 FarBlur(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            return boxBlur(_BlitTexture,uv,_SourceSize.zw,20);
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
            }
           
            //rgb
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
    }
}
