using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orbit_Camera3 : MonoBehaviour
{

    [SerializeField]
    Transform focus = default;

    [SerializeField, Range(1f, 20f)]
    float distance = 5f;

    [SerializeField, Min(0f)]
    float focusRadius = 1f;

    [SerializeField, Range(0f, 1f)]
    float focusCentering = 0.5f;

    [SerializeField, Range(1f, 360f)]
    float rotationSpeed = 90f;

    [SerializeField, Range(-89f, 89f)]
    float minVerticalAngle = -45f, maxVerticalAngle = 45f;

    [SerializeField, Min(0f)]
    float alignDelay = 5f;

    [SerializeField, Range(0f, 90f)]
    float alignSmoothRange = 45f;

    [SerializeField]
    LayerMask obstructionMask = -1;

    Camera regularCamera;

    Vector3 focusPoint, previousFocusPoint;

    Vector2 orbitAngles = new Vector2(45f, 0f);

    float lastManualRotationTime;

    Vector3 CameraHalfExtends
    {
        get
        {
            Vector3 halfExtends;
            halfExtends.y =
                regularCamera.nearClipPlane *
                Mathf.Tan(0.5f * Mathf.Deg2Rad * regularCamera.fieldOfView);
            halfExtends.x = halfExtends.y * regularCamera.aspect;
            halfExtends.z = 0f;
            return halfExtends;
        }
    }
    void Awake()
    {
        regularCamera = GetComponent<Camera>();
        focusPoint = focus.position;
        transform.localRotation = Quaternion.Euler(orbitAngles);
    }
    void UpdateFoucsPoint()
    {
        //主要作用：求focusPonit，记录previousFocusPoint
        previousFocusPoint = focusPoint;
        Vector3 targetPoint = focus.position;
        if (focusRadius > 0f)
        {
            float distance2 = Vector3.Distance(targetPoint, focusPoint);
            float t = 1f;
            if (distance2 > 0.01f && focusCentering > 0f)
            {
                t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime);
            }
            if (distance2 > focusRadius)
            {
                t = Mathf.Min(t, focusRadius / distance2);
            }
            focusPoint = Vector3.Lerp(
                    targetPoint, focusPoint, t);
        }
        else focusPoint = targetPoint;
    }
    int cnt = 0;
    bool ManualRotation()
    {//返回值：根据鼠标坐标输入，是否需要更改相机转角
     /*Vector2 input = new Vector2(
         Input.GetAxis("Vertical Camera"),
         Input.GetAxis("Horizontal Camera"));*/
        Vector2 input = new Vector2(0,0);
        const float e = 0.001f;
        if (input.x < -e || input.x > e || input.y < -e || input.y > e)
        {
            //Debug.Log((cnt++)+" "+input.x);
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
            lastManualRotationTime = Time.unscaledTime;
            return true;
        }
        return false;
    }
    void OnValidate()
    {
        if (maxVerticalAngle < minVerticalAngle)
            maxVerticalAngle = minVerticalAngle;
    }
    bool AutomaticRotation()
    {
        //Debug.Log("Now Time : "+(lastManualRotationTime ));
        if (Time.unscaledTime - lastManualRotationTime < alignDelay)
        {
            return false;
        }
        Vector2 movement = new Vector2(
            focusPoint.x - previousFocusPoint.x,
            focusPoint.z - previousFocusPoint.z);
        float movementDeltaSqr = movement.sqrMagnitude;
        if (movementDeltaSqr < 0.0001f)
        {
            return false;//如果当前角度和上次处理时没变化，那就不需要改变转角
        }
        //否则把orbitAngles改成 由相差角度 得到的 向量headingAngle
        float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
        float rotationChange = rotationSpeed * Mathf.Min(
            movementDeltaSqr, Time.unscaledDeltaTime);
        //平缓变换，当前需要转多少角度?
        //变换速度不应保持线性，加入随剩余角度变化的速度
        //当剩余角小于alignSmoothRange时，乘上对应比例
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));
        if (deltaAbs < alignSmoothRange)
            rotationChange *= deltaAbs / alignSmoothRange;
        else if (180f - deltaAbs < alignSmoothRange)
            rotationChange *= (180f - deltaAbs) / alignSmoothRange;
        orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);
        return true;
    }
    void ConstrainAngles()
    {
        orbitAngles.x =
            Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);
        if (orbitAngles.y < 0f) orbitAngles.y += 360f;
        else if (orbitAngles.y >= 360f) orbitAngles.y -= 360f;
    }
    void LateUpdate()
    {

        UpdateFoucsPoint();
        Quaternion lookRotation = transform.localRotation;
        if (ManualRotation() || AutomaticRotation())
        {
            ConstrainAngles();
            lookRotation = Quaternion.Euler(orbitAngles);
            //相机需要转变角度，则全局orbitAngles 控制当前转角
        }
        Vector3 lookDirection = lookRotation * Vector3.forward;
        Vector3 lookPosition = focusPoint - lookDirection * distance;

        //摄像机遮挡检测，相机 -> 物体被挡住后，相机位置改为 阻挡点->物体
        RaycastHit hit;
        /*if (Physics.Raycast(
            focusPoint ,- lookDirection , out hit , distance))
        {
            lookPosition = focusPoint - lookDirection * hit.distance;
        }*/
        if (Physics.BoxCast(focusPoint, CameraHalfExtends
            ,-lookDirection, out hit,lookRotation, distance))
        {
            lookPosition = focusPoint - lookDirection * (hit.distance +regularCamera.nearClipPlane);
        }
        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }
    static float GetAngle(Vector2 direction)
    {
        float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
        return direction.x < 0f ? 360f - angle : angle;
    }

}
