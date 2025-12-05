using UnityEngine;

// Lớp CatEnemy kế thừa từ lớp Enemy
public class CatEnemy : Enemy
{
    [Header("Cài Đặt Bắn Đạn Của Mèo")]
    public GameObject enemyBulletPrefab; // Prefab đạn của kẻ địch (EnemyBullet)
    public Transform firePoint;          // Điểm xuất phát đạn trên Cat Enemy
    public float fireRate = 1.5f;        // Tần suất bắn (1.5s/viên)
    private float nextFireTime;          // Thời điểm có thể bắn tiếp theo

    [Header("Cài Đặt Di Chuyển Mèo")]
    [Tooltip("Tốc độ bay cố định của Mèo (Nếu lớp cha không có moveSpeed).")]
    public float catMoveSpeed = 3f; // Tốc độ bay thẳng

    // GHI ĐÈ hàm Start() của lớp cha (Enemy)
    protected override void Start()
    {
        // QUAN TRỌNG: Gọi hàm Start() của lớp cha để khởi tạo máu, Rigidbody và tìm Player.
        base.Start();

        // Thiết lập thời gian bắn ban đầu
        nextFireTime = Time.time + Random.Range(0.5f, fireRate);
    }

    // GHI ĐÈ hàm FixedUpdate() của lớp cha (Enemy)
    protected override void FixedUpdate()
    {
        // QUAN TRỌNG: Gọi hàm FixedUpdate() của lớp cha.
        // Logic di chuyển đã được xử lý trong hàm Move() bị override bên dưới.
        base.FixedUpdate();

        // Thêm logic bắn đạn
        HandleShooting();
    }

    // GHI ĐÈ hàm Move() để NGĂN CHẶN Cat Enemy đuổi theo Player
    protected override void Move()
    {
        // Thiết lập vận tốc cố định để bay thẳng sang trái
        if (rb != null)
        {
            // Sử dụng Vector2.left (ngang sang trái) và tốc độ riêng của Mèo
            rb.linearVelocity = Vector2.left * catMoveSpeed;
        }
    }

    private void HandleShooting()
    {
        if (Time.time > nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    private void Shoot()
    {
        if (enemyBulletPrefab != null && firePoint != null)
        {
            // Đạn sẽ bay theo logic được thiết lập trong EnemyBullet.cs
            Instantiate(enemyBulletPrefab, firePoint.position, Quaternion.identity);
        }
    }
}