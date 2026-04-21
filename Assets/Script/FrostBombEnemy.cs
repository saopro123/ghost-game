using UnityEngine;

public class FrostBombEnemy : Enemy
{
    protected override void Die()
    {
        if (isDying) return;

        // 1. Phát âm thanh nổ băng
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFrostBlastSFX();
        }

        // 2. Kích hoạt hiệu ứng màn hình xanh
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.StartCoroutine(LevelManager.Instance.FrostBlastEffect());
        }

        // Tiêu diệt quái thường
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy e in allEnemies)
        {
            // e is ShieldProviderEnemy check để không diệt nhầm mini-boss nếu bạn muốn
            if (e != null && !e.isBoss && e != this)
            {
                e.TakeDamage(9999, false);
            }
        }

        base.Die();
    }
}