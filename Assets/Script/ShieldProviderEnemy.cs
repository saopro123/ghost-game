using UnityEngine;

public class ShieldProviderEnemy : Enemy
{
    public float stopXPosition = 6f;
    private bool hasReachedPos = false;

    protected override void Start()
    {
        // Khởi tạo các thành phần lớp cha thủ công vì mini-boss có máu riêng
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = 30; // Máu thấp theo ý bạn

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTarget = playerObj.transform;

        levelManager = LevelManager.Instance;

        Enemy.activeShielders++;
    }

    protected override void Move()
    {
        if (!hasReachedPos)
        {
            transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);
            if (transform.position.x <= stopXPosition) hasReachedPos = true;
        }
    }

    protected override void Die()
    {
        if (isDying) return;
        // isDying = true; // Sẽ được set trong base.Die()

        Enemy.activeShielders = Mathf.Max(0, Enemy.activeShielders - 1);

        if (BlessingMenu.Instance != null)
        {
            BlessingMenu.Instance.ShowBlessingSelection();
        }

        base.Die();
    }
}