# Lab Experiment Escape — Roadmap phát triển phần còn lại của game

> Người lập: Claude (cùng vienthl) — Ngày: 03/07/2026 — Branch hiện tại: `develop`
> Tài liệu này là kế hoạch phát triển tổng thể, chia phase → feature → task, kèm thiết kế logic chi tiết, tiêu chí nghiệm thu và phân công theo vai trò team.

---

## 0. Hiện trạng (đã kiểm chứng trong code, không phải phỏng đoán)

### Đã có và chạy được
| Hệ thống | Trạng thái |
|---|---|
| Player di chuyển 4 hướng, animation 8 hướng | ✅ Hoàn chỉnh (velocity-based, có Interpolate) |
| Ném bình lửa/điện, cooldown, hit effect | ✅ Hoàn chỉnh |
| Máu player: i-frame, nhấp nháy, knockback | ✅ Hoàn chỉnh |
| Quái Quai1: wander + chase (LOS, hysteresis), máu, thanh máu, chết fade-out | ✅ Hoàn chỉnh |
| Game Over + R restart | ✅ Hoạt động (OnGUI) |
| Camera follow SmoothDamp | ✅ Hoàn chỉnh |

### Lỗ hổng lớn nhất (điểm mấu chốt của roadmap)
1. **Animation E (uống) và F (nhặt) là "vỏ rỗng"** — chỉ chạy animation, không có logic. `PlayerHealth.Heal()` và `EnemyHealth.Heal()` tồn tại nhưng **chưa nơi nào gọi**.
2. **Không có mục tiêu chơi**: không cửa thoát, không chìa khóa, không điều kiện thắng. Game hiện tại chỉ là "đấu trường sống sót vô hạn".
3. **Chỉ có 1 scene Level1** (+ `MainScene.unity` cũ ở gốc Assets — cần dọn). Thiết kế gốc cần 5 scene: Main Menu, Level 1–3, End Screen.
4. **Chỉ có 1 loại quái** — thiết kế gốc có Echo Drone, Security Bot, Mutant Plant, boss NEXUS-CORE.
5. **Quái là object đặt tay trong scene, chưa phải prefab** — sửa 1 con phải sửa 4 chỗ; sang Level 2/3 sẽ không nhân bản được.
6. **Thư mục `Assets/audio/music`, `Assets/audio/sfx`, `Assets/ui` đang RỖNG** — chưa có bất kỳ âm thanh/asset UI nào.
7. UI toàn bộ bằng `OnGUI` (health bar, game over) — đủ cho prototype nhưng không scale cho menu/inventory/dialogue.
8. Không có save/checkpoint, không pause, không settings.
9. Cốt truyện (Nexus Fluid, memory loss, 2 ending) **chưa xuất hiện trong game** dưới bất kỳ hình thức nào.

---

## 1. Định hướng tổng thể

**Trục phát triển đề xuất: biến "đấu trường" thành "escape game có mục tiêu".** Combat đã ổn; thứ game thiếu là **lý do để di chuyển** (mục tiêu, chìa khóa, cửa, câu chuyện). Ưu tiên: *core loop hoàn chỉnh cho Level 1 trước, rồi mới nhân rộng nội dung*.

**3 trụ cột thiết kế (pillars):**
1. **Escape có nhịp độ** — mỗi phòng là một bài toán nhỏ: né/diệt quái → lấy đồ → mở đường.
2. **Tài nguyên khan hiếm** — bình thuốc ném và bình hồi máu là đồ nhặt được, không phải vô hạn → quyết định "đánh hay né" có ý nghĩa.
3. **Câu chuyện qua môi trường** — ghi chú, terminal, lore pickup thay vì cutscene dài (vừa sức team 4 người).

**Thứ tự phase:** P0 nền tảng kỹ thuật → P1 core loop Level 1 → P2 chiều sâu combat/quái → P3 Level 2 → P4 Level 3 + boss + ending → P5 menu/audio/polish/save. (P5 có thể chạy song song từ sau P1 vì thuộc mảng UI/Systems của thành viên riêng.)

---

## 2. PHASE 0 — Nền tảng kỹ thuật (làm trước, tránh nợ kỹ thuật nhân 3 lần khi sang Level 2/3)

### 0.1 Prefab hóa quái + ScriptableObject stats
- **Việc**: Kéo Quai1 trong scene thành prefab `Assets/Enemy/Quai1/Quai1.prefab`; 4 con trong Level1 thành prefab instance.
- **`EnemyStats` (ScriptableObject)**: `maxHealth, moveSpeed, chaseSpeed, detectRange, loseRange, damagePerHit, damageCooldown, attackPauseDuration`. `Quai1AutoMove`/`EnemyDamage`/`EnemyHealth` đọc từ stats asset (giữ field cũ làm fallback nếu stats null — không phá scene hiện tại).
- **Lý do**: Level 2/3 cần nhiều biến thể quái; chỉnh chỉ số 1 chỗ, designer (Long Viên) cân bằng không cần đụng code.
- **Nghiệm thu**: xóa 4 quái cũ, kéo 4 prefab instance vào, game chạy y hệt.

### 0.2 GameManager + Scene flow
- **`GameManager` (singleton, DontDestroyOnLoad)**: trạng thái run (Playing / Paused / GameOver / LevelComplete), API `LoadLevel(int)`, `RestartLevel()`, `CompleteLevel()`.
- **`GameEvents` (static event hub)**: `OnPlayerDied`, `OnEnemyDied(EnemyHealth)`, `OnItemPicked(ItemData)`, `OnObjectiveCompleted(string)`, `OnLevelCompleted` — các hệ UI/audio/objective đăng ký nghe, tránh GetComponent chằng chịt giữa các script.
- **Build Settings**: thêm scene theo index: `0 MainMenu, 1 Level1, 2 Level2, 3 Level3, 4 EndScreen`. Dọn `Assets/MainScene.unity` (xóa hoặc chuyển vào `Assets/scene/_deprecated/`).
- **Nghiệm thu**: R restart đi qua GameManager thay vì `SceneManager` gọi thẳng trong `GameOverUI`; chuyển scene không mất nhạc nền (khi có audio).

### 0.3 Chuyển UI sang Canvas (Screen Space)
- Thay `OnGUI` của `PlayerHealthUI` + `GameOverUI` bằng Canvas prefab `HUD.prefab`: thanh máu (Image fill), số bình thuốc mang theo, khung objective, panel Game Over.
- **Lý do**: OnGUI không làm được inventory/dialogue/menu; làm 1 lần dùng cho mọi level.
- **Giữ nguyên** `WorldHealthBar` của quái (sprite-based, hoạt động tốt).
- **Nghiệm thu**: HUD hiển thị đúng ở các độ phân giải 16:9 / 16:10; Game Over vẫn hiện sau 0.8s, R restart.

### 0.4 AudioManager (khung trước, asset sau)
- `AudioManager` singleton: `PlayMusic(clip, loop)`, `PlaySfx(clip)`, `SetMusicVolume/SetSfxVolume` (lưu PlayerPrefs). 2 AudioSource (music/sfx).
- Điểm cắm sẵn (gọi qua GameEvents): ném bình, bình nổ, player trúng đòn, quái chết, nhặt đồ, mở cửa, game over, victory.
- Nguồn asset miễn phí gợi ý: Kenney.nl (sci-fi sounds), freesound.org, OpenGameArt (nhạc nền lab/ambient).
- **Nghiệm thu**: mute/unmute hoạt động, không lỗi khi clip chưa gán (null-safe).

**Ước lượng P0: ~1 tuần** (0.1 + 0.2 song song với 0.3 + 0.4 nếu chia 2 người).

---

## 3. PHASE 1 — Hoàn thiện core loop Level 1 (ưu tiên cao nhất — game "có thể thắng")

### 1.1 Hệ thống Item & Pickup (làm phím F có thật)
**Thiết kế data:**
- `ItemData` (ScriptableObject): `id, tên, icon, loại (HealPotion / ThrowablePotion / KeyCard / LoreNote), giá trị (heal amount / số lượng đạn / id cửa)`.
- `WorldItem` (MonoBehaviour, trigger collider): nằm trên sàn, nhấp nhô nhẹ (sin bob), highlight khi player đứng gần (đổi màu viền hoặc hiện icon phím `F`).

**Logic nhặt (F):**
1. `PlayerInteractor` (script mới trên Player): `OverlapCircle` bán kính ~0.6 tìm `WorldItem`/`Interactable` gần nhất.
2. Có item trong tầm + bấm F → chạy `PickUpRoutine()` sẵn có (animation đã hoạt động) → giữa routine (`yield` ~0.4s) gọi `inventory.Add(item)` + `Destroy(worldItem)` + SFX.
3. **Không có gì trong tầm → KHÔNG chạy animation nhặt** (sửa `PlayerMovement.Update`: F chỉ kích hoạt khi `PlayerInteractor.HasTarget`). Đây là nâng cấp logic quan trọng: hiện tại F là animation vô nghĩa.

**Inventory tối giản (đủ dùng, không làm grid phức tạp):**
- `PlayerInventory`: `int firePotions, lightningPotions, healPotions; List<string> keyIds; List<ItemData> loreNotes`.
- HUD hiển thị: icon + số lượng 3 loại bình, icon chìa khóa đang có.
- **`PlayerAttack` đổi sang tiêu hao đạn**: ném lửa trừ `firePotions`, hết đạn → không ném + SFX "click rỗng". Đặt số đạn khởi đầu Level 1: 6 lửa / 2 điện (Long Viên cân bằng sau).

**Nghiệm thu**: nhặt được bình trên sàn, số lượng HUD tăng; ném hết đạn thì không ném được; F xa item không chạy animation.

### 1.2 Uống thuốc hồi máu (làm phím E có thật)
- Bấm E: chỉ chạy `DrinkRoutine()` nếu `healPotions > 0` **và** máu chưa đầy; giữa routine gọi `PlayerHealth.Heal(40)` (hàm có sẵn, chưa ai gọi) + trừ 1 bình + SFX + hiệu ứng số máu xanh nổi lên (floating text đơn giản).
- Máu đầy/hết bình → không chạy animation (đồng bộ nguyên tắc với F).
- **Nghiệm thu**: đứng máu 50/100 uống → 90/100, trừ 1 bình; máu đầy bấm E không có gì xảy ra.

### 1.3 Cửa + Chìa khóa + Điều kiện thắng Level 1
**Thiết kế:**
- `Door` (Interactable): `requiredKeyId`, 2 trạng thái Locked/Open (đổi sprite + tắt collider). Đứng gần + F: có key → mở (animation/SFX) ; không key → hiện text "Cần thẻ Keycard B1" 1.5s.
- `ExitZone` (trigger ở cửa thoát cuối map): player bước vào khi cửa đã mở → `GameManager.CompleteLevel()` → panel "Level Complete" (thống kê: thời gian, quái diệt, đồ nhặt) → nút sang Level 2 (tạm thời quay Main Menu khi Level 2 chưa có).
- **Bố cục Level 1 đề xuất** (Long Viên): keycard đặt ở phòng có 2 quái canh — buộc người chơi chạm trán ít nhất 1 lần; đường về cửa thoát có lối tắt mở được từ 1 phía (shortcut kinh điển).
- **Quái rơi đồ**: Quai1 chết rơi 30% bình lửa, 10% bình hồi máu (`LootTable` đơn giản trong `EnemyHealth.Die()` — instantiate `WorldItem`).

**Nghiệm thu**: đi hết vòng lặp nhặt key → mở cửa → vào ExitZone → màn hình thắng. Không key thì cửa không mở.

### 1.4 Objective HUD + hướng dẫn đầu game
- `ObjectiveManager`: danh sách objective theo level (`"Tìm keycard"` → `"Mở cửa Containment"` → `"Thoát khỏi khu B1"`), cập nhật qua GameEvents, hiện góc phải HUD, gạch ngang khi xong.
- Màn hình đầu Level 1: overlay 3 dòng phím tắt (WASD / chuột / E / F) tự ẩn sau 5s hoặc khi player di chuyển.
- **Nghiệm thu**: objective đổi đúng thứ tự theo tiến trình thật.

**Ước lượng P1: ~2 tuần.** Kết thúc P1, game là một game hoàn chỉnh mini: có thắng, có thua, có tài nguyên.

---

## 4. PHASE 2 — Chiều sâu combat & hệ quái (nâng "logic hay hơn")

### 2.1 Hiệu ứng riêng cho 2 loại bình (hiện chỉ khác số damage — lãng phí design)
- **Bình lửa (25%)**: thêm **DoT** — đốt 5%/giây trong 3s (`BurnEffect` component gắn tạm lên quái, tick bằng coroutine, stack refresh không cộng dồn). Sprite quái nhuộm cam nhạt khi đang cháy.
- **Bình điện (50%)**: thêm **stun 1.2s** — quái đứng yên (set `attackPauseTimer` qua API mới `Quai1AutoMove.Stun(duration)`), lóe trắng. Cân bằng lại: điện hiếm (đạn ít), lửa phổ thông.
- **Nghiệm thu**: quái cháy chết sau DoT dù không bị ném thêm; quái bị điện đứng im đúng 1.2s.

### 2.2 Echo Drone — quái tuần tra theo lộ trình (đúng thiết kế gốc Level 1)
- Refactor nhẹ: tách phần "di chuyển 4 hướng + raycast tường + sprite 4 hướng" của `Quai1AutoMove` thành base class `EnemyMovement4Dir`; `Quai1AutoMove` (wander+chase) và `PatrolMovement` mới cùng kế thừa.
- `PatrolMovement`: đi theo mảng waypoint (`Transform[]`), tới nơi → chờ 1s → điểm kế; phát hiện player (dùng lại detect+LOS sẵn có) → **hú còi** (SFX + dấu `!` trên đầu 0.5s) → chase; mất dấu → quay về waypoint gần nhất.
- **Nghiệm thu**: drone đi đúng vòng, phát hiện → báo động → đuổi, mất dấu → về tuyến.

### 2.3 Spawner + Wave phòng thủ cục bộ (dùng cho các "phòng khóa")
- `EnemySpawner`: kích hoạt khi player vào phòng (trigger), khóa cửa phòng, spawn N quái theo wave, diệt hết → mở khóa (mini lockdown arena — đúng chất lab bị phong tỏa).
- Dùng prefab quái từ P0. Giới hạn đồng thời ≤ 5 con (hiệu năng + độ khó).
- **Nghiệm thu**: vào phòng cửa sập xuống, diệt hết wave cửa mở, không respawn khi quay lại.

### 2.4 Game feel combat bổ sung (nhỏ nhưng đáng)
- Hit-stop 0.05s khi bình trúng quái; camera shake nhẹ (0.1s, biên độ 0.1) khi player trúng đòn; damage number nổi trên đầu quái; vệt sáng (trail) sau bình điện.
- **Nghiệm thu**: cảm giác "đánh có lực" — test mù với 1 bạn ngoài team.

**Ước lượng P2: ~2 tuần.**

---

## 5. PHASE 3 — Level 2: Security Nexus (nội dung + cơ chế mới)

### 3.1 Map Level 2 (Long Viên)
- Chủ đề: hành lang an ninh, camera, cửa nhiều lớp. Tận dụng tileset `Assets/sprites/map` hiện có; nếu thiếu, bổ sung từ cùng pack itch.io/Kenney đã dùng.
- Cấu trúc: 3 khu (Server / Giám sát / Kho vũ khí), mỗi khu 1 keycard màu khác nhau, khu cuối cần cả 3 (hub-and-spoke — người chơi tự chọn thứ tự).

### 3.2 Security Bot (đúng thiết kế gốc: giáp + detect rộng + rơi keycard)
- Kế thừa `EnemyMovement4Dir` + stats riêng: máu 200, detectRange 6, chaseSpeed 3, **giáp**: nhận 50% damage từ lửa (yếu điện — dạy người chơi dùng đúng bình qua gameplay).
- Chết **chắc chắn rơi keycard** nếu là bot "canh cửa" (flag `guaranteedDrop` trên spawner/prefab instance).

### 3.3 Mutant Plant — hazard môi trường
- Đứng yên, nhả **đám mây độc** hình tròn bán kính 1.5 mỗi 4s (DoT 5/s khi player đứng trong, hiệu ứng màn hình viền xanh). Diệt được bằng lửa (x2 damage), điện vô hiệu.
- Vai trò level design: chặn lối tắt — người chơi tốn đạn lửa hoặc đi vòng.

### 3.4 Puzzle "tắt AI an ninh" (điểm nhấn cốt truyện Level 2)
- 3 terminal (Interactable F) rải ở 3 khu → kích hoạt đủ 3 → cửa phòng điều khiển mở → terminal cuối chạy đoạn thoại text: **twist Rivera đã ký thỏa thuận thí nghiệm** (đọc từ `LoreNote` asset). Đây là nơi cốt truyện chính thức vào game.
- Dialogue box tối giản: panel + typewriter effect + Space để qua trang (`DialogueBox` dùng chung về sau).

**Nghiệm thu P3**: chơi liền mạch Level 1 → 2, thắng Level 2 bằng đủ 3 keycard + 3 terminal; chết ở Level 2 restart đúng Level 2.
**Ước lượng: ~2.5 tuần.**

---

## 6. PHASE 4 — Level 3: Core Abyss, boss, lựa chọn đạo đức, 2 ending

### 4.1 Boss NEXUS-CORE
- **Cấu trúc 3 pha theo máu** (máy trạng thái đơn giản — enum + switch trong `NexusCoreBoss`):
  - **Pha 1 (100–70%)**: đứng giữa arena, bắn đạn turret nhắm player mỗi 1.5s (projectile của địch — tái dùng `Projectile` với flag `hostile`, đổi filter tag).
  - **Pha 2 (70–35%)**: thêm spawn 2 Echo Drone con mỗi 10s (tối đa 4); turret bắn chùm 3 viên rẻ quạt.
  - **Pha 3 (<35%)**: lộ **lõi yếu điểm** 3s mỗi 8s (chỉ nhận damage lúc lộ lõi — nhịp đánh-né rõ ràng), bắn vòng tròn 8 hướng.
- Thanh máu boss riêng trên HUD (to, dưới đáy màn hình).
- **Nghiệm thu**: đủ 3 pha chuyển đúng ngưỡng; chết ở pha nào restart lại boss từ đầu; đạn boss không hủy đạn player (đã có filter Projectile-vs-Projectile — cần thêm phân biệt phe: đạn địch **được phép** trúng player).

### 4.2 Lựa chọn đạo đức + 2 ending (đúng thiết kế gốc)
- Sau khi boss chết: 2 Interactable — **"Phá lò phản ứng"** (Good) / **"Hấp thụ Nexus Fluid"** (Bad). Chọn xong khóa lựa chọn kia.
- `EndScreen` scene: nhận `GameManager.endingChoice`, hiện 1 trong 2 đoạn kết (text + 1 ảnh tĩnh mỗi ending — vừa sức, không cần cutscene động), nút về Main Menu.
- **Trạng thái Infected (nếu còn thời gian — nice-to-have)**: đo tổng thời gian đứng trong vùng Nexus Fluid qua các level; vượt ngưỡng → sprite player đổi sang bộ "dark glowing veins" (asset thiết kế gốc đã định) + tự khóa ending Good (chiều sâu cho người chơi lại lần 2). *Cắt được nếu trễ tiến độ.*

**Ước lượng P4: ~2.5 tuần.**

---

## 7. PHASE 5 — Meta systems & polish (song song từ sau P1, mảng của UI/UX & Systems)

### 5.1 Main Menu (scene index 0)
- Nền: ảnh lab + hiệu ứng đèn nhấp nháy (đã có sẵn `lightBulb` animation controller trong Assets!). Nút: Chơi mới / Tiếp tục / Cài đặt / Thoát.

### 5.2 Pause menu
- ESC → `Time.timeScale = 0` + panel (Tiếp tục / Chơi lại / Cài đặt / Về menu). **Lưu ý kỹ thuật**: mọi coroutine gameplay đang dùng `WaitForSeconds` (bị ảnh hưởng đúng bởi timescale — ổn), nhưng animation flash của PlayerHealth cũng dừng — chấp nhận được khi pause.

### 5.3 Checkpoint & Save
- **Trong level**: checkpoint tại cửa mỗi khu (respawn tại checkpoint với máu 60%, giữ inventory) thay vì restart cả level — giảm ức chế.
- **Giữa session**: save PlayerPrefs/JSON: level cao nhất đã tới + settings. KHÔNG save giữa level (giữ đơn giản).

### 5.4 Settings
- Âm lượng nhạc/SFX (nối AudioManager P0), fullscreen/windowed, reset save.

### 5.5 Polish tổng
- SFX/nhạc phủ toàn bộ (danh sách điểm cắm đã có từ P0.4); ambient riêng mỗi level (Containment u ám / Nexus điện tử / Abyss trầm).
- Minimap đơn giản (nice-to-have): camera phụ render layer riêng, góc phải trên.
- Màn hình loading giữa level với 1 câu lore ngẫu nhiên.

**Ước lượng P5: ~2 tuần rải rác song song.**

---

## 8. Phân công đề xuất (theo vai trò đã có của team)

| Thành viên | Vai trò | Phụ trách chính |
|---|---|---|
| **Long Viên** (Leader, Level Design) | Điều phối + map | Bố cục Level 1 hoàn chỉnh (P1.3), map Level 2/3 (P3.1, P4), đặt quái/item/checkpoint, cân bằng chỉ số qua EnemyStats |
| **Anh Huy** (Player & Item) | P1 trọn gói | ItemData/WorldItem/Inventory/Interactor (P1.1–1.2), tiêu hao đạn, hiệu ứng bình lửa/điện (P2.1) |
| **Anh Vủ** (Enemy & NPC) | P0.1 + P2 + P4.1 | Prefab hóa + EnemyStats, EnemyMovement4Dir refactor, Echo Drone patrol, Security Bot, Mutant Plant, Spawner, boss NEXUS-CORE |
| **Trung Hiếu** (UI/UX & Systems) | P0.2–0.4 + P5 | GameManager/GameEvents, Canvas HUD, AudioManager, Objective HUD, Dialogue box, Menu/Pause/Save/Settings, End Screen |

Nhánh git đề xuất: mỗi phase một nhánh feature (`feature/p1-items`, `feature/p2-enemies`…) merge về `develop`; `main` chỉ nhận build ổn định cuối mỗi phase.

---

## 9. Lịch tổng thể đề xuất (~10 tuần)

| Tuần | Mốc |
|---|---|
| 1 | P0 xong: prefab quái, GameManager, Canvas HUD, khung Audio |
| 2–3 | P1 xong: **Level 1 thắng được** — build demo #1 cho bạn bè test |
| 4–5 | P2 xong: 2 loại bình khác biệt, Echo Drone patrol, phòng wave |
| 6–7 | P3 xong: Level 2 + Security Bot + Mutant Plant + twist cốt truyện — demo #2 |
| 8–9 | P4 xong: Level 3 + boss + 2 ending — **content complete** |
| 10 | P5 chốt + full playtest + sửa bug + build final |

**Nguyên tắc cắt giảm khi trễ (thứ tự hy sinh):** Infected state → Minimap → Mutant Plant → pha 3 của boss → giảm Level 3 còn boss arena duy nhất. **Không bao giờ cắt**: P1 (điều kiện thắng), 2 ending (yêu cầu thiết kế gốc).

---

## 10. Rủi ro & đối sách

| Rủi ro | Đối sách |
|---|---|
| Refactor `Quai1AutoMove` thành base class làm hỏng quái đang chạy tốt | Làm trên nhánh riêng; giữ Quai1 nguyên hành vi, test song song trước khi merge; EnemyStats có fallback về field cũ |
| Đạn boss (hostile projectile) xung đột filter "Projectile bỏ qua Projectile" vừa thêm | Thêm enum `ProjectileTeam {Player, Enemy}`; chỉ bỏ qua đạn **cùng phe**; đạn địch trúng player gọi `TakeDamage(dmg, vị trí đạn)` (knockback sẵn có) |
| Scene .unity conflict khi 4 người cùng sửa | Quy ước: 1 người sở hữu 1 scene tại 1 thời điểm; logic để trong prefab tối đa; bật Force Text serialization (đang bật sẵn) |
| Asset audio/UI tìm không kịp | Chốt nguồn ngay tuần 1 (Kenney all-in-1 pack); placeholder beep trước, thay sau |
| Ôm đồm inventory phức tạp | Chốt cứng: inventory là các biến đếm, không grid, không drag-drop |

---

## 11. Việc nên làm NGAY tuần này (theo thứ tự)

1. Commit các fix combat vừa xong lên `develop` (sau khi test trong Unity Editor theo checklist plan trước).
2. Prefab hóa Quai1 (P0.1) — 1 buổi, mở khóa mọi việc về quái sau này.
3. Dựng `GameManager` + Build Settings 5 scene (P0.2) — khung xương của mọi phase.
4. Anh Huy bắt đầu `ItemData` + `WorldItem` + nhặt bình hồi máu (P1.1) — tính năng "ăn tiền" nhất với người chơi.
5. Long Viên phác bố cục Level 1 bản escape (vị trí keycard, cửa, exit) trên giấy/Tiled trước khi đặt tile.
