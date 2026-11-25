using UnityEngine;

// 醒狮转身：根据角色朝向显示正面/侧面/背面贴图（类似蛋仔派对的转身效果）
// SpriteRenderer 贴图法线朝 -Z，所以面片朝向相机时需翻转 180 度
public class BillboardSprite : MonoBehaviour
{
    public Sprite spriteFront;
    public Sprite spriteSide;
    public Sprite spriteBack;

    SpriteRenderer sr;
    Transform body; // 父物体 = 角色，用它的 forward 判断朝向

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        body = transform.parent;
    }

    void LateUpdate()
    {
        if (Camera.main == null || body == null || sr == null) return;

        // 角色朝向（水平投影）
        Vector3 facing = body.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.001f) return;
        facing.Normalize();

        // 相机到角色的水平方向
        Vector3 toCam = Camera.main.transform.position - transform.position;
        toCam.y = 0f;
        if (toCam.sqrMagnitude < 0.001f) return;
        toCam.Normalize();

        // dot: facing 与「指向相机」的夹角。>0.35 面朝相机(正面)，<-0.35 背对(背面)，否则侧面
        float dot = Vector3.Dot(facing, toCam);
        Sprite target = dot > 0.35f ? spriteFront : (dot < -0.35f ? spriteBack : spriteSide);
        if (target != null) sr.sprite = target;

        // SpriteRenderer 正面朝 -Z：先让 +Z 指向相机，再转 180 度让 -Z 朝相机
        transform.rotation = Quaternion.LookRotation(toCam, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
    }
}
