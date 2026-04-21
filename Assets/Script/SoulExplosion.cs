// Trong SoulExplosion.cs
using UnityEngine;

public class SoulExplosion : MonoBehaviour
{
    [HideInInspector] public int explosionDamage; // Sẽ được gán từ Enemy
    public float lifetime = 0.5f;

    void Start()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayExplosionSFX();
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Gửi sát thương và đánh dấu isFromExplosion = true
                enemy.TakeDamage(explosionDamage, true);
            }
        }
    }
}