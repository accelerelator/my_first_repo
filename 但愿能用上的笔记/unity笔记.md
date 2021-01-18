# 1.Raycast忽略部分物体

![图像_2021-01-17_182124.png](https://i.loli.net/2021/01/17/dYHurEWDQNmOLzM.png)

![图像_2021-01-17_182431.png](https://i.loli.net/2021/01/17/KUkVezYCBET2XLN.png)

maxDistance是此射线检测的最大距离，layerMask代表的物体才会被检测

raycast只检查置于Ignore Raycast层上的对象，不想让某些物体被检查，首先要新建一个低于Ignore Raycast层的层，接着在这些物体上添加参数。

```
[SerializeField]
LayerMask probeMask = -1;
```

-1代表全部层，在unity内修改这些物体属于哪些层。