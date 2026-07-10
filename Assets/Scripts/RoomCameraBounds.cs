using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Giới hạn KHUNG NHÌN camera theo TỪNG PHÒNG — dùng cho Level 2 nơi các phòng
/// tách rời nhau: camera bị "nhốt" trong hình chữ nhật của phòng đang chứa
/// người chơi, nên không bao giờ lộ vùng map chưa vẽ (kể cả khoảng trống giữa
/// các phòng). Khi người chơi teleport sang phòng khác, camera tự đổi vùng nhốt.
///
/// Cách dùng: gắn vào GameObject "CinemachineCamera" (cùng chỗ CinemachineCamera),
/// khai báo rect từng phòng (x, y = góc dưới-trái theo world; width/height).
/// Phòng hẹp/thấp hơn khung nhìn thì camera canh giữa phòng theo trục đó.
/// </summary>
[AddComponentMenu("Cinemachine/Extensions/Room Camera Bounds")]
[SaveDuringPlay]
public class RoomCameraBounds : CinemachineExtension
{
    [System.Serializable]
    public struct Room
    {
        public string name;
        public Rect area;
    }

    [Tooltip("Danh sách phòng — camera bị nhốt trong phòng đang chứa người chơi")]
    public Room[] rooms;

    // Phòng hiện tại (giữ lại khi người chơi thoáng chốc không nằm trong phòng nào)
    private int currentRoom = -1;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize || rooms == null || rooms.Length == 0)
            return;

        Transform target = vcam.Follow;
        if (target == null)
            return;

        int idx = FindRoom(target.position);
        if (idx < 0)
            return;

        Rect r = rooms[idx].area;

        // Nửa kích thước khung nhìn — kẹp MÉP camera chứ không chỉ kẹp tâm
        float halfH = state.Lens.OrthographicSize;
        float halfW = halfH * state.Lens.Aspect;

        Vector3 pos = state.GetCorrectedPosition();

        // Phòng hẹp hơn khung nhìn -> canh giữa; ngược lại kẹp trong mép phòng
        float x = (r.width <= 2f * halfW) ? r.center.x
                : Mathf.Clamp(pos.x, r.xMin + halfW, r.xMax - halfW);
        float y = (r.height <= 2f * halfH) ? r.center.y
                : Mathf.Clamp(pos.y, r.yMin + halfH, r.yMax - halfH);

        state.PositionCorrection += new Vector3(x - pos.x, y - pos.y, 0f);
    }

    int FindRoom(Vector3 p)
    {
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].area.Contains((Vector2)p))
            {
                currentRoom = i;
                return i;
            }
        }

        // Không nằm trong phòng nào (khung hình teleport/rơi ra mép): giữ phòng cũ
        if (currentRoom >= 0)
            return currentRoom;

        // Chưa từng có phòng: lấy phòng gần nhất
        int nearest = -1;
        float best = float.MaxValue;
        for (int i = 0; i < rooms.Length; i++)
        {
            Vector2 c = rooms[i].area.center;
            float d = ((Vector2)p - c).sqrMagnitude;
            if (d < best) { best = d; nearest = i; }
        }
        currentRoom = nearest;
        return nearest;
    }

    // Vẽ toàn bộ rect phòng trong Scene view để dễ canh chỉnh
    void OnDrawGizmosSelected()
    {
        if (rooms == null)
            return;
        Gizmos.color = Color.yellow;
        foreach (var room in rooms)
        {
            Rect r = room.area;
            Gizmos.DrawWireCube(r.center, new Vector3(r.width, r.height, 0f));
        }
    }
}
