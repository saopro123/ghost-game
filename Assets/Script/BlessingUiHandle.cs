using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BlessingUIHandler : MonoBehaviour
{
    public Image cardImage;
    public Text nameText;
    public Text descText;
    public Sprite cardBack; // Hình mặt sau bạn vẽ

    private BlessingData currentData;
    private bool isFlipped = false;
    private bool canSelect = false;

    public void Setup(BlessingData data)
    {
        currentData = data;
        isFlipped = false;
        cardImage.sprite = cardBack; // Luôn hiển thị mặt sau trước
        nameText.text = "";
        descText.text = "";
    }

    public void OnCardClick()
    {
        // Kiểm tra thêm: Nếu Canvas đang mờ đi (alpha < 1) thì không cho bấm nữa
        CanvasGroup parentGroup = GetComponentInParent<CanvasGroup>();
        if (parentGroup != null && parentGroup.alpha < 0.9f) return;

        if (!isFlipped)
        {
            StartCoroutine(FlipRoutine());
        }
        else if (canSelect)
        {
            BlessingMenu.Instance.OnBlessingSelected(currentData);
        }
    }

    IEnumerator FlipRoutine()
    {
        // Hiệu ứng co lại rồi giãn ra (giả lập lật bài)
        for (float i = 1; i >= 0; i -= 0.1f)
        {
            transform.localScale = new Vector3(i, 1, 1);
            yield return null;
        }

        cardImage.sprite = currentData.cardFace;
        nameText.text = currentData.blessingName;
        descText.text = currentData.description;
        isFlipped = true;

        for (float i = 0; i <= 1; i += 0.1f)
        {
            transform.localScale = new Vector3(i, 1, 1);
            yield return null;
        }
        isFlipped = true;

        // Đợi 0.5 giây thời gian thực để người chơi kịp nhìn thẻ rồi mới cho phép bấm chọn
        yield return new WaitForSecondsRealtime(1f);
        canSelect = true;
    }
}