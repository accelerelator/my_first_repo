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

# 2.Quaternion乘法

```
static operator*(lhs:Quaternion, rhs:Quaternion):Quaternion
static operator*(rotation:Quaternion, point:Vector3):Vector3
```

第一种是计算对一个物体旋转lhs以后再旋转rhs，第二个是计算笛卡尔坐标系下某一个**点**，在经过rotation的旋转之后的位置，返回值是 Vector3

Quaternion.Inverse返回参数的反向转角

FromToRotation(fromDirection,toDirection)从fromDirection到toDirection创建一个旋转。

https://blog.csdn.net/u012200908/article/details/45224557

# 3.Debug.Assert

> Debug.Assert有什么作用？
> 如果第一个参数为false，则使用第二个参数消息（如果提供）记录断言错误。第三个参数是如果在控制台中选择了消息，则在编辑器中突出显示的内容。

举例：在CustomGravity中用list管理GravitySource，每当生成GravitySource，则尝试向list中添加，如果已存在，Debug.Assert将提示

#4.virtual关键字