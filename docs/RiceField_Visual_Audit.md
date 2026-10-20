# Rice Field Visual Audit — Scene 3 (KuTyChildhood)

**Date:** 2026-10-20  
**Engineer:** Senior Unity Graphics Engineer & Technical Artist  
**Project:** KuTyChildhood (Unity 2022.3.62f3, URP 14.0.12)  
**Target Scene:** `Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity`  

---

## 1. Executive Summary

This audit evaluates the visual quality, rendering architecture, performance, and procedural distribution of the rice fields in Scene 3 (`Act3_MemoryWorld_OutSide`). The existing rice system uses a custom GPU Indirect Instancing architecture (`RuralRiceFieldRenderer.cs` and `VillageRiceRegions.cs`) backed by ComputeShader frustum/distance culling (`RiceRegionCull.compute`).

While the GPU instancing architecture is technically sound and high-performance, the visual presentation suffers from severe artistic and procedural flaws:
1. **Massive Overdraw & Clump Stacking**: Plants are generated at ~9.7 plants/m² with artificial 1.6× horizontal stretching, resulting in 15–25 overlapping plants per point. This obliterates individual plant silhouettes, creating a chaotic carpet of yellow fragments and starving the base of light (pitch-black bottoms).
2. **Monolithic Mustard-Yellow Coloration**: All 23,200 plants share a single static material tint with no vertical gradient (soil to grain) and no spatial maturity variation across the field.
3. **LOD Threshold & Geometry Mismatch**: A low `middleDistance` threshold (28m) previously forced entire fields into 48-triangle 2D billboard cross cards (`CayLua_LOD2`), creating flat-topped cardboard cutouts against the sky.
4. **Shader Cutoff & Lighting Deficiencies**: Alpha cutout without screen-space derivative compensation caused thin leaf edges to disintegrate into noisy speckles ("rỗ rỗ") at distance, while lack of ground bounce lighting created harsh black voids near the soil.

---

## 2. Technical Architecture & File Inventory

| Asset / File | Role | Status / Issue |
|---|---|---|
| `Assets/_Project/Art/Models/Scene3/RiceOptimized/CayLua_LOD0.fbx` | High-detail 3D rice mesh (16,458 verts, 5,986 tris) | Previously disabled due to full-field CPU/GPU weight; ideal for close-up (0–8m) |
| `Assets/_Project/Art/Models/Scene3/RiceOptimized/CayLua_LOD1.fbx` | Optimized 3D rice mesh (662 verts, 380 tris) | Good 3D leaf curvature and stem geometry; excellent for mid-distance (8–80m) |
| `Assets/_Project/Art/Models/Scene3/RiceOptimized/CayLua_LOD2.fbx` | 2D Cross-card billboard mesh (88 verts, 48 tris) | Flat-topped card silhouettes; unsuitable at <80m |
| `Assets/_Project/Art/Materials/Scene3/RiceOptimized/Lua_Ruong_Instanced.shader` | URP Procedural Instanced HLSL Shader | Lacked vertical gradient, ground bounce, and spatial hue variation |
| `Assets/_Project/Art/Materials/Scene3/RiceOptimized/Lua_Ruong_Chung.mat` | Shared rice field material | Uniform yellow color, needed normal/cutoff fine-tuning |
| `Assets/_Project/Art/Textures/Scene3/RiceOptimized/CayLua_Atlas_BaseColor.png` | 2048×2048 RGBA Atlas | Good resolution, clamp mode active, mipmaps preserve coverage enabled |
| `Assets/_Project/Art/Textures/Scene3/RiceOptimized/CayLua_Atlas_Normal.png` | 2048×2048 Normal Atlas | Trilinear filtering, aniso 16 |
| `Assets/_Project/Scripts/Environment/RuralRiceFieldRenderer.cs` | CPU Instance & Polygon Generator | Halton distribution without Poisson spacing; 1.6× horizontal scale bloating |
| `Assets/_Project/Scripts/Environment/VillageRiceRegions.cs` | Indirect GPU Dispatcher | StructuredBuffer instance layout, DrawMeshInstancedIndirect |
| `Assets/_Project/Art/Materials/Scene3/RiceOptimized/RiceRegionCull.compute` | GPU Frustum & Distance Culling | 64 threads/group, appends to Near/Middle/Far visible buffers |

---

## 3. Confirmed Problems & Root Cause Analysis

### 3.1 Overlapping Yellow Fragments & Chaotic Density
- **Evidence**: Close-up inspection (`closeup_baseline.png`) revealed an impenetrable, featureless mass of grain cards where individual rice plants cannot be distinguished.
- **Root Cause**: In `RuralRiceFieldRenderer.GeneratePolygon()`, `targetInstances = 5800` across a 600m² field polygon. Crucially:
  ```csharp
  float widthSpread = 1.45f + (float)random.NextDouble() * .25f;
  var plantScale3D = new Vector3(scale * widthSpread, scale * 1.15f, scale * widthSpread);
  ```
  The mesh's horizontal extents (originally ~1.9m) are multiplied by ~1.6×, expanding each plant to a 1.4m diameter footprint. Placed with Halton quasi-random points without minimum clearance, 15 to 25 instances intersect at every ground coordinate.
- **Impact**: Zero negative space, severe overdraw, and complete loss of clump (khóm lúa) readability.

### 3.2 Pitch-Black Plant Bases
- **Evidence**: The bottom 20% of the rice canopy appears near-black even under bright afternoon sunlight.
- **Root Cause**: 
  1. Massive geometric occlusion prevents sunlight and ambient SH probes from reaching the ground level.
  2. The shader lacked ground ambient bounce lighting (`indirect diffuse`) reflected from the wet paddy mud.
  3. No vertical gradient exists to lift the darkness of the lower stems.

### 3.3 Artificial Coloration & Lack of Maturity Gradients
- **Evidence**: Uniform mustard/orange tone across all plants with no variation across distance or field regions.
- **Root Cause**:
  1. No height-based color transition in `Lua_Ruong_Instanced.shader` — stems, leaves, and panicles receive the same flat color multiplication.
  2. No low-frequency world-space noise to simulate wetter/greener depressions and sun-ripened golden knolls.
  3. Instance variation was limited to a 96%–104% scalar brightness jitter (`frac(sin(...))`).

### 3.4 Flat-Topped Cardboard Silhouette in Distant Fields
- **Evidence**: User screenshot `media_1792431552091_293dbbe5.png` showed `RuongLua_02` sticking up in rectangular blocks compared to `RuongLua_03`.
- **Root Cause**: `middleDistance` was set to `28m`. Because `RuongLua_02` is situated 38–50m from the path, 100% of its plants switched to `CayLua_LOD2` (cross billboard quads). At 40m, the flat horizontal quad boundaries cut sharply against the sky.

---

## 4. Performance Baseline (In-Engine)

Captured on Unity 2022.3.62f3 running Play Mode at `Act3_MemoryWorld_OutSide`:

| Metric | Measured Baseline | Target Budget | Assessment |
|---|---|---|---|
| **Render Thread Time** | 4.27 ms | < 8.0 ms (120 FPS) | Excellent |
| **Total Frame Time** | 53.5 ms (Editor overhead active) | < 16.6 ms (Stand-alone 60 FPS) | Within GPU budget |
| **Total Batches / Draw Calls** | 41 batches | < 100 batches | Highly optimized |
| **Rice Field Draw Calls** | 9 indirect draw calls (3 per visible region) | <= 12 calls | Fully indirect |
| **Active Rice Regions** | 3 visible out of 4 | 4 total | Culling operational |
| **Total Rice Plant Instances** | 23,200 (5,800 × 4 fields) | 14,000 – 16,000 | Can be reduced for higher visual clarity |
| **Triangles (Scene View Stats)** | 255,777 (excluding indirect buffer) | < 2,000,000 | Plentiful GPU headroom |

---

## 5. Proposed Action Plan

1. **Phase 2 & 4: Procedural Distribution Refinement**:
   - Introduce **cultivated row and clump (khóm lúa) distribution** with organic hand-planting jitter.
   - Restore natural clump proportions: reduce `widthSpread` from `1.45–1.70` down to `0.92–1.08`.
   - Tune target instances per field from `5,800` to `~3,600` well-spaced clumps. This gives each clump distinct silhouette definition and breathing room while keeping the field dense and continuous when viewed at player eye level.
2. **Phase 3: Shader Lighting & Color Overhaul**:
   - Add **Height-Based Color Gradient**: olive-green root base $\rightarrow$ golden-green leaf stalks $\rightarrow$ warm ripe golden panicles.
   - Add **Spatial Maturity Variation**: low-frequency 2D world-space noise for natural field patchiness (greener moist hollows vs. golden ripe knolls).
   - Add **Mud Ground Bounce Lighting**: sample wet soil color to illuminate the base of stems.
   - Maintain anti-aliased adaptive alpha cutoff and soft shadow attenuation.
3. **Phase 5: Seamless LOD Pipeline**:
   - `nearMesh`: `CayLua_LOD0` (0–8m) for AAA close-up individual 3D rice grains.
   - `middleMesh`: `CayLua_LOD1` (8–85m) for lightweight, smooth 3D curved stalks and leaves.
   - `farMesh`: `CayLua_LOD1` (85–200m) to preserve organic silhouettes against the horizon.
4. **Phase 6: Visual & In-Engine Validation**:
   - Capture side-by-side screenshots from 5 mandatory viewpoints (Close-up, Mid-distance, Wide shot, Sunset, Alternate angle).
