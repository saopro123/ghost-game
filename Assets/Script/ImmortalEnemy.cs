using UnityEngine;

public class ImmortalEnemy : Enemy
{
    // Ghi đè hàm Move để bỏ logic đuổi theo Player
    protected override void Move()
    {
        if (rb != null)
        {
            // Chỉ bay thẳng sang trái với tốc độ moveSpeed
            // Có nhân với globalSpeedMultiplier để đồng bộ với các sự kiện môi trường (như Xanh lá)
            rb.linearVelocity = Vector2.left * moveSpeed * Enemy.globalSpeedMultiplier;
        }
    }

    public override void TakeDamage(int damageAmount, bool isFromExplosion = false)
    {
        // Nếu không phải sát thương từ vụ nổ, chỉ nháy sáng báo hiệu chứ không trừ máu
        if (!isFromExplosion)
        {
            if (spriteRenderer != null)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(HitFlashRoutine(1));
            }
            return;
        }

        // Nếu là sát thương nổ, nhận sát thương như bình thường
        base.TakeDamage(damageAmount, isFromExplosion);
    }

    protected override void Die()
    {
        if (isDying) return;

        // 50% cơ hội rơi ra vật phẩm khi bị tiêu diệt
        if (Random.value <= 0.5f)
        {
            SpawnRandomPickup();
        }
        base.Die();
    }
}