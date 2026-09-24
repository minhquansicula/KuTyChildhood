# Chìa Khóa Ký Ức — KuTy

Unity **2022.3.62f3**, URP, Windows.

Mở project bằng Unity Hub, chờ package import hoàn tất, sau đó mở
`Assets/_Project/Scenes/MainMenu.unity` và nhấn **Play**.
Nếu Editor đang mở từ trước khi setup, đóng/mở lại project để nạp cấu hình mới.

Prototype cốt truyện có đủ luồng. **Cảnh 1 hiện được ưu tiên:** văn phòng tương tác với sếp, laptop, hồ sơ và cửa sổ nhìn ra phố; các cảnh tiếp theo vẫn là bản mẫu.

- [Hướng dẫn chạy, kiến trúc và nơi gắn asset](docs/implementation-guide.md)
- [Cảnh văn phòng: tương tác và vị trí gắn 9 asset](docs/office-scene.md)
- [Kết quả kiểm chứng](docs/verification-results.md)
- [Thiết kế và kế hoạch cũ — chỉ để tham khảo](docs/game-design-document.md)

Menu **KuTy** trong Unity có công cụ tạo prototype, nâng cấp scene mẫu, kiểm tra cấu hình và build Windows.
Scene và prefab đã có liên kết Inspector; có thể thay greybox bằng asset của team.
Hình ảnh và âm thanh hiện là placeholder, chưa phải bản mỹ thuật hoàn thiện.

Giữ `Assets` (kèm `.meta`), `Packages` và `ProjectSettings` trong Git.
Các thư mục build, cache và bản sao kiểm thử `.unity-validation` đã được ignore.
