# Saltwake — CLAUDE.md

## Project Overview

Saltwake is a 2D Metroidvania-style platformer built in Unity. The game features room-based level design with gated progression, melee and ranged combat, infinite jumping, dashing, and flying/ground enemy types. The visual theme is snowy mountains and frozen lakes under a night sky.

**Note:** The player has infinite jumps, which is the primary traversal mechanic. Wall jumping still exists in the code but is not important for level design — levels should be designed around infinite jumps, not wall jumps.

## Tech Stack

- **Engine:** Unity (2D, URP)
- **Language:** C# (.NET)
- **UI:** TextMeshPro, Unity UI Canvas
- **Physics:** Rigidbody2D with continuous collision detection
- **Level Design:** Tilemap-based with CameraRoom triggers for room boundaries

## Project Structure

```
Assets/
├── MetroidvaniaController/       # Core game framework
│   ├── Scripts/
│   │   ├── Player/               # CharacterController2D, PlayerMovement, Attack, CameraFollow
│   │   ├── Enemies/              # Enemy.cs (patrol), Ally.cs (companion NPC)
│   │   └── Environment/          # DestructibleObject, Grass, CameraRoom, KillZone
│   ├── Prefabs/                  # Player, enemy, weapon, particle prefabs
│   └── Scenes/                   # Demo scene
├── Level1/                       # Level 1 scene, scripts, and assets
│   ├── Level1.unity
│   ├── Scripts/                  # All Level 1 scripts:
│   │   ├── CrowEnemy.cs          # Flying enemy AI
│   │   ├── CrowBoss.cs           # 3-phase boss AI
│   │   ├── RoomManager.cs        # Enemy tracking, gate control, rewards
│   │   ├── RoomGate.cs           # Barrier with open/close + fade
│   │   ├── Pickup.cs             # Collectible pickups (heart, buffs)
│   │   ├── BuffManager.cs        # Temporary damage/speed buffs
│   │   ├── DialogueBox.cs        # Styled typewriter text box UI
│   │   └── BossHealthBar.cs      # Boss HP bar UI
│   └── Enemies/                  # Crow.prefab, CrowBoss.prefab
├── Level2/                       # Level 2 scene and assets
│   └── Level2.unity
├── HealthHeartSystem/            # Heart-based health UI
│   └── Scripts/                  # PlayerStats.cs (singleton), HealthBarController.cs
└── UI/                           # UI prefabs and scripts
```

## Key Systems and Patterns

### Player
- `CharacterController2D.cs` — core physics, health (10 HP), invincibility frames (1s), knockback, death/respawn
- `PlayerMovement.cs` — input handler (WASD/arrows, Space, Shift)
- `Attack.cs` — melee (K key, 4 damage, 0.9 range) and throwable (V key, 2 damage)
- `PlayerStats.cs` — singleton health manager with Heal(), TakeDamage(), AddHealth()

### Enemies
- `Enemy.cs` — ground patrol, 10 HP, auto-flip at walls/edges, 2 contact damage, 20% heart drop on death
- `CrowEnemy.cs` — flying enemy, 8 HP, vision-based pursuit (8 unit range), hover + steering AI, 20% heart drop on death
- `CrowBoss.cs` — Level 1 boss, 40 HP, 3-phase AI (circling, summoning, frenzy), spawns minion crows
- `Ally.cs` — companion NPC with decision-tree AI, melee + ranged attacks

### Room Progression
- `RoomManager.cs` — tracks enemies per room, seals gates on entry, opens on clear, spawns rewards
- `RoomGate.cs` — solid barrier with open/close and fade animation
- Rooms are gated: kill all enemies to unlock the exit

### Pickups & Buffs
- `Pickup.cs` — collectible with types: Heart (2 HP heal), DamageBuff, SpeedBuff, FullHeal
- `BuffManager.cs` — on player, manages temporary damage (1.5x for 15s) and speed (1.5x for 15s) buffs

### Camera
- `CameraFollow.cs` — smooth follow (8 u/s), room-based bounds clamping via CameraRoom triggers, camera shake on attacks

### Health UI
- `HealthBarController.cs` — dynamic heart container instantiation, fractional heart display
- 2 HP per heart, max expandable via PlayerStats.MaxTotalHealth

### Dialogue
- `DialogueBox.cs` — singleton UI with typewriter effect, fade in/out, called via `DialogueBox.Show(message, duration)`
- `DialogueOnlyTrigger.cs` — trigger-based, calls DialogueBox.Show(), configurable duration, one-time option
- `RevealBlocksAndDialogueTrigger.cs` — reveals hidden objects + paired dialogue

### Boss UI
- `BossHealthBar.cs` — singleton UI with slider, called via `BossHealthBar.Show/UpdateHealth/Hide`

### Environment
- `CameraRoom.cs` — BoxCollider2D trigger, passes bounds to CameraFollow on player entry
- `DestructibleObject.cs` — 3 HP breakable objects with shake effect
- `KillZone.cs` / `KillZoneTrigger.cs` — hazard zones (scene reload or respawn)

## Coding Conventions

- Scripts use `MonoBehaviour` inheritance, `[SerializeField]` for inspector-exposed fields
- Physics interactions use `OnCollisionEnter2D` / `OnTriggerEnter2D`
- Enemy damage uses `ApplyDamage(int damage)` method convention — all damageable entities implement this
- Coroutines for timed effects (invincibility, stun, animations)
- Tags used: "Player", "Enemy", "DrawCharacter" (for ally targeting)
- Prefabs stored in `MetroidvaniaController/Prefabs/` or level-specific folders
- Level-specific scripts go in `Level{N}/Scripts/`
- Singletons used for global state (PlayerStats, DialogueBox, BossHealthBar)

## Level Design

- Levels are Unity scenes (`Level1/Level1.unity`, `Level2/Level2.unity`)
- Each level is divided into rooms defined by `CameraRoom` BoxCollider2D triggers
- Room transitions happen when the player walks into the next room's trigger
- Rooms are gated via RoomManager + RoomGate — enemies must be cleared to progress
- Level transitions use `SceneManager.LoadSceneAsync()` via build index (triggered by RoomManager on boss room clear)
- Respawn checkpoints use `PlayerRespawn.cs` with designated Transform references

## Build & Run

- Open in Unity Editor
- Scenes must be added to Build Settings in order (Level1, Level2, etc.)
- Play mode starts from the active scene

## Important Notes

- Player health is managed by the `PlayerStats` singleton — always use `PlayerStats.Instance` for health operations
- The `ApplyDamage()` pattern is the standard way to deal damage to any entity
- Camera bounds are per-room — new rooms must have a `CameraRoom` component
- Enemy AI uses `Physics2D` raycasts and overlap checks — layer masks matter
- All timed gameplay effects (buffs, invincibility, stun) use coroutines with `WaitForSeconds`
