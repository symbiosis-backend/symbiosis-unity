using System.Collections.Generic;

namespace OzGame.Okey
{
    public class BotMemory
    {
        private readonly List<OkeyTile> seenDiscards = new List<OkeyTile>();

        public IReadOnlyList<OkeyTile> SeenDiscards => seenDiscards;

        public void RememberDiscard(OkeyTile tile)
        {
            if (tile != null) seenDiscards.Add(tile);
        }
    }
}
