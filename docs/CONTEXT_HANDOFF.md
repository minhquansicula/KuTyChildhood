# Bàn giao ngữ cảnh KuTy — cập nhật 10/11/2026

## 1. Đọc trước khi làm tiếp

- **Mới nhất: đọc mục12a-15 trước — user yêu cầu theo plan `C:\Users\LENOVO\.opencode\plan\KuTy-Simulator-UX-IMPLEMENT-HANDOFF.md`: Đợt1 quay lại dọn mâm làm mẫu mỹ thuật/UX, CHỜ REVIEW trước nhân rộng.** Đã có ảnh before/after + clip GameView,499assertionsPASS; không là nghiệmthuUX. Sau đó đọc12a-14→12a-13→12a-12→12a-10 (4trò+thư,5task50mảnh,Act_Ending khôngScene3),12a-9/12a-8. Mọi thay đổi vẫnlocalchưacommit/push.
- Người dùng giao tiếp bằng tiếng Việt.
- Workspace: `E:\FPT\FALL26\PRU213\KuTy` — Windows / PowerShell.
- Unity 2022.3.62f3, URP. AGENTS.md yêu cầu dùng **Unity Skills** khi làm project Unity.
- Load skill `unity-skills` trước khi thao tác Editor. Không đoán tên/parameter REST skill.
- Không discard/reset các thay đổi hiện có. Gameplay local cần thiết đã được commit trước merge; các thư mục công cụ/Blender còn ngoài phạm vi vẫn giữ nguyên, không tự add toàn bộ.
- User đã chốt giữ khu rửa chén local/bốn minigame, nhận phòng khách của Đạt và yêu cầu merge cả Thuận + Đạt, kiểm Scene2. Đã thực hiện ở mục12a-6; **chưa push**. Không suy diễn merge/QA thành nghiệm thu tay mẹ, xóa công việc local hoặc cho phép push.
- Hiện không còn Plan mode; vẫn tuân thủ mode mới nếu phiên tiếp theo có thông báo khác.
- Không tự spawn subagent nếu chưa được user hoặc hướng dẫn áp dụng yêu cầu.

## 2. Phạm vi người dùng đã chốt MỚI NHẤT

**CHỈ LÀM SCENE 1 VÀ SCENE 2. CẮT SCENE 3 KHỎI LUỒNG CHƠI.**

- Không tiếp tục đầu tư shop, bắn bi hoặc nhiệm vụ Scene 3.
- Giữ file/source Scene 3 để dự phòng, không tự xóa asset hàng loạt.
- Luồng đề xuất đã trao đổi:
  `MainMenu → Act1_Office_Blockout → Act1_CutScene_Act2 → Act2_MemoryWorld_Home → đọc thư mẹ → Act_Ending`.
- Cắt Scene 3 **CHƯA IMPLEMENT**. Build Windows hiện tại vẫn bao gồm Scene 3.
- User muốn simulator tuổi thơ: thao tác đồ vật trực tiếp, giữ không khí phụ mẹ làm việc nhà.
- User cho phép dùng Blender, nhưng Blender chạy trong một phiên MCP riêng, không biết scene Unity.

## 3. Git hiện tại

- Branch `main`, đồng bộ `origin/main` ở commit `e59bf09`.
- Commit này đã merge `origin/dat-dev/feature/scene2` tới `d56c88c` từ lần làm trước.
- Sau đó có nhiều thay đổi LOCAL chưa commit/push: chiều cao, giường, simulator.
- Đã xác nhận lại `git status` khi tạo tài liệu này; không fetch remote ở bước bàn giao.
- Có thư mục mới `Tools/SimulatorAssets/` do công việc Blender ở phiên khác. **Không xóa/ghi đè.**

## 4. Những việc đã sửa trước simulator

### Chiều cao
- `Scene2MotherDemo`: root scale đồng đều `1.5`.
- Mẹ cao khoảng 2.5 m để cao hơn tủ lạnh đang cao khoảng 2.38 m trong scene, theo yêu cầu rõ của user.
- `Player`: root scale đồng đều `1.3636364` để Tý tăng cùng hệ số so với lần mẹ scale 1.1.
- Chỉ chỉnh trong Scene 2; đừng tự normalize lại chiều cao vì cho là thiếu thực tế.

### Xuống giường
- Lỗi cũ: `bedExitPoint` null, fallback cách tâm giường .55 m vẫn nằm trên mặt nệm.
- Đã thêm `BedroomArea/BedExitPoint` ở world `(8.8, .05, 2.414544)` và gán vào BedWakeUpManager.
- `BedWakeUpController.RiseFromBed()` chỉ nâng camera ngồi dậy .18 m theo world-space,
  không nâng tới standing pose trên nệm; sau màn đen mới chuyển tới điểm xuống sàn.
- Mở rộng `ChildWakeHandsSmoke` kiểm tra chân xuống sàn, không đứng trên nệm.
- Test đã PASS, kể cả lượt kiểm tra cuối sau khi làm simulator.

## 5. Simulator hiện đã IMPLEMENT

Đây là **bản gameplay đầu tiên / simulator-lite**, chưa phải toàn bộ polish mỹ thuật và animation.

### File mới
- `Assets/_Project/Scripts/Systems/HouseholdWorkSession.cs`
  - Camera/input lease: snapshot parent, local pose, FOV, near clip, HUD và khôi phục khi thoát.
- `Assets/_Project/Scripts/Systems/HouseholdSimulatorState.cs`
  - Luật và dữ liệu serializable, tách khỏi input/visual.
- `Assets/_Project/Scripts/Systems/Scene2HouseholdSimulator.cs`
  - Raycast/gesture, hoạt động, pause, save, completion callbacks, instruction UI.
- `Assets/_Project/Scripts/Systems/Scene2HouseholdSimulator.Visuals.cs`
  - Sinh mesh/material nhẹ ở runtime, workstand, đạo cụ tạm, cleanup.
- `Assets/_Project/Editor/Scene2SimulatorSetup.cs`
  - Installer additive/repeatable cho 7 station, không rebuild nhà/nhân vật/bếp.
- `Assets/_Project/Editor/Scene2SimulatorChecks.cs`
  - Play Mode test qua cùng ProcessPointer/raycast path với input thật.
- Các file mới trong Assets có `.meta`, cần giữ khi commit sau này.
- Hướng dẫn chi tiết: `docs/scene2-household-simulator.md`.

### 7 hoạt động 3D mới
1. ArrangeKeepsakes: kéo/xoay/đặt ba vật vào vùng tương ứng.
2. FetchWater: múc tại nguồn, kéo gáo, lăn chuột nghiêng để đổ.
3. MeasureRice: đong ba ca, chỉ kéo sang đích không tự đổ.
4. SetTable: đặt ba đồ vật lên vùng mâm/bàn.
5. WashDishes: làm ướt → xà phòng → chà ba vùng mỗi mặt → lật → tráng → giá; ba chén.
6. PutAwayDishes: đặt đồ vào vùng tương ứng.
7. ReadLetter: mở phong bì, mở giấy, đọc ít nhất bốn giây rồi tiếp tục luồng thư cũ.

### Những phần được tận dụng
- `Scene2RadioKitchenSequence`: radio và mang củi có sẵn, giữ nguyên.
- `Scene2KitchenHandsOn`: vo gạo / nhóm lửa / nấu cơm 3D có sẵn.
  - Đã chuyển sang ưu tiên chuột; `F1` bật/tắt keyboard assistance.
  - Thêm lưu dở lượt vo, củi, than, nhiệt, nhiên liệu và cơm.
- Mini-game tùy chọn vẫn là bản cũ; bắn bi/văn phòng chưa được redesign ở lượt này.

### Quyền sở hữu tiến trình
- `HouseholdChoreFlow` là nơi duy nhất hoàn thành nhiệm vụ/thưởng.
- `HouseholdChoreInteractable` route tới simulator trước, giữ fallback bảng 2D khi không được handle.
- Không đổi enum IDs hoặc thứ tự nhiệm vụ tùy tiện.
- `Scene2Checkpoint` bổ sung `Work` (string JSON) và `WorkChore` (int).
- `KuTyCheckpointTestScope` đã backup/restore thêm hai khóa này.
- ResetHomeChores gọi simulator.ResetWork để không tái dùng state đã hoàn thành.
- Chỉ có một current-work payload; tiến sang required chore khác sẽ clear payload.

### Giới hạn cần nhớ
- Ban đầu đạo cụ mới là procedural placeholder. **Hiện bộ rửa chén/chén-đĩa đã thay bằng Blender, xem mục 12**; các đạo cụ khác vẫn procedural.
- Chưa có animation tay cầm riêng/IK; tay đi bộ được ẩn theo input lock hiện có.
- Rương/tủ chưa mở cánh/nắp thật, hiện là vùng xếp đồ 3D trên work surface.
- Chưa mô phỏng nước/vải vật lý, chưa làm spill damage.
- Chưa nâng cấp hoạt động tùy chọn hoặc thêm thoại mới của mẹ.
- Không gọi bản này là đã hoàn thành toàn bộ kế hoạch polish.

## 6. Kiểm thử và build ĐÃ CHẠY

- `KuTy > Tests > Check household simulator`: **PASS 717 assertions**.
  - Luật, ray/gesture, cản bởi tường, 16:9/21:9, pause, lưu dở/reopen,
    camera/lens/input restoration, thưởng một lần, reset cache.
  - Đây là scripted tests, KHÔNG thay thế human mouse-feel/playtest.
- `KuTy > Tests > Check hands-on rice and cooking`: **PASS 39 assertions**.
  - Sửa test chờ điều kiện kết thúc xuống giường thay vì đoán animation 1.2 giây.
- `Tools > KuTy > Tests > Child Wake Hands Smoke`: **PASS**, gồm chân ở sàn.
- Windows development build mới nhất thành công **0 errors / 0 warnings**:
  `Builds/Simulator-20261108/KuTy.exe`.
- Build gồm 6 scene, **vẫn có Act3** vì yêu cầu bỏ Scene 3 đến sau bước build.
- Build lần đầu mất ~19 phút vì shader; incremental sau đó ~15 giây.
- Direct3D11 standalone startup đã quan sát 35 giây không thấy startup exceptions;
  chỉ process test được dừng sau cửa sổ quan sát. Chưa playtest hết game trong player.
- Probe `-nographics` trước đó không phù hợp URP và có shader warnings; không coi đó là test hình ảnh hợp lệ.
- Lần build đầu có cảnh báo mesh môi trường cũ `Mesh_0` >2 triệu tam giác; chưa sửa mesh này.
- Log/screenshot simulator local:
  `Tools/ProjectAudit/simulator-checks.txt`, `Tools/ProjectAudit/Simulator/`.
  Hai đường dẫn đã được thêm vào `.gitignore`.
- Atlas font/prefilter URP và legacy preview PNG do validation ghi lại đã được khôi phục,
  không để lẫn vào diff tính năng. Bản auto-generated được giữ dưới Builds/.../validation-generated-backups.
- Sau test: Editor đã thoát Play Mode, scene Home không dirty, checkpoint test backup đã được restore/xóa.

## 7. BỎ SCENE 3 — việc còn phải làm

`Assets/_Project/Scripts/Systems/QuestManager.cs` đã có:
- `[SerializeField] private bool skipScene3 = false;`
- `HouseholdDestination => skipScene3 ? SceneNames.Act_Ending : SceneNames.Act3_OutSide`.
- CompleteHouseholdChores có nhánh skip: advance Ending và MarkFinished.
- ContinueOutside dùng HouseholdDestination.

Cần kiểm tra/triển khai thay vì xóa scene ngay:
1. Bật route bỏ Scene 3 trong cấu hình thực sự được dùng khi bootstrap/Play riêng scene.
2. Bỏ Scene 3 khỏi enabled Build Settings, giữ source dự phòng.
3. Sửa objective/lời thoại ra sân, mua hàng, chơi bi; xem lại thưởng 2000đ không còn mục đích.
4. Xử lý Continue cho save cũ ở Outside để không resume vào cảnh đã cắt.
5. Điều chỉnh kết thúc cho hành trình Scene 1–2; giữ cutscene trung gian và Ending nếu user không đổi ý.
6. Cập nhật regression tests đang kỳ vọng outdoor là default; test cả thư và ending handoff.
7. Build/test lại sau đổi scope. Không dùng build cũ làm bằng chứng scope mới.

## 8. Blender MCP / phiên độc lập

- User cung cấp session `ses_e4594c61bffe2K6DmcDvuWbYvC` để giao việc Blender.
- Thử functions.subagent với sessionID bị từ chối: **not a child of current session**.
- Không có lệnh nào được gửi thành công từ phiên Unity này sang phiên đó.
- Không tìm cách đổi quan hệ phiên hoặc vượt giới hạn này.
- Người dùng đã được cung cấp prompt độc lập để dán sang Blender MCP.
- Prompt yêu cầu source riêng, không đọc/sửa Unity, không push; dựng từng bộ rồi dừng để duyệt.

## 9. Bàn giao Blender ban đầu — ĐÃ TÍCH HỢP ở phiên tiếp theo, xem mục 12

Đã xác nhận có thư mục và đã đọc `asset-report.txt` ở:
`Tools/SimulatorAssets/Dishwashing_Set/`.

Theo báo cáo của agent Blender (cần kiểm chứng lại khi import):
- Source/Dishwashing_Set.blend.
- Export/FBX/Dishwashing_Set.fbx và 6 FBX lẻ:
  DishBowl, DishPlate, WashBasin, ScrubSponge, SoapBar, DryingRack.
- Export/Textures/: 21 PNG 1024, 7 bộ material BaseColor/Normal/Roughness.
- Preview/: ảnh lòng chén, mặt ngoài, bộ đầy đủ; final `Preview_Set_Seated_v02.png`.
- 6 visible meshes, tổng 13.160 triangles; **105 collider proxy meshes** và 10 Empty.
- Chén .14 m đường kính / .065 m cao; đĩa .22/.02 m; thau nhôm .46/.16 m;
  sponge .09×.06×.03 m; soap .08×.05×.025 m; giá .45×.30×.25 m.
- Empty: Grip_R/L, InteriorCenter/ExteriorCenter, WaterSurface, Grip/ScrubContact, RackSlot_01..03.
- Blender coordinates Z up; FBX xuất -Z forward / Y up, global scale1, Bake Space Transform=false.
- FBX có COL_* children: **ẩn/bỏ renderers của proxy khi import**, không để lộ chúng.
- Không dùng một convex hull kín miệng chén/thau. Cũng không bật toàn bộ 105 proxy vô điều kiện:
  simulator đang dùng thao tác có giới hạn/raycast, nên chọn collision phù hợp và đo chi phí.
- Normal OpenGL +Y; Roughness linear. URP cần tạo material và chuyển roughness→smoothness đúng kênh.
- Báo cáo chỉ xác nhận Blender roundtrip, **CHƯA xác nhận Unity**.
- Ở thời điểm bàn giao ban đầu, các file này chưa tích hợp. **Hiện đã tích hợp Unity, xem mục 12.** Không xóa/ghi đè bản bàn giao.

## 10. Đề xuất bắt đầu phiên mới

Danh sách dưới đây là đề xuất ban đầu; việc import ở bước 3–6 đã thực hiện, xem mục 12.

1. Đọc tài liệu này, `docs/scene2-household-simulator.md`, AGENTS.md và load Unity Skills.
2. Kiểm tra git status, không reset công việc local.
3. Xem preview/report Blender và hỏi/chốt ưu tiên nếu cần:
   - cắt route Scene 3, hoặc
   - import bộ Dishwashing_Set thay procedural props rửa chén.
4. Khi import: copy asset được chọn vào thư mục Assets riêng có tên rõ ràng;
   không copy cả Tools vào Assets và không nhập .blend working source trực tiếp nếu không cần.
5. Kiểm chứng dimensions/orientation/pivots, dựng URP material, prefab/attachments,
   rồi bind vào simulator; luật hiện tại có hardcoded bán kính/độ cao theo bowl mẫu .28 m,
   trong khi bowl Blender .14 m, nên **không thay mesh rồi giữ nguyên hit zones/vết bẩn**.
6. Rerun tests sau thay collider/mesh/camera và cập nhật tests theo hình học thực tế.

## 11. Thông tin công cụ hữu ích

- Registry Unity: `$HOME/.unity_skills/registry.json`.
- Instance gần nhất: `ChiaKhoaKyUc_B92B4C12`, projectName `Chia Khoa Ky Uc`, port8090.
  Phải đọc registry/xác nhận lại, không mặc định instance vẫn chạy.
- Gắn `expectInstance` hoặc `expectProject` đúng vào REST write.
- Theo Unity Skills: health probe một lần cùng first request; discovery schema trước thao tác chưa biết;
  dry-run cho thao tác rủi ro; đọc waitUrl compile+reload sau script refresh.
- Python command `python` trên máy đang trỏ Microsoft Store alias, không chạy được;
  dùng PowerShell/.NET hoặc kiểm tra interpreter thực sự trước.
- Nếu câu hỏi về OpenCode, load skill opencode và dùng docs V2; không đoán API/cấu hình.
- Đừng tự hứa đã làm việc ở Blender MCP hoặc gửi lệnh sang session khác nếu tool không xác nhận.

## 12. Cập nhật sau tích hợp Dishwashing_Set — 08/11/2026

## 12a-15. Đợt1 theo plan mới — dọn mâm vertical slice CHỜ USER REVIEW — 10/11/2026

- User yêu cầu đọc `C:\Users\LENOVO\.opencode\plan\KuTy-Simulator-UX-IMPLEMENT-HANDOFF.md` vàAGENTS, khảo sátlocal rồi implementĐợt1. Yêu cầu này **đổi lại ưu tiên ở12a-14**: được sửa dọn mâm làm mẫu trước, không tiếp tục nhân rộng thêm polish sang3trò trước khi review mẫu. Đã đọc plan386dòng, Git/Editor/source/diff/handoffmới. HEAD`b35d7c1`, nhiềulocalmodified/untrackedgiữnguyên; không setup/installer/subagent/merge/commit/push/reset/stash/discard.
- Editor8090instance`ChiaKhoaKyUc_B92B4C12`,bypass/full, Home saved/EditMode đầu/cuối. Không sửa/lưu scene hoặc source materials/textures/mesh. HomeSHA256 vẫn`303137E58841DB3C05474F9C33FFC12A79EFA9174A8CDEA3BE06B1C13CA85B86`. Luồng5task50mảnh/stableIDs/migration/bốtrí/bếpnguồn/camera rửa/F/thư/endingkhôngđổi.
- Chụpbefore mới bằng suite197rồi giữ `Tools/TableUxSlice/before-ui.png`, `before-world.png`. Khảo sátmatbàn: `Assets/_Project/Art/Scene2/Materials/Kitchen/MAT_DiningTable.mat`,URP/Lit,smoothness0,5,metallicglossmap+normalmap. Tint đơn thuần không giải quyết vân cam. Trial hiện **chỉ trong hoạtđộng**: cloneMaterial,512²GPUcopyBaseMap giảmcontrast về55%,saturation78%,tint(.8,.85,.88),smoothness.12,bumpScale.25,disablemetallicmap trêncopy. Cachetexture runtime,không đọc/ghiimport/sourceasset. Close khôi phục đúng materialrefs (không ghi đè nếu reference đã được thay khác trong runtime).
- Thêm `Scene2HouseholdSimulator.TablePresentation.cs`: camera runtime tập trung vùng thao tác, tablelocaleye(0,1.065,-.83),aim(0,.015,-.13),FOV48wide/52hẹp<1,6aspect; không dời station/bàn/ghế. Lượt đầu crop quá sát làm cắt mépđĩa dù tâmPASS; đã chỉnh góc/FOV và kiểm **8góc bounds mỗi món** tại16:9/21:9/4:3, chừa mép/footer. Camera không đổi khi cầm/đặt, restorepose/FOVkhi thoát.
- UI dọn mâm: nhãn nhỏ góctrêntrái tên+0/3, actionngắn gần vật/con trỏ, footer gọn, nút`?`help và`Aa`cỡchữ+22%. Bỏ stepper mặcđịnh và bỏ worldlabels thườngtrực; idle không showmọiđích, chỉ cầm mới hiện ghostpose/guide dịu. Chén lớn/nhỏ gọi theo kíchcỡ nguồn thay vì trái/phải; mappingitem-slot/placedMaskgiữnguyên. Textcard vừanộidung, tránh che boundingbox món, help mở lại được; sau thànhcông thu gọn, khi đứng lâu nhắc nhẹ. Header/footer/help/buttons chặn UI-raycast, tooltip khôngtươngtác khôngcảnworld. Những thay đổi này chỉ table; Configure resetstyle khi rửa chén sử dụng lạiCanvas.
- Input click/drag đã có được giữ, bổ sung hoverdelay.1s,lift.16s,hạ.18s, kiểm đoạn di chuyển khôngqua solid và giữ validation-preview-commit dùng cùng kếtquả. Previewmeshkhớp pose cuối, gold/sagealpha dịu hơn thayneon. Sau món cuối giữ khungbànđãdọn .85s rồi callbacktask; **save được placedMask7 khi rời trong nhịp nghỉ**, reopen từcheckpoint xóa cache vẫn hoànthànhtask/thưởngđúng1lần. Không cần checkpoint schema mới.
- Contactsound không dùng rewardping mỗi món nữa: âm tổng hợp ngắn runtime, quietSFXbus, pitch khác plate/bowls. **Chưa có foley sứ/gỗ thu thật; không gọi âm này là foley chuẩn.** Không thay thoại/lore mẹ hoặc ending. Clipvisual hiện khôngtrackaudio; cần nghe/tune trongplaytest hoặc cung cấpfoleyhợplệ trướcnghiệmthuâmthanh.
- QA mới `Scene2TableUxSliceChecks.cs`: scoped save,3GameViewresolution tạm1280×720/1680×720/1024×768,normal+largehelptext, fullbounds, sourcepose/materialrefs/color,cameraFOV,click+drag+settle,donebeat/completecheckpointreward. Xuất**MP4GameView1280×720gồmCanvas**,140frames15fps(~9,3s), quaUnityEditor.MediaEncoder. Đã decode bằngVideoPlayer, sampleframe0/35/70/105/138 thànhPNGvàxem cácframeđầu/giữa/cuối. Nativecontainer báo139frames/9,2667s do rounding; validator cho±1frame metadata, không bỏ qua lỗi chuẩn bị/size.
- **Recording dùng Editor-only `EditorTablePointerPlayback`**, chỉ thay livepolling bằng edge `ProcessPointer` do fixture cấp để quay ổn định khi Editorbackground. Camera/UI/save/validation/reward vẫnchạy, OnApplicationFocus/Pause khôngbịbỏ; playerbuild khôngchứa flag. Đây là **scripted shared-pointer clip**, không OS-mouse/standalone hoặc first-time-userplay. Suite197 riêng vẫnkiểm stalegesture/pause/focus/invalid/occlusion/UIblocking khi khôngbật playback. Prerequisites2bếp advanceAPI; completioncallbackfixture dùng`flow.Complete(...,false)` suppressmemorytoast; không gọi clip là fullstory/endingplaytest.
- **CompileUnitySkillsCycle22success0errors; final499PASS =135slice +197fouractivities/UI +86washing +81sourcekitchen.** Suite197 chỉ đổi visualassert cho guide mảnh và đợi donebeat, không nới ruleđặt/visibility. Testscope save restored/xóa backup; GameView customsize/selection,Time.captureFramerate vàrunInBackground đượcrestore. KhôngtuyênbốConsolelịchsử/nativewarningsạch.
- Reviewevidence `Tools/TableUxSlice/README.md`: before-ui/world,material-original-same-view/material-muted-same-view,idle/hover/held/valid/placed/completed,3aspectnormal+largehelp,`table-interaction.mp4`,decodedmovieframes. Đãxem actualimages ở kíchthướcchơi (không chỉcameraRenderbỏCanvas). **DỪNG ởmẫu này, chờuserreview mỹthuật/feel rồi mới áp dụng hướng mới sang3trò.** Cần OS-mouse/ngườimới/tuneaudio; PASSkỹthuậtkhônglà“đẹpnhất” haynghiệmthuUXcuối.

## 12a-14. Ưu tiên trước: rửa chén + hai trò bếp, dọn mâm để bạn user làm — 10/11/2026

- **User nói rõ: “cái dọn mâm đó để sau đi, bạn hãy ưu tiên làm mấy cái kia tại dọn mâm là bạn tôi làm rồi”.** Đã dừng sửa phần dọn mâm; không revert patch trước đó khi chưa được yêu cầu. Không chỉnh thêm `.TableInteraction.cs`, `.Visuals.cs`, tablelayout hoặc test dọn mâm ở lượt này. Chỉ chạy suite fouractivities có kiểm table như regression. Mọi local/user work giữ nguyên; không subagent/setup/installer/merge/commit/push.
- Khảo sát lại Git/Editor:HEAD`b35d7c1`,HomeEditMode,8090đúnginstance`ChiaKhoaKyUc_B92B4C12`bypass/full. Scene không chỉnh/lưu, SHA256 trước/sau vẫn `303137E58841DB3C05474F9C33FFC12A79EFA9174A8CDEA3BE06B1C13CA85B86`. Không đổi nguồn asset/bố trí/camera rửa thấp-gần/F/5task50mảnh/enum/checkpoint/ending.
- **Rửa chén:** sửa `.AuthoredDishes.cs`, thêm `.WashingUx.cs`, helper`HouseholdActivityPointer.cs`, nối lifecycle/UI trong simulator chính. Quickclickcầm và clickđặt bên cạnh drag; offset đo trên mesh thật, hỗ trợ dock vào thau/vòi thật thay vì giữ chính xác pixel; nhả drag trả món về vùng chà an toàn. Khi đã ướt+cóxàphòng,bấm trực tiếp món để chà (không bắt click miếng rửa riêng). Chà vẫn raymesh tiếp xúc đúngpatch/mặt, capđónggóp theo dt và travel, đứng yên/nhảy xa/occlusion không chà xuyên. Không autozoom lúc chà.
- Nút **Lật món / Nhìn gần[F] / Đặt lại / Rời đi[ESC]** có enabledstate theo bước, GraphicRaycaster chặn click xuốngworld. Lật không cần cầm, tween0,28s; wheel vẫn shortcut, lock chà khi đang lật. UI click-up và đứng con trỏ trên nút không làm hủy/kẹt tween. Pause/focus trả đồ/khóa vòi/giữ vết đã chà, consume stale release. Một món được cất xong mới highlight món sau, không tự nhấc. Header Món1/3, sạch từngmặt, một primaryaction, nhắc modeđangcầm/chà.
- Vòng nhận biết món hiện tại + vùng thau/xàphòng/chà/vòi/giá, rackpreview đúngmesh/pose. Validation cất chỉ xanh+commit khi sạch, ở vùnggiá và thực sự có phầnmesh nhìn thấy. Giá có colliderbox quanh khung rỗng: refine bằng MeshCollider query đúngmesh giá; tâm chén nhỏ bị thanhgỗ che thì thử6mesh-contactanchors + exactmeshrays qua silhouette (không nhận góc bounds rỗng). **Không blanket-ignore rack/occlusion**; đã kiểm opaqueobstacle chặn cất mà không mất progress. Không đọc vertices của mesh imported khôngreadable nữa; không đổi importsettings hay bỏ visibility để épPASS.
- **Nhóm lửa:** `.Scene2KitchenHandsOn.cs` thành partial, thêm `.Ux.cs`: outline quanh3củi nguồn, ghost đúngmesh tại slot khi kéo, tự cănxoay và settle0,18s sau nhả chuột (lock repeatedinput), thả sai/hủy trởvề. Giữ chuột ở miệngbếp tự chia hơi0,55s + nghỉ0,35s; không trừ than vì giữ lâu. Keyboardassist F1/F/Space vẫn giữ. Không sửa bakedfiretexture hoặc hứa tắt từngpixel lửa.
- **Nấu cơm:** fuel drag lấy projection trục `fuelAxis` lên màn hình (vuông góc không chỉnhfuel), hànhtrình clamp0..1 trên trục hiệncó; hỗ trợnhẹ gầnfuel0,5. Nhiệt Yếu—Vừa—To có chữ, chỉnh vừa rồi thả tay vẫn nấu. Lửa quá yếu/to chỉ dừngprogress, không trừcơm đãchín. Click númvung mở có kiểmsoát0,55s hoặc giữ-kéolên vẫn hoạtđộng; pause/focus/hủy giữa chừng đóng lại antoàn; mởxong restđúng điểmđỡ hiệncó.
- **UI bếp:** xem actualscreenshot phát hiện paneldưới che củi; đã chuyển hướng dẫn lên safearea phía trên, lời mẹ ởpanel riêng bên dưới hướngdẫn, hidepanelrỗng khi SilenceMother. Không đổi nội dunglời mẹ/ending. QA kiểm textfit và controlspanel không che visiblepickpoints của3củi/fuel. Không coi tăngassertions là nghiệmthu UX; cần OS-mouse/userplay.
- **Compile UnitySkills thành công0lỗi; regression cuối364PASS =86authoredwashing +81sourcekitchen sharedpointer +197fouractivities/UI.** Reports ở Tools/DishwashingSimulator/checks.txt, Tools/ExistingAssetMinigames/SourceArticulation/pointer-checks.txt, Tools/Scene2FourActivities/checks.txt. Testhelpers cập nhật cơ chế mới (neutralrelease saupause; buttonflip qua EventSystem; wheelkhôngcầm; ticksettling; holdbreath; fuelprojection; clicklid; hot/lowpreserveprogress). Fouractivities vẫn APIadvancebếp/rửa; các suite86/81 kiểm pointer riêng; không gọi suite197 là toànluồngmouseplaytest.
- Đã xem actualUnity `01-station-ui.png`, `02-close-foam-ui.png`, `fire-ux-ui.png`, `cooking-ux-ui.png` (2ảnhbếp ở SourceArticulation). Kếtthúc HomeEditMode; checkpointtestscope restored/xóa backup. LượtQA trongquátrình cóFAIL (fixture stale release, rackcenter occluded, meshkhôngreadable), đã sửa và rerunPASS; khôngcoiFAIL làQAcuối. Chưa standalone/OS-mouse/người mới; chưa hoàn tất giai đoạn4 toànproject, fontlarge hoặc full16:9/21:9/4:3 textreview.

## 12a-13. Giai đoạn 1 Simulator Interaction & UX — dọn mâm làm mẫu — 10/11/2026

- User giao kế hoạch4giaiđoạn, bắt đầu dọn mâm trước. Lượt này triển khai **giai đoạn1**, chưa triển khai lại flow rửa chén/bếp hay toàn bộ UI giai đoạn2–4. Không subagent/install/setup/merge/commit/push. Git vẫn `b35d7c1`; nhiều local modified/untracked của user/lượt trước giữ nguyên. UnitySkills đúng `ChiaKhoaKyUc_B92B4C12`,8090,bypass/full; bắt đầu và kết thúc Home saved/EditMode.
- Đọc journal thực tế thấy các edit sau mốc9339 (có lịch sử xóa `tripo_part_23`, chỉnh rồi undo vị trí nắp). Không phục hồi theo snapshot cũ. **Không sửa/lưu scene trong lượt này**; SHA256 Home trước/sau test vẫn `303137E58841DB3C05474F9C33FFC12A79EFA9174A8CDEA3BE06B1C13CA85B86`.
- Tách partial mới `Scene2HouseholdSimulator.TableInteraction.cs`: click nhanh cầm, click tiếp đặt; giữ-di chuyển qua ngưỡng10px scale theo resolution để drag; mouse-up nhấc đồ không đặt; drag-release không đồng thời thành click; giữ offsetXZ điểm bấm, nâng nhẹ, tự căn rotation. Chuột phải trả về; UI GraphicRaycaster chặn carry/đặt và nuốt release; pause/focus cancel + chặn gesture cũ; settling0,18s khóa input lặp rồi mới `state.Place/Touch` một lần. Storage runtime khởi tạo lazy trên main thread, tránh null khi hot reload.
- Validation dùng chung snapshot cho preview/commit, entry radius0,16m/exit0,185m hysteresis, kiểm viewport và solid occlusion camera→slot/carry→slot. Không bỏ visibility để dễ chơi. Khi chưa đúng hướng dẫn đưa vào hình bóng/cách tránh vật chắn; thả sai trả về không trừ progress. Đây vẫn là điểm khởi đầu cần user playtest, không phải thông số đã nghiệm thu.
- `.Visuals.cs`: preview mesh đúng món tại slot, không collider/scripts; tint vàng/xanh đi kèm caption và highlight vật runtime bằng PropertyBlock, không sửa source material. Chén trái/phải nguồn **khác kích cỡ**, giữ slot riêng + hình bóng tương ứng; không thêm flexible mapping hoặc thay checkpoint schema/enumIDs. Không đổi table/chairs/stove/washing pose/camera, thư,5task/50mảnh,ending.
- **QA cuối333PASS =197 fouractivities/table/UI +79authoredwashing +57sourcekitchen sharedpointer**. `Scene2FourActivitiesChecks.cs` mở rộng actual ray/button-edge path: click và drag, UI blocker thật (đợi layout1frame), pause/focus/rightcancel/outsideviewport, hysteresis/solidoccluder, settling/repeatededges, partial save→xóa cache runtime→restore, camera/FOV restore, no duplicate reward. Settling test tick qua cùng `ProcessPointer(dt)` vì Editor có thể khôngfocus; không vô hiệu focus guard của gameplay. Fouractivities fixture vẫn dùng API hoàn tất2tròbếp/rửa; suite57/79 riêng kiểm pointer. Không gọi333assertions là OS-mouse/standalone/fulljourney playtest.
- Compile qua UnitySkills thành công, feedback cuối không lỗi. Có lượt đầu FAIL do UI fixture chưa layout và transient storage hotreload null; đã sửa và chạy lại197PASS. Không tuyên bố Console lịch sử/nativewarnings sạch. Testscope capture/restore save; backup `Library/KuTyCheckpointTestBackup.json` đã được restore/xóa. Diffcheck các C# đã sửa không whitespace errors; không format YAML Home.
- Đã xem actualUnity `Tools/Scene2FourActivities/Table-UI.png`, `Table-ClickHeld-Preview.png`, `Table-Valid-Preview.png`. Reports `Tools/Scene2FourActivities/checks.txt`, `Tools/DishwashingSimulator/checks.txt`, `Tools/ExistingAssetMinigames/SourceArticulation/pointer-checks.txt`. Ảnh UI hiện ở kích thước GameView khoảng1086×499, ảnh world1600×900; **chưa kiểm đầy đủ16:9/21:9/4:3 text hoặc large-font cho giai đoạn1**. Cần playtest người mới bằng chuột thật trước kết luận UX dễ hiểu; chưa hoàn tất toàn bộ kế hoạch4giaiđoạn.

## 12a-12. Review Đạt và polish tương tác dọn mâm local — 09/11/2026

- Đã fetch origin: nhánh `dat-dev/feature/scene2` từ `b64c1c5` lên `8d2a40a` (`update cutscene`). Delta chỉ `OfficeToHome_Final.mp4`, không scene/script/meta. Đây là video trước Home; chưa QA playback clip mới, chưa nhận vào local. User nhấn mạnh ưu tiên code local, phần Đạt muốn đối chiếu là bàn ăn/dọn mâm đã tích hợp từ b64c1c5.
- User cho phép hoàn thiện tương tác minigame bằng Unity Skills; vẫn không merge/commit/push. Chỉnh `Scene2HouseholdSimulator.cs` và `.Visuals.cs` trên local: vòng chỉ vị trí runtime trên bàn thật, vàng khi hover/cầm, xanh khi vào bán kính thả hợp lệ, ẩn sau đặt; chén trái/phải phân biệt; caption theo vật đang cầm và phản hồi thả sai. Không đổi bán kính nhận, pose bàn/ghế, mesh nguồn, camera rửa, policy 5 task hay route Act_Ending.
- Unity Skills instance B92/8090: compile cycle12 thành công, 0 lỗi. QA mới **282 assertions PASS**:149 fouractivities (thêm10 kiểm guide/drop),76 authoredwashing,57 source kitchen sharedpointer. Đã xem ảnh `Tools/Scene2FourActivities/SetTable-Layout.png`; report tại checks.txt tương ứng và `Tools/ExistingAssetMinigames/SourceArticulation/pointer-checks.txt`. Không gọi đây là OS mouse/standalone test hay nghiệm thu mỹ thuật toàn bộ 4 trò; không rerun ending/migration vì không sửa route/save.
- Kết thúc EditMode; checkpoint backup đã restore/xóa. HEAD vẫn b35d7c1, scene không được sửa/lưu trong lượt này; toàn bộ thay đổi trước giữ nguyên. `git diff --check` toàn workspace báo trailing whitespace trong YAML Home đang modified từ trước; không tự format scene. Ưu tiên đọc mục này rồi12a-11→12a-10→12a-9→12a-8.

## 12a-11. Chuyển context, user báo dat-dev có commit mới; lúc bàn giao CHƯA review/merge — 09/11/2026

- Yêu cầu cuối của user: **“có commit mới của git dat-dev ấy, mà giờ context lớn quá, tôi muốn qua context mới mà vẫn giữ được ngữ cảnh”**. Lượt này chỉ cập nhật bàn giao, đọc Git status/HEAD; **không fetch, không review commit mới, không merge, không commit/push và không thao tác Unity thêm**. Không diễn giải nhắc có commit mới thành cho phép tự merge/push.
- Workspace vẫn `E:\FPT\FALL26\PRU213\KuTy`; `main`, HEAD`b35d7c1`, ahead6 so với remote-tracking origin/main đang có local. Vì chưa fetch, không khẳng định đây là trạng thái remote mới nhất. Nhánh Đạt đã biết từ lượt trước là `origin/dat-dev/feature/scene2`, incoming từng merge là`b64c1c5`; **SHA/nội dung commit user vừa báo hiện chưa biết**. Không merge lại bản cũ rồi gọi là cập nhật mới.
- Working tree còn toàn bộ modified/untracked của các lượt bếp nguồn, bàn thật, rửa chén camera gần,5task/checkpointv3/thưtrongnhà/endingroute và các thư mục local trước đó (`Tools/MotherPolish/`, model/downloads/tools...). Không reset/stash/discard, không `git add .`, không chạy installer/setup một lần để “làm sạch” scene.
- **Trạng thái gameplay phải giữ khi xem commit Đạt:** đúng5task `LightFire→CookRice→SetTable→WashDishes→ReadLetter`,50mảnh, không task cũ/optional; hai trò bếp dùng bản sao mesh gốc, không BlenderKitchen root cũ; dọn mâm dùng DiningTable thật/chénđĩa có sẵn; camera rửa thấp/gần/Fcậncảnh; khu rửa/bố trí user giữ nguyên; thư mẹ nằm trên bàn tiếp khách trong nhà `bandai_khach`; **xongScene2→cutsceneThuận`Act_Ending`,KHÔNGScene3**. Checkpointv3 migratev1/v2 và legacyoutdoorContinue vềthưtrongHome. User đã nhắc rất rõ route này, không khôi phục quyết định cũ “Scene3 mặc định/skipending opt-in”.
- QA chốt trước bàn giao: **486PASS** (57bếp sharedpointer+139fouractivities/board/thư+76washing+93targeting+43actualendingjourney+78migration);6buildscenes/42sprites/0missingrefs;1552pose/prefabrecords kiểm chỉ thưvà2markerUI đổi. Home saved/EditMode,checkpointtestbackup đãrestore/xóa tại kiểm cuối lượt gameplay. **Không phải QA cho commit Đạt mới chưa review.** Chưa standalone/userOSmouseplaytest; không nghiệm thu tay/mỹ thuật mẹ.
- Tài liệu/evidence ưu tiên: `Tools/Scene2FiveTasks/README.md` + `regression-summary.txt` + ảnh `five-task-board.png`, `letter-indoors.png`, `ending-final-card.png`; tiếp theo `Tools/DishwashingCamera/README.md`, `Tools/ExistingAssetMinigames/README.md`. Các báo cáo12task/130mảnh/Scene3 trong lịch sử không còn là policy hiện tại.
- **Bắt đầu context mới:** đọcAGENTS và mục này→12a-10→12a-9→12a-8; dùngUnitySkills khi cầnEditor. Nếu user yêu cầu xem commit mới, fetch/review đúng nhánh thực tế trước, đối chiếu source/scene mới với local và lập phương án giữ gameplay/bố trí; không checkout đèHome, không nhận xóa prop/route/task cũ chỉ vì merge text không conflict. Giữ local, chờ user cho phép trước commit/merge/push nếu chưa có yêu cầu rõ. Không cần rerun toàn bộ test chỉ để đọc bàn giao.

## 12a-10. Chỉ4trò+thư; thưvàonhà; sauScene2→cutsceneThuận, KHÔNGScene3 — 09/11/2026

- User phàn nàn taskkhôngcònnữavẫncótrênbảng và thưbámassetbànđãra ngoài. Đã hỏi rõ; user chọn **“Chỉ4trò+đọcthư”**, sauđó “oke làmtiếpđi”. Policy trước giữ12bước simpleUI **đãbịthaythế**, không đem chốt cũ phủ định userlầnnày.
- Thứtự mới **LightFire→CookRice→SetTable→WashDishes→ReadLetter**,5bước×10=50mảnh, không optionalXếpDụngCụ/XayNgô. Enum15ID giữ nguyên; các componenttaskkhác disable, không xóaprop/asset. HUDTab/checklist/hint/objective/endingkeepsake theo5task. Newgame đi thẳngnhómlửa, khôngbắt xếprương/radio/gomcủi/múcnước/đonggạo/vogạo/cấtchén.
- Checkpointv3 migratev1/v2 theo nhữngtaskcòngiữthựcđãxong, khôngclamp12index→5index. Retainedpartialwork giữstableenum, removedwork khôngreopen. Testmigration cóscopeCapture/Restore đểkhôngmấtsaveuser.
- **User nhắc rõ:“scene3tôicắtrarồimàxongscene2sẽlàcutscenedothuan-devlàm”**. Đã xác nhậnassistantgiữnhầmroutecũ; sửaHome`QuestManager.skipScene3=true`, defaultmới=true, destination`Act_Ending`. Không tự bật lạiScene3 dohandofflịchsử. LegacyoutdoorsaveContinue vềHomeđọcthư, không tảiScene3; dữliệucũkhôngxóa. EndingDirector/EndingCameraRig/sceneAct_Ending củaThuận giữnguyên, khôngdựngcutscenethaythế. Đây là yêu cầu rõ đểbậtendingroute, khôngcầnchờ“opt-in”cũ.
- Thưthật`HouseLayout/SM_Letter` trướcở~`(4.72,2.36,-10.08)`dobámcollidertable ngoài. Rebind`Scene2TabletopLetter.table` sang`BoBanGhe_TiepKhach/bandai_khach`, thưhiện~`(8.587,1.193,-3.527)`đượcmặtbànđỡ. Khôngdờibàn/ghế; chỉthư/modelvà2markerReadLetter. ĐãxemảnhactualUnity`Tools/Scene2FiveTasks/letter-indoors.png`. Setup`Scene2FiveTaskSetup.Apply`đãchạy,khôngchạylại; backup`Builds/FiveTasks-20261109/Home-before.unity`.
- Sourcechính:HouseholdChoreFlow,Scene2HouseholdActivityPolicy,Scene2Checkpoint,Scene2QuestHUD,HouseholdChoreInteractable,EndingSummary,Scene2TabletopLetter; QuestManagerrouteflag. Khôngsửamẹ/camera rửachén/nguồnbếp/bốtríkhu rửa; toàn bộlocal **khôngcommit/push**.
- **QA chốt policy5task+endingroute:486assertions PASS** =57sourcepointer+139fouractivities(board/letter/support/removedprompts)+76washingcamera+93targeting5task+43fullwake/letter4s/actualendingfilm/finalcard50/50+78migrationv1/v2/v3. HandoffđãloadAct_Endingthật,chạyfilmkhônginjectskip,chờcardrevealđủ50/50và5keepsake; riêng4physicaltasksđượcfixtureadvanceAPItronghandoff,suitepointerkiểmriêng,khônguserOSmouseplaytest. 6buildscenes/42sprites/0missingref-script-GUID. `check-layout.py`1552pose/prefabrecordsPASS,chỉthưvà2markerUIchủđộngđổi,khôngdờikhu rửachén/bàn/ghế/bếp. Home saved/EditMode,checkpointbackup đãrestore/xóa,HEADvẫnb35d7c1,khôngcommit/push.
- Reports/ảnh chốtcopyvào`Tools/Scene2FiveTasks/`:five-task-board,letter-indoors,ending-final-card;regression-summary6suite. Runnerall-in-one bịtimeout giữaPlay/Editbackground nên đãchạytáchsuite/đọcreport/kếtthúcEditMode;khôngcoitimoutlàPASS. Lượtđầuhandoff40PASScònScene3 trướcusernhắc **khônglàQAendingroute**. WashingfixturelượtđầuFAILdoPickSpongeclicktoolngoàiviewlúcFcậncảnh;sửafixtureclickchénvisibleđúnggameplayFmode,khôngnớivisibility/occlusion. Scene3file/buildentry giữlegacykhôngxóa,nhưngrouteHomevàContinuekhôngtảiScene3. Giữnguyênnộidungasset/scriptendingThuận,khôngsửamẹ.

## 12a-9. Camera rửa chén gần/thấp, không dời bố trí — 09/11/2026

- User: muốn làm nhiệm vụ rửa chén “lại gần và đẹp hơn chứ đừng đứng ở cao”. Dùng UnitySkills đúnginstanceB92/8090. **Chỉ chỉnh camera runtime**, không chạy setup bố trí hoặc di chuyển thau/vòi/chén/đĩa/kệ; vẫnlocal,khôngcommit/push.
- `Scene2HouseholdSimulator.AuthoredDishes.cs` bỏ auto-fit fullboundingbox dẫn đến birdseye. Overview vịtrí`(-3.497,1.134,-1.867)`, pitch26,4°, FOV64; focus theo thau/fixture. F cận cảnh cáchmón0,56m, camera hơnmón~0,46m, nhìn55°/FOV48 để chà lòngchén rõ; F quayoverview đểcầm/lật/tráng/cất. Không autozoom khi cầmmiếngrửa, tránh camera tự đổi lúc user chưa yêu cầu.
- `HouseholdWorkSession.MoveTo` thêmoptionaltargetFov(default48); simulator chỉ dùng64→48 choauthoredwashing, minigamekhác giữ48. Cancel/complete restore pose/FOV ban đầu. Không đổi model/material/lighting bố trí.
- **PASS82** dishwashing saucamera (thêm5assert camera/restore so77), **PASS147** fouractivities. 16:9/21:9, pointermeshcontact thật/2mặt/3món, pause/focus/checkpoint/reward/sourcepose vẫn kiểm. Fixture sử dụngFđể chà vếtkhuất rồi trởoverview trướcPickDish/Flip; không nới occlusion hoặc bỏ vết đểPASS. Không gọi461lượt bếp làQA camera mới.
- Ảnhbefore/after actualUnity và reports `Tools/DishwashingCamera/`; đã xemoverview thấp vàcloseup. Một lượt thửfitbounds vẫn cao1,759m nên không dùng; pose cuối1,134m đãPASS. TestFramePump cần bậtApplication.runInBackground tạiEnteredPlayMode trướcRunner.Start rồirestore khiEnteredEditMode; đã sửa tooling tránh chờ RESTwake mớitick. Home EditMode/checkpoint scope restore khi bàn giao; tiếp tục giữ mọi local/user edits.

## 12a-8. Hai trò bếp đã bind sang mesh nguồn gốc; mọi thay đổi local — 09/11/2026

- User tiếp tục yêu cầu “làm tốt nhất có thể”, chọn skill phù hợp, rồi “làm tiếp đi”. Dùng Unity Skills đúng8090/instanceB92. **Không commit/push**, không sửa mẹ/nguồn Blender mẹ, không đổi khu rửa chén/phòng khách/bàn ăn.
- Đã bỏ cách nắp nghiêng `beplua_naucom` khỏi đường bind vì mặt dưới/vành không đạt. Chọn **SM_StovePot gốc** cho cả LightFire/CookRice: source closedlid tách khỏi thân/quai, sửa topology contact bằng clipping và241triangle vá theo boundary mỗi phần; nắp kín mặt dưới, không kéo theo tay nồi. Bản sao lưu `Assets/_Project/Art/Scene2/SourceKitchenArticulation/`; không ghi đè FBX/material/texture gốc, không dựng bếp/nồi/nắp procedural thay thế. Củi/mặt cơm lấy triangle/UV nguồn **SM_RiceCookingStove**. Hình dạng nguồn closedstove được dùng thay vì giữ silhouette nắp nghiêng, không nhận vơ đã giải articulation sạch cho model nghiêng.
- `Scene2SourceKitchenArticulation.cs`, `Scene2SourceKitchenBinding.cs`, `Scene2SourceKitchenLayout.cs` làm sourcecopy/provenance/bind hẹp. Root fire/cook BlenderKitchen cũ giữ inactive, không xóa. `Bind original source kitchen only` đã chạy, **không chạy lại**; có guard assets/layout/backup. Marker mới source lid rest fit8ray/clearfootprint, đáy cách mặt đỡ3mm. Scene snapshot `Builds/SourceKitchen-20261109/Home-before-source-binding.unity`.
- `Scene2KitchenHandsOn.ProcessKitchenPointer` dùng chung mouse thực/fixture: visibility và solidocclusion, đặt3củi/thổi tại miệngbếp (không thêm ống tựdựng), dragfuel/simmer/liftlid; pause/focus/cancel/checkpoint guards. Giữ4minigame, các việc khác simpleUI, thứ tự12+2/tổng130/checkpointv2.
- **QA sau bind + refine source rice:62sharedpointer,147fouractivities,77dishwashing,133targeting đều PASS**. Suite62 thao tác qua ray/button edges, chờ simmer realtime, không directPlaceKindling/Blow/Simmer/LiftLid làm giả mouseplay. Prerequisites6task chỉ advance trong fixture62; UI được suite147 kiểm riêng.6buildscenes/42sprites,0missingrefs/scripts/GUID. Chưa OS-mouse/userplay/standalone độc lập; không dùng test làm nghiệm thu mỹ thuật mẹ.
- Binding xác nhận636transform gốc không đổi localpose/parent. **Sau đó Editor có chỉnh tay tray**: journal9338/9339, `Scene2RadioKitchenWorld/WoodRestByStove/Imported delivery tray` localX`-.492`,localZ`-.642`; đã giữ, không revert. Evidence `Tools/ExistingAssetMinigames/SourceArticulation/manual-tray-journal.json`. Script `Tools/ExistingAssetMinigames/check-preserved-layout.py` audit YAMLpose/prefaboverrides, giải thích2strippedalias nồi cũ không còn cần serialize (không phải xóa prefabchildren). Không épPASS bằng bỏ qua mọi posekhác.
- Đã duyệt ảnh source-only/mặt dưới/sourcefire/sourcecook/openrice/lidrest từ actualUnity trong `Tools/ExistingAssetMinigames/SourceArticulation/`. Các lỗi đã sửa: clipped plane phải xử lý orientededge balance ở nguồn remesh; cap không kéo UV qua vùng lửa atlas; rice source cần tách textureisland và fit relief, nếu không bị cap che; source lidrest phải rayfloor thật; flushUndo trướcsave; Editorbackground test cần framepump tránh test chỉ tick khi RESTwake.
- **Kiểm cuối sau tray user mới:**461assertions PASS (62sourcepointer+147fouractivities+77washing+133targeting+42mothernavigation/conversation). Đây chỉ là test mẹ hồi quy, không sửa mẹ hoặc nghiệm thu tay/mỹ thuật. `regression-summary.txt` đã có5suite. Layout audit1529pose/prefabrecordsPASS, chỉ đúng2property tray user mới được giữ theo journal; không missing/không đổi pose gốc khác. Home saved/EditMode,checkpointbackup đãrestore/xóa. GitHEAD vẫn`b35d7c1`,mainahead6,khôngcommit/push. SHA256 RiceCookingStove vẫn`24D9A9D6EAB44780E6A5594D8C5D2AB269D40B8D8FD23D89994550C9655A8F40`,StovePot`C2838C96E87550840A112E0D7EAB531AC74FC697E8E150C777FF0401194B2084`.
- Giới hạn: vùng lửa/than của model gốc có bakedtexture, light/embers/heat vẫn điều khiển nhưng không phải dập tắt từng pixel lửa; sourceart không phải modelremesh hoàn hảo. Lịch sử cắt nắp nghiêng/FAIL686edge ở12a-7 được giữ làm evidence, **không phải trạng thái gameplay hiện tại**. Đọc README mới trước dùng reports cũ. Chưa buildstandalone hoặc OS/user mouseplaytest độc lập; Console vẫn có lịch sử/nativewarnings domainreload và negativeBoxscale củascene, không tuyên bố toàn bộConsole sạch.

## 12a-7. LỊCH SỬ — minigame trên asset sẵn có; dọn mâm đã thay local, lúc đó hai trò bếp CHƯA thay — 09/11/2026

- **Bổ sung lượt tiếp tục cùng ngày:** đã dùng Unity Skills khảo sát nắp theo mặt phẳng nghiêng + connected component, tốt hơn cắtY ở góc trên nhưng **FAIL mặt dưới**. Partition giữ đủ999.956triangle nguồn; kiểm weldededges phát hiện686cạnh mới hở ở nắp/thân. Hạ ngưỡng kéo theo vành/tay nồi. Không bind mesh lỗi và không dựng prop thay thế; LightFire/CookRice vẫn BlenderKitchen. `Scene2OriginalGeometry.cs` thêm exporttopology/previewđa góc/kiểmseam/guardnguồn, `Tools/ExistingAssetMinigames/survey-lid.cjs` phân tíchread-only. Evidence `Tools/ExistingAssetMinigames/ArticulationSurvey/` và README giải thích rõ. Compile116success,636transform giữlocalpose/parent,Home khôngdirty; không đổi scene/gameplay/FBX trong lượt tiếp tục,khôngcommit/push. Không chạy lại147/133/77,không gọi khảo sát làQA chuyểnbếp. Bước tiếp cần xử lýtopology đường tiếp xúc trên bản saoassetgốc và kiểmunderside trướcbind; không lặp cắtngang/giảmplane vô hạn. Guard ban đầu soRendererAABB vớivertexbounds gâyexception,đã sửa và report chạy lại thành công.
- User yêu cầu check lại minigame, bố trí/chơi trên asset có sẵn, không dùng asset assistant dựng; tiếp theo nhắn “oke giờ bạn làm tiếp đi”. Giữ4minigame/flow cũ, không tự bỏ nhiệm vụ; lượt này **không commit/push**.
- Đã đọc source và inventory live Editor qua Unity Skills đúng8090/instanceB92. Snapshot `Builds/ExistingAssets-20261109/Home-before.unity`, source-only previews/inventory/connected-part report cùng thư mục. Không mở worktree hoặc chạy installer tổng.
- **Dọn mâm đã thay:** `Scene2AuthoredTableLayout.cs`, nhánh `BuildPlacement` reuse exactmesh/material/scale New_Bowl/New_Plate/DishBowl trên **DiningTable thật**. Bỏ phát sinh workstand/đũacube/nồiprocedural trong nhánh này, giữ source legacy không xóa. `HouseholdChoreFlow.EnsureDiningTableTarget` ưu tiên bàn authored thay `tripo_part_3`. Chỉ dời surface/view marker của SetTable; không dời bàn/ghế, chỗ rửa hoặc đồ gốc. Món bắt đầu/sau đặt cách mặt bàn support vài mm, fitfootprint.
- Setup `Scene2ExistingAssetReview.BindTable` đã chạy và lưu; **không chạy lại**, có guard report. Lần đầu tìm target EditMode không có vì Flow tạo runtime gây exception và scene dirty bởi chính setup; đã hoàn tất đúng phần pending, không discard userwork. RESTmenu executed không bảo đảm callback không lỗi; luôn xem log/report.
- **QA mới sau đổi bàn:**147fouractivities (thêm kiểm exactsource/noprototype/support/wrongdrop),133targeting (đã chuyển E bàn thật),77dishwashing. Compilecycle109success. Checkpoint scoped restore. Đã xem `Tools/Scene2FourActivities/SetTable-Layout.png` actualUnity: bàn ăn hiện có/chénđĩa thật; không bàn thao tác phủ lên. Fixture147 vẫn gọi trực tiếp PlaceKindling/Simmer/LiftLid cho phần bếp, không tự gọi đây là toàn bộ mouseplaytest.
- **Hai trò LightFire/CookRice CHƯA đổi khỏi BlenderKitchen.** Survey model bếp gốc: StovePotVisual một connectedmesh, beplua_naucom5nhóm nhưng nắp/thân vẫn liền trong nhóm lớn; tách ngưỡngY kéo theo tay nồi hoặc làm mất vành. `Scene2OriginalGeometry.cs` chỉ preview bản sao triangle/UV nguồn; CHƯA xuất/bind mesh tách vào gameplay. Không nghiệm thu phần nắp tách hay báo tất cả4trò đã dùng assetgốc. Tiếp tục giải articulation nguồn trước khi thay bếp đang chơi được; không tự dựng bếp/nắp mới trái yêu cầu.
- Báo cáo `Tools/ExistingAssetMinigames/README.md` và evidence cùng thư mục. Các thay đổi minigame/source/scene/docs trong lượt này còn **local chưa commit** trênHEADb35d7c1. Các thư mục untracked trước giữ nguyên, không add/reset/stash. Chưa standalone/user mouseplaytest độc lập, không sửa mẹ/Blender/phòngkhách.
- **Yêu cầu cuối trước đổi context:** user muốn giữ ngữ cảnh sang context mới, không yêu cầu làm thêm gameplay/Git. Đã cập nhật tài liệu này; không tự hiểu bàn giao là cho phép commit/push hay đã chấp nhận phần bếp còn dở.
- Kiểm cuối trước bàn giao:6buildscenesPASS,42sprites,0missingreferences; `Library/KuTyCheckpointTestBackup.json` không còn (scope đã restore). Git vẫnHEAD`b35d7c1`, `main` ahead6 so vớiorigin/main, không mergepending. Source/scene/tests/docs của lượt assethiệncó còn modified/untracked, không discard. Report cũ trong `Tools/Scene2Integration/` được giữ làm bằng chứng bản sau merge, report mới trong `Tools/ExistingAssetMinigames/` mới áp dụng cho bàn đã chuyển.
- **Bước tiếp hợp lý:** đọc12a-7 rồi12a-6/12a-4/12a-3, nạpUnitySkills, xác nhận liveEditor/Home và các thay đổi user mới. Tiếp tục tìm cách articulation bếp/nắp/củi trên meshnguồn có sẵn, xem hình ảnh trước bind; không chạy installer hoặc thay bằng prop tự dựng. Sau khi đổi bếp cần kiểm thao tác pointer thật/sharedinput, pause/focus/cancel/checkpoint, nắp có mặt đỡ, regressionrửachén/dọnmâm và giữ toàn bộ localpose user. Không dùngPASS147 làm chứng nhận rằnghai trò bếp đã dùngmodelgốc.

## 12a-6. Đã merge dat-dev + Thuận, QA Scene 2; CHƯA push — 09/11/2026

- User chốt ưu tiên **bố trí local khu rửa chén**, giữ bốn minigame đã nói (LightFire, CookRice, WashDishes, SetTable), nhận **phòng khách theo commit mới của Đạt**, rồi yêu cầu: “giờ bạn hãy merge của thuan-dev và dat-dev, check lại toàn bộ scene 2 cho tôi nhé.” Đã cho phép tiến hành merge; không còn chờ chốt như12a-5.
- `main`: baseline local gameplay/assets `0739a2d`; merge Đạt `5b288e9` (incoming `b64c1c5`), merge Thuận `7dd6d8a` (incoming `30190e9`, nhánh thực tế `origin/ThuanND_Scene_1`). Không merge Nhan hoặc nhánh khác; **CHƯA push**.
- Trước merge: snapshot các file local (trừ MotherPolish chỉ inventory/hash) và manifest 3.571 file tại ignored `Builds/Integration-20261109/`. Không reset/stash/discard. Baseline commit chỉ stage148 đường dẫn Assets đã kiểm, không `git add .`, không đưa thử nghiệm MotherPolish vào.
- Scene Đạt resolve **có chọn lọc**: giữ Home local làm nền, nhận toàn bộ37 file ngoài Home của Đạt; qua Editor additive scene lấy `BoBanGhe_TiepKhach`, cập nhật DiningTable/DiningChair_02 và thêm hai ghế theo world pose incoming. Ba ghế bị thay giữ inactive thay vì xóa object/reference. **Không nhận các thay đổi/xóa đồ bếp, kết cấu ngoài phạm vi phòng khách**; giữ cả kitchen/lid/rest marker/rửa chén/chồng chén. Không đặt conflict markers vào Assets; không chạy helper destructive của Đạt.
- `Scene2BranchIntegration.cs` hỗ trợ capture baseline, copy phòng khách một lần, thêm4MeshCollider mới, verify toàn bộ transform/reference/material và chụp ảnh. Navigation mẹ đã bake lại theo bố trí mới bằng menu keep-layout. Source/controller/model/Blender mẹ không đổi.
- Merge Thuận nhận ending/voice/summary và thay đổi waterHit layer. Giữ policy4minigame/UI đơn giản; không nhận thay đổi pot assist cho phép bỏ qua vật cản theo tên hoặc không thấy nồi. `EndingSummary.Capture` đã kiểm giữ kết quả Home sau khi chuyển scene.
- **QA sau merge:480 assertions PASS**:129 fouractivities,77dishwashing,42mother navigation,84chapterhandoff,127targeting,8routing/checkpoint,13guidance.6buildscenes PASS missing GUID/script/serializedref,42sprites hợp lệ. Compile cuối cycle100 success.
- Test handoff mới `Scene2MergedJourneyChecks.cs`: chờ thức dậy6,5s thật, E/simple UI, đọc thư confirm4s thật,130mảnh,2000đ một lần, checkpoint ra sân, ending summary giữ sau unload. **Fixture advance4physicaltasks trực tiếp**, không gọi là mouse-playtest toàn luồng; các suite129/77 kiểm thao tác vật lý riêng.
- Targeting cũ ban đầu FAIL do kỳ vọng aimassist20° của minigame vo gạo đã bỏ và chờ xuống giường1,1s ngắn hơn rising+fade mới. Đã sửa fixture theo policy, chờ1,5s, tránh modal reward, guard input, hủy đúng Kitchen.End; mở rộng thêm nhóm lửa/nấu/rửa. Sau sửa127PASS, vẫn giữ chặn tường/xa/nhìn lệch/directray và hai vị trí đứng hợp lệ mỗi target. Không sửa bố trí để làm test pass.
- Bảo toàn qua Editor: **629 transform gốc còn đủ,627 giữ localpose/parent; chỉ2diningpose chủ động đổi theo Đạt**. Hash **3.257 file MotherPolish nguyên**, Blender nguồn đúng SHA256 ở12a-1, không mất file local ban đầu. Checkpoint scope đã restore; Home saved/Edit Mode khi bàn giao.
- Báo cáo và ảnh đã xem: `Tools/Scene2Integration/README.md` + reports/ảnh cùng thư mục; evidence/backups chi tiết vẫn ở `Builds/Integration-20261109/`. Ảnh phòng khách2góc và rửa chén lấy từ actual Unity, không ảnh sinh AI.
- **Giới hạn:** chưa standalone/user mouse-playtest toàn luồng/video playback end-to-end; không nghiệm thu tay/mỹ thuật mẹ; không chạy smoke856/39/voice/completeflow legacy giả định cũ. Console có lịch sử lỗi cũ và native assertion domainreload giữaPlay/Edit (đã có từ trước), không tuyên bố hoàn toàn sạch. Bảy suite mới đều PASS có guard lỗi trong runtime.
- Các mục2/3 phía đầu tài liệu và12a-5 là lịch sử tại thời điểm viết; thông tin Git hiện tại ưu tiên mục này. Lượt merge không tự đổi route: giữ mặc định ra Scene3, skipending vẫn opt-in, không tự thực hiện đề xuất cắt Scene3 cũ.

## 12a-5. LỊCH SỬ — review commit dat-dev; lúc đó CHƯA merge/commit/push, chuyển context

### Yêu cầu và quyết định còn chờ
- Sau khi xong luồng4minigame/nắp nồi, user yêu cầu: **“check commit với của dat-dev… được thì merge và push lên git”**. Đây là cho phép có điều kiện, không phải cho phép ghi đè mọi bố trí hoặc add toàn bộ file untracked.
- Assistant đã fetch/review và phát hiện scene trùng sửa. **CHƯA merge, CHƯA tạo commit, CHƯA push; CHƯA thay source hoặc scene đang làm bằng bản dat.**
- Đề xuất cuối đã gửi user: commit phần gameplay/local đã hoàn thiện (**không đưa thử nghiệm MotherPolish lên**), ưu tiên bố trí local ở vùng trùng, nhận phần mới của Đạt, kiểm tra rồi merge/push `main`.
- **User CHƯA đồng ý phương án đó**; câu tiếp theo của user là muốn sang context mới mà giữ ngữ cảnh. Không tự suy diễn yêu cầu bàn giao thành xác nhận commit/resolve/push. Phiên tiếp theo hỏi/chốt phạm vi trước khi tiếp tục Git write.

### Git và nhánh chính xác
- Working directory gốc vẫn `E:\FPT\FALL26\PRU213\KuTy`, local branch `main`, HEAD **`b5470280953507e8d7d8e31ab8ae9cccd5f00b17`**.
- Remote `origin`: `https://github.com/minhquansicula/KuTyChildhood.git`.
- Đã chạy **`git fetch origin --prune`**. Khi fetch, `origin/main` vẫn `b547028`; `origin/dat-dev/feature/scene2` tiến tới **`b64c1c52fa4be880af4552a37bb05eea53b3dbf8`**.
- Incoming chỉ **1 commit**: `feat(scene2): update cutscene pacing and add living room furniture`, tác giả DatNguyenSE, 09/11/2026 04:18 +0700. `origin/ThuanND_Scene_1` cũng được fetch tới `30190e9` nhưng **không thuộc yêu cầu đang review**, không tự merge nhánh đó.
- `main` có nhiều modified/untracked của gameplay vừa làm và công việc local trước phiên (mẹ/nav, lu/nước, dishwashing, model/texture Downloads, chồng chén, policy UI, source/tools/docs). Không reset/stash/discard hoặc `git add .` mù. Đặc biệt `Tools/MotherPolish/` chứa nhiều candidate/render/source lớn chưa nghiệm thu, không thuộc commit gameplay đề xuất.

### Worktree và bằng chứng review
- Đã tạo worktree riêng **`E:\FPT\FALL26\PRU213\KuTy\Builds\DatReview-20261109`**, branch **`integration/dat-review-20261109`**, HEAD b64c1c5, tracking `origin/dat-dev/feature/scene2`. Đây là checkout sạch của dat để đọc, **không chứa local gameplay**, không phải bản đã tích hợp.
- Lần `git worktree add` đầu timeout120s; sau khi xác nhận không còn process git, đã bỏ **chỉ stale index.lock của worktree do phiên này tạo**, `read-tree HEAD`, `checkout-index --all --quiet`, unlock. Kết quả cuối `git -C Builds/DatReview-20261109 status --short --branch` sạch; **không còn trạng thái initializing/locked**. Không lặp lại create/repair nếu đã sạch.
- Không chuyển session working directory sang worktree, không mở Unity Editor của worktree. AGENTS của worktree cũng yêu cầu Unity Skills. Không dùng các QA local129/77 như chứng nhận runtime cho checkout dat hoặc scene merge.
- Evidence ignored: **`Builds/DatReviewEvidence-20261109/`** gồm `base.unity` (origin/main), `dat.unity`, `local.unity` snapshot hiện tại, `merged-preview.unity`, `SetupTiepKhachScene2.cs`, `scene-overlap.json` và log checkout. Merge preview chỉ ở thư mục này, **không đưa file còn conflict markers vào Assets**.
- Đã dùng **`git merge-file -p local base dat`** trên bản sao: exit22 và **22 conflict groups**. Cả hai phía sửa các block prefab **DryingRack, ScrubSponge, WashBasin, SoapBar, cailugao, xuonggiuong** và SceneRoots. Đây là xung đột với bố trí/props người dùng, không chỉ khác thứ tự YAML.
- Lúc xuất bằng PowerShell ban đầu có mojibake tiếng Việt; đã xuất lại source/base/dat **bằng cmd native redirection (byte-preserving)**, chạy lại merge vẫn22. Không dùng bản mojibake cũ làm source commit.

### Những gì incoming thực sự thay đổi / rủi ro cần duyệt
- 38 file: LivingRoom models/materials/textures + metas, Editor helper `SetupTiepKhachScene2.cs`, video runtime `OfficeToHome_Final.mp4`, `.gitignore` thêm `*.zip`, scene Home thay đổi lớn (+2953/-1202 toàn commit).
- Scene không chỉ thêm furniture: dời/scale nhiều props, kết cấu nhà/bếp. Incoming xóa các object/prefab như `DiningChair_01/03/04`, `TimberBaseRail (7)/(14)`, `TimberPost_Left (1)`, `WindowFrame_Left (1)`, `gianbep2`, `ToolSet`, `tripo_part_1 (1)`. Các object xóa này không bị local sửa cùng block, nên merge text có thể tự xóa mà không báo conflict; vẫn cần kiểm ý đồ/ảnh/collision/navigation.
- Helper menu `SetupTiepKhachScene2.ApplyOriginalFurniture()` dòng90–94 **xóa toàn bộ children** của `BoBanGhe_TiepKhach` rồi tạo lại, dòng124–125 tự save scene, không giữ các pose/scale user đã sửa. Không chạy helper này trên scene đang làm chỉ để “import thử”; đã review tĩnh, chưa sửa code dat.
- Incoming FBX/texture lớn (FBX40–53MB mỗi file, tổng incoming hàng trăm MB), chưa test Unity/FBX/material/clip trên worktree dat. Không nói “merge-ready” chỉ vì Git fast-forward được trên branch sạch.
- Cần chọn chính sách scene (đề xuất giữ local vùng trùng), kiểm missing references/colliders/bố trí và navigation mẹ sau tích hợp. Main remote chỉ chứa phiên bản trước local; nếu chỉ đẩy b64c1c5 thì **chưa chia sẻ các chức năng local vừa hoàn thiện**. Vì vậy cần chốt commit scope thay vì âm thầm fast-forward remote rồi báo đã tích hợp cả hai.

### Bàn giao an toàn
- Trong lượt review này chỉ đọc Git/source + fetch/tạo worktree/evidence, không đổi source Unity, không chạy lại Editor test/Blender/installer, không commit/push.
- QA hợp lệ gần nhất cho **bản local trước dat** là PASS129 luồng4minigame/nắp và PASS77 rửa chén, compilecycle90; chi tiết mục12a-4. Không coi là kết quả sau merge.
- Tiếp tục đọc mục12a-4 và12a-3 để không làm mất minigame mới, policy UI/nắp, chồng chén, scale/layout user đã đặt. Mẹ/Blender chưa nghiệm thu vẫn theo hạn chế mục12a-2/12a-1.

## 12a-4. CHỐT chỉ giữ bốn minigame, việc khác UI đơn giản; sửa nắp nồi — 09/11/2026

- User thấy nắp lơ lửng sau nấu, yêu cầu giữ nấu cơm/đặt củi/rửa chén/dọn mâm. Đã hỏi rõ và user chốt **phương án đầu: các việc còn lại chuyển tương tác/UI đơn giản, không bỏ khỏi câu chuyện**. Không diễn giải thành xóa 8 nhiệm vụ hoặc tự skip checkpoint.
- `Scene2HouseholdActivityPolicy` chọn đúng `LightFire, CookRice, WashDishes, SetTable`. Interactable mở `ShowSimpleAction` cho các việc khác, dùng nút Thực hiện/Quay lại + Enter/1/Esc; không board kéo thả/canh nhịp. Public handlers của simulator/kitchen/radio đều từ chối game đã bỏ. Giữ code/asset legacy không xóa local work.
- **Gom củi CarryWood** chuyển UI; **đặt 3 thanh/nhóm lửa LightFire** vẫn physical. Radio/wood state sau xác nhận giữ qua event, vẫn có ambience/call. Vo gạo UI vẫn dùng target nồi imported, không bật lại nồi legacy.
- Thư mẹ đọc UI, chờ4s không đếm pause rồi Đọc xong. Mở/hủy không thưởng; confirm pause/sameframe/double bị chặn. Thứ tự12 chính+2optional, IDenum, tổng130, checkpointv2 giữ nguyên; không auto-complete các việc simple.
- Nắp cũ `finishedRice` giữ offset ngang.57m+cao.22m vô hạn. Đã thay bằng `lidRestPoint`: marker `CookingLid_SupportedRest` dưới cookRoot, ray-check footprint vùng sàn trống cạnh bếp, đáy cáchsàn3mm. Lift/bob theo world-up/metres; pause/mấtfocus/hủy nhấc nửa chừng trả về nồi. Nếu không marker thì đóng nắp, không bay.
- Bind marker đã chạy qua Unity Skills instance `ChiaKhoaKyUc_B92B4C12`, port8090. Không chạy installer tổng. Chỉ thêm marker+reference, không dời/scale đồ gốc. **445 transform cũ không đổi local pose/parent**, report `Tools/Scene2FourActivities/layout-preservation.json`. Snapshot `Builds/Scene2FourActivities-20261109/before/`.
- **PASS129** suite mới (UI/policy/wood/cooking/lid/table/reading), **PASS77** rửa chén sau policy. Test snapshot/restore checkpoint riêng; không làm mất checkpoint user đang chơi trước khi thoát Play. Test không trigger xác nhận thư qua flow để tránh scene transition, chỉ xác nhận nút đọc callback độc lập. Không chạy lại856/39 legacy có giả định game đã tắt.
- Đã duyệt ảnh UI nước, nắp trên sàn sau nấu, bố trí đặt củi/dọn mâm. `Tools/Scene2FourActivities/README.md` ghi hạn chế/đường code/report. Dọn mâm vẫn dùng workstand cũ; không tự nhận đã remodel hay tích hợp mâm mới. Chưa standalone/user mouse-playtest. Compile cuốicycle90 success. Home saved/Edit Mode khi bàn giao; không reset/commit/push hoặc sửa mẹ/Blender/chồng chén.

## 12a-3. Rửa chén simulator dùng đồ người dùng tự bố trí — 09/11/2026

### Bổ sung mới nhất — chồng chén/dĩa
- User đồng ý thêm cả đồ chờ rửa cạnh thau và đồ sạch trên kệ/tủ. Đã thêm `Scene2_DishStacks`: 7 chồng/26 món (13 chờ rửa, 13 sạch), dùng mesh/material hiện có; không đổi vật gốc, không cản vòi/xà phòng/miếng rửa/giá. Kệ gốc có đồ liền mesh nên chỉ xếp thêm trên chồng sẵn có/mặt tủ, không xóa geometry.
- Đây là **set dressing**, chưa cho lấy/cất từng món mới, không tăng 3 món minigame hoặc rebind PutAwayDishes. Các anchor FreeSpace chỉ authoring. Không tự gọi chúng là gameplay cất đồ đã hoàn thiện.
- Script Editor `Scene2DishStackSetup.cs`; ảnh/report/hướng dẫn `Tools/DishStacks/`. Đã xem ba ảnh sau bố trí. Suite washing chạy lại PASS77; checkpoint test đã được bảo toàn bằng scope. Preserve: 380 transform cũ không đổi local pose/parent; snapshot `Builds/DishStacks-20261109/Home-before-stacks-100251.unity`. Không chạy lại lệnh add nếu nhóm có sẵn; không ghi đè user bố trí tiếp.

### Minigame đã làm trước khi thêm chồng

- Đã nhập 4 FBX Downloads (chỗ rửa/chén/dĩa/vòi) + embedded base/normal/metallic/roughness vào `Assets/_Project/Art/Scene2/DownloadedKitchenProps/`, texture riêng từng món tránh trùng tên; URP Lit, metallic(R)/1-roughness(A). Người dùng đã **tự bố trí lại** các object `New_*` và đồ Dishwashing cũ; không chạy lại importer/placement cũ để ghi đè.
- Yêu cầu mới: làm minigame rửa chén giống simulator trên bố trí này. Dùng Unity Skills đúng instance `ChiaKhoaKyUc_B92B4C12`, port8090; không thêm package.
- Thêm `Scene2DishwashingLayout` cùng `Scene2HouseholdSimulator.AuthoredDishes.cs`; tích hợp nhánh mới vào simulator hiện có, giữ rules/save/reward/order. Rửa 3 món nguồn **New_Bowl, New_Plate, DishBowl**, không thay bằng model mẹ/props khác.
- Gameplay: giữ kéo đồ làm ướt trong thau hoặc vòi; bấm xà phòng; kéo miếng rửa/F cận cảnh để chà mesh-contact đúng vết; cầm đồ+lăn chuột lật mặt; mở vòi tráng hết bọt; thả đồ sạch vào giá. Không tiến độ khi giữ đứng yên, sai mặt, qua vật cản, vòi đóng hoặc rack sớm. F cận cảnh giữ mode sponge khi mouse-down trên chén. Pause/focus đóng vòi; Esc giữ tiến độ.
- Bind đã chạy bằng `Scene2AuthoredDishwashingSetup`: E trên `New_WashStation`, mesh collider, disable component E WashDishes cũ (không xóa object); station `idleDressing=null` vì props trải nhiều hierarchy, runtime ẩn/restore riêng từng nguồn. `existingFixture` trỏ chỗ rửa mới; **không đổi station root/view transform cũ**.
- Chỉ thêm marker `New_Faucet/Dishwashing_WaterOutlet`; không dời faucet để khớp thau. Vòi hiện do user đặt nên nước rơi vào chỗ rửa phía sau thau, không giả vị trí vòi ở giữa thau. Dòng nước/splash là hiệu ứng, không fluid sim; chưa có hands/IK.
- Runtime clone đúng mesh/material/pose, không dịch nguồn Edit Mode; rack 3 món có spacing/bounds tránh overlap, giữ bản sạch khi complete. Reward đúng một lần theo flow; không tự skip quest trong game.
- **PASS77 assertions** `Tools/DishwashingSimulator/checks.txt`, có ray focus/Interact, strokes, occlusion, cả hai mặt/3 món, wet/soap/rinse/rack gates, F, checkpoint reopen, cancel/pause/focus, reward, source restoration. Các nhiệm vụ trước chỉ auto-advance trong test fixture, không phải hồi quy đầy đủ các nhiệm vụ đó. Checkpoint scope đã restore sau test.
- Đã xem ảnh actual Unity `02-close-foam-ui.png`, `04-all-clean.png` và các ảnh scrub/rinse. Preserve report: **379 Transform/RectTransform gốc không đổi local pose/parent, không mất transform**, chỉ thêm marker. Snapshot trước phiên `Builds/DishwashingSimulator-20261109/before/`. Compile cycle84 success; Home saved/Edit Mode. Chưa user-playtest/standalone; chưa chạy lại suite856 legacy (có giả định mesh/layout rửa cũ).
- Hướng dẫn và limitations: `Tools/DishwashingSimulator/README.md`. Test menu `KuTy/Tests/Check user-arranged dishwashing simulator`. Không reset/commit/push; giữ mọi sửa local trước phiên.
- Phần mẹ **không được sửa trong task rửa chén**. Khi đọc workspace đã thấy các candidate `Tools/MotherPolish/Source/MomUser_Polish_Candidate_v005.blend` và README/report có sẵn, cùng tiến trình Blender nền render v006, trái trạng thái handoff cũ. Đã hỏi user về nguồn phiên khác nhưng user chuyển task; không tự nhận đó là công việc mình hay nghiệm thu/import. Blender nguồn đã đối chiếu SHA256 đúng mục12a-1; giữ nguyên.

## 12a-2. Tay mẹ còn biến dạng; bàn giao sang context mới, 09/11/2026

- User phản hồi: **tay mẹ vẫn biến dạng**, muốn mẹ có hồn hơn, yêu cầu “ra lệnh cho Blender hoàn thành người mẹ”. Không dùng các kết quả PASS gameplay ở mục 12a-1 để khẳng định mỹ thuật/animation tay đã đạt.
- Đã đọc lại `Tools/MotherDelivery/USER-MOM-ARMALIGNMENT.md` và `Source/user_mom_armalignment_status.json`: bốn action đứng từng sửa hướng khuỷu, nhưng **Walk mang hậu tố ArmAlignment chưa áp dụng bản sửa đó**. Đây là manh mối cần kiểm tra, chưa phải kết luận duy nhất về nguyên nhân biến dạng.
- Đã tạo **`docs/BLENDER_MOTHER_POLISH_BRIEF.md`**: lệnh đầy đủ để giao phiên Blender. Ưu tiên chẩn đoán/sửa vai–nách–khuỷu–cẳng tay–cổ tay–bàn tay/ống tay áo, toàn bộ5clip và transitions; rồi thêm thở, trọng tâm, ánh nhìn, cử chỉ ấm áp. Yêu cầu before/after, cận cảnh/đa góc/video30FPS, FBX roundtrip và bộ xuất nhất quán Generic.
- **CHƯA gửi trực tiếp sang phiên Blender khác; CHƯA sửa rig/weights/mesh/animation theo brief này; CHƯA nhận candidate mới.** Assistant đã nói rõ điều này với user. Không tự báo “Blender đang làm/đã xong”. Các bước Blender kiểm/xuất Walk ở mục12a-1 là công việc trước đó, không phải lượt sửa tay mới.
- Brief yêu cầu làm candidate trên bản sao riêng trong `Tools/MotherPolish/`, không ghi đè `.blend/.blend1`/delivery cũ; giữ nhận dạng model, phong cách, tỷ lệ và bố trí Unity. Nếu cần đổi skeleton/bind pose, bàn giao model + mọi clip cùng rig, không ghép clip lệch vào avatar28bones cũ.
- Lúc chuyển context này, user chỉ yêu cầu bàn giao. Không chạy thêm Editor/Blender/test hoặc thay đổi gameplay. Tiếp tục bảo toàn mọi thay đổi local, không reset/commit/push. AGENTS.md yêu cầu Unity Skills khi làm Unity.
- Bắt đầu context mới: đọc AGENTS.md, mục này, mục12a-1 và brief; xác định user đã giao brief cho Blender/chưa và có candidate mới không. Nếu có, kiểm provenance/compatibility và hình ảnh trước khi import; không chạy lại installer tổng, không thay model/bố trí để che lỗi.

## 12a-1. Mẹ đi lại và trò chuyện trong nhà, 08/11/2026

### Yêu cầu và bảo toàn
- User yêu cầu hoàn thiện mẹ đi lại/nói chuyện; khi phát hiện Walk đúng rig chưa xuất, user chọn
  **“Kiểm tra bản Blender”**: được kiểm trên bản sao, xuất clip riêng nếu đạt, không sửa Blender gốc.
- Không đổi model hoặc tỷ lệ mẹ (root1.5, model1.435...), không dời props/bố trí của user.
  Không reset/commit/push/build exe. Không cài thêm AI package.
- `Tools/MotherDelivery/Source/Mom_User_ArmAlignment.blend` giữ nguyên SHA256
  `b70eb75be45a6bdaa2e87a1acdb61d6a79041806f8200252fd07603a24fb7f2b`.
  Bản sao kiểm tra: ignored `Builds/MotherHome-20261108/Mom_Walk_Review.blend`.
  Snapshot scene trước bind: `Builds/MotherHome-20261108/Home-before-bind-225003.unity`.
  Bind kiểm toàn bộ **551 transform** hiện có; đối chiếu YAML cuối **368 block Transform/RectTransform**
  explicit đều không đổi local pose. Prefab overrides/props local trước phiên vẫn giữ.

### Đã tích hợp
- Clip đúng `MomUser_Walk_InPlace_Gameplay_ArmAlignment`: kiểm49frame@30FPS, loop1.6s,
  root drift0, loop matrix error0, sole floor ~0. Đã xem contact sheet8pha trước/hông và FBX roundtrip
  (28bones/2meshes, floor min~-0.00000024m, loop0). Không dùng Mother_Walk cũ khác rig.
- Asset mới riêng `Assets/_Project/Characters/MomUserHome/`: FBX walk, `MotherHome.controller`
  clone4standingstate + `Mother_Walk`, `MotherIndoorNavigation.asset` bake từ renderer hiện tại.
  Source model/prefab/controller/standingFBX cũ không đổi. Walk dùng avatar Generic hiện có.
- `Scene2MotherHomeMovement.cs`: sở hữu riêng runtime NavMeshData instance; chọn điểm đứng gần
  activity anchors, ưu tiên chore hiện tại, có nhịp nghỉ/quay về, tốc độ~.35875m/s theo scale clip.
  Chỉ nhận full path; kiểm physics capsule cả từng đoạn path và từng bước, dừng/nhường Tý,
  dừng khi dialogue/task/pause/ReadLetter, không warp/đi xuyên/tự hoàn thành hay thưởng.
  Thêm solid capsule runtime riêng; giữ interaction trigger cũ. Tý không đi xuyên mẹ.
- **Quan trọng:** user đã dời `MomUser_Model` riêng so với empty `Scene2MotherDemo` (~2.69m).
  Trước sửa, collider/E/voice ở vị trí khác model; screenshot chỉ có props dù test root đã đi.
  Movement.Start giờ **rebase root ở runtime về XZ của model, counter-adjust model child để
  giữ nguyên world pose user đã đặt**, sau đó bind navigation/body/voice đúng người nhìn thấy.
  Edit Mode vẫn giữ nguyên transform gốc. Home runtime mới ~(-2.6788,.015,-5.7426).
- `Scene2MotherFootClearance.cs`: Generic sole probes từ source vertices/weights/bind poses,
  bù visual lift nhỏ khi blend (không đổi actor/collider/skin weights). Phát hiện blend ở scale thật
  lún6.8mm; sau correction test min~-3.64mm, lift~3.16mm, đạt ngưỡng5mm; không phải foot IK đầy đủ.
- `Scene2MotherDemo.cs`: Walk theo movement, giữ Call/Talk/Nod/Idle và pause; approval LOS bỏ
  qua own body/player để solid capsule mới không tự chặn gesture. Tạo sole correction runtime.
- `Scene2MotherCompanion.cs`: giữ E/1/2/3/Esc; thêm kiểm khoảng cách/LOS cho cả direct calls,
  small talk4nhóm theo tiến độ (12câu), phản ứng guidance gần mẹ, dừng/quay người khi conversing.
- `Scene2KitchenHandsOn.cs`: chỉ thêm cập nhật vị trí motherVoice theo anchor mỗi frame.
  Radio voice đã parent vào voice anchor nên tự theo. **Casual lines mới là phụ đề, chưa có bản thu
  hoặc lip-sync mới**; không ghép audio sai lời để giả hoàn tất voice acting.
- Setup mới `Scene2MotherHomeSetup.cs`: Prepare reviewed mother walk assets; Enable mother
  walking and conversations (keep layout); Rebake mother indoor navigation (keep layout).
  Bind đã chạy; **không chạy lại installer mẹ cũ**. Khi user đổi bố trí lớn, lưu scene rồi chỉ Rebake
  navigation mới. Physics runtime vẫn chặn đường cũ nếu geometry/bố trí đã thay đổi.

### QA cuối / trạng thái bàn giao
- **PASS 42 mother home assertions**, `Tools/MotherHome/home-checks.txt`: nguồn model đúng,
  soles49frame/loop/crossfade, rebase vẫn giữ world placement, body/E/model align, đi thực từ bếp tới
  khu dọn mâm (~9.12,.015,-3.44) và về chỗ model đã đặt, full-route collision, vật cản động, player
  không xuyên mẹ, pause, real E, quay về Tý, chặn hội thoại xa/xuyên tường, tự mở lượt đi tiếp,
  không thay chore progress/reward/scale. Có assert camera thật sự frame **visible model**, không
  chỉ empty root (để không lặp lỗi QA ảnh trước).
- Bộ cũ **MomUser standing interactions in isolation PASS**: Talk/Call/Nod/Idle, event/pause,
  không rewards/checkpoint. Report mới copy `Tools/MotherHome/standing-checks.txt`; report tracked
  cũ trong MotherDelivery được copy lại từ snapshot trước test, không dùng git reset.
- Hồi quy trên runtime cuối: **PASS 856 simulator**, **PASS 39 kitchen**. Reports ở
  `Tools/MotherHome/simulator-checks.txt`, `Tools/MotherHome/Kitchen/verification.txt`.
  Output kitchen tracked được copy lại từ `Builds/MotherHome-20261108/before-reports/` sau khi
  giữ ảnh/report mới trong MotherHome/Kitchen. Không ghi đè bản bàn giao Blender.
- Đã duyệt ảnh cuối `Tools/MotherHome/Mother-Walking.png`, `Mother-Conversation.png`,
  `Mother-Conversation-UI.png`: model thực ở đúng chỗ trong bếp/nhà chính và có hội thoại.
  Blender evidence + scripts: `Tools/MotherHome/`; hướng dẫn `Tools/MotherHome/README.md`.
- Compile cuối job `685c58a0`, cycle75 success (runtime + Editor checks). Home cuối saved/Edit Mode,
  không compile; Console hiện tại Error0; checkpoint backup scope đã được restore/xóa.
  Các lỗi test/debug trung gian được giữ trong history; chỉ các kết quả PASS cuối ở trên là bàn giao.
- Chưa playtest cảm giác bằng chuột thực từ user, chưa build standalone. Động tác đi là bước chậm,
  chưa có turn clip riêng/seated/cooking-prop IK/lip-sync; không tự gọi các phần này là đã làm.

## 12a0. Bàn giao lu cũ + nước, 08/11/2026

### Cập nhật tiếp tục — QA đã hoàn thành (ưu tiên phần này)
- Đã đọc AGENTS/Unity Skills và dùng đúng instance `ChiaKhoaKyUc_B92B4C12`, port8090.
  Không chạy lại installer/import/bind/save scene; không đổi bố trí, asset Blender hoặc các thay đổi local khác.
- Tái hiện FAIL281 bằng log từng frame: `ToggleDishCloseView` đặt `held=1` ngay lập tức,
  nhưng `Application.isFocused=False` từ đầu; Update frame sau gọi ReleasePointer, held về -1,
  `mouseUp=False`. Đây là safe release khi Editor mất focus, không phải lỗi equip.
- Sửa **test**, không sửa gameplay focus: assert equip ngay khi toggle; gọi callback mất focus thật
  để kiểm thả tool và bảo toàn dish progress; chờ zoom, toggle lại để kiểm re-equip và release.
  Không xóa assertion cầm sponge và không vô hiệu hóa cơ chế an toàn gameplay.
- Duyệt ảnh phát hiện liner nước cũ như một ống/nắp nổi: sửa runtime `WaterRice.cs`:
  clone material lu nước để bật Cull Off (không sửa shared material gốc); liner thuôn không vòng rim
  phẳng, hạ contents floor xuống Mouth-.32 thay -.20 cho lu nước. Liner cao .20, bán kính trên
  1.16*openingRadius, không vượt mép thấp phía trước. Lu gạo giữ quy tắc cũ.
  Đây vẫn là profile approximation cho mesh cũ, không phải remodel chính xác lòng lu.
- Tests bổ sung: transform world của runtime water jar khớp original (~1e-6m), material clone tách
  khỏi original, liner không có rim thay thế, nước/lót nằm thấp hơn miệng lu.
  Inspect menu báo derived water mesh max vertex error=0 so với source trong Edit Mode.
- **Kết quả cuối: PASS 856 simulator assertions**, `Tools/ProjectAudit/simulator-checks.txt`.
  **PASS 39 kitchen assertions** trên runtime code cuối; lượt sau chỉ sửa Editor test/inspection.
  Report/ảnh bếp mới: `Tools/ProjectAudit/Simulator/Kitchen/verification.txt`, `play-wash.png`,
  `play-fire.png`, `play-cook.png`. Các output bếp tracked được copy lại từ snapshot trước lượt test,
  không dùng git reset/revert. Snapshot: ignored `Builds/LegacyJarQA-20261108/before/`.
- Đã xem ảnh mới `FetchWater-Blender-16x9`, `FetchWater-Pouring`, `FetchWater-Received`,
  `MeasureRice-Blender-16x9`, `WashDishes-Blender-16x9`, `WashDishes-CloseView` trong Simulator.
  Nước/nồi/thau không còn thấy sàn xuyên qua lu; gáo nằm cạnh nồi, không bị giấu trong nồi.
  Lòng lu còn tối, liner procedural có thể nhận ra ở góc gần; không gọi là photorealistic.
  Shader vẫn Fresnel/specular/probe + gợn analytic, không refraction/simulation; chưa human mouse-feel QA.
- Cuối phiên: Home `isDirty=false`, ngoài Play, không compile. `debug_get_logs(Error)` Console hiện tại
  trả count0; history Console vẫn có lỗi cũ (không clear history để che chúng). Checkpoint scope báo
  restore; `Library/KuTyCheckpointTestBackup.json` không còn. Compile cuối job `08de295d`, cycle62, success.
- Không reset/commit/push, không build exe mới. `git diff --check` chỉ báo trailing whitespace sẵn có
  trong 4 dòng serialized Unity scene thuộc thay đổi local trước phiên; không sửa scene vì việc này.

**Các mục “đang lỗi/chưa duyệt” bên dưới là trạng thái lúc bàn giao trước khi tiếp tục, không phải kết quả cuối.**

**Đọc mục này trước các kết quả PASS lịch sử bên dưới. User yêu cầu chuyển context window.**

### Yêu cầu / phạm vi
- User không muốn dùng thúng gạo/thùng nước mới làm nguồn: phải tận dụng **lu gạo và lu nước có sẵn**;
  giữ gáo dừa/ca nhôm Blender mới. Không cần sửa Blender, không dời bố trí đã xếp.
- Yêu cầu mới nhất trước chuyển context: làm nước nhìn thật hơn (trong, phản sáng/gợn).
- Không reset/revert/commit/push công việc local; bảo toàn mọi thay đổi phiên khác và Tools deliveries.

### Đã sửa (chưa hoàn tất QA cuối)
- `Assets/_Project/Scripts/Systems/HouseholdJarSource.cs`: component trên lu gốc, chứa
  original renderer + derived openBody/openedLid mesh, mouthLocal/openingRadius.
- `Assets/_Project/Editor/LegacyTransferJarSetup.cs`: menu
  `KuTy > Scene 2 > Use existing rice and water jars`, yêu cầu Home saved/Edit Mode.
  Bind `Station.transferJar`, sinh mesh dẫn xuất trong `Assets/_Project/Art/Scene2/WaterRice/LegacyJars/`.
  Không sửa mesh/material/transform gốc. Có menu Inspect existing water and rice jars (report local).
- Đường dẫn nguồn:
  - Water: `HouseLayout/TraditionalHouseInterior/WaterJarVisual` — mesh tripo_part_10 Instance,
    bounds center ~(-6.439,.425,-.804), extents (.431,.492,.478). **Lu này đã mở sẵn**;
    bản sửa mới nhất clone toàn bộ geometry, KHÔNG cắt nắp. Mouth tính ở 94% bounds height.
  - Rice: `HouseLayout/KitchenArea/RiceJar/cailugao` — Mesh_0, bounds center ~(-6.494,.649,-2.178),
    extents (.588,.629,.594). Nắp liền cùng mesh: derived body cắt ở 76% bounds height,
    giữ phần trên riêng để đặt xuống cạnh lu khi activity. Đây là approximation, không nắp remodel.
- `Scene2HouseholdSimulator.WaterRice.cs`: nguồn dùng mesh lu tại world pose gốc; hide renderer
  gốc khi runtime copy đang dùng, restore khi cancel/reset/destroy. Bỏ bucket/basket/workstand
  khi station có transferJar. Nồi nhận/gáo/ca cạnh lu trên nền, không đổi transform lu gốc.
  Thêm inner wall/rim/floor tạm (lu 1 mặt cần mặt trong để không thấy xuyên sàn), cache mesh liner.
  Source fill theo mouth, dùng profile hình tròn gần đúng, không đo lòng lu chính xác.
- Pointer dùng MeshCollider query chính xác cho lu thay broad box; collider gốc giữ solid cho walking.
- `Scene2HouseholdSimulator.cs`: Station.transferJar; restore visibility lifecycle; caption lu nước/lu gạo.
- `Scene2HouseholdSimulator.Visuals.cs`: không BuildWorkstand nếu station.transferJar.
- Bộ thúng/thùng Blender vẫn giữ trong Assets/Tools làm fallback, KHÔNG xóa.

### Nước mới
- `Assets/_Project/Shaders/HouseholdWater.shader`: shader URP transparent, Fresnel alpha,
  main-light specular + reflection probe/environment, analytic animated normal slopes + local ripple.
  Không bật global opaque/depth textures, không refraction thực, không fluid simulation.
- `Assets/_Project/Scripts/Systems/HouseholdWaterSurface.cs`: ripple qua MaterialPropertyBlock,
  rate limit .35s; Disturb khi múc nguồn/trút vào nồi và khi nhúng chén trong thau rửa.
- `Assets/_Project/Editor/HouseholdWaterLookSetup.cs`: menu Polish household water materials;
  sửa WR_Water.mat shader sang KuTy/Household Water. BaseColor cuối (.30,.40,.36,.32),
  smoothness .94, WaveStrength .016. WaterRice importer gọi polish sau build materials.
- `WaterRiceFill` attach ripple effect khi dùng shader nước mới. Dishwashing thau dùng chung
  waterRiceAssets.waterMaterial nếu có; ripple rings cũ vẫn còn.
- Shader ban đầu lỗi Pow5; đã sửa sang pow(1-nv,5), compile/render thành công sau đó.

### Kết quả thật / blocker cuối
- Lu cũ trước polish nước: full simulator **PASS 844**.
- Lu cũ + water shader lần đầu: **PASS 843**; ảnh render bộc lộ lu nước 1 mặt bị xuyên
  và gáo nằm khuất trong nồi. Đã sửa mới nhất: liner/floor cho lu nước, home gáo/ca cạnh nồi,
  tăng alpha .32, thêm tests shader/ripple. **Chưa duyệt ảnh mới nhất**, vì test dừng trước capture cuối.
- LƯỢT TEST CUỐI **FAIL 281 assertions**, không phải PASS:
  `Close view equips the sponge without needing to click an offscreen tool`
  tại `Assets/_Project/Editor/Scene2SimulatorChecks.cs:180`.
  Test đã qua FetchWater/MeasureRice, đang ở WashDishes. Có thể ReleasePointer vì GameView
  mất focus/Application.isFocused trong thời gian wait .4s làm held=-1; cần xác minh,
  không tùy tiện bỏ test hay đổi gameplay để che lỗi. `ToggleDishCloseView` đã được gọi,
  dishZoomBlend > .99 đã PASS trước khi held check fail. Những lần trước check này PASS.
- Code cuối compile thành công job `71dfcb16` (cycle52); menu polish thành công.
- Report mới nhất `Tools/ProjectAudit/simulator-checks.txt` là FAIL281. Đọc trước khi kết luận.
- Chưa chạy lại hồi quy bếp sau lượt này, chưa build exe mới. PASS865/kitchen39 bên dưới là lịch sử.
- Test runner tự thoát Play khi Finish; cần query Editor lại, không tự giả định state khi mở context.
- Lu bind menu đã chạy, Home đã save. User có thể chỉnh scene đồng thời: kiểm tra state/dirty
  trước save/rebind; không chạy installer tổng hoặc import lại toàn bộ.
- Snapshot scene trước lu reuse: ignored `Builds/LegacyJarReuse-20261108/before/`.
  Report inspection: `Tools/ProjectAudit/Simulator/legacy-jar-inspection.txt`.
- Ảnh FetchWater/MeasureRice hiện có trong Tools/ProjectAudit/Simulator có thể là ảnh
  lượt trước khi sửa inner wall; không dùng chúng để khẳng định final look đã duyệt.

### Bắt đầu phiên mới
1. Load Unity Skills, health đúng instance `ChiaKhoaKyUc_B92B4C12`, port8090; kiểm scene/play/dirty.
2. Đọc report test fail và các source kể trên; kiểm git status, không chạm changes khác.
3. Điều tra test close-view held bị reset (focus/input); fix đúng nguyên nhân, rerun full simulator.
4. Duyệt ảnh nước trong lu/nồi/thau, tránh xuyên hình học; điều chỉnh nhẹ shader nếu cần.
5. Rerun kitchen regression, checkpoint restoration, Console; cập nhật handoff kết quả cuối.

## 12a. Lịch sử — WaterRice_Set đã tích hợp, 08/11/2026

- User đã đưa ảnh bàn giao WaterRice_Set và yêu cầu làm tiếp; đã tích hợp múc nước + đong gạo.
- Bản gốc `Tools/SimulatorAssets/WaterRice_Set` không sửa. 8 FBX + 25 PNG copy byte-identical
  sang `Assets/_Project/Art/Scene2/WaterRice`. Không import .blend/preview vào Assets.
- 8 prefab sạch (4 vật dụng, 4 nội dung tham chiếu), 8 material URP Lit, 8 map packed
  metallic/smoothness sinh ra. Toàn bộ 130 COL_* loại khỏi prefab, giữ trong source exports.
- Data: `WaterRiceAssets.asset`; scene chỉ thêm ref `waterRiceAssets` trên simulator.
  Đã so diff với snapshot đầu lượt: không sửa transform/bố trí rửa chén hoặc station nào.
- Bounds Unity xác minh: gáo (.32,.055,.11), ca (.13,.10,.09), thùng (.32,.36,.32), thúng (.38,.20,.38).
  FBX Empty cần normalize semantic axes vì corrective rotation mesh root không giữ đúng frame Empty:
  ContentsRoot/FillCenter forward physical +Y, Grip forward -Z, PourSpout -X hơi hướng lên.
  Tọa độ marker giữ nguyên từ FBX. Import report có inventory pose thật.
- Nồi nhận là prefab dẫn xuất từ **SM_RiceWashingPot có sẵn**, giữ Pot_Body + 2 Handle,
  bỏ nội dung/đạo cụ/collider cũ trong bản dẫn xuất. Không chỉnh model/scene pot gốc.
- FetchWater dùng gáo dừa + thùng; MeasureRice ca nhôm + thúng. Vùng múc dùng FillCenter,
  vùng đổ dùng PourSpout thật, nghiêng quanh grip root (positive local Z hạ mép -X).
  3 query triggers/hoạt động; query source/receiver không cướp click khỏi tool.
- `WaterRiceFill` dựng nội dung runtime từ inner-wall profiles trong handoff, clip mặt phẳng
  ngang world rồi cap kín. 4 mesh nội dung xuất từ Blender giữ làm reference prefab;
  **không render bằng cách scale dọc reference mesh**, tránh xuyên thành thuôn.
  Giữ material nguồn. Có UV/tangents/normal, ẩn khi rỗng, destroy mesh khi close.
- Đây là visual approximation, KHÔNG fluid/volume/granular solver. Khi nghiêng mạnh, pool
  hiện theo phần cavity dưới mép thấp nhất; lượng logic chỉ giảm khi đổ đúng nồi.
  Không spill penalty, không cạn nguồn, không từng hạt gạo; stream là line effect có lighting.
- Vẫn giữ workstand procedural cũ của 2 activity và E target/order/save/reward cũ.
  Chưa bố trí idle WaterRice set vào scene Edit Mode; prefab sẵn để user duyệt/bố trí tiếp.
- Source mới:
  - `Assets/_Project/Editor/WaterRiceAssetSetup.cs` — importer + QA + bind art.
  - `Assets/_Project/Editor/WaterRicePreview.cs` — 2 preview với checkpoint scope.
  - `Assets/_Project/Scripts/Systems/WaterRiceAssets.cs` — asset references.
  - `Assets/_Project/Scripts/Systems/WaterRiceFill.cs` — clipped profile-fitted visual contents.
  - `Assets/_Project/Scripts/Systems/Scene2HouseholdSimulator.WaterRice.cs` — build/input/visual transfer.
  - Đã sửa simulator controller/visuals và mở rộng Scene2SimulatorChecks.
- Menu import: `KuTy > Scene 2 > Import Blender water and rice set`. Require Home saved/Edit Mode,
  refuse overwrite imported source copy khác delivery; không reposition objects.
- **PASS 865 simulator assertions**, **PASS 39 kitchen** trên code cuối (sau RecalculateTangents).
  Gồm rửa chén authored layout cũ, 3 mức fill/profile containment/full cap area, mặt ngang lúc
  nghiêng nhẹ, real-ray pick/múc/đổ qua actual markers, wrong-target rejection, checkpoint fill,
  stream thật, hoàn thành nước/3 ca gạo, pause/reward/camera lifecycle.
- Một lượt cuối bị Play Mode của phiên khác chặn; đã hỏi user, user chọn **Dừng và kiểm thử tiếp**.
  Khi dừng thì phiên đó đã tự thoát; sau đó đã rerun simulator 865 và kitchen39 thành công.
  Có thay đổi đồng thời `ChildWalkingHands.cs` / `WalkingHandsSmoke.cs` ngoài phạm vi này;
  không đụng/hoàn tác các file đó.
- Quick preview menus:
  `KuTy > Scene 2 > Preview Blender water scooping (temporary checkpoint)`;
  `KuTy > Scene 2 > Preview Blender rice measuring (temporary checkpoint)`.
  Stop Play trả checkpoint gốc. Giữ chuột trái cầm/múc, wheel nghiêng đổ, chuột phải đặt lại,
  Esc thoát và lưu dở. Human mouse-feel vẫn cần user duyệt; chưa build standalone mới.
- Đã xác minh cả 2 preview lần lượt active đúng FetchWater/MeasureRice (`IsWorking=true`,
  WaterRiceArt bound) rồi Stop. Trạng thái cuối: Home saved, ngoài Play, không compile,
  Console0errors, `Library/KuTyCheckpointTestBackup.json` không còn (đã restore).
- Report: `Tools/ProjectAudit/Simulator/waterrice-import.txt`, `Tools/ProjectAudit/simulator-checks.txt`.
  Ảnh camera: FetchWater-Blender-16x9.png / FetchWater-Pouring.png / FetchWater-Received.png,
  MeasureRice-Blender-16x9.png / MeasureRice-Pouring.png / MeasureRice-Received.png trong Simulator.
  Đã sửa cap bị sao/răng cưa phát hiện từ ảnh rồi thêm test full cap coverage để chống regression.
- Snapshot riêng: ignored `Builds/WaterRice-Integration-20261108/before/` và before-kitchen/.
  Report/ảnh kitchen mới vào `Tools/ProjectAudit/Simulator/Kitchen`; legacy tracked outputs
  được khôi phục từ snapshot trước lượt test này. Không reset/commit/push, không cắt Scene3.

## 12b. Lịch sử gameplay rửa chén trước WaterRice

### Cập nhật mới nhất: tận dụng khu rửa cũ (sau mục bên dưới)

**Gameplay pass sau khi user tự bố trí từng món (ưu tiên thông tin này):**
- User đang đợi Blender làm WaterRice_Set; yêu cầu hoàn thiện riêng gameplay rửa chén trên
  bố trí đã tự xếp. Không import/làm WaterRice_Set ở lượt này; không di chuyển lại props.
- Runtime đọc pose/scale từ 5 object con của `Station.idleDressing`: WashBasin, DryingRack,
  DishBowl, ScrubSponge, SoapBar. Thau/giá/sponge/soap đã được user phóng to và xoay/dời;
  các giá trị hiện tại trong scene là nguồn đúng, không dùng lại tọa độ cố định của lượt trước.
- Pointer bounds, thau/nước, drop zone giá, pick collider và camera tự tính theo bố trí hiện tại.
  Bowl sạch theo slot của giá nhưng giữ scale chén riêng (không bị scale 2.35 của giá phóng to).
- Di chuyển HouseholdChoreInteractable từ idle root sang surface WashDishes để khi ẩn idle,
  OnDisable không tự cancel Begin. Menu bind idempotent không đổi transform:
  `KuTy > Scene 2 > Bind authored dishwashing gameplay (keep layout)`.
- **Không chạy lại menu Place dishes beside existing washing area nếu chỉ cần bind gameplay**:
  đó là công cụ xếp layout ban đầu, sẽ đổi station pose/view của user.
- Giữ BoxCollider cũ cho walking. Riêng pointer trong activity query exact legacy mesh bằng
  collider tạm dưới runtime world; bỏ broad box cũ khỏi ray occlusion khi đã có exact query.
  Vật cản thật vẫn chặn ray. Drop volumes thau/giá không cướp click khỏi chén/sponge.
- F: cầm sponge + nhìn gần chén; giữ chuột trái chà; F lần nữa đặt lại tool và về toàn cảnh.
  Khi cầm chén, camera tự về overview để kéo sang thau/giá. Bỏ text cố định vẽ trên sàn.
- **PASS 754 simulator assertions, PASS 39 kitchen**, Console cuối không lỗi, Home saved,
  ngoài Play Mode. Checkpoint backup đã được khôi phục bởi test scope. Chưa build exe mới.
- Tests mới gồm tìm E focus thật từ chỗ đứng không bị collider chặn, vào/cancel/resume qua
  actual Interact, complete qua callback thật và reject thưởng lần hai, so pose/scale với source,
  F zoom/equip/release, wash hai mặt, partial save/reload, camera restore.
- Ảnh mới trong `Tools/ProjectAudit/Simulator/`: WashDishes-CloseView.png,
  WashDishes-Blender-16x9.png, WashDishes-CleanRack.png. Kitchen outputs mới lưu subfolder Kitchen;
  tracked legacy ảnh/report được khôi phục từ snapshot trước test của lượt này.
- Snapshot trước lượt này: ignored `Builds/Dishwashing-AuthoredLayout-20261108/before/`.
  Không dùng nó để undo các thay đổi user đang tự sắp xếp.

**Lịch sử lượt xếp ban đầu (đã được bố trí mới của user thay thế):**
- User yêu cầu giữ khu rửa/chậu/nồi cũ, đặt bộ mới ở khoảng trống để tận dụng asset cũ.
- Đã giữ nguyên `HouseLayout/KitchenArea/Sink/DishwashingStationVisual` và toàn bộ đồ gắn liền;
  đặt bộ mới trên nền trống cạnh mép bệ, station world ~`(-1.727, .012, -.494)`, yaw -90°.
- Thêm `Station.existingFixture` / `idleDressing`; WashDishes không tạo bàn/khăn/chân bàn tạm.
  Bộ prefab idle có collider mesh tĩnh và HouseholdChoreInteractable để tương tác E, hiện trong Edit Mode;
  ẩn khi thao tác, hiện lại khi hủy/restart, không chồng bộ runtime sau hoàn tất.
- Editor mới: `DishwashingPlacementSetup.cs`, menu Place dishes beside existing washing area.
- Normal maps 7 file verified/reimport NormalMap linear, không convert grayscale;
  importer cấu hình texture trước khi import FBX, tránh cảnh báo normal slot ở lần import đầu.
- **Kết quả mới: simulator PASS 745, kitchen PASS 39**, Console cuối không lỗi.
  Số assertion simulator giảm do đã bỏ renderer bàn/chân bàn khỏi lượt kiểm từng material.
- Rice pot và một số props trong scene hiện đã có vị trí authored khác snapshot bàn giao trước.
  Không hoàn tác các vị trí này. Chỉ căn lại washView theo bounds nước hiện tại bằng menu
  `KuTy > Scene 2 > Align rice view to current pot`; vị trí nồi giữ nguyên.
- Kitchen smoke thêm Physics.SyncTransforms trước kiểm collider.Raycast và diagnostics khi không click được.
- Ảnh mới: `Tools/ProjectAudit/Simulator/LegacyWashing-Idle.png`,
  `WashDishes-Blender-16x9.png` (runtime, bộ mới + khu cũ). Ảnh top-down bị mái nhà che, không dùng để duyệt layout.
- Build exe ở mục bên dưới **chưa chứa bố trí mới này**; chưa rebuild lượt tận dụng asset cũ.
- Không commit/push, không sửa Blender gốc, không cắt Scene 3.

### Phạm vi / yêu cầu gần nhất
- User duyệt bộ Blender qua ảnh, hỏi bước tiếp theo rồi yêu cầu “làm mọi thứ tốt nhất có thể”.
- Đã thông báo và thực hiện phạm vi **tích hợp/polish rửa chén Scene 2**, giữ việc cắt Scene 3 thành lượt riêng.
- **Scene 3 vẫn CHƯA cắt**: route, save Outside và Build Settings vẫn như trước. Không tự coi build mới là bản scope Scene 1–2.
- Không reset/commit/push; các thay đổi local đầu phiên vẫn giữ nguyên. Không sửa source Blender.

### Đã làm
- Thư mục Unity: `Assets/_Project/Art/Scene2/Dishwashing/`:
  6 FBX lẻ (không import combined FBX/.blend), 21 texture gốc, 7 map MetallicSmoothness sinh từ Roughness,
  7 URP Lit materials, 6 prefab và `DishwashingAssets.asset`.
- Prefab loại bỏ 105 mesh proxy COL_* và không dùng physics colliders. Hoạt động runtime dùng 5 query triggers.
- Xác minh chén Unity 14×6.5×14 cm; chuyển axes Empty sang hệ Unity. 3 chén sạch úp đúng ở RackSlot_01..03.
- Lấy vị trí/normal của 6 vết bẩn từ bề mặt mesh thật qua raycast lúc authoring. Pointer chà nằm trên mặt chén đang lộ;
  chà ngoài chén, giữ yên hoặc nhảy pointer xa không làm sạch. Không còn dùng hardcoded geometry của chén 28 cm cho bộ mới.
- Mặt xanh miếng rửa hướng vào chén lúc chà; flip/đổi chén cập nhật ngay geometry; bọt nhỏ dần khi tráng,
  nước trong thau dùng transparency và ripple nhẹ. Không phải mô phỏng nước vật lý/IK tay.
- Bôi xà phòng lại yêu cầu tráng lại. Giữ nguyên luật save dở, thứ tự chore, một owner thưởng và enum IDs.
- SetTable / PutAwayDishes dùng chung chén/đĩa mới. Các đạo cụ khác vẫn procedural.
- Sửa `WorldLabel` trước đây chưa gán text.

### Source/editor mới
- `Assets/_Project/Scripts/Systems/DishwashingAssets.cs` — data/ref hình học và prefab.
- `Assets/_Project/Scripts/Systems/Scene2HouseholdSimulator.Dishwashing.cs` — visual/contacts bộ mới.
- `Assets/_Project/Editor/DishwashingAssetSetup.cs` — importer repeatable, không ghi đè FBX/source map đã khác bản giao.
- `Assets/_Project/Editor/DishwashingPreview.cs` — human review nhanh, dùng checkpoint backup/restore.
- `Assets/_Project/Editor/Scene2KitchenAccessSetup.cs` — sửa lối tiếp cận nồi vo gạo cũ.
- Đã chỉnh simulator/controller/visuals và mở rộng `Scene2SimulatorChecks.cs`.

### Lỗi bếp có sẵn phát hiện khi hồi quy
- Kitchen smoke ban đầu fail vì nồi vo gạo nằm trong broad solid bounds của `DishwashingStationVisual`;
  dời ra phía trước còn đụng `HouseLayout/TraditionalHouseInterior/Chair`.
- Đã dời **riêng washRoot** world delta `(0.83586264, 0, -1.1440387)` so với đầu phiên;
  sửa washView để nhìn từ phía trước, không nhìn xuyên bàn rửa. Không tắt sink collider, rebuild nhà hay đổi scale mẹ/Tý.
- So với scene local đầu phiên, chỉ thêm ref dishwashingAssets và thay 2 transform washRoot/washView.
- `Scene2KitchenSmoke` hiện target actual water/hearth/lid hit bounds thay vì đoán từ pivot FBX; thêm diagnostics vật cản.
  Vẫn kiểm tra tia E thực sự không bị chặn. Hồi quy sau sửa PASS 39.
- Không chạy lại installer kitchen tổng vì có thể ghi đè bố trí local. Menu sửa nhỏ:
  `KuTy > Scene 2 > Clear imported rice washing approach` (yêu cầu scene saved, repeatable).

### Validation mới
- Simulator sau sửa bố trí bếp: **PASS 758 assertions**.
- Kitchen: **PASS 39 assertions**.
- Quick preview xác nhận `IsWorking=true`, Blender data bound; đã thoát Play Mode và restore checkpoint.
- Windows development build: `Builds/Dishwashing-20261108/KuTy.exe`, **Succeeded, 0 errors / 1 warning**, ~76 giây.
  Warning môi trường cũ `Mesh_0` >2,097,152 triangles + Fast Midphase, chưa sửa.
- Build vẫn bao gồm 6 scene / Act3. Direct3D11 startup 35 giây không thấy startup errors/exceptions;
  chỉ dừng process test được tạo. Chưa human playtest hoặc chạy hết game trong standalone.
- Báo cáo: `Tools/ProjectAudit/simulator-checks.txt`, `Tools/ProjectAudit/Simulator/dishwashing-import.txt`.
- Ảnh render 1600×900 (không UI): `WashDishes-Blender-16x9.png`, `WashDishes-InteriorFoam.png`,
  `WashDishes-ExteriorFoam.png`, `WashDishes-CleanRack.png` trong `Tools/ProjectAudit/Simulator/`.
- Kitchen report/ảnh mới giữ trong `Tools/ProjectAudit/Simulator/Kitchen/`;
  tracked legacy outputs, font atlas và URP build prefilter sinh tự động được giữ backup rồi khôi phục để không lẫn diff tính năng.
- Snapshot file local trước khi làm trong ignored `Builds/Dishwashing-Integration-20261108/pre-integration/`;
  snapshot validation/generated backups cũng ở thư mục đó. Không dùng snapshot này để ghi đè công việc mới ở phiên sau.

### Cho user xem thử
1. Mở scene Home đã lưu, ngoài Play Mode.
2. `KuTy > Scene 2 > Preview Blender dishwashing (temporary checkpoint)`.
3. Chơi bằng chuột; Stop Play Mode để trả checkpoint gốc. Preview không save scene.
4. Cần human review cảm giác cầm/chà trước khi làm tiếp IK tay/animation hoặc các bộ Blender khác.
