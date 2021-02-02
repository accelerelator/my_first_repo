# 扫描效果模仿

## 原理



灵感和部分思路来源是https://zhuanlan.zhihu.com/p/347319259，感谢作者分享，侵删。

然而我挠头看了半天没能完全理解，无奈按自己的理解写完了最终效果，以下记录大致过程。

![Scanf.gif](https://i.loli.net/2021/02/02/o3Pn12FdzpHZRGB.gif)

## 脚本



首先我们需要检测鼠标点击位置**像素对应的世界坐标**，然后把坐标传参到自己写的GraphControl脚本里，在那个脚本里完成脚本到shader的传参。

```
if (Input.GetMouseButtonDown(0))
{
            scanfrange = 0;
            f = true;
            Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                GetComponent<GraphControl>().MousePos = new Vector4(hit.point.x, hit.point.y, hit.point.z, 0);
            }
}
```

shader的主要原理是robert边缘检测和基于深度纹理重建世界坐标，这两点在《入门精要》里正好都讲到了，于是主要的实现我就照搬了实例的代码。

重建坐标需要的转换矩阵

```
material.SetMatrix("_FrustumCornersRay", frustumCorners);
```

边缘检测需要的一些参数

```
material.SetFloat("_EdgeOnly", edgesOnly);
            material.SetColor("_EdgeColor", edgeColor);
            material.SetColor("_BackgroundColor", backgroundColor);
            material.SetFloat("_SampleDistance", sampleDistance);
            material.SetVector("_Sensitivity", 
                new Vector4(sensitivityNormals, sensitivityDepth, 0.0f, 0.0f));
```

以及，这个扫描边缘处会有一些条纹，我把产生影响的部分参数开放了

```
material.SetFloat("_MaxRange", MaxRange);
 material.SetColor("_ScanfColor", ScanfColor);
material.SetFloat("_ScanfRange", ScanfRange);
material.SetVector("_MousePos",MousePos);
```

是的，我为了偷懒把两个功能写在一个shader里了（逃

接着让shader影响屏幕画面

```
Graphics.Blit(src, dest, material);
```

鼠标检测，画面后处理两个脚本挂相机上

## shader

顶点着色器代码着实有点长就不写了，总之片元里会得到当前像素的世界坐标worldPos，以及按给定比例混合的边缘颜色finnalEdgeColor

```
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
```

配合鼠标位置，我们只考虑当前扫描到的距离>像素离鼠标位置的距离 的像素

```
float pixelDistance = distance(worldPos,_MousePos) ;
if(pixelDistance<_ScanfRange)...
else return finalColor;
```

如果当前像素被扫描到了，我们计算像素到扫描边界这段距离里，当前扫描线走到了哪里，并得到一个比例

```
float percent = saturate( (_MaxRange - _ScanfRange) 
				/(_MaxRange-pixelDistance) ) ;
```

已知我们最后返回的颜色值无非是在采样颜色上加上扫描颜色和边缘检测，这个扫描颜色需要走的比扫描线慢一些所以让percent乘以扫描颜色再加上去，就有渐隐效果了。

然后是条纹，我们考虑 当前像素到鼠标距离 这个数值的sin值，我们只在这个sin绝对值达到 1 时才加入条纹颜色，就得到了edgeCircleMask2

为什么要乘10?提高sin的频率，直接让条纹变得更密集

至于-1，是绝对值离1 相差不超过0.1 就算条纹存在，0.1影响宽度

```
if((abs(abs(sin(pixelDistance*10)) - 1)) >= 0.1f)
{
	edgeCircleMask2 = 0;
}
else if(pixelDistance < 4)
{
	edgeCircleMask2 = 0;
}
finalColor += _ScanfColor * (edgeCircleMask2) * pow(percent,1);
```

最后让混合了边缘颜色的 颜色 加上去,这个edgepercent可以调整参数，让边缘消失的快些或慢些

```
float percent = saturate( (_MaxRange - _ScanfRange) 
				/(_MaxRange-pixelDistance) ) ;
float edgepercent = saturate((_MaxRange - _ScanfRange) 
				/(_MaxRange-pixelDistance)) ;
...+finnalEdgeColor* edgepercent;
```

## 挖坑

游戏内扫描后一段时间边缘会保留下来，然而我的代码在扫描线走完后就自动归零了，导致扫描线也没了，目前还没想到什么好办法，挖坑待填。