Shader "Unlit/OutlineUI"

{
    Properties
    {
        // 混合模式设置 - 用于控制透明度和颜色混合
        [Enum(UnityEngine.Rendering.BlendMode)]_MySrcMode("My Src Mode", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)]_MyDstMode("My Dst Mode", Float) = 10
        
        // 主纹理 - UI元素的基础纹理
        _MainTex ("Texture", 2D) = "white" {}
        
        // 噪点纹理 - 用于创建动态噪点效果
        _Noise("Noise", 2D) = "white" {}
        
        // 噪点强度 - 控制噪点的影响程度和方向
        _NoiseIntensity("Noise Intensity", Vector) = (0, 0, 0, 0)
        
        // 描边宽度 - 控制UI元素描边的粗细
        _OutlineWidth("Outline Width", Float) = 0.01
        
        // 开关：是否使用渐变纹理作为描边颜色
        [Toggle(_USETINTOUTLINE)]_UseTintOutline("Use Tint Outline", Float) = 0
        
        // 渐变纹理 - 用于创建彩色描边效果
        _TintTexture("Tint Texture", 2D) = "white" {}
        
        // 描边颜色 - HDR格式支持更鲜艳的颜色
        [HDR]_OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
        
        // 开关：是否只显示描边，不显示原UI
        [Toggle(_OUTLINEONLY)]_OutlineOnly("Outline Only", Float) = 0
        
        // 淡入淡出值 - 控制描边的显示/隐藏过渡
        _Fade("Fade", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags
        {
            // 渲染队列设置为透明，确保正确的渲染顺序
            "Queue" = "Transparent" 
            
            // 支持精灵图集
            "CanUseSpriteAtlas" = "True" 
            
            // 忽略投影器
            "IgnoreProjector" = "True" 
            
            // 渲染类型为透明
            "RenderType" = "Transparent" 
            
            // 预览类型为平面
            "PreviewType" = "Plane"
        }
        
        // 设置混合模式
        Blend [_MySrcMode] [_MyDstMode]
        
        // 关闭背面剔除
        Cull Off
        
        // 关闭深度写入
        ZWrite off

        Pass
        {
            CGPROGRAM
            // 声明顶点着色器和片段着色器函数
            #pragma vertex vert
            #pragma fragment frag
            
            // 定义着色器特性，用于开关控制
            #pragma shader_feature _USETINTOUTLINE
            #pragma shader_feature _OUTLINEONLY

            // 包含Unity的CG函数库
            #include "UnityCG.cginc"

            // 应用程序数据结构 - 从Unity传递到顶点着色器的数据
            struct appdata
            {
                float4 vertex : POSITION; // 顶点位置
                float2 uv : TEXCOORD0;    // 纹理坐标
            };

            // 顶点到片段的数据结构 - 从顶点着色器传递到片段着色器的数据
            struct v2f
            {
                float2 uv : TEXCOORD0;    // 纹理坐标
                float4 vertex : SV_POSITION; // 裁剪空间顶点位置
            };

            // 声明材质属性变量
            sampler2D _MainTex;          // 主纹理
            float4 _MainTex_ST;          // 主纹理变换
            float4 _MainTex_TexelSize;   // 主纹理像素大小
            sampler2D _Noise;            // 噪点纹理
            float4 _Noise_ST;            // 噪点纹理变换
            float4 _NoiseIntensity;      // 噪点强度
            float _OutlineWidth;         // 描边宽度
            sampler2D _TintTexture;      // 渐变纹理
            float4 _TintTexture_ST;      // 渐变纹理变换
            float4 _OutlineColor;        // 描边颜色
            float _Fade;                 // 淡入淡出值

            // 顶点着色器函数
            v2f vert(appdata v)
            {
                v2f o;
                
                // 将顶点位置从模型空间转换到裁剪空间
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // 应用主纹理变换到UV坐标
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }

            // 片段着色器函数
            fixed4 frag(v2f i) : SV_Target
            {
                // 计算噪点效果
                // 采样噪点纹理，并应用时间动画
                float noise = tex2D(_Noise, i.uv * _Noise_ST.xy + _Noise_ST.zw * _Time.y).r;
                noise = noise - 0.5; // 将噪点值范围从[0,1]调整为[-0.5,0.5]
                
                // 计算噪点对UV的偏移
                float2 noiseUV = noise * _NoiseIntensity.xy;
                float2 finUV = i.uv + noiseUV; // 应用噪点偏移后的最终UV
                
                // 计算UV缩放因子，用于将像素空间的描边宽度转换为UV空间
                float2 UVScale = float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
                float2 UVFactor = 100 / UVScale; // 调整描边宽度的因子
                
                // 采样周围8个方向的像素alpha值，用于检测边缘
                float top = tex2D(_MainTex, (float2(0, -1) * _OutlineWidth) * UVFactor + finUV).a;
                float Bottom = tex2D(_MainTex, (float2(0, 1) * _OutlineWidth) * UVFactor + finUV).a;
                float Right = tex2D(_MainTex, (float2(-1, 0) * _OutlineWidth) * UVFactor + finUV).a;
                float Left = tex2D(_MainTex, (float2(1, 0) * _OutlineWidth) * UVFactor + finUV).a;
                float TopLeft = tex2D(_MainTex, (float2(0.705, 0.705) * _OutlineWidth) * UVFactor + finUV).a;
                float TopRight = tex2D(_MainTex, (float2(-0.705, 0.705) * _OutlineWidth) * UVFactor + finUV).a;
                float BottomLeft = tex2D(_MainTex, (float2(0.705, -0.705) * _OutlineWidth) * UVFactor + finUV).a;
                float BottomRight = tex2D(_MainTex, (float2(-0.705, -0.705) * _OutlineWidth) * UVFactor + finUV).a;
                
                // 计算描边掩码 - 取周围像素的最大alpha值
                float outlineMask = max(top, Bottom);
                outlineMask = max(outlineMask, Right);
                outlineMask = max(outlineMask, Left);
                outlineMask = max(outlineMask, TopLeft);
                outlineMask = max(outlineMask, TopRight);
                outlineMask = max(outlineMask, BottomLeft);
                outlineMask = max(outlineMask, BottomRight);
                
                // 增强描边效果
                outlineMask *= 3;
                outlineMask = min(outlineMask, 1); // 确保值在[0,1]范围内
                
                // 计算描边颜色
                // 采样渐变纹理，并应用时间动画
                float3 tintColor = tex2D(_TintTexture, i.uv * _TintTexture_ST.xy + _TintTexture_ST.zw * _Time.y).rgb;
                tintColor *= _OutlineColor.rgb; // 应用描边颜色
                
                // 如果启用了使用渐变纹理的开关，则使用渐变颜色；否则使用纯色
                #ifdef _USETINTOUTLINE
                tintColor = tintColor;
                #else
                tintColor = _OutlineColor.rgb;
                #endif

                // 采样主纹理
                float4 maintex = tex2D(_MainTex, i.uv);
                
                // 计算淡入淡出掩码
                float lerpmask1 = min(_Fade * 3, 1) * (1 - maintex.a);
                
                // 如果启用了只显示描边的开关，则强制显示描边
                #ifdef _OUTLINEONLY
                lerpmask1 = 1.0;
                #endif

                // 混合主纹理颜色和描边颜色
                float3 lerpcolor1 = lerp(maintex.rgb, tintColor, lerpmask1);
                float3 lerpcolor2 = lerp(lerpcolor1, tintColor, lerpmask1);
                
                // 计算最终alpha值
                float op = lerp(maintex.a, outlineMask, _Fade);
                float op1 = op * min(_Fade * 3, 1) * (1 - maintex.a);
                float alpha = 0;
                
                // 根据开关决定alpha值的计算方式
                #ifdef _OUTLINEONLY
                alpha = op1;
                #else
                alpha = op;
                #endif

                // 构建最终颜色
                float4 col;
                col.rgb = lerpcolor2 * alpha;
                col.a = alpha;

                return col;
            }
            ENDCG
        }
    }
}