using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    public static class StoryThemedTileLibrary
    {
        private static readonly Vector2 TileSize = new(110f, 150f);
        private static readonly Dictionary<int, List<TileData>> TilesByLevel = new();
        private static Transform templateRoot;

        public static IReadOnlyList<TileData> GetTiles(int levelNumber, Transform owner)
        {
            if (!StoryThemedContentLibrary.TryGetDefinition(levelNumber, out StoryThemedContentLibrary.StoryLevelDefinition definition))
                return null;

            if (HasValidTiles(levelNumber))
                return TilesByLevel[levelNumber];

            EnsureTemplateRoot(owner);

            List<TileData> tiles = new();
            string resourceRoot = $"Mahjong/{definition.ResourceFolder}/Tiles/";

            for (int i = 1; i <= definition.TileCount; i++)
            {
                string id = $"{definition.TilePrefix}_{i:00}";
                Texture2D texture = Resources.Load<Texture2D>(resourceRoot + id);
                if (texture == null)
                {
                    Debug.LogWarning($"[StoryThemedTileLibrary] Missing tile texture: {resourceRoot}{id}");
                    continue;
                }

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);

                Tile tile = CreateTemplateTile(id, sprite);
                tiles.Add(new TileData
                {
                    Id = id,
                    Prefab = tile
                });
            }

            TilesByLevel[levelNumber] = tiles;
            return tiles;
        }

        private static bool HasValidTiles(int levelNumber)
        {
            if (!TilesByLevel.TryGetValue(levelNumber, out List<TileData> tiles) || tiles == null || tiles.Count == 0)
                return false;

            for (int i = tiles.Count - 1; i >= 0; i--)
            {
                TileData tile = tiles[i];
                if (tile == null || tile.Prefab == null)
                    return false;
            }

            return true;
        }

        private static void EnsureTemplateRoot(Transform owner)
        {
            if (templateRoot != null)
                return;

            GameObject root = new("StoryThemedTileTemplates");
            root.SetActive(false);

            if (owner != null)
                root.transform.SetParent(owner, false);

            templateRoot = root.transform;
        }

        private static Tile CreateTemplateTile(string id, Sprite sprite)
        {
            GameObject go = new(id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Tile));
            go.transform.SetParent(templateRoot, false);
            go.SetActive(false);

            Image rootImage = go.GetComponent<Image>();
            if (rootImage != null)
                rootImage.color = new Color(1f, 1f, 1f, 0f);

            Tile tile = go.GetComponent<Tile>();
            tile.ConfigureRuntimeTemplate(id, sprite, TileSize);
            return tile;
        }
    }
}
