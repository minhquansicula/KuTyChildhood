using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Uses the office's render asset only while this scene is loaded.</summary>
[DisallowMultipleComponent]
public sealed class OfficeRenderQuality : MonoBehaviour
{
    [SerializeField] private RenderPipelineAsset pipeline;
    private RenderPipelineAsset previous;
    private int qualityLevel;
    private int previousAA;
    private bool applied;

    public void Configure(RenderPipelineAsset asset)
    {
        Restore();
        pipeline = asset;
        Apply();
    }

    private void OnEnable() => Apply();
    private void OnDisable() => Restore();

    private void Apply()
    {
        if (!Application.isPlaying || applied || !pipeline) return;
        qualityLevel = QualitySettings.GetQualityLevel();
        previous = QualitySettings.renderPipeline;
        previousAA = QualitySettings.antiAliasing;
        QualitySettings.renderPipeline = pipeline;
        applied = true;
    }

    private void Restore()
    {
        if (!applied) return;
        if (QualitySettings.GetQualityLevel() == qualityLevel && QualitySettings.renderPipeline == pipeline)
        {
            QualitySettings.renderPipeline = previous;
            QualitySettings.antiAliasing = previousAA;
        }
        applied = false;
    }
}
