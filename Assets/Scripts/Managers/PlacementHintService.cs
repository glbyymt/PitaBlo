using System.Collections.Generic;
using Pitablock.Controllers;
using Pitablock.Data;
using UnityEngine;

namespace Pitablock.Managers
{
    /// <summary>
    /// おしえて機能用：手持ちブロックのおすすめ配置を探索する。
    /// </summary>
    public static class PlacementHintService
    {
        public static bool TryFindHint(
            BlockController handBlock,
            out Vector2Int[] gridPositions,
            out Vector2Int shapeAnchor)
        {
            gridPositions = null;
            shapeAnchor = default;

            var session = GameModeSession.Instance;
            var grid = GridManager.Instance;
            if (handBlock == null || session == null || grid == null)
            {
                return false;
            }

            if (session.TryGetAuthoritativeHint(handBlock, out gridPositions, out shapeAnchor))
            {
                return true;
            }

            return TryFindBestPlacement(handBlock, grid, session.ActiveMode, out gridPositions, out shapeAnchor);
        }

        private static bool TryFindBestPlacement(
            BlockController handBlock,
            GridManager grid,
            GameMode mode,
            out Vector2Int[] gridPositions,
            out Vector2Int shapeAnchor)
        {
            gridPositions = null;
            shapeAnchor = default;
            Vector2Int[] best = null;
            var bestScore = int.MinValue;

            for (var x = 0; x < GridManager.GridWidth; x++)
            {
                for (var y = 0; y < GridManager.GridHeight; y++)
                {
                    var anchor = new Vector2Int(x, y);
                    var candidate = handBlock.GetGridPositionsForAnchorPublic(anchor);
                    if (!grid.CanPlaceBlock(candidate))
                    {
                        continue;
                    }

                    var score = ScorePlacement(candidate, mode, handBlock.Data.blockId);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }
            }

            if (best == null)
            {
                return false;
            }

            shapeAnchor = handBlock.ComputeShapeAnchorPublic(best);
            gridPositions = best;
            return true;
        }

        private static int ScorePlacement(Vector2Int[] positions, GameMode mode, int blockId)
        {
            var filledRows = 0;
            for (var y = 0; y < GridManager.GridHeight; y++)
            {
                var count = 0;
                foreach (var pos in positions)
                {
                    if (pos.y == y)
                    {
                        count++;
                    }
                }

                if (count > 0)
                {
                    var existing = CountRowOccupancy(y);
                    if (existing + count >= GridManager.GridWidth)
                    {
                        filledRows++;
                    }
                }
            }

            var score = filledRows * 100;
            if (mode == GameMode.BlockRoad && blockId == 3)
            {
                score += 10;
            }

            score -= positions[0].y;
            return score;
        }

        private static int CountRowOccupancy(int rowY)
        {
            var count = 0;
            for (var x = 0; x < GridManager.GridWidth; x++)
            {
                if (GridManager.Instance.IsCellOccupied(new Vector2Int(x, rowY)))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
