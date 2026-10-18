# Office Scene 1 — Map khối mẫu

Scene: `Assets/_Project/Scenes/Act1_Office_Blockout.unity`.

Map theo [plan Scene 1](scene1-office-plan.md): phòng sếp ở góc bắc-tây, cửa mở ra hành lang, ba bàn nhân viên và bàn Tý cạnh dãy cửa sổ phía đông. Có khu tủ hồ sơ, máy in, cây nước, thùng rác và phông thành phố. Kích thước sàn 14 × 10 m, trần cao 3 m.

## Xem và đi thử

Mở scene rồi Play. Bắt đầu trong phòng sếp với đoạn thoại khoảng 22 giây, dùng phụ đề tiếng Việt. Chuột cho nhìn nhẹ; hết thoại mới được đi. WASD di chuyển, E mở cửa một lần và ngồi tại ghế Tý cạnh cửa sổ. Esc tạm dừng/tiếp tục; mất focus cũng tạm dừng. Ngồi xong chỉ nhìn quanh, chưa có mini-game hoặc đứng dậy. Nhân vật sếp vẫn là khối mẫu.

Nếu đứng trong vùng cửa sắp quét qua, lùi ra rồi nhấn E. Nhìn vào phần tựa hoặc mặt ghế có áo khoác để hiện prompt ngồi. Sau khi ngồi, camera chuyển êm tới bàn và phát `ReadyAtDesk` một lần.

Trong Scene View, có thể tắt `OFFICE_BLOCKOUT/01_Architecture/Ceiling_HIDE_FOR_TOP_VIEW_SLOT` để nhìn từ trên. Bật lại trước khi Play hoặc lưu bản hoàn thiện.

## Thay bằng asset riêng

Các món đồ có object gốc kết thúc bằng `_SLOT`, với hai nhóm con:

- `Visual_REPLACE`: các khối hiển thị, có thể tắt rồi đưa FBX/prefab vào vị trí tương ứng.
- `Collision`: các collider dùng khi đi thử; giữ hoặc chỉnh kích thước theo model mới. Tránh thêm collider trùng.

Giữ transform của `_SLOT` làm mốc. Bàn có mặt cao khoảng 0,8 m, ghế có mặt ngồi cao khoảng 0,48 m. Đổi scale ở model con, tránh đổi scale cả nhóm nếu chưa kiểm tra lại collider.

Bốn bàn nhân viên hiện đã dùng bàn/CRT/bàn phím/ghế lấy từ `Office_Workstation.fbx`. Các nhóm `Imported*` nằm dưới `Visual_REPLACE`; khối mẫu cũ tương ứng được giữ tắt. Material đã khôi phục từ texture nhúng trong GLB gốc; xem `Assets/_Project/Art/Office/RetroOffice/Materials`. Không xóa nhóm `Collision` hoặc `TySeatAnchor` khi thay model.

Cửa nằm dưới `02_BossOffice/BossDoor_Hinge`. Giữ object bản lề, `OfficeDoorInteractable` và collider; đặt model cánh cửa bên dưới, cạnh bản lề trùng gốc. Giữ `TySeatAnchor`, `TyChair_SLOT` và reference trên `OfficeIntroSystems/OfficeIntroController`. Walkthrough cũ đã được gỡ khỏi scene.

Chỉnh lời thoại/thời lượng/voice từng câu ở `OfficeIntroSystems`. Voice để trống dùng phụ đề theo thời lượng fallback. Clip sếp cũ chưa được gán vì chưa đối chiếu kịch bản. Audio Traffic/Keyboard/DeskSlam dùng clip sẵn có; DoorHandle/ChairShift/Ventilation là WAV placeholder tạo cục bộ. Có thể thay clip trực tiếp trên AudioSource, giữ reference.

Các nhóm `01_Architecture` đến `06_Lighting` tách kiến trúc, phòng sếp, bàn nhân viên, tiện ích, ngoại cảnh và ánh sáng.

## Công cụ

- `KuTy > Office Blockout > Build New Map`: tạo scene một lần, từ chối ghi đè scene đã tồn tại.
- `KuTy > Office Blockout > Capture and Validate`: chụp bốn góc và kiểm tra tuyến bằng capsule. Khôi phục trần/ngoại cảnh sau khi chụp; không chỉnh nội thất hoặc tự lưu scene.
- `KuTy > Office Intro > Run Integration Checks`: chạy Play và kiểm tra luồng bằng component/raycast/CharacterController.Move; kết quả ở `Assets/Screenshots/OfficeIntro_Integration.txt`. Đây không phải mô phỏng phím thật.
- `KuTy > Office Intro > Install on Current Map`: dùng cho blockout chưa có intro; từ chối cài chồng. Scene hiện tại đã cài, không cần chạy lại.
- Ảnh và báo cáo nằm trong `Assets/Screenshots/OfficeBlockout_*`.

Scene chạy độc lập, chưa thay mục Scene 1 trong Build Settings. Chi tiết nghiệm thu ở [báo cáo triển khai](scene1-office-implementation.md).

## Thư viện office dùng lại

Bộ asset tập trung tại Assets/_Project/Art/Office/RetroOffice. Kéo prefab từ Prefabs/Furniture, Electronics hoặc Props; Parts chứa bộ phận rời. Models giữ FBX nguồn, Materials giữ material gốc, Textures giữ ảnh nguồn và map chuyển kênh, Documentation có danh mục đối chiếu. Legacy chứa material màu tạm cũ. Đã kiểm tra 141 prefab và giữ 442 GUID khi di chuyển.

