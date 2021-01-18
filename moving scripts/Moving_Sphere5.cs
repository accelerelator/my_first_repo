using UnityEngine;
//
/// <summary>
/// 
/// </summary>
public class Moving_Sphere5 : MonoBehaviour
{

    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;

    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f, maxAirAcceleration = 1f;

    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 2f;

    /// <summary>
    /// //最多跳几次
    /// </summary>
    [SerializeField, Range(0, 5)]
    int maxAirJumps = 0;

    [SerializeField, Range(0f, 100f)]
    float maxSnapSpeed = 100f;//防止物体在台阶处跳起的最大速度

    [SerializeField, Range(0, 90)]
    float maxGroundAngle = 25f,maxStairsAngle = 50f;

    /// <summary>
    /// stairsMask是掩码，某一层选中则对应位数为1
    /// </summary>
    [SerializeField]
    LayerMask probeMask = -1, stairsMask = -1;

    /// <summary>
    /// 起跳检测物体正下方时，只检测特定距离的平面
    /// </summary>
    [SerializeField, Min(0f)]
    float probeDistance = 1f;

    Vector3 contactNormal;//跳跃垂直与当前地面法线
    Vector3 velocity, desiedVelocity;
    Rigidbody body;
    bool desiredJump;

    /// <summary>
    /// steep类参数用于控制峭壁类地形的判定
    /// </summary>
    Vector3 steepNormal;
    /// <summary>
    /// 有几个地面类的接触点
    /// </summary>
    int groundContactCount;

    /// <summary>
    /// 有几个陡峭的接触点
    /// </summary>
    int steepContactCount;
    bool onGround => groundContactCount > 0;
    bool onSteep => steepContactCount > 0;

    int jumpPhase;//当前已跳跃次数
    float minGroundDotProduct, minStairsDotProduct;

   

    /// <summary>
    /// 上次接地/跳跃以来经过了多少物理步长（每次fixedupdate+1）
    /// </summary>
    int stepsSinceLastGrounded, stepsSinceLastJump;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        OnValidate();
    }
    void EvaluateCollision(Collision col)
    {
        float minDot = GetMinDot(col.gameObject.layer);
        for (int i = 0; i < col.contactCount; i++)
        {
            //遍历某次碰撞的所有接触点,有偶数个点
            //即角度小于minDot的情况下，算做地面
            Vector3 normal = col.GetContact(i).normal;
            if (normal.y >= minDot)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
            //不是地面则判断是否为峭壁
            else if (normal.y > -0.01f)
            {
                steepContactCount += 1;
                steepNormal += normal;
            }
        }
    }
    void OnCollisionEnter(Collision col)
    {
        EvaluateCollision(col);
    }
    void OnCollisionStay(Collision col)//即不接触任何物体判断为不在地
    {
        EvaluateCollision(col);
    }
    void Update()
    {
        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);
        desiedVelocity = new Vector3(
            playerInput.x, 0f, playerInput.y) * maxSpeed;//期望速度
        desiredJump |= Input.GetButtonDown("Jump");
    }
    void FixedUpdate()
    {
        UpdateState();
        AdjustVelocity();
        
        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }
        body.velocity = velocity;
        ClearState();
    }
    void ClearState()
    {
        groundContactCount = steepContactCount = 0;
        contactNormal = steepNormal = Vector3.zero;
    }
    void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
    }
    void UpdateState()
    {
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump += 1;
        velocity = body.velocity;
        //先判断在不在地面
        //不在则尝试让物体贴近地面
        //如果是卡在缝隙里则进入第三种操作，尝试把接触点合并以使跳跃操作有效
        if (onGround || SnapToGround() || CheckSteepContacts())
        {
            stepsSinceLastGrounded = 0;
            //这种情况用于踩墙跳
            //接触墙也算接地，因此下一时刻把当前起跳次数置0
            if (stepsSinceLastJump > 1)
            {
                jumpPhase = 0;
            }
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
            
        }
        else
        {
            contactNormal = Vector3.up;

        }
    }
    /// <summary>
    /// 只有物体不在地面onGround==false时调用
    /// 尝试把物体靠向地面
    /// 见UpdateState
    /// </summary>
    /// <returns></returns>
    bool SnapToGround()
    {
        //只在离开地面情况下调用一次
        //每次空格后2步长再snap地面，即起跳空格后2步长内跳跃的向上速度不会被影响
        if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2)
        {
            return false;
        }
        RaycastHit hit;
        float speed = velocity.magnitude;
        if (speed > maxSnapSpeed)
        {
            return false;
        }
        if (!Physics.Raycast(
            body.position, Vector3.down, out hit,
            probeDistance, probeMask
        ))
        {
            return false;
        }
        //hit.normal.y 是射线碰撞到的物体(这里就是地面)的法线
        //如果角度小于阈值，则判断在地面上
        if (hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
        {
            return false;
        }
        //此时我们确定物体在地面上空且未接触地面

        groundContactCount = 1;
        contactNormal = hit.normal;
        float dot = Vector3.Dot(velocity, hit.normal);
        //hit.normal * dot 和velocity在地面法线方向上数值一样
        //这个操作能消除y方向上的速度，即“让物体速度平行于地面”
        //这样就实现了物体经过台阶时不会跳起
        if (dot > 0f)
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }
        return false;
    }
    void Jump()
    {
        //引入jumpDirection规定不同情况下应该有的起跳方向
        Vector3 jumpDirection;
        if (onGround)
        {
            jumpDirection = contactNormal;
        }
        else if (onSteep)//接触墙面或近似垂直面时，向这个面的法向起跳
        {
            jumpDirection = steepNormal;
            jumpPhase = 0;
        }
        //这段没看懂
        else if (maxAirJumps > 0 && jumpPhase <= maxAirJumps)
        {
            if (jumpPhase == 0)
            {
                jumpPhase = 1;
            }
            //jumpPhase = 1;
            jumpDirection = contactNormal;
        }
        else
        {
            return;
        }
        
        stepsSinceLastJump =0;
        jumpPhase += 1;
        float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
        //关键：加入一点正向上的分量确保物体是向上跳的
        jumpDirection = (jumpDirection + Vector3.up).normalized;
        float alignedSpeed = Vector3.Dot(velocity, contactNormal);
        // 这个情况只会出现在多段跳里,jumpspeed将要添加的速度
        //要控制多段跳的速度不能大于单次跳的速度
        //此时判断velocity.y > 0f不再正确,要判断地面法线方向上的速度
        if (alignedSpeed > 0f)
        {
            jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
        }
        //原本是垂直地面起跳velocity += contactNormal * jumpSpeed;
        velocity += jumpDirection * jumpSpeed;

    }
    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        //输入向量，返回此向量在接触面的投影
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }

    /// <summary>
    /// 确保速度是平行于接触面的
    /// </summary>
    void AdjustVelocity()
    {
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        //把当前速度投影到平行于接触面，垂直于接触面两个方向
        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        float newX =
            Mathf.MoveTowards(currentX, desiedVelocity.x, maxSpeedChange);
        float newZ =
            Mathf.MoveTowards(currentZ, desiedVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }
    /// <summary>
    /// 返回特定层物体的角度阈值
    /// 有地面和台阶两种物体，返回的值是判断此类物体的最小角度
    /// </summary>
    /// <param name="layer"></param>
    /// <returns></returns>
    float GetMinDot(int layer)
    {
        return (stairsMask & (1 << layer)) == 0 ?
            minGroundDotProduct : minStairsDotProduct;
    }
    bool CheckSteepContacts()
    {//如果陡峭的接触点大于一个，则尝试吧他们视作一个接触点来处理
        if (steepContactCount > 1)
        {
            steepNormal.Normalize();
            if (steepNormal.y >= minGroundDotProduct)//如果合并后不再是陡峭的接触点
            {
                steepContactCount = 0;
                groundContactCount = 1;
                contactNormal = steepNormal;
                return true;
            }
        }
        return false;
    }
}


