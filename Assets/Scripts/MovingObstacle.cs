using UnityEngine;

// 移动机关：Mathf.PingPong 实现匀速往返，无需动画师打关键帧
// 面试话术：用代码驱动机关，方便策划/程序协作，参数可在 Inspector 调
public class MovingObstacle : MonoBehaviour
{
    public enum MoveAxis { X, Y, Z }

    [Header("运动参数")]
    public MoveAxis axis = MoveAxis.X;
    public float amplitude = 3f;   // 相对初始点的半幅
    public float speed = 2f;       // 单位/秒
    public float phase = 0f;       // 相位偏移，让多个鼓错开节奏

    Vector3 origin;

    void Start()
    {
        origin = transform.position;
    }

    void Update()
    {
        // PingPong 输出 0..2*amplitude，减 amplitude 后得到 -amp..+amp
        float offset = Mathf.PingPong(Time.time * speed + phase, amplitude * 2f) - amplitude;
        Vector3 p = origin;
        switch (axis)
        {
            case MoveAxis.X: p.x += offset; break;
            case MoveAxis.Y: p.y += offset; break;
            case MoveAxis.Z: p.z += offset; break;
        }
        transform.position = p;
    }
}
