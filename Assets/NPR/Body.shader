Shader "ToonShader/Body"
{
    Properties
    {
        [Header(Textures)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _LightMap ("Light Map",2D) = "white" {}//光照贴图
        [Toggle(_USE_LIGHTMAP_AO)] _UseLightMapAO ("Use LightMap AO",Range(0,1)) = 1
        
        [Header(Ramp Shadow)]
        _RampTex("Ramp Tex",2D) = "white" {}
        [Toggle(_USE_RAMP_SHADOW)] _UseRampShadow ("Use Ramp Shadow",Range(0,1)) = 1
        _ShadowRampWidth("Shadow Ramp width",Float) = 1//阴影边缘宽度
        _ShadowPosition("Shadow Position",Float) = 0.55//阴影位置
        _ShadowSoftness("Shadow Softness",Float) = 0.5//阴影柔和度
        [Toggle] _UseRampShadow2 ("Use Ramp Shadow 2",Range(0,1)) = 1 //使用第2行阴影 开关
        [Toggle] _UseRampShadow3 ("Use Ramp Shadow 3",Range(0,1)) = 1 //使用第3行阴影 开关
        [Toggle] _UseRampShadow4 ("Use Ramp Shadow 4",Range(0,1)) = 1 //使用第4行阴影 开关
        [Toggle] _UseRampShadow5 ("Use Ramp Shadow 5",Range(0,1)) = 1 //使用第5行阴影 开关
        
        [Header(Lighting Options)]
        _DayOrNight ("Day Or Night",Range(0,1)) = 0 //日夜切换
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalRenderPipeline"//指定渲染管线URP
            "RenderType" = "Opaque"//渲染队列
        }
        HLSLINCLUDE //公共代码块
            #pragma multi_compile _MAIN_LIGHT_SHADOWS //主光源阴影
            #pragma multi_compile _MAIN_LIGHT_SHADOWS_CASCADE //主光源阴影级联
            #pragma multi_compile _MAIN_LIGHT_SHADOWS_SCREEN //主光源阴影屏幕空间

            #pragma multi_compile_fragment  _LIGHT_LAYERS //光照层
            #pragma multi_compile_fragment  _LIGHT_COOKIES //光照
            #pragma multi_compile_fragment  _SCREEN_SPACE_OCCLUSION //屏幕遮挡
            #pragma multi_compile_fragment  _ADDITIONAL_LIGHT_SHADOWS //额外光照阴影
            #pragma multi_compile_fragment  _SHADOWS_SOFT //阴影软化

            // #pragma multi_compile_fragment _REFLECTION_PROBE_BLENDING //反射探针
            // #pragma multi_compile_fragment _REFLECTION_PROBE_BOX_PROJECTION //反射探针盒投影
        
            #pragma shader_feature_local    _USE_LIGHTMAP_AO
            #pragma shader_feature_local    _USE_RAMP_SHADOW
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            sampler2D _BaseMap;
            sampler2D _LightMap;
            //Ramp
            sampler2D _RampTex;
            //常量缓冲区
            CBUFFER_START(UnityPerMaterial)
               
                float _ShadowRampWidth;
                float _ShadowPosition;
                float _ShadowSoftness;
                float _UseRampShadow2;
                float _UseRampShadow3;
                float _UseRampShadow4;
                float _UseRampShadow5;
                float _DayOrNight;
            CBUFFER_END

            
            // half RampShadow1D(half input)
            // {
            //     half row = 0.1f;
            //     if (input > 0.8) row = 0.9f;
            //     else if (input > 0.6) row = 0.7f;
            //     else if (input > 0.4) row = 0.5f;
            //     else if (input > 0.2) row = 0.3f;
            //
            //     return row;
            // }
            //float step(float edge, float x);
            //如果 x < edge，返回 0； edge > x ? 0 : 1
            //如果 x >= edge，返回 1。edge <= x ? 1 * 0
            half RampShadow1D(float input,float useShadow2,float useShadow3,float useShadow4,float useShadow5,
                float shadowValue1,float shadowValue2,float shadowValue3,float shadowValue4,float shadowValue5)
            {
                float v1 = step(0.6f,input) * step(input,0.8f);
                float v2 = step(0.4f,input) * step(input,0.6f);
                float v3 = step(0.2f,input) * step(input,0.4f);
                float v4 = step(0.2f,input);
                
                float blend12 = lerp(shadowValue1,shadowValue2,useShadow2);
                float blend15 = lerp(shadowValue1,shadowValue5,useShadow5);
                float blend13 = lerp(shadowValue1,shadowValue3,useShadow3);
                float blend14 = lerp(shadowValue1,shadowValue4,useShadow4);
                
                //default
                float result = blend12;
                result = lerp(result,blend15,v1);
                result = lerp(result,blend13,v2);
                result = lerp(result,blend14,v3);
                result = lerp(result,shadowValue1,v4);
                return result;
            }

            struct UniversalAttributes
            {
                float4 positionOS : POSITION;
                float2 uv0 :  TEXCOORD0;
                float2 uv1 :  TEXCOORD1;
                float3 normalOS : NORMAL;//局部坐标法线,通过cpu传递
                float4 color : COLOR0;//顶点颜色
            };

            struct UniversalVarings
            {
                float4 positionCS : POSITION;
                float2 uv0 : TEXCOORD0;
                float3 normalWS: TEXCOORD1;//通过顶点着色器 转换到世界坐标系
                float4 color : TEXCOORD2;//从顶点着色器中传递的 顶点颜色
            };

            //1 半兰伯特
            //2 环境光遮蔽 让物体表面 缝隙 凹进去的地方 更暗，使用贴图实现lightmap
            //R 控制高光，金属
            //G 控制阴影AO 衣服褶皱
            //B 高光强度
            half4 MainFS(UniversalVarings input) : SV_TARGET
            {
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 N = normalize(input.normalWS);
                float NoL = dot(N,L);//法线->光源

                float4 baseMap = tex2D(_BaseMap,input.uv0);
                float4 lightMap = tex2D(_LightMap,input.uv0);
                
                //
                half4 vertexColor = input.color;
                // return half4(vertexColor.ggg,1);
                
                //Lambert
                half lambert = NoL;
                half halfLambert = lambert * 0.5f + 0.5f;//dot结果[-1~1] * 0.5 = [-0.5,0.5] + 0.5 = [0~1]
                halfLambert *= pow(halfLambert,2);
                half lamberstep = smoothstep(0.01,0.4,halfLambert);//0.01 ~ 0.4之间平滑插值
                half shadowFactor = lerp(0,halfLambert,lamberstep);
                
                //AO
                #if defined(_USE_LIGHTMAP_AO)
                    half ambient = lightMap.g;
                #else
                    half ambient = halfLambert;
                #endif
                half shadow = (ambient + halfLambert) * 0.5f;
                //shadow = 0.95 <= ambient ? 1 : shadow;
                //shadow = ambient <= 0.05 ? 0 : shadow;
                shadow = lerp(shadow,1,step(0.95,ambient));//一亮到底
                shadow = lerp(shadow,0,step(ambient,0.05));//一暗到底
                //是否处于阴影区域
                half isShadowArea = step(shadow,_ShadowPosition);
                half shadowDepth = saturate((_ShadowPosition - shadow) / _ShadowPosition);
                shadowDepth = pow(shadowDepth,_ShadowSoftness);
                shadowDepth = min(shadowDepth,1);//不能超过1
                half rampWidthFactor = vertexColor.g * 2.0f * _ShadowRampWidth;//使用顶点颜色G通道 控制宽度
                half shadowPosition = (_ShadowPosition - shadowFactor) / _ShadowPosition;
                
                //Ramp
                half rampU = 1 - saturate(shadowDepth/ rampWidthFactor);
                // half rampV = RampShadow1D(lightMap.a);
                //根据lightMap的alpha通道 选择ramp行
                half rampID = RampShadow1D(lightMap.a,
                    _UseRampShadow2,_UseRampShadow3,_UseRampShadow4,_UseRampShadow5,
                    1,2,3,4,5);
                half rampV = 0.45f - (rampID - 1.0f) * 0.1f;

                half2 rampDayUV = half2(rampU,rampV + 0.5f);//Ramp上半部分是白天的阴影 下半部分是夜晚的阴影 所以这里 + 0.5f
                half4 rampDayColor = tex2D(_RampTex,rampDayUV);

                half2 rampNightUV = half2(rampU,rampV);
                half4 rampNightColor = tex2D(_RampTex,rampNightUV);
                
                half3 rampColor = lerp(rampDayColor.rgb,rampNightColor.rgb,_DayOrNight);

                #if defined(_USE_RAMP_SHADOW)
                    half3 finalColor = baseMap.rgb * rampColor * (isShadowArea ? 1.0f : 1.2f);
                #else
                    half3 finalColor = baseMap.rgb * halfLambert * (shadow + 0.2f);//+0.2可以让AO更亮一些    
                #endif
                // half3 finalColor = baseMap.rgb * rampColor * (isShadowArea ? 1.0f : 1.2f);
                
                return half4(finalColor.rgb,1.0f);
            }
        ENDHLSL

        Pass
        {//正面渲染
            name "UniversalForward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Cull Back
            HLSLPROGRAM

            
            #pragma vertex MainVS
            #pragma fragment MainFS
            
            // float3 _LightDirection;
            // float3 _LightPosition;
            
            //顶点着色器的数据 是通过CPU传递的，CPU通过指针获取数据
            UniversalVarings MainVS(UniversalAttributes attributes)
            {
                UniversalVarings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(attributes.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;

                VertexNormalInputs verteNormalInput = GetVertexNormalInputs(attributes.normalOS.xyz);
                output.normalWS = verteNormalInput.normalWS;

                output.uv0 = attributes.uv0;
                output.color = attributes.color;
                
                return output;
            }
            
            ENDHLSL
        }

        Pass
        {//背面渲染
            name "UniversalForward"
            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
                "Queue" = "Geometry+1" // 确保渲染顺序 在正面渲染完成之后
            }
            Cull Front
            HLSLPROGRAM

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv0 :  TEXCOORD0;
                float2 uv1 :  TEXCOORD1;
                float3 normalOS : NORMAL;//局部坐标法线,通过cpu传递
                float4 color : COLOR0;//顶点颜色
            };
            
            struct Varings
            {
                float4 positionCS : POSITION;
                float2 uv0 : TEXCOORD0;
                float3 normalWS: TEXCOORD1;//通过顶点着色器 转换到世界坐标系
                float4 color : TEXCOORD2;//从顶点着色器中传递的 顶点颜色
            };

            
            #pragma vertex BackMainVS
            #pragma fragment MainFS
            
            // float3 _LightDirection;
            // float3 _LightPosition;
            
            //顶点着色器的数据 是通过CPU传递的，CPU通过指针获取数据
            Varings BackMainVS(Attributes attributes)
            {
                Varings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(attributes.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;

                VertexNormalInputs verteNormalInput = GetVertexNormalInputs(attributes.normalOS.xyz);
                output.normalWS = -verteNormalInput.normalWS;

                output.uv0 = attributes.uv1;
                output.color = attributes.color;
                
                return output;
            }
            
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode"="ShadowCaster" //投射阴影
            }
            ZWrite On
            ZTest LEqual //距离相机越近 Z值越小
            ColorMask 0 //不写入颜色缓存
            Cull Off
            
            HLSLPROGRAM

            #pragma multi_compile_instancing //GPU实例化编译
            // #pragma multi_compile DOTS_INSTANCING_ON //DOTS实例化编译
            #pragma multi_compile_vertex CASTING_PUNCTUAL_LIGHT_SHADOW//点光源阴影

            #pragma vertex ShadowVS
            #pragma fragment ShadowFS

            //光源方向和位置 是由编译器赋值，
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varings
            {
                float4 positionCS : POSITION;
            };
            
            float4 GetShadowPositionHClip(Attributes input)
            {//将阴影的世界空间顶点位置转换到 裁剪空间
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW//点光源
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS,normalWS,lightDirectionWS));

                #if UNITY_REVERSED_Z //翻转Z缓冲区
                    positionCS.z = min(positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }
            
            Varings ShadowVS(Attributes input)
            {
                Varings output;
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowFS(Varings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
