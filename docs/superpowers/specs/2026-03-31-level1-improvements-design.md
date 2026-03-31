# Level 1 Improvements Design — Saltwake

**Date:** 2026-03-31
**Status:** Approved

## Overview

Improve Level 1 by adding gated room progression, enemy drop rewards, minimal narrative, a dialogue text box UI, and a Giant Crow boss fight. The level follows a linear room chain with a clear difficulty curve.

## Design Decisions

| Area | Choice | Rationale |
|---|---|---|
| Direction | Gated progression | Rooms lock until cleared — gives structure and pacing |
| Rewards | Direct stat pickups | Immediate, no inventory system needed |
| Narrative | Minimal/atmospheric | Two dialogue moments — intro and pre-boss |
| Boss | Giant Crow (scaled existing enemy) | Thematic consistency with Level 1's crow enemies |
| Level structure | Linear room chain | Simplest to build, clear pacing, easy to tune |

---

## 1. Room Gating System

Each room has a **RoomGate** — a barrier blocking the exit. When the player enters a room:

1. The entrance gate (if present) seals behind them — Room 1 has no entrance gate (spawn room), all others do
2. All enemies in the room are tracked via a **RoomManager** component on each CameraRoom
3. When the last enemy dies, the exit gate opens (visual/audio cue — gate crumbles, ice shatters)
4. A stat pickup spawns near the opened gate

### Implementation

- New `RoomManager.cs` script attached alongside `CameraRoom`
- Holds references to all enemies in its room, listens for their deaths
- When enemy count hits 0: activates gate-open animation, spawns reward pickup
- No fragment tracking or inventory — "kill everything, door opens, grab reward, move on"

---

## 2. Enemy Drops & Stat Pickups

### Pickup Types

**Heart Pickup:**
- Restores 2 HP (one heart) on contact
- Spawns at the opened gate location after room clear
- Uses existing `PlayerStats.Heal()`

**Temporary Buff Pickups:**
- **Damage Boost** — melee does 6 instead of 4 for 15 seconds
- **Speed Boost** — run speed +25% for 15 seconds
- Visual indicator on player while active (tint or particle trail)

### Drop Logic

- **Individual enemies:** 20% chance to drop a heart on death
- **Room clear reward:** Guaranteed pickup (heart or random buff) at the gate

### Implementation

- `Pickup.cs` base script with enum for type (Heart, DamageBuff, SpeedBuff)
- Pickups are prefabs with trigger collider and small bounce/float animation
- `BuffManager.cs` on the player tracks active temporary buffs, resets after duration
- Drop logic in `Enemy.cs`/`CrowEnemy.cs` (on death) and `RoomManager.cs` (on room clear)

---

## 3. Narrative

### Dialogue Moments

**Intro (Room 1 spawn):**
> "The salt winds carry something wrong from the peak. Push forward."

**Pre-Boss (entering Boss Room):**
> "The air splits. Something ancient circles above."

Both use `DialogueOnlyTrigger` with `onlyOnce = true`.

### Dialogue Text Box UI

Replaces raw TextMeshPro text with a styled container:

**Visual:**
- Semi-transparent dark panel (black at ~70% opacity), slightly rounded corners
- Subtle cold blue-white border/edge glow (matches snowy theme)
- White centered text, positioned bottom-center (~15% from bottom edge)
- Width: ~60% of screen, height auto-fits to text content
- 16px-equivalent padding

**Animation:**
- Appear: panel fades in over 0.3s, text types in character-by-character (~30 chars/second)
- Hold: visible for configured duration (default 3s after typing completes)
- Disappear: entire box fades out over 0.5s

**Implementation:**
- `DialogueBox.cs` UI component on a Canvas panel prefab
- Prefab contains: Panel (Image + rounded sprite), child TextMeshPro text, CanvasGroup for fades
- Coroutine-driven typewriter + fade logic
- `DialogueOnlyTrigger` updated to call `DialogueBox.Show(message, duration)`
- Reusable for any future dialogue

---

## 4. Giant Crow Boss — The Stormcrow

Final room boss. Visually a ~3x scaled Crow sprite.

### Stats

- Health: 40 HP (10 hits at base melee damage of 4)
- Contact damage: 2 HP
- Boss health bar at top of screen

### Phase 1 — Circling (40-28 HP)

- Flies in a wide circle around the arena
- Periodically dives at the player
- Dive: 0.5s telegraph (sprite flashes/shakes), then charges in straight line at player position
- Dive cooldown: 3s

### Phase 2 — Summoning (28-14 HP)

- Same circling + dive, but dive cooldown drops to 2s
- Every 8s, spawns 2 regular Crows (existing `CrowEnemy` AI)
- Max 4 spawned crows alive at once

### Phase 3 — Frenzy (14-0 HP)

- Stops circling, hovers briefly, then performs rapid triple-dive (3 dives, 0.8s between each)
- No more crow spawning
- After burst: 2s pause (vulnerability window), then repeats
- Speed increased by 30%

### On Death

- All spawned crows die instantly
- Boss death animation (falls, fades out)
- 1.5s pause, then exit gate opens
- Full heal pickup spawns (all hearts restored)
- Exiting room triggers Level 2 scene load

### Implementation

- `CrowBoss.cs` extending steering/vision logic from `CrowEnemy.cs`
- `BossHealthBar.cs` UI component on canvas
- Boss room's `RoomManager` has a boss flag — spawns boss on room entry instead of tracking existing enemies
- `RoomManager` also tracks spawned crows (from Phase 2) so they can be killed on boss death

---

## 5. Level Flow

The player descends diagonally then ascends to the summit — down into danger, then back up to confront the boss.

### Room 1 — The Awakening (top-left)
- Player spawn + intro dialogue
- 2-3 patrol enemies (warm-up)
- Gate clear drops a heart
- Exit: down-right to Room 2

### Room 2 — The Descent (mid-left/center)
- Introduces Crows (2 crows + 1 patrol)
- Gate clear drops a damage buff
- Exit: down-right to Room 3

### Room 3 — The Frozen Lake (bottom-center)
- Lowest point, platforming + tight combat (2 patrols + 2 crows)
- Gate clear drops a heart
- Exit: up-right to Room 4

### Room 4 — The Ridge (right-side)
- Hardest standard room, vertical ascent with crows attacking mid-air (3 crows + 1 patrol)
- Player uses infinite jumps to ascend — crows pressure them during the climb
- Gate clear drops a speed buff
- Pre-boss dialogue triggers at exit
- Exit: up to Boss Room

### Boss Room — The Stormcrow's Nest (top-right)
- Gate seals on entry, boss spawns
- Giant Crow boss fight (3 phases)
- On victory: full heal + Level 2 transition

### Difficulty Curve
Patrol-only -> mixed -> crow-heavy -> boss

---

## 6. New Scripts Summary

| Script | Purpose | Attaches To |
|---|---|---|
| `RoomManager.cs` | Tracks enemies, manages gates, spawns rewards | CameraRoom GameObjects |
| `RoomGate.cs` | Gate barrier with open/close animation | Gate GameObjects |
| `Pickup.cs` | Collectible pickups (heart, damage buff, speed buff) | Pickup Prefabs |
| `BuffManager.cs` | Tracks active temporary buffs on the player | Player GameObject |
| `DialogueBox.cs` | Styled text box with typewriter + fade animations | UI Canvas |
| `CrowBoss.cs` | Giant Crow boss AI with 3 phases | Boss Prefab |
| `BossHealthBar.cs` | Boss HP bar UI | UI Canvas |

## 7. Modified Scripts

| Script | Change |
|---|---|
| `Enemy.cs` | Add 20% heart drop on death |
| `CrowEnemy.cs` | Add 20% heart drop on death |
| `DialogueOnlyTrigger.cs` | Use new `DialogueBox` instead of raw TMP |
| `PlayerMovement.cs` | Expose run speed for buff modification |
| `Attack.cs` | Expose melee damage for buff modification |
