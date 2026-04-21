using UnityEngine;

public class GoldenCoinEnemy : Enemy
{
    [Header("== Golden Coin Settings ==")]
    [Tooltip("Số vàng cộng trực tiếp khi bắn hạ đồng xu.")]
    public int goldReward = 20;

    protected override void Start()
    {
        // Gán thông số riêng cho Đồng xu
        isBoss = false;
        contactDamage = 100; // Sát thương va chạm cực cao như đã bàn

        base.Start();

        // Bạn nên set maxHealth của nó cao trong Inspector (ví dụ: 50) 
        // để người chơi phải tập trung bắn mới phá được.
    }

    // Ghi đè để nó chỉ bay thẳng sang trái, không đuổi theo Player
    protected override void Move()
    {
        if (rb != null)
        {
            // Bay thẳng sang trái với tốc độ moveSpeed
            // Có nhân với globalSpeedMultiplier để khớp với sự kiện Xanh Lá nếu cần
            rb.linearVelocity = Vector2.left * moveSpeed * Enemy.globalSpeedMultiplier;
        }
    }

    // Ghi đè hàm Die để cộng đúng 20 vàng
    protected override void Die()
    {
        // 1. Cộng vàng trực tiếp cho Player
        if (playerTarget != null)
        {
            Player p = playerTarget.GetComponent<Player>();
            if (p != null)
            {
                p.AddGold(goldReward);
                Debug.Log("Golden Coin Destroyed! +20 Gold");
            }
        }

        // 2. Nếu có Blessing Nổ (ID 6), nó vẫn sẽ nổ nhờ logic trong base.Die()
        // Gọi Die của lớp cha để xử lý hiệu ứng mờ dần và hủy Object
        base.Die();
    }
    // Trong GoldenCoinEnemy.cs

    void Update()
    {
        // Kiểm tra nếu đồng xu bay thoát khỏi màn hình bên trái (thường là X < -12)
        if (transform.position.x < -12f)
        {
            // Chỉ cộng 1 vàng vì người chơi không bắn hạ được
            if (Player.Instance != null)
            {
                Player.Instance.AddGold(1);
                Debug.Log("Coin escaped. +1 Gold only.");
            }

            // Hủy object để tránh tốn bộ nhớ
            Destroy(gameObject);
        }
    }
    // Trong GoldenCoinEnemy.cs

    public override void TakeDamage(int damageAmount, bool isFromExplosion = false)
    {
        // Truyền đủ 2 tham số vào base
        base.TakeDamage(damageAmount, isFromExplosion);

        if (currentHealth > 0 && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCoinHitSFX();
        }
    }
}