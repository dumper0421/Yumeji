using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 화면 전체에 포토샵식 곱하기 / 색상 닷지 레이어를 얹는 Renderer Feature.
/// Renderer2D 에셋에 등록해 두고, 세기는 코드에서 <see cref="Weight"/>로 조절한다.
///
/// Weight가 0이면 패스를 아예 넣지 않으므로 이 효과를 쓰지 않는 씬에는 비용이 없다.
/// 포스트 프로세싱 전에 그리므로 Volume의 Color Adjustments는 이 결과 위에 걸린다.
/// Screen Space Overlay UI(대사창 등)는 그 뒤에 그려져서 영향을 받지 않는다.
/// </summary>
public class ScreenBlendLayersFeature : ScriptableRendererFeature
{
    private static readonly int WeightId = Shader.PropertyToID("_ScreenBlendWeight");

    [Tooltip("Yumeji/ScreenBlendLayers 셰이더를 쓰는 머티리얼. 색과 불투명도는 여기서 조절한다.")]
    [SerializeField]
    private Material _material;

    [SerializeField]
    private RenderPassEvent _event = RenderPassEvent.BeforeRenderingPostProcessing;

    private BlendPass _pass;

    /// <summary>0이면 꺼짐, 1이면 머티리얼에 설정한 그대로.</summary>
    public static float Weight
    {
        get => Shader.GetGlobalFloat(WeightId);
        set => Shader.SetGlobalFloat(WeightId, Mathf.Clamp01(value));
    }

    public override void Create()
    {
        _pass = new BlendPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null || Weight <= 0f)
            return;

        CameraType cameraType = renderingData.cameraData.cameraType;
        if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
            return;

        _pass.renderPassEvent = _event;
        _pass.ConfigureInput(ScriptableRenderPassInput.Color);
        renderer.EnqueuePass(_pass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (_material == null || Weight <= 0f)
            return;

        _pass.Setup(renderer.cameraColorTargetHandle, _material);
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
    }

    private class BlendPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler Sampler = new ProfilingSampler("ScreenBlendLayers");

        private RTHandle _source;
        private RTHandle _temp;
        private Material _material;

        public void Setup(RTHandle source, Material material)
        {
            _source = source;
            _material = material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(
                ref _temp,
                desc,
                FilterMode.Point,
                TextureWrapMode.Clamp,
                name: "_ScreenBlendLayersTemp"
            );
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _source == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, Sampler))
            {
                // 자기 자신을 읽으면서 쓸 수 없으므로 임시 텍스처에 복사한 뒤 합성해서 되돌린다
                Blitter.BlitCameraTexture(cmd, _source, _temp);
                Blitter.BlitCameraTexture(cmd, _temp, _source, _material, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _temp?.Release();
        }
    }
}
