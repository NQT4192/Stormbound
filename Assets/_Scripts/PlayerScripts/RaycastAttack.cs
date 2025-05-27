using UnityEngine;

public class RaycastAttack : MonoBehaviour
{
    [Header("Cấu hình Raycast")]
    public Transform rayOrigin;       // Vị trí bắt đầu ray (tay hoặc camera)
    public float maxDistance = 100f;  // Độ dài tối đa của ray
    public int damage = 10;           // Sát thương gây ra

    private RaycastHit hitInfo;       // Lưu thông tin va chạm

    void Update()
    {
        // 1. Luôn vẽ tia ray đỏ trong Scene view
        Vector3 direction = rayOrigin.forward;

        bool isHit = Physics.Raycast(rayOrigin.position, direction, out hitInfo, maxDistance);

        if (isHit)
        {
            // Nếu ray trúng vật, vẽ từ rayOrigin đến điểm trúng
            Debug.DrawLine(rayOrigin.position, hitInfo.point, Color.red);
        }
        else
        {
            // Nếu không trúng gì, vẽ tia dài tối đa
            Debug.DrawLine(rayOrigin.position, rayOrigin.position + direction * maxDistance, Color.red);
        }

        // 2. Gây sát thương khi bấm chuột trái và ray đang trúng vật
        if (Input.GetMouseButtonDown(0) && isHit)
        {
            Health health = hitInfo.collider.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
                Debug.Log("Gây sát thương: " + damage + " vào " + hitInfo.collider.name);
            }
            else
            {
                Debug.Log("Vật trúng không có component Health.");
            }
        }
    }
}