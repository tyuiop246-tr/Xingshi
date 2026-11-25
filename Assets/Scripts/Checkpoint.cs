using UnityEngine;

// 复活点：玩家走过激活（浅蓝光圈变亮+光柱），死亡后从此重生
public class Checkpoint : MonoBehaviour
{
    public int index;
    public bool Activated { get; private set; }

    public GameObject ring;   // 光圈
    public GameObject beam;   // 光柱
    public Material inactiveMat;
    public Material activeMat;

    float pulse;

    // 重开时由 PlayerController 调用，恢复所有复活点到未激活状态
    public static void ResetAll()
    {
        foreach (Checkpoint cp in FindObjectsOfType<Checkpoint>())
        {
            cp.Activated = false;
            if (cp.ring != null && cp.inactiveMat != null)
                cp.ring.GetComponent<Renderer>().sharedMaterial = cp.inactiveMat;
            if (cp.beam != null && cp.inactiveMat != null)
                cp.beam.GetComponent<Renderer>().sharedMaterial = cp.inactiveMat;
        }
    }

    void Update()
    {
        // 光柱缓慢脉动，未激活时也有微光，激活后更亮
        pulse += Time.deltaTime * (Activated ? 2f : 1f);
        if (beam != null)
        {
            float s = 1f + Mathf.Sin(pulse) * 0.06f;
            beam.transform.localScale = new Vector3(s, 1f, s);
        }
        if (ring != null)
            ring.transform.Rotate(Vector3.up, (Activated ? 60f : 20f) * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (Activated) return;
        PlayerController3D p = other.GetComponent<PlayerController3D>();
        if (p == null) p = other.GetComponentInParent<PlayerController3D>();
        if (p == null) return;

        Activated = true;
        if (ring != null && activeMat != null) ring.GetComponent<Renderer>().sharedMaterial = activeMat;
        if (beam != null && activeMat != null) beam.GetComponent<Renderer>().sharedMaterial = activeMat;
        p.SetCheckpoint(transform.position + Vector3.up * 1.2f, index);
        if (HUDController.Instance != null)
            HUDController.Instance.ShowTip("复活点已激活！下次死亡将在此重生");
        Debug.Log("[醒狮] 复活点 " + index + " 激活");
    }
}
