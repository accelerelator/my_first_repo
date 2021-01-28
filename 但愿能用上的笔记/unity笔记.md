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

最后可以加上QueryTriggerInteraction.Ignore参数，用于无视trigger碰撞体

QueryTriggerInteraction.Collide则是确保检测trigger

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

#4.射线检测延迟

在运动物体上添加射线检测时，如果物体速度过快会出现检测bug，解决办法是延长射线检测的距离，提前检测确保正确效果。

bug：CatLikeCoding基础9，物体加速到一定速度离开水面后，在完全离开水面的情况下会有几帧判断成物体完全在水中。

解释：首先，检测是否在水中是通过从物体顶端向下发射长度为物体高度的射线判断是否碰撞水体trigger实现的，物体颜色深度与“浸水程度”正相关。

如果速度够快，当物体离开水面后，按道理此时物体的材质应由于物体离开水面切换成地面对应的材质，但由于函数调用顺序问题有几帧仍认为物体在水里，因而没有切换材质，依然进行了射线检测，又因为射线距离只有物体高度那么长，因此没碰到任何碰撞体=>颜色和完全在水中一样。这个设计是为了确保完全入水时显示正确而存在的。

总而言之，**加长射线距离**确能在延迟状态下使“浸水程度”参数为负数，即使材质仍没来得及切换，显示上是正确的。

