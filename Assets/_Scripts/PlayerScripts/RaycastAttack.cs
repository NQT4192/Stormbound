using UnityEngine;

public class RaycastAttack : MonoBehaviour
{
    [Header("Cấu hình Raycast")]
    public Transform rayOrigin;
    public float maxDistance = 100f;
    public int damage = 10;

    public float bulletSpeed = 200f;      
    public float bulletLength = 2f;       
    public Material bulletMaterial;

    private RaycastHit hitInfo;
    private Vector3 targetPoint;
    private bool shooting = false;
    private float traveledDistance = 0f;

    private LineRenderer bulletLine;

    void Update()
    {
        Vector3 direction = rayOrigin.forward;

        // Nếu chưa bắn thì tìm điểm chạm mới
        if (!shooting)
        {
            bool isHit = Physics.Raycast(rayOrigin.position, direction, out hitInfo, maxDistance);
            targetPoint = isHit ? hitInfo.point : rayOrigin.position + direction * maxDistance;
        }

        if (Input.GetMouseButtonDown(0) && !shooting)
        {
            shooting = true;
            traveledDistance = 0f;

            // Tạo LineRenderer mới cho viên đạn
            if (bulletLine == null)
            {
                GameObject lineObj = new GameObject("BulletLine");
                bulletLine = lineObj.AddComponent<LineRenderer>();
                bulletLine.positionCount = 2;
                bulletLine.material = bulletMaterial != null ? bulletMaterial : new Material(Shader.Find("Sprites/Default"));
                bulletLine.startColor = Color.yellow;
                bulletLine.endColor = Color.yellow;
                bulletLine.startWidth = 0.02f;
                bulletLine.endWidth = 0.02f;
            }
        }

        if (shooting)
        {
            if (bulletLine == null)
            {
               
                shooting = false;
                return;
            }

            traveledDistance += bulletSpeed * Time.deltaTime;
            float totalDistance = Vector3.Distance(rayOrigin.position, targetPoint);

            if (traveledDistance > totalDistance)
            {
                traveledDistance = totalDistance;
            }

            Vector3 startPos = rayOrigin.position + rayOrigin.forward * Mathf.Max(traveledDistance - bulletLength, 0);
            Vector3 endPos = rayOrigin.position + rayOrigin.forward * traveledDistance;

            bulletLine.SetPosition(0, startPos);
            bulletLine.SetPosition(1, endPos);

            if (traveledDistance >= totalDistance)
            {
                shooting = false;

                if (bulletLine != null)
                {
                    Destroy(bulletLine.gameObject, 0.05f);
                    bulletLine = null; 
                }

                // Gây sát thương nếu trúng vật có Health
                if (hitInfo.collider != null)
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

                    ShowImpact(hitInfo.point, hitInfo.normal);
                }
            }
        }
    }

    private void ShowImpact(Vector3 point, Vector3 normal)
    {
        Debug.DrawRay(point, normal * 0.3f, Color.white, 0.2f);
    }
}
