# 1.流体纹理

## 1.1



材质有两张纹理

```
sampler2D _MainTex, _FlowMap;
```

对主纹理采样的uv坐标，受时间和flowmap的rg值影响，前者影响流动速度，后者影响流动方向

注意对flowmap采样后要转换到[0,1]内

```
UV  = uv - flowfactor * time.y
```

然而时间推移下偏移回越来越大，所以要让偏移按周期循环

```
UV  = uv - flowfactor * frac(time.y)
```

以下是关键的着色器，flowfactor即流体采样结果

```
#pragma surface surf Standard fullforwardshadows
#pragma target 3.0
void surf (Input IN, inout SurfaceOutputStandard o) {
			float2 flowVector = tex2D(_FlowMap, IN.uv_MainTex).rg * 2 - 1;
			float2 uv = FlowUV(IN.uv_MainTex, flowVector, _Time.y);
			fixed4 c = tex2D (_MainTex, uv) * _Color;
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
```

此时，uv偏移周期如下

![图像_2021-01-27_102826.png](https://i.loli.net/2021/01/27/QaS2hpqJ8u7OFVT.png)

## 1.2

此时纹理会周期性突变回初始值，需要修改。思路是采样时加入一个混合纹理，**快要到突变周期时**让颜色逐渐变黑以实现过渡。

原采样

```
c = tex2D (_MainTex, uv) * _Color;
```

新采样

```
c = tex2D (_MainTex, uvw.xy) * uvw.z * _Color;
```

这个权重z可以是这样的

![图像_2021-01-27_103236.png](https://i.loli.net/2021/01/27/yR3JQb6k5xATK8Z.png)

所以有

```
float progress = frac(time.y)
uvw.xy = uv - flowfactor * frac(progress)
uvw.z = 1 - abs (1- 2 * progress)
```

## 1.3

现在流体会周期性变全黑，加入噪声稍微改善效果

噪声贴图获取，"Flow (RG, A noise)"表示期望图片内含噪声的标签

```
[NoScaleOffset] _FlowMap ("Flow (RG, A noise)", 2D) = "black" {}
```

噪声影响时间

```
float noise = tex2D(_FlowMap, IN.uv_MainTex).a;
float time = _Time.y * _Speed + noise;
float3 uvw = FlowUVW(IN.uv_MainTex, flowVector,time);
```

现在情况变为周期性局部变黑，对同一纹理采样两次，实现A纹理权重0时B纹理权重1的效果

![图像_2021-01-27_102826.png](https://i.loli.net/2021/01/27/QaS2hpqJ8u7OFVT.png)

```
float3 uvwA = FlowUVW(IN.uv_MainTex, flowVector, time, false);
float3 uvwB = FlowUVW(IN.uv_MainTex, flowVector,time, true);
fixed4 texA = tex2D(_MainTex, uvwA.xy) * uvwA.z;
fixed4 texB = tex2D(_MainTex, uvwB.xy) * uvwB.z;
fixed4 c = (texA + texB) * _Color;
```

传参true则时间偏移0.5实现两个纹理周期交替

```
float3 FlowUVW (...) 
{
	float phaseOffset = flowB ? 0.5 : 0;
	float progress = frac(time + phaseOffset);
...	}
```

uv也可受偏移影响，这样两个纹理图案不相同

```
uvw.xy = uv - flowVector * (progress + flowOffset);
```

## 1.4

此时画面效果基本完成，变化周期为1秒，添加一些参数控制最终效果

平铺属性影响单位面积内的纹理大小，纹理越小，看起来就越密集，注意 tiling要在phaseOffset添加之前

```
uvw.xy = uv - flowVector * (progress );
uvw.xy *= tiling;
uvw.xy += phaseOffset;
```

速度

```
//旧
float time = _Time.y + noise;
//新
float time = _Time.y * _Speed + noise;
```

流动强度

```
float2 flowVector = tex2D(_FlowMap, IN.uv_MainTex).rg * 2 - 1;
//新
flowVector *= _FlowStrength;
```

_FlowOffset 这个属性可理解为改变纹理的初始相位

```

uvw.xy = uv - flowVector * (progress + flowOffset);
```

通常只取0或-0.5，取-0.5时progress + flowOffset范围在[-0.5,0.5]，直观来说就是此时图案的扭曲程度较小

## 1.5

最后是添加法线贴图阶段。

基础的法线贴图代码：

```
float3 normalA = UnpackNormal(tex2D (_NormalMap,uvwA.xy)) * uvwA.z;

float3 normalB = UnpackNormal(tex2D (_NormalMap,uvwB.xy)) * uvwB.z;

o.normal = normalize(normalA +normalB)

```

然而为了方便操作，我们使用导数纹理，这张纹理A通道是x方向导数，G通道是Y方向导数，B通道是原始的高度值。以下是读取这种纹理的代码

```
float3 UnpackDerivativeHeight (float4 textureData) 
{
float3 dh = textureData.agb;
dh.xy = dh.xy * 2 - 1;
return dh;
}
//注意是agb	

float3 dhA =UnpackDerivativeHeight(tex2D(_DerivHeightMap, uvwA.xy)) *
(uvwA.z * finalHeightScale);

float3 dhB =
UnpackDerivativeHeight(tex2D(_DerivHeightMap, uvwB.xy)) *
(uvwB.z * finalHeightScale);

o.Normal = normalize(float3(-(dhA.xy + dhB.xy), 1));
```

finalHeightScale可以方便地对最终高度做缩放

# 2.方向流体

这种流体使用两张纹理，主纹理按照flowmap给定的方向旋转，实现flowmap指定的流动方向。

关键和上一节一样，由flowmap即导数纹理得到dh，再由高度平方和颜色相乘得到最终结果。

```
fixed4 c = dh.z * dh.z * _Color;
```

以一张纹理为单位旋转会造成失真，因此我们把纹理拆分成多个方块，在方块内主纹理按照flowmap无失真地旋转，再采用一系列重叠得到最终结果。

2.1纹理旋转

我们先来看怎么在划分纹理的同时按照flowmap实现旋转（对应代码中FlowCell函数）

![图像_2021-01-28_092000.png](https://i.loli.net/2021/01/28/2C7z1cEtuM6YOX4.png)

```
float3 FlowCell (float2 uv, float2 offset, float time)
```

offset是等会多次采样时对应的偏移，分别是00,10,01,11。

得到偏移后的uv坐标，offset10为例，此时坐标轴向u正向（右）移动1，图像上则向左移动。这个公式则是划分纹理的基本公式，加上偏移得到最终的uv坐标进对flowmap行采样。注意，采样得到的是流向flow。
$$
floor(uv * _GridResolution) / _GridResolution
$$


```
float2 uvTiled =(floor(uv * _GridResolution + offset) + shift) / _GridResolution;
float3 flow = tex2D(_FlowMap, uvTiled).rgb;
```

接下来用上一节的代码，用主纹理uv+flow得到结果

```
float tiling = flow.z * _TilingModulated + _Tiling;

float2 uvFlow = DirectionalFlowUV(uv, flow, tiling, time,derivRotation);
float3 dh = UnpackDerivativeHeight(tex2D(_MainTex, uvFlow));
```

注意上面derivRotation是在DirectionalFlowUV中计算完out的旋转矩阵，这个矩阵最终作用于dh。

```
dh.xy = mul(derivRotation, dh.xy);
dh *= flow.z *_HeightScaleModulated + _HeightScale;
```

接下来多次采样再叠加，注意叠加是A纹理（1-t），B纹理t的的权重比例。

```
float3 dhA = FlowCell(uv, float2(0, 0), time);
float3 dhB = FlowCell(uv, float2(1, 0), time);
float3 dhC = FlowCell(uv, float2(0, 1), time);
float3 dhD = FlowCell(uv, float2(1, 1), time);

float2 t = abs(2 * frac(uv * _GridResolution) - 1);

float wA = (1 - t.x) * (1 - t.y);
float wB = t.x * (1 - t.y);
float wC = (1 - t.x) * t.y;
float wD = t.x * t.y;
float3 dh = dhA * wA + dhB * wB + dhC * wC + dhD * wD;

fixed4 c = dh.z * dh.z * _Color;
```

读了好久教程，放几张图方便以后回忆

这是未经过偏移 + 划分后的纹理

![图像_2021-01-28_093817.png](https://i.loli.net/2021/01/28/KUIsyShdqMWYgJe.png)

这是offset(1,0)的纹理，能看出图像是上图左移一格的结果

![图像_2021-01-28_093942.png](https://i.loli.net/2021/01/28/yoUDbBZiaY75rev.png)

然后在cell函数里offset*= 0.5 

![图像_2021-01-28_094041.png](https://i.loli.net/2021/01/28/9CItiMgrBHc6Eum.png)

对于每个单元格，按A(1-t),B(t)的权重混合

```
float2 t = abs(2 * frac(uv * _GridResolution) - 1);
```

这样u方向的混合就完成了

![图像_2021-01-28_094323.png](https://i.loli.net/2021/01/28/dzkQEVb52yslFmP.png)



# 3.波浪

填坑待补

# 4.水面透视

## 4.1水下雾效

雾效的关键都在于由**深度纹理重建像素的世界坐标**，接下来写下我对此处代码以及unityshader 入门精要中13.3代码的对比理解。

为什么需要像素的世界坐标？因为像素离相机越远，雾就越浓。具体像素最终颜色的计算，无非是**纹理采样颜色（用深度影响的雾效系数插值）-> 雾效本身颜色**。雾效系数计算方法很多，但都主要和像素深度有关。

视角空间下的线性深度值，两份代码中共同需要的部分

```
float linearDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv_depth));
```

精要中的代码直接获取了具体像素的世界坐标，简要记录如下。

点的世界坐标= 相机的世界坐标（_WorldSpaceCameraPos）+ **深度纹理中该像素的深度值** * 　interpolatedRay 。对于特定时刻，已知由相机原点指向近裁剪平面的四个角能得到四个不同的向量，interpolatedRay就是这四个向量中离像素最近的那一个。最终系数使用的是worldpos.y

```
float3 worldPos = _WorldSpaceCameraPos + linearDepth * i.interpolatedRay.xyz;
```

CatLikeCoding教程里，ColorBelowWater函数计算水下的雾效颜色，因此只获取了水到像素的距离。

```
o.Emission = ColorBelowWater(IN.screenPos) * (1 - c.a);
```

screenPos屏幕空间坐标

```
//深度纹理不检测水面物体，这个是水面下物体相对相机的距离
	float backgroundDepth =
		LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
	//水体片元对屏幕的距离
	float surfaceDepth = UNITY_Z_0_FAR_FROM_CLIPSPACE(screenPos.z);
	//水相对水底的距离，用来求雾效系数
	float depthDifference = backgroundDepth - surfaceDepth;
```

直接return depthDifference已经能看到距离对雾效浓度的影响了，但是我们需要把当前颜色和雾的颜色混合起来，所以在shader里使用了grabpass

```
GrabPass { "_WaterBackground" }
```

在函数定义文件里使用深度纹理和屏幕抓取图像

```
sampler2D _CameraDepthTexture, _WaterBackground;
...
float3 backgroundColor = tex2D(_WaterBackground, uv).rgb;
float fogFactor = exp2(-_WaterFogDensity * depthDifference);
return lerp(_WaterFogColor, backgroundColor, fogFactor);
```

在最后的使用里，我们是把抓取并处理后的结果作为最终的颜色，这个颜色不应受任何透明度影响（即透明度应为1）

```
o.Emission = ColorBelowWater(IN.screenPos) * (1 - c.a);
```

我们用color函数来处理这一步

![图像_2021-01-31_092855.png](https://i.loli.net/2021/01/31/DC4OInG8btFTB3X.png)

```
#pragma surface surf Standard alpha finalcolor:ResetAlpha

void ResetAlpha (Input IN, SurfaceOutputStandard o, inout fixed4 color) {
			color.a = 1;
		}
```

