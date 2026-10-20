# Lúa tối ưu: model đã chuẩn bị, phần đồ họa bạn tự làm trong Unity

Bộ này được làm từ `BLENDER/caylua_nhe.blend` của bạn: giữ hình dáng cây lúa vàng, dùng ba mức chi tiết để nhìn gần có hạt lúa, nhìn xa giảm hình học. Đây là cách xây dựng ruộng có chuyển động gió theo hướng hình ảnh bạn thích ở Sekiro; không phải shader gốc của Sekiro và không phải bản sao loài cỏ trắng trong ảnh.

## 1. Những file đã có

| File | Dùng lúc nào | Tam giác/cây |
| --- | --- | ---: |
| `Assets/_Project/Art/Models/Scene3/RiceOptimized/CayLua_LOD0.fbx` | Rất gần camera | 5.986 |
| `Assets/_Project/Art/Models/Scene3/RiceOptimized/CayLua_LOD1.fbx` | Khoảng cách trung bình | 380 |
| `Assets/_Project/Art/Models/Scene3/RiceOptimized/CayLua_LOD2.fbx` | Xa, cây nhỏ trên màn hình | 48 |

- `Assets/_Project/Art/Textures/Scene3/RiceOptimized/CayLua_Atlas_BaseColor.png`: ảnh màu có alpha, 2048×2048, dùng chung cả ba model.
- `Assets/_Project/Art/Textures/Scene3/RiceOptimized/CayLua_Atlas_Normal.png`: normal map để bổ sung ánh sáng trên bông ở các bản dùng ảnh.
- `D:/FPT-ki7/PRU/Project_PRu/BLENDER/CayLua_ToiUu.blend`: nguồn Blender có ba collection LOD0/LOD1/LOD2, texture đã đóng gói. Mặc định chỉ hiện LOD0; bật từng collection để xem riêng.

Mỗi FBX có một mesh, một slot material, gốc cây ở (0,0,0), chiều cao gốc khoảng 2 mét. Khi đặt vào game, giảm **Scale đồng đều X/Y/Z** để phù hợp lúa trong map. Không dùng lại hệ số 80 và phép xoay -90° của model cũ.

Mesh có sẵn `Vertex Color`: R = độ cao từ gốc đến ngọn; G = mức rung chi tiết. UV0 dùng cho texture, UV1 lưu dự phòng cùng dữ liệu gió. Không bật Generate Lightmap UVs nếu muốn giữ dữ liệu dự phòng UV1. Phần gió bên dưới đọc Vertex Color, không đọc UV1.

Ảnh `CayLua_LOD_Comparison.png`: trái LOD0, giữa LOD1, phải LOD2. Đây là render kiểm tra model trong Blender; ánh sáng trong Unity còn phụ thuộc shader và scene của bạn.

Đã kiểm tra nhập cả ba FBX bằng Unity **2022.3.62f3** trong project tạm: đúng số tam giác, một submesh/material, trục +Y, gốc tại mặt đất, rotation/scale mặc định, đủ UV0/UV1 và Vertex Color.R chạy từ 0 đến 1. Kết quả trong `unity_import_report.json`. Scene game và shader gió chưa được chạy thử; bài hướng dẫn dưới đây là phần bạn sẽ thực hiện.

## 2. Nhập texture và tạo shader màu

Làm thử trong một scene mới trước khi thay cả ruộng.

1. Trong Unity mở thư mục `Assets/_Project/Art/Textures/Scene3/RiceOptimized`.
2. Bấm **một lần** vào `CayLua_Atlas_BaseColor`. Trong Inspector đặt Texture Type = Default, bật sRGB, Alpha Source = Input Texture Alpha, Max Size = 2048. Bật Generate Mip Maps, Filter Mode = Trilinear, Aniso Level = 4, Wrap Mode = Clamp. Bấm Apply. Nếu thấy tùy chọn Mip Maps Preserve Coverage, có thể bật và đặt Alpha Cutoff = 0.3 để giữ bông khi xa.
3. Chọn `CayLua_Atlas_Normal`, đặt Texture Type = Normal map. Đây đã là ảnh normal, không tạo lại từ grayscale. Bật mipmaps, Wrap Mode = Clamp, Max Size = 2048, Apply.
4. Trong thư mục material của bạn, chuột phải → Create → Shader Graph → URP → Lit Shader Graph, tên `Lua_Gio_ToiUu`. Mở graph.
5. Trong Graph Inspector → Graph Settings → Universal/Lit đặt Surface Type = **Opaque**, Render Face = **Both**, bật **Alpha Clipping**. Đây là cách bỏ nền trong suốt của ảnh bông lúa.
6. Blackboard → dấu `+` → Texture2D, đặt tên `LuaAtlas`, bật Exposed trong Property Inspector. Kéo property từ Blackboard vào graph.
7. Tạo node **Sample Texture 2D**, để Type = Default; nối `LuaAtlas.Out → Sample.Texture`. Để UV mặc định UV0.
8. Nối `Sample.RGB → Fragment.Base Color`, `Sample.A → Fragment.Alpha`. Đặt Fragment.Alpha Clip Threshold = **0.3**, Metallic = **0**, Smoothness = **0.15**.
9. Bấm Save Asset. Về Project, chuột phải shader graph → Create → Material, tên `Lua_Chung`. Trong material gán ảnh `CayLua_Atlas_BaseColor` vào ô `LuaAtlas` bằng kéo thả hoặc nút tròn chọn file.

Để thêm normal: tạo property Texture2D `LuaNormal`, một Sample Texture 2D riêng đặt **Type = Normal**, Space = Tangent. Nối property vào Texture, Sample.RGB → Normal Strength.In; Strength = 0.7; Out → Fragment.Normal (Tangent Space). Save Asset và gán `CayLua_Atlas_Normal` trong material. Có thể hoàn thiện bước này sau khi màu và alpha đúng.

Không nối Vertex Color vào Base Color: dữ liệu màu đỉnh trong bộ này dùng để điều khiển gió.

## 3. Ráp một cây và thiết lập LOD

1. Hierarchy → Create Empty, tên `CayLua_ToiUu`; đặt Rotation = (0,0,0), Scale = (1,1,1).
2. Kéo ba FBX vào làm con của Empty này. Cả ba phải có cùng Local Position = (0,0,0), Rotation = (0,0,0), Scale = (1,1,1).
3. Mở từng con có Mesh Renderer và gán **cùng material `Lua_Chung`** vào Element 0. Xóa collider nếu có; không cần collider trên mỗi cây để làm gió.
4. Chọn Empty cha → Add Component → **LOD Group**. Dùng ba mức LOD0/LOD1/LOD2; tại từng mức bấm Add và kéo đúng Mesh Renderer tương ứng vào. Không để một renderer nằm trong hai mức.
5. Giá trị chuyển thử: LOD0 xuống LOD1 ở **40%**, LOD1 xuống LOD2 ở **12%**, LOD2 sang Culled ở **2%**. Đây là tỷ lệ chiều cao trên màn hình, không phải khoảng cách mét. Bấm Recalculate Bounds sau khi thêm đủ renderer. Ban đầu Fade Mode = None.
6. Kéo thanh camera xem trước trong LOD Group để kiểm tra mỗi lần chỉ một mức xuất hiện. Nếu thấy ba cây đè lên nhau, renderer chưa được gán đúng.
7. Tạo prefab bằng cách kéo Empty cha vào Project. Khi cần đổi chiều cao cây, chỉnh Scale của cha đồng đều; dùng prefab này để thử vài chục cây trước.

Nếu bông lúa đổi hình quá rõ lúc đi xa, kéo mốc 12% thấp hơn để giữ LOD1 lâu hơn. LOD2 là ảnh trên các mặt phẳng giao nhau, không dùng để soi gần. Bản LOD0 vẫn có gần 6.000 tam giác nên cũng không phủ cả ruộng bằng mức này.

## 4. Làm gió trong Shader Graph: đầy đủ dây cần nối

Mở lại `Lua_Gio_ToiUu`. Các node này nối vào **Vertex.Position**, còn nhánh texture ở Fragment giữ như trên.

Tạo node bằng Space hoặc chuột phải → Create Node, gõ tên. Để nhập một số vào cổng, bấm ô số khi cổng chưa có dây. Nếu cần hai hoặc ba số, tạo node Vector 2/Vector 3 riêng rồi nối Out vào cổng; không ép một cổng đang có dây trở thành ô số.

### A. Lấy vị trí trên mặt đất và độ uốn của ngọn

1. Tạo **Position**, Space = World; nối Out vào **Split**.
2. Tạo **Combine**: Split.R → Combine.R, Split.B → Combine.G. Dùng đầu ra **RG** của Combine; đây là tọa độ X/Z trên mặt đất.
3. Tạo **Vertex Color** và một **Split** khác; Vertex Color.Out → Split.In.
4. Tạo **Multiply** tên dễ nhớ là `DoUon`: Split màu.R vào **cả A và B**. Kết quả là R²: gốc bằng 0, ngọn uốn nhiều, không khiến cả cây trượt khỏi đất.

### B. Tạo sóng gió di chuyển qua ruộng

1. Tạo **Vector 2** = (0.8, 0.6). Tạo **Dot Product**: Combine.RG → A, Vector2.Out → B.
2. Tạo **Multiply** `TanSo`: Dot Product.Out → A, B = **0.6**.
3. Tạo **Time**, lấy cổng **Time** → Multiply `TocDo`.A, B = **0.9**.
4. Tạo **Add**: TanSo.Out → A, TocDo.Out → B. Add.Out → **Sine.In**.
5. Tạo **Multiply** `BienDo`: Sine.Out → A, B = **0.12**. Đây là độ lệch tối đa tính theo mét trong world space.
6. Tạo **Multiply** `GioTheoNgon`: BienDo.Out → A, DoUon.Out → B.
7. Tạo **Vector 3** = (0.8, 0, 0.6), rồi Multiply `DoLech`: Vector3.Out → A, GioTheoNgon.Out → B.
8. Tạo **Add** `ViTriMoi`: Position(World).Out → A, DoLech.Out → B.
9. Tạo **Transform**, chọn **Type = Position, From = World, To = Object**. ViTriMoi.Out → Transform.In; Transform.Out → **Vertex.Position**.
10. Bấm Save Asset. Trong Game khi Play, ngọn lúa sẽ đung đưa còn gốc đứng yên.

Sơ đồ nhánh cuối:

```text
Sine × 0.12 × (VertexColor.R²) × Vector3(0.8,0,0.6)
                          ↓ độ lệch world
Position(World) → Add → Transform(World → Object, Position) → Vertex.Position
```

Transform phải đổi **vị trí** về Object Space vì Vertex.Position nhận tọa độ Object Space. Không chọn Direction cho nhánh này. Tài liệu: [Transform Node](https://docs.unity.cn/Packages/com.unity.shadergraph%4014.0/manual/Transform-Node.html), [các cổng Vertex/Fragment](https://docs.unity.cn/Packages/com.unity.shadergraph%4010.8/manual/Built-In-Blocks.html).

Nếu cây sau khi giảm Scale rung quá mạnh, giảm 0.12 xuống 0.04–0.08. Nếu toàn cây dịch chuyển, kiểm tra dây DoUon và Vertex Color.R. Nếu cây biến mất hoặc bay khỏi scene, kiểm tra đang cộng vị trí World rồi Transform về Object trước khi nối Vertex.Position.

### C. Thêm từng đợt gió mạnh/yếu

Sau khi nhánh B hoạt động:

1. Time.Time → Multiply.A; nối một Vector2(-0.08,-0.06) vào B.
2. Combine.RG → Add.A, Multiply trên → Add.B.
3. Add.Out → **Gradient Noise.UV**, Scale = **0.2**.
4. Gradient Noise.Out → Multiply.A, B = **0.6**; kết quả → Add.A, B = **0.4**. Nhánh này tạo độ mạnh khoảng 0.4–1.
5. Tạo Multiply `GioCoDot`: GioTheoNgon.Out → A, kết quả Add trên → B. **Thay dây** GioTheoNgon.Out → DoLech.B bằng GioCoDot.Out → DoLech.B.

Dùng tọa độ World giúp những cây gần nhau chịu cùng đợt gió, các đợt chạy qua cả ruộng thay vì từng cây lắc độc lập.

### D. Thêm rung nhỏ ở lá và bông (tùy chọn)

Lấy lại Dot Product.Out: nhân **3**, cộng Time.Time nhân **3.2**, đưa qua Sine rồi nhân **0.015**. Nhân tiếp với **Split màu.G** và **DoUon.Out**. Cộng kết quả này với GioCoDot.Out trước DoLech.B. Giảm 0.015 nếu hạt lúa rung nhanh hoặc nhấp nháy.

Không cần Animator, xương hay Rigidbody trên từng cây cho các chuyển động này. Đây là chuyển động hình ảnh của mesh; nếu cần nhân vật đẩy lúa rồi lúa bật lại, phải thêm dữ liệu tương tác vào shader hoặc hệ thống điều khiển riêng.

## 5. Cách đưa vào cả ruộng để tránh quay lại tình trạng lag

Model nhẹ giải quyết lượng hình học; số GameObject/renderer, bóng và pixel chồng nhau vẫn cần xử lý.

- **Một material dùng chung**: không tạo material mới cho từng cây, không gọi `renderer.material` để đổi màu hàng nghìn cây. Ngẫu nhiên yaw và scale nhẹ khi phân bố để giảm cảm giác lặp.
- **Gió chạy trong shader**: không tạo một Update/Animator/Rigidbody cho mỗi cây. Không bật Static Batching cho lúa cần gió theo vị trí/đỉnh riêng.
- **Bóng có giới hạn**: lúc thử hiệu năng, tắt Cast Shadows ở LOD1/LOD2, giữ bóng LOD0 nếu cần. Sau đó đánh giá phần ruộng có quá phẳng không rồi cân bằng.
- **Không tăng mật độ vô hạn**: những mặt alpha vẫn tốn pixel dù rất ít tam giác. Camera nhìn sát ngang xuyên nhiều hàng lúa có thể vẫn nặng.
- **Instancing theo ô ruộng**: khi chuyển sang hàng nghìn cây, dùng renderer chia ruộng thành các ô, lưu transform cây và gửi theo nhóm cùng mesh/material/LOD qua `Graphics.RenderMeshInstanced`. LOD Group của prefab giúp thử hình ảnh; nó không tự biến hàng nghìn prefab thành một renderer.

Trong URP, chỉ bật Enable GPU Instancing trên material chưa đủ để kết luận đang dùng instancing: SRP Batcher có thể được ưu tiên cho GameObject tương thích. Mở Window → Analysis → **Frame Debugger**, Enable, xem lệnh `Render Mesh (instanced)` để xác nhận. Xem [GPU instancing trong Unity 2022.3](https://docs.unity3d.com/2022.3/Documentation/Manual/GPUInstancing.html).

Khi tự viết renderer: mỗi ô phân loại cây theo khoảng cách, mỗi LOD dùng một mesh, mỗi nhóm có thể bắt đầu ở 256 transform. Gọi RenderMeshInstanced mỗi frame cho các ô cần vẽ; tái sử dụng mảng, không cấp phát lại mỗi frame. API xét culling theo cả nhóm nên đừng gửi toàn map trong một nhóm lớn. Giới hạn instance thực tế phụ thuộc dữ liệu gửi, đừng mặc định mọi trường hợp đều 1023. Xem [RenderMeshInstanced](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Graphics.RenderMeshInstanced.html).

Để tránh cây biến mất ở mép camera khi gió uốn: renderer cuối cần bounds bao cả biên độ gió. Sau khi Recalculate Bounds cho prefab, tăng Size của LOD Group một chút nếu vùng chuyển LOD quá chật; việc này **không** sửa mesh culling bounds. Mesh/renderer bounds của hệ thống instancing cũng phải được nới khi triển khai.

## 6. Kiểm tra để biết phần nào còn nặng

Thử cùng vị trí camera và độ phân giải, lần lượt: một cây → khoảng 100 cây → mật độ ruộng mong muốn. Mở Profiler xem CPU Main/Render Thread, GPU nếu máy hỗ trợ; Game → Stats xem triangles và batches. Đo trong build Development để giảm ảnh hưởng của Unity Editor.

Nếu triangles vẫn rất cao: LOD0 đang tồn tại quá xa. Nếu triangles đã thấp mà CPU cao: số object/draw cần giảm bằng instancing và chia ô. Nếu GPU cao khi nhìn ngang ruộng: giảm mật độ, bề mặt alpha chồng nhau và bóng. Tắt bóng tạm để so sánh, rồi bật lại phần cần thiết.

Bộ model này chưa chứng minh một mức FPS cụ thể trên map của bạn. Kết quả cuối phải được đo sau khi bạn hoàn thiện material, gió, LOD và cách phân bố/render ruộng.
