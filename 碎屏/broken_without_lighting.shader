Shader "Custom/RenderImage/nolightScreenBroken" {
	Properties {
		_MainTex ("Main Tex", 2D) = "white" {}

		_BrokenNormalMap("BrokenNormal Map",2D)="bump"{}
		_LuminanceScale("LuminanceScale", Range(0, 1.0))=0.25
		_BrokenScale("BrokenScale",Range(0, 0.2))=0.1

	}
	SubShader {
		
 
		Pass{
			Tags { "LightMode"="ForwardBase" }
 
			CGPROGRAM
 
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
 
			#pragma vertex vert
			#pragma fragment frag
 
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _BrokenNormalMap;
			float4 _BrokenNormalMap_ST;
			float _BrokenScale;
			float _LuminanceScale;
 
			struct a2v{
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;

			};
 
			struct v2f{
				float4 pos : SV_POSITION;
				float4 uv : TEXCOORD0;

			};
 
			v2f vert(a2v v){
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				
				o.uv.xy=TRANSFORM_TEX(v.texcoord,_MainTex);

				o.uv.zw=TRANSFORM_TEX(v.texcoord, _BrokenNormalMap);

				
				return o;
			}
 
			fixed4 frag(v2f i) : SV_Target{


				//片元对应法线贴图采样
				fixed4 packedNormal = tex2D(_BrokenNormalMap,i.uv.zw);
				fixed3 tangentNormal;
				//将颜色转换成法线方向
				tangentNormal=UnpackNormal(packedNormal);
				//法线方向倾斜系数
				tangentNormal.xy *= _BrokenScale;
				tangentNormal.z = sqrt(1-saturate(dot (tangentNormal.xy,tangentNormal.xy)));
				float2 offset =  tangentNormal.xy;
				
				//如果没有offset 则每个片元则按照
				//自己在主帖图的原uv位置读取颜色值
				//加了offset表示的意思是原uv位置基础上
				//加上片元对应玻璃破碎法线贴图上法线偏移值来读取颜色值
				//原代码
				fixed3 col= tex2D(_MainTex, i.uv.xy + offset).rgb;
				
				//return fixed4(col,1.0);
				//以下是使画面变灰的代码
				//将颜色的rgb值全部变成颜色均值
				//rgb值一致的时候 颜色是属于白灰黑色系
				//值越小越偏黑 越偏大越偏白 中间过渡阶段是各种灰色
				fixed luminance = (col.r + col.g + col.b) / 3;
				fixed3 gray = fixed3(luminance,luminance,luminance);
				fixed3 finalCol = lerp(gray, col,_LuminanceScale);
				return fixed4(gray, 1.0f);
			}
			ENDCG
			}
		
		}
	FallBack "Diffuse"
}