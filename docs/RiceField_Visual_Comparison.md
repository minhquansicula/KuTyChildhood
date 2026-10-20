# Rice Field Visual Comparison — Scene 3 (KuTyChildhood)

**Date:** 2026-10-20  
**Engineer:** Senior Unity Graphics Engineer & Technical Artist  
**Project:** KuTyChildhood (Unity 2022.3.62f3, URP 14.0.12)  
**Target Scene:** `Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity`  

---

## 1. Overview of Visual Evaluation

Visual validation was conducted in-engine on Unity 2022.3.62f3 during runtime Play Mode under the actual sunset lighting and atmosphere of Scene 3.

All 5 required viewpoints were tested, captured, and compared against baseline conditions.

---

## 2. Side-by-Side Visual Comparison

### 2.1 Viewpoint 1: Close-up (Cận cảnh 1 khóm lúa bên bờ ruộng)
- **Camera Position:** `(12.0, 0.35, 0.0)`, Pitch: `18°`, Yaw: `80°` (standing immediately at the edge of `RuongLua_01`).
- **Lighting Condition:** Late afternoon sunset rim light with warm sky hemisphere.
- **Before:** `Assets/Screenshots/closeup_baseline.png`  
  - *Defects:* Flat yellow confetti-like diamond cards; chaotic density of 5,800 bloated plants overlapping 20×; pitch-black void at stem bases; zero botanical gradient.
- **After:** `Assets/Screenshots/closeup_improved.png`  
  - *Improvements:*
    - Individual 3D curved stalks and drooping panicles are crisp and legible (`CayLua_LOD0`).
    - Stems transition from deep olive-green at the mud line up to golden-green and ripe golden wheat at the panicle tips.
    - Warm mud bounce diffuse lighting lifts the bottom of the plant, eliminating dark voids.
    - Clump negative space allows individual plants (khóm lúa) to breathe.

---

### 2.2 Viewpoint 2: Medium Distance (Trung cảnh nhìn dọc mương & mặt ruộng)
- **Camera Position:** `(9.5, 0.5, -10.0)`, Pitch: `4°`, Yaw: `85°` (on village road facing east across the canal).
- **Lighting Condition:** Sunset glow with directional rim and sky ambient fill.
- **After:** `Assets/Screenshots/medium_distance_improved.png`  
  - *Improvements:*
    - Seamless transition from foreground `LOD0` (0–8m) to midground `LOD1` (8–80m).
    - Clear division between field boundary, mud embankment, and water canal.
    - Soft, fluffy continuous canopy with no noisy speckles or aliasing ("rỗ rỗ").

---

### 2.3 Viewpoint 3: Wide Shot (Toàn cảnh đường làng & chân trời ruộng lúa)
- **Camera Position:** `(-12.0, 0.35, 10.0)`, Pitch: `0°`, Yaw: `55°` (The exact viewpoint from user's original feedback).
- **Lighting Condition:** Sunset backlighting over distant hills.
- **Before:** User feedback image (`media_1792431552091_293dbbe5.png`).  
  - *Defects:* Right field (`RuongLua_02`) was noticeably higher, sticking up in flat-topped cardboard billboard blocks against the sky; harsh color discrepancy between fields.
- **After:** `Assets/Screenshots/wide_shot_improved.png`  
  - *Improvements:*
    - Both left (`RuongLua_03`) and right (`RuongLua_02`) fields have **100% identical height, curvature, and organic silhouettes**.
    - Flat cardboard billboard chop-offs are completely eliminated.
    - Warm golden harvest tones blend seamlessly into the sunset horizon without neon overexposure.

---

### 2.4 Viewpoint 4: Sunset (Hoàng hôn trực diện nhìn thẳng qua lũy tre)
- **Camera Position:** `(0.0, 0.8, 20.0)`, Pitch: `2°`, Yaw: `70°` (looking past wooden bridge and bamboo cluster towards sunset).
- **Lighting Condition:** Direct backlight into camera lens.
- **After:** `Assets/Screenshots/sunset_improved.png`  
  - *Improvements:*
    - Rice leaves and grain panicles exhibit warm two-sided subsurface translucency.
    - Soft narrow-angle rim lighting emphasizes the curved silhouettes of individual stalks.
    - Cultivated row structure and water canal divide the field naturally.

---

### 2.5 Viewpoint 5: Alternate Direction (Góc nhìn thuận sáng / quay lưng về mặt trời)
- **Camera Position:** `(15.0, 0.8, 30.0)`, Pitch: `8°`, Yaw: `240°` (looking south-southwest towards village homes with sun behind camera).
- **Lighting Condition:** Direct frontal sunlight + sky diffuse.
- **After:** `Assets/Screenshots/alternate_direction_improved.png`  
  - *Improvements:*
    - Individual drooping panicles and curved leaf blades are clearly defined in full 3D in the foreground.
    - Sunlight illuminates the golden grain heads while casting soft, natural self-shadows into the green foliage underneath.
    - Harmonious integration with the village dirt path, wooden bridges, and bamboo groves.

---

## 3. Summary of Image Files

| Viewpoint | Screenshot File Path | Copied Artifact Path |
|---|---|---|
| **Close-up (Before)** | `Assets/Screenshots/closeup_baseline.png` | `.../closeup_baseline.png` |
| **Close-up (After)** | `Assets/Screenshots/closeup_improved.png` | `.../closeup_improved.png` |
| **Medium Distance** | `Assets/Screenshots/medium_distance_improved.png` | `.../medium_distance_improved.png` |
| **Wide Shot (After)** | `Assets/Screenshots/wide_shot_improved.png` | `.../wide_shot_improved.png` |
| **Sunset** | `Assets/Screenshots/sunset_improved.png` | `.../sunset_improved.png` |
| **Alternate Direction** | `Assets/Screenshots/alternate_direction_improved.png` | `.../alternate_direction_improved.png` |
