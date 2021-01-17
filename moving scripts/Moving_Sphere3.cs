using UnityEngine;

public class Moving_Sphere3 : MonoBehaviour
{

    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;

    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f, maxAirAcceleration = 1f;

    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 2f;

    [SerializeField, Range(0, 5)]
    int maxAirJumps = 0;//最多跳几次

    [SerializeField, Range(0, 5)]
    float maxGroundAngle = 25f;
    Vector3 contactNormal;//跳跃垂直与当前地面法线
    Vector3 velocity, desiedVelocity;
    Rigidbody body;
    bool desiredJump, onGround;
    int jumpPhase;//当前已跳跃次数
    float minGroundDotProduct;
    void Awake()
    {
        body = GetComponent<Rigidbody>();
        OnValidate();
    }
    void EvaluateCollision(Collision col)
    {
        for (int i = 0; i < col.contactCount; i++)
        {
            //遍历某次碰撞的所有接触点,有偶数个点
            //y值大于设定的角度->在地面（仅限此处球体）
            //即角度小于min的情况下，算做地面
            Vector3 normal = col.GetContact(i).normal;
            if(normal.y >= minGroundDotProduct)
            {
                onGround = true;
                contactNormal = normal;
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
        float acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;
        velocity.x = Mathf.MoveTowards(velocity.x, desiedVelocity.x, maxSpeedChange);
        velocity.z = Mathf.MoveTowards(velocity.z, desiedVelocity.z, maxSpeedChange);
        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }
        body.velocity = velocity;
        onGround = false;
    }
    void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle*Mathf.Deg2Rad);
    }
    void UpdateState()
    {
        velocity = body.velocity;
        if (onGround) jumpPhase = 0;//一旦落地，跳跃归零
        else
        {
            contactNormal = Vector3.up;

        }
    }
    void Jump()
    {
        if (onGround || jumpPhase < maxAirJumps)
        {
            jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            float alignedSpeed = Vector3.Dot(velocity, contactNormal);
            // 这个情况只会出现在多段跳里,jumpspeed将要添加的速度
            //要控制多段跳的速度不能大于单次跳的速度
            //此时判断velocity.y > 0f不再正确,要判断地面法线方向上的速度
            if(alignedSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }
            velocity+= contactNormal *jumpSpeed;
        }

    }
    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        //输入向量，返回此向量在接触面的投影
        return vector = contactNormal * Vector3.Dot(vector, contactNormal);
    }
    void AdjustVelocity()
    {//相对接触面进行速度修改
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;
        velocity.x = Mathf.MoveTowards(velocity.x, desiedVelocity.x, maxSpeedChange);
        velocity.z = Mathf.MoveTowards(velocity.z, desiedVelocity.z, maxSpeedChange);
            
    }
}


