# Level 3 — The Drowned Hollow

A flooded sea cave beneath the island. Bioluminescent, claustrophobic, dripping. Where Level 1 was open snowy mountains and aerial combat against the Stormcrow, Level 3 is **cramped, vertical, and amphibious** — the player fights through tide pools, tunnels, and submerged chambers down toward the lair of an ancient leviathan.

## Design Pillars (mapped to proposal)

- **Freedom (mechanical):** Infinite-jump traversal is exploited via vertical shafts and rising-tide rooms — the player can always go *up*, but the cave funnels them *down*.
- **Mastery:** New enemy archetypes force the player to learn new approaches (armored angles, telegraphed pulses, ranged arcs). Damage sponges are avoided; every enemy has a clear "tell."
- **Mystery:** Memory fragment narration delivered via existing `DialogueBox` triggers — players learn the protagonist used to sail these waters.
- **Reuses Level 1 systems:** `RoomManager`, `RoomGate`, `Pickup`, `BuffManager`, `CameraRoom`, `BossHealthBar`, `DialogueBox`. No engine changes required.

## Setting & Mood

- Palette: deep teal, bone-white coral, pale-green bioluminescence
- Background: dripping stalactites, shipwreck ribs, schooling silhouettes
- Audio cue (suggested): low ocean drone + intermittent water drips; music swells at the boss room

## Room Layout (8 rooms)

| # | Name | Purpose | Enemies | Notes |
|---|---|---|---|---|
| 1 | **Tidewash Entry** | Onboarding to the cave. Soft intro, no gates. | 1 Crab Bruiser | Dialogue trigger: "I've… been to this shore before." |
| 2 | **Lure Pools** | Teaches AnglerLurker telegraph. | 2 Angler Lurkers, 1 Crab | Dim lighting; lures glow before lunge. |
| 3 | **Coral Gauntlet** | Vertical shaft using infinite jumps. | 3 Jellyfish Drifters | Players must rise through the shock-pulse rhythm. |
| 4 | **The Spitter Choir** | Introduces ranged threat. | 2 Brine Spitters on ledges, 1 Crab below | Ranged + ground melee mix. |
| 5 | **Memory Alcove** | Optional reward room. | (none) | Hidden pickup + dialogue fragment. No gating. |
| 6 | **Black Tidepool** | Combination room. | 2 Anglers, 2 Jellyfish, 1 Spitter | Difficulty spike before boss. |
| 7 | **Stillwater Sanctum** | Pre-boss safe room. | (none) | Full-heal pickup, dialogue: name of the boss spoken. |
| 8 | **The Brinewyrm's Maw** | Boss arena. | Brinewyrm (boss) | Multi-phase fight; rising tide hazard. |

## New Enemies (sea-cave themed)

### Angler Lurker — `AnglerLurker.cs`
- **HP:** 6, **Damage:** 2 (lunge contact)
- **Behavior:** Idles in place. A glowing lure hovers above it. When the player enters a sight cone (forward, 5u), the lure flashes red as a 0.5s telegraph, then the lurker lunges in a straight line. Hits a wall = stunned 1.5s (vulnerable window).
- **Counter:** Bait the lunge, then strike during stun. Teaches reading telegraphs.
- **Drop:** 25% Heart.

### Crab Bruiser — `CrabBruiser.cs`
- **HP:** 12 (effectively), **Damage:** 2 (charge contact)
- **Behavior:** Patrols a ledge. When the player is in front, charges horizontally. Has an **armored front**: melee from the front deals 0 damage (sparks). Damage only registers from **above** (player attacking while above the crab) or from **behind**.
- **Counter:** Jump over to flank, or strike from above mid-air.
- **Drop:** 30% Heart.

### Jellyfish Drifter — `JellyfishDrifter.cs`
- **HP:** 4, **Damage:** 2 (shock pulse)
- **Behavior:** Drifts slowly along a vertical bob path. Periodically (every 2.5s) emits a **shock pulse** in a 2u radius. Telegraphed by 0.6s of color flash. Pulse damages player on overlap.
- **Counter:** Strike between pulses, or stay outside the radius.
- **Drop:** 20% Heart.

### Brine Spitter — `BrineSpitter.cs`
- **HP:** 6, **Damage:** 2 (projectile)
- **Behavior:** Stationary turret. Tracks the player horizontally. Every 2.0s, fires an arcing `BrineProjectile` aimed at the player's current position with gravity applied. Cannot move.
- **Counter:** Close the gap and melee, or hit with a thrown weapon (V).
- **Drop:** 25% Heart.

## Boss — The Brinewyrm — `BrinewyrmBoss.cs`

A serpent that lives beneath the arena floor. The arena has 4 surface points along the bottom and 3 raised platforms accessible via infinite-jump. The wyrm is invulnerable while submerged — it must surface to be hit.

- **HP:** 60
- **Hit window:** Only vulnerable while a "head segment" is exposed above ground (~1.8s after a surface).

### Phase 1 — Surfacing (100% → 65%)
- Picks a random surface point.
- 0.7s telegraph (red flash + rumble).
- Head bursts up, snaps left/right, then retreats.
- 1.5s pause between attacks.

### Phase 2 — Rising Tide (65% → 30%)
- Spawns a `RisingTideHazard` from the floor that rises to mid-arena and recedes on a cycle (8s up, 4s held, 4s down).
- Continues surfacing attacks during tide.
- Player must use upper platforms to avoid the tide.
- Summons 1 Jellyfish Drifter when the tide peaks (max 3 alive).

### Phase 3 — Frenzy (30% → 0%)
- Surface telegraph drops to 0.3s.
- Surfaces twice in quick succession (left + right side).
- Spits 3 brine projectiles in a fan after each surface.
- Tide cycles faster (5s up / 2s held / 2s down).

### Death
- Drops a `FullHeal` pickup at the arena center.
- Calls `BossHealthBar.Hide()`.
- `RoomManager` triggers level transition (`loadNextLevelOnClear`) — same pattern as Level 1.

## Memory Fragment Hook
The boss-clear reward should be paired (in a follow-up commit) with the **memory fragment** system from the proposal — for now we leave a `DialogueBox.Show("...fragment text...", 6f)` call after boss death so the narrative beat lands even before the inventory system exists.

## Dependencies / Editor Setup Checklist
1. Add `Level3.unity` to Build Settings (after Level2).
2. On every room: `CameraRoom` (BoxCollider2D trigger) + `RoomManager` (with entrance/exit `RoomGate` references and enemies list).
3. Boss room: set `isBossRoom = true`, assign `BrinewyrmBoss` prefab, set `loadNextLevelOnClear = true`.
4. New enemy prefabs go in `Assets/Level3/Enemies/`.
5. Tag projectiles ignore "Enemy" layer collisions to avoid friendly fire.
