using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    // Đảm bảo chỉ có một instance duy nhất (Singleton)
    public static AudioManager Instance;

    [Header("== Audio Sources ==")]
    [Tooltip("AudioSource cho nhạc nền (nhạc lặp)")]
    public AudioSource MusicSource;
    [Tooltip("AudioSource cho hiệu ứng âm thanh (SFX)")]
    public AudioSource SFXSource;

    [Header("== Music Clips ==")]
    [Tooltip("Nhạc nền khi chơi game (BGM)")]
    public AudioClip mainBackgroundMusic;
    [Tooltip("Nhạc nền khi Boss xuất hiện (Boss BGM)")]
    public AudioClip bossMusic;

    [Header("== Sound Effect Clips ==")]
    [Tooltip("Âm thanh khi người chơi bắn đạn")]
    public AudioClip playerShootSFX;

    [Header("== Fade Settings ==")]
    [Tooltip("Thời gian chuyển đổi âm thanh (giây)")]
    public float fadeDuration = 1.0f;
    [Tooltip("Mức âm lượng nhạc nền tối đa")]
    [Range(0f, 1f)]
    public float maxMusicVolume = 0.6f;
    public AudioClip frostBlastSFX;

    private Coroutine fadeRoutine;

    void Awake()
    {
        // Thực hiện logic Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Thiết lập âm lượng tối đa ban đầu
        MusicSource.volume = maxMusicVolume;

        // Bắt đầu chơi nhạc nền mặc định ngay khi khởi động
        PlayMusic(mainBackgroundMusic);
    }

    // ==========================================================
    // ** CHỨC NĂNG CƠ BẢN VÀ CẢI TIẾN FADE **
    // ==========================================================

    /// <summary>
    /// Phát nhạc nền mới với hiệu ứng fade-out/fade-in.
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || MusicSource == null) return;

        // Nếu clip hiện tại đã là clip muốn phát, không làm gì cả
        if (MusicSource.clip == clip && MusicSource.isPlaying) return;

        // Nếu đang có Coroutine Fade đang chạy, dừng nó lại
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        // Bắt đầu Coroutine chuyển đổi nhạc
        fadeRoutine = StartCoroutine(FadeMusicRoutine(clip));
    }

    private IEnumerator FadeMusicRoutine(AudioClip newClip)
    {
        // 1. Fade Out (Giảm âm lượng về 0)
        float startVolume = MusicSource.volume;
        float timer = 0f;

        if (startVolume > 0.01f)
        {
            while (timer < fadeDuration)
            {
                // SỬA TẠI ĐÂY: Dùng unscaledDeltaTime
                timer += Time.unscaledDeltaTime;
                MusicSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
                yield return null;
            }
        }

        MusicSource.Stop();

        // 2. Thiết lập nhạc mới
        MusicSource.clip = newClip;
        MusicSource.loop = true;
        MusicSource.Play();

        // 3. Fade In (Tăng âm lượng lên maxMusicVolume)
        timer = 0f;
        while (timer < fadeDuration)
        {
            // SỬA TẠI ĐÂY: Dùng unscaledDeltaTime
            timer += Time.unscaledDeltaTime;
            MusicSource.volume = Mathf.Lerp(0f, maxMusicVolume, timer / fadeDuration);
            yield return null;
        }

        MusicSource.volume = maxMusicVolume;
        fadeRoutine = null;
    }
    /// <summary>
    /// Phát một hiệu ứng âm thanh (SFX) một lần.
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && SFXSource != null)
        {
            SFXSource.PlayOneShot(clip);
        }
    }

    // ==========================================================
    // ** HÀM GỌI CHO GAME LOGIC **
    // ==========================================================

    public void PlayBossMusic()
    {
        PlayMusic(bossMusic);
    }

    public void PlayMainMusic()
    {
        PlayMusic(mainBackgroundMusic);
    }

    public void PlayPlayerShootSFX()
    {
        PlaySFX(playerShootSFX);
    }
    // Thêm vào AudioManager.cs

    [Header("== New Sound Effects ==")]
    public AudioClip explosionSFX;
    public AudioClip coinHitSFX;
    public AudioClip pickupSFX; // Tiện tay thêm tiếng nhặt đồ luôn

    // Hàm phát tiếng nổ
    public void PlayExplosionSFX()
    {
        if (explosionSFX != null)
            SFXSource.PlayOneShot(explosionSFX);
    }

    // Hàm phát tiếng đồng xu bị trúng đạn
    public void PlayCoinHitSFX()
    {
        if (coinHitSFX != null)
            SFXSource.PlayOneShot(coinHitSFX);
    }

    // Hàm phát tiếng nhặt đồ
    public void PlayPickupSFX()
    {
        if (pickupSFX != null)
            SFXSource.PlayOneShot(pickupSFX);
    }
    public void PlayFrostBlastSFX()
    {
        if (frostBlastSFX != null)
            SFXSource.PlayOneShot(frostBlastSFX);
    }

    // Hàm dùng để tắt nhạc ngay lập tức
    public void StopMusic()
    {
        if (MusicSource != null)
        {
            MusicSource.Stop();
            fadeRoutine = null; // Hủy coroutine fade nếu đang chạy
        }
    }
    [Header("== Music Clips ==")]
    // ... các biến cũ ...
    public AudioClip dialogueMusic; // Nhạc nền dành riêng cho hội thoại

    [Header("== Sound Effect Clips ==")]
    // ... các biến cũ ...
    public AudioClip nextDialogueSFX; // Tiếng click khi qua lời thoại

    // --- HÀM GỌI CHO DIALOGUE ---

    public void PlayDialogueMusic()
    {
        PlayMusic(dialogueMusic);
    }

    public void PlayNextDialogueSFX()
    {
        if (nextDialogueSFX != null)
            SFXSource.PlayOneShot(nextDialogueSFX);
    }
}