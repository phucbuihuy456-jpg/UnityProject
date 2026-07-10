/// <summary>
/// Lưu giữ tiến trình người chơi (máu hiện tại và số flame đã nhặt) xuyên suốt
/// các lần chuyển scene.
///
/// Dùng biến static nên dữ liệu tồn tại trong suốt phiên chơi (cho tới khi thoát
/// game hoặc gọi Clear()). Khi đi qua cửa hầm (DungeonDoor) sang màn mới, máu và
/// flame sẽ được giữ nguyên; khi bắt đầu game mới hoặc chơi lại sau khi chết thì
/// gọi Clear() để về giá trị mặc định.
/// </summary>
public static class GameProgress
{
    // Đã có dữ liệu được lưu hay chưa. false = bắt đầu màn mới (dùng giá trị mặc định).
    public static bool HasData = false;

    // Máu hiện tại của người chơi (giá trị tuyệt đối).
    public static float Health = 0f;

    // Tổng số flame đã nhặt, giữ đồng nhất giữa các scene.
    public static int FlameCount = 0;

    /// <summary>Lưu lại máu hiện tại.</summary>
    public static void SaveHealth(float health)
    {
        Health = health;
        HasData = true;
    }

    /// <summary>Lưu lại số flame đã nhặt.</summary>
    public static void SaveFlames(int flameCount)
    {
        FlameCount = flameCount;
        HasData = true;
    }

    /// <summary>Xóa tiến trình (bắt đầu game mới / chơi lại từ đầu).</summary>
    public static void Clear()
    {
        HasData = false;
        Health = 0f;
        FlameCount = 0;
    }
}
