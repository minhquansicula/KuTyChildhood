# Nhân vật Mẹ — KuTy / Unity URP

> **Đang duyệt lại tạo hình.** Bản `Source/Mother.blend` đã được sửa thêm theo phản hồi hình ảnh. FBX, prefab và báo cáo xuất trước đó chưa đại diện cho bản sửa này. Chưa dùng gói cũ làm bản bàn giao cuối; cần duyệt hình và kiểm tra lại biến dạng trước khi xuất lại.

## Sử dụng

- Prefab: `Assets/_Project/Characters/Mother/Mother.prefab`.
- Kéo prefab vào scene, giữ scale `(1,1,1)`. Không kéo file `.blend` vào Assets.
- Shader URP/Lit, 4 bone influences/vertex, animation 30 FPS, in-place.
- Rig **Generic**, dùng bộ animation đi kèm; chưa cung cấp Humanoid retarget.
- `Animator.applyRootMotion = false`; di chuyển GameObject bằng hệ thống di chuyển của game khi chạy `Mother_Walk`.
- Socket: `Socket_Hand_R`, `Socket_Hand_L`, `Socket_LookAt`, `Socket_Voice` nằm trong hierarchy xương.

## Animation

Gọi `animator.CrossFadeInFixedTime("Mother_CallChild", 0.15f)` hoặc thay tên state bên dưới.

| State | Giây | Loop |
|---|---:|---|
| Mother_Idle_Standing | 5 | Có |
| Mother_SitDown | 2.6 | Không |
| Mother_Idle_Seated | 5 | Có |
| Mother_PrepVegetables | 7.2 | Có |
| Mother_StandUp | 2.6 | Không |
| Mother_CallChild | 3.2 | Không |
| Mother_PointToStove | 2.8 | Không |
| Mother_NodApprove | 2 | Không |
| Mother_Walk | 1.2 | Có |
| Mother_CheckPot | 3.6 | Không |

Controller không tự chuyển state để tránh can thiệp logic nhiệm vụ. Sau SitDown chuyển sang Idle_Seated; sau StandUp chuyển Idle_Standing. PointToStove/NodApprove xuất phát từ tư thế ngồi; CallChild/CheckPot xuất phát từ tư thế đứng.

Ghế, mâm rau, nồi và vật cầm tay không nằm trong prefab. Cần căn vị trí theo scene; animation không tự tìm vật hay ghế. Chưa tự thay NPC trong scene hiện có.

Prefab không thêm NavMeshAgent, collider hay logic hội thoại; dùng hệ thống NPC hiện có của game. Chưa có lip-sync. Bản Blender đang duyệt có hai Shape Key ở `Mother_SkinMesh`: `WarmSmile` (mặc định 0.8) và `Concern` (mặc định 0); các điều khiển này chưa được xuất lại và kiểm tra trong Unity.

## Bản duyệt dáng mảnh / nét mặt dịu

- Dựng lại phần da qua cổ tay và tay áo liên tục qua vai; bàn tay thả thấp, hướng theo cẳng tay.
- Thu gọn đùi, bắp chân, bàn chân và dép; giảm độ phồng ngực/hông áo.
- Áo nâu trơn, nẹp giữa và hàng nút nhỏ; quần tối màu, dép quai chữ V.
- Khôi phục phần da trán và dựng lại tóc ôm đầu, để lộ chân mày.
- Ảnh kiểm tra: `Preview/09_Repair_Front.png`, `10_Repair_Side.png`, `11_Repair_Back.png`, `12_WarmFace_Material.png`, `13_Wrist_Material.png`.
- Bản lưu trước sửa: `Source/Progress/07_BeforeSlenderWarmReview.blend`.

## Nguồn và kiểm tra

- File chỉnh sửa: `Source/Mother.blend`; các mốc cũ giữ tại `Source/Progress`.
- FBX: `Exports/SK_Mother.fbx` và `Exports/Animations`.
- Ảnh xem trước: `Preview`.
- Kiểm tra Blender: `Source/delivery_validation.json`.
- Kiểm tra import Unity: `Source/unity_validation.txt`.
- Kiểm tra đọc lại FBX: `Source/fbx_roundtrip_validation.json`.
- Gói mang sang dự án URP khác: `Mother_Unity_URP.unitypackage`.
- Tạo lại trong dự án: menu **Tools > KuTy > Build Mother Delivery**.

Hình học cơ thể dựa trên MakeHuman; tài nguyên xuất MakeHuman theo CC0. Script dựng và tài nguyên nguồn lưu trong `Source` để truy vết.
