using UnityEngine;

/// <summary>
/// Enum đại diện cho các trạng thái chính của game.
/// Dùng bởi GameManager để theo dõi game đang ở hồi nào.
/// </summary>
public enum GameState
{
    MainMenu,
    Act1_RealWorld,             // Hồi 1: Văn phòng
    Act2_MemoryWorld_Home,      // Hồi 2: Trong nhà
    Act3_MemoryWorld_OutSide,   // Hồi 3: Ngoài trời
    Act_Ending                  // Kết thúc
}
