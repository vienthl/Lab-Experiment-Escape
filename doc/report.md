# Course Project: Lab Experiment Escape

> Updated report — reflects the actual implemented project, not the original 3-level proposal. Structure follows the course rubric (Requirements 1–14 + Extra Functions) so each graded item can still be located easily. This revision expands every section with exact class names, field names, default tuning values, and file paths pulled directly from the current codebase.

## I. Members

- *Trương Huỳnh Long Viên* - DE180530 (Leader): Level Designer & Project Manager.
- *Huỳnh Văn Anh Huy* - DE180746: Player & Item Developer.
- *Phan Anh Vủ* - DE170544: Enemy & NPC Developer.
- *Trương Trung Hiếu* - DE180324: UI/UX & Systems Developer.

## II. Story

Lab Experiment Escape is a top-down 2D survival-escape game set in 2026. The player controls a lab researcher trapped inside **Facility-07**, a secret research site owned by the **Helix Corp** corporation. On **05.17.2026**, an experiment under **Project Elysium**, involving a substance called **Nexus Fluid**, suffers a catastrophic containment breach — spawning hostile "Echo Entities" and triggering a full facility lockdown. The in-game security-log narration (Intro cinematic) names the sole survivor **Dr. Alex Rivera**; the same name is reused as the protagonist in both endings for narrative continuity.

Exact narration quoted from the shipped `IntroCinematicController.cs` (panel 2, "Incident Report"):

```
> NEURAL LINK SYNCHRONIZATION FAILED.
> NEXUS FLUID CONTAINMENT BREACH DETECTED.
> ECHO ENTITIES ACTIVE. FACILITY LOCKDOWN ENGAGED.
```

and panel 3, "Survivor Record":

```
The experiment created living memory constructs called Echo Entities.
They now roam the sealed halls of Facility-07.

Alex is the sole survivor. Escape before the Nexus remembers you.
```

The playable story unfolds across **two** escalating containment zones (not three, see §VII for scope reduction rationale):

- **Level 1 (Containment Wing):** the player clears sequential enemy waves guarding the exit (`LockdownRoomController`), armed only with thrown chemical potions and scavenged supplies, before the locked door releases. Surviving this encounter exposes the player to trace Nexus Fluid — completing Level 1 unconditionally sets `SaveData.isInfected = true` in `GameManager.CompleteLevel()`, triggering a mild infection that persists into Level 2 (visualized as a sprite color tint).
- **Level 2 (Security Nexus):** a second containment room guarded by **Bug214** (a visually distinct Echo Entity reusing Quai1's AI) and a stronger, unique threat — **Boss2**. Boss2 dropping to 50% HP triggers a one-time *Among Us*-style vision-limiting effect (the screen goes dark except for a lit radius around the player, via `VisionLimiter.cs`). Defeating Boss2 before the room's countdown timer expires conditionally drops a **Cure Potion** (only if time remains — `Boss2Health.ShouldDropCure()`), which the player drinks with **E** to clear the infection (`PlayerInfection.ConsumeAndCure()`). Reaching the exit door (`Door_Extrance`) while cured triggers the **Happy Ending** (`HappyEndingController`) — Dr. Rivera escapes and reintegrates into normal life. If the countdown reaches zero first, `LockdownRoomController.sceneOnTimeExpired` forces an immediate, unconditional transition to the **Bad Ending** (`BadEndingController`) — special forces intervene and eliminate Dr. Rivera to prevent the infection from spreading beyond the facility.

Compared with the original 3-level pitch (a "Good/Bad Ending via accumulated Nexus Fluid flask count, read once at a Level 3 EndScreen" system culminating in a reactor-core boss fight), the shipped design condenses the experience to **2 levels** and ties the ending branch directly to two concrete, independently-testable gameplay triggers — a cured/timely escape vs. a hard countdown expiry — rather than an accumulated collectible counter evaluated at the very end.

## III. Game Design

**Characters actually implemented (4, not 5 as originally scoped):**

| Character | Script(s) | Role | Key defaults |
|---|---|---|---|
| Dr. Alex Rivera (Player) | `PlayerMovement`, `PlayerHealth`, `PlayerAttack`, `PlayerInventory`, `PlayerInteractor`, `PlayerInfection` | Player character | `maxHealth = 100`, `invincibilityDuration = 0.6s`, `knockbackForce = 7`, `moveSpeed = 3` |
| Quai1 | `Quai1AutoMove` + shared `EnemyHealth` + `EnemyDamage` | Basic chasing enemy (Level 1 & 2) | `maxHealth = 100`, `detectRange = 4`, `loseRange = 5.5`, `chaseSpeed = 2.5` |
| Bug214 | Same scripts as Quai1 (plain prefab duplicate, not a Prefab Variant) | Visual variant, Level 2 only | Identical defaults to Quai1, independently re-skinnable |
| Boss2 | `Boss2Health`, `Boss2AI`, `Boss2HealthUI` | Level 2 unique boss | `maxHealth = 400`, `visionLimitThreshold = 0.5 (50%)`, `detectRange = 6`, `chaseSpeed = 2.2`, `attackRange = 1.3`, `attackCooldown = 1.2s` |

**Scenes (6, not 5):** `Intro` → `MainMenu` → `Level1` → `Level2` → `HappyEnding` / `BadEnding`. The originally-planned single "EndScreen" was split into two dedicated, fully-cinematic ending scenes matching the Intro's visual style and code architecture (see §VI-a).

### Player detail

Moves with WASD (legacy Input Manager, `Input.GetAxisRaw("Horizontal"/"Vertical")`), throws Fire/Lightning potions with the mouse buttons (ammo-limited, consumed from `PlayerInventory` via `TryConsume(ItemType, 1)`), drinks a Heal Potion or the special Cure Potion with **E** (context-sensitive — while infected and carrying a Cure Potion, E always prioritizes curing over the generic Drink animation), picks up the nearest item in range with **F** (gated by `PlayerInteractor.HasTarget`), and pauses with **ESC**. Movement is animated through a single 2D Simple Directional Blend Tree (`MoveX`/`MoveY` floats); action states (Drink/PickUp/Throw/Die) are separate Animator Triggers, each backed by a coroutine in `PlayerMovement.cs` that locks movement input for the action's duration (`drinkDuration = 0.8s`, `pickUpDuration = 0.8s`, `throwDuration = 0.5s`) and always releases the lock in a `finally` block so a stopped/interrupted coroutine can never leave input permanently frozen.

### Enemy detail

`Quai1AutoMove.cs` implements wander-then-chase behavior: it picks a random unblocked direction every `changeDirectionInterval` (2s default) while wandering, switches to chasing when the player enters `detectRange` **and** a raycast confirms line-of-sight against the Wall layer, and gives up when the player exceeds `loseRange` or line-of-sight is lost. Wall/obstacle detection uses `Physics2D.RaycastAll` cast from just outside the enemy's own collider, so the enemy never blocks on its own body. Contact damage is delegated to a separate `EnemyDamage.cs` component (`damagePerHit = 10`, `damageCooldown = 1s`) rather than being handled inside the movement script, keeping "how it moves" and "how it hurts you" independently tunable.

### Boss detail

`Boss2AI.cs` is a simplified variant of the same idea (no wander state — the boss stands its ground until the player is detected), with an added periodic **Attack** trigger fired whenever the player stays within `attackRange` past `attackCooldown` (purely cosmetic — the real damage still comes from `EnemyDamage`, matching Quai1's pattern exactly). `Boss2Health.cs` layers three boss-specific behaviors on top of the same HP/death shape as `EnemyHealth`: a one-time vision-limiting trigger at `visionLimitThreshold` (`VisionLimiter.Instance.Activate()`), a conditional Cure Potion drop gated on the room's remaining lockdown time, and Attack/Die Animator triggers that are safely no-ops if the Animator Controller has no matching state (Unity simply leaves an unconsumed trigger armed — no error).

## IV. Sprites

**a. Player – Dr. Alex Rivera:** 4-directional top-down sprite (2D Simple Directional Blend Tree driven by `MoveX`/`MoveY` Animator floats), with dedicated `Drink`, `PickUp`, `Throw`, and `Die` Trigger parameters, plus `LastMoveX`/`LastMoveY` floats used to keep the character facing the last movement direction during actions that zero out `MoveX`/`MoveY`. A single sprite set covers both the healthy and infected states — infection is expressed as a runtime color tint (`SpriteRenderer.color`, applied via `PlayerHealth.SetBaseTint(Color)`) rather than a second sprite sheet, so no separate "infected" art asset was required. This also means the infection tint composes correctly with the existing hit-flash coroutine, which always restores the sprite to whatever `baseColor` currently is.

**b. Enemies – Quai1 / Bug214 / Boss2:** Quai1 and Bug214 share an identical rig (4-directional Blend Tree, `Quai1AutoMove.cs`, `MoveX`/`MoveY` floats only) with different sprite sheets for visual variety; Bug214 was produced as a **plain Ctrl+D duplicate** of the Quai1 prefab (explicitly not a Prefab Variant), so it has zero inheritance link back to Quai1 and can be re-skinned or re-tuned fully independently. Boss2 has its own dedicated Animator Controller (`Boss2Move.controller`) with 4-directional movement clips (`Boss2_back/left/normal/right`) plus `Attack`/`Die` Trigger parameters; a dedicated Attack/Die animation *clip* pair is a nice-to-have, not required — the boss functions correctly using only the Move blend tree, since damage delivery and death sequencing do not depend on animation state at all.

**c. Background & Tileset:** a blue-toned sci-fi laboratory tileset (`Map1`) arranged with Unity's Tilemap system, reused across Level 1 and Level 2 with different prop layouts and enemy/item placements.

**d. Items & Props:** a shared potion sprite sheet (`potion2.png`, indices `potion2_9`/`_11`/`_12` etc.) supplies the Fire (red), Lightning (orange), Heal (blue), and Cure (green — specifically frame `potion2_12`, 94×161) potion icons. All pickups are represented in-world via the `WorldItem.cs` prefab pattern: a bobbing sine-wave animation (`bobHeight = 0.08`, `bobSpeed = 2.5`), a highlight color swap when the player is the nearest target (`highlightColor`), and an `ItemData` ScriptableObject reference (`id`, `displayName`, `icon`, `type`, `amount`) resolved through `Assets/Resources/Items/*.asset`.

**e. Ending Cinematics:** 6 AI-generated (Gemini) backdrop illustrations — 3 per ending scene (Status Report / Epilogue / Title Reveal panels), 16:9, matching the Intro's CCTV-security-log aesthetic (cool teal/cyan grading for Happy, harsh crimson/black for Bad) — in the process of being imported (Texture Type: Sprite, Filter Mode: Point, Compression: None) and wired into `HappyEndingController`/`BadEndingController`'s `Status/Epilogue/Title Backdrop` fields.

## V. Game development: Requirements

### a. Requirement 1 – Keyboard Input

Dr. Alex Rivera is controlled entirely via keyboard + mouse, using Unity's **legacy Input Manager** (`Input.GetAxisRaw` / `Input.GetKeyDown` / `Input.GetMouseButtonDown`) rather than the new Input System package. WASD/arrow keys drive 4-directional movement; Left/Right mouse buttons throw Fire/Lightning potions; **E** drinks (context-sensitive drink/cure); **F** picks up the nearest reachable item; **ESC** pauses. No dash/dodge action exists in the current build.

Representative excerpt from `PlayerMovement.cs` (movement input → Animator, exactly as shipped):

```csharp
movement.x = Input.GetAxisRaw("Horizontal");
movement.y = Input.GetAxisRaw("Vertical");
movement = movement.normalized;

if (movement != Vector2.zero)
    lastMoveDirection = movement;

animator.SetFloat("MoveX", movement.x);
animator.SetFloat("MoveY", movement.y);
animator.SetFloat("Speed", movement.sqrMagnitude);
```

The context-sensitive **E** key (merged from an earlier separate **C** binding for the Cure Potion) reads:

```csharp
if (Input.GetKeyDown(KeyCode.E))
{
    // Đang nhiễm độc + có bình cure → ưu tiên uống cure để chữa; không thì uống bình hồi máu như bình thường.
    if (infection != null && infection.IsInfected && infection.HasCure)
        StartCoroutine(CureRoutine());
    else
        StartCoroutine(DrinkRoutine());
}
```

### b. Requirement 2 – Game Over

On `PlayerHealth.IsDead`, `GameOverUI.cs` waits `showDelay = 0.8s` then draws a full-screen OnGUI overlay ("GAME OVER" + "Nhấn R để về Menu"); no score or elapsed-time readout is shown, as no scoring system exists in this project. Pressing **R** calls `GameManager.NotifyPlayerDied()`, which:

1. Sets `CurrentContext = GameContext.Dead`.
2. Resets `Time.timeScale` to 1.
3. Calls `ResetProgress()` — wipes `SavedData` back to `new SaveSystem.SaveData()` and re-saves it to disk, so health/potions/infection all return to defaults.
4. Loads `MainMenu`, where `MainMenuController.ApplyPlayButtonContext()` relabels the PLAY button "RESTART" based on `GameManager.CurrentContext`.

### c. Requirement 3 – Animations and Collisions

All characters (Player, Quai1, Bug214, Boss2) use 2D Simple Directional Blend Trees driven by `MoveX`/`MoveY` floats for 4-directional movement, plus dedicated Trigger parameters for context actions. Physical collisions use `Rigidbody2D` (`gravityScale = 0`, `collisionDetectionMode = Continuous`, `interpolation = Interpolate`) against Tilemap-based wall colliders on a dedicated **Wall** layer. Trigger colliders serve two purposes:

- **Item pickup detection** — not a physics trigger callback at all; `PlayerInteractor.cs` polls `Physics2D.OverlapCircleAll(transform.position, interactRadius = 0.6)` every frame and tracks the nearest `WorldItem`.
- **Zone/door transitions** — `ExitZone.cs` (`OnTriggerEnter2D`) and `LockdownRoomController.cs`'s own optional entry trigger.

A real, fixed bug is documented here as it directly demonstrates this requirement's collision/trigger distinction: each door's physical blocking collider (`DoorController.blockCollider`, non-trigger, enabled/disabled by `OpenDoor()`/`CloseDoor()`) **must** be a separate GameObject/collider from any `ExitZone`'s detection trigger. A single shared collider cannot simultaneously (a) physically block the player while the door is closed and (b) remain active as an always-on trigger once the door opens and disables that same collider. This exact conflict was found and fixed on `Door_Extrance` during Level 2 testing by moving `ExitZone` onto a dedicated child `ExitTrigger` GameObject with its own always-enabled `Is Trigger` collider, referencing the door only through `ExitZone.watchedDoor`.

### d. Requirement 4 – Score / Points System

**Not implemented.** No `ScoreManager.cs`, point values, or score HUD exist in the current project; the HUD (`LevelHUD.cs`) instead surfaces potion counts, infection status, and the active lockdown wave/timer.

### e. Requirement 5 – Sound

Rather than a single persistent `SoundManager` singleton with per-scene `AudioClip[]` arrays, sound is implemented as a lightweight, decentralized system built around two small helpers:

- **`Assets/Scripts/Audio/AudioOneShot.cs`** — a static helper, `AudioOneShot.Play(AudioClip clip, Vector3 position, float volume = 1)`, that fires one-shot SFX via `AudioSource.PlayClipAtPoint`. Because this spawns its own temporary GameObject internally, it is safe to call from an object that destroys or deactivates itself immediately after (a dying enemy, a collected item) — the sound finishes playing independently of the caller's lifetime.
- **`Assets/Scripts/Audio/LevelMusic.cs`** — a small looping-BGM component (`musicClip`, `volume`) dropped into each gameplay scene.

Every gameplay script that needed audio now exposes its own `AudioClip` field(s) and calls `AudioOneShot.Play(...)` at the relevant moment:

| Script | Event | Field(s) |
|---|---|---|
| `PlayerMovement` | Drink / Cure animation start | `drinkSound` |
| `PlayerMovement` | Item picked up mid-animation | `pickUpSound` |
| `PlayerAttack` | Potion thrown | `throwSound` |
| `Projectile` | Impact on enemy/wall | `impactSound` |
| `PlayerHealth` | Hurt / Death | `hurtSound`, `deathSound` |
| `EnemyHealth` | Hit / Death | `hitSound`, `deathSound` |
| `Boss2Health` | Hit / Death / 50% HP vision-trigger | `hitSound`, `deathSound`, `visionTriggerSound` |
| `Boss2AI` | Attack | `attackSound` |
| `DoorController` | Open / Close (suppressed on the scene's initial forced-close) | `openSound`, `closeSound` |
| `WorldItem` | Collected | `collectSound` |
| `LockdownRoomController` | Wave start / All clear / Time expired | `waveStartSound`, `allClearSound`, `timeExpiredSound` |
| `GameOverUI` | Player just died | `gameOverSound` |
| `IntroCinematicController` / `HappyEndingController` / `BadEndingController` | Per-character typewriter blip, panel transition | `typewriterBeep`, `panelTransitionSound` |

MainMenu, Intro, and both Ending scenes each carry their own dedicated looping-music `AudioSource` (added at runtime via `gameObject.AddComponent<AudioSource>()`). All `AudioClip` fields above currently exist in code but are **unassigned** — sourcing and wiring the actual clips (via freesound.org, filtered to the CC0 license) is an in-progress task, with a keyword shortlist already prepared per SFX category (throw → "whoosh throw", fire impact → "fire explosion small", lightning impact → "electric zap", door → "sci-fi door open hiss", etc.).

### f. Requirement 7 – Multiple Scenes

The game contains **6** distinct scenes: `Intro`, `MainMenu`, `Level1`, `Level2`, `HappyEnding`, `BadEnding`. Scene transitions go through `GameManager` (`CompleteLevel(nextScene)` → `FadeAndLoad()` coroutine, fading to black over `fadeDuration = 0.5s`, loading the scene, then fading back in; or `LoadLevel(sceneName)` for an instant cut) or direct `SceneManager.LoadScene()` calls from the Intro/Ending controllers. Each gameplay scene has its own Tilemap layout and enemy/boss configuration; Level 2 additionally carries `InfectionNoticeUI` and `VisionLimiter`, neither of which exist in Level 1.

### g. Requirement 8 – Difficulty Changes Over Time

Difficulty is expressed through `LockdownRoomController.cs`'s sequential wave system rather than a dedicated multiplier manager. On `StartLockdown()`: all configured `doorsToLock` are locked, `waves[0]` is activated (`SetActive(true)` on its pre-placed `enemies`/`bosses`), and a countdown begins (`remainingTime = timeLimit`). Each `EnemyHealth`/`Boss2Health` in the active wave subscribes to its own `OnDied` event; once every member of the current wave has died, `ActivateNextWave()` advances to the next wave **without resetting the timer**, keeping time pressure cumulative across the whole room rather than per-wave. Level 2 escalates further via Boss2's HP-triggered vision-limiting ability, active only in the second half of the fight. No separate `DifficultyManager.cs` enemy-speed/spawn-rate multiplier system exists; per-scene difficulty is instead authored directly through each `LockdownRoomController` / enemy prefab's own Inspector values (e.g. Quai1's `chaseSpeed = 2.5` vs Boss2's `chaseSpeed = 2.2` but far higher HP and an extra Attack behavior).

### h. Requirement 9 – Promotion Screen

`IntroCinematicController.cs` plays a fully code-generated, 4-panel CCTV/security-log-styled cinematic (Logo/Emblem reveal with a glitch-jitter effect on 3 stacked ghost-colored copies of the wordmark, Incident Report, Survivor Record, Title Reveal) before the Main Menu. Each panel fades via a `CanvasGroup` alpha tween (`SwitchPanel()` coroutine, ease via `Mathf.SmoothStep`); the Incident/Survivor panels type their body text out character-by-character (`TypeText()` coroutine, `typeCharsPerSecond = 45`, extra pause on `\n` and `.`). 18 animated scanline bars scroll continuously; a red "REC" dot blinks at 2 Hz on any panel with a backdrop photo. **ENTER** advances panels (or instantly completes the current typewriter line if one is mid-type); **ESC** skips straight to MainMenu at any point after `skipDelay = 0.5s`.

As of the latest revision, entering the Intro also calls `GameManager.Instance.ResetProgress()` (or `SaveSystem.ResetSave()` directly if no `GameManager` yet exists), guaranteeing that replaying the Intro always starts a genuinely fresh game rather than silently inheriting a previous session's saved potions/health/infection state.

### i. Requirement 10 – Player Upgrade During Scene

The originally-planned permanent stat upgrades (HP Injector / Speed Booster / Energy Shield) were **not implemented**. In their place, a lighter-weight sustain mechanic exists: `EnemyHealth.cs` exposes a `[Range(0,1)] healPlayerOnKillPercent` field (default **0.03 = 3%**) that heals the player for that percentage of their max HP whenever a Quai1 or Bug214 dies (`playerHealth.Heal(playerHealth.MaxHealth * healPlayerOnKillPercent)`), configurable per-enemy prefab in the Inspector, and intentionally excluded from `Boss2Health` (defeating Boss2 does not heal the player — the design keeps the boss fight itself the "hard" resource-drain moment).

### j. Requirement 11 – Player Appearance Change During Scene

Implemented as a binary infection state rather than a multi-stage sprite-swap: `PlayerInfection.cs` tracks `IsInfected` (persisted across the Level1→Level2 transition via `SaveSystem.SaveData.isInfected`), and applies a configurable `infectedTint` Color (Inspector-adjustable, default a light red `(1, 0.55, 0.55)`) to the player's `SpriteRenderer` through `PlayerHealth.SetBaseTint(Color)` — deliberately routed through the *same* `baseColor` field the hit-flash coroutine already uses, so infection tinting and the existing invincibility-flash effect compose correctly instead of fighting over `spriteRenderer.color`. The player becomes infected automatically the moment Level 1 is completed (`GameManager.CompleteLevel()` checks `SceneManager.GetActiveScene().name == "Level1"`), and is cured by drinking a Cure Potion (**E**, `PlayerInfection.ConsumeAndCure()`) after defeating Boss2.

### k. Requirement 12 – New Game Objects Spawned During Game

- `PlayerAttack.Throw()` instantiates a Fire/Lightning potion `Projectile` prefab at a small offset from the player, oriented toward the mouse cursor.
- `LockdownRoomController.ActivateNextWave()` **activates** (rather than instantiates) pre-placed, initially-hidden enemy/boss GameObjects wave by wave — chosen over `Instantiate()` so designers can hand-place and hand-tune each wave's exact enemies directly in the scene.
- `EnemyHealth.Die()` conditionally `Instantiate()`s a `WorldItem` loot prefab at the death position (`Random.value <= dropChance`, default 0.3).
- `Boss2Health.Die()` conditionally `Instantiate()`s the Cure Potion `WorldItem`, gated on `ShouldDropCure()` checking `LockdownRoomController.RemainingTime > 0` (or no time limit configured at all).

### l. Requirement 13 – Child Game Objects Spawned During Game

Loot drops (regular enemy loot and Boss2's Cure Potion) are instantiated as **independent scene-root objects** at the enemy's death position rather than parented under the dying enemy — specifically so they are unaffected by the parent's `FadeOutAndDeactivate()` coroutine (which fades every child `SpriteRenderer` to transparent over `deathFadeDuration = 0.25s` before calling `gameObject.SetActive(false)` on the enemy) that runs immediately after `Die()`.

### m. Requirement 14 – Save System

`Assets/Scripts/Core/SaveSystem.cs` persists progress to a **JSON file** (`Application.persistentDataPath/save.json`, via `JsonUtility.ToJson`/`FromJson`) rather than `PlayerPrefs`. Full field list:

| Field | Type | Default | Purpose |
|---|---|---|---|
| `firePotions` | int | 0 | Fire potion ammo |
| `lightningPotions` | int | 0 | Lightning potion ammo |
| `healPotions` | int | 0 | Heal potion count |
| `curePotions` | int | 0 | Cure potion count |
| `isInfected` | bool | false | Infection status carried Level1→Level2 |
| `currentHealth` | float | **-1** (sentinel) | Player HP at last level completion; -1 = never saved yet → `PlayerHealth` falls back to `maxHealth` |
| `keyIds` | `List<string>` | empty | Collected keycard IDs |
| `levelReached` | string | `"Level1"` | Last completed scene name |

`GameManager` loads the save once at startup (`Awake()` → `SaveSystem.Load()`) and writes it back out on every successful level completion (`CompleteLevel()` — also snapshotting `PlayerHealth.CurrentHealth`), on the player's death (`NotifyPlayerDied()` → `ResetProgress()`, wiping everything back to defaults rather than merely reloading the last checkpoint), and whenever the Intro scene plays (also a full reset, guaranteeing new games start clean even if an old `save.json` exists from a previous session). There is no level-index/checkpoint-zone granularity — persistence is scoped to whole-level completions only. GitHub repository tracks version history per requirement #15.

## VI. Game development: extra functions

### a. Extra Function 1 – Dual Ending System (Happy / Bad Ending)

Rather than an accumulated "Nexus Fluid flask count" threshold read at a single EndScreen, the ending branch is driven directly by two concrete, independent gameplay triggers:

- **`ExitZone.cs`'s `requireCured` flag** — set on Level 2's exit door (`Door_Extrance`). `OnTriggerEnter2D` checks the player's tag, the watched door's open state, and (if `requireCured`) `PlayerInfection.IsInfected`; only a cured player passing through an open door calls `GameManager.CompleteLevel("HappyEnding")`.
- **`LockdownRoomController.cs`'s `sceneOnTimeExpired` field** — checked directly inside the countdown's `Update()` the instant `remainingTime` reaches 0, calling `GameManager.Instance?.LoadLevel("BadEnding")` unconditionally, regardless of the player's position, HP, or infection state. (Deliberately implemented as a direct code call rather than a `UnityEvent` Inspector reference to `GameManager`, since `GameManager` is a cross-scene `DontDestroyOnLoad` singleton with no fixed scene-local instance a `UnityEvent` could safely reference.)

Both ending scenes reuse the Intro's exact code-generated CCTV-log visual architecture — same `CreateGroup`/`CreatePanel`/`CreateText`/`Stretch` UI-builder helpers, same scanline/REC-indicator/typewriter/fade machinery — with 3 panels each (Status Report → Epilogue → Title Reveal, vs. the Intro's 4) and their own distinct color palettes: a calm mint/teal (`(0.25, 0.9, 0.6)`) for `HappyEndingController`, a harsh crimson (`(0.95, 0.15, 0.18)`) for `BadEndingController`. Sample titles: Happy Ending's title panel reads **"ESCAPE COMPLETE"** / *"THE ECHO FADES. ALEX REMAINS."*; Bad Ending's reads **"CONTAINMENT COMPLETE"** / *"THE ECHO CLAIMS ANOTHER."*

### b. Extra Function 2 – Vision-Limiting Boss Ability ("Among Us" effect)

`Assets/Scripts/UI/VisionLimiter.cs` procedurally generates a 512×512 radial gradient mask texture at runtime (`Texture2D.SetPixel` per-pixel loop, alpha = `Mathf.InverseLerp(innerFraction * radius, radius, distanceFromCenter)`) and draws it via `OnGUI`, sized and positioned each frame so the fully-transparent center always lands on `Camera.main.WorldToScreenPoint(player.position)` and always covers the whole screen regardless of resolution (`coverSize` derived from `visionRadius / innerFraction`, clamped against `Screen` dimensions). It is rendered at `GUI.depth = 1000` specifically so other HUD elements (drawn at the default depth 0) remain visible **on top of** the darkness rather than being obscured by it. `Boss2Health.TakeDamage()` calls `VisionLimiter.Instance.Activate()` exactly once, the first time `HealthPercent <= visionLimitThreshold (0.5)`, darkening the whole screen except a `visionRadius = 160px` lit circle around the player for the remainder of the encounter.

### c. Extra Function 3 – Infection / Cure Narrative Loop

A cross-cutting mechanic tying together `PlayerInfection.cs`, the new `ItemType.CurePotion` enum value, Boss2's conditional loot drop, and the Dual Ending System above: completing Level 1 infects the player; Level 2's `InfectionNoticeUI.cs` surfaces a one-time Bottom-Center notice on scene entry (`"Bạn đã bị nhiễm độc nhẹ sau trận chiến ở Level 1..."`, auto-dismissing after `autoHideAfter = 6s` or any key press past a `minDisplayTime = 1.5s` grace window); `LevelHUD.cs` shows a persistent **"NHIỄM ĐỘC (bấm C để chữa)"**-style warning line while infected (plus the live Cure Potion count); and the only way to clear the infection is to defeat Boss2 within the lockdown's time limit, retrieve the resulting Cure Potion, and drink it.

*(Note: the originally-planned Audio Log / Data Pad lore-collectible system, and its "collect all 9 → secret consent-form scene" twist, were not implemented in this version.)*

## VII. Development Progress

Scoped against the game's own (reduced, 2-level) design rather than the original 3-level pitch, the following systems are complete and playable end to end:

1. **Player core** — movement/animation, potion throwing with ammo, i-frame + knockback + hit-flash, contextual E-key drink/cure, health persistence across levels.
2. **Item & Inventory system** — `ItemData`/`WorldItem`/`PlayerInventory`/`PlayerInteractor`, 6 item types (`FirePotion`, `LightningPotion`, `HealPotion`, `KeyCard`, `LoreNote`, `CurePotion`).
3. **Level 1** — wave-based lockdown room (`LockdownRoomController`) using Quai1, door lock/unlock, timed challenge.
4. **Level 2** — Bug214 + Boss2 encounter, HP-triggered vision-limiting effect, conditional Cure Potion drop, infection/cure loop, dual-ending branching.
5. **Save / Restart / New-Game reset** — JSON persistence across levels; death and Intro replay both correctly wipe progress back to defaults.
6. **Front-end flow** — code-generated MainMenu (settings, resolution, context-aware PLAY/RESUME/RESTART label), additive-scene Pause (`Time.timeScale = 0` + MainMenu loaded `LoadSceneMode.Additive` on top of frozen gameplay), Intro cinematic.
7. **Dual Ending cinematics** — Happy/Bad ending scenes matching the Intro's visual style, wired to the gameplay triggers above.
8. **Audio hook infrastructure** — every gameplay event that should make a sound has a call site and an Inspector field ready to receive a clip (27 call sites across 14 scripts).

### Known issues found & fixed this phase

- **Git merge disaster & recovery.** Merging a teammate's branch (`origin/anhhuy1`) introduced duplicate `PlayerAttack` classes (a full parallel reimplementation of the player/menu/item systems under `Assets/MainCharacter/Scripts/`) and silently deleted ~21 of this branch's own script files via tracked renames. Recovered via `git reset --hard` to the last known-good commit + `git push --force-with-lease`, after which the teammate's Mr.X boss content was selectively re-merged with a targeted `git checkout <commit> -- <path>` (not a full branch merge), avoiding the same class-collision problem a second time.
- **Animator "frozen" bug (no reaction to player proximity).** Root cause: the Animator component's default **Culling Mode** was `Cull Update Transforms` instead of `Always Animate`, so Unity stopped updating the state machine when it (incorrectly) judged the object off-screen. Fixed per-prefab.
- **Corrupted Animator Controller graph.** A blend tree that silently refused to create new states, accompanied by `UnityEditor.Graphs.AnimationStateMachine.Graph.GenerateConnectionKey` `NullReferenceException` spam in the Console — a known Unity Editor bug indicating corrupted internal graph data. Fixed by deleting and recreating the `.controller` asset from scratch.
- **`ExitZone`/door-collider conflict** (detailed in §V-c) — Level 2's exit door silently failed to trigger the ending transition because its `ExitZone` shared a collider with the door's own non-trigger physical blocker. Fixed by giving `ExitZone` a dedicated child trigger collider.
- **Debug `GameManager` duplicate-instance edge case.** A temporary `GameManager` GameObject left in a gameplay scene for standalone testing (with a `forceInfectedOnLoad` debug toggle) silently self-destructs in `Awake()` whenever a *real*, already-`DontDestroyOnLoad`'d `GameManager` from an earlier scene already exists — meaning the debug toggle has zero effect once the full MainMenu→Level1→Level2 flow is used instead of loading Level2 standalone. Documented as a testing-workflow gotcha rather than a product bug.
- **Content typo:** the Intro's Title Reveal panel currently renders "LAB" + "EXPERIENCE ESCAPE" (a leftover naming drift from the project's actual title, *Lab Experiment Escape* — the top banner text earlier in the same scene correctly reads "LAB EXPERIMENT ESCAPE"). Flagged for a one-line text fix, not yet corrected.

## VIII. Future Works

1. **Audio pass** — source and assign all `AudioClip` fields (SFX + music) from freesound.org (CC0-filtered), using the keyword shortlist already prepared per category (combat, enemy/boss, doors, lockdown, UI, cinematic).
2. **Ending art integration** — finish importing the 6 Gemini-generated backdrop illustrations and assign them to `HappyEndingController` / `BadEndingController`'s `Status/Epilogue/Title Backdrop` fields; tune each panel's scrim darkness for text readability.
3. **Exit-trigger regression pass** — verify every `ExitZone` in the project (Level 1's `Door_Exit` included) uses a dedicated trigger collider separate from its door's physical blocker, following the fix applied to `Door_Extrance`.
4. **Full playthrough QA** — Level1 → Level2 → Happy/Bad Ending branching under both win conditions (cured-in-time vs. timer-expired), plus regression-testing the death/restart and Intro-replay progress-reset behavior.
5. **Content fix** — correct the Intro Title Reveal panel's "EXPERIENCE ESCAPE" text back to "EXPERIMENT ESCAPE" for consistency with the project title.
6. **Optional polish** — author dedicated Attack/Die animation clips for Boss2 and wire them to the existing (currently unused) Attack/Die Animator triggers; the boss is fully functional without them.
7. **Build, export, demo recording & GitHub submission** — Windows build, gameplay demo recording, GitHub branch cleanup/tag/README covering the final feature set as documented in this report.

*(Per-member day-by-day scheduling is intentionally left to the team to fill in — this report only reflects the technical state of the project, not individual task ownership going forward.)*

## Appendix

### 1. Repository & asset sources

- Github: https://github.com/vienthl/Lab-Experiment-Escape.git
- Asset sources: team-produced/sourced pixel art for characters, enemies, and tileset; AI-generated (Gemini) backdrop illustrations for the Intro/Ending cinematics; SFX/BGM to be sourced from freesound.org (CC0).

### 2. Script inventory (this development phase)

| Path | Purpose |
|---|---|
| `Assets/Scripts/PlayerMovement.cs` | Movement, Animator params, Drink/PickUp/Throw/Cure coroutines, all keyboard input |
| `Assets/Scripts/PlayerHealth.cs` | HP, i-frames, knockback, hit-flash, true-damage bypass, infection tint hook |
| `Assets/Scripts/PlayerAttack.cs` | Potion throwing, ammo consumption |
| `Assets/Scripts/PlayerInfection.cs` | Infection state, cure consumption, tint application |
| `Assets/Scripts/PlayerHealthUI.cs` | OnGUI top-left player health bar |
| `Assets/Scripts/GameOverUI.cs` | Death overlay + restart |
| `Assets/Scripts/DoorController.cs` | Door open/close animator triggers + physical block collider |
| `Assets/Scripts/Projectile.cs` | Thrown potion collision/damage/impact |
| `Assets/Scripts/MainMenuController.cs` | Code-generated Main Menu UI (Canvas/uGUI) |
| `Assets/Scripts/IntroCinematicController.cs` | Intro cinematic |
| `Assets/Scripts/HappyEndingController.cs` | Happy Ending cinematic |
| `Assets/Scripts/BadEndingController.cs` | Bad Ending cinematic |
| `Assets/Scripts/Core/GameManager.cs` | Singleton, pause flow, save orchestration, scene fade transitions, progress reset |
| `Assets/Scripts/Core/SaveSystem.cs` | JSON persistence |
| `Assets/Scripts/Items/ItemData.cs` | ScriptableObject item definition + `ItemType` enum |
| `Assets/Scripts/Items/WorldItem.cs` | In-world pickup behavior |
| `Assets/Scripts/Items/PlayerInventory.cs` | Potion/key counts, `Add`/`TryConsume` |
| `Assets/Scripts/Items/PlayerInteractor.cs` | Nearest-item detection + `Interact()` |
| `Assets/Scripts/Level/ExitZone.cs` | Level-exit trigger + ending routing (`requireCured`) |
| `Assets/Scripts/Level/LockdownRoomController.cs` | Wave/timer gating, `sceneOnTimeExpired` |
| `Assets/Scripts/UI/LevelHUD.cs` | Potion/infection/wave HUD |
| `Assets/Scripts/UI/VisionLimiter.cs` | Among-Us-style darkness effect |
| `Assets/Scripts/UI/InfectionNoticeUI.cs` | Bottom-center infection notice |
| `Assets/Scripts/Audio/AudioOneShot.cs` | Static one-shot SFX helper |
| `Assets/Scripts/Audio/LevelMusic.cs` | Looping background music component |
| `Assets/Enemy/Scripts/EnemyHealth.cs` | Shared enemy HP/death/loot/heal-on-kill |
| `Assets/Enemy/Scripts/EnemyDamage.cs` | Contact damage to player |
| `Assets/Enemy/Scripts/WorldHealthBar.cs` | World-space enemy health bar |
| `Assets/Enemy/Quai1/Scripts/Quai1AutoMove.cs` | Wander/chase AI shared by Quai1 & Bug214 |
| `Assets/Enemy/BossLevel2/Scripts/Boss2Health.cs` | Boss HP/death, vision-trigger, conditional loot |
| `Assets/Enemy/BossLevel2/Scripts/Boss2AI.cs` | Boss idle/chase/attack behavior |
| `Assets/Enemy/BossLevel2/Scripts/Boss2HealthUI.cs` | OnGUI top-center boss health bar |

*(`MenuManager.cs`, `BackToMainMenu.cs`, `UIClickSound.cs`, and `HitEffect.cs` also exist in the project but predate this development phase and were not modified — omitted from the detailed breakdown above pending a separate audit.)*

## Contribution

- *Trương Huỳnh Long Viên* - DE180530 (Leader – Level Designer / PM): 25%
- *Huỳnh Văn Anh Huy* - DE180746 (Player & Item Developer): 25%
- *Phan Anh Vủ* - DE170544 (Enemy & NPC Developer): 25%
- *Trương Trung Hiếu* - DE180324 (UI/UX & Systems Developer): 25%

*(Percentages carried over from the original report as a placeholder — adjust to reflect actual contribution for this development phase.)*
