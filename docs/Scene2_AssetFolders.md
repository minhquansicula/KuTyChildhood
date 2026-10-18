# Asset Scene 2

Asset riêng của Scene 2 được lưu tại `Assets/_Project/Art/Scene2/`:

- `Models/`: FBX và mesh; `Models/Act2Home/` giữ các model nhà, giường và texture đi kèm ban đầu.
- `Materials/`: vật liệu bếp, nhà, nền và tường.
- `Textures/`: texture của các đạo cụ đã nhập.
- `Prefabs/`: cột nhà đơn và tường phòng ngủ.
- `Shaders/`: shader tường bếp.
- `BedroomDecor/`: bộ texture/material riêng của tranh tường, kệ tivi và rương đồ.

Scene vẫn ở `Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity`; script giữ nguyên thư mục hiện tại.

Khi thêm asset Scene 2, dùng các thư mục trên. Không di chuyển toàn bộ `Assets/_Project/Art` hoặc thư mục asset Scene 3. Di chuyển asset qua Unity Project/AssetDatabase để giữ `.meta` và GUID. Nếu script dùng đường dẫn cố định, cập nhật cùng lần di chuyển.

Trước khi merge, cập nhật các nhánh liên quan và kiểm tra xung đột. Không có cách bố trí thư mục nào bảo đảm tránh mọi conflict nếu nhiều người cùng sửa hoặc di chuyển một file.
