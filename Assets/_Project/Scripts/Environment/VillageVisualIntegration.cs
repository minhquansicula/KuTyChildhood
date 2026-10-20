using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Scene-local atmosphere and render budget; restores the project's pipeline on leaving the village.</summary>
[ExecuteAlways,DisallowMultipleComponent]
public sealed class VillageVisualIntegration : MonoBehaviour
{
    public UniversalRenderPipelineAsset villagePipeline;
    public Color fogColor=new Color(.871f,.796f,.694f);
    public float fogDensity=.007f,heightFalloff=.22f;
    public bool useHeightFog=true;
    private RenderPipelineAsset previousPipeline;
    private UniversalRenderPipelineAsset runtimePipeline;
    private bool ownsPipeline;
    private void OnEnable(){Apply();}
    private void OnValidate(){if(isActiveAndEnabled)Apply();}
    public void Apply()
    {
        UniversalRenderPipelineAsset activePipeline = villagePipeline;
        if (villagePipeline && Application.isPlaying)
        {
            // Adapt resolution on an instance; never change the project's shared URP asset in Play Mode.
            if (!runtimePipeline)
            {
                runtimePipeline = Instantiate(villagePipeline);
                runtimePipeline.name = villagePipeline.name + " (Runtime)";
                runtimePipeline.hideFlags = HideFlags.DontSave;
            }
            runtimePipeline.renderScale = Mathf.Min(1f, 1920f / Mathf.Max(Screen.width, 1));
            activePipeline = runtimePipeline;
        }
        if(activePipeline && QualitySettings.renderPipeline!=activePipeline)
        {
            if (!ownsPipeline) previousPipeline = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline=activePipeline;
            ownsPipeline=true;
        }
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=fogColor;RenderSettings.fogDensity=fogDensity;
        RenderSettings.reflectionIntensity=.6f;RenderSettings.ambientIntensity=.7f;
        Shader.SetGlobalColor("_VillageFogColor",QualitySettings.activeColorSpace==ColorSpace.Linear?fogColor.linear:fogColor);
        Shader.SetGlobalVector("_VillageFogParameters",new Vector4(fogDensity,heightFalloff,.2f,useHeightFog?1:0));
    }
    private void OnDisable()
    {
        Shader.SetGlobalVector("_VillageFogParameters",Vector4.zero);
        if(ownsPipeline && (QualitySettings.renderPipeline==villagePipeline || QualitySettings.renderPipeline==runtimePipeline))
            QualitySettings.renderPipeline=previousPipeline;
        ownsPipeline=false;
        if (runtimePipeline)
        {
            if (Application.isPlaying) Destroy(runtimePipeline);
            else DestroyImmediate(runtimePipeline);
            runtimePipeline=null;
        }
    }
}
