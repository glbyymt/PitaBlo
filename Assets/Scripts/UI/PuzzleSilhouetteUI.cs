using System.Collections.Generic;
using Pitablock.Core;
using Pitablock.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Pitablock.UI
{
    /// <summary>
    /// 型はめ遊び用のシルエット表示。
    /// </summary>
    public class PuzzleSilhouetteUI : MonoBehaviour
    {
        private readonly List<Image> silhouetteImages = new();
        private GameBoardUI boardUI;
        private RectTransform overlayRect;

        public void Configure(GameBoardUI board)
        {
            boardUI = board;
            if (boardUI?.PlacedCellsRect == null)
            {
                return;
            }

            overlayRect = new GameObject("SilhouetteOverlay", typeof(RectTransform)).GetComponent<RectTransform>();
            overlayRect.SetParent(boardUI.PlacedCellsRect, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.SetAsFirstSibling();
        }

        public void Refresh()
        {
            Clear();

            var session = GameModeSession.Instance;
            if (session == null || boardUI == null || overlayRect == null)
            {
                return;
            }

            var cells = session.GetSilhouetteCells();
            if (cells == null || cells.Count == 0)
            {
                return;
            }

            var cellSize = boardUI.CellSize;
            var sprite = SpriteFactory.CreateRoundedSquareSprite(64, Color.white);

            foreach (var gridPos in cells)
            {
                var go = new GameObject($"Silhouette_{gridPos.x}_{gridPos.y}", typeof(RectTransform), typeof(Image));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(overlayRect, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.sizeDelta = new Vector2(cellSize, cellSize);
                rect.anchoredPosition = boardUI.GridToAnchored(gridPos);

                var image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.color = new Color(0.2f, 0.45f, 0.85f, 0.28f);
                image.raycastTarget = false;
                silhouetteImages.Add(image);
            }
        }

        public void Clear()
        {
            foreach (var image in silhouetteImages)
            {
                if (image != null)
                {
                    Destroy(image.gameObject);
                }
            }

            silhouetteImages.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
