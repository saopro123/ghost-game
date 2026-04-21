using UnityEngine;

[CreateAssetMenu(fileName = "New Blessing", menuName = "Game/Blessing Data")]
public class BlessingData : ScriptableObject
{
    public string blessingName;
    [TextArea] public string description;
    public Sprite cardFace; // Hình ảnh thẻ bạn sắp vẽ
    public BlessingType type;
    public int id; // Định danh để xử lý logic

    public enum BlessingType { Divine, Devil, Mystic }
}