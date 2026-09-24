# 🎮 KẾ HOẠCH PHÁT TRIỂN — "CHÌA KHÓA KÝ ỨC"
> Kế hoạch tham khảo cũ; giá tiền/vật phẩm và luồng nhiệm vụ trong tài liệu này đã được thay bằng [nền code cốt truyện hiện tại](implementation-guide.md).

> **Team**: 3-4 người | **Thời gian**: 5 tuần | **Engine**: Unity 2022.3 LTS / URP  
> **Trình độ**: Mới với Unity, được hỗ trợ bởi AI tools + tự học Blender

---

## 📋 MỤC LỤC

1. [Kiến trúc kỹ thuật](#1-kiến-trúc-kỹ-thuật)
2. [Cấu trúc thư mục dự án](#2-cấu-trúc-thư-mục-dự-án)
3. [Mô tả chi tiết từng Script](#3-mô-tả-chi-tiết-từng-script)
4. [Sơ đồ hệ thống](#4-sơ-đồ-hệ-thống)
5. [Phân công vai trò](#5-phân-công-vai-trò)
6. [Lộ trình 5 tuần (Sprint Plan)](#6-lộ-trình-5-tuần)
7. [Task Breakdown — Mini-games & Systems](#7-task-breakdown)
8. [AI Tools & Workflow](#8-ai-tools--workflow)
9. [Quản lý rủi ro](#9-quản-lý-rủi-ro)
10. [Checklist chuẩn bị](#10-checklist-chuẩn-bị)
11. [Hướng dẫn Setup Unity](#11-hướng-dẫn-setup-unity)

---

## 1. KIẾN TRÚC KỸ THUẬT

### 1.1 Nguyên tắc thiết kế

| Nguyên tắc | Chi tiết |
|---|---|
| **Interface-based Interaction** | Mọi vật tương tác implement `IInteractable`. `PlayerInteraction` chỉ gọi `.Interact()`, không cần if/else riêng từng loại |
| **ScriptableObject cho Data** | Dữ liệu vật phẩm shop (`ItemData`) dùng ScriptableObject — sửa trong Inspector, không cần sửa code |
| **Singleton cho Manager** | Chỉ `GameManager` và `AudioManager` dùng Singleton + `DontDestroyOnLoad`. Các manager khác là MonoBehaviour thường |
| **1 Scene cho Act2** | 3 khu vực (bếp, sân, tiệm) nằm trên 1 scene liền mạch — tránh lỗi truyền dữ liệu giữa scene |
| **URP + Post Processing** | Bloom, Color Grading, Vignette cho hiệu ứng hoài niệm |

### 1.2 Flow chuyển Scene

```
MainMenu  →(Bấm Chơi)→  Act1_RealWorld  →(Chạm vật kỷ niệm, fade vàng ấm)→  Act2_MemoryWorld  →(Thu thập đủ 3/3 mảnh)→  Act3_Ending  →(Chơi lại)→  MainMenu
```

### 1.3 Sơ đồ kiến trúc hệ thống

```
                     ┌──────────────────────────────────┐
                     │         GameManager               │
                     │  (Singleton, DontDestroyOnLoad)   │
                     │  - GameState tracking             │
                     │  - Cursor management              │
                     │  - Scene coordination             │
                     └────────┬───────────┬──────────────┘
                              │           │
              ┌───────────────┘           └───────────────┐
              ▼                                           ▼
    ┌───────────────────┐                     ┌──────────────────┐
    │   SceneLoader     │                     │   AudioManager   │
    │  - Fade in/out    │                     │  - BGM crossfade │
    │  - Async loading  │                     │  - SFX playback  │
    │  - Custom colors  │                     │  - Auto-switch   │
    └───────────────────┘                     └──────────────────┘

    ┌────────────────────────────────────────────────────────────┐
    │                    PLAYER SYSTEM                           │
    │  FirstPersonController ──→ PlayerInteraction              │
    │  (WASD + Mouse Look)       (Raycast + E key)              │
    │                                    │                       │
    │                           ┌────────▼────────┐              │
    │                           │  IInteractable  │              │
    │                           └────────┬────────┘              │
    │                    ┌───────┬───────┼───────┬──────┐        │
    │                    ▼       ▼       ▼       ▼      │        │
    │              Dish   Marble   Shop   Memory        │        │
    │           Interact Interact Interact Trigger       │        │
    └────────────────────────────────────────────────────────────┘

    ┌────────────────────────────────────────────────────────────┐
    │                    GAME SYSTEMS                            │
    │                                                            │
    │  DishWashingGame ──→ CurrencyManager ←── ShopManager      │
    │  (progress bar)       (tiền +/-)          (mua bán)       │
    │       │                                      │             │
    │       ▼                                      ▼             │
    │  MemoryCollectionManager ←──────────── InventoryManager   │
    │  (track 3/3 mảnh ký ức)                 (túi đồ)          │
    │       │                                                    │
    │       ▼                                                    │
    │  MarbleShootingGame                                        │
    │  (physics bắn bi)                                          │
    └────────────────────────────────────────────────────────────┘

    ┌────────────────────────────────────────────────────────────┐
    │                      UI LAYER                              │
    │                                                            │
    │  HUDController     ShopUI        DialogueUI               │
    │  (crosshair,       (panel shop,  (narration,              │
    │   tiền, ký ức)     nút mua)      typewriter)              │
    │                                                            │
    │  ProgressBarUI     MarbleAimUI   MainMenuUI   EndingUI    │
    │  (rửa chén)        (lực, lượt)   (Chơi/Thoát) (kết thúc)│
    └────────────────────────────────────────────────────────────┘
```

---

## 2. CẤU TRÚC THƯ MỤC DỰ ÁN

```
KuTy/                                          ← Root project
├── .gitignore                                  ← Git ignore cho Unity
├── docs/
│   ├── game-design-document.md                 ← GDD gốc
│   └── development-plan.md                     ← File này
│
└── Assets/
    └── _Project/                               ← Thư mục chính team (tránh lẫn asset import)
        │
        ├── Scripts/                            ← TẤT CẢ CODE C#
        │   ├── Core/
        │   │   ├── GameState.cs                  Enum trạng thái game
        │   │   ├── GameManager.cs                Singleton quản lý state, cursor, pause
        │   │   └── SceneLoader.cs                Load scene với hiệu ứng fade
        │   │
        │   ├── Player/
        │   │   ├── FirstPersonController.cs      Di chuyển WASD + Mouse Look + Sprint
        │   │   └── PlayerInteraction.cs          Raycast + E key + UI prompt
        │   │
        │   ├── Interaction/
        │   │   ├── IInteractable.cs              Interface chung cho vật tương tác
        │   │   ├── InteractableBase.cs           Abstract base class (prompt, one-time use)
        │   │   ├── DishInteractable.cs           Trigger mini-game rửa chén
        │   │   ├── MarbleInteractable.cs         Trigger mini-game bắn bi
        │   │   ├── ShopInteractable.cs           Trigger mở shop UI
        │   │   └── MemoryTrigger.cs              Trigger xuyên không (Act1 → Act2)
        │   │
        │   ├── MiniGames/
        │   │   ├── DishWashingGame.cs            Mini-game rửa chén (hold-to-progress)
        │   │   └── MarbleShootingGame.cs         Mini-game bắn bi (drag aim + physics)
        │   │
        │   ├── Systems/
        │   │   ├── CurrencyManager.cs            Quản lý tiền (Add/Spend/HasEnough)
        │   │   ├── InventoryManager.cs           Quản lý túi đồ (Add/Remove/HasItem)
        │   │   ├── ShopManager.cs                Logic mua bán + trigger ký ức đặc biệt
        │   │   └── MemoryCollectionManager.cs    Track 3/3 mảnh ký ức + auto trigger Act3
        │   │
        │   ├── UI/
        │   │   ├── HUDController.cs              HUD: crosshair, tiền, mảnh ký ức
        │   │   ├── ProgressBarUI.cs              Thanh progress rửa chén
        │   │   ├── MarbleAimUI.cs                UI lượt bắn + lực + bi trúng
        │   │   ├── DialogueUI.cs                 Text narration + hiệu ứng typewriter
        │   │   ├── ShopUI.cs                     Panel shop + danh sách items + nút Mua
        │   │   ├── MainMenuUI.cs                 Menu chính: Chơi / Thoát
        │   │   └── EndingUI.cs                   Màn hình kết: thông điệp + fade in
        │   │
        │   ├── Audio/
        │   │   ├── AudioManager.cs               Singleton: BGM crossfade, SFX, ambient
        │   │   └── SoundLibrary.cs               ScriptableObject chứa AudioClip refs
        │   │
        │   └── Data/
        │       └── ItemData.cs                   ScriptableObject: tên, giá, icon, type
        │
        ├── Scenes/                             ← CÁC SCENE UNITY
        │   ├── MainMenu.unity                    Menu chính
        │   ├── Act1_RealWorld.unity              Hồi 1: Căn nhà cũ (xám lạnh)
        │   ├── Act2_MemoryWorld.unity            Hồi 2: Thế giới ký ức (vàng ấm)
        │   └── Act3_Ending.unity                 Hồi 3: Kết thúc
        │
        ├── Prefabs/                            ← PREFAB ĐÃ SETUP SẴN
        │   ├── Player/                           Player + Camera + Controller
        │   ├── Interactables/                    Bồn rửa, chỗ chơi bi, quầy hàng
        │   ├── MiniGames/                        Setup mini-game (bi, chén, sân)
        │   ├── UI/                               Canvas, panels, buttons
        │   └── Environment/                      Props: bếp, kệ, cây cối
        │
        ├── Art/                                ← TÀI NGUYÊN ĐỒ HỌA
        │   ├── Models/                           3D models (.fbx) từ AI/Blender
        │   ├── Materials/                        Unity Materials
        │   ├── Textures/                         Texture maps
        │   └── Animations/                       Animation clips (Mixamo/Blender)
        │
        ├── Audio/                              ← TÀI NGUYÊN ÂM THANH
        │   ├── BGM/                              Nhạc nền (lo-fi, acoustic)
        │   └── SFX/                              Sound effects (chén, bi, nước, chim)
        │
        ├── UI/                                 ← TÀI NGUYÊN GIAO DIỆN
        │   ├── Sprites/                          Icons, backgrounds, buttons
        │   └── Fonts/                            Font chữ (Vietnamese support)
        │
        ├── ScriptableObjects/                  ← DỮ LIỆU TĨNH (sửa trong Inspector)
        │   ├── Items/                            ItemData assets (kẹo dừa, diều giấy...)
        │   └── Audio/                            SoundLibrary asset
        │
        └── Settings/                           ← CÀI ĐẶT URP
            └── (URP Asset, Quality, Renderer)
```

---

## 3. MÔ TẢ CHI TIẾT TỪNG SCRIPT

### 3.1 Core (3 scripts)

#### `GameState.cs`
- **Loại**: Enum
- **Chức năng**: Định nghĩa 4 trạng thái game: `MainMenu`, `Act1_RealWorld`, `Act2_MemoryWorld`, `Act3_Ending`
- **Dùng bởi**: GameManager

#### `GameManager.cs`
- **Loại**: MonoBehaviour, Singleton, DontDestroyOnLoad
- **Chức năng**:
  - Theo dõi `GameState` hiện tại
  - Quản lý cursor (ẩn khi FPS, hiện khi UI/menu)
  - Điều phối: `StartNewGame()`, `EnterMemoryWorld()`, `EnterEnding()`, `ReturnToMainMenu()`, `QuitGame()`
  - Pause/Resume game (`Time.timeScale`)
  - Event `OnGameStateChanged` để các hệ thống khác lắng nghe
- **Dùng bởi**: Tất cả systems

#### `SceneLoader.cs`
- **Loại**: MonoBehaviour, Singleton, DontDestroyOnLoad
- **Chức năng**:
  - Load scene async với hiệu ứng fade (Image đen full screen)
  - `LoadScene(sceneName, fadeDuration)` — fade đen chuẩn
  - `LoadSceneWithColor(sceneName, color, duration)` — fade vàng ấm cho xuyên không
  - Chặn input trong lúc fade (raycastTarget)
- **Setup**: Cần Canvas (Sort Order 999) + Image full screen đen, alpha=0

---

### 3.2 Player (2 scripts)

#### `FirstPersonController.cs`
- **Loại**: MonoBehaviour, RequireComponent(CharacterController)
- **Chức năng**:
  - Di chuyển WASD bằng `CharacterController.Move()`
  - Mouse Look: xoay ngang (body) + dọc (camera), clamp ±85°
  - Sprint: giữ Left Shift (3.5 → 6 speed)
  - Gravity: -9.81 * 2
  - Ground check: SphereCast
  - `LockMovement(bool)` — khóa di chuyển khi mini-game/UI
  - `TeleportTo(position, rotation)` — dịch chuyển tức thì
- **Không cần**: Jump (game exploration)

#### `PlayerInteraction.cs`
- **Loại**: MonoBehaviour
- **Chức năng**:
  - Mỗi frame: Raycast từ camera, distance 3m, LayerMask "Interactable"
  - Hit có `IInteractable` → hiện prompt trên HUD + highlight crosshair
  - Bấm E → gọi `currentTarget.Interact()`
  - `LockInteraction(bool)` — khóa khi đang mini-game

---

### 3.3 Interaction (6 scripts)

#### `IInteractable.cs` — Interface
```
- GetPromptText() → string     // "[E] Rửa chén"
- Interact() → void             // Logic khi bấm E
```

#### `InteractableBase.cs` — Abstract base
```
- promptText (SerializeField)
- isOneTimeUse (SerializeField)
- hasBeenUsed (tracking)
- Interact() → check one-time → gọi OnInteract()
- OnInteract() → abstract, class con override
```

#### `DishInteractable.cs`
- Kế thừa `InteractableBase`, prompt "[E] Rửa chén"
- `OnInteract()` → gọi `DishWashingGame.StartGame()`
- One-time use: true

#### `MarbleInteractable.cs`
- Kế thừa `InteractableBase`, prompt "[E] Chơi bi"
- `OnInteract()` → gọi `MarbleShootingGame.StartGame()`
- One-time use: true

#### `ShopInteractable.cs`
- Kế thừa `InteractableBase`, prompt "[E] Mua hàng"
- `OnInteract()` → gọi `ShopUI.Instance.OpenShop()`
- One-time use: **false** (mua nhiều lần)

#### `MemoryTrigger.cs`
- Kế thừa `InteractableBase`, prompt "[E] Chạm vào"
- `OnInteract()` → hiện dialogue → fade vàng ấm → load Act2
- One-time use: true

---

### 3.4 MiniGames (2 scripts)

#### `DishWashingGame.cs`
- **Cài đặt**: 5 chén, 3 giây/chén, 2 đồng/chén (tổng 10 đồng)
- **Flow**:
  1. Player bấm E vào bồn → `StartGame()`
  2. Lock player + hiện cursor
  3. Giữ chuột trái → ProgressBar tăng
  4. Thả chuột → dừng (KHÔNG giảm — game thư giãn)
  5. Bar đầy → chén sạch (swap material) → +2 đồng
  6. 5/5 chén → `CurrencyManager.Add(10)` + `MemoryCollectionManager.CollectMemory(Mother)`
  7. Hiện dialogue "Ký ức về Mẹ" → unlock player

#### `MarbleShootingGame.cs`
- **Cài đặt**: 5 bi mục tiêu, cần đẩy 3/5 ra ngoài, 5 lượt bắn
- **Flow**:
  1. Player bấm E → `StartGame()`
  2. Camera chuyển top-down view
  3. Kéo chuột → hiện LineRenderer hướng + thanh lực
  4. Thả chuột → `Rigidbody.AddForce(direction * force, Impulse)`
  5. Chờ bi dừng → check bi ngoài vòng
  6. Đủ 3/5 → WIN → `MemoryCollectionManager.CollectMemory(Friends)`
  7. Hết lượt chưa đủ → cho chơi lại (không penalty)
  8. Restore camera → unlock player
- **Physics**: Sphere Collider + Rigidbody (mass: 0.05, drag: 2, angular drag: 3)

---

### 3.5 Systems (4 scripts)

#### `CurrencyManager.cs`
- Singleton nhẹ (không DontDestroyOnLoad)
- `AddMoney(amount)`, `SpendMoney(amount) → bool`, `HasEnoughMoney(amount) → bool`
- Event `OnMoneyChanged(int)` → HUD update
- `ResetCurrency()` cho chơi lại

#### `InventoryManager.cs`
- `List<ItemData>` ownedItems
- `AddItem()`, `RemoveItem()`, `HasItem()`, `HasSpecialItem()`
- Event `OnItemAdded`, `OnItemRemoved`

#### `ShopManager.cs`
- `List<ItemData>` shopItems (gán trong Inspector)
- `PurchaseItem(item) → bool`:
  - Check đã mua chưa → check đủ tiền → trừ tiền → thêm inventory
  - Nếu `item.isSpecialItem` → trigger `MemoryCollectionManager.CollectMemory(SimpleJoy)`
- Event `OnPurchaseSuccess`, `OnPurchaseFailed`

#### `MemoryCollectionManager.cs`
- `Dictionary<MemoryType, bool>` — track 3 mảnh: Mother, Friends, SimpleJoy
- `CollectMemory(type)`:
  - Mark collected → play SFX → trigger UI animation → hiện dialogue
  - Nếu 3/3 → delay 3s → `GameManager.EnterEnding()`
- Enum `MemoryType { Mother, Friends, SimpleJoy }`

---

### 3.6 UI (7 scripts)

| Script | Chức năng chính |
|---|---|
| `HUDController.cs` | Crosshair (đổi màu khi highlight), prompt tương tác, hiện tiền, 3 icon mảnh ký ức (xám → vàng sáng khi thu thập, animation bounce) |
| `ProgressBarUI.cs` | Slider 0→1, label "Chén 2/5", fill color gradient (xanh nước → xanh lá), Show/Hide |
| `MarbleAimUI.cs` | Text lượt bắn, text bi trúng, slider lực (xanh → đỏ), Show/Hide |
| `DialogueUI.cs` | Panel dưới màn hình, title + body, hiệu ứng typewriter (0.03s/ký tự), fade in/out, auto-hide sau duration |
| `ShopUI.cs` | Panel shop, spawn item slots từ prefab, hiện tên/giá/icon/nút Mua, message "Đã mua!" / "Không đủ tiền!", nút Đóng, lock/unlock player |
| `MainMenuUI.cs` | `OnPlayButtonClicked()` → `GameManager.StartNewGame()`, `OnQuitButtonClicked()` → quit |
| `EndingUI.cs` | TextMeshPro thông điệp kết, CanvasGroup fade in (delay 1s, duration 2s), nút Chơi lại / Thoát |

---

### 3.7 Audio (2 scripts)

#### `SoundLibrary.cs` — ScriptableObject
- Chứa references đến tất cả AudioClip
- Chia nhóm: BGM (4 tracks), SFX Rửa chén (2), SFX Bắn bi (3), SFX Shop (2), SFX Memory (2), SFX UI (2), Ambient (3)

#### `AudioManager.cs` — Singleton, DontDestroyOnLoad
- 3 AudioSource: BGM (loop), SFX (one-shot), Ambient (loop)
- `PlayBGM(clip)` với crossfade
- `PlaySFX(clip)` hoặc `PlaySFX("marble_shoot")` theo tên
- Tự động đổi BGM khi GameState thay đổi (subscribe event)

---

### 3.8 Data (1 script)

#### `ItemData.cs` — ScriptableObject
```
- itemName: string           ("Kẹo dừa")
- description: string        ("Viên kẹo thơm mùi tuổi thơ")
- icon: Sprite               (Hình 2D)
- price: int                 (2 đồng)
- itemType: ItemType enum    (Candy, Toy, Special)
- isSpecialItem: bool        (true cho Con diều giấy → trigger ký ức)
```

**Items dự kiến:**

| Vật phẩm | Giá | Loại | Trigger ký ức? |
|---|---|---|---|
| Kẹo dừa | 2 đồng | Candy | ❌ |
| Kẹo kéo | 3 đồng | Candy | ❌ |
| Bi ve | 2 đồng | Toy | ❌ |
| **Con diều giấy** | **8 đồng** | **Special** | **✅** |

---

## 4. SƠ ĐỒ HỆ THỐNG

### 4.1 Flow gameplay chính

```
Player bấm E
       │
       ▼
IInteractable.Interact()
       │
       ├──→ DishInteractable → DishWashingGame
       │         │
       │         ├──→ CurrencyManager.AddMoney(+10)
       │         └──→ MemoryCollectionManager.CollectMemory(Mother) ✨
       │
       ├──→ MarbleInteractable → MarbleShootingGame
       │         │
       │         └──→ MemoryCollectionManager.CollectMemory(Friends) ✨
       │
       ├──→ ShopInteractable → ShopUI.OpenShop()
       │         │
       │         └──→ ShopManager.PurchaseItem()
       │                   │
       │                   ├──→ CurrencyManager.SpendMoney(-price)
       │                   ├──→ InventoryManager.AddItem()
       │                   └──→ (if special) MemoryCollectionManager.CollectMemory(SimpleJoy) ✨
       │
       └──→ MemoryTrigger → SceneLoader.LoadSceneWithColor(vàng ấm)
                                  → Act2_MemoryWorld

Khi 3/3 ✨ thu thập:
MemoryCollectionManager → delay 3s → GameManager.EnterEnding() → Act3
```

### 4.2 Flow tiền tệ

```
DishWashingGame (rửa 5 chén × 2đ = 10 đồng)
        │
        ▼
CurrencyManager (+10 đồng)
        │
        ▼
ShopManager (mua: kẹo dừa 2đ, kẹo kéo 3đ, bi ve 2đ = 7đ)
        │         (còn: 3 đồng, cần thêm 5đ nữa)
        ▼
⚠️ Không đủ mua Con diều giấy (8đ)
→ Player cần quay lại rửa thêm chén (nếu game cho phép)
→ HOẶC: điều chỉnh giá/reward cho cân bằng
```

> **Lưu ý**: Nên cân nhắc cho player rửa chén nhiều lần HOẶC tăng reward lên để đủ mua Con diều giấy mà không bị frustrating.

---

## 5. PHÂN CÔNG VAI TRÒ

### Team 3-4 người

| Vai trò | Trách nhiệm chính | Scripts phụ trách | Kỹ năng cần học |
|---|---|---|---|
| **🎮 Lead Dev** (TV1) | Core systems, Player, tích hợp | GameManager, SceneLoader, FirstPersonController, PlayerInteraction, IInteractable, InteractableBase, MemoryTrigger, MemoryCollectionManager | C# cơ bản, Unity scripting |
| **🕹️ Gameplay Dev** (TV2) | Mini-games, Economy, UI | DishWashingGame, MarbleShootingGame, CurrencyManager, ShopManager, InventoryManager, các Interactables, ProgressBarUI, MarbleAimUI, ShopUI | Unity Physics, UI Canvas |
| **🎨 3D Artist / Level** (TV3) | 3D models, Level design, Lighting | Không code, setup Scenes, Prefabs | Meshy/Tripo AI, Blender, Unity Scene |
| **🎵 UI/Audio** (TV4 hoặc kiêm) | UI design, Audio, VFX | HUDController, DialogueUI, MainMenuUI, EndingUI, AudioManager, SoundLibrary | Canvas/UI, tìm asset free |

### Quy tắc làm việc nhóm

- **Git**: Mỗi người branch riêng, merge qua Pull Request
- **Naming**: Scripts `PascalCase`, Variables `camelCase`, Prefab `Prefab_TenVat`
- **Commit**: Mỗi ngày ≥1 commit, message rõ ràng
- **Daily standup** (5-10 phút): Hôm qua? Hôm nay? Bị block?

---

## 6. LỘ TRÌNH 5 TUẦN

### Tổng quan

```
Tuần 1 ──→ Tuần 2 ──→ Tuần 3 ──→ Tuần 4 ──→ Tuần 5
NỀNTẢNG   GAMEPLAY   ECONOMY    TÍCHHỢP    POLISH
                     + STORY               + NỘP
```

---

### 📅 TUẦN 1: NỀN TẢNG (Foundation)

> **Mục tiêu**: Di chuyển FPS trong level blockout + tương tác cơ bản

| # | Task | Người | Ưu tiên |
|---|---|---|---|
| 1.1 | Setup Unity Project + URP + Git repo | Lead Dev | 🔴 P0 |
| 1.2 | First Person Controller (WASD + Mouse Look) | Lead Dev | 🔴 P0 |
| 1.3 | Raycast Interaction System + IInteractable | Lead Dev | 🔴 P0 |
| 1.4 | Blockout Level Act2 (ProBuilder/Cube) | 3D Artist | 🔴 P0 |
| 1.5 | Generate 3D models batch 1 bằng AI | 3D Artist | 🟡 P1 |
| 1.6 | Tìm hiểu Unity UI Canvas cơ bản | Gameplay Dev | 🟡 P1 |
| 1.7 | Tìm + download nhạc/SFX miễn phí | UI/Audio | 🟢 P2 |

**✅ Demo cuối tuần 1**: Nhân vật FPS đi trong level blockout, nhìn vào cube → hiện "[E]" → bấm E → log console.

---

### 📅 TUẦN 2: MINI-GAMES (Gameplay)

> **Mục tiêu**: 2 mini-games chơi được + HUD cơ bản

| # | Task | Người | Ưu tiên |
|---|---|---|---|
| 2.1 | Mini-game Rửa chén: Hold click + Progress Bar | Gameplay Dev | 🔴 P0 |
| 2.2 | Mini-game Bắn bi: Drag aim + Physics shot | Gameplay Dev | 🔴 P0 |
| 2.3 | HUD UI: Crosshair + Tiền + Mảnh ký ức | UI/Audio | 🔴 P0 |
| 2.4 | Tích hợp Interactable vào mini-games | Lead Dev | 🟡 P1 |
| 2.5 | 3D models batch 2 + Blender cleanup | 3D Artist | 🟡 P1 |
| 2.6 | Dress up level (thay cube → models thật) | 3D Artist | 🟡 P1 |
| 2.7 | CurrencyManager (logic tiền) | Lead Dev | 🟡 P1 |

**✅ Demo cuối tuần 2**: Chơi được rửa chén (nhận tiền), chơi được bắn bi. HUD hiển thị tiền.

---

### 📅 TUẦN 3: ECONOMY + STORY (Systems)

> **Mục tiêu**: Shop hoạt động, hệ thống ký ức hoàn chỉnh

| # | Task | Người | Ưu tiên |
|---|---|---|---|
| 3.1 | ItemData ScriptableObjects + ShopManager | Lead Dev | 🔴 P0 |
| 3.2 | ShopUI (Panel vật phẩm + nút Mua) | Gameplay Dev | 🔴 P0 |
| 3.3 | InventoryManager | Gameplay Dev | 🔴 P0 |
| 3.4 | MemoryCollectionManager (track 3/3) | Lead Dev | 🔴 P0 |
| 3.5 | Act1 Scene: Căn phòng cũ + MemoryTrigger | Lead + 3D | 🟡 P1 |
| 3.6 | DialogueUI + Narration | UI/Audio | 🟡 P1 |
| 3.7 | Audio integration: BGM + SFX | UI/Audio | 🟡 P1 |
| 3.8 | Lighting + URP Post-processing | 3D Artist | 🟡 P1 |

**✅ Demo cuối tuần 3**: Flow: rửa chén → tiền → tiệm tạp hóa → mua đồ. Thu thập ký ức.

---

### 📅 TUẦN 4: TÍCH HỢP (Integration)

> **Mục tiêu**: Game chơi được từ đầu đến cuối (Act1 → Act2 → Act3)

| # | Task | Người | Ưu tiên |
|---|---|---|---|
| 4.1 | Tích hợp tất cả systems | Lead Dev | 🔴 P0 |
| 4.2 | SceneLoader + Fade transitions | Lead Dev | 🔴 P0 |
| 4.3 | Act3 Ending scene | Lead + 3D | 🔴 P0 |
| 4.4 | Main Menu | Gameplay Dev | 🟡 P1 |
| 4.5 | Polish mini-games (balance, feel) | Gameplay Dev | 🟡 P1 |
| 4.6 | Particle effects (bụi nắng, đom đóm) | 3D Artist | 🟢 P2 |
| 4.7 | **Playtesting nội bộ lần 1** | **CẢ TEAM** | 🔴 P0 |

**✅ Demo cuối tuần 4**: Game chơi hoàn chỉnh Menu → Act1 → Act2 → Act3.

---

### 📅 TUẦN 5: POLISH + NỘP (Release)

> **Mục tiêu**: Sửa bugs, đánh bóng, build final

| # | Task | Người | Ưu tiên |
|---|---|---|---|
| 5.1 | Fix critical bugs | CẢ TEAM | 🔴 P0 |
| 5.2 | Polish UI/UX (animations, transitions) | Gameplay + UI | 🟡 P1 |
| 5.3 | Polish audio (volume balance) | UI/Audio | 🟡 P1 |
| 5.4 | Polish visuals (lighting, particles) | 3D Artist | 🟡 P1 |
| 5.5 | **Playtesting lần 2** (cho người ngoài chơi) | **CẢ TEAM** | 🔴 P0 |
| 5.6 | Fix bugs cuối | CẢ TEAM | 🔴 P0 |
| 5.7 | Build final (.exe) | Lead Dev | 🔴 P0 |
| 5.8 | Tài liệu nộp bài | CẢ TEAM | 🔴 P0 |

**✅ Kết quả**: Game hoàn chỉnh, build .exe, sẵn sàng nộp.

---

## 7. TASK BREAKDOWN

### 7.1 Mini-game Rửa chén — Chi tiết

```
Khởi tạo:
├── totalDishes = 5
├── washTimePerDish = 3 giây (giữ chuột)
└── moneyPerDish = 2 đồng (tổng: 10)

Gameplay Loop:
├── Player bấm E → StartGame()
├── Lock di chuyển + hiện cursor
├── Giữ chuột trái → progress += Time.deltaTime / washTime
├── Thả chuột → progress dừng (KHÔNG giảm)
├── progress >= 1 → chén sạch (swap material)
│   ├── CurrencyManager.Add(2)
│   └── Chuyển chén tiếp (reset progress)
└── 5/5 chén → hoàn thành
    ├── MemoryCollectionManager.CollectMemory(Mother)
    ├── DialogueUI "Ký ức về Mẹ..."
    └── Unlock player
```

### 7.2 Mini-game Bắn bi — Chi tiết

```
Khởi tạo:
├── targetMarbles = 5 (bi mục tiêu trong vòng tròn)
├── targetToKnock = 3 (cần đẩy ra 3/5)
├── maxShots = 5
├── minForce = 3, maxForce = 15
└── Bi physics: mass 0.05, drag 2, angular drag 3

Gameplay Loop:
├── Player bấm E → StartGame()
├── Camera → top-down view
├── MouseDown → bắt đầu aim
├── MouseDrag → hiện LineRenderer (hướng ngược kéo, dài = lực)
├── MouseUp → Rigidbody.AddForce(dir * force, Impulse)
├── Chờ tất cả bi velocity < 0.05 → kiểm tra
│   ├── Bi ngoài vòng → marblesKnocked++
│   ├── Đủ 3/5 → WIN
│   ├── Hết lượt < 3/5 → cho chơi lại (reset)
│   └── Còn lượt → reset bi player
└── WIN → MemoryCollectionManager.CollectMemory(Friends) → restore camera
```

### 7.3 Shop System — Chi tiết

```
Flow mua hàng:
├── Player bấm E vào quầy → ShopUI.OpenShop()
├── Lock player + hiện cursor
├── Hiện panel: danh sách items từ ShopManager
│   └── Mỗi item: icon + tên + giá + nút Mua
├── Bấm Mua → ShopManager.PurchaseItem(item)
│   ├── Check đã mua? → "Đã mua rồi!"
│   ├── Check tiền? → CurrencyManager.SpendMoney()
│   │   └── Không đủ → "Không đủ tiền!"
│   ├── OK → InventoryManager.AddItem()
│   ├── Nếu isSpecialItem → MemoryCollectionManager.CollectMemory(SimpleJoy)
│   └── Update UI (disable nút, hiện message)
└── Bấm Đóng → ShopUI.CloseShop() → unlock player
```

---

## 8. AI TOOLS & WORKFLOW

### 8.1 Tạo 3D Assets

| Tool | Dùng cho | Tip |
|---|---|---|
| **Meshy AI** | Props đơn giản: chén, bi, bếp, kệ, kẹo, diều | Prompt style: "low-poly", "cartoon", "chibi" |
| **Tripo AI** | Objects phức tạp: quầy tạp hóa, cây khế | Tốt với objects, kém với characters |
| **Blender** | Fix mesh AI, retopology, UV, animation | Workflow: AI → Blender fix → FBX → Unity |

### 8.2 Workflow 3D model

```
1. Prompt AI: "A low-poly chibi style Vietnamese kitchen sink, cartoon, warm colors"
2. Download .fbx/.glb
3. Blender:
   ├── Check mesh (xóa faces thừa, fix normals)
   ├── Scale đúng tỷ lệ
   ├── UV map (nếu cần)
   └── Export FBX (Apply Transform ✓)
4. Unity:
   ├── Import → set Scale Factor
   ├── Assign materials
   └── Tạo Prefab
```

### 8.3 Animation & Audio

| Nguồn | Dùng cho |
|---|---|
| **Mixamo** (free) | Animation nhân vật: idle, walk |
| **Blender** | Animation props: chén rung, bi lăn, diều bay |
| **Freesound.org** | SFX miễn phí |
| **Pixabay Music** | Nhạc nền lo-fi/acoustic miễn phí |
| **Suno AI / Udio** | Generate nhạc nền custom |

### 8.4 AI cho Code

- **GitHub Copilot / Antigravity**: Viết code C#, debug
- **ChatGPT / Gemini**: Hỏi cách implement, giải thích lỗi
- **Nguyên tắc**: Luôn HIỂU code AI viết trước khi dùng!

---

## 9. QUẢN LÝ RỦI RO

### Bảng rủi ro

| Rủi ro | XS | Tác động | Dự phòng |
|---|---|---|---|
| 3D model AI bị xấu | Cao | TB | Asset Store miễn phí thay thế |
| Physics bắn bi khó | TB | Cao | Đơn giản hóa: bắn thẳng, không drag |
| Tích hợp lỗi | Cao | Cao | Test từng system riêng, merge thường xuyên |
| Không kịp tiến độ | TB | Cao | Cắt features P2 trước, đơn giản hóa P1 |
| Git conflict | TB | TB | Mỗi người làm folder riêng |

### 🚨 Features có thể CẮT (theo thứ tự)

1. ❌ Particle effects (bụi nắng, đom đóm)
2. ❌ NPC người mẹ animation
3. ❌ Nhật ký/ghi chú ở Act1
4. ⚠️ Inventory UI (chỉ cần shop mua)
5. ⚠️ Act1 scene (bắt đầu thẳng Act2 + text giới thiệu)
6. ⚠️ Act3 cutscene (thay bằng text + fade)

### ⛔ KHÔNG ĐƯỢC CẮT (Core)

- Player controller + interaction
- 2 mini-games (rửa chén + bắn bi)
- Shop system
- Memory collection (3/3 → kết thúc)
- Scene transitions cơ bản

---

## 10. CHECKLIST CHUẨN BỊ (Tuần 0)

- [ ] Cài Unity Hub + **Unity 2022.3 LTS** (cả team cùng version!)
- [ ] Cài Visual Studio / VS Code + C# extension
- [ ] Tạo **GitHub repo** + mời cả team
- [ ] Cài **Git** + hiểu: clone, commit, push, pull, branch, merge
- [ ] Tạo tài khoản: **Meshy AI**, **Tripo AI**, **Mixamo**, **Freesound**
- [ ] Cài **Blender** (version mới nhất)
- [ ] Xem 2-3 video YouTube về Unity cơ bản (Interface, GameObject, Component, Script)
- [ ] Đọc lại **GDD** — cả team cùng hiểu game

---

## 11. HƯỚNG DẪN SETUP UNITY

### Bước 1: Mở Project
- Unity Hub → Add → chọn folder root project
- Chọn Unity **2022.3 LTS**, template **3D (URP)**

### Bước 2: Tạo Layers
- Edit → Project Settings → Tags and Layers
- Thêm: **"Interactable"** (Layer 6), **"Ground"** (Layer 7)

### Bước 3: Tạo 4 Scenes
- File → New Scene → Save vào `Assets/_Project/Scenes/`
  - `MainMenu.unity`
  - `Act1_RealWorld.unity`
  - `Act2_MemoryWorld.unity`
  - `Act3_Ending.unity`
- File → Build Settings → Add tất cả 4 scene

### Bước 4: Setup GameManager (MainMenu scene)
1. Empty GameObject "GameManager" → thêm `GameManager.cs`
2. Empty GameObject "SceneLoader" → thêm `SceneLoader.cs`
   - Tạo Canvas (Sort Order: 999) → Image full-screen đen, alpha=0
3. Empty GameObject "AudioManager" → thêm `AudioManager.cs`
   - Thêm 3 AudioSource (BGM, SFX, Ambient)

### Bước 5: Setup Player (Act1 & Act2 scenes)
1. Empty GameObject "Player"
2. Thêm `CharacterController`
3. Thêm `FirstPersonController.cs` + `PlayerInteraction.cs`
4. Camera là child → gán vào `playerCamera`

### Bước 6: Setup Interactables (Act2 scene)
- Mỗi vật: Layer = "Interactable" + Collider + Script tương ứng

### Bước 7: Tạo ScriptableObject Items
- Right-click → Create → KuTy → Item Data
- Tạo 4 items: Kẹo dừa (2đ), Kẹo kéo (3đ), Bi ve (2đ), Con diều (8đ ★)

---

> **Tài liệu này là tài liệu tham khảo chính cho team phát triển.**  
> **Đọc kết hợp với GDD** (`game-design-document.md`) để hiểu đầy đủ game.
