// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Scan With Edge" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_MousePos ("mouse position",Vector) = (0,0,0)
		_MaxRange("max scanf range",Float) = 1
		_ScanfRange("now scanf range",Float) = 1
		_ScanfColor("color",Color) = (1,1,1,1)

		_EdgeOnly ("Edge Only", Float) = 1.0
		_EdgeColor ("Edge Color", Color) = (0.4,0.6,0.85,0.8)
		_BackgroundColor ("Background Color", Color) = (0.4,0.6,0.85,0.8)
		_SampleDistance ("Sample Distance", Float) = 1.0
		_Sensitivity ("Sensitivity", Vector) = (1, 1, 1, 1)
	
	}
	SubShader {
	//Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		CGINCLUDE
		
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		half4 _MainTex_TexelSize;

		fixed4 _ScanfColor;
		float4x4 _FrustumCornersRay;
		float3 _MousePos;
		float _ScanfRange,_MaxRange;

		sampler2D _CameraDepthNormalsTexture;
		sampler2D _CameraDepthTexture;

		fixed _EdgeOnly;
		fixed4 _EdgeColor;
		fixed4 _BackgroundColor;
		float _SampleDistance;
		half4 _Sensitivity;

		float _LastScanTime;
		struct v2f {
			float4 pos : SV_POSITION;
			half2 uv : TEXCOORD0;
			half2 uv_depth : TEXCOORD1;
			float4 interpolatedRay : TEXCOORD2;

			half2 edge_uv[5]: TEXCOORD3;
		};
		
		v2f vert(appdata_img v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			
			o.uv = v.texcoord;
			o.uv_depth = v.texcoord;

			half2 preuv = v.texcoord;
			o.uv[0] = preuv;

			#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0)
				o.uv_depth.y = 1 - o.uv_depth.y;
			#endif
			
			int index = 0;
			if (v.texcoord.x < 0.5 && v.texcoord.y < 0.5) {
				index = 0;
			} else if (v.texcoord.x > 0.5 && v.texcoord.y < 0.5) {
				index = 1;
			} else if (v.texcoord.x > 0.5 && v.texcoord.y > 0.5) {
				index = 2;
			} else {
				index = 3;
			}

			#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0)
				index = 3 - index;
			#endif
			
			o.interpolatedRay = _FrustumCornersRay[index];
			o.edge_uv[1] = preuv + _MainTex_TexelSize.xy * half2(1,1) * _SampleDistance;
			o.edge_uv[2] = preuv + _MainTex_TexelSize.xy * half2(-1,-1) * _SampleDistance;
			o.edge_uv[3] = preuv + _MainTex_TexelSize.xy * half2(-1,1) * _SampleDistance;
			o.edge_uv[4] = preuv + _MainTex_TexelSize.xy * half2(1,-1) * _SampleDistance;
			return o;
		}
		fixed4 Scanf(float3 worldPos)
		{
			float pixelDistance = distance(worldPos,_MousePos) ;
			if(pixelDistance<_ScanfRange)
			{
				float scanfPercent = 1 - (_ScanfRange - pixelDistance)/5 ;
				float maxPercent = 1 - (_MaxRange - pixelDistance)/5;
				float percent = lerp(0,1,saturate((_MaxRange - _ScanfRange)/(_MaxRange-pixelDistance)));
				_ScanfColor.a*=percent;
				return _ScanfColor;
			}
			return  0;
			
		}
		half CheckSame(half4 center, half4 sample) {
			half2 centerNormal = center.xy;
			float centerDepth = DecodeFloatRG(center.zw);
			half2 sampleNormal = sample.xy;
			float sampleDepth = DecodeFloatRG(sample.zw);
			
			// difference in normals
			// do not bother decoding normals - there's no need here
			half2 diffNormal = abs(centerNormal - sampleNormal) * _Sensitivity.x;
			int isSameNormal = (diffNormal.x + diffNormal.y) < 0.1;
			// difference in depth
			float diffDepth = abs(centerDepth - sampleDepth) * _Sensitivity.y;
			// scale the required threshold by the distance
			int isSameDepth = diffDepth < 0.1 * centerDepth;
			
			// return:
			// 1 - if normals and depth are similar enough
			// 0 - otherwise
			return isSameNormal * isSameDepth ? 1.0 : 0.0;
		}
		fixed4 frag(v2f i) : SV_Target {
			float linearDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv_depth));
			float3 worldPos = _WorldSpaceCameraPos + linearDepth * i.interpolatedRay.xyz;
			
			half4 sample1 = tex2D(_CameraDepthNormalsTexture, i.edge_uv[1]);
			half4 sample2 = tex2D(_CameraDepthNormalsTexture, i.edge_uv[2]);
			half4 sample3 = tex2D(_CameraDepthNormalsTexture, i.edge_uv[3]);
			half4 sample4 = tex2D(_CameraDepthNormalsTexture, i.edge_uv[4]);
			
			half edge = 1.0;
			
			edge *= CheckSame(sample1, sample2);
			edge *= CheckSame(sample3, sample4);
			
			fixed4 withEdgeColor = lerp(_EdgeColor, tex2D(_MainTex, i.edge_uv[0]), edge);
			fixed4 onlyEdgeColor = lerp(_EdgeColor, _BackgroundColor, edge);
			fixed4 finnalEdgeColor= lerp(withEdgeColor, onlyEdgeColor, _EdgeOnly);

			fixed4 finalColor = tex2D(_MainTex, i.uv);

			float pixelDistance = distance(worldPos,_MousePos) ;
			if(pixelDistance<_ScanfRange)
			{
				float percent = saturate( (_MaxRange - _ScanfRange) 
				/(_MaxRange-pixelDistance) ) ;
				float edgepercent = saturate((_MaxRange - _ScanfRange) 
				/(_MaxRange-pixelDistance)) ;
				float edgeCircleMask2  = 1; 
				if((abs(abs(sin(pixelDistance*10)) - 1)) >= 0.1f)
				{
					edgeCircleMask2 = 0;
				}
				else if(pixelDistance < 4)
				{
					edgeCircleMask2 = 0;
				}
				finalColor += 
				_ScanfColor * (edgeCircleMask2) * pow(percent,1)
				+finnalEdgeColor* edgepercent;
				return finalColor;
			}
			else{
				_LastScanTime = _Time.y;
			}
			return finalColor;
		}
		
		ENDCG
		
		Pass {
			
			ZTest Always Cull Off ZWrite Off
			     	
			CGPROGRAM  
			
			#pragma vertex vert  
			#pragma fragment frag  
			  
			ENDCG  
		}
	} 
	FallBack "Transparent/VertexLit"
}
