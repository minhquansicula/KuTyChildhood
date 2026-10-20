# Unity CLI cho project Ku Tý

Project hiện được bind với:

- Unity CLI: `1.0.0-beta.8`
- Unity Editor: `2022.3.62f3`
- Editor path: `D:\Unity IDE\2022.3.62f3\Editor\Unity.exe`
- Project path: `E:\FPT\FALL26\PRU213\KuTy`

Mở PowerShell tại thư mục project trước khi chạy các lệnh bên dưới.

## 1. Kiểm tra nhanh

```powershell
unity --version
unity doctor --ci --format json --non-interactive
unity projects verify . --expect-editor 2022.3.62f3 --format json --non-interactive
```

`projects verify` chỉ kiểm tra cấu trúc project như file `.meta`, GUID, conflict marker và `manifest.json`; lệnh này không mở hoặc sửa scene.

## 2. Mở project bằng CLI

Chỉ dùng khi project chưa được mở bằng Unity Editor:

```powershell
unity open . --args "-unityskills-coldstart"
```

Tham số `-unityskills-coldstart` yêu cầu UnitySkills bridge khởi động cùng Editor.

Mở thẳng Scene 1 khi Editor đang đóng:

```powershell
unity open . --args "-unityskills-coldstart -openscene Assets/_Project/Scenes/Act1_RealWorld.unity"
```

Không chạy `unity open`, `unity test`, `unity run` hoặc `unity build` khi cùng project đang mở trong Editor.

## 3. Chạy test headless

Project hiện đã bật quyền CLI test. Đóng Unity Editor trước, sau đó chạy:

```powershell
unity test . --mode EditMode --output .\TestResults\editmode.xml --timeout 1800 --format json --non-interactive
```

PlayMode test:

```powershell
unity test . --mode PlayMode --output .\TestResults\playmode.xml --timeout 1800 --format json --non-interactive
```

Exit code thường gặp:

- `0`: test thành công.
- `6`: không có kết quả hợp lệ, compile lỗi, timeout, license lỗi hoặc Editor crash.
- `8`: có test thất bại.

## 4. Build game

CLI build hiện đang tắt để tránh build nhầm khi project còn thay đổi. Khi muốn dùng:

1. Trong Unity mở `Window > UnitySkills`.
2. Chọn tab `Unity CLI`.
3. Bật `Headless builds (unity build)`.
4. Đóng Unity Editor.
5. Đảm bảo Git working tree sạch rồi chạy:

```powershell
unity build . --target StandaloneWindows64 --output-path .\Builds\win64 --timeout 3600 --format json --non-interactive
```

Không thêm `--allow-dirty-build` trừ khi bạn chủ động chấp nhận build từ các file chưa commit.

## 5. Tạo lại Scene 1 Office Art Pass

Đây là công cụ chỉnh hình ảnh của prototype cũ, không dựng mặt bằng hoặc luồng mở đầu theo [plan Scene 1 mới](scene1-office-plan.md). Chạy lại script này không có nghĩa là đã triển khai bản office mới.

Script `Assets/_Project/Editor/KuTyOfficeCliArtPass.cs` tạo một bản scene riêng, không ghi đè scene gốc và không đổi logic gameplay. Trước khi chạy, hãy đóng Unity Editor:

```powershell
unity run . `
  --editor-path "D:\Unity IDE\2022.3.62f3\Editor\Unity.exe" `
  --timeout 1800 `
  --format ndjson `
  --non-interactive `
  -- -executeMethod KuTyOfficeCliArtPass.CreateBatch -nographics
```

Kết quả được tạo tại:

```text
Assets/_Project/Scenes/Act1_RealWorld_OfficeArtPass.unity
```

Bạn cũng có thể chạy cùng thao tác trong Editor bằng menu `KuTy > CLI > Create Office Art Pass Scene`.

Khi đã xem và chấp nhận bản preview, áp dụng art pass vào Scene 1 thật bằng menu:

```text
KuTy > CLI > Apply Office Art Pass To Scene 1
```

Lần áp dụng đầu tiên tự tạo bản sao an toàn tại:

```text
Assets/_Project/Scenes/Backups/Act1_RealWorld_BeforeOfficeArtPass.unity
```

Menu này chỉ dựng lại nhóm `OfficeArtPass_CLI`, tạo các anchor độc lập cho ghế và vị trí gục xuống bàn, rồi nối lại các reference liên quan. Task system và logic gameplay hiện có không bị thay thế.

## 6. Unity Editor batchmode truyền thống

Đây là CLI có sẵn của Unity Editor, độc lập với lệnh `unity` beta ở trên:

```powershell
& "D:\Unity IDE\2022.3.62f3\Editor\Unity.exe" `
  -batchmode `
  -projectPath "E:\FPT\FALL26\PRU213\KuTy" `
  -runTests `
  -testPlatform EditMode `
  -testResults "E:\FPT\FALL26\PRU213\KuTy\TestResults\editmode.xml" `
  -quit
```

Không mở cùng project bằng GUI trong lúc chạy batchmode.

## 7. Vị trí cấu hình của project

Binding CLI nằm tại:

```text
Library/UnitySkills/cli_config.json
```

Đây là file sinh tự động trong `Library`; không cần commit lên Git. Muốn đổi quyền cho `test`, `run` hoặc `build`, dùng tab `Unity CLI` trong cửa sổ UnitySkills thay vì sửa JSON bằng tay.

## 8. Quy tắc an toàn

- Không commit license, `.ulf`, secret hoặc mật khẩu lên repository.
- Không chạy hai Unity Editor trên cùng project.
- Dùng `doctor --ci` trước test/build tự động.
- Dùng `projects verify` trước khi nhập nhiều asset mới hoặc trước khi build.
- Unity CLI vẫn là beta; chạy `unity <command> --help` trước khi dùng cờ mới.
