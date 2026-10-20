# Scene 3 — Đường làng, ruộng lúa và tiệm tạp hóa

## Gỡ vật cản vô hình ở đường đầu làng (18/10/2026)

`JournalTable` tại (-6,5; 0,48; -0,7) đã bị ẩn MeshRenderer nhưng vẫn còn BoxCollider đặc 2,2 × 0,96 × 1,3 m. Nhân vật bị kẹt ở (-6,303; 0,28; 0,245) khi đi về hướng -Z, dù vẫn xoay nhìn và đi hướng khác được. Đã tái hiện bằng capsule cast (trúng bàn cách 3,5 cm) và CharacterController cùng kích thước người chơi: trước sửa dừng ở cả hai phía của bàn; sau sửa đi hết đoạn 2,345 m theo cả hai chiều, sai số điểm đích 2,4 cm.

Collider của bàn ẩn được tắt; collider `Journal` chuyển thành trigger trên layer tương tác cũ, giữ script/tham chiếu đọc nhật ký và raycast E. Không tắt va chạm nhà, mặt đất hoặc các ranh giới map. Lệnh editor `player_collision_fix` trong `Scene3VillageComposition.PlayerDiagnostics.cs` lưu scene sau khi kiểm tra đi bộ, ray đọc nhật ký, dữ liệu gameplay và các tuyến qua cầu/sân bi. Backup và bằng chứng trước/sau nằm ở `Tools/Scene3/VillageComposition/PlayerDiagnostics/before-invisible-journal-collision.unity` và `collision-repair.txt`.

## Vườn chuối lớn hơn, chỉ ở cuối làng (18/10/2026)

`Scene3VillageComposition.Bananas.cs` đặt **bảy** instance của `Art/Materials/Scene3/buichuoi.fbx` trong group `BuiChuoi_ChiCuoiLang`, tăng từ ba bụi nhỏ theo yêu cầu tiếp theo. Các bụi cao 3,8–4,6 m, xoay khác nhau, scale đồng đều và chân lún 5 cm. Vẫn gom phía sau dãy nhà cuối làng (tâm X từ -24,9 đến -14,8; Z từ 65,1 đến 69,8), thành hai đám đan lá tự nhiên. Không rải chuối vào các vùng khác; placeholder hoặc instance rời của cùng model được ẩn nếu có. Các lệnh polish cũ không đặt lại những instance này.

Bảy bụi dùng chung mesh gốc 49.762 tam giác và một material URP Lit lì, texture nén/mipmap tối đa 2048. Nhận bóng, không tạo thêm lượt đổ bóng; LODGroup một mức chỉ cull khi chiếm dưới 2,5% chiều cao màn hình, không giảm hay làm biến dạng hình học. Không thêm script chạy mỗi frame hoặc collider cho lá. Đã kiểm tra số lượng/vùng đặt, tỷ lệ/tiếp đất, tham chiếu gameplay và các tuyến đi qua cầu/tới sân bi. Ảnh, báo cáo và bản sao scene trước sửa ở `Tools/Scene3/VillageComposition/Bananas`; bản trước tăng kích thước/số lượng là `before-larger-banana-grove.unity`. Menu: `KuTy → Scene 3 → Place banana grove at village end`.

## Thay tiệm tạp hóa và bà bán hàng (18/10/2026)

`Scene3VillageComposition.Grocery.cs` dùng `tiemtaphoa.fbx` và `Babantaphoa.fbx` đã nhập trong `Art/Materials/Scene3`. Phần hình phác thảo cũ dưới `ShopCounter/TiemTapHoa_Visual` được ẩn; giữ nguyên component `ShopInteractable` trên `ShopCounter` và toàn bộ tham chiếu UI, danh sách hàng, giá tiền, nhiệm vụ. Root quầy cũ được đưa về scale 1 trước khi căn model bằng bounds, tránh kéo méo FBX do scale X/Y/Z khác nhau. Tiệm cao 4,5 m, bà cao 1,55 m, đứng bên hàng hóa phía trước để bảng treo không che mặt. Hình học FBX gốc giữ nguyên.

Hai model có tổng khoảng 99.422 tam giác, hai renderer dùng material URP Lit riêng được chia sẻ, texture nén/mipmap tối đa 2048, smoothness 0,12. Các collider hộp đơn giản giữ mặt hiên và kho/quầy; không dùng collider mesh cho toàn bộ tiệm. Hitbox tương tác là trigger 5,1 × 1,9 × 1,6 m phía trước, giữ khoảng tương tác chung 3 m. Bà cũng nằm trên layer Interactable, tìm được ShopInteractable qua parent.

Menu: `KuTy → Scene 3 → Replace grocery store and shopkeeper`. File bridge: `grocery_inspect`, `grocery_install`, `grocery_capture`, `grocery_test`. Backup trước thay model: `Tools/Scene3/VillageComposition/Grocery/before-new-grocery.unity`. Kiểm tra gồm tỷ lệ/tiếp đất, tham chiếu UI/nhiệm vụ, ray từ ba vị trí, CharacterController đi từ đường tới tiệm và các lối đi qua cầu. Test Play Mode bấm nút UI thật để mua hũ bi 1.500 đồng và kẹo 500 đồng, kiểm tra nhận hàng/trừ tiền, nhận ký ức và mở nhiệm vụ chơi bi. Tiền/trạng thái nhiệm vụ thử chỉ thay đổi trong Play Mode rồi thoát, không lưu vào scene. Báo cáo và ảnh ở cùng thư mục `Grocery`.

## Trẻ khoác vai quanh sân bi — cập nhật hiện tại (18/10/2026)

Đã tăng từ 5 lên **12 trẻ**, chia bốn nhóm ba bé quanh vòng bi. Mỗi nhóm đứng sát, cùng hướng vào giữa sân; hai bé ngoài đặt tay lên vai bé giữa, bé giữa khoác tay qua vai hai bạn. Các khoảng trống giữa nhóm giữ lối vào và camera chơi bi thông thoáng. Chiều cao các nhóm 1,18–1,25 m; chân đứng trên collider sân hiện có.

`Scene3VillageComposition.ChildrenCrowd.cs` tạo ba tư thế từ FBX gốc: ngoài trái, giữa và ngoài phải. Vai/khuỷu tay và hông/đầu gối được điều chỉnh bằng biến đổi có trọng số rồi bake vào mesh; chân được thu về dáng đứng. UV, texture và mặt của model được giữ, file FBX gốc không bị sửa. Mười hai bé dùng chung ba mesh tư thế và một material, không thêm Animator hay tính pose mỗi frame. Các lệnh chỉnh actor cũ nhận biết nhóm đông mới và không đặt trở lại năm bé dang tay.

Menu: `KuTy → Scene 3 → Add twelve children with arms around shoulders`. Bản sao trước sửa ở `Tools/Scene3/VillageComposition/before-children-shoulder-crowd-20261018.unity`. Ảnh cận cảnh và báo cáo ở `Tools/Scene3/VillageComposition/Actors/ShoulderGroups`; ảnh tổng thể ở `Actors/01_TreChoiBi.png`. Đã kiểm tra 16 cặp tiếp xúc tay–vai trên vertex thật (khoảng cách nhỏ nhất của mỗi cặp, lớn nhất khoảng 4 mm), tỷ lệ/tiếp đất, ray tương tác từ bốn phía, lối đi và tham chiếu gameplay. Những phần dưới mô tả giai đoạn trước khi tăng số trẻ.

## Model dân làng, hitbox sân bi và cây liền khối (18/10/2026)

`Scene3VillageComposition.Actors.cs` sử dụng ba FBX người dùng đã nhập (`contrau`, `nguoidatrau`, `trecon`), không sửa hình học hoặc file gốc. Tỷ lệ đo bằng bounds của renderer rồi scale đồng đều: người lớn 1,65 m, trâu 1,45 m, năm trẻ 1,18–1,27 m. Bàn chân/chân trâu đặt đúng nền. Hai cặp người dắt trâu thay hình phác thảo cũ, đi theo các đoạn đường cong hiện tại; dây dắt được đặt trong cùng hierarchy để không bị bỏ lại khi nhóm di chuyển. Texture nhúng trong FBX được tách ra, dùng material URP chung, mipmap và giới hạn 2048; các model lặp chia sẻ mesh/material.

Năm trẻ đứng cách tâm vòng bi khoảng 2,45–2,65 m và quay vào vòng, chừa khoảng mở hướng đường vào sân. Chúng nằm trong `BaiBanBi_VaNhomTre/TreMoi_QuanhSanBi`; các hình phác thảo cũ được ẩn. Pose của model nhập được giữ nguyên. Hitbox `PlayMarbles` cũ lệch vòng chơi khoảng 3,6 m đã được đưa về tâm vòng và đổi thành trigger 3,2 × 1,56 × 3,2 m, đứng trên mặt sân. Không đổi collider tính điểm, lực bắn, kích thước bi hay khoảng tương tác chung của người chơi. Tham chiếu `MarbleShootingGame.marbleGround` bị trống được nối lại với collider sân `SanBi_DatNen` hiện có.

Tám cây Maxtree được căn tán bổ sung theo bounds của lá gốc, thay vì giả định pivot FBX trùng tâm tán. Thân/cành/lá được bake vào tọa độ local của cây và gộp thành **một MeshRenderer cho mỗi LOD**, dùng chung các material cũ. Các mesh cũ được giữ ẩn để có thể phục hồi. Gió, bóng và ba LOD vẫn hoạt động; di chuyển root cây không còn để tán hoặc thân ở vị trí khác. Lệnh polish bố cục cũ sẽ không tạo thêm tán rời trên cây đã gộp.

Menu áp dụng: `KuTy → Scene 3 → Fit villagers, marble interaction and joined tree canopies`. Bản sao scene trước sửa: `Tools/Scene3/VillageComposition/before-actors-scale-hitbox-trees-20261018.unity`. Báo cáo, ảnh kiểm tra và kết quả ở `Tools/Scene3/VillageComposition/Actors`. Kiểm tra gồm tỷ lệ, tiếp đất, hướng nhìn của trẻ, LOD cây liền khối, ray tương tác từ bốn phía trong khoảng 3 m, tham chiếu gameplay và các lối đi qua cầu/vào sân bi.

## Bố cục theo ảnh tham chiếu — trạng thái hiện tại (15/10/2026)

`Scene3VillageComposition.Reference.cs` thay bố cục dải ruộng bao quanh bằng hai vùng. Cụm làng ở trái/giữa có mặt đất Y = 0,20 m, năm căn nhà cũ cao 4–5 m, sân gạch đỏ, đống rơm hình nón, đường đất cong quanh ao làng và sân bi. Các instance nhà, model, loader, material và LOD gốc được giữ; chỉ đổi vị trí, xoay và scale đồng đều. Tiệm được chuyển cả hierarchy để hình ảnh và trigger đi cùng nhau. Sân bi được chuyển nguyên nhóm, kể cả camera/điểm ngắm/bi/vòng phấn; mặt sân Y = 0,225 m tránh trùng mặt đất. Bi vẫn có đường kính 10 cm.

Bốn ruộng phía phải có kích thước **20 × 30 m mỗi thửa**, đáy Y = 0, nước Y = 0,025 m. Bờ ngăn rộng 0,60 m, cao khoảng 0,14 m, dựng trực tiếp trên cùng mesh địa hình để không chồng góc. 14.564 bụi lúa cao 0,70–0,90 m được chia thành bốn vùng GPU, ngẫu nhiên yaw/tilt/scale và vùi chân khoảng 5 cm. `VillageRiceRegions` giữ compute culling, LOD và tối đa 12 indirect draw commands cho lúa; không tạo GameObject theo từng bụi. Lúa/cỏ/tre nhỏ không đổ bóng, vẫn nhận bóng.

Các `CauVanQuaMuong`, ngõ thẳng, bờ ruộng dải cũ và collider của chúng đã tắt. Chỉ còn **ba cầu 1,4 × 2,9 m**, chung mesh được tạo ở kích thước thực, scale luôn `(1,1,1)`. Một cầu qua kênh ngang ở đầu làng, hai cầu qua kênh dọc ở Z = 20 và 48; chiều dài cầu vuông góc với kênh. Không có phép kéo cầu tới đầu đường. Collider thân cây cũ bị vô hiệu và collider mới đi theo cây Maxtree đã đặt lại.

Tre bản địa được gom GPU instancing thành lũy tre giữa làng và ruộng, có khoảng mở tại hai cầu; cây Maxtree giữ thân/LOD và bổ sung tán lá xanh dùng atlas Maxtree có sẵn. Cỏ nhỏ chỉ rải theo bờ ao, móng nhà và mép đường; sân bi và lối vào được giữ thoáng. Bốn mặt nước ruộng, hai kênh và một ao dùng chung shader gợn sóng, không có planar reflection hay probe chạy liên tục. Fog Unity dùng chính xác `ExponentialSquared`, `#E2CFA9`, mật độ `0,012`; URP riêng scene giới hạn shadow atlas 2048, tầm bóng 45 m.

Menu áp dụng: `KuTy → Scene 3 → Build reference village and broad paddy plots`. Lệnh file bridge: `reference_v1`, `reference_polish`, `reference_validate`, `reference_capture`. Có bản sao trước sửa tại `Tools/Scene3/VillageComposition/before-reference-village.unity`. Báo cáo kiểm tra tại `reference_village_report.txt`, ảnh tại `Reference/01.png` đến `04.png`. Ảnh 01 tạm ẩn fog để xem rõ bố cục từ trên cao; ảnh 02–04 giữ fog trong game. Đã kiểm tra biên thửa/kích thước cây, cầu, collider địa hình, đi qua ba cầu/vào sân bi/xuống ruộng và tương tác nước. Các số draw chỉ tính riêng lúa; chưa phải phép đo FPS bản Build.

Các phần bên dưới mô tả các giai đoạn trước; thông số bố cục ở phần trên được ưu tiên.

## Maxtree trong vùng người chơi và ruộng hạ thấp (15/10/2026)

Nguồn FBX và Maps ở `BLENDER/MT_PM_V060_FBX` được giữ nguyên. Chỉ chọn Cyperus eragrostis, Digitaria sanguinalis, Lolium perenne, Setaria viridis và hai dáng Alnus cremastogyne. Các bản dùng trong game ở `Art/Models/Scene3/MaxtreeOptimized`; model gốc hàng triệu tam giác không nhập vào Assets. Cây lớn dùng 36.000 / 10.800 / 3.060 tam giác; cây vườn 26.000 / 7.800 / 2.210. Phép xoay ở node FBX được đưa vào vertex/normal của 18 mesh LOD để các mesh vẽ trực tiếp vẫn đứng đúng trục. Cây/cỏ dùng albedo và opacity Maxtree gốc, alpha clip, vật liệu lì và gió trong shader, không dùng glossiness. Texture cỏ/lá 1024, thân cây 2048, DXT5, mipmap giữ alpha coverage, trilinear và anisotropic 4.

`Maxtree_VungNguoiChoi` thay 8 cây bóng mát/sân vườn trong vùng chơi, giữ các collider thân cũ và cảnh quan xa. LOD cây đổi theo tỷ lệ chiếm màn hình 0,32 / 0,14 / 0,035. `VillageGroundCoverRenderer` chứa 1.388 vị trí cỏ của 4 loài, trong đó 199 bụi cỏ dại trong ruộng; không có GameObject, collider hay Update cho từng bụi. Cỏ được chia tile để loại khỏi frustum rồi gom GPU instancing theo loài/LOD: gần 10 m, giữa 24 m, bỏ ngoài 48 m. Ảnh kiểm tra hướng ruộng có 304 bụi thấy được và 15 draw calls riêng cho cỏ. Đây không phải số draw calls toàn scene hoặc phép đo FPS.

16 lòng ruộng là phần lõm thật trên mesh nền và MeshCollider, đáy khoảng -0,22 m với dốc nối bờ; nước ở -0,12 m. Các cụm lúa hạ 0,17 m, giữ plantingDepth 0,085 m và đủ 3.641 cây như trước; chân lúa thực tế khoảng -0,243 m, chìm nhẹ dưới bùn. Nước, outline, tương tác chân, gợn sóng và collider đồng bộ. Mesh collider được recook rõ ràng sau khi thay địa hình. Lúa vẫn dùng instancing/LOD/gió hiện có.

Không đặt cỏ vào sân chơi bi, đường chính, lối vào sân bi, lối vào tiệm, nền nhà hoặc cảnh nền ngoài vùng đi lại. Đã kiểm tra lối đi tới tiệm và sân bi, xuống/lên ruộng hai bên, xuống/lên hai mương, tương tác nước và dữ liệu gameplay trước khi lưu scene. Backup: `Tools/Scene3/VillageComposition/before-maxtree-playable-village.unity`. Báo cáo `maxtree_report.txt`; ảnh ở `VillageComposition/Maxtree`. Các lệnh editor: `maxtree_v1`, `maxtree_finish_v2`, `maxtree_validate`, `maxtree_capture`. Chưa đo FPS mới trong bản Build.

## Bố cục hồi ức hiện tại (12/10/2026)

### HDRI ấm, bờ đất lổn nhổn và chân lúa chôn dưới đất

Nguồn người dùng `golf_course_sunrise_4k.exr` được giữ nguyên và sao chép vào `Art/Textures/Scene3/HDRI`. Bản import cho Windows dùng 2048 × 1024, BC6H HDR, mipmaps/trilinear, tắt Read/Write. Material HDRI có tint `(0.60, 0.53, 0.44)`, exposure khoảng 0,673; Ambient Skybox và Reflection Probe lấy ánh sáng từ trời này. Các cây cận cảnh khổng lồ trong ảnh HDRI không phải model scene. Theo yêu cầu giữ cảnh quan, `VillageHdriSky.shader` giữ toàn bộ cây/cảnh trong panorama và giảm chiều rộng/cao góc nhìn 5 lần, đưa chúng về một vành cảnh xa. Panorama lặp quanh chân trời để nối kín 360°; các đĩa mặt trời lặp trong ảnh được che bằng mẫu trời lân cận, chỉ vẽ một mặt trời thấp theo Directional Light. Haze ấm nhẹ không che mất cảnh quan. File EXR và các model cây không sửa. Material đang dùng là `HDRI_LangQue_ChanTroiXa.mat`; `_LandscapeScale` điều chỉnh mức thu nhỏ, panorama nguyên bản vẫn giữ riêng để phục hồi.

16 bờ ruộng được tạo lại với tiết diện không đối xứng, độ rộng và cao biến thiên theo noise, các hõm và cục đất nhỏ gộp trong cùng mesh/collider. Đỉnh bờ chính giới hạn 12,3 cm, đỉnh cục đất tối đa 14,3 cm; vẫn không có GameObject/cỏ riêng cho từng cục. Vật liệu `BoRuong_DatLomNhom.mat` tăng phần đất trần, giảm lớp xanh đều. Các vòng/góc bờ vẫn nối kín và mặt nước/footprint giữ cùng đường viền.

`RuralRiceFieldRenderer.plantingDepth = 0.085` cho cả 16 ruộng. Khi tạo matrix, offset được bù thêm bằng đáy bounds của mesh và scale từng cây, nên đáy model thực nằm khoảng 7,3 cm dưới mặt đất thay vì phụ thuộc pivot. Mật độ 3.641 cây, seed, scale, mesh LOD, khoảng chuyển LOD và gió giữ nguyên. Cài bằng `warm_hdri_v1`; sửa nền cây HDRI bằng `hdri_background_v2`. Backup trước thay đổi: `before-warm-hdri-and-rough-banks.unity`; báo cáo `warm_hdri_report.txt`. Đã kiểm tra lối đi, nước, kênh và dữ liệu gameplay; chưa đo lại FPS.

### Kênh sâu và bờ ruộng thấp, nối liền

`Scene3VillageComposition.DeepCanals.cs` thay nền phẳng trong làng bằng mesh địa hình/collider có hai lòng kênh thật. Đáy ở khoảng Y = −0,82 m, mặt nước Y = −0,18 m, cột nước khoảng 0,64 m. Hai sườn thoải khoảng 40° để CharacterController có thể đi xuống và trở lại đường. Mặt nước rộng khoảng 2 m, có mép dao động nhẹ theo dòng cong; các cầu hiện có vẫn vượt được kênh. Nền đồng xa và các mảng màu nền được cắt khỏi hình chiếu vùng chơi để không che mặt nước nằm thấp.

64 đoạn bờ riêng của 16 ruộng được tắt và thay bằng 16 mesh vòng liên tục. Các góc bo cùng độ cao, nối bằng vertex dùng chung; bề rộng, đường mép và đỉnh bờ thay đổi nhẹ. Bờ cao khoảng 4,3–9,1 cm thay cho khoảng 14,5 cm trước đây. Nước ruộng được cắt theo đường viền bo góc; `VillageWaterSystem.Surface.outline` kiểm tra đúng vùng nước, không tạo gợn trên phần góc bờ khô. Shader nhận khoảng cách tới mép từ vertex màu để bọt bờ đi theo đường cong. Lúa, nhà, cầu, sân bi và ánh sáng hoàng hôn giữ bố trí hiện có; không tạo lại cỏ bờ.

Unity đã xác minh đáy/cột nước bằng raycast, đi xuống/ra cả hai kênh với CharacterController slope limit 45°, đi qua cầu tới tiệm/sân bi, 16 vòng bờ và hiệu ứng nước. Scene được lưu trong Edit Mode. Lệnh bridge: `deep_canals_v1`, hoàn thiện cắt nền `deep_canals_backdrop_v2`, chụp `deep_canals_capture`. Backup: `Tools/Scene3/VillageComposition/before-deep-canals.unity`; báo cáo `deep_canals_report.txt`, ảnh trong `DeepCanals`. Thay đổi này là địa hình tĩnh và hiệu ứng tiếp xúc nước; không mô phỏng dòng chảy chất lỏng hoặc bơi.

Scene vẫn dùng `LangQue_VietNam_Gon`, có thêm nhóm `LangQue_HoiUc`. Đường làng và cả hai mương uốn nhẹ; 16 thửa ruộng, bờ ruộng và vị trí lúa dùng cùng phép biến đổi `VillageLandscapeShape.RoadBend` nên cây, nước và mép bờ khớp nhau. Nhà được xoay và đặt lệch nhẹ, sân đất đổi sang mép bo không đều. Không biến dạng mesh nhà; các tham chiếu prefab, scale và LOD của năm nhà được giữ nguyên.

Hàng tre và gò đất lặp đều quanh bốn cạnh đã tắt. Cụm tre/cây được đặt rải rác, để lại khoảng nhìn ra đồng. Đồng xa là cảnh nền, có một mesh lúa LOD xa gộp và các mảng màu đất/lúa; không mở thêm vùng chơi trống. Phạm vi đi lại vẫn nằm trong xóm, với bốn giới hạn cũ và bốn đoạn chặn góc. Tổng phần nền xa/chi tiết mới khoảng 89.576 tam giác và 16 Renderer; cỏ ven bờ đã xoá vẫn không được tạo lại. Đây là ngân sách hình học, chưa phải kết quả đo FPS.

Sân bi giữ tâm vòng tại `(-15, 44)`, nền phẳng Y = 0,10 m, bi đường kính 10 cm và camera chơi hiện có. Nền sân bo tự nhiên, có lối vào cong, một cây tán rộng, ghế nghỉ, hộp bi, đồ dùng nhỏ và đôi dép. Ba bạn đứng quanh vòng phấn, bảng sân thử nghiệm đã ẩn. Các tham chiếu và dữ liệu component gameplay ngoài phép ánh xạ lúa/nước được kiểm tra không đổi. Mặt nền mới dùng MeshCollider với contact offset 0,2 mm.

Trời cam tím cũ `Scene3_Sunset.mat` còn được giữ để phục hồi; scene hiện dùng HDRI ấm mô tả ở trên. Reflection Probe nước chỉ chụp lại khi đổi ánh sáng và khi bắt đầu chơi, không chụp mỗi frame.

Kiểm tra Unity đạt: đi qua cầu tới tiệm/sân bi, lối vào vòng phấn, giới hạn map, ray nền dưới sáu viên bi, gợn nước và splash hai mương. Mô phỏng vật lý trong preview scene riêng trên chính mesh nền sân: bi truyền chuyển động, không xuyên nền và dừng trong 8 giây; viên nhận lực đi 0,517 m. Scene đã lưu trong Edit Mode. Chưa chạy lại toàn bộ chuỗi nhiệm vụ hoặc đo FPS ở cùng camera.

Script bố trí: `Assets/_Project/Editor/Scene3VillageComposition.Memory.cs`. Các lệnh bridge `memory_layout_v1`, `memory_polish_v2`, `memory_finish_v3` tương ứng dựng, hoàn thiện và xác minh/lưu; `memory_sunset_v1` phục hồi hoàng hôn. Không chạy lại lệnh cài nước chữ nhật trên bố cục này vì nó sẽ làm phẳng mương cong. Báo cáo và bốn ảnh xem trước ở `Tools/Scene3/VillageComposition/Memory`; bản scene trước thay đổi là `Tools/Scene3/VillageComposition/before-memory-landscape.unity`.

## Bố cục làng quê gọn trước khi đổi sang hồi ức (09/10/2026)

Scene `Act3_MemoryWorld_OutSide` dùng nhóm `LangQue_VietNam_Gon`. Nền được thu từ 180 × 210 m xuống 88 × 102 m, giới hạn đi lại tại X ±43 m, Z −12 đến 85 m. Lũy tre và gò đất thấp bao quanh ranh giới; bốn Box Collider chặn người chơi ra ngoài. Đây là phạm vi một xóm nhỏ, không mở thêm vùng đất ngoài làng.

Đường đất giữa làng nối năm nhà, tiệm tạp hóa và bãi chơi bi. Các nhà quay về đường, có sân đất khô, cây ổi, bàn ghế, hàng rào tre; lối ngõ ngắn và cầu ván bắc qua mương dẫn nước. Vườn rau và bụi chuối lấp khoảng đất nhỏ giữa khu ở và ruộng.

| Khu vực | Tọa độ X/Z (m) |
| --- | --- |
| Nhà 01 | −18 / 4 |
| Nhà 02 | 18 / 44 |
| Nhà 03 | −25 / 27 |
| Nhà 04 | 18 / 66 |
| Nhà 05 | −23 / 65 |
| Tiệm tạp hóa | 20 / 20 |
| Tâm bãi bi | −15 / 44 |

Bãi bi rộng 14 × 14 m, có cây bóng mát, ghế nghỉ và ba bạn nhỏ quanh vòng bi. Các bi, điểm bắn, bàn collider, vòng tính điểm và camera minigame được chuyển cùng nhau; các component gameplay và tham chiếu giữ nguyên. Tiệm giữ vị trí tương tác và nội dung bán hàng hiện có.

16 thửa ruộng có mặt nước nông và bờ đất, bố trí tránh sân nhà và sân bi. Ruộng dùng 3.641 cây từ bộ `RiceOptimized`, chia thành 82 ô để culling. `RuralRiceFieldRenderer` vẽ bằng GPU instancing, không tạo GameObject cho mỗi cây. Ba mức mesh có 5.986 / 380 / 48 tam giác; chuyển ở khoảng cách 6 m và 25 m. Ngưỡng hiển thị xa 180 m phủ toàn xóm. Shader `Lua_Ruong_Instanced` dùng chung atlas màu/normal, chuyển động gió theo vertex color và uốn nhẹ khi người chơi tới gần. Lúa không cast shadow để giảm chi phí; vẫn nhận bóng từ cảnh.

Muốn chỉnh mật độ, chọn `RuongLua_01` đến `RuongLua_16`, đổi **Spacing** trên component `RuralRiceFieldRenderer` (mặc định 0,78 m; giảm làm dày và tăng chi phí). **Plant Scale** mặc định 0,62. Chỉnh **Wind Amplitude**, **Wind Speed** trên material `Lua_Ruong_Chung`. Không cần chỉnh shader `Lua_Gio` cũ.

Lúa có gió nền nhẹ và từng dải gió mạnh chạy qua ruộng. Mỗi đợt lấy hướng lệch, độ mạnh và thời gian bắt đầu khác nhau từ một seed theo chu kỳ; dải gió lan theo tọa độ thế giới nên các ruộng phản ứng liên tục, không rung đồng loạt. Gốc đứng yên nhờ vertex color R = 0, thân/ngọn cong và lá rung nhanh hơn khi dải gió tới. Cùng công thức dùng cho Forward và Depth; cả ba LOD dùng chung gió. Bounds vẽ đã nới để ngọn lúa đang cong không bị culling ở mép camera.

Trong material `Lua_Ruong_Chung`, **Gust Strength** mặc định 0,28 m điều khiển độ cong khi gió mạnh; **Gust Interval** 24 giây là chu kỳ đợt gió; **Gust Travel Speed** 14 m/s là tốc độ dải gió chạy qua làng; **Gust Band Width** 10 m là nửa bề rộng vùng ảnh hưởng. **Wind Direction (XZ)** mặc định (0,8; 0,6) là hướng gió nền. Chu kỳ tự kéo dài nếu cần để dải gió thoát hết map trước đợt kế tiếp. Ngẫu nhiên ở đây là chuỗi giả ngẫu nhiên có thể tái hiện để kiểm tra. Không thêm Rigidbody hoặc GameObject riêng cho từng cây.

Ảnh trước/sau và báo cáo nằm trong `Tools/Scene3/VillageComposition`. Bản sao scene trước thay đổi là `Act3_MemoryWorld_OutSide.before-compact-village.unity`; nhóm bố cục/lúa cũ được giữ nhưng tắt. Công cụ `KuTy → Scene 3 → Compose compact Vietnamese village with new rice` từ chối tạo trùng khi nhóm làng gọn đã tồn tại.

Kiểm tra gồm biên dịch C#/shader trong Unity 2022.3.62f3, ảnh từ camera Unity, bố trí ruộng không chồng sân, tham chiếu gameplay và nền đi lại tại điểm xuất phát, đường, cầu, tiệm và sân bi. Đã chạy CharacterController qua cầu vào lối tiệm/sân bi và thử đi qua cả bốn ranh giới; tất cả đều đạt. Bản scene đã lưu được kiểm tra trong project tạm riêng; báo cáo là `verification_saved_scene.json`. Đây chưa phải phép đo FPS hay kiểm thử toàn bộ chuỗi nhiệm vụ; nhân vật trẻ/người dân và gia súc vẫn là model phác thảo có sẵn.

## Bờ ruộng có đất và cỏ (09/10/2026; cỏ đã xóa 10/10)

Theo yêu cầu ngày 10/10/2026, toàn bộ 17 object `CoBoRuong_Gop` / `CoBoKenh_Gop` đã xóa khỏi scene. Số cụm cỏ còn lại = 0; bộ cỏ trước đó có 46.730 lá / 140.190 tam giác, từng giảm 80% rồi được xóa hoàn toàn. Bờ đất, nền màu xanh, lúa và bố cục giữ nguyên. Công cụ dựng lại bờ không sinh cỏ nữa. Scene đã lưu; 39 component gameplay và kiểm tra lối đi đạt. Bản sao scene trước xóa là `Tools/Scene3/VillageComposition/before-remove-bank-grass.unity`; báo cáo `remove_grass_report.txt`. Các mesh cỏ cũ được giữ làm tài nguyên phục hồi, không còn renderer trong scene. Chưa đo lại FPS ở cùng góc camera; ảnh Stats ban đầu báo 84,7 FPS, main thread 11,8 ms, render thread 2,1 ms, chưa đủ xác định riêng phần nào gây tụt FPS.

Các đoạn bên dưới mô tả bộ cỏ trước khi xóa.

64 bờ thửa ruộng và 4 bờ mương đã thay khối hộp bằng mesh đất có sườn thoải, mặt trên hơi gồ ghề. Bờ ruộng rộng khoảng 0,72 m, cao 0,145 m; bờ mương rộng 0,82 m, cao 0,17 m. MeshCollider theo hình bờ, không dùng collider riêng cho cỏ. Cầu ván được chừa trống để người chơi đi qua.

Cỏ thấp mọc ở hai mép, lá mảnh cong và có độ cao/màu khác nhau; có gió nhẹ trên ngọn. Khoảng 46.730 lá cỏ được gộp thành 17 mesh (mỗi ruộng một mesh và một mesh ven mương), dùng chung material `CoBoRuong_Nham`; không tạo GameObject hay Rigidbody cho từng lá. Các mesh cỏ không cast shadow. Đây là bản cỏ đơn giản dựng từ geometry, chưa phải bộ texture cỏ ảnh chụp.

Ba material nằm tại `Assets/_Project/Art/Materials/Scene3/RiceOptimized`: `BoRuong_DatCo` cho bờ, `NenCo_Nham` cho nền xanh/gò đất quanh làng, `CoBoRuong_Nham` cho lá. Shader `Matte_Grass_Earth.shader` chỉ tính ánh sáng khuếch tán và ánh sáng môi trường, không có specular hoặc phản chiếu bóng. Màu cỏ loang theo tọa độ thế giới, có chi tiết nhỏ giảm dần ở xa; đất dùng texture `brown_mud_dry_diff_4k`. Vì vậy bề mặt bớt phẳng và bớt cảm giác nhựa, không chỉ giảm một giá trị Smoothness.

Muốn chỉnh màu, chọn material và sửa **Grass colour**; **Grass coverage** giảm sẽ lộ thêm đất (hợp với bờ, nên giữ 1 cho lá). **Earth tint** đổi màu đất, **Earth scale in world metres** tăng sẽ lặp texture đất nhiều hơn. **Grass blade wind** chỉ dùng trên material lá, đang đặt 0,04 m. Nếu dùng URP/Lit cho cỏ khác, đặt Metallic = 0, Smoothness khoảng 0–0,15 và tắt Specular Highlights/Environment Reflections để giảm bóng; vẫn cần màu/texture biến thiên để tránh mặt xanh trơn.

Công cụ dựng bờ nằm trong `Assets/_Project/Editor/Scene3VillageComposition.Banks.cs`. Bản sao scene trước chỉnh bờ là `Tools/Scene3/VillageComposition/before-natural-banks.unity`. Kiểm tra shader bằng render Unity và kiểm tra CharacterController qua các cầu/lối tiệm/sân bi, bốn ranh giới map; các component gameplay giữ nguyên. Chưa đo FPS trên máy đích.

## Nước phản chiếu, gợn chân và nước bắn (12/10/2026)

Cả hai `KenhDanNuoc` tại X ±5 và 16 `MatNuoc` trong ruộng dùng shader URP `KuTy/VillageWater`, viết ở `Assets/_Project/Art/Materials/Scene3/VillageWater.shader`. Sóng normal theo tọa độ thế giới, lọc chi tiết theo kích thước pixel để giảm nhấp nháy xa; Fresnel, phản chiếu cubemap môi trường, ánh mặt trời, viền bọt và độ trong tạo kiểu nước game có shader. Material riêng trong thư mục `Materials/Scene3/Water`: `NuocMuong_PhanChieu` và `NuocRuong_GonSong`. Shader Graph `NuocRuong` cũ được giữ để phục hồi.

Mặt nước dùng chung quad bốn vertex/hai tam giác, tổng 18 mặt/36 tam giác. Nước không có collider rắn; giữ Ground và bờ/cầu hiện tại. Đây là phản ứng mặt nước nông và hiệu ứng hạt, chưa có bơi hay mô phỏng chất lỏng thể tích. Không chạy camera phản chiếu phẳng mỗi frame, không bật thêm bản sao opaque/depth texture. Một reflection probe 128 px chụp môi trường một lần khi vào scene; loại Water layer khỏi cubemap để không tự phản chiếu. Cả hai mặt của shader đều render được.

`VillageWaterSystem.cs` trên `LangQue_VietNam_Gon/Nuoc_TuongTac` lấy tọa độ chân CharacterController rồi tra 18 hình chữ nhật nước. Đi tạo vòng gợn lan, đứng yên không tạo liên tục; trên cầu, khi khóa điều khiển, pause và teleport không tạo nước bắn sai. Gợn có tám slot GPU dùng lại; tự tắt vòng lặp ripple khi hết tuổi rồi khởi động lại khi bước tiếp. Một ParticleSystem world-space `NuocBan_Pool`, tối đa 128 giọt, dùng shader tròn `VillageWaterDroplets`; vận tốc bắn lên theo tốc độ rơi và chịu gravity, tự mờ đi. Không Instantiate/Destroy từng hạt hay từng vòng gợn.

Player scene 3 được bật nhảy bằng **Space**, cao 0,85 m; tốc độ khi chạm ruộng nước còn 80%, mương còn 70%. Các scene khác giữ mặc định không bật nhảy. Gợn/giọt là phản ứng tương tác với player; collider nền và luật các minigame vẫn giữ nguyên. Runtime tự dựng lại MaterialPropertyBlock theo ID/biên mặt nước khi load scene vì các block không serialize.

Menu **KuTy → Scene 3 → Install reflective water and footstep splashes** áp hệ thống và kiểm tra bước chân, đứng yên, landing ở cả hai mương, cầu, input bị khóa, giới hạn pool và hết tuổi/khởi động lại. C# và shader biên dịch; kiểm tra lối đi làng vẫn chạy. 38 component gameplay khác không đổi; FirstPersonController là component được chủ ý bổ sung nhảy và hệ số tốc độ. Backup: `Tools/Scene3/VillageComposition/before-interactive-water.unity`. Báo cáo `interactive_water_report.txt`; ảnh Unity tại `Tools/Scene3/Water` gồm hai mương, ruộng, gợn và nước bắn. Chưa đo FPS trên cùng camera sau thêm nước.

## Nhà Tencent 01 đã nhập vào game (09/10/2026)

Căn nhà tải từ Tencent thay nhà đầu tiên ở `LangQue_VietNam_Gon/NhaDan_VaSanVuon/SanNha_Gon_01/Nha01_NgoiDo_HienTon`, tọa độ X/Z −18 / 4, cửa hướng về đường làng. Sân, cây ổi, bàn ghế và cầu vào sân giữ vị trí; nhà cũ đổi tên có hậu tố `_Cu_DaTat` và tắt để phục hồi. Collider hộp cũ `VaChamNha` được tắt.

OBJ gốc có 1.500.012 tam giác, vẫn ở thư mục `BLENDER` bên ngoài Unity project. Script `Tools/Scene3/import_tencent_house.py` tạo FBX 3 LOD: 30.000 / 12.000 / 3.000 tam giác, giữ chiều rộng khoảng 7,2 m và đặt gốc tại mặt đất, sau đó chạy `repair_tencent_house.py` để tạo lại UV và bake atlas từ model gốc. Không dùng UV sau decimate vì texture cửa sổ bị kéo thành tam giác. Các mặt vữa gần mặt phẳng được chiếu lên mặt phẳng tường và dùng normal phẳng; mái ngói, tôn, cửa giữ normal chi tiết. Bản Blender đã sửa là `BLENDER/NhaQue_Tencent_01_SuaTuong.blend`. Bản gần đã giảm khoảng 98% tam giác so với file tải về.

FBX nằm tại `Assets/_Project/Art/Models/Scene3/TencentHouse01/NhaQue_Tencent_01.fbx`; prefab dùng trong scene là `Assets/_Project/Prefabs/Scene3/TencentHouse01/NhaQue_Tencent_01.prefab`. LODGroup chuyển theo tỷ lệ chiều cao vật thể trên màn hình, các ngưỡng 0,22 / 0,085 / 0,003; khoảng cách thực phụ thuộc camera/FOV và LOD Bias. Mesh trung bình có MeshCollider tĩnh, hoạt động độc lập với việc thay LOD của renderer. Không thêm Rigidbody.

Mỗi LOD dùng một material URP/Lit `Nha01_LOD0_Tencent_PBR.mat` / `Nha01_LOD1_Tencent_PBR.mat` / `Nha01_LOD2_Tencent_PBR.mat` trong `Assets/_Project/Art/Materials/Scene3/TencentHouse01`. Atlas riêng có kích thước 2048 / 1024 / 512; BaseColor giữ sRGB, Normal nhập đúng Normal Map, MetallicSmoothness dùng dữ liệu tuyến tính. Metallic ở R, Smoothness = 1 − Roughness ở A; Smoothness trên material nhân thêm 0,6. Bật mipmap, trilinear và anisotropic 4. Ảnh 4K gốc và material cũ vẫn được giữ để phục hồi.

Bản FBX/Blender trước sửa UV/normal nằm trong `Tools/Scene3/TencentHouse01/Repair/BeforeRepair`. Báo cáo planarity và atlas là `Tools/Scene3/TencentHouse01/Repair/repair_report.json`; ảnh ép từng LOD trong Unity nằm tại `Tools/Scene3/TencentHouse01/Repair/Unity`. Cả ba LOD đã áp vào scene và kiểm tra bằng camera Unity; cửa sổ không còn bị kéo thành tam giác và tường không còn các mảng normal tam giác lớn. Mức xa vẫn có texture mờ hơn do atlas nhỏ hơn. Kiểm tra bố cục, collider/lối đi và 39 component gameplay đạt; scene đã lưu.

Công cụ cài nhà: `Assets/_Project/Editor/Scene3VillageComposition.TencentHouse.cs`, menu **KuTy → Scene 3 → Install downloaded Tencent house 01**. Bản sao scene trước đổi nhà nằm tại `Tools/Scene3/VillageComposition/before-tencent-house01.unity`. Đã kiểm tra 39 component gameplay giữ nguyên serialized data, kích thước/phạm vi sân, LOD mesh, normal map, bố cục ruộng và CharacterController qua các cầu/lối tiệm/sân bi. Ảnh Unity: `Tools/Scene3/VillageComposition/After/05_NhaTencent01.png`. Chưa đo FPS hoặc kiểm thử toàn bộ nhiệm vụ.

## Nhà Tencent chữ L 02 (10/10/2026)

Model gốc nằm tại `BLENDER/NhaQue_L/bf81dc6ffe8a6a79404b6ecf30e0dfa3.obj`, có 1.499.748 tam giác. Nhà thay mẫu số 2 tại X/Z (18, 44), hiên quay về đường làng; kích thước sau xoay là khoảng 6,28 × 4,45 × 7,80 m. Chỉ scale đồng đều, không ép riêng trục, remesh, làm phẳng hay smooth vị trí vertex. Nhà cũ giữ nhưng tắt; sân và đồ sân không dời.

`Tools/Scene3/prepare_tencent_house02.py` tạo từng LOD độc lập từ model gốc và kiểm tra sai lệch bề mặt hai chiều bằng BVH. Các mesh 40.000 / 16.000 / 5.000 tam giác đã vượt qua giới hạn kiểm tra. Trong khoảng 50.000 điểm của bản gần, sai lệch lớn nhất 5,03 mm, trung bình 0,75 mm; đây là kiểm tra lấy mẫu, không phải chứng minh mọi điểm trùng tuyệt đối. Sai lệch lớn nhất được lấy mẫu ở LOD giữa/xa là 12,07 / 57,37 mm. Bản xa chỉ hiện khi vật thể chiếm dưới 9% chiều cao màn hình. File gốc được xác nhận SHA256 giữ nguyên.

`bake_tencent_house02.py` tạo UV mới và bake màu, normal, metallic/smoothness từ model gốc, dùng atlas riêng 2048 / 1024 / 512. Không tái dùng UV sau collapse. FBX: `Assets/_Project/Art/Models/Scene3/TencentHouse02/NhaQue_Tencent_02.fbx`; prefab: `Assets/_Project/Prefabs/Scene3/TencentHouse02/NhaQue_Tencent_02.prefab`. Ba material URP/Lit nằm trong `Art/Materials/Scene3/TencentHouse02`. LODGroup có ngưỡng 0,24 / 0,09 / 0,003; MeshCollider tĩnh dùng bản xa và không thay theo LOD render. Bản Blender để chỉnh tiếp là `BLENDER/NhaQue_L/NhaQue_L_Game.blend`, kèm model gốc ẩn để đối chiếu; model gốc không được xuất vào FBX.

Công cụ cài: `Scene3VillageComposition.TencentHouse02.cs`, menu **KuTy → Scene 3 → Install downloaded L house 02**. Scene trước đổi nhà được lưu tại `Tools/Scene3/VillageComposition/before-tencent-house02.unity`. Đã biên dịch và áp trong Unity, kiểm tra 39 component gameplay giữ nguyên, nhà trong sân khô, lối đi/cầu/ranh giới đạt, scene đã lưu, cỏ ven bờ vẫn bằng 0. Ảnh ép từng LOD qua bốn camera Unity nằm trong `Tools/Scene3/TencentHouse02/Unity`. Báo cáo hình học là `geometry_report.json`; kiểm tra đường bao qua bốn góc nằm trong `silhouette_report.json`. Tỷ lệ giao/hợp của silhouette gần so với gốc đạt 99,955–99,972%; bản giữa đạt ít nhất 99,887%, bản xa ít nhất 99,663% trong bốn phép chiếu 1000 × 850 px. Chưa đo FPS mới hay thử toàn bộ nhiệm vụ.

## Nhà gạch quê 03, màu cũ (10/10/2026)

Model đúng sau khi người dùng thay file là `BLENDER/Nhaque3/71df8728df7b6501c041c2a53af8b2c4.obj`, có 1.500.008 tam giác, SHA256 `890927cda7e3a54c9275807a3a83b975ae4bbd1a72372035e1599b22a4522623`. File OBJ cũ còn trong thư mục là bản trùng nhà 02 và không được dùng. Bản mới thay nhà số 3 tại X/Z (−25, 27), hiên hướng ra đường, kích thước khoảng 6,30 × 5,19 × 7,00 m. Sân và đồ sân giữ vị trí, nhà phác thảo cũ tắt để phục hồi.

`prepare_tencent_house03.py` tạo LOD 40.000 / 16.000 / 12.000 tam giác, giảm khoảng 97,3% tam giác ở bản gần. Bản xa 5.000 và 8.000 bị loại do sai lệch lấy mẫu vượt giới hạn. Chỉ scale đồng đều và collapse cạnh; không remesh, ép trục hay thay vị trí vertex để làm phẳng tường. 99% điểm kiểm tra bản gần có sai lệch dưới 2,53 mm; lớn nhất được lấy mẫu 28,83 mm, trung bình 0,67 mm. Bản giữa và xa có sai lệch lấy mẫu lớn nhất 41,68 mm. Đây là phép đo lấy mẫu; không khẳng định mọi chi tiết trùng tuyệt đối. Kiểm tra bốn phép chiếu 1000 × 850 px cho silhouette gần khớp gốc ít nhất 99,953%, giữa 99,846%, xa 99,813%.

`village_house_weathering.py` tạo vật liệu Blender từ atlas gốc: saturation 0,72, brightness nhân 0,88, loang màu nâu xám nhẹ, vệt mưa, chân tường ẩm và rêu thưa, tôn có vệt gỉ. Roughness tối thiểu 0,72, metallic nhân 0,5. Các hiệu ứng được bake vào atlas, không thêm noise shader, script hay object lúc chơi. Hình ảnh gốc không sửa. Vật liệu giảm cảm giác mới/bóng; dáng kiến trúc stylized của model vẫn giữ.

`bake_tencent_house03.py` tạo UV riêng và bake albedo bằng Emission (không lẫn ánh sáng), normal và metallic/smoothness. Split normal phẳng của mesh thấp chỉ dùng để ổn định hướng chiếu, không di chuyển vertex; normal bake phục hồi shading gốc ở hai LOD gần. Giảm cage extrusion đã sửa vệt đen trên mái. `refine_house03_far.py` bake lại atlas xa từ LOD0 sạch hơn, tránh ray lọt vào khe nhỏ của model gốc. LOD2 dùng normal hình học, tắt normal map chi tiết để giảm aliasing và công việc shader. Atlas 2048 / 1024 / 512 có mipmap, trilinear, anisotropic 4. Normal nhập đúng loại từ metadata trước lần import đầu tiên.

FBX: `Assets/_Project/Art/Models/Scene3/TencentHouse03/NhaQue_Tencent_03.fbx`; prefab: `Assets/_Project/Prefabs/Scene3/TencentHouse03/NhaQue_Tencent_03.prefab`. LODGroup có ngưỡng theo chiều cao màn hình 0,24 / 0,09 / 0,003, collider tĩnh dùng mesh 12.000 tam giác và không đổi theo LOD render. Ba material riêng nằm trong `Art/Materials/Scene3/TencentHouse03`. File chỉnh tiếp là `BLENDER/Nhaque3/NhaQue_03_Game.blend`, giữ model gốc ẩn; bản chuẩn bị chưa làm cũ là `NhaQue_03_Prepare.blend`. Chạy prepare → bake → refine_far → verify_silhouette → install_house03_assets để dựng lại bộ asset; menu Unity **KuTy → Scene 3 → Install weathered rural house 03** cài vào scene.

Đã cài và lưu scene trong Unity, kiểm tra nhà ở trong sân khô, 39 component gameplay giữ nguyên, CharacterController qua cầu/lối tiệm/sân bi và bốn ranh giới đạt; cỏ ven bờ vẫn bằng 0. Ảnh ép ba LOD qua bốn camera nằm trong `Tools/Scene3/TencentHouse03/Unity`. Báo cáo surface/silhouette/weathering trong `Tools/Scene3/TencentHouse03`; báo cáo cài đặt `Tools/Scene3/VillageComposition/tencent_house03_report.txt`. Backup scene trước đổi nhà: `before-tencent-house03.unity` trong cùng thư mục. SHA256 file OBJ gốc sau xử lý giữ nguyên. Chưa đo FPS mới hay thử toàn bộ chuỗi nhiệm vụ.

## Nhà quê 04 có bếp củi (10/10/2026)

Nguồn là `BLENDER/Nhaque4/2f79e2974f1488051a3e23b79cf6144f.obj`, 1.499.650 tam giác, SHA256 `04cb02b36b40d9d4b556efb06c5b0137956728a3a5437d8eadfa0f4d216a0d5b`. Cả sáu file OBJ/MTL/texture gốc giữ nguyên checksum sau xử lý. Nhà thay vị trí số 4 trong làng tại X/Z (18, 66), quay Y 270° ra đường; kích thước Unity khoảng 4,95 × 3,92 × 7,80 m. Giữ bếp gạch đốt củi, nồi đen, củi xếp, mái tôn vá và màu cũ của model; không thêm hiệu ứng thời tiết vào shader lúc chơi.

LOD là 40.000 / 16.000 / 8.000 tam giác, giảm khoảng 97,3% ở bản gần. Chỉ scale đồng đều và collapse cạnh, không remesh hoặc ép phẳng tường. Sai lệch lấy mẫu lớn nhất của bản gần 8,92 mm, 99% điểm dưới 4,62 mm; bản giữa 26,89 mm, bản xa 48,37 mm. Kiểm tra silhouette bốn phép chiếu 1000 × 850 px cho tỷ lệ giao/hợp thấp nhất lần lượt 99,850% / 99,690% / 99,380%. Đây là phép kiểm tra lấy mẫu và đường bao, không bảo đảm từng chi tiết trùng tuyệt đối.

UV và atlas PBR riêng 2048 / 1024 / 512, màu bake bằng Emission không lẫn đèn. Roughness tối thiểu 0,72. Atlas giữa/xa chiếu lại từ bản gần sạch để tránh ray lọt vào các khe nhỏ. Bản gần dùng normal map bake; bản giữa/xa dùng normal hình học để tránh mảng tam giác tối do chiếu normal và giảm chi phí shader. Bản giữa làm mượt normal qua góc 55°, giữ cạnh sắc; hash vị trí vertex và topology trước/sau bước này giống nhau. Không dùng normal map thử nghiệm từ cage trong material giữa cuối cùng. Texture có mipmap, trilinear, anisotropic 4, nén và metadata đúng loại Normal trước import đầu tiên.

FBX: `Assets/_Project/Art/Models/Scene3/TencentHouse04/NhaQue_Tencent_04.fbx`; prefab: `Assets/_Project/Prefabs/Scene3/TencentHouse04/NhaQue_Tencent_04.prefab`. Ba material URP/Lit trong `Art/Materials/Scene3/TencentHouse04`. LODGroup chuyển theo chiều cao màn hình 0,24 / 0,09 / 0,003; collider tĩnh dùng mesh 8.000 tam giác và giữ nguyên khi đổi LOD render. Bản Blender chỉnh tiếp là `BLENDER/Nhaque4/NhaQue_04_Game.blend`, giữ model gốc ẩn để đối chiếu. Chuỗi dựng asset: prepare → bake → refine_far → repair_mid_normals → finalize_shading → verify_silhouette → install_assets; các script mang hậu tố house04 trong `Tools/Scene3`.

Đã cài bằng `Scene3VillageComposition.TencentHouse04.cs`, lưu scene, kiểm tra nhà trong sân khô, 39 component gameplay giữ nguyên, lối đi/cầu/tiệm/sân bi và bốn ranh giới đạt; cỏ ven bờ còn 0. Ảnh ép ba LOD qua bốn camera Unity nằm trong `Tools/Scene3/TencentHouse04/Unity`. Báo cáo hình học/silhouette/shading nằm trong cùng thư mục; báo cáo cài `Tools/Scene3/VillageComposition/tencent_house04_report.txt`, bước shading cuối ghi `tencent_house04_shading_report.txt`. Backup trước đổi nhà là `before-tencent-house04.unity`. Chưa đo FPS mới hoặc thử toàn bộ chuỗi nhiệm vụ.

## Nhà quê Tencent số 5 (10/10/2026)

Nguồn `BLENDER/NhaQue5/2d4563d05713d86596e74567977ff461.obj` có 1.500.018 tam giác, SHA256 `5ae4c8f58a1f412f64ef7ba6e08c9bb98edd5c37f4f742ea0e5837b23add4285`. Model là nhà vôi xanh cũ, mái ngói rêu và hiên tôn gỉ. OBJ và bốn texture PBR gốc được giữ nguyên, kiểm tra lại hash trước khi xuất vào Assets.

`Tools/Scene3/build_tencent_house05.py` tái sử dụng quy trình nhà số 4 với đầu vào/đầu ra riêng cho số 5. Các bước Blender: `prepare`, `bake`, `refine`, `shading`, `verify`; bước Python thường: `publish`. Mesh giảm bằng edge collapse có kiểm tra sai lệch hai chiều; không remesh, kéo riêng các trục, làm phẳng tường hoặc làm mượt vị trí vertex. Ba LOD có 40.000 / 16.000 / 5.000 tam giác. Kiểm tra hơn 32.000–49.000 mẫu: sai lệch bề mặt tối đa trước scale scene là 5,5 / 10,6 / 37,7 mm. Kiểm tra silhouette bốn hướng ở độ phân giải 1000 × 850; mức gần phải trùng ít nhất 99,5%.

UV và atlas PBR độc lập 2048 / 1024 / 512. Màu bake bằng Emission, không lẫn đèn; roughness tối thiểu 0,72. Atlas giữa/xa được chiếu từ bản gần sạch; các mức này dùng normal hình học để tránh vệt chiếu normal và giảm aliasing. Mức giữa làm mượt normal qua góc 55°, giữ cạnh sắc; hash vị trí/topology trước và sau không đổi. Normal map chỉ bật ở mức gần. Texture nén, mipmap, trilinear, anisotropic 4; metadata loại Normal được tạo trước import.

FBX: `Assets/_Project/Art/Models/Scene3/TencentHouse05/NhaQue_Tencent_05.fbx`; prefab: `Assets/_Project/Prefabs/Scene3/TencentHouse05/NhaQue_Tencent_05.prefab`. LODGroup chuyển mức theo tỉ lệ chiều cao trên màn hình 0,24 / 0,09 / 0,003; collider tĩnh dùng mesh 5.000 tam giác và không thay khi chuyển LOD. Bản Blender để chỉnh tiếp: `BLENDER/NhaQue5/NhaQue_05_Game.blend`, có model gốc ẩn để đối chiếu; model gốc không xuất vào FBX.

`Scene3VillageComposition.TencentHouse05.cs` thay đúng nhà trong `LangQue_VietNam_Gon/NhaDan_VaSanVuon/SanNha_Gon_05`, giữ tâm X/Z `(-23,65)`, quay 90° về phía đường. Scale đồng đều 0,8 để chiều cao khoảng 4,56 m phù hợp những nhà còn lại. Nhà phác thảo cũ được tắt và giữ lại; backup scene là `Tools/Scene3/VillageComposition/before-tencent-house05.unity`. Lệnh editor kiểm tra component gameplay, sân khô, lối đi rồi lưu scene. Ảnh các LOD trong Unity: `Tools/Scene3/TencentHouse05/Unity`; báo cáo `tencent_house05_report.txt`. Chưa đo FPS của bản build ở cùng camera.

## Bi ve Tencent cho sân chơi (10/10/2026)

Nguồn `BLENDER/VienBi/1ca9a2dd15e97dae21ef7955ada009d2.obj` có 1.500.000 tam giác, SHA256 `ca7ad5afd5d1fe19d2cb8a021fbdc461d4780992a71be9a3350e58c67aee05ad`. File chứa cả cụm bi, không phải một viên đơn. Tách theo các phần mesh rời cho ra bốn viên gần hình cầu và một khối nhiều viên dính nhau; chỉ lấy màu/họa tiết của bốn viên tách rời. Sáu phiên bản cho `PlayerMarble` và `Target0`–`Target4` có bốn màu/họa tiết khác nhau cộng hai biến thể màu. File OBJ/texture nguồn không sửa.

`build_vienbi_game_assets.py` tạo bản màu ban đầu; `round_vienbi_assets.py` sửa lệch hình của nguồn bằng UV sphere đều 32 đoạn, 14 vòng và bake lại màu lên UV mới. Mỗi mesh có 832 tam giác, tổng sáu viên 4.992 tam giác, texture 512 × 512. Tâm mesh ở gốc, bán kính chuẩn 0,5. Theo yêu cầu tăng độ dễ nhìn, scene hiện dùng scale đồng đều **0,1**, tương ứng đường kính **10 cm**; SphereCollider bán kính local 0,5 tương ứng bán kính thế giới 5 cm. Bản kích thước 16 mm trước đó giữ làm tuỳ chọn phục hồi. Material URP/Lit dùng màu nguồn đổi sắc nhẹ, kính opaque để tránh chi phí trong suốt; không có normal map. Báo cáo hình học là `game_assets_report.json`; bản màu/mesh trước làm tròn lưu trong `Tools/Scene3/VienBi/BeforeRound`.

Sáu FBX nằm trong `Assets/_Project/Art/Models/Scene3/VienBi`, texture trong `Art/Textures/Scene3/VienBi`, material trong `Art/Materials/Scene3/VienBi`. Texture nén, có mipmap, trilinear; mesh import tắt Read/Write, animation, collider phụ và material tự sinh. Material ưu tiên màu emission để nắng hoàng hôn không nhuộm tất cả viên đỏ, còn giữ một phần phản sáng URP/Lit. `Scene3VillageComposition.VienBi.cs` dùng menu **KuTy → Scene 3 → Use readable 10 cm marbles** để áp bi lớn, vòng phấn đường kính 72 cm và camera gần khi chơi. Nới khoảng cách giữa các bi để không chồng lên nhau. Giữ nguyên sáu Rigidbody/SphereCollider và mọi tham chiếu của minigame; 38 component gameplay khác không đổi. Menu **Fix marbles to round 16 mm glass spheres** khôi phục bản nhỏ.

Khối lượng/lực bắn thay theo đường kính và khoảng cách sân. Bản 10 cm có khối lượng khoảng 1,318 kg, impulse 0,977–6,348 N·s; lực kéo tối đa đạt ở 32 cm, bán kính nhận thao tác 7 cm và vạch ngắm dài tối đa 80 cm. Camera lúc chơi FOV 50°, near clip 5 mm; FOV/near clip/transform cũ được phục hồi khi thoát. Các thông số ngắm được serialize riêng, còn contact offset 0,2 mm, ngưỡng sleep và giới hạn spin được áp lại trong `Awake` vì thuộc tính native không lưu trong YAML scene. Ground collider chỉ được chỉnh tại sân bi, không đổi physics toàn project. Kiểm tra va chạm hai viên có cùng đường kính/cấu hình trong physics preview scene riêng: truyền chuyển động, không xuyên nền, dừng trong 8 giây; kiểm tra lối đi làng vẫn chạy. Ảnh Unity là `Tools/Scene3/VienBi/Unity/Marble_Game_Area.png`; báo cáo hiện tại `Tools/Scene3/VillageComposition/readable_marbles_report.txt`. Backup trước tăng lên 10 cm là `before-readable-marbles.unity`. Chưa đo FPS mới ở cùng camera hoặc chạy đủ chuỗi nhiệm vụ.

## Bố cục trước đây (05/10/2026, đã thay thế)

Nhóm `LangQue_BoCucMoi` trong scene 3 là bố cục đang dùng. Năm nhà được chuyển ra nền đất khô ở hai mép ngoài ruộng, lần lượt tại X/Z `(-56,-9)`, `(56,10)`, `(-56,33)`, `(56,66)`, `(-56,92)`. Mỗi sân rộng 23×20 m có một cây ổi low-poly, một bàn và hai ghế gỗ; lối đất rộng 3,2 m nối ra đường. Khuôn viên nhà cách mặt nước/ruộng gần nhất ít nhất 5 m. Nhóm nhà cũ được giữ nhưng tắt để có thể so sánh hoặc phục hồi.

Lúa cũ và các mặt nước ruộng cũ được tắt, không xoá. Tám mảng ruộng mới hiện có 310 cụm lúa nhẹ, mỗi cụm gồm 9 cây (2.790 cây tổng cộng), lấy từ mẫu lúa mới khoảng 5.986 tam giác/cây. Prefab `CayLuaNhe_Cum3x3` xoay phần mesh đúng trục để cây đứng thẳng, đưa điểm thấp nhất của rễ về mặt đất; các instance chỉ xoay ngẫu nhiên quanh trục Y nên không bị nằm ngang. Có thể cập nhật bằng menu Unity **KuTy → Scene 3 → Plant new light rice across fields**; script bỏ qua cụm đã tồn tại. `caylua.fbx` gốc và 30 cụm cũ vẫn được giữ nhưng không hiển thị. Đường đất đến các sân chừa khe khô trong lúa và mặt nước. Bãi bi nằm trên dải đất khô phía trái, thay ba ô ruộng cũ tại Z=27/46/65, với nền chơi 24×22 m, không có ruộng hay nước ngay quanh sân. Gameplay bắn bi vẫn giữ các object/component cũ.

Dựng lại bằng menu Unity **KuTy → Scene 3 → Rebuild village, rice fields and marble clearing** trên bản scene chưa có nhóm mới. Bản scene trước khi đổi bố cục được lưu tại `Tools/Scene3/Act3_MemoryWorld_OutSide.before-village-layout.unity`. Các mô tả bên dưới ghi lại bố cục trước đợt chỉnh này.

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

Năm nhà dân mới được đặt xen kẽ hai bên đường trong nhóm `Scene3_DuongLang_RuongLua/NhaDan_XenGiuaRuongLua`. Mỗi nhà có nền đất và lối vào; năm ô lúa được chừa đúng diện tích sân nhà và lối đi. Prefab nhà, mesh lúa đã chừa và cách dựng lại nằm trong `Tools/Scene3/README.md`. Tiệm, sân bi và đường làng giữ vị trí cũ.

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

### 2026-10-15: HDRI vertical seam correction

The distant-horizon shader's atan2 longitude branch cut could select the coarsest panorama mip for a pixel strip, producing a bright vertical "beacon". VillageHdriSky.shader now unwraps longitude derivatives and samples both the panorama and sun-replacement neighbour with explicit gradients. The 5x scenery reduction, trees, warm tint and source EXR are unchanged.

Verified in Unity using `hdri_seam_check`: isolated sky render at the offending longitude with the seam offset across a pixel quad. Before: maximum sky seam contrast 0.12956; after: 0.00347 (surrounding variation 0.00303), no shader compiler errors. Before/after captures are under Tools/Scene3/VillageComposition/HdriSeam. Diagnostic uses a preview scene and does not save or modify the user's scene.
