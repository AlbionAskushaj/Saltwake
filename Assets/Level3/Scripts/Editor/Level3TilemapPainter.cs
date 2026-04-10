// Auto-paints Level 3's Tilemap_Foreground and Tilemap_Background with the
// Cave Platformer Tileset sprites, using the existing BoxCollider2D geometry
// as the source of truth for where rocks should go.
//
// Run from the Unity menu:  Tools > Saltwake > Paint Level 3 Cave Tilemap
//
// Safe to re-run: it clears existing tiles first, so you can tweak the
// sprite indices at the top of this file and click the menu again.

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Level3TilemapPainter
{
    // --- Tile selection (cave_tileset.png sprite indices) ---
    // The sheet is 8 tiles wide. Rows 8..11 are the blue/sea-cave variant.
    // Change these if you want different rock tiles painted.
    const int FG_FLOOR_TOP = 65;   // walkable top of a floor
    const int FG_FLOOR_BODY = 73;  // mid body fill
    const int FG_CEILING = 89;     // bottom edge (hangs down from ceiling)
    const int FG_PLATFORM = 65;    // platforms are usually 1 tile tall
    const int BG_FILL = 81;        // dark blue background fill

    const string TILESET_PATH =
        "Assets/Cave Platformer Tileset/Enviroment sprite sheets/cave_tileset.png";
    const string TILE_ASSET_FOLDER = "Assets/Level3/Tilemaps/CaveTiles";

    [MenuItem("Tools/Saltwake/Paint Level 3 Cave Tilemap")]
    public static void Paint()
    {
        // 1. Load all sprites from the cave tileset
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(TILESET_PATH);
        var sprites = new Dictionary<int, Sprite>();
        foreach (var obj in allAssets)
        {
            if (obj is Sprite s)
            {
                // sprite name format: "tileset-cave_N"
                int underscore = s.name.LastIndexOf('_');
                if (underscore >= 0 &&
                    int.TryParse(s.name.Substring(underscore + 1), out int idx))
                {
                    sprites[idx] = s;
                }
            }
        }
        if (sprites.Count == 0)
        {
            Debug.LogError(
                "Level3TilemapPainter: no sprites found at " + TILESET_PATH +
                ". Make sure the tileset is imported as Sprite Mode = Multiple.");
            return;
        }

        // 2. Ensure the tile asset folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Level3/Tilemaps"))
            AssetDatabase.CreateFolder("Assets/Level3", "Tilemaps");
        if (!AssetDatabase.IsValidFolder(TILE_ASSET_FOLDER))
            AssetDatabase.CreateFolder("Assets/Level3/Tilemaps", "CaveTiles");

        // 3. Create Tile assets for each sprite we use
        Tile tFloorTop = GetOrCreateTile(sprites, FG_FLOOR_TOP);
        Tile tFloorBody = GetOrCreateTile(sprites, FG_FLOOR_BODY);
        Tile tCeiling = GetOrCreateTile(sprites, FG_CEILING);
        Tile tPlatform = GetOrCreateTile(sprites, FG_PLATFORM);
        Tile tBg = GetOrCreateTile(sprites, BG_FILL);

        if (tFloorTop == null || tFloorBody == null || tCeiling == null ||
            tPlatform == null || tBg == null)
        {
            Debug.LogError("Level3TilemapPainter: one or more tiles failed to load. " +
                "Check the sprite indices at the top of the script.");
            return;
        }

        // 4. Find the two tilemaps in the active scene
        Tilemap fg = FindTilemapByName("Tilemap_Foreground");
        Tilemap bg = FindTilemapByName("Tilemap_Background");
        if (fg == null || bg == null)
        {
            Debug.LogError("Level3TilemapPainter: Tilemap_Foreground or " +
                "Tilemap_Background not found in the active scene.");
            return;
        }

        fg.ClearAllTiles();
        bg.ClearAllTiles();

        // 5. Paint foreground rocks over every floor / ceiling / platform / ledge
        int fgPainted = 0;
        foreach (var col in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            var go = col.gameObject;
            string name = go.name;

            bool isFloor = name.EndsWith("_Floor");
            bool isCeiling = name.EndsWith("_Ceiling");
            bool isPlatform = name.Contains("Platform") || name.Contains("Ledge");

            if (!isFloor && !isCeiling && !isPlatform) continue;

            GetTileBounds(col, out int x0, out int x1, out int y0, out int y1);
            for (int x = x0; x < x1; x++)
            {
                for (int y = y0; y < y1; y++)
                {
                    Tile pick;
                    if (isFloor)
                        pick = (y == y1 - 1) ? tFloorTop : tFloorBody;
                    else if (isCeiling)
                        pick = (y == y0) ? tCeiling : tFloorBody;
                    else
                        pick = tPlatform;
                    fg.SetTile(new Vector3Int(x, y, 0), pick);
                    fgPainted++;
                }
            }
        }

        // 6. Paint background fill behind every room (CameraRoom triggers)
        int bgPainted = 0;
        foreach (var col in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            var go = col.gameObject;
            // Rooms are named "RoomN_<something>" and have CameraRoom component.
            // We use a looser check: name starts with "Room" and contains no underscore suffix
            // like _Floor/_Ceiling/_Gate/_Tide (those are children).
            if (!go.name.StartsWith("Room")) continue;
            if (go.name.EndsWith("_Floor") || go.name.EndsWith("_Ceiling") ||
                go.name.Contains("Platform") || go.name.Contains("Ledge") ||
                go.name.EndsWith("_Tide") || go.name.Contains("_Gate")) continue;

            GetTileBounds(col, out int x0, out int x1, out int y0, out int y1);
            for (int x = x0; x < x1; x++)
                for (int y = y0; y < y1; y++)
                    if (bg.GetTile(new Vector3Int(x, y, 0)) == null)
                    {
                        bg.SetTile(new Vector3Int(x, y, 0), tBg);
                        bgPainted++;
                    }
        }

        // 7. Hide the Option A colored fallback boxes (floors/ceilings/platforms/tide).
        // Leave gates alone — they have their own RoomGate fade logic.
        int hidden = 0;
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            string name = sr.gameObject.name;
            if (name.EndsWith("_Floor") || name.EndsWith("_Ceiling") ||
                name.Contains("Platform") || name.Contains("Ledge") ||
                name.EndsWith("_Tide"))
            {
                if (sr.enabled)
                {
                    sr.enabled = false;
                    hidden++;
                }
            }
        }

        EditorUtility.SetDirty(fg);
        EditorUtility.SetDirty(bg);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log(
            $"Level3TilemapPainter: painted {fgPainted} foreground tiles, " +
            $"{bgPainted} background tiles, hid {hidden} fallback sprites. " +
            "Save the scene (Cmd+S) to keep changes.");
    }

    [MenuItem("Tools/Saltwake/Clear Level 3 Cave Tilemap")]
    public static void Clear()
    {
        Tilemap fg = FindTilemapByName("Tilemap_Foreground");
        Tilemap bg = FindTilemapByName("Tilemap_Background");
        if (fg != null) fg.ClearAllTiles();
        if (bg != null) bg.ClearAllTiles();

        // Re-enable the Option A fallback sprites
        int shown = 0;
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            string name = sr.gameObject.name;
            if (name.EndsWith("_Floor") || name.EndsWith("_Ceiling") ||
                name.Contains("Platform") || name.Contains("Ledge") ||
                name.EndsWith("_Tide"))
            {
                if (!sr.enabled)
                {
                    sr.enabled = true;
                    shown++;
                }
            }
        }

        if (fg != null) EditorUtility.SetDirty(fg);
        if (bg != null) EditorUtility.SetDirty(bg);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"Level3TilemapPainter: cleared tilemaps, re-enabled {shown} fallback sprites.");
    }

    // --- helpers ---

    static Tile GetOrCreateTile(Dictionary<int, Sprite> sprites, int index)
    {
        if (!sprites.TryGetValue(index, out var sprite) || sprite == null)
        {
            Debug.LogWarning($"Level3TilemapPainter: sprite index {index} not found in tileset.");
            return null;
        }
        string path = $"{TILE_ASSET_FOLDER}/{sprite.name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (existing != null) return existing;
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    static Tilemap FindTilemapByName(string name)
    {
        foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (tm.gameObject.name == name) return tm;
        return null;
    }

    static void GetTileBounds(BoxCollider2D col, out int x0, out int x1, out int y0, out int y1)
    {
        Vector2 center = (Vector2)col.transform.position + col.offset;
        Vector2 half = col.size * 0.5f * col.transform.lossyScale;
        float minX = center.x - half.x;
        float maxX = center.x + half.x;
        float minY = center.y - half.y;
        float maxY = center.y + half.y;
        x0 = Mathf.FloorToInt(minX);
        x1 = Mathf.FloorToInt(maxX);
        y0 = Mathf.FloorToInt(minY);
        y1 = Mathf.FloorToInt(maxY);
        // Edge case: if exact integer, make sure we include at least 1 tile
        if (x1 == x0) x1 = x0 + 1;
        if (y1 == y0) y1 = y0 + 1;
    }
}
