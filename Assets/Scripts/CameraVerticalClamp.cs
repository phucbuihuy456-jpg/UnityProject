using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Cinemachine Extension giới hạn vị trí Y của camera.
/// Gắn component này vào GameObject "CinemachineCamera".
///
/// - clampMax + maxY: camera KHÔNG bao giờ đi lên cao hơn maxY.
///   => Khi nhân vật nhảy lên, camera dừng lại ở maxY nên không lộ
///      phần phía trên map còn trống / chưa vẽ.
/// - clampMin + minY: camera không đi xuống thấp hơn minY (tùy chọn).
/// - lockY: khóa cứng Y tại giá trị maxY (camera chỉ pan ngang).
/// </summary>
[AddComponentMenu("Cinemachine/Extensions/Camera Vertical Clamp")]
[SaveDuringPlay]
public class CameraVerticalClamp : CinemachineExtension
{
    [Header("Giới hạn trên (chống lộ phần trên map)")]
    [Tooltip("Bật để giới hạn độ cao tối đa của camera")]
    public bool clampMax = true;
    [Tooltip("Camera không vượt lên cao hơn giá trị Y này")]
    public float maxY = 1.68f;

    [Header("Giới hạn dưới (tùy chọn)")]
    [Tooltip("Bật để giới hạn độ cao tối thiểu của camera")]
    public bool clampMin = false;
    [Tooltip("Camera không đi xuống thấp hơn giá trị Y này")]
    public float minY = 0f;

    [Header("Khóa cứng trục Y")]
    [Tooltip("Nếu bật: camera giữ nguyên Y = maxY, chỉ di chuyển ngang theo nhân vật")]
    public bool lockY = false;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        // Xử lý ở bước cuối, sau khi Cinemachine đã tính xong vị trí.
        if (stage != CinemachineCore.Stage.Finalize)
            return;

        Vector3 pos = state.GetCorrectedPosition();
        float targetY = pos.y;

        if (lockY)
        {
            targetY = maxY;
        }
        else
        {
            if (clampMax && targetY > maxY) targetY = maxY;
            if (clampMin && targetY < minY) targetY = minY;
        }

        // Áp chỉnh sửa thông qua PositionCorrection (cách an toàn của Cinemachine 3.x).
        state.PositionCorrection += new Vector3(0f, targetY - pos.y, 0f);
    }
}
