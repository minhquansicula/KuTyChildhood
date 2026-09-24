# Cảnh 1 — Văn phòng ngột ngạt

Scene: `Assets/_Project/Scenes/Act1_RealWorld.unity` (Unity 2022.3.62f3). Đây là cảnh đang được ưu tiên; các cảnh sau vẫn là prototype.

Người chơi nhìn và đi lại ngay từ đầu. Lời quát của sếp hiện bằng phụ đề, còn âm thanh đường phố/đèn huỳnh quang hiện có bản thử. Có thể thay các `AudioClip` trên `OfficeRoot/OfficeAtmosphere` mà không cần sửa code. `StreetTrafficAudio` nằm cạnh cửa sổ; tiếng gõ phím phát từ chính bàn mới.

## Bàn của người chơi

`OfficeRoot/Workstation/OfficeDesk/OfficeDesk_New` là **bàn bạn đã chọn trong ảnh**. Người chơi bắt đầu tại bàn này, nhìn về phía cửa kính. MeshCollider của FBX làm bàn cứng, layer `Interactable` và `OfficeWorkstationInteractable` cho phép nhìn vào bàn rồi nhấn **E**. Cùng một bàn xử lý ba bước: đọc báo cáo bị trả lại → xem hồ sơ → sửa báo cáo (giữ chuột trái). Sau đó tương tác với `OfficeChair` để sang cảnh tiếp theo.

BoxCollider của object cha `OfficeDesk` (bàn ảo cũ) và primitive `OldTable` đã được bỏ; object cha chỉ còn để tổ chức hierarchy. Các bản sao bàn FBX khác và `OfficeInterior_New` vẫn có MeshCollider không phải trigger. Bàn cũ `OfficeDeskModel_Imported` được giữ ở trạng thái tắt để không mất asset của bạn.

`PlayerDeskAfternoonLight` là đèn Spot vàng ấm chiếu từ phía cửa sổ xuống đúng bàn này. Nó chỉ chiếu layer `Interactable`, nên các bản bàn khác không nhận vệt sáng riêng này. Nếu sau này bạn đổi layer bàn, hãy cập nhật `Culling Mask` của đèn tương ứng.

## Sếp tại cửa kính

Hình bóng sếp đã nằm trong texture/mesh cửa kính của `OfficeInterior_New`; không cần tạo thêm model sếp. `BossSilhouette` là điểm tương tác vô hình đặt **ngay trước cửa kính**, giữ collider, `OfficeInteractable` và AudioSource. Nhìn vào vùng hình sếp từ trong phòng rồi nhấn **E** để nghe phản ứng; tương tác này không khóa tiến trình chính.

`BossGlassInteriorGlow` là Point Light xanh lạnh đặt sát mặt kính phía trong phòng, hắt sáng từ khu vực bóng sếp. Project URP đã nâng giới hạn đèn bổ sung trên mỗi mesh từ 4 lên 8, để đèn này và vệt nắng bàn không bị bốn đèn trần lấn mất. Đây là ánh sáng trong scene, chưa phải shader emissive trên chính bề mặt kính. Nếu muốn đường viền bóng sếp tự phát sáng/bloom thật rõ, bước sau là tách phần kính/bóng ra material riêng trong Blender hoặc Unity và chỉnh emission.

## Asset và chỗ thay thế

| Asset | Object / vị trí |
| --- | --- |
| Văn phòng | `OfficeRoot/OfficeInterior_New` — FBX một mesh liền; tách trong Blender nếu cần chỉnh riêng cửa/tường. |
| Bàn chính | `OfficeRoot/Workstation/OfficeDesk/OfficeDesk_New` — giữ MeshCollider, layer `Interactable`, `OfficeWorkstationInteractable`. |
| Ghế / điểm nghỉ | `OfficeChair` — vùng tương tác hiện vẫn dùng collider; có thể đặt mesh ghế làm child. |
| Cửa kính và hình sếp | Hình nằm trên `OfficeInterior_New`; vùng bấm E là `BossSilhouette`, ánh sáng là `BossGlassInteriorGlow`. |
| Phố ngoài cửa sổ | `StreetView/StreetBackdrop2D` — kéo ảnh vào `SpriteRenderer.sprite` sau khi import dạng Sprite (2D and UI). |
| Đèn trần | `FluorescentFixture` — giữ Light và AudioSource, có thể thay phần vỏ bằng model. |

Nếu thay FBX bằng prefab mới, nhớ chuyển MeshCollider/layer/script tương tác sang bản mới. Chỉ kéo model mới vào Scene sẽ **không** tự có collider và phím E. Các asset FBX/texture hiện nằm trong `Assets/_Project/Art/Models/OfficeSceneNew` và `Assets/_Project/Art/Textures/OfficeSceneNew`.

## Kiểm tra nhanh

- Mở cảnh, Play: nhìn xuống bàn chính, prompt **E** xuất hiện; tương tác ba lần để đến bước giữ chuột sửa báo cáo.
- Đi đến cửa kính có hình sếp: nhìn vào giữa cửa, prompt **E** xuất hiện; bấm E không làm mất quyền đi lại.
- Quan sát chỉ bàn của người chơi có vệt sáng chiều riêng; phía cửa kính có ánh xanh lạnh.
- Thay Sprite/AudioClip rồi Play để kiểm tra crop, âm lượng và vị trí âm thanh.

Menu **KuTy > Setup > Build interactive office scene (Act 1)** chỉ dành cho scene mẫu chưa có `OfficeRoot`; nếu đã tồn tại, lệnh giữ nguyên bố cục hiện tại.
