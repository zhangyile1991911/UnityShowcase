Shader "Custom/Unlit/Ground_URP"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags 
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        
        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };
            

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
        CBUFFER_END
            
        Varyings vert (Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            
            VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
            output.positionHCS = vertexInput.positionCS;
            output.uv = TRANSFORM_TEX(input.uv, _MainTex);
            return output;
        }
            
        half4 frag (Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            return col;
        }
            
        ENDHLSL
        //UnlitColor
        Pass
        {
            Name "GroundUnlitColor"
            Tags { "RenderType"="Geometry" "LightMode" = "UniversalForward" }
            
            ZWrite On
            ZTest LEqual
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        //WriteDepth
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma vertex vert
            #pragma fragment customDepthFragment
            
            void customDepthFragment(Varyings input,out half4 outNormalWS : SV_Target0)
            {
                half4 detailMask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                outNormalWS = half4(NormalizeNormalPerPixel(detailMask),0);
            }
            
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}