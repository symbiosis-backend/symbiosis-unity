using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MahjongGame
{
    public static class StoryChinaTileLibrary
    {
        private const string ResourceRoot = "Mahjong/StoryChina/Tiles/";
        private static readonly Vector2 TileSize = new(110f, 150f);
        private static readonly List<TileData> Tiles = new();
        private static Transform templateRoot;

        private static readonly string[] TileIds =
        {
            "china_bamboo_forest",
            "china_chopsticks",
            "china_paper_scroll",
            "china_tiger",
            "china_tortoise",
            "china_shrimp_noodles",
            "china_kungfu_monk",
            "china_lotus_flower",
            "china_dumplings",
            "china_river_landscape",
            "china_high_speed_train",
            "china_imperial_seal",
            "china_yellow_river",
            "china_panda",
            "china_peking_duck",
            "china_compass",
            "china_dharma_wheel",
            "china_forbidden_city",
            "china_spicy_chicken",
            "china_gunpowder",
            "china_space_station",
            "china_dragon",
            "china_yin_yang"
        };

        public static IReadOnlyList<TileData> GetTiles(Transform owner)
        {
            if (HasValidTiles())
                return Tiles;

            Tiles.Clear();
            EnsureTemplateRoot(owner);

            for (int i = 0; i < TileIds.Length; i++)
            {
                string id = TileIds[i];
                Texture2D texture = Resources.Load<Texture2D>(ResourceRoot + id);
                if (texture == null)
                {
                    Debug.LogWarning($"[StoryChinaTileLibrary] Missing China tile texture: {id}");
                    continue;
                }

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);

                Tile tile = CreateTemplateTile(id, sprite);
                Tiles.Add(new TileData
                {
                    Id = id,
                    Prefab = tile
                });
            }

            return Tiles;
        }

        private static bool HasValidTiles()
        {
            if (Tiles.Count == 0)
                return false;

            for (int i = Tiles.Count - 1; i >= 0; i--)
            {
                TileData tile = Tiles[i];
                if (tile == null || tile.Prefab == null)
                    return false;
            }

            return true;
        }

        private static void EnsureTemplateRoot(Transform owner)
        {
            if (templateRoot != null)
                return;

            GameObject root = new("StoryChinaTileTemplates");
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
