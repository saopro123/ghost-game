using UnityEngine;

public class Pickup : MonoBehaviour
{
    public enum PickupType { Heal, ExplosiveRounds, Invincibility, BlessingShard }
    public PickupType type;
    public float moveSpeed = 2f;

    void Update()
    {
        // Vật phẩm bay từ phải sang trái chậm chậm để người chơi kịp nhặt
        transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);

        // Tự hủy nếu bay mất khỏi màn hình
        if (transform.position.x < -12f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                ApplyEffect(player);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPickupSFX();
            }
            Destroy(gameObject);
        }
    }

    void ApplyEffect(Player player)
    {
        switch (type)
        {
            case PickupType.Heal:
                player.Heal(25); // Hồi 1 tim
                break;
            case PickupType.ExplosiveRounds:
                player.ActivateExplosiveRounds(10f); // Hiệu ứng nổ trong 10 giây
                break;
            case PickupType.Invincibility:
                player.ActivatePowerInvincibility(5f); // Bất tử 5 giây
                break;
            case PickupType.BlessingShard:
                player.AddBlessingShard();
                break;
        }
    }
}