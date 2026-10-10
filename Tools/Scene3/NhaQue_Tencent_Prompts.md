# 5 prompt nhà quê Việt Nam cho Tencent Hunyuan 3D

Tạo từng căn riêng bằng Text-to-3D. Các prompt là model ngoại thất để đặt vào làng ở scene 3, dùng tông chất liệu cũ của nhà scene 2 và tỷ lệ gần thực tế. Kết quả AI vẫn cần kiểm tra mesh và material trước khi dùng trong Unity.

## 1. Nhà ba gian, hiên tôn phía trước

```text
One standalone single-storey Vietnamese countryside house from the 1990s, approximately 8 by 6 metres. Three-bay rectangular facade, straight pitched roof of aged reddish terracotta tiles, faded ivory lime-plaster walls, dark wooden double doors and two wooden shutter windows. A shallow weathered grey corrugated-metal awning covers the front veranda, supported by three slender wooden posts. Small plaster chips reveal brick near the base; subtle moss beneath the eaves. Well-used, inhabited and structurally sound. Stylized 3D game asset, simple geometry, softly painted textures, natural proportions, muted earthy colours, matte rough surfaces. Complete exterior, flat foundation, isolated house only, neutral lighting, shadow-free diffuse textures. Tile details mainly in texture.
```

## 2. Nhà chữ L, mái che tôn bên hông

```text
One standalone single-storey Vietnamese countryside house from the 1990s, approximately 7 by 7 metres, with a compact L-shaped footprint. Two connected wings have simple straight pitched roofs of weathered terracotta tiles. Faded cream lime-plaster walls, dark brown wooden doors, muted sage-green wooden shutters. A low dull-grey corrugated-metal awning on simple wooden posts shelters the inner corner and side entrance. Slight rust along the awning edges, worn plaster and a small exposed brick patch. Modest, inhabited and structurally sound. Stylized 3D game asset, simple geometry, softly painted textures, natural proportions, earthy colours and matte surfaces. Complete exterior, flat foundation, isolated house only, neutral lighting, shadow-free diffuse textures.
```

## 3. Nhà gạch cũ, hiên hẹp

```text
One standalone single-storey rural Vietnamese brick house from the 1990s, approximately 7 by 5 metres. Rectangular body, straight pitched roof of dark weathered red terracotta tiles. Muted orange-brown brick walls with irregular faded cream plaster patches, plain timber double door and two small wooden shutter windows. A narrow front veranda is sheltered by overlapping dull-grey corrugated-metal sheets on two thin timber posts. Light rust, softened brick colour and subtle age marks; maintained and inhabited. Stylized 3D game asset, simplified geometry, softly painted textures, natural building proportions, muted earthy palette and matte materials. Complete exterior on all sides, flat foundation, isolated house only, neutral lighting, shadow-free diffuse textures. Brick and tile details mainly in texture.
```

## 4. Nhà ngang dài, có gian bếp bên hông

```text
One standalone single-storey Vietnamese countryside family house from the 1990s, approximately 9 by 6 metres. Long rectangular main house with an aged red terracotta pitched roof and straight ridge, faded pale-yellow plaster, three front openings with dark wooden doors and shutters. A small attached kitchen wing on the right has a matching tiled roof. A patched dull-grey corrugated-metal canopy covers the side service porch, supported by plain timber posts. Worn plaster near the ground, restrained rust and slight moss under the eaves. Inhabited and structurally sound. Stylized game asset, simple geometry, softly painted textures, natural proportions, muted earthy colours, matte materials. Complete exterior, flat foundation, isolated house only, neutral lighting, shadow-free diffuse textures.
```

## 5. Nhà nhỏ màu vôi xanh nhạt, mái tôn vá

```text
One standalone small single-storey Vietnamese countryside house from the 1990s, approximately 6 by 5 metres. Compact rectangular body, aged red-brown terracotta pitched roof with a straight ridge. Faded pale grey-green limewashed walls, ivory window frames, dark wooden double door, two simple wooden shutter windows. A slightly uneven front porch canopy made of three overlapping weathered corrugated-metal sheets, grey with restrained rust-brown patches, supported by thin timber posts. Chipped paint and faint water stains give a gently aged, inhabited appearance. Stylized 3D game asset, simple geometry, softly painted textures, natural proportions, muted earthy palette, rough matte materials. Complete exterior, flat foundation, isolated house only, neutral lighting, shadow-free diffuse textures. Tile details mainly in texture.
```

Hướng dẫn prompt chính thức: https://intl.cloud.tencent.com/document/product/1284/75290

Nếu có chế độ giảm mặt/retopology, chọn sau khi duyệt hình dáng. Mục tiêu tham khảo: 8.000–15.000 tam giác cho một căn nhà ngoại thất, texture 1K–2K; kiểm tra số thực tế sau xuất. Ưu tiên thể hiện ngói/gạch/vết cũ bằng texture. Không gộp cây, sân và vật trang trí vào nhà để dễ bố trí và culling.

