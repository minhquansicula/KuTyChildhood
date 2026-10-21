using UnityEditor;
using UnityEngine;

// Import settings for the new task artwork; existing art keeps its authored settings.
public sealed class HouseholdTaskArtImporter : AssetPostprocessor
{
    private static readonly string[] Names =
        { "Task_Worktop", "Dish_Sponge", "Letter_Page", "Fire_Kindling", "Dish_Rack", "Fold_Blanket",
          "Wood_Mat", "Wood_Rope", "Wood_Log_Dark", "Rice_Pot_Served" };

    [InitializeOnLoadMethod]
    private static void ScheduleExistingSprites()
    {
        EditorApplication.delayCall += ConfigureExistingSprites;
    }

    private static void ConfigureExistingSprites()
    {
        foreach (string name in Names)
        {
            string path = "Assets/_Project/Resources/Scene2/MiniGames/" + name + ".png";
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
            int maxSize = MaxSize(name);
            bool transparency = name != "Task_Worktop";
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.maxTextureSize == maxSize && importer.alphaIsTransparency == transparency &&
                !importer.mipmapEnabled && importer.wrapMode == TextureWrapMode.Clamp) continue;
            Apply(importer, name);
            importer.SaveAndReimport();
        }
    }

    private static int MaxSize(string name) =>
        name == "Task_Worktop" || name == "Letter_Page" || name == "Wood_Mat" ? 1024 :
        name == "Wood_Rope" || name == "Wood_Log_Dark" ? 512 : 256;

    private static void Apply(TextureImporter importer, string name)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = name != "Task_Worktop";
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.maxTextureSize = MaxSize(name);
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
    }

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/_Project/Resources/Scene2/MiniGames/")) return;
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (name != "Task_Worktop" && name != "Dish_Sponge" && name != "Letter_Page" &&
            name != "Fire_Kindling" && name != "Dish_Rack" && name != "Fold_Blanket" &&
            name != "Wood_Mat" && name != "Wood_Rope" && name != "Wood_Log_Dark" &&
            name != "Rice_Pot_Served") return;
        Apply((TextureImporter)assetImporter, name);
    }
}
