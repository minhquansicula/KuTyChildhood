# MomUser — đã đưa vào Unity bằng FBX

## Cập nhật tỷ lệ Scene 2 — 2026-11-07

Theo yêu cầu mới của người dùng: mẹ phải cao hơn Tý học lớp 3. Cấu hình **instance trong scene** hiện thay thế thông tin “không đổi scale” của lần tích hợp ban đầu bên dưới:

- `Scene2MotherDemo/MomUser_Model`: uniform local scale `1.43500519`, chiều cao dự kiến khoảng **1,65 m** từ chiều cao mesh đã đo `1,14982164 m`.
- `Player`: CharacterController cao **1,30 m**, center `(0, 0.65, 0)`; `PlayerCamera.localPosition.y = 1.20` khi đứng. Đây là nhân vật góc nhìn thứ nhất, không phải một mesh Tý được đo chiều cao.
- Collider tương tác mẹ: height `1.65`, center `(0, 0.825, 0)`, radius `0.28`; voice anchor local Y `1.452`.
- Chỉ override trong `Act2_MemoryWorld_Home.unity`; không Apply lên prefab, không đổi FBX/import scale/rest skeleton/source Blender, không sửa runtime scripts.
- Scene sạch trước khi sửa, đã sao lưu trước thao tác (đường dẫn trong `Source/user_mom_scene2_scale_plan.json`), lưu trước QA; sau QA Editor ở Edit Mode, scene không dirty.
- Smoke test mới `a988d2a4` passed: binding/avatar hợp lệ → CallChild, bàn tay chuyển động → Idle. Không Error/Exception trong cửa sổ test; cảnh báo collider scale âm của tường vẫn còn. Chưa kiểm thử lại toàn tuyến nhiệm vụ, mọi điểm tương tác từ tầm mắt mới hoặc toàn bộ biến dạng animation.
- Báo cáo: `Source/user_mom_scene2_scale_applied.json`, `Source/user_mom_scene2_scale_runtime.json`, `Source/user_mom_scene2_scale_readback.json`.

Các số liệu 1,149822 m/scale không đổi phía dưới mô tả **asset nguồn và lần cài ban đầu**, không phải chiều cao instance hiện tại trong Scene 2.

## Vị trí

- Scene: `Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity`.
- Hierarchy: `Scene2MotherDemo/MomUser_Model`.
- Prefab: `Assets/_Project/Characters/MomUserDemo/MomUser.prefab`.
- Model nguồn Unity: `Assets/_Project/Characters/MomUserDemo/SK_MomUser.fbx`.
- Giữ đúng mẫu `C:/Users/LENOVO/Downloads/mom.fbx`, từ bản authoring `Mom_User_ArmAlignment.blend`.
- **Không chép .blend vào Assets.** Chỉ chép 5 FBX nguồn; texture được nhúng trong FBX model, rồi Unity trích xuất để tạo material URP. Prefab/controller/material/texture là các asset hỗ trợ được tạo trong Unity.

## Bộ cũ đã gỡ an toàn

Chỉ gỡ **`Assets/_Project/Characters/Mother/`** và `.meta` tương ứng qua AssetDatabase, sau khi:

1. Kiểm tra không có asset bên ngoài còn phụ thuộc bộ cũ.
2. Sao lưu đủ 69 file và folder meta, đối chiếu hash khớp.
3. Gắn và kiểm tra model mới thành công trong Scene 2.

Backup: `Tools/MotherDelivery/Backups/UnityMotherReplace_20261106_215049/`.
Scene trước lần gắn mới cũng được sao lưu tại `Tools/MotherDelivery/Backups/BeforeMomUserBind_20261107_002228.unity`.

Không xóa file `mom.fbx`, source .blend ngoài Assets, thoại/âm thanh mẹ, nhiệm vụ, tương tác hoặc voice anchor. Khung `Scene2MotherDemo` vẫn giữ vì các hệ thống gameplay tham chiếu tới nó; chỉ model con được gắn mới. Không xóa theo từ khóa Mother/Mom toàn project.

## Animation / tương thích code hiện có

| FBX mới | Tên clip/state trong Unity | Giây | Loop |
| --- | --- | --- | --- |
| `Animations/MomUser_Idle_Gameplay.fbx` | `Mother_Idle_Standing` | 5 | Có |
| `Animations/MomUser_CallChild_Gameplay.fbx` | `Mother_CallChild` | 3,4 | Không |
| `Animations/MomUser_TalkIdle_Gameplay.fbx` | `Mother_TalkIdle` | 5 | Có |
| `Animations/MomUser_Nod_Gameplay.fbx` | `Mother_NodApprove` | 1,8 | Không |

Tên state/clip Mother ở bảng là **alias tương thích script Scene2MotherDemo**, không phải sử dụng lại model/animation cũ. Mesh, skeleton và chuyển động đều từ FBX MomUser mới. Không chỉnh runtime script, thoại hoặc logic nhiệm vụ của người dùng.

Idle/CallChild đã nối với behavior hiện có. TalkIdle/Nod đã import và có state riêng, nhưng chưa tự thêm logic trò chuyện mới để gọi hai state đó. Walk và bộ ngồi chưa đưa vào vì còn lỗi đã ghi trước.

## Cấu hình và kiểm tra

- Generic avatar hợp lệ, dùng chung skeleton; không tuyên bố Humanoid.
- Model 2 skinned mesh, 50.662 tam giác, chiều cao 1,149822 m. Không tự tăng scale để ép thành người trưởng thành.
- FBX: `-Z forward`, `Y up`, unit scale giữ nguyên, không bake-space-transform/apply-transform, không leaf bones, bake 30 FPS, không all-actions/NLA/preview/camera/đèn.
- Mỗi animation FBX có đúng một take; kèm skin bind-pose giống model để bảo toàn skeleton. Unity scene chỉ dùng model `SK_MomUser.fbx`, không tạo thêm nhân vật từ các FBX clip.
- Import lại trong Blender: các mẫu mesh/bone sai lệch dưới 0,001 mm; giữ đúng chiều cao, đủ frame.
- Unity: material URP/Lit với base map, normal map và metallic/smoothness được pack từ map nguồn; skin tối đa 4 ảnh hưởng, root motion tắt.
- Play Mode đã chạy bài kiểm tra có assertion: avatar/rig hợp lệ → gọi con → bàn tay chuyển động thực tế → tự về Idle. Bài kiểm tra job `863fb524` passed, không có runtime Error/Exception trong cửa sổ kiểm tra.
- Sau gỡ bộ cũ: quét scene không có missing script/reference; quét serialized GUID không còn tham chiếu asset cũ.
- Ảnh Unity: `Preview/UserMomAnimation/Unity/MomUser_InHome.png` và `MomUser_RuntimeCall.png`.

**Giới hạn:** đây là kiểm chứng import/binding và smoke test, không chứng nhận hết biến dạng tay/quần áo ở mọi frame hoặc toàn bộ gameplay. Các cảnh báo collider scale âm có sẵn của tường và warning CS0184 cũ chưa sửa vì ngoài phạm vi. Scene đã lưu phần gắn model trước Play test; không ghi đè thêm các thay đổi live/dirty sau kiểm tra.

## Công cụ

Editor helper mới: `Assets/_Project/Editor/MomUserFbxIntegration.cs`, menu `KuTy > Mom User FBX`.
Các bước build/bind có guard chống ghi đè/tạo trùng; không chạy lại để “sửa thử”. **Không chạy menu Build Mother Delivery cũ**, vì công cụ cũ trỏ tới bộ Mother đã nghỉ dùng và có thể tạo lại nó.

Báo cáo chi tiết: `Source/user_mom_unity_final_status.json`, `user_mom_unity_roundtrip.json`, `user_mom_unity_runtime_scenario.json`. Không commit/push hoặc chỉnh Git config.
