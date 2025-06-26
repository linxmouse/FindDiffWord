Shader "Custom/UVCrop"
{
    Properties
    {
        _MainTex ("纹理", 2D) = "white" {}
        _Color ("着色", Color) = (1,1,1,1)
        _UVRect ("裁剪参数", Vector) = (0,0,1,1)  // x=cropLeft, y=cropTop, z=1.0f - cropRight, w=1.0f - cropBottom
    }
    
    SubShader
    {
        // 渲染标签：设置为透明队列，支持alpha混合
        Tags
        {
            "Queue"="Transparent"           // 在透明队列中渲染
            "IgnoreProjector"="True"        // 忽略投影器
            "RenderType"="Transparent"      // 渲染类型为透明
            "PreviewType"="Plane"           // 预览类型
            "CanUseSpriteAtlas"="True"      // 支持精灵图集
        }
        
        // 渲染状态设置
        Cull Off                            // 不剔除任何面（正面和背面都渲染）
        Lighting Off                        // 关闭光照计算
        ZWrite Off                          // 不写入深度缓冲
        Blend SrcAlpha OneMinusSrcAlpha     // 标准alpha混合：源颜色*源alpha + 目标颜色*(1-源alpha)
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert             // 指定顶点着色器函数名
            #pragma fragment frag           // 指定片段着色器函数名
            #include "UnityCG.cginc"        // 包含Unity的通用CG函数库
            
            // 顶点着色器的输入结构（从CPU传入的顶点数据）
            struct appdata_t
            {
                float4 vertex : POSITION;       // 顶点位置（世界空间）
                float2 texcoord : TEXCOORD0;    // UV坐标
                float4 color : COLOR;           // 顶点颜色
            };
            
            // 顶点着色器到片段着色器的数据传递结构
            struct v2f
            {
                float4 vertex : SV_POSITION;    // 屏幕空间位置（必须）
                float2 texcoord : TEXCOORD0;    // 纹理采样用的UV坐标
                float2 originalUV : TEXCOORD1;  // 原始UI的UV坐标（用于裁剪判断）
                float4 color : COLOR;           // 传递给片段着色器的颜色
            };
            
            // 着色器属性（与Properties中的对应）
            sampler2D _MainTex;         // 主纹理采样器
            float4 _MainTex_ST;         // 纹理的缩放和偏移参数（Scale, Offset）
            fixed4 _Color;              // 着色颜色
            float4 _UVRect;             // 裁剪参数向量
            
            // 顶点着色器：处理每个顶点，将顶点从模型空间转换到屏幕空间
            v2f vert(appdata_t IN)
            {
                v2f OUT;                
                // 将顶点从对象空间转换到裁剪空间（屏幕空间）
                OUT.vertex = UnityObjectToClipPos(IN.vertex);                
                // 保存原始UV坐标，用于后面的裁剪边界检查
                OUT.originalUV = IN.texcoord;               
                // 设置纹理采样的UV坐标（保持原始比例，不做变换）
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);                
                // 传递颜色（顶点颜色 * 材质颜色）
                OUT.color = IN.color * _Color;                
                return OUT;
            }
            
            // 片段着色器：处理每个像素，决定最终的颜色输出
            fixed4 frag(v2f IN) : SV_Target
            {
                // 获取当前像素的UV坐标
                float2 uv = IN.originalUV;
                float cropLeft = _UVRect.x;         // x 从左边裁切的量
                float cropRight = _UVRect.z;        // 1-z 从右边裁切的量
                float cropBottom = _UVRect.y;       // y 从下边裁切的量
                float cropTop = _UVRect.w;          // 1-w 从上边裁切的量               
                // 检查当前像素是否在显示区域内
                if (uv.x < cropLeft || uv.x > cropRight || 
                    uv.y < cropBottom || uv.y > cropTop)
                {
                    discard; // 丢弃超出边界的像素(透明)
                }
                // 在显示区域内，使用原始UV坐标采样纹理
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                // 返回最终颜色
                return c;
            }
            ENDCG
        }
    }
} 