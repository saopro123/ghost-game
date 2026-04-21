using UnityEngine;
using UnityEngine.UI;

public class UITextEffects : MonoBehaviour
{
    public enum EffectType { Rainbow, PulsingAlpha }
    public EffectType effect;
    public float speed = 2f;

    private Text text;

    void Start() => text = GetComponent<Text>();

    void Update()
    {
        if (text == null) return;

        if (effect == EffectType.Rainbow)
        {
            // Đổi màu cầu vồng (Dùng unscaledTime để chạy được cả khi game Pause)
            text.color = Color.HSVToRGB(Time.unscaledTime * speed * 0.1f % 1f, 0.7f, 1f);
        }
        else if (effect == EffectType.PulsingAlpha)
        {
            // Nhấp nháy mờ dần (Alpha)
            float alpha = Mathf.PingPong(Time.unscaledTime * speed, 0.7f) + 0.3f;
            Color c = text.color;
            c.a = alpha;
            text.color = c;
        }
    }
}