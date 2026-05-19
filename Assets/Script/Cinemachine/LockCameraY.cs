using Cinemachine;
using UnityEngine;

public class LockCameraY : CinemachineExtension
{
    public float fixedY = 0f; // 고정할 y좌표

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state, float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Finalize)
        {
            Vector3 pos = state.FinalPosition;
            pos.y = fixedY; // y축 값을 강제로 고정
            state.PositionCorrection += (pos - state.FinalPosition);
        }
    }
}
