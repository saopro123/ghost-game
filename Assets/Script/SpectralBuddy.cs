using UnityEngine;

public class SpectralBuddy : MonoBehaviour
{
    [Header("== Follow Settings ==")]
    public Transform playerTransform;
    public Vector3 followOffset = new Vector3(-1.5f, 0.5f, 0f); // Bay phía sau và hơi cao hơn player
    public float followSpeed = 5f;

    [Header("== Shooting Settings ==")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.5f; // Bắn chậm hơn player một chút
    private float nextFireTime;

    void Start()
    {
        // Nếu chưa gán player, tự đi tìm
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        // Làm con đệ hơi trong suốt cho giống linh hồn
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0.6f;
            sr.color = c;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // 1. Logic bay theo (Lướt mượt mà)
        Vector3 targetPosition = playerTransform.position + followOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        // 2. Logic tự động bắn
        if (Time.time > nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            PlayerBullet bulletScript = bulletObj.GetComponent<PlayerBullet>();

            if (bulletScript != null)
            {
                // Sát thương bằng 50% sát thương hiện tại của Player
                bulletScript.damage = Mathf.RoundToInt(Player.Instance.damagePerBullet * 0.5f);
                // Làm đạn của đệ nhỏ hơn một chút cho dễ phân biệt
                bulletObj.transform.localScale *= 0.7f;
            }
        }
    }
}