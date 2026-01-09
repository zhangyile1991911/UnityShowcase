Shader "ToonShader/Face"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        
        [Header(Shadow Options)]
        [Toggle(_USE_SDF_SHADOW)] _USE_SDF_SHADOW ("Use SDF Shadow",Range(0,1)) = 1
        _SDF("SDF",2D) = "white"{}
        _ShadowMask("ShadowMask",2D) = "white"{}
        _ShadowColor("ShadowColor",Color) = (1,0.87,0.87,1)
        
        [Header(Head Direction)]
        [HideInInspector]_HeadForward("Head Forward",Vector) = (0,0,1,0)//在Inspector中隐藏，通过脚本传递
        [HideInInspector]_HeadRight("Head Right",Vector) = (1,0,0,0)
        [HideInInspector]_HeadUp("Head Up",Vector) = (0,1,0,0)
        
        _FaceBlushColor("Face Blush Color",Color) =(1,0,0,1)
        _FaceBlushStrength("Face Blush Strength",Float) = 0
    }
    SubShader
    {
        Tags {
            "RenderPipeline"="UniversalRenderPipeline"
            "RenderType"="Opaque"
        }
        HLSLINCLUDE
            #pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile _ADDITIONAL_LIGHTS

            #pragma multi_compile _MAIN_LIGHT_SHADOWS //主光源阴影
            #pragma multi_compile _MAIN_LIGHT_SHADOWS_CASCADE //主光源阴影级联
            #pragma multi_compile _MAIN_LIGHT_SHADOWS_SCREEN //主光源阴影屏幕空间

            #pragma multi_compile_fragment  _LIGHT_LAYERS //光照层
            #pragma multi_compile_fragment  _LIGHT_COOKIES //光照
            #pragma multi_compile_fragment  _SCREEN_SPACE_OCCLUSION //屏幕遮挡
            #pragma multi_compile_fragment  _ADDITIONAL_LIGHT_SHADOWS //额外光照阴影
            #pragma multi_compile_fragment  _SHADOWS_SOFT //阴影软化

            #pragma shader_feature_local _USE_SDF_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                
                sampler2D _BaseMap;
            
                sampler2D _SDF;//
                sampler2D _ShadowMask;//阴影选择

                float4 _ShadowColor;
                float3 _HeadForward;
                float3 _HeadRight;
                float3 _HeadUp;
                float4 _FaceBlushColor;
                float _FaceBlushStrength;
            CBUFFER_END
        ENDHLSL

        Pass
        {
            name "FaceColor"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            HLSLPROGRAM
             struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv0 :  TEXCOORD0;
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

            
            #pragma vertex MainVS
            #pragma fragment MainFS
            //顶点着色器的数据 是通过CPU传递的，CPU通过指针获取数据
            Varings MainVS(Attributes attributes)
            {
                Varings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(attributes.positionOS);
                output.positionCS = vertexInput.positionCS;

                VertexNormalInputs verteNormalInput = GetVertexNormalInputs(attributes.normalOS);
                output.normalWS = verteNormalInput.normalWS;

                output.uv0 = attributes.uv0;
                output.color = attributes.color;
                
                return output;
            }

            //1 半兰伯特
            //2 环境光遮蔽 让物体表面 缝隙 凹进去的地方 更暗，使用贴图实现lightmap
            //R 控制高光，金属
            //G 控制阴影AO 衣服褶皱
            //B 高光强度
            half4 MainFS(Varings input) : SV_TARGET
            {
                Light mainLight = GetMainLight();
                // float3 L = normalize(float3(0.3f,-.9f,0.3f));
                
                float3 L = normalize(mainLight.direction);
                
                float3 N = normalize(input.normalWS);
                float NoL = dot(N,L);//法线->光源

                //归一化 向量
                half3 headUpDir = normalize(_HeadUp);
                half3 headForwardDir = normalize(_HeadForward);
                half3 headRightDir = normalize(_HeadRight);

                
                half4 baseMap = tex2D(_BaseMap,input.uv0);
                half4 shadowMask = tex2D(_ShadowMask,input.uv0);

                half lambert = NoL;
                half halfLambert = lambert * 0.5f + 0.5f;
                halfLambert *= pow(halfLambert,2);

                //面部阴影
                half3 LpU = dot(L,headUpDir) / pow(length(headUpDir),2) * headUpDir;//光源方向在面部上方投影
                half3 LpHeadHorizon = normalize(L - LpU);//头部水平面上投影
                half value = acos(dot(LpHeadHorizon,headRightDir));//光照方向和有方向夹角
                half exposeRight = step(value,0.5);//判断光照来自 左还是右
                half valueR = pow(1 - value * 2,3);//右侧阴影强度
                half valueL = pow(value * 2 - 1,3);//左侧阴影强度
                half mixValue = lerp(valueL,valueR,exposeRight);//混合
                half sdfLeft = tex2D(_SDF,half2(1-input.uv0.x,input.uv0.y)).r;//左侧距离场
                half sdfRight = tex2D(_SDF,input.uv0).r;//右侧距离场
                
                half mixSDF = lerp(sdfRight,sdfLeft,exposeRight);
                half sdf = step(mixValue,mixSDF);//硬边界阴影
                sdf = lerp(0,sdf,step(0,dot(LpHeadHorizon,headForwardDir)));//右侧阴影
                sdf *= shadowMask.g;//控制阴影强度
                //记录一个踩过的坑,Mask贴图设置 TextureType设置为Default,TextureShape=2D,Alpha Source设置为InputTexutreAlpha
                sdf = lerp(sdf,1,shadowMask.a);//阴影遮罩
                
                #if defined(_USE_SDF_SHADOW)
                    half3 finalColor = lerp(_ShadowColor.rgb * baseMap.rgb,baseMap.rgb,sdf);
                #else
                    half3 finalColor = baseMap.rgb * halfLambert;
                #endif
                //基础颜色贴图中的alpha值 存放着面部腮红信息
                // return baseMap.aaaa;
                half blushStrength = lerp(0,baseMap.a,_FaceBlushStrength);
                finalColor = lerp(finalColor,finalColor * _FaceBlushColor, blushStrength); ;
                
                return half4(finalColor,1.0f);
                
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
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                //点光源
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
