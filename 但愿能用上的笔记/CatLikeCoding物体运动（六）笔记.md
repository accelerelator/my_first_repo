# CatLikeCoding物体运动（六）笔记

##1.管理重力源

每个重力源都有GravitySource，CustomGravity用一个List<GravitySource>管理所有重力源

GravitySource用以下代码来添加到list中

```
void OnEnable () {
		CustomGravity.Register(this);
	}

	void OnDisable () {
		CustomGravity.Unregister(this);
	}
```

CustomGravity用Debug.Assert尝试添加新重力源，Debug.Assert不通过则不进行后续代码

```
public static void Register (GravitySource source) {
		Debug.Assert(
			!sources.Contains(source),
			"Duplicate registration of gravity source!", source
		);
		sources.Add(source);
	}

	public static void Unregister (GravitySource source) {
		Debug.Assert(
			sources.Contains(source),
			"Unregistration of unknown gravity source!", source
		);
		sources.Remove(source);
	}
```

GravityPlane指的是平面类型的重力场，继承至GravitySource

```
public class GravityPlane : GravitySource 
```

重载了基本函数，这要求父函数是virtual。重力的数值跟距离有关，大于range则不施加重力，小于range则距离乘以重力。

为什么距离用dot？因为不止看垂直与平面的距离，也要看水平方向的距离。

```
public override Vector3 GetGravity (Vector3 position)
{
Vector3 up = transform.up;
		float distance = Vector3.Dot(up, position - transform.position);
		if (distance > range) {
			return Vector3.zero;
		}
		float g = -gravity;
		if (distance > 0f) {
			g *= 1f - distance / range;
		}
		return g * up;
}
```

为此重力场添加矩阵，确保图线是在模型空间画的

矩阵的构造原理是用我们给定的重力范围作为图线的y分量大小，其他属性按既有的来

```
void OnDrawGizmos () {
		Vector3 scale = transform.localScale;
		scale.y = range;
		Gizmos.matrix =
			Matrix4x4.TRS(transform.position, transform.rotation, scale);
		Vector3 size = new Vector3(1f, 0f, 1f);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(Vector3.zero, size);
		if (range > 0f) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireCube(Vector3.up, size);
		}
	}
```

实现重力场的显示

到此为止，CustomGravity->GravitySource->

##2.添加重力场

重力球原理

![图像_2021-01-20_202424.png](https://i.loli.net/2021/01/20/ZKiPUAVuHWk8DBF.png)

实际代码

```

	public override Vector3 GetGravity (Vector3 position) {
		Vector3 vector = transform.position - position;
		float distance = vector.magnitude;
		if (distance > outerFalloffRadius || distance < innerFalloffRadius) {
			return Vector3.zero;
		}
		float g = gravity / distance;
		if (distance > outerRadius) {
			g *= 1f - (distance - outerRadius) * outerFalloffFactor;
		}
		else if (distance < innerRadius) {
			g *= 1f - (innerRadius - distance) * innerFalloffFactor;
		}
		return g * vector;
	}

	void Awake () {
		OnValidate();
	}

	void OnValidate () {
		innerFalloffRadius = Mathf.Max(innerFalloffRadius, 0f);
		innerRadius = Mathf.Max(innerRadius, innerFalloffRadius);
		outerRadius = Mathf.Max(outerRadius, innerRadius);
		outerFalloffRadius = Mathf.Max(outerFalloffRadius, outerRadius);

		innerFalloffFactor = 1f / (innerRadius - innerFalloffRadius);
		outerFalloffFactor = 1f / (outerFalloffRadius - outerRadius);
	}

	void OnDrawGizmos () {
		Vector3 p = transform.position;
		if (innerFalloffRadius > 0f && innerFalloffRadius < innerRadius) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(p, innerFalloffRadius);
		}
		Gizmos.color = Color.yellow;
		if (innerRadius > 0f && innerRadius < outerRadius) {
			Gizmos.DrawWireSphere(p, innerRadius);
		}
		Gizmos.DrawWireSphere(p, outerRadius);
		if (outerFalloffRadius > outerRadius) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(p, outerFalloffRadius);
		}
	}
```



### 2.1重力盒

区间内无重力

![图像_2021-01-20_202847.png](https://i.loli.net/2021/01/20/ZHuWfbQFviCRSNX.png)

对力场内任一点，考虑两个参数：离中心距离，离最近一面的距离在某一坐标轴上的投影。注意离中心距离带正负，表示当前点在中心的左右

只考虑最近面的重力，距离由盒子大小-当前坐标得到，比较xyz可得最近面。

> 为了支持任意旋转的立方体，我们必须旋转相对位置以与立方体对齐。

```c#
position =
			transform.InverseTransformDirection(position - transform.position);

//得到指向某一轴的重力向量vector
return transform.TransformDirection(vector);
```

上面两行中间需要加入计算当前点和面之间的关系的代码

```
int outside = 0;
		if (position.x > boundaryDistance.x) {
			vector.x = boundaryDistance.x - position.x;
			outside = 1;
		}
		else if (position.x < -boundaryDistance.x) {
			vector.x = -boundaryDistance.x - position.x;
			outside = 1;
		}

		if (position.y > boundaryDistance.y) {
			vector.y = boundaryDistance.y - position.y;
			outside += 1;
		}
		else if (position.y < -boundaryDistance.y) {
			vector.y = -boundaryDistance.y - position.y;
			outside += 1;
		}

		if (position.z > boundaryDistance.z) {
			vector.z = boundaryDistance.z - position.z;
			outside += 1;
		}
		else if (position.z < -boundaryDistance.z) {
			vector.z = -boundaryDistance.z - position.z;
			outside += 1;
		}

```

outside是当前点在几个面外侧，只在一个面外侧则只有一个轴的重力分量非零

```

		if (outside > 0) {
			float distance = outside == 1 ?
				Mathf.Abs(vector.x + vector.y + vector.z) : vector.magnitude;
			if (distance > outerFalloffDistance) {
				return Vector3.zero;
			}
			float g = gravity / distance;
			if (distance > outerDistance) {
				g *= 1f - (distance - outerDistance) * outerFalloffFactor;
			}
			return transform.TransformDirection(g * vector);
		}
```

