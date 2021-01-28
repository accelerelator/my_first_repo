这节主要讲了几种环境交互区域

# 1.加速

思路很简单，把物体velocity.y向着开放出来的speed用加速度插值

以下是注意的细节：

velocity.y按加速区域本身的坐标系，插值结束后转换回去

```
Vector3 velocity = transform.InverseTransformDirection(body.velocity);
...
body.velocity = transform.TransformDirection(velocity);
```

SnapToGround()函数固定在物体离开地面后调用一次，当向下固定长度的射线能接触到地面时，把y向速度消除。对应的计数器stepsSinceLastJump在<=2下不调用此函数，每次空格都把计数器归零，两步长后物体已经远离地面，故空格跳跃不被影响。

因此加速区域在插值结束后要把stepsSinceLastJump赋值-1，保证不被错误地影响。

# 2.检测

## 基本逻辑

检测物体是否进入并在显示材质上体现区别

对于这类区域，一个脚本DetectionZone用OnTriggerEnter等函数检测，并调用另一个脚本MaterialSelector里的函数以修改显示材质。

检测到物体时不直接调用函数，而是启用两个事件以方便调用多个物体的多个函数。

```
UnityEvent onFirstEnter = default, onLastExit = default;
void OnTriggerEnter (Collider other) {
		onFirstEnter.Invoke();
	}
void OnTriggerExit (Collider other) {
		onLastExit.Invoke();
	}
```

这是修改材质的代码，**函数绑定到事件后，参数需要自行指定**。

```
public void Select (int index) {
		if (
			meshRenderer && materials != null &&
			index >= 0 && index < materials.Length
		) {
			meshRenderer.material = materials[index];
		}
	}
```

## 特殊效果

接下来是一个比较实用的效果，即区域视做开关，有物体进入着启用事件->障碍物消失。需要玩家把物体推到区域中，自己离开。

思路是写个collider的list，有物体进入时就把collider加入list，有物体离开就把物体移出list。只在list为空时才启用进入和离开事件。以下代码注意list增删和事件启用的前后关系不同。

```
	List<Collider> colliders = new List<Collider>();
	void OnTriggerEnter (Collider other) {
		if (colliders.Count == 0) {
			onFirstEnter.Invoke();
			enabled = true;
		}
		colliders.Add(other);
	}

	void OnTriggerExit (Collider other) {
		if (colliders.Remove(other) && colliders.Count == 0) {
			onLastExit.Invoke();
			enabled = false;
		}
	}
```

## 进一步处理

游戏内可能会出现物体在检测区域当中销毁的情况，因此有必要持续检测list中的元素

```
void FixedUpdate () {
		for (int i = 0; i < colliders.Count; i++) {
			Collider collider = colliders[i];
			if (!collider || !collider.gameObject.activeInHierarchy) {
				colliders.RemoveAt(i--);
				if (colliders.Count == 0) {
					onLastExit.Invoke();
					enabled = false;
				}
			}
		}
	}

```

以及，物体enable==false时也是可以检测碰撞的，因此大多时候都可以把物体enable关掉，只在进入事件启用时开启

```
void Awake () {enabled = false;}
```

热重载（在编辑器播放模式下重新编译）将调用OnDisable，而OnDisable下为了保险也放入了离开事件的启用，为了不在热重载下错误地调用事件，加入以下代码提前结束函数。

```
void OnDisable () {
#if UNITY_EDITOR
		if (enabled && gameObject.activeInHierarchy) {
			return;
		}
#endif
		if (colliders.Count > 0) {
			colliders.Clear();
			onLastExit.Invoke();
		}
	}
```

# 3.滑动方块

这种交互只能实现简单线性移动的物体，复杂移动还得靠动画。

首先是检测，这次不再把事件对应的函数的参数开放出来，而是声明<float>类型的事件，把时间相关的参数直接写在invoke里

这是移动函数，也是物体进入事件对应的函数

relativeTo是局部插值的开关，true则from和to都填写局部坐标

```
public void Interpolate (float t) {
		Vector3 p;
		if (relativeTo) {
			p = Vector3.LerpUnclamped(
				relativeTo.TransformPoint(from), relativeTo.TransformPoint(to), t
			);
		}
		else {
			p = Vector3.LerpUnclamped(from, to, t);
		}
		body.MovePosition(p);
	}
```

对应事件的声明

```
[System.Serializable]
	public class OnValueChangedEvent : UnityEvent<float> { }

	[SerializeField]
	OnValueChangedEvent onValueChanged = default;
```

在invoke时指定参数	

```
onValueChanged.Invoke(smoothstep ? SmoothedValue : value);	
```

对于平台而言，每一帧都在调用事件（如果enable），时间影响的是事件的参数，进而影响插值移动的位置，实现平台的线性移动。

物体进入检测区->Reversed = 0，autoReverse = 1

物体进入检测区->Reversed = 1，autoReverse = 0，此时平台会停在原地，

```
void FixedUpdate () {
		float delta = Time.deltaTime / duration;
		if (Reversed) {
			value -= delta;
			if (value <= 0f) {
				if (autoReverse) {
					value = Mathf.Min(1f, -value);
					Reversed = false;
				}
				else {
					value = 0f;
					enabled = false;
				}
			}
		}
		else {
			value += delta;
			if (value >= 1f) {
				if (autoReverse) {
					value = Mathf.Max(0f, 2f - value);
					Reversed = true;
				}
				else {
					value = 1f;
					enabled = false;
				}
			}
		}
		onValueChanged.Invoke(smoothstep ? SmoothedValue : value);
	}
```

value就是当前帧线性插值的结果，smoothvalue则是平滑插值

```
float SmoothedValue => 3f * value * value - 2f * value * value * value;
```

