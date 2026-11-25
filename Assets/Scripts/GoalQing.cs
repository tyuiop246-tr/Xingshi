using UnityEngine;

// 终点「青」：玩家触碰后完成关卡，触发正反馈
public class GoalQing : MonoBehaviour
{
    public Transform qingTransform;
    public float spinSpeed = 60f;
    public float bobAmplitude = 0.25f;
    public float bobSpeed = 2f;

    Vector3 basePos;
    bool collected;

    void Start()
    {
        if (qingTransform == null) qingTransform = transform;
        basePos = qingTransform.position;
    }

    void Update()
    {
        if (collected) return;
        qingTransform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        qingTransform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmplitude);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        PlayerController3D player = other.GetComponent<PlayerController3D>();
        if (player == null) player = other.GetComponentInParent<PlayerController3D>();
        if (player == null) return;

        collected = true;
        player.CollectQing();
        if (HUDController.Instance != null) HUDController.Instance.ShowWin(player.Progress, player.DeathCount);
        Debug.Log("[醒狮] 采青成功！距离=" + player.Progress + " 米，死亡=" + player.DeathCount);
    }

    public static void ResetAll()
    {
        foreach (GoalQing goal in FindObjectsOfType<GoalQing>())
        {
            goal.collected = false;
            if (goal.qingTransform != null) goal.qingTransform.position = goal.basePos;
        }
    }
}
