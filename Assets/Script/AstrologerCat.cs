using UnityEngine;

public class AstrologerCat : MonoBehaviour
{
    [Header("== Cài Đặt Di Chuyển ==")]
    [Tooltip("Vị trí mèo sẽ dừng lại để thực hiện sự kiện (thường là vị trí BossStopPosition).")]
    public Vector3 stopPosition = new Vector3(8f, 0f, 0f);
    [Tooltip("Tốc độ mèo di chuyển đến vị trí dừng (đơn vị/giây).")]
    public float moveSpeed = 5f;
    [Tooltip("Thời gian chờ đợi trước khi mèo rời đi sau khi sự kiện Tarot Card kết thúc.")]
    public float waitBeforeExitDuration = 1f;

    private bool hasStopped = false;
    private bool isExiting = false;

    // Vị trí để mèo bay ra khỏi màn hình (xa hơn vị trí spawn)
    private readonly Vector3 exitPosition = new Vector3(-12f, 0f, 0f);

    void Start()
    {
        // Vị trí ban đầu đã được LevelManager đặt (thường là x=12f)
        // Bắt đầu di chuyển ngay lập tức
    }

    void Update()
    {
        if (!hasStopped)
        {
            // Di chuyển đến vị trí dừng
            transform.position = Vector3.MoveTowards(transform.position, stopPosition, moveSpeed * Time.deltaTime);

            if (transform.position == stopPosition)
            {
                hasStopped = true;
                // Khi mèo dừng lại, nó có thể kích hoạt hiệu ứng hình ảnh hoặc âm thanh
                Debug.Log("Astrologer Cat: Stopped at the center, waiting for Tarot Card logic.");

                // Lưu ý: Logic Tarot Card đã được LevelManager kích hoạt ngay sau khi spawn mèo.
                // Mèo chỉ cần đứng yên và chờ Tarot Card hoàn tất.
            }
        }
        else if (isExiting)
        {
            // Di chuyển ra khỏi màn hình
            transform.position = Vector3.MoveTowards(transform.position, exitPosition, moveSpeed * Time.deltaTime);

            if (transform.position == exitPosition)
            {
                // Mèo đã rời khỏi màn hình, hủy đối tượng
                Destroy(gameObject);
            }
        }
    }

    // Hàm được gọi từ script Tarot Card sau khi lá bài được chọn
    public void StartExitRoutine()
    {
        // Bắt đầu Coroutine để chờ đợi rồi rời đi
        StartCoroutine(ExitSequence());
    }

    private System.Collections.IEnumerator ExitSequence()
    {
        // Chờ một chút để tạo sự liền mạch sau khi lá bài biến mất
        yield return new WaitForSeconds(waitBeforeExitDuration);

        Debug.Log("Astrologer Cat: Starting exit.");
        isExiting = true;

        // Cần đảm bảo rằng sau khi mèo đi, game sẽ được Resume.
        // Tuy nhiên, logic Resume Game (ResumeGameAfterTarot) đã được gọi trong TarotCard.cs
        // Nên mèo chỉ cần tự hủy.
    }
}