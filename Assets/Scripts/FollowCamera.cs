using UnityEngine;

// 第三人称跟随相机：平滑跟随 + 固定偏移，白盒阶段保证视野稳定
public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 5f, -9f);
    public float smooth = 6f;
    public float lookAhead = 2f;

    void LateUpdate()
    {
        if (target == null && PlayerController3D.Instance != null)
            target = PlayerController3D.Instance.transform;
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smooth * Time.deltaTime);
        Vector3 lookAt = target.position + Vector3.forward * lookAhead + Vector3.up * 1f;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(lookAt - transform.position), smooth * Time.deltaTime);
    }
}
