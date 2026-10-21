# Theo dõi nâng cấp hình ảnh mini game Scene 2

Cập nhật: 21/10/2026. Tài liệu này dùng để ghi lại ảnh người dùng cung cấp, việc import, gắn sprite vào UI và kết quả chơi thử. **Chưa có ảnh nào được cung cấp hoặc gắn vào mini game trong lần lập tài liệu này.** Mini game hiện tại vẫn chạy bằng các hình UGUI tạo bằng code.

## Tình trạng chung

- Scene: `Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity`.
- UI mini game: `Assets/_Project/Scripts/UI/HouseholdActionBoard.cs` (tạo giao diện và đọc chuột lúc chạy).
- Thứ tự bắt buộc hiện tại lấy từ `HouseholdChoreFlow.Order`: xếp đồ vào rương → xay ngô → gom củi → múc nước → đong gạo → vo gạo → nhóm lửa → nấu cơm → dọn mâm → rửa chén → cất chén → mở thư của mẹ.
- `Chỉnh radio` và `Xếp dụng cụ` vẫn chơi được nhưng là nhiệm vụ tùy chọn, không nằm trong danh sách bắt buộc. Mỗi việc tùy chọn thưởng 2 mảnh ký ức.
- Mỗi việc bắt buộc thưởng 10 mảnh ký ức. Tài liệu cũ `docs/Act2_HouseholdChores.md` còn mô tả 14 việc bắt buộc/140 mảnh; khi kiểm tra tiến độ hãy ưu tiên thứ tự trong code hiện tại.

| Thứ tự | Nhiệm vụ | Ảnh nguồn | Import | Gắn vào mini game | Chơi thử |
| --- | --- | --- | --- | --- | --- |
| 1 | Xếp đồ vào rương | Chưa nhận | Chưa làm | Chưa làm | Chưa thử bản dùng ảnh |
| 2 | Xay ngô | Chưa lập danh sách chi tiết | Chưa làm | Chưa làm | Chưa thử bản dùng ảnh |
| 3 | Gom củi | Chưa lập danh sách chi tiết | Chưa làm | Chưa làm | Chưa thử bản dùng ảnh |
| 4 | Múc nước | Chưa lập danh sách chi tiết | Chưa làm | Chưa làm | Chưa thử bản dùng ảnh |
| 5 | Đong gạo | Chưa lập danh sách chi tiết | Chưa làm | Chưa làm | Chưa thử bản dùng ảnh |
| 6 | Vo gạo | Chưa lập danh sách chi tiết | Chưa làm | Chưa làm | Chưa thử bản dùng ảnh |
| 7 | Nhóm lửa | Chưa lập danh sách chi tiết | Chưa làm | Chưa làm | Chưa thử bản dùng ảnh |
| 8 | Nấu cơm | Chưa lập danh sách chi tiết | Chưa làm | Chưa làm | Chưa thử bản dùng ảnh |
| 9 | Dọn mâm cơm | Chưa lập danh sách chi tiết | Chưa làm | Chưa làm | Chưa thử bản dùng ảnh |
| 10 | Rửa chén | Chưa lập danh sách chi tiết | Chưa làm | Chưa làm | Chưa thử bản dùng ảnh |
| 11 | Cất chén | Chưa lập danh sách chi tiết | Chưa làm | Chưa làm | Chưa thử bản dùng ảnh |
| 12 | Mở thư của mẹ | Chưa lập danh sách chi tiết | Chưa làm | Chưa làm | Chưa thử bản dùng ảnh |
| Tùy chọn | Chỉnh radio; Xếp dụng cụ | Chưa lập danh sách chi tiết | Chưa làm | Chưa làm | Chưa thử bản dùng ảnh |

## Nhiệm vụ 1: Xếp đồ vào rương

### Người chơi sẽ làm gì

Đến rương `BedroomArea/BedroomDecor/StorageChest` và nhấn **E**. Trong mini game, kéo **áo**, **khăn** và **kỷ vật** vào đúng ba ngăn có nhãn `NGĂN ÁO`, `NGĂN KHĂN`, `HỘP KỶ NIỆM`; sau đó kéo **nắp rương xuống**. Đặt sai thì đồ trở về vị trí đầu. Phần chơi này đã có trong `HouseholdActionBoard.cs`; thay ảnh là công việc trình bày và animation, cần giữ nguyên điều kiện hoàn thành và thưởng.

### Bộ ảnh cần cung cấp trước

Ưu tiên **5 tấm độc lập**. Hãy gửi ảnh gốc chất lượng cao; nếu ảnh chưa tách nền thì gửi nguyên ảnh, ghi rõ món cần lấy. Có thể dùng ảnh do bạn vẽ/tạo hoặc ảnh có quyền sử dụng cho game. Ghi link nguồn và giấy phép nếu lấy trên mạng.

| Mức | Tên file đề xuất | Nội dung cần thấy | Từ khóa Google Images (sao chép để tìm) | Tách nền? |
| --- | --- | --- | --- | --- |
| Cần | `Chest_Open.png` | Rương gỗ cũ **mở**, nhìn từ trên xuống hoặc góc chéo nhẹ; thấy rõ ba ngăn trống. Đây là nền của mini game, không phải model 3D trong phòng. | `old wooden chest open top view empty compartments game asset`; `rương gỗ cũ mở nắp nhìn từ trên xuống ngăn đồ png` | Nên tách nền ngoài viền rương; phần trong ngăn vẫn giữ. |
| Cần | `Chest_Lid.png` | **Chỉ nắp rương** tương ứng với rương trên, đủ mép và chốt khóa để kéo xuống. | `wooden chest lid separate top view transparent png game sprite`; `nắp rương gỗ tách riêng png` | Có, nền trong suốt. |
| Cần | `Old_Shirt.png` | Một áo cũ gấp gọn, nhìn cùng góc với lòng rương. | `folded old shirt top view transparent png game asset`; `áo cũ gấp lại nhìn từ trên xuống png` | Có, nền trong suốt. |
| Cần | `Old_Towel.png` | Một khăn vải cũ gấp gọn, hình dáng khác áo. | `folded vintage cloth towel top view transparent png`; `khăn vải cũ gấp lại nhìn từ trên xuống png` | Có, nền trong suốt. |
| Cần | `Keepsake_Box.png` | Hộp kỷ vật nhỏ bằng gỗ hoặc thiếc, đọc rõ khi thu nhỏ. | `small vintage keepsake box top view transparent png game asset`; `hộp kỷ vật nhỏ cũ nhìn từ trên xuống png` | Có, nền trong suốt. |

Nếu khó tìm ảnh rương có đúng **ba ngăn**, có thể gửi `Chest_Open.png` với lòng rương trống; ba vùng nhận đồ sẽ được vẽ bằng UI ở phía trên ảnh. Đừng chọn ảnh có sẵn áo, khăn hoặc hộp trong rương vì các vật đó cần di chuyển độc lập.

### Ảnh tùy chọn để đẹp hơn

| Tên file đề xuất | Dùng để làm gì | Từ khóa tìm kiếm | Tách nền? |
| --- | --- | --- | --- |
| `Hand_Cursor.png` | Bàn tay chỉ dẫn kéo vật/nắp. Có thể dùng chung ở các mini game sau. | `illustrated hand cursor pointing transparent png game ui`; `bàn tay hoạt hình chỉ kéo đồ png trong suốt` | Có. |
| `Chest_Closed.png` | Khung kết thúc sau khi kéo nắp xuống, nếu ảnh nắp riêng không đủ diễn tả rương đóng. | `old wooden chest closed top view transparent png game sprite` | Nên có. |
| `Chest_Scratches.png` | Vân gỗ/vết xước phủ nhẹ lên nền rương. | `wood grain scratches transparent overlay png` | Có. Không cần tìm nếu rương nền đã có vân đẹp. |

### Quy cách ảnh để các tấm ghép được với nhau

1. Chọn **cùng một phong cách** cho cả bộ: minh họa 2D màu mộc, gỗ nâu ấm, vải ngà, ít bóng đổ giả. Tránh trộn ảnh chụp thật với tranh hoạt hình.
2. Chọn **cùng góc nhìn**. Ưu tiên nhìn gần từ trên xuống để thấy ba ngăn. Nắp phải cùng phối cảnh với thân rương. Nếu dùng hai nguồn khác nhau, gửi cả hai để kiểm tra trước khi tách nền.
3. Vật rời là **PNG RGBA** có nền trong suốt, mép sạch; không giữ ô caro giả hay viền trắng. Nếu Google chỉ có JPG, gửi bản gốc để xử lý tách nền sau.
4. Kích thước tham khảo: nền rương khoảng **1024 × 512 px** trở lên; nắp khoảng **1024 × 256 px**; mỗi vật rời khoảng **256 × 256 px**. Đây là mục tiêu chất lượng ảnh nguồn, không phải bắt buộc đúng kích thước.
5. Không gộp 5 món vào một ảnh. Không đưa chữ, nút E hay nhãn ngăn vào ảnh; chữ sẽ do Unity UI hiển thị để giữ tiếng Việt rõ nét.
6. Nên tìm ảnh cho phép sử dụng trong game hoặc tự tạo. Lưu lại link trang nguồn/điều kiện sử dụng; ảnh trên Google Images không tự động được phép tái sử dụng.

### Nơi đặt ảnh và cách gắn vào Unity (bước thực hiện sau khi nhận đủ ảnh)

- Đích dự kiến: `Assets/_Project/Art/Scene2/Textures/Bedroom/ChestMinigame/`. Giữ file nguồn ngoài `Assets` cho tới khi chốt bộ ảnh; khi import chỉ đưa bản dùng cuối cùng vào thư mục này. Không thay `SM_StorageChest.fbx` hoặc material của rương 3D.
- Trong Inspector từng ảnh: `Texture Type = Sprite (2D and UI)`, bật alpha/transparency, dùng độ phân giải phù hợp UI. Kiểm tra viền ảnh sau import.
- Gắn `Chest_Open` làm nền mặt thao tác; gắn ba sprite vật vào các `MovableItem`; hiển thị bóng mờ của cùng sprite tại ba `Compartment`; gắn `Chest_Lid` vào `FinalGesture`. `Hand_Cursor` chỉ dẫn thao tác nếu có.
- Animation đề xuất: đồ nhấc lên hơi phóng to khi kéo, thả đúng thì thu nhỏ và đặt vào ngăn; thả sai thì trượt về; ngăn đúng sáng nhẹ; nắp kéo theo chuột rồi đóng với một nhịp ngắn. Animation chỉ phản hồi trạng thái, không tự đánh dấu hoàn thành khi chưa đủ ba vật và thao tác đóng nắp.
- Code hiện **chưa có chỗ chọn các sprite này trong Inspector**; cần sửa `HouseholdActionBoard.cs` hoặc thêm cấu hình sprite riêng trước khi ảnh xuất hiện trong Play Mode. Import ảnh một mình chưa đủ.

### Checklist để lần sau tiếp tục

- [x] Xác nhận nhiệm vụ bắt buộc đầu tiên và thao tác thực tế trong code.
- [x] Lập danh sách ảnh, tên file, từ khóa tìm kiếm và yêu cầu tách nền.
- [ ] Người dùng gửi 5 ảnh chính hoặc link nguồn có quyền dùng.
- [ ] Kiểm tra cùng phong cách, cùng góc nhìn; chọn ảnh chốt.
- [ ] Tách nền/làm sạch mép, xuất PNG RGBA cho vật rời và nắp.
- [ ] Import sprite vào Unity; ghi lại đường dẫn và kiểm tra preview.
- [ ] Gắn sprite vào UI mini game; giữ vùng kéo/thả khớp hình và kích thước màn hình.
- [ ] Hoàn thiện animation kéo, thả đúng/sai, đóng nắp.
- [ ] Chơi thử trong Scene 2: nhấn E, hoàn tất bằng chuột, thả sai, Esc hủy, chơi lại; kiểm tra không thưởng trùng và không làm mất hoặc đổi cỡ rương 3D.
- [ ] Chụp ảnh mini game sau khi nâng cấp và cập nhật bảng tình trạng chung.

**Nhật ký:**

| Ngày | Thay đổi | Ảnh nguồn/đường dẫn | Kết quả kiểm tra | Việc tiếp theo |
| --- | --- | --- | --- | --- |
| 21/10/2026 | Tạo tài liệu; chưa import ảnh hoặc sửa UI | Chưa có | Đã đối chiếu thứ tự nhiệm vụ và thao tác với code | Nhận/chọn 5 ảnh chính cho rương |

Sau khi hoàn tất nhiệm vụ 1, thêm mục chi tiết tương tự cho **nhiệm vụ 2: Xay ngô** rồi cập nhật bảng đầu file.
