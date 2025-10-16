using UnityEngine;
using Unity.Cinemachine;

[ExecuteAlways]
public class CinemachineConfineX : CinemachineExtension
{
    [Tooltip("Fallback minimum world X if the GameController does not expose limits.")]
    public float minX = -5.451f;

    [Tooltip("Fallback maximum world X if the GameController does not expose limits.")]
    public float maxX = 200f;

    [Tooltip("Extra padding applied to controller limits.")]
    public Vector2 padding = Vector2.zero;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Body)
            return;

        float clampMin = minX;
        float clampMax = maxX;

        var controller = GameController.instance;
        if (controller != null && controller.AreHorizontalLimitsEnabled)
        {
            clampMin = controller.CurrentMinX + padding.x;
            clampMax = controller.CurrentMaxX + padding.y;
        }

        if (clampMin > clampMax)
        {
            float temp = clampMin;
            clampMin = clampMax;
            clampMax = temp;
        }

        var pos = state.RawPosition;
        pos.x = Mathf.Clamp(pos.x, clampMin, clampMax);
        state.RawPosition = pos;
    }
}
