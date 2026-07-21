using System;
using System.Collections.Generic;
using UnityEngine;

namespace OzGame.Okey
{
    public class TileMgr : MonoBehaviour
    {
        public List<OkeyTile> CreateTiles()
        {
            var tiles = new List<OkeyTile>(106);
            var id = 0;
            foreach (OkeyColor color in new[] { OkeyColor.Red, OkeyColor.Yellow, OkeyColor.Blue, OkeyColor.Black })
            {
                for (var number = 1; number <= 13; number++)
                {
                    for (var copy = 0; copy < 2; copy++)
                    {
                        tiles.Add(new OkeyTile
                        {
                            id = id++,
                            color = color,
                            number = number,
                            copyIndex = copy,
                            type = OkeyTileType.Number,
                            runtimeGuid = Guid.NewGuid().ToString("N")
                        });
                    }
                }
            }

            for (var copy = 0; copy < 2; copy++)
            {
                tiles.Add(new OkeyTile
                {
                    id = id++,
                    color = OkeyColor.None,
                    number = 0,
                    copyIndex = copy,
                    type = OkeyTileType.FakeJoker,
                    runtimeGuid = Guid.NewGuid().ToString("N")
                });
            }
            return tiles;
        }

        public void BuildRound(OkeyMatch match, OkeyRulesConfig config)
        {
            var tiles = CreateTiles();
            Shuffle(tiles, match.matchSeed);
            var indicatorIndex = tiles.FindIndex(t => t.type == OkeyTileType.Number);
            var indicator = tiles[indicatorIndex];
            tiles.RemoveAt(indicatorIndex);
            indicator.isIndicator = true;

            var real = new OkeyTile
            {
                color = indicator.color,
                number = indicator.number == 13 ? 1 : indicator.number + 1,
                type = OkeyTileType.Number,
                isRealOkey = true
            };

            foreach (var tile in tiles)
                tile.isRealOkey = tile.type == OkeyTileType.Number && tile.color == real.color && tile.number == real.number;

            match.indicatorTile = indicator;
            match.realOkeyTile = real;
            match.stockPile = tiles;
        }

        public OkeyTile Draw(OkeyMatch match)
        {
            if (match.stockPile.Count == 0) return null;
            var index = match.stockPile.Count - 1;
            var tile = match.stockPile[index];
            match.stockPile.RemoveAt(index);
            return tile;
        }

        public void Shuffle(List<OkeyTile> tiles, int seed)
        {
            var rng = new System.Random(seed);
            for (var i = tiles.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
            }
        }
    }
}
