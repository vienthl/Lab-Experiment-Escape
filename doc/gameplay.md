# Gameplay

Tài liệu gameplay trò chơi.

## Luật chơi

Dr. Alex Rivera phải sống sót qua 2 khu vực của Facility-07: **Level 1 (Containment Wing)** — dọn sạch các đợt quái để mở cửa thoát; **Level 2 (Security Nexus)** — hạ Boss2, uống bình cure để hết nhiễm độc rồi thoát ra trước khi hết giờ. Kết thúc theo 1 trong 2 hướng: **Happy Ending** (đã chữa nhiễm độc + thoát kịp giờ) hoặc **Bad Ending** (hết giờ trước khi chữa xong).

## Điều khiển

| Phím | Hành động |
|---|---|
| W/A/S/D hoặc mũi tên | Di chuyển 4 hướng |
| Chuột trái | Ném bình lửa (tốn 1 bình, trừ 1 điểm) |
| Chuột phải | Ném bình điện (tốn 1 bình, trừ 2 điểm) |
| F | Nhặt vật phẩm gần nhất trong tầm |
| E | Uống bình hồi máu — hoặc uống bình cure để hết nhiễm độc nếu đang nhiễm và có bình |
| R | Kích hoạt khiên bảo vệ 20 giây (nếu đã nhặt được mũ thủy tinh từ MrX) |
| ESC | Tạm dừng (Pause) |

## Cấp độ và thử thách

- **Level 1 — Containment Wing:** phòng khóa kiểu wave (`LockdownRoomController`) — 2 đợt Quai1 rồi tới boss **MrX**. Giết xong tất cả thì cửa tự mở. Giết MrX xong rơi ra **mũ khiên thủy tinh** (nhặt 1 lần, dùng vĩnh viễn cả game).
- **Level 2 — Security Nexus:** đợt Bug214 rồi tới **Boss2** (có thêm khả năng bắn đạn tầm xa). Vùng nhìn quanh Player bị giới hạn kiểu *Among Us* (`VisionLimiter`) — vùng sáng lớn hơn nếu mang đủ bình lửa. Có giới hạn thời gian (đồng hồ đếm ngược) — hết giờ trước khi chữa xong nhiễm độc thì tự động vào **Bad Ending**.

## Luồng logic chi tiết

### 1. Player tấn công Quái (ném bình lửa/điện)

**File liên quan:** `PlayerAttack.cs` → `Projectile.cs` → `EnemyHealth.cs`

1. **`PlayerAttack.Update()`** — mỗi frame kiểm tra: Player còn sống (`health.IsDead`), không đang bận uống/nhặt (`movement.IsBusy`), đã hết cooldown (`throwCooldown`). Đọc `Input.GetMouseButtonDown(0)` (chuột trái = lửa) hoặc `(1)` (chuột phải = điện).
2. **`TryThrow(prefab, ammoType)`** — gọi `PlayerInventory.TryConsume(ammoType, 1)` để trừ 1 bình; hết bình thì `return`, không ném, không tốn cooldown. Tiêu thành công thì trừ điểm ngay (`GameManager.AddScore(-1)` cho lửa, `-2` cho điện — trừ dù trúng hay trượt), rồi gọi `Throw(prefab)`.
3. **`Throw(prefab)`** — tính hướng bay từ Player tới vị trí chuột, `Instantiate` prefab bình xoay đúng góc, gọi `Projectile.Launch(dir, speed)`, gọi `PlayerMovement.TriggerThrow(dir)` chạy animation ném, phát `throwSound`.
4. **`Projectile.Update()`** — vượt `maxRange` (18) thì tự `Destroy`.
5. **`Projectile.OnTriggerEnter2D(other)`** — bỏ qua Player/HitEffect/Projectile khác; tìm `EnemyHealth` trên vật chạm — sát thương theo % máu tối đa của quái đó (Lightning 50%, Fire 25%), gọi `enemy.TakeDamage(dmg)`. Trúng gì cũng phát hiệu ứng + âm thanh rồi `Destroy`.
6. **`EnemyHealth.TakeDamage(amount)`** — trừ máu, cập nhật thanh máu (`WorldHealthBar.SetFill()`), phát âm thanh, bắn event `OnDamaged`. Máu về 0 gọi `Die()`.
7. **`EnemyHealth.Die()`** — cộng điểm (`GameManager.AddScore(10)`), phát âm thanh chết, bắn event `OnDied` (báo `LockdownRoomController` bớt 1 con trong wave), hồi máu Player 3% (`healPlayerOnKillPercent`), rơi loot theo `dropChance`, tắt AI/collider, chạy `FadeOutAndDeactivate()`.

### 2. Player nhặt bình (F) rồi uống (E)

**File liên quan:** `PlayerInteractor.cs` → `PlayerMovement.cs` → `WorldItem.cs` → `PlayerInventory.cs`

1. **`PlayerInteractor.Update()`** — `Physics2D.OverlapCircleAll(transform.position, interactRadius)` tìm `WorldItem` gần nhất trong bán kính 0.6, gọi `SetHighlighted(true/false)` để đổi màu báo hiệu.
2. **`PlayerMovement.Update()`** — bấm **F** và `interactor.HasTarget == true` → chạy `PickUpRoutine()`.
3. **`PickUpRoutine()`** — khóa di chuyển, animation Trigger `"PickUp"`, đợi `pickUpDuration` (0.8s), gọi `interactor.Interact()` + âm thanh nhặt.
4. **`PlayerInteractor.Interact()`** — gọi `WorldItem.Collect(out quantity)` lấy `ItemData` + số lượng, gọi `PlayerInventory.Add(data, quantity)`.
5. **`WorldItem.Collect()`** — trả về data, phát âm thanh, `Destroy(gameObject)`.
6. **`PlayerInventory.Add(data, quantity)`** — `switch` theo `data.type`: cộng `firePotions`/`lightningPotions`/`healPotions`/`curePotions`, set `hasShieldItem = true`, hoặc thêm `keyIds`. Bắn event `OnChanged`.
7. Bấm **E**: `PlayerMovement.Update()` check `infection.IsInfected && infection.HasCure` → đúng thì `CureRoutine()` (uống cure, gọi `PlayerInfection.ConsumeAndCure()`); sai thì `DrinkRoutine()` bình thường.

### 3. Player tấn công Boss (MrX / Boss2)

Dùng lại đúng luồng ném bình ở mục 1, khác ở bước 5 của `Projectile.OnTriggerEnter2D()`:

- Ngoài `EnemyHealth`, còn check **`Boss2Health`** (Boss2, Level2) và **`BossHealth`** (MrX, Level1).
- Máu boss lớn hơn nhiều nên % thấp hơn: Lightning 10%, Fire 5% máu tối đa boss.
- **`Boss2Health.TakeDamage()`** — thêm kiểm tra mốc `visionLimitThreshold` (50%) để phát 1 lần âm thanh "sting" báo hiệu giai đoạn nguy hiểm.
- **`Boss2Health.Die()`** — cộng **+70 điểm**, rơi bình cure có điều kiện (`ShouldDropCure()` kiểm tra `LockdownRoomController.RemainingTime > 0`).
- **`BossHealth.Die()`** (MrX) — cộng **+50 điểm**, luôn rơi mũ khiên thủy tinh (`shieldItemPrefab`).

### 4. Boss tấn công Player

Hai kiểu sát thương tách biệt: cận chiến (chạm) và tầm xa (đạn bắn).

**4a. Cận chiến** — dùng chung `EnemyDamage.cs` cho mọi loại (Quai1, Bug214, Boss2):
1. **`OnCollisionEnter2D/Stay2D`** — check tag Player + hết `damageCooldown` (1s).
2. **`TryDamagePlayer(other)`** — gọi `playerHealth.TakeDamage(damagePerHit, transform.position)`.

**4b. Đuổi theo + tấn công (Boss2AI.cs):**
1. **`UpdateChaseState()`** — trong `Detect Range` (6) và có `HasLineOfSightToPlayer()` (raycast không bị tường chặn) → `isChasing = true`; xa hơn `Lose Range` (8) hoặc mất tầm nhìn → bỏ cuộc.
2. Đang đuổi → **`RepathTowardsPlayer()`** cập nhật hướng mỗi `chaseRepathInterval` (0.2s), gọi **`TryAttack()`** và **`TryShoot()`**.
3. **`TryAttack()`** — trong `Attack Range` (1.3) → chỉ bấm Trigger `"Attack"` đổi animation; sát thương thật do `EnemyDamage` lo.
4. **`TryShoot()`** — Player nằm giữa `Attack Range` và `Shoot Range` (4) → `Instantiate` `BossProjectile` xoay đúng hướng, `Launch(dir)`.

**4c. Đạn bay (BossProjectile.cs):**
1. Tự hủy sau `maxRange` (10) nếu không trúng gì.
2. **`OnTriggerEnter2D`** — bỏ qua đạn khác/Boss2/quái khác; trúng Player → `TakeDamage`; trúng bất kỳ thứ gì khác (kể cả tường) cũng dừng đạn tại đó.

**4d. MrX (TestBoss.cs)** — độc lập với Boss2AI, cùng ý tưởng detect/chase/attack, thêm **Hidden Skill**: máu ≤ 30% thì lao tới Player, hút 30% máu tối đa qua `TakeTrueDamage()` (bỏ qua i-frame), tự hồi đầy máu.

**4e. Nhận sát thương (`PlayerHealth.TakeDamage()`)** — điểm hội tụ mọi nguồn sát thương:
1. Khiên đang bật (`PlayerShield.IsShieldActive`) → miễn nhiễm tuyệt đối, `return` ngay.
2. I-frame (`invincibilityDuration`) → còn bất tử thì bỏ qua.
3. Trừ máu, âm thanh đau, knockback, nhấp nháy sprite.
4. Máu về 0 → `Die()` → `PlayerMovement.Die()` chạy animation chết.

### 5. Hiển thị điểm trên HUD

**File liên quan:** `GameManager.cs` → `SaveSystem.cs` → `LevelHUD.cs`

1. Mọi nơi cộng/trừ điểm gọi **`GameManager.Instance?.AddScore(amount)`**.
2. **`AddScore(amount)`** — `SavedData.score += amount`, kẹp tối thiểu 0.
3. **`LevelHUD.OnGUI()`** — đọc `GameManager.Instance.SavedData.score` mỗi frame, vẽ `"Điểm: {score}"` bằng `GUI.Label` (OnGUI, không dùng Canvas).
4. **Lưu trữ** — `GameManager.CompleteLevel()` lưu điểm vào file JSON khi qua level; `ResetProgress()` (chết hoặc chơi lại từ Intro) tạo `SaveData` mới → điểm về 0.
