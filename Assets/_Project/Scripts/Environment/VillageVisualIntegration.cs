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
    private bool ownsPipeline;
    private void OnEnable(){Apply();}
    private void OnValidate(){if(isActiveAndEnabled)Apply();}
    public void Apply()
    {
        if(villagePipeline && QualitySettings.renderPipeline!=villagePipeline)
        {previousPipeline=QualitySettings.renderPipeline;QualitySettings.renderPipeline=villagePipeline;ownsPipeline=true;}
        if(villagePipeline && Application.isPlaying)villagePipeline.renderScale=Mathf.Min(1,1920f/Mathf.Max(Screen.width,1));
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=fogColor;RenderSettings.fogDensity=fogDensity;
        RenderSettings.reflectionIntensity=.6f;RenderSettings.ambientIntensity=.7f;
        Shader.SetGlobalColor("_VillageFogColor",QualitySettings.activeColorSpace==ColorSpace.Linear?fogColor.linear:fogColor);
        Shader.SetGlobalVector("_VillageFogParameters",new Vector4(fogDensity,heightFalloff,.2f,useHeightFog?1:0));
    }
    private void OnDisable()
    {
        Shader.SetGlobalVector("_VillageFogParameters",Vector4.zero);
        if(ownsPipeline && QualitySettings.renderPipeline==villagePipeline)QualitySettings.renderPipeline=previousPipeline;
        ownsPipeline=false;
    }
}
