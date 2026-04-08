# Level 3 — Editor Setup Steps

Everything that *can* be authored from outside Unity is already done. Here's what I built for you and what you need to finish in the editor.

---

## What's already in the scene (`Level3.unity`)

8 rooms laid out left-to-right along x. Room R is centered at `x = (R-1) * 28, y = 0`. Each room is 28 wide × 16 tall.

Per room, pre-wired:
- **Room root** with `BoxCollider2D` (trigger), `CameraRoom`, `RoomManager`
- **EntranceGate** (left wall) — solid `BoxCollider2D` + `RoomGate` script + `SpriteRenderer` (no sprite assigned)
- **ExitGate** (right wall) — same
- **RewardSpawn** empty Transform — wired into `RoomManager.rewardSpawnPoint`
- **Floor** (28×2 at y=-7) and **Ceiling** (28×2 at y=+7) — sealing the room exactly with the 12-tall gates

Per-room extras already placed:
- **Room 1** — `PlayerSpawn` empty Transform
- **Room 2** — `Spawn_AnglerLurker_A`, `Spawn_AnglerLurker_B`, `Spawn_CrabBruiser` markers
- **Room 3** — 3 platforms (low/mid/high) for the vertical jelly shaft + 3 jellyfish spawn markers
- **Room 4** — 2 ledges + `Spawn_Spitter_Left`, `Spawn_Spitter_Right`, `Spawn_CrabBruiser`
- **Room 5** — empty (Memory Alcove — optional reward room)
- **Room 6** — 2 platforms + 5 enemy markers (mixed)
- **Room 7** — empty (Stillwater Sanctum — pre-boss safe room)
- **Room 8 (Brinewyrm's Maw)** —
  - 3 raised platforms (left/mid/right)
  - 4 `BrinewyrmSurface_N` markers along the floor
  - **RisingTideHazard** GameObject with the script attached and configured (`lowY=-10`, `highY=-2`, `riseTime=8`, `holdTime=4`, `fallTime=4`)
  - `BossSpawn` Transform wired into `RoomManager.bossSpawnPoint`
  - `RoomManager.isBossRoom = true`, `loadNextLevelOnClear = true`

Script `.meta` files for the 7 new Level 3 scripts are pre-created with stable GUIDs (`a1..a7`) so the scene's MonoBehaviour references resolve cleanly on first Unity import.

---

## What you need to do in Unity

### 1. Open the project & verify the scene loads

1. Open the project in Unity.
2. Let it import — it'll compile the new scripts in `Assets/Level3/Scripts/`.
3. Open `Assets/Level3/Level3.unity`.
4. Check the Console for errors. If any script reference is "missing," tell me which one — I'll fix the GUID.

### 2. Add Level3 to Build Settings

`File → Build Settings → Add Open Scenes`. Drag it to be **after Level2** in the list.

### 3. Drop the Player into the scene

1. Find the Player prefab (likely `Assets/MetroidvaniaController/Prefabs/Player.prefab`).
2. Drag it into the scene hierarchy.
3. Move it to the `PlayerSpawn` marker position (or set its Transform to match — it's at world `(-10, -5)`).

### 4. Create prefabs for the 4 new enemies + projectile + boss

Each prefab is roughly: empty GameObject → add components → drop into `Assets/Level3/Enemies/`.

#### Crow heart pickup reference
Locate the Heart pickup prefab Level 1 already uses (`Pickup` script with `PickupType.Heart`). You'll drag this into each new enemy's `heartPickupPrefab` slot.

#### `Crab.prefab`
- Empty GameObject named "Crab"
- Tag: **Enemy**
- Add: `SpriteRenderer` (sprite of your choice), `Rigidbody2D` (gravity scale 3, freeze rotation Z), `BoxCollider2D` (sized to sprite)
- Add child empty GO `GroundCheckFront` placed at front-bottom corner of crab; add another `WallCheckFront` at front-middle
- Add `CrabBruiser` script
- Inspector: drag the rigidbody, sprite renderer, both check transforms, heart pickup prefab

#### `AnglerLurker.prefab`
- GameObject + SpriteRenderer + Rigidbody2D (gravity 0, dynamic) + BoxCollider2D + tag **Enemy**
- Child GameObject "Lure" with its own SpriteRenderer (the glowing bulb)
- Add `AnglerLurker` script
- Inspector: drag rigidbody, body sprite renderer, the **Lure** sprite renderer into `lureRenderer`, heart pickup prefab

#### `Jellyfish.prefab`
- GameObject + SpriteRenderer + Rigidbody2D (gravity 0) + CircleCollider2D + tag **Enemy**
- Add `JellyfishDrifter` script — defaults are fine
- Inspector: drag rigidbody, sprite, heart pickup prefab

#### `BrineProjectile.prefab`
- GameObject + SpriteRenderer + Rigidbody2D (gravity scale 1.5) + CircleCollider2D (set as **Trigger**)
- Add `BrineProjectile` script
- In the script's `terrainMask` field, select your Ground/Terrain layer
- Save as prefab — **don't put it in any room**, the spitters and boss will spawn it at runtime

#### `BrineSpitter.prefab`
- GameObject + SpriteRenderer + BoxCollider2D + tag **Enemy** (no Rigidbody — stationary)
- Child empty GO "Muzzle" placed where projectiles fire from
- Add `BrineSpitter` script
- Inspector: drag the BrineProjectile prefab into `brineProjectilePrefab`, drag Muzzle into `muzzle`, heart pickup prefab

#### `Brinewyrm.prefab`
- GameObject + SpriteRenderer + Rigidbody2D (gravity 0, kinematic) + BoxCollider2D
- **Important**: tag it **Enemy** so the player's attacks find it via the existing `ApplyDamage` pattern
- Add `BrinewyrmBoss` script
- The `surfacePoints`, `tideHazard`, `jellyfishPrefab`, `brineProjectilePrefab`, `fullHealPickupPrefab`, `rewardSpawnPoint` fields will be wired *after* you drop it in the scene (next step). Leave them empty for now in the prefab.

### 5. Place enemy instances in each room

For each `Spawn_*` marker GameObject in the scene:
1. Drag the matching enemy prefab into the hierarchy.
2. Set its Transform position to match the marker's position (or just parent → unparent so it inherits).
3. Drag the new enemy GameObject into that room's `RoomManager.enemies` list (in the inspector).
4. **Delete the spawn marker** once the enemy is placed (it was just a positioning hint).

Repeat for every marker in rooms 2, 3, 4, 6.

### 6. Wire up Room 8 (boss room)

1. Select the `Room8_BrinewyrmsMaw` GameObject in the hierarchy.
2. In `RoomManager`, drag your `Brinewyrm.prefab` into the `Boss Prefab` slot.
3. Set `Next Level Build Index` to whatever scene comes after Level3 (probably Level4 or back to a hub — leave at -1 for now if undecided).
4. The boss will spawn at the existing `BossSpawn` child Transform when the player enters the room.

But the boss prefab itself needs the 4 surface points, the tide hazard, and the projectile/jellyfish references. **The boss prefab won't have these at edit time** — you have to wire them on the *instance* once it's spawned, OR (cleaner) pre-spawn the boss in the scene for editing, wire its references, then make it inactive at start (RoomManager will spawn its own at runtime). Since RoomManager *spawns* the boss at runtime via `Instantiate`, prefab references must be set on the **prefab itself**, not a scene instance — and prefabs can't reference scene-only objects like `BrinewyrmSurface_N`.

**This is the one real wrinkle.** Two ways to solve it:

#### Option A (recommended) — Use a public setter pattern
Add a small initialization step. Open `RoomManager.cs` and after the `Instantiate(bossPrefab, ...)` line, add a call to a new `Initialize` method on `BrinewyrmBoss` that takes the surface points, tide hazard, etc. as arguments. The `RoomManager` already has a serialized reference to the spawned boss; you'd just need a serialized array of "boss scene refs" on the room. **Tell me if you want this and I'll implement it** — it's a 30-line change to `RoomManager` + adding an `Initialize` method to `BrinewyrmBoss`.

#### Option B — Pre-place the boss in the scene
1. Drag the Brinewyrm prefab into the scene at the `BossSpawn` position.
2. Wire all its references (surface points, tide hazard, projectile prefab, jellyfish prefab) on the instance.
3. **Disable the GameObject** in the inspector (uncheck the top checkbox).
4. In `RoomManager`, **clear** the `Boss Prefab` slot, and instead drag the disabled scene boss into the `enemies` list along with `isBossRoom = false`.
5. You'll need a small custom enabler — easier to just go with Option A.

**My recommendation: ask me to do Option A.** It keeps the prefab pattern clean.

### 7. Sprites & visuals

Everything in the scaffolded scene is invisible — gates, floors, ceilings, platforms have colliders but no sprites. You can:
- Paint a Tilemap on top of the floor/ceiling colliders for the sea-cave look (recommended — Level 1 uses this approach)
- Or assign sprites to each `SpriteRenderer` directly

The colliders will keep working regardless of how you do the visuals.

### 8. Test

1. Press Play in the Level 3 scene.
2. Walk right from PlayerSpawn — you should pass through Room 1 (no enemies, no gating) into Room 2.
3. Room 2 should seal both gates after a 3-second delay; clearing the enemies should reopen the exit.
4. Continue through to Room 8 and fight the Brinewyrm.

---

## Things I deliberately did NOT do (and why)

- **No tilemaps.** Tilemaps require referencing tile assets by GUID, which means choosing your tileset first. Paint these in the Unity tile editor.
- **No sprite assignments.** Same reason — sprite GUIDs vary per asset. The `SpriteRenderer` components exist; just drag a sprite into them.
- **No prefab instances of enemies in the scene.** Prefab instances in YAML are complex (`PrefabInstance` objects with override modifications), and the prefabs don't exist yet anyway. Spawn markers serve the same purpose with zero risk.
- **No Player in the scene.** Same reason — drag the existing Player prefab in.
- **No memory fragment inventory.** The proposal calls for it but it's a separate system; the boss death dialogue is a placeholder hook.

---

## Quick reference: world positions of key things

| Thing | World position |
|---|---|
| Room R center | `((R-1)*28, 0)` |
| Room R floor top | `y = -6` |
| Room R ceiling bottom | `y = +6` |
| Room R left gate | `((R-1)*28 - 14, 0)` |
| Room R right gate | `((R-1)*28 + 14, 0)` |
| PlayerSpawn | `(-10, -5)` |
| Brinewyrm Surface 1 | `(185, -5.5)` |
| Brinewyrm Surface 2 | `(192, -5.5)` |
| Brinewyrm Surface 3 | `(200, -5.5)` |
| Brinewyrm Surface 4 | `(207, -5.5)` |
| RisingTideHazard (low) | `(196, -10)` rises to `y=-2` |
