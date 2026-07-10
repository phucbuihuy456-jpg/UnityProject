using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;

/// <summary>
/// Cinemachine Extension giới hạn KHUNG NHÌN camera trong một vùng chữ nhật,
/// để camera không bao giờ lộ phần map chưa vẽ — cả 4 phía, kể cả khi nhảy chạm trần.
///
/// Khác với việc chỉ kẹp vị trí tâm camera: script tính cả nửa chiều rộng/cao
/// khung nhìn (theo OrthographicSize và Aspect) nên MÉP camera mới là thứ bị chặn.
///
/// Cách dùng: gắn vào GameObject "CinemachineCamera".
///  - Kéo tilemap (vd Background) vào "Bounds Tilemap": vùng giới hạn = vùng đã vẽ,
///    tự mở rộng khi bạn vẽ thêm. Tilemap trống => KHÔNG giới hạn (camera tự do).
///  - Hoặc bật "Use Manual Bounds" và nhập hình chữ nhật min/max thủ công.
/// </summary>
[AddComponentMenu("Cinemachine/Extensions/Camera Bounds")]
[SaveDuringPlay]
public class CameraBounds : CinemachineExtension
{
    [Header("Tự lấy vùng theo Tilemap (ưu tiên)")]
    [Tooltip("Kéo tilemap Background (hoặc Ground) vào đây. Vùng = phần đã vẽ. Trống => không giới hạn.")]
    public Tilemap boundsTilemap;

    [Header("Hoặc nhập hình chữ nhật tay")]
    [Tooltip("Bật để dùng min/max bên dưới thay cho Tilemap")]
    public bool useManualBounds = false;
    public Vector2 min = new Vector2(-20f, -10f);
    public Vector2 max = new Vector2(20f, 10f);

    [Header("Lề thu vào (tùy chọn)")]
    [Tooltip("Thu nhỏ vùng vào trong ngần này (đơn vị world)")]
    public float padding = 0f;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize)
            return;

        if (!TryGetBounds(out float minX, out float maxX, out float minY, out float maxY))
            return; // không có vùng hợp lệ -> không giới hạn

        // Nửa kích thước khung nhìn của camera
        float halfH = state.Lens.OrthographicSize;
        float halfW = halfH * state.Lens.Aspect;

        Vector3 pos = state.GetCorrectedPosition();
        float x = pos.x, y = pos.y;

        // Trục X: nếu vùng hẹp hơn khung nhìn thì canh giữa, ngược lại kẹp mép
        if (maxX - minX <= 2f * halfW) x = (minX + maxX) * 0.5f;
        else x = Mathf.Clamp(x, minX + halfW, maxX - halfW);

        // Trục Y
        if (maxY - minY <= 2f * halfH) y = (minY + maxY) * 0.5f;
        else y = Mathf.Clamp(y, minY + halfH, maxY - halfH);

        state.PositionCorrection += new Vector3(x - pos.x, y - pos.y, 0f);
    }

    bool TryGetBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        minX = maxX = minY = maxY = 0f;

        if (!useManualBounds)
        {
            if (boundsTilemap == null)
                return false; // chưa gán tilemap, chưa bật manual -> không giới hạn
            BoundsInt cb = boundsTilemap.cellBounds;
            if (cb.size.x <= 0 || cb.size.y <= 0)
                return false; // tilemap trống
            Vector3 wMin = boundsTilemap.CellToWorld(cb.min);
            Vector3 wMax = boundsTilemap.CellToWorld(cb.max);
            minX = wMin.x; maxX = wMax.x; minY = wMin.y; maxY = wMax.y;
        }
        else
        {
            minX = min.x; maxX = max.x; minY = min.y; maxY = max.y;
        }

        minX += padding; minY += padding;
        maxX -= padding; maxY -= padding;

        return maxX > minX && maxY > minY;
    }

    void OnDrawGizmosSelected()
    {
        if (!TryGetBounds(out float minX, out float maxX, out float minY, out float maxY))
            return;
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Gizmos.DrawWireCube(center, new Vector3(maxX - minX, maxY - minY, 0f));
    }
}
