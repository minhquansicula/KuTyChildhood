using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class Act2BedroomSetup
{
    [MenuItem("KuTy/Setup/1. Build House Layout")]
    public static void BuildLayout()
    {
        // Root object for the house
        GameObject houseRoot = GameObject.Find("HouseLayout");
        if (houseRoot == null)
        {
            houseRoot = new GameObject("HouseLayout");
        }

        // 1. Create Main Floor
        GameObject mainFloor = CreateCube("MainFloor", houseRoot.transform, new Vector3(0, 0, 0), new Vector3(20, 0.5f, 20));
        mainFloor.layer = LayerMask.NameToLayer("Ground");

        // 2. Bedroom Area (Let's put it in the bottom-left corner: X: -5 to -10, Z: -5 to -10)
        // Actually let's use exact coordinates. Bedroom size: 6x6. Center: (-7, 0, -7).
        
        // 3. Bedroom Walls
        // Wall height = 3m
        float wallHeight = 3f;
        float wallThickness = 0.2f;

        // North wall of bedroom (Z = -4)
        CreateCube("BedroomWall_North", houseRoot.transform, new Vector3(-7, wallHeight / 2, -4), new Vector3(6, wallHeight, wallThickness));
        // South wall of bedroom (Z = -10)
        CreateCube("BedroomWall_South", houseRoot.transform, new Vector3(-7, wallHeight / 2, -10), new Vector3(6, wallHeight, wallThickness));
        // West wall of bedroom (X = -10)
        CreateCube("BedroomWall_West", houseRoot.transform, new Vector3(-10, wallHeight / 2, -7), new Vector3(wallThickness, wallHeight, 6));
        
        // East wall of bedroom (X = -4) - WITH A DOORWAY
        // Split East wall into two parts to leave a 1.5m gap for the door
        CreateCube("BedroomWall_East_1", houseRoot.transform, new Vector3(-4, wallHeight / 2, -8.5f), new Vector3(wallThickness, wallHeight, 3f));
        CreateCube("BedroomWall_East_2", houseRoot.transform, new Vector3(-4, wallHeight / 2, -4.75f), new Vector3(wallThickness, wallHeight, 1.5f));
        
        // Doorway gap is between Z = -5.5 and Z = -7.0 (center Z = -6.25, width = 1.5f)

        // 4. Move Bed into Bedroom
        GameObject bed = GameObject.Find("BedBlock");
        if (bed != null)
        {
            bed.transform.position = new Vector3(-8.5f, 0.25f, -8.5f);
            
            // Move player onto bed
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                var fpc = player.GetComponent<FirstPersonController>();
                if (fpc != null)
                {
                    fpc.TeleportTo(new Vector3(-8.5f, 0.55f, -8.5f), Quaternion.identity);
                }
                else
                {
                    player.transform.position = new Vector3(-8.5f, 0.55f, -8.5f);
                }
            }
        }

        // 5. Create Wardrobe
        GameObject wardrobe = CreateCube("WardrobeBlock", houseRoot.transform, new Vector3(-5.5f, 1f, -9.5f), new Vector3(1.5f, 2f, 0.8f));
        wardrobe.layer = LayerMask.NameToLayer("Ground");
        
        // Add Interaction to Wardrobe
        Transform wTrigger = wardrobe.transform.Find("InteractTrigger");
        if (wTrigger == null)
        {
            GameObject tObj = new GameObject("InteractTrigger");
            tObj.transform.SetParent(wardrobe.transform, false);
            tObj.transform.localPosition = Vector3.zero;
            tObj.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            
            var col = tObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            
            var interactable = tObj.AddComponent<SimpleInteractable>();
            interactable.SetPrompt("[E] Mở tủ đồ");
            
            tObj.layer = LayerMask.NameToLayer("Interactable");
        }

        // Clean up old ground if exists to prevent Z-fighting
        GameObject oldGround = GameObject.Find("Ground");
        if (oldGround != null && oldGround != mainFloor)
        {
            oldGround.SetActive(false); // Hide it instead of destroying to be safe
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("House Layout and Bedroom Built Successfully!");
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) return existing;

        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.localScale = scale;
        
        // Remove default collider if we want, but BoxCollider is fine for walls
        return obj;
    }
}
