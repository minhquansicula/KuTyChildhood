# Scene 3 reference backdrop

21 October 2026. Replaces the repeated golf-course sky visible in the user's first screenshot with the distant atmosphere of the second reference: peach/gold clouds, blue-violet sky, layered mountains and tropical greenery.

## Assets and behaviour

- Texture: `Assets/_Project/Art/Textures/Scene3/HDRI/VillageGoldenAfternoon.png`, 1774 × 887, generated with the built-in imagegen tool using the second screenshot as a style reference. This is an LDR panorama, not an HDR capture. sRGB, mipmaps, trilinear filtering, U repeat, V clamp, no CPU read/write.
- Material: `Assets/_Project/Art/Materials/Scene3/VillageGoldenAfternoon.mat`.
- Shader: `Assets/_Project/Art/Materials/Scene3/VillageReferenceBackdrop.shader`. Maps one full panorama around 360°, without the old fivefold landscape repetition. Corrects longitude mip gradients, softens the edge join and converges to a uniform zenith colour. One procedural sun follows the existing directional light; the generated image contains no solar disc.
- Installer/captures: `Assets/_Project/Editor/Scene3VillageComposition.ReferenceBackdrop.cs`. Menus **KuTy > Scene 3 > Art direction > 5 Apply reference afternoon backdrop** and **6 Capture reference backdrop comparison**.

Scene geometry, sun transform/colour/intensity, fog, pipeline, vegetation, water, colliders and all 45 gameplay script payloads were preserved during installation. Checkpoint values were verified unchanged. Changing the sky updates its ambient probe, so the environment can receive different ambient colours. The original HDRI and materials remain available.

Backup and before/after evidence: `Tools/Scene3/VillageComposition/ReferenceBackdrop/`. `before.unity` is a scene copy made immediately before the backdrop replacement; restoring it also restores other scene state at that point. Comparison images cover the entrance, marble yard, village end, sky towards/away from the sun, zenith and longitude join. The source texture has modest resolution; close sky views are intentionally softer than foreground geometry.

## Generation prompt

Built-in imagegen, one image, opaque background, second user screenshot supplied as a reference:

> Create a production game skybox texture, use-case stylized-concept. The attached reference is ONLY a mood/style reference for its distant background sky, mountains, palms and greenery. Generate a NEW 360 degree seamless equirectangular panorama with 2:1 aspect ratio, ideally 4096x2048. Full longitude 360 degrees, latitude +90 at top to -90 at bottom, geometric horizon at image midline (50% height). A beautiful rural Vietnamese tropical golden afternoon, soft blue-lavender upper sky with fluffy peach pink and pale golden cumulus clouds with luminous rim light, gentle sunlit haze, layered distant blue-purple mountains, a low distant band of lush green broadleaf tree canopies and occasional coconut palms. All mountain peaks and tree silhouettes confined to the narrow band from 40% to 55% of image height, modest atmospheric scale, no huge foreground trees. Bottom half subdued hazy green fields gradually fading to an unobtrusive warm green nadir, no near objects. Upper quarter uncluttered blue-lavender sky, all zenith pixels consistent without radial artifacts. Warm bright sky but preserve cloud detail, no white blown out areas. Cinematic soft stylized 3D game art, rich greens and gentle pastel sky, match reference mood. IMPORTANT: NO sun disc anywhere, no concentrated bright solar hot spot or glare; sunlight will be rendered by the game as exactly one sun. Left/right edges MUST join seamlessly in cloud shapes and terrain height/color. No image repetition within the panorama. No houses, gate, people, children, roads, fences, poles, wires, text, watermarks, black borders, UI or close foreground. Do not reproduce the reference village scene; only create its distant environmental sky and horizon as a usable full spherical skybox.

The generated native result is 1774 × 887 despite the requested ideal resolution; no artificial upscale was applied.
