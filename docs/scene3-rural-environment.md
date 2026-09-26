# Scene 3 — Đường làng, ruộng lúa và tiệm tạp hóa

Cập nhật 27/09/2026, bản sửa bố cục đất trống và hoàng hôn. Scene: `Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity`.

## Phạm vi và căn cứ

Chỉ dựng môi trường scene 3; không sửa C# của nhóm, scene khác, nhiệm vụ, inventory hay chuyển cảnh. GDD mục 4.1–4.2 định hướng low-poly/chibi, chiều Việt Nam, quầy nhỏ/kệ kẹo/đồ chơi treo/radio và sân bi có cây ổi. Implementation guide yêu cầu giữ object, layer, component và tham chiếu gameplay gốc.

Yêu cầu mới của người dùng thay bố cục sân kín/xe hàng trước đó bằng tiệm cố định, đường làng giữa ruộng lúa nước và sân bi với bạn bè ở đoạn sau tiệm. Ruộng lúa và bạn bè là bổ sung theo yêu cầu này, không phải nội dung được khẳng định đã có trong MD gốc.

## Bố cục

Tuyến hình ảnh: **đường làng → tiệm tạp hóa → đi tiếp → sân bi ve**.

| Khu | Vị trí gần đúng | Nội dung |
| --- | --- | --- |
| Xuất phát | (0, 0.05, -5), hướng +Z | Đường đất cong nhẹ rộng 5.2m, ruộng nước hai bên |
| Tiệm bên phải | Quầy (20, 0.6, 20), nhà kéo về Z=27 | Biển “TẠP HÓA CÔ BA”, mái ngói, hiên mở, kệ hàng, kẹo/bi, diều, radio; bãi đất X=12–28, Z=14–32 |
| Sân bi đoạn sau, bên trái | Tâm (-17, 0.08, 46) | Bãi đất X=-26–-9, Z=36–57; ba bạn bè và biển sân bi, không nằm trên đường chính |
| Người dân về làng | Hai nhóm trên đường, gần Z=29 và Z=63 | Người đội nón dẫn trâu/bò bằng dây; animation cảnh nền đi qua theo hướng -Z |

Đường kéo dài từ Z=-15 tới Z=105. Có mương, bờ ruộng và cầu ván vào tiệm/sân bi. Lúa được cắt khỏi hai bãi đất và lối vào, mặt nước được chia nhỏ để không nằm dưới bãi; không chỉ che ruộng bằng một lớp nền. Ground khoảng 180×210m. Đây là một không gian mở trong một scene, chưa có streaming thế giới.

## Hierarchy và tài nguyên

- `Scene3_DuongLang_RuongLua`: cảnh quan mới, chia thành nhóm kiến trúc, ruộng/cây/mương và đường/sân/bạn bè.
- `ShopCounter/TiemTapHoa_Visual`: hình ảnh tiệm làm con quầy tương tác gốc.
- `Art/Materials/Scene3`: vật liệu và font biển hiệu riêng.
- `Art/Models/Scene3`: 12 mesh lúa gộp theo ô ruộng và một mesh đường đất, tránh tạo hàng chục nghìn GameObject.
- `Art/Textures/Scene3`: texture đất riêng, có UV lặp trên đường.
- `Art/Animations/Scene3`: clip di chuyển hai nhóm người/gia súc, chạy vòng 40 giây bằng Animation component riêng.
- `Art/Materials/Scene3/Scene3_Sunset.shader` và `.mat`: skybox hoàng hôn riêng cho scene này, không thay shader/pipeline của team.

## Thay đổi presentation trong scene

Tắt bốn tường bao và mở rộng Ground; giữ object/layer gốc. Quầy ở X=20, Z=20, giữ collider và ShopInteractable. Toàn bộ cụm bi/camera/điểm bắn/điểm nhìn/trigger được dời cùng nhau tới bãi riêng, hạ mặt bi về khoảng Y=0.08; vòng LineRenderer world space đi cùng cụm. Xóa cây, nhà xa, ghế, chòi và cầu trang trí của bản trước. Ẩn phần hình ảnh kitchen/journal/key/door và khối PlayMarbles xanh không liên quan; giữ các object/component gốc để không phá tham chiếu của nhóm. Chưa thiết kế lại chuỗi nhiệm vụ nên các object legacy ẩn chưa phải luồng chơi hoàn chỉnh.

Skybox cam tím có đĩa mặt trời thấp, sương và nắng ấm được đặt riêng trong scene. Không sửa cấu hình pipeline chung.

Bảng giá trang trí giữ kẹo mút 500đ và hũ bi 1.500đ. Các item asset và luật mua không đổi.

## Giới hạn và xác minh

Đây là bản bối cảnh low-poly để duyệt, chưa phải mỹ thuật cuối. Ba bạn bè là mô hình tĩnh. Người dân và gia súc là model ghép khối có chuyển động vị trí đơn giản chạy vòng, chưa có dáng đi khớp chân, AI/pathfinding, thoại hoặc tương tác. Lúa/nước/mương là trang trí, nền đi lại vẫn phẳng. Bi giữ bàn collider, vòng và physics gốc dưới lớp nền, không đổi luật chơi. Chưa thêm âm thanh, chuyển cảnh hay post-processing mới.

Đã nhập và xem Play mode trong Unity 2022.3.62f3: trời hoàng hôn, tiệm và người/gia súc hiển thị. Xác nhận animation chạy bằng tọa độ Z của nhóm thay đổi trong Inspector. Đã kiểm tra từ trên cao cả nền tiệm và bãi bi không có lúa xuyên vào. Mesh OBJ được bù trục X vì Unity chuyển hệ tọa độ khi nhập; kiểm tra các face theo tọa độ sau nhập không có vertex trong hai bãi hoặc lối vào. Mesh có normals riêng. Lần reload cuối không có lỗi tải Transform mới trong Editor.log. Kiểm tra Git: không đổi C# hoặc scene khác; object gốc giữ đầy đủ, 76 MonoBehaviour gốc giữ nguyên serialized data. Chưa kiểm thử toàn bộ chuỗi nhiệm vụ/mua hàng/chơi bi.

UI nâu cũ có thể hiện trong Edit/Scene view; script của nhóm ẩn khi Play. Xem bối cảnh bằng Play/Game view hoặc Frame Selected vào object cảnh quan.

Branch local `nhan-dev/feature/scene3-shop`; chưa commit/push/PR.
