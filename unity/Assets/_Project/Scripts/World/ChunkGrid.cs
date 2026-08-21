using System.Collections.Generic;
using UnityEngine;

namespace SushiSurvival.World
{
    public static class ChunkGrid
    {
        public static Vector2Int WorldToChunk(Vector2 worldPos, int chunkSize, float tileSize)
        {
            if (chunkSize <= 0 || tileSize <= 0f) return Vector2Int.zero;

            int tileX = Mathf.FloorToInt(worldPos.x / tileSize);
            int tileY = Mathf.FloorToInt(worldPos.y / tileSize);

            return new Vector2Int(FloorDiv(tileX, chunkSize), FloorDiv(tileY, chunkSize));
        }

        public static List<Vector2Int> GetRequiredChunks(Vector2Int center, int radius)
        {
            var chunks = new List<Vector2Int>();

            for (int y = center.y - radius; y <= center.y + radius; y++)
                for (int x = center.x - radius; x <= center.x + radius; x++)
                    chunks.Add(new Vector2Int(x, y));

            return chunks;
        }

        private static int FloorDiv(int a, int b) => Mathf.FloorToInt(a / (float)b);
    }
}
