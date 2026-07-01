using UnityEngine;

public class SkeletonBone : MonoBehaviour
{
    [Header("Bone Settings")]
    public float speed = 5f;             // Tốc độ bay của xương
    public float rotationSpeed = 360f;   // Tốc độ xoay tròn của xương (độ/giây)
    public float damage = 1f;            // Sát thương gây ra
    public float lifeTime = 3f;          // Thời gian tự hủy nếu không chạm gì

    [Header("Layers")]
    public LayerMask obstacleLayer;      // Lớp mặt đất/tường để tự hủy khi chạm vào

    private Vector2 flyDirection;

    void Start()
    {
        // Tự động hủy sau một khoảng thời gian đề phòng bay ra ngoài bản đồ
        Destroy(gameObject, lifeTime);
    }

    // Thiết lập hướng bay cho xương (từ SkeletonAI gọi qua)
    public void SetDirection(Vector2 direction)
    {
        flyDirection = direction.normalized;
    }

    void Update()
    {
        // Di chuyển xương bay đi
        transform.Translate(flyDirection * speed * Time.deltaTime, Space.World);

        // Xoay tròn xương tạo hiệu ứng ném đẹp mắt
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Chạm vào Player -> Gây sát thương và hủy xương
        if (other.CompareTag("Player"))
        {
            HealthManager playerHealth = other.GetComponent<HealthManager>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            Destroy(gameObject);
            return;
        }

        // Chạm vào Tường/Đất -> Hủy xương
        // Kiểm tra xem layer của va chạm có nằm trong obstacleLayer không
        if (((1 << other.gameObject.layer) & obstacleLayer) != 0)
        {
            Destroy(gameObject);
        }
    }
}
