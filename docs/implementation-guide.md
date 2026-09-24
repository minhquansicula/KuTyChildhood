# Nền code Unity cho cốt truyện KuTy

Mở project bằng Unity **2022.3.62f3**, vào `Assets/_Project/Scenes/MainMenu.unity`, nhấn Play. Các scene greybox, prefab, URP, font tiếng Việt và âm báo mẫu đã được tạo. Nếu bắt đầu từ project chưa có scene, chọn **KuTy > Setup > Create playable prototype**. Với scene mẫu phiên bản cũ, chọn **KuTy > Setup > Upgrade existing scenes to story flow**. Lệnh nâng cấp chỉ chỉnh các object mẫu có tên xác định, không thay asset hình/âm của bạn.

## Luồng đã nối code

1. **Hồi 1:** văn phòng có thể đi lại và nhìn quanh; sếp quát, phố xá/tiếng còi qua cửa sổ. Đọc laptop, kiểm tra giấy tờ, sửa báo cáo, ngồi nghỉ để chuyển cảnh. Xem [hướng dẫn cảnh văn phòng](office-scene.md).
2. **Hồi 2:** đọc `Journal` → rửa 5 chén, nhận tổng **2.000đ** và ký ức #1 → mua `Kẹo mút` (500đ) và `Hũ bi ve` (1.500đ), nhận ký ức #2 → chơi bắn bi, nhận ký ức #3 → quay lại nhật ký ghép chìa khóa → nhặt `DreamKeyVisual` → mở `ChainedDoor`.
3. **Hồi 3:** fade trắng vào màn kết, hiện đúng thông điệp cuối và nút Chơi lại.

`QuestManager` là nguồn trạng thái duy nhất cho thứ tự nhiệm vụ. `MemoryCollectionManager` chỉ lưu và trình bày ba mảnh ký ức; **không tự chuyển scene**. `CurrencyManager`, `InventoryManager`, ký ức và nhiệm vụ sống trong Act2 nên một lượt chơi mới sẽ tự reset. `GameManager` giữ quyền khóa input riêng cho từng UI/minigame.

## Nơi gắn asset của bạn

| Asset | Chỗ thay/gắn trong Unity |
| --- | --- |
| Môi trường nhà, bồn rửa, quầy, sân | Thay mesh/renderer của `JournalTable`, `Sink`, `ShopCounter`, `MarbleTable` trong `Act2_MemoryWorld`; giữ Collider, layer `Interactable` và các component logic ở object cũ. |
| Bàn/ghế/laptop/cửa sổ/vách ngăn/tài liệu/sếp/đèn Hồi 1 | Xem [bảng vị trí gắn asset cảnh văn phòng](office-scene.md). |
| Nhật ký | Thay renderer của `Journal`; giữ `JournalInteractable`, Collider, layer. UI trang nhật ký là `UI/JournalPanel`. |
| Chìa khóa 3D | Thay mesh ở `DreamKey/DreamKeyVisual`; giữ `DreamKeyInteractable` và Collider, gán lại `DreamKeyController.keyVisual` nếu đổi GameObject. |
| Cửa, xích Hồi 2 | Thay mesh ở `ChainedDoor` và `Chains`; giữ `ChainedDoorInteractable`, collider/layer; nếu đổi object xích, gán `chainsVisual`. |
| Tranh icon vật phẩm | `ScriptableObjects/Items/Item0.asset` (kẹo mút) và `Item2.asset` (hũ bi ve), trường `icon`. |
| Tiếng gõ phím, giao thông, giọng sếp, tiếng đèn | `Act1_RealWorld > OfficeRoot > OfficeAtmosphere`, các ô AudioClip tương ứng. |
| Nhạc/ambience/SFX | `Assets/_Project/Generated/SoundLibrary.asset` và `Resources/KuTyAudio.prefab`. Các clip hiện có chỉ là tiếng mẫu. |

Lưu ý: khi thay object bằng prefab mới hoàn toàn, cần chuyển lại component, reference trong Inspector, layer `Interactable` và collider. Cách an toàn nhất là giữ object gốc và thay phần hiển thị thành child prefab của nó.

## Điều khiển & chỉnh số liệu

- WASD/chuột: di chuyển/nhìn; E: tương tác; Shift: chạy; Esc: đóng UI, tạm nghỉ minigame hoặc pause.
- Rửa chén: giữ chuột trái, mặc định 3 giây/chén, `DishWashingGame.moneyPerDish = 400`.
- Bắn bi: kéo từ bi xanh rồi thả; đẩy 3 bi đỏ ra vòng trong tối đa 5 lượt. Có thể chỉnh lực, mass, drag trong Inspector.
- Nhật ký: mở bằng E khi nhìn vào cuốn sách, đóng bằng nút hoặc Esc. Nhiệm vụ hiện trên HUD.
- Shop: danh sách vật phẩm nằm ở `Act2 Systems > ShopManager`. Hai asset cốt truyện phải có tổng giá không vượt thưởng rửa chén.

## Kiểm tra

- **KuTy > Validate prototype:** scene/build list, URP và kinh tế 2.000đ.
- **KuTy > Build Windows prototype:** tạo `Builds/Windows/KuTy.exe`.
- Batch integration test: chạy Unity với `-executeMethod KuTySmokeTests.Run` (không thêm `-quit`). Kết quả nằm ở `smoke-results.txt`.

Project là nền gameplay và greybox; chuyển động nhân vật, model, cutscene, ánh sáng, nhạc và voice chất lượng cuối vẫn là phần asset/polish cần làm tiếp.
