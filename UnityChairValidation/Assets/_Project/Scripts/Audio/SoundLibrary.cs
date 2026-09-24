using UnityEngine;

/// <summary>
/// SoundLibrary — ScriptableObject chứa tất cả AudioClip references.
/// 
/// Tạo asset: Right-click → Create → KuTy → Sound Library
/// Gán các audio clip trong Inspector.
/// AudioManager sẽ dùng SoundLibrary để play âm thanh.
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "KuTy/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Header("Background Music")]
    public AudioClip bgmMainMenu;
    public AudioClip bgmAct1;           // Tông xám lạnh, buồn
    public AudioClip bgmAct2;           // Lo-fi/acoustic ấm áp, hoài niệm
    public AudioClip bgmAct3;           // Nhẹ nhàng, cảm động

    [Header("SFX - Rửa chén")]
    public AudioClip sfxWaterSplash;    // Tiếng nước rửa
    public AudioClip sfxDishClean;      // Tiếng chén sạch / ting!

    [Header("SFX - Bắn bi")]
    public AudioClip sfxMarbleShoot;    // Tiếng bắn bi
    public AudioClip sfxMarbleHit;      // Tiếng bi va chạm
    public AudioClip sfxMarbleRoll;     // Tiếng bi lăn

    [Header("SFX - Shop")]
    public AudioClip sfxPurchase;       // Tiếng mua thành công (cha-ching)
    public AudioClip sfxPurchaseFail;   // Tiếng mua thất bại (buzz)

    [Header("SFX - Memory")]
    public AudioClip sfxMemoryCollected;    // Tiếng chuông nhẹ khi thu thập ký ức
    public AudioClip sfxAllMemoriesComplete; // Tiếng hội tụ chìa khóa

    [Header("SFX - UI")]
    public AudioClip sfxButtonClick;
    public AudioClip sfxButtonHover;

    [Header("Ambient")]
    public AudioClip ambBirds;          // Tiếng chim
    public AudioClip ambWindChime;      // Tiếng chuông gió
    public AudioClip ambKitchen;        // Tiếng bếp (nước sôi, v.v.)
}
