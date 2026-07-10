using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Ép tính lại collider của tilemap khi vào scene.
///
/// Lý do cần: CompositeCollider2D lưu sẵn hình dạng collider đã bake trong file
/// scene. Nếu tile được thêm/sửa từ bên ngoài Unity (không qua Tile Palette),
/// phần bake này không được cập nhật — đứng trên tile mới sẽ rơi xuyên sàn.
/// Script này rebuild toàn bộ ngay khi scene chạy nên collider luôn khớp
/// với những gì đã vẽ.
///
/// Cách dùng: gắn vào GameObject tilemap có collider (vd "Ground").
/// </summary>
[RequireComponent(typeof(TilemapCollider2D))]
public class TilemapColliderRefresher : MonoBehaviour
{
    void Start()
    {
        Tilemap tilemap = GetComponent<Tilemap>();
        TilemapCollider2D tilemapCollider = GetComponent<TilemapCollider2D>();
        CompositeCollider2D composite = GetComponent<CompositeCollider2D>();

        // 1) Đánh dấu lại TOÀN BỘ tile là "vừa thay đổi" để hàng đợi collider có việc
        if (tilemap != null)
            tilemap.RefreshAllTiles();

        // 2) Tắt/bật TilemapCollider2D: buộc Unity vứt shape cũ và sinh lại từ đầu
        //    (chỉ gọi ProcessTilemapChanges thôi là không đủ — lúc load scene không có
        //    thay đổi nào đang chờ nên nó no-op, composite vẫn dùng geometry bake cũ)
        tilemapCollider.enabled = false;
        tilemapCollider.enabled = true;
        tilemapCollider.ProcessTilemapChanges();

        // 3) Dựng lại composite từ shape mới
        if (composite != null)
        {
            composite.GenerateGeometry();
            Debug.Log($"[TilemapColliderRefresher] {name}: đã dựng lại collider — {composite.pathCount} đường, {composite.pointCount} điểm.");
        }
    }
}
