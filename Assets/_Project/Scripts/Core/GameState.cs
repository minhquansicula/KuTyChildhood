using UnityEngine;

/// <summary>
/// Enum đại diện cho các trạng thái chính của game.
/// Dùng bởi GameManager để theo dõi game đang ở hồi nào.
/// </summary>
public enum GameState
{
    MainMenu,
    Act1_RealWorld,     // Hồi 1: Thế giới hiện thực (căn nhà cũ)
    Act2_MemoryWorld,   // Hồi 2: Thế giới ký ức (gameplay chính)
    Act3_Ending         // Hồi 3: Kết thúc (thức tỉnh)
}
