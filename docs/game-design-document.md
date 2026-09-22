# 📖 GAME DESIGN DOCUMENT
## "CHÌA KHÓA KÝ ỨC" (tên tạm)

---

## 1. TỔNG QUAN DỰ ÁN

| Mục | Nội dung |
|---|---|
| **Thể loại** | First-Person Exploration & Puzzle nhẹ, mang tính kể chuyện (Narrative-driven) |
| **Góc nhìn** | First-Person (góc nhìn thứ nhất) |
| **Phong cách đồ họa** | 3D Chibi / Low-poly, hoài niệm |
| **Nền tảng** | PC (Windows) |
| **Thời lượng chơi** | 15–25 phút/lượt chơi |
| **Đối tượng chơi** | Người chơi thích trải nghiệm cảm xúc, hoài niệm tuổi thơ Việt Nam |
| **Công cụ** | Unity 2022.3 LTS / URP, Meshy hoặc Tripo AI cho asset 3D |

### Câu chuyện một dòng (Elevator Pitch)
Một nhân viên văn phòng kiệt sức vì áp lực công việc trở về căn nhà cũ, vô tình xuyên không vào chính ký ức tuổi thơ của mình — nơi họ phải thu thập lại những mảnh ký ức đẹp đẽ để ghép thành "Chìa khóa ước mơ", giúp bản thân thoát khỏi sự ngột ngạt của hiện thực.

---

## 2. CỐT TRUYỆN & CHỦ ĐỀ

### 2.1 Thông điệp cốt lõi
Tuổi thơ giản dị, gia đình, và những ước mơ nhỏ bé là "chìa khóa" giúp con người tìm lại chính mình giữa cuộc sống hiện đại áp lực.

### 2.2 Cấu trúc 3 hồi (3-Act Structure)

**HỒI 1 — MỞ ĐẦU (Hiện thực)**
- Nhân vật chính (nhân viên văn phòng) trở về căn nhà cũ sau nhiều năm, mang theo mệt mỏi, deadline, áp lực.
- Bước vào một căn phòng cũ (có thể là phòng ngủ tuổi thơ) → chạm vào 1 vật kỷ niệm (con lật đật, cuốn tập cũ...) → màn hình mờ dần, ánh sáng chuyển vàng ấm → xuyên không.

**HỒI 2 — THẾ GIỚI KÝ ỨC (Gameplay chính)**
Nhân vật quay về làm một đứa trẻ, dạo bước qua 3 khu vực ký ức đặt liền kề nhau trên **cùng một bản đồ nhỏ gọn** (gian bếp → sân nhà → tiệm tạp hóa đầu ngõ, đi bộ qua lại được, không cần loading màn hình giữa các khu). Mỗi khu vực gắn với 1 kỷ niệm và 1 "mảnh ký ức":

| Khu vực | Bối cảnh | Mini-game / Hoạt động | Mảnh ký ức |
|---|---|---|---|
| 1. Gian bếp | Giúp mẹ rửa chén sau bữa cơm | Mini-game Progress Bar (rửa chén) | Ký ức về Mẹ |
| 2. Sân nhà | Chơi bắn bi cùng bạn hàng xóm | Mini-game Vật lý (bắn bi, Rigidbody) | Ký ức về Bạn bè |
| 3. Tiệm tạp hóa đầu ngõ | Dùng tiền kiếm được mua kẹo/đồ chơi | Hệ thống Shop/Inventory | Ký ức về Niềm vui giản dị |

**HỒI 3 — KẾT THÚC (Thức tỉnh)**
- Sau khi thu thập đủ 3 mảnh ký ức → chúng hợp lại thành "Chìa khóa ước mơ" phát sáng.
- Cutscene ngắn: nhân vật tỉnh dậy trong căn nhà cũ ở hiện tại, tay cầm vật kỷ niệm, ánh mắt đã khác — nhẹ nhõm hơn, tìm lại được lý do để tiếp tục cố gắng.
- Màn hình kết: dòng chữ thông điệp + tên game.

---

## 3. GAMEPLAY CHI TIẾT

### 3.1 Hệ thống tương tác cơ bản (nền tảng xuyên suốt)
- **Cơ chế**: Raycast từ camera, khoảng cách ~2–3m, layer "Interactable"
- **Input**: Nhìn vào vật → hiện icon/prompt "Nhấn [E]" → bấm E → gọi hàm `Interact()`
- **Phản hồi hình ảnh (Visual Feedback)**: 
  - **Phương án khuyến nghị cho team mới (dễ làm)**: Khi Raycast trúng vật `IInteractable`, đổi màu crosshair từ trắng → đỏ/vàng + hiện text "[E] Tương tác" trên UI. Chỉ cần code C# thuần (đổi `color` của Image UI), **không cần viết shader**.
  - **Phương án nâng cao (chỉ làm nếu còn dư thời gian ở Ưu tiên 3)**: Outline sáng nhẹ quanh vật thể bằng shader riêng hoặc asset có sẵn (VD: "Quick Outline" trên Asset Store, không cần tự viết shader từ đầu) — đẹp hơn nhưng tốn công cấu hình, dễ gây lỗi render với người mới nên không ưu tiên.

### 3.2 Mini-game 1: Rửa chén (Progress Bar)
- **Mục tiêu**: Giúp mẹ, kiếm tiền để dùng ở tiệm tạp hóa (Hồi 3)
- **Cách chơi**: Giữ chuột trái (hoặc bấm liên tục) lên chồng chén → thanh Progress tăng dần → đầy thanh → chén "sạch" (đổi model/hiệu ứng nước) → lặp lại với chén tiếp theo
- **Thưởng**: Hoàn thành đủ số chén → nhận tiền + mảnh ký ức "Ký ức về Mẹ"
- **Độ khó**: Dễ, mang tính thư giãn, không giới hạn thời gian gắt gao (game hoài niệm, không nên tạo áp lực)

### 3.3 Mini-game 2: Bắn bi (Vật lý Rigidbody)
- **Mục tiêu**: Thắng bạn chơi bi để có ký ức về tình bạn
- **Cách chơi**: Kéo chuột để canh lực + hướng bắn → thả chuột → viên bi (Rigidbody) lăn theo vật lý thực, va chạm với bi đối thủ
- **Điều kiện thắng**: Bắn trúng đủ số lượng bi mục tiêu trong giới hạn lượt bắn
- **Thưởng**: Thắng → nhận mảnh ký ức "Ký ức về Bạn bè"

### 3.4 Hệ thống Cửa hàng / Inventory
- **Giao diện**: UI đơn giản hiện danh sách vật phẩm (kẹo, đồ chơi) kèm giá tiền
- **Luồng**: Người chơi có tiền từ mini-game rửa chén → vào tiệm tạp hóa → chọn vật phẩm → bấm mua → trừ tiền, vật phẩm vào túi đồ
- **Liên kết cốt truyện**: Mua đủ 1 món đồ chơi đặc biệt (gợi ý: viên kẹo dừa hoặc con diều giấy) → kích hoạt mảnh ký ức cuối "Niềm vui giản dị"

### 3.5 Hệ thống thu thập Mảnh ký ức
- Mỗi khu vực hoàn thành → 1 mảnh ký ức bay vào UI góc màn hình (icon dạng mảnh ghép/ánh sáng)
- Khi đủ 3/3 mảnh → tự động kích hoạt sự kiện ghép "Chìa khóa ước mơ" → chuyển sang Hồi 3

---

## 4. THIẾT KẾ THẾ GIỚI & MỸ THUẬT

### 4.1 Phong cách hình ảnh
- **Model**: Chibi/Low-poly — tỷ lệ đầu to, thân nhỏ, ít chi tiết, mang cảm giác dễ thương, hoài niệm chứ không chân thực
- **Màu sắc chủ đạo**: Tông vàng ấm (giống hoàng hôn/nắng chiều Việt Nam), tương phản với tông xám lạnh của thế giới hiện thực ở Hồi 1
- **Ánh sáng**: 
  - Bloom nhẹ tạo cảm giác lung linh, mơ màng
  - Color Grading ngả vàng/cam ấm
  - Vignette nhẹ ở góc màn hình tạo chiều sâu, cảm giác hồi tưởng

### 4.2 Bối cảnh 3 khu vực (không gian nhỏ, gọn)
1. **Gian bếp**: bồn rửa, chạn bát, bếp củi/bếp ga cũ, cửa sổ có nắng chiếu vào
2. **Sân nhà**: sân đất/sân gạch, cây khế hoặc cây ổi, chỗ ngồi chơi bi
3. **Tiệm tạp hóa**: quầy hàng nhỏ, kệ kẹo, đồ chơi treo lủng lẳng

> **Lưu ý về NPC người bán hàng**: AI 3D (Meshy/Tripo) tạo mesh nhân vật người thường rất tệ, đặc biệt phong cách Chibi (dễ bị méo mặt, tay chân sai tỷ lệ). Có 2 hướng xử lý thay vì cố generate:
> - **Phương án đơn giản (khuyến nghị)**: Không cần NPC người — thiết kế quầy hàng tĩnh (không có người), đặt thêm 1 chiếc radio cũ trên quầy phát tiếng nói/nhạc rè rè tạo cảm giác có sự sống mà không cần nhân vật
> - **Phương án khác**: Tải NPC low-poly có sẵn, miễn phí trên Unity Asset Store (tìm "low poly character free" hoặc "chibi character free"), chỉnh sửa màu/trang phục lại cho hợp bối cảnh, thay vì tự generate bằng AI

### 4.3 Âm thanh
- Nhạc nền: lo-fi/acoustic nhẹ nhàng, mang hơi hướng làng quê Việt Nam
- SFX: tiếng chén va, tiếng bi lăn/va chạm, tiếng chuông gió, tiếng chim

---

## 5. GIAO DIỆN NGƯỜI DÙNG (UI/UX)

| Màn hình | Thành phần |
|---|---|
| **HUD trong game** | Crosshair nhỏ giữa màn hình, prompt tương tác "[E] Tương tác", icon tiền góc trên, icon mảnh ký ức góc trên |
| **Mini-game Rửa chén** | Progress bar giữa màn hình |
| **Mini-game Bắn bi** | Chỉ báo lực/hướng bắn, số lượt còn lại |
| **Shop** | Panel danh sách vật phẩm, ảnh + giá + nút Mua |
| **Main Menu** | Nút Chơi / Thoát, tên game, hình nền tĩnh phong cách chibi |
| **Màn hình kết** | Hình ảnh "Chìa khóa ước mơ", thông điệp kết, nút Chơi lại/Thoát |

---

## 6. KIẾN TRÚC KỸ THUẬT (Technical Overview)

### 6.1 Cấu trúc Script đề xuất
```
Assets/
├── Scripts/
│   ├── Player/
│   │   ├── PlayerController.cs      (di chuyển FPS)
│   │   └── PlayerInteraction.cs      (Raycast + phím E)
│   ├── Interactables/
│   │   ├── IInteractable.cs          (interface chung)
│   │   ├── MemoryPiece.cs            (vật thu thập ký ức)
│   │   └── ShopItem.cs
│   ├── MiniGames/
│   │   ├── DishWashingGame.cs        (progress bar)
│   │   └── MarbleShootingGame.cs     (vật lý bắn bi)
│   ├── Systems/
│   │   ├── InventorySystem.cs
│   │   ├── ShopSystem.cs
│   │   ├── MemoryCollectionManager.cs (theo dõi 3/3 mảnh)
│   │   └── GameManager.cs            (quản lý trạng thái/scene chung)
│   └── UI/
│       ├── HUDController.cs
│       ├── ShopUI.cs
│       └── ProgressBarUI.cs
├── Scenes/
│   ├── MainMenu
│   ├── Act1_RealWorld
│   ├── Act2_MemoryWorld      (gộp chung: bếp + sân + tiệm tạp hóa)
│   └── Act3_Ending
```

> **Vì sao gộp cả 3 khu vực Hồi 2 vào 1 scene duy nhất?**
> Với team mới dùng Unity, việc truyền dữ liệu (tiền, số mảnh ký ức đã nhặt) giữa nhiều scene qua `DontDestroyOnLoad`/Singleton rất dễ phát sinh lỗi mất dữ liệu, null reference khó debug. Gian bếp, sân nhà và tiệm tạp hóa là 3 không gian nhỏ, hoàn toàn đặt liền kề nhau được trên cùng 1 bản đồ nhỏ gọn — người chơi chỉ cần đi bộ qua lại giữa các khu, không cần load scene mới. Việc này vừa giảm rủi ro lỗi kỹ thuật, vừa giúp người chơi cảm nhận không gian ký ức liền mạch hơn thay vì bị ngắt quãng bởi màn hình loading.
> Chỉ thật sự cần load scene riêng khi chuyển **giữa các Hồi** (Act1 → Act2 → Act3), vì đây là thời điểm hợp lý để có "khoảng lặng" chuyển cảnh (fade), và dữ liệu cần giữ lại lúc này ít hơn nhiều (chỉ cần biết đã đủ 3/3 mảnh ký ức chưa).

### 6.2 Nguyên tắc thiết kế hệ thống
- Dùng **Interface `IInteractable`** cho mọi vật tương tác được (chén, bi, vật phẩm shop, mảnh ký ức) → code Raycast chỉ cần gọi `.Interact()`, không cần if/else riêng từng loại
- Dùng **ScriptableObject** để lưu dữ liệu vật phẩm shop (tên, giá, icon) → dễ chỉnh sửa không cần sửa code
- **GameManager** dùng Singleton pattern đơn giản, quản lý tiền, số mảnh ký ức đã thu thập — chỉ cần tồn tại xuyên suốt lúc chuyển **giữa các Hồi** (Act1 → Act2 → Act3) bằng `DontDestroyOnLoad`, không cần áp dụng trong nội bộ Hồi 2 vì giờ đã gộp chung 1 scene, giảm đáng kể nguy cơ lỗi mất dữ liệu giữa các khu vực nhỏ

---

## 7. LỘ TRÌNH PHÁT TRIỂN (tham chiếu)

Xem chi tiết đầy đủ trong tài liệu **"Kế hoạch phát triển 5 tuần"** đã tạo trước đó — tài liệu này (GDD) là nền tảng thiết kế, còn tài liệu kia là lịch trình thực thi theo tuần.

---

## 8. GHI CHÚ MỞ RỘNG (nếu còn thời gian — Ưu tiên 3)

- Thêm hiệu ứng particle: bụi nắng lơ lửng trong gian bếp, đom đóm ở sân nhà buổi tối
- Thêm NPC người mẹ có animation đơn giản (không cần AI, chỉ cần vài animation lặp)
- Thêm nhật ký/ghi chú nhỏ để người chơi đọc thêm về bối cảnh nhân vật chính ở Hồi 1
