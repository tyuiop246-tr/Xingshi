using UnityEngine;

// 3D 平台跳跃角色控制：地面检测 + 跳跃缓冲 + 土狼时间 + 空中微调
// 设计意图：白盒阶段先把手感做扎实，落点精准度才有意义
[RequireComponent(typeof(CharacterController))]
public class PlayerController3D : MonoBehaviour
{
    public static PlayerController3D Instance { get; private set; }

    [Header("移动")]
    public float moveSpeed = 7f;
    public float airControl = 0.45f;
    public float rotateSpeed = 12f;

    [Header("跳跃")]
    public float jumpHeight = 2.2f;
    public float gravity = -24f;
    public float coyoteTime = 0.12f;
    public float jumpBuffer = 0.12f;

    public bool HasQing { get; private set; }
    public int DeathCount { get; private set; }
    public float Progress { get; private set; }

    CharacterController cc;
    Vector3 velocity;
    float lastGroundedTime;
    float lastJumpPressed;
    Vector3 spawnPoint;
    Vector3 respawnPoint;      // 当前复活点（未激活时 = 起点）
    int activatedCheckpoint = -1;
    float highestZ;

    void Awake()
    {
        Instance = this;
        cc = GetComponent<CharacterController>();
        // 圆柱顶面边缘有弧度，放宽坡度限制 + 缩小 stepOffset，避免被斜面弹开/滑落
        cc.slopeLimit = 70f;
        cc.stepOffset = 0.25f;
    }

    void Start()
    {
        spawnPoint = transform.position;
        respawnPoint = spawnPoint;
        highestZ = transform.position.z;
    }

    void Update()
    {
        if (TitleScreen.Paused || (TitleScreen.Instance != null && !TitleScreen.GameStarted))
        {
            velocity = Vector3.zero;
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");   // A/D 或 ←/→ : 左右
        float v = Input.GetAxisRaw("Vertical");     // W/S 或 ↑/↓ : 前后
        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        bool grounded = cc.isGrounded;
        if (grounded)
        {
            lastGroundedTime = Time.time;
            if (velocity.y < 0f) velocity.y = -3f;  // 更强的贴地力，站在窄桩上不飘
        }

        if (Input.GetButtonDown("Jump")) lastJumpPressed = Time.time;

        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        bool buffered = Time.time - lastJumpPressed <= jumpBuffer;
        if (canJump && buffered)
        {
            velocity.y = Mathf.Sqrt(2f * -gravity * jumpHeight);
            lastJumpPressed = -999f;
            lastGroundedTime = -999f;
        }

        // 地面：直接设速 / 松手立刻归零，消除圆柱顶面的打滑惯性
        // 空中：保留插值，维持跳跃的惯性感
        if (grounded)
        {
            Vector3 desired = input * moveSpeed;
            velocity.x = desired.x;
            velocity.z = desired.z;
        }
        else
        {
            Vector3 desired = input * moveSpeed;
            velocity.x = Mathf.Lerp(velocity.x, desired.x, airControl * Time.deltaTime * 12f);
            velocity.z = Mathf.Lerp(velocity.z, desired.z, airControl * Time.deltaTime * 12f);
        }
        velocity.y += gravity * Time.deltaTime;

        cc.Move(velocity * Time.deltaTime);

        if (input.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(input.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotateSpeed * Time.deltaTime);
        }

        if (transform.position.z > highestZ) highestZ = transform.position.z;
        Progress = highestZ;

        if (transform.position.y < -8f) Respawn();
    }

    // 由复活点调用，记录重生位置（只接受更靠后的复活点）
    public void SetCheckpoint(Vector3 position, int index)
    {
        if (index <= activatedCheckpoint) return;
        activatedCheckpoint = index;
        respawnPoint = position;
    }

    public void Respawn()
    {
        cc.enabled = false;
        transform.position = respawnPoint;
        cc.enabled = true;
        velocity = Vector3.zero;
        DeathCount++;
        string msg = activatedCheckpoint >= 0 ? "跌落！从复活点重新出发" : "跌落！回到起点，再试一次";
        if (HUDController.Instance != null) HUDController.Instance.ShowTip(msg);
    }

    public void CollectQing()
    {
        HasQing = true;
    }

    public void ResetCharacter()
    {
        HasQing = false;
        DeathCount = 0;
        Progress = 0f;
        highestZ = spawnPoint.z;
        activatedCheckpoint = -1;
        respawnPoint = spawnPoint;
        Checkpoint.ResetAll();
        Respawn();
        DeathCount = 0;
    }
}
