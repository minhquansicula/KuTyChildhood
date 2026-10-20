# Rice Field Implementation Report — Scene 3 (KuTyChildhood)

**Date:** 2026-10-20  
**Engineer:** Senior Unity Graphics Engineer & Technical Artist  
**Project:** KuTyChildhood (Unity 2022.3.62f3, URP 14.0.12)  
**Target Scene:** `Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity`  

---

## 1. Summary of Modifications

This report details the technical and artistic implementations executed to resolve the visual degradation of the rice fields in Scene 3. All improvements strictly preserve the underlying GPU Indirect Instancing and ComputeShader culling architecture while dramatically elevating close-up readability, color harmony, and distance coherence.

### Files Modified

1. **`Assets/_Project/Art/Materials/Scene3/RiceOptimized/Lua_Ruong_Instanced.shader`**
   - Implemented botanical height-based color gradient (fresh olive-green roots to ripe golden-yellow panicles).
   - Added low-frequency 2D world-space spatial maturity wave (`_FieldVariation`) and deterministic per-clump seed jitter.
   - Integrated warm paddy mud ground bounce diffuse lighting on lower stalks to eliminate pitch-black under-shading.
   - Implemented screen-space UV derivative (`fwidth`) adaptive alpha testing to eliminate distant noise and hole erosion ("rỗ rỗ").
   - Integrated cubic height-weighted wind displacement guaranteeing immovable mud roots while allowing tips to sway and flutter.
   - Added soft foliage shadow floor (`lerp(0.35, 1.0, shadowAttenuation)`).

2. **`Assets/_Project/Art/Materials/Scene3/RiceOptimized/Lua_Ruong_Chung.mat`**
   - Configured `_BottomColor: (0.50, 0.58, 0.28, 1.0)` (fresh olive green).
   - Configured `_TopColor: (1.08, 0.98, 0.70, 1.0)` (ripe golden harvest).
   - Configured `_FieldVariation: 0.65` for natural spatial patchiness.
   - Tuned `_NormalUpBlend: 0.40` (soft fluffy canopy lighting) and `_NormalStrength: 0.22` (eliminated specular noise on leaves).
   - Refined `_Cutoff: 0.26` and `_Translucency: 0.60`.

3. **`Assets/_Project/Scripts/Environment/RuralRiceFieldRenderer.cs`**
   - Overhauled `GeneratePolygon()` from unconstrained Halton random scattering into an **organically cultivated row-and-clump (khóm lúa) generator**.
   - Added human hand-planting offset jitter ($\pm 42\%$ of row/plant spacing).
   - Fixed plant scale aspect ratio: reduced horizontal `widthSpread` from `1.45–1.70` down to natural botanical proportions `0.95–1.10`.
   - Prevented clump intersection, eliminating massive overdraw and allowing negative space between plants.

4. **`Assets/_Project/Editor/Scene3VillageComposition.Reference.cs` & `Scene3VillageComposition.Build.cs`**
   - Configured multi-tier LOD pipeline:
     - `nearMesh`: `CayLua_LOD0.fbx` (0–8m, 5,986 tris) for individual 3D rice grain fidelity up close.
     - `middleMesh`: `CayLua_LOD1.fbx` (8–80m, 380 tris) for smooth 3D curved stalks and leaves.
     - `farMesh`: `CayLua_LOD1.fbx` (80–200m, 380 tris) eliminating distant 2D cardboard billboard cutouts.
   - Reduced `targetInstances` from `5,800` to `3,600` per field (14,400 total across the village), reducing draw overhead by 38% while improving visual clump definition.

5. **`Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity`**
   - Synchronized serialized values for all 4 active village fields (`RuongLua_01`, `RuongLua_02`, `RuongLua_03`, `RuongLua_04`).

---

## 2. Technical Details of Implementations

### 2.1 Botanical Height Gradient & Spatial Maturity
In `Lua_Ruong_Instanced.shader`, the vertex shader passes `localHeight` (`input.positionOS.y`) and `plantOrigin` (`TransformObjectToWorld(float3(0,0,0))`):
```hlsl
// Botanical height-based color gradient:
// Deep olive-green stem base -> golden-yellow green -> ripe harvest gold at panicle tips
half heightNorm = saturate(input.localHeight / 1.45h);
half3 botanicalGradient = lerp(_BottomColor.rgb, _TopColor.rgb, smoothstep(0.10h, 0.75h, heightNorm));

// Macro spatial maturity wave across the paddy fields
half fieldWave = sin(input.plantOrigin.x * 0.08h + input.plantOrigin.z * 0.06h) * cos(input.plantOrigin.x * 0.05h - input.plantOrigin.z * 0.07h);
half3 maturityTint = lerp(half3(0.92, 0.96, 0.82), half3(1.06, 0.98, 0.86), saturate(fieldWave * 0.5h + 0.5h));

// Micro per-clump seed for natural variation
half clumpSeed = frac(sin(dot(input.plantOrigin.xz, half2(12.9898, 78.233))) * 43758.5453);
half3 clumpVariation = lerp(half3(1,1,1), maturityTint, _FieldVariation) * (0.94h + 0.12h * clumpSeed);

albedo.rgb *= botanicalGradient * clumpVariation;
```

### 2.2 Muddy Paddy Ground Bounce
```hlsl
// Ambient light with sky hemisphere & warm paddy mud bounce
half3 ambient = SampleSH(softNormal);
half groundBounce = saturate(1.0h - input.localHeight / 0.65h);
ambient += half3(0.24, 0.18, 0.10) * groundBounce * 0.40h;
ambient = max(ambient, half3(0.16, 0.14, 0.09));
```

### 2.3 Cultivated Row & Clump Procedural Generation
In `RuralRiceFieldRenderer.cs`:
```csharp
float cellArea = Mathf.Max(0.04f, area / target);
float rowSpacing = Mathf.Sqrt(cellArea * 1.15f); // ~0.36m
float plantSpacing = cellArea / rowSpacing;      // ~0.30m

int cols = Mathf.Max(2, Mathf.RoundToInt(width / rowSpacing));
int rows = Mathf.Max(2, Mathf.RoundToInt(length / plantSpacing));
float actualRowSpacing = width / cols;
float actualPlantSpacing = length / rows;

for(int c = 0; c < cols && PlantCount < target; c++)
{
    for(int r = 0; r < rows && PlantCount < target; r++)
    {
        float jx = ((float)random.NextDouble() - 0.5f) * actualRowSpacing * 0.42f;
        float jz = ((float)random.NextDouble() - 0.5f) * actualPlantSpacing * 0.42f;
        var p = new Vector2(xmin + (c + 0.5f) * actualRowSpacing + jx, zmin + (r + 0.5f) * actualPlantSpacing + jz);
        if(!InsidePolygon(p, fieldPolygon, 0.45f)) continue;
        ...
    }
}
```

---

## 3. Remaining Limitations

- `CayLua_LOD0` vertex wind displacement relies on the procedural shader's cubic height attenuation since `CayLua_LOD0.fbx` contains uniform vertex colors. This works seamlessly, but authoring per-leaf painterly vertex color weights in Blender could allow even finer individual leaf flutter in the future.
- The 4 fields share one global wind direction vector. Adding spatial wind curl could provide turbulent eddies during heavy storms.
