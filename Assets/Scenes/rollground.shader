Shader "Custom/rollground"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0,10)) = 1.0
        _KernelSize ("Kernel Size", Range(0,5)) = 3
        _Opacity ("Opacity", Range(0, 1)) = 1.0
        _CameraDist ("Camera Dist", Float) = 1.0
        _DownStart ("DownStart", Float) = 10.0
        _DownAmplify ("DownAmplify",Float) = 1.0
    }
    
    SubShader
    {
        HLSLINCLUDE
        
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _BlurSize;
            int _KernelSize;
            float _Opacity;
            float _CameraDist;
            float _DownStart;
            float _DownAmplify;
        CBUFFER_END
        
        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };
        
        struct Varyings
        {
            float2 uv : TEXCOORD0;
            float4 positionHCS : SV_POSITION;
        };
        
        Varyings vert (Attributes input)
        {
            Varyings output;
            
            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

            float3 cameraWorldPos = _WorldSpaceCameraPos;
            float distanceToCamera = abs(positionWS.z - cameraWorldPos.z);
            
            float maxDownValue = 50;
            float down = smoothstep(_DownStart, (_DownStart + maxDownValue) * _DownAmplify , distanceToCamera);
            float downValue = clamp(0, maxDownValue, distanceToCamera - _DownStart);
            positionWS.y -= downValue * down;
            
            output.positionHCS = TransformWorldToHClip(positionWS);
            output.uv = TRANSFORM_TEX(input.uv, _MainTex);
            
            return output;
        }
        
        half4 frag_box (Varyings i) : SV_Target
        {
            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
        }

         Varyings UnlitVertex(Attributes attributes)
        {
            Varyings o = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(attributes);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            
            o.positionHCS = TransformObjectToHClip(attributes.positionOS);
            o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
            return o;
        }
        
        void customDepthFragment(Varyings input,out half4 outNormalWS : SV_Target0)
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            half4 detailMask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            
            // if (detailMask.a > 0)
            // {
            //     outNormalWS = half4(NormalizeNormalPerPixel(detailMask),0);
            // }
            // else
            // {
            //     discard;
            // }
            outNormalWS = half4(NormalizeNormalPerPixel(detailMask),0);
        }
        
        ENDHLSL

        Pass
        {
            Name "GroundColor"
            
            Tags {
            "RenderType" = "Transparency"
            "Queue" = "Transparency"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            
           
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_box
            ENDHLSL
        }

         Pass
        {
            Name "GroundDepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma target 2.0
            #pragma shader_feature _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex UnlitVertex
            #pragma fragment customDepthFragment
            
            
           
            
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}