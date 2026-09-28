# GIAO VIỆC CHO AGENT: XÂY DỰNG SCENE 2 (ACT 2 - MEMORY WORLD HOME)

## 🚨 QUY TẮC TỐI THƯỢNG (CRITICAL RULE)
- **CHỈ ĐƯỢC PHÉP** chỉnh sửa, làm việc và thêm tính năng vào scene: `Act2_MemoryWorld_Home.unity`.
- **TUYỆT ĐỐI KHÔNG** đụng chạm, thay đổi hay chỉnh sửa các scene khác (ví dụ: Act 1, Act 3, MainMenu, v.v.).

---

## 1. BỐI CẢNH & Ý TƯỞNG (THEME & CONCEPT)
- **Chủ đề:** Ký ức tuổi thơ tại một ngôi nhà xưa ở Việt Nam.
- **Không gian:** Nhà cũ, mộc mạc, chưa có bếp ga (phải đun củi), có lu nước, chạn bát gỗ.
- **Mục tiêu chính:** Người chơi thức dậy, thực hiện các công việc nhà (nấu cơm, dọn dẹp) thông qua các mini-game tương tác để tái hiện tuổi thơ.

---

## 2. CÁC HỆ THỐNG ĐÃ HOÀN THIỆN (HIỆN TRẠNG SCENE)

### 2.1. Khu vực Phòng ngủ (BedroomArea)
- **BedBlock:** Người chơi bắt đầu scene ở trên giường. Bấm `[E]` để thức dậy và xuống giường.
- Script quản lý: `BedWakeUpController` (Teleport người chơi xuống đất, mở khóa di chuyển và hiển thị UI Checklist).

### 2.2. Khu vực Bếp (KitchenArea) & Cơ chế Mini-game
Đã tạo sẵn các khối Placeholder cho nhà bếp và gán script tương tác (có thể chơi đi chơi lại nhiều lần - `isOneTimeUse = false`):
1. **WaterJar (Lu nước):** Script `MashToCompleteTask` - Nháy phím `Space` 5 lần để múc nước.
2. **RiceWashingBowl (Thau vo gạo):** Script `RiceWashingTask` - Dùng chuột xoay 3 vòng, sau đó bấm Chuột Phải để chắt nước.
3. **FirewoodStove (Bếp củi):** Script `MashToCompleteTask` - Nháy phím `Space` 15 lần để thổi lửa.
4. **RicePot (Nồi cơm):** Script `HoldToCompleteTask` - Giữ phím `F` 4 giây để chắt nước cơm/nấu cơm.
5. **DiningTable (Bàn ăn):** Script `HoldToCompleteTask` - Giữ phím `F` 3 giây để dọn mâm cơm.
6. **DishCabinet (Chạn bát):** Script `HoldToCompleteTask` - Giữ phím `F` 2 giây để úp bát.

### 2.3. Hệ thống Checklist Công Việc (KitchenFlowManager)
- Ngay khi người chơi rời khỏi giường, một bảng UI Checklist (Góc trên bên trái) sẽ xuất hiện.
- Chứa danh sách các việc cần làm: Múc nước lu -> Vo gạo -> Nhóm lửa -> Nấu cơm -> Dọn mâm cơm.
- Khi một tương tác ở bếp hoàn thành, script sẽ gọi `KitchenFlowManager.Instance.MarkTaskCompleted("Tên_GameObject")` để tự động gạch bỏ task đó trên giao diện UI (chuyển sang màu xanh lá).

---

## 3. NHIỆM VỤ CẦN LÀM TIẾP THEO DÀNH CHO AGENT
Agent tiếp nhận file này hãy đọc kỹ và tiếp tục thực hiện các đầu việc sau trong `Act2_MemoryWorld_Home`:

1. **Tinh chỉnh Level Design (Môi trường):**
   - Điều chỉnh lại vị trí tường (`FrontWall`, v.v.), nền nhà (`MainFloor`) sao cho khớp khít thành một khối nhà hoàn chỉnh, không bị lệch hoặc hở.
   - Thay thế các khối hộp (Cube/Placeholder) của Bếp củi, Lu nước, Nồi cơm... bằng model 3D thực tế (nếu dự án có sẵn Assets), hoặc điều chỉnh scale/material cho giống thật hơn.

2. **Hoàn thiện cốt truyện (QuestManager):**
   - Hiện tại game đang ép bypass nhiệm vụ đọc nhật ký (`QuestStep.ReadJournal` -> `WashDishes`). 
   - Cần thiết kế lại luồng `QuestManager` để nó đồng bộ với `KitchenFlowManager`. Khi người chơi hoàn thành toàn bộ Checklist nấu ăn, Quest chính thức mới được cập nhật.

3. **Cải thiện UI/UX & Feedback:**
   - Đảm bảo thanh `ProgressBarUI` hiển thị chính xác tiến trình lúc nhấn giữ phím/nháy phím.
   - Thêm hiệu ứng âm thanh (Audio) và Particle (lửa cháy, nước chảy) khi người chơi hoàn thành từng công đoạn.

4. **Xử lý Minigame Rửa chén (DishWashingGame):**
   - Tích hợp logic rửa chén hiện có (`Sink`) vào luồng công việc của gian bếp. Rửa chén có thể là bước cuối cùng sau khi ăn cơm xong.

**Nhắc lại:** MỌI THAY ĐỔI CHỈ DIỄN RA TRONG `Act2_MemoryWorld_Home`!
