Shader "Custom/GameObjectBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0, 10)) = 1.0
        _KernelSize ("Kernel Size", Range(0,5)) = 3
        _Opacity ("Opacity", Range(0, 1)) = 1.0
        _CameraDist ("Camera Dist",Float) = 10.0
    }
    
    SubShader
    {
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        CGINCLUDE
        #include "UnityCG.cginc"
        
        sampler2D _MainTex;
        float4 _MainTex_ST;
        float4 _MainTex_TexelSize;
        float _BlurSize;
        int _KernelSize;
        float _Opacity;
        float _CameraDist;
        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };
        
        struct v2f
        {
            float2 uv : TEXCOORD0;
            float blur : TEXCOORD1;
            float4 vertex : SV_POSITION;
        };
        
        v2f vert (appdata v)
        {
            v2f o;
            float4 worldPos = mul(unity_ObjectToWorld,float4(v.vertex.xyz,1.0));
            float3 cameraWorldPos = _WorldSpaceCameraPos;
            float distanceToCamera = abs(worldPos.z - cameraWorldPos.z);
            o.blur = step(_CameraDist,distanceToCamera);
            
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            return o;
        }

        fixed4 blur(float2 inputUV)
        {
            fixed4 color = fixed4(0, 0, 0, 0);
            const int halfSize = _KernelSize / 2;
            float2 texelSize = _MainTex_TexelSize.xy * _BlurSize;
            float weight = 1.0 / (_KernelSize * _KernelSize);
            [loop]
            for (int x = -halfSize; x <= halfSize; x++)
            {
                [loop]
                for (int y = -halfSize; y <= halfSize; y++)
                {
                    float2 offset = float2(x, y) * texelSize;
                    color += tex2D(_MainTex, inputUV + offset) * weight;
                }
            }
            color.a *= _Opacity;
            return color;
        }
        fixed4 frag_box (v2f i) : SV_Target
        {
            if (i.blur > 0.5)
            {
                return tex2D(_MainTex, i.uv);
            }

            return blur(i.uv);
        }
        ENDCG
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_box
            ENDCG
        }
    }
    FallBack "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}