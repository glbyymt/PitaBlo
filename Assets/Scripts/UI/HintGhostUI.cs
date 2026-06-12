using System.Collections;
using System.Collections.Generic;
using Pitablock.Controllers;
using Pitablock.Core;
using Pitablock.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Pitablock.UI
{
    /// <summary>
    /// おしえて機能用のゴースト表示。
    /// </summary>
    public class HintGhostUI : MonoBehaviour
    {
        private readonly List<Image> ghostImages = new();
        private GameBoardUI boardUI;
        private RectTransform overlayRect;
        private Coroutine pulseCoroutine;

        public void Configure(GameBoardUI board)
        {
            boardUI = board;
            if (boardUI?.PlacedCellsRect == null)
            {
                return;
            }

            overlayRect = new GameObject("HintGhostOverlay", typeof(RectTransform)).GetComponent<RectTransform>();
            overlayRect.SetParent(boardUI.PlacedCellsRect, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
        }

        public void Show(BlockController handBlock)
        {
            Hide();

            if (handBlock == null || boardUI == null || overlayRect == null)
            {
                return;
            }

            if (!PlacementHintService.TryFindHint(handBlock, out var gridPositions, out _))
            {
                return;
            }

            var cellSize = boardUI.CellSize;
            var sprite = SpriteFactory.CreateRoundedSquareSprite(64, Color.white);
            var color = handBlock.Data != null ? handBlock.Data.blockColor : Color.white;

            foreach (var gridPos in gridPositions)
            {
                var go = new GameObject($"HintGhost_{gridPos.x}_{gridPos.y}", typeof(RectTransform), typeof(Image));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(overlayRect, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = Vector2.zero;
                rect.sizeDelta = new Vector2(cellSize, cellSize);
                rect.anchoredPosition = boardUI.GridToAnchored(gridPos);

                var image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.color = new Color(color.r, color.g, color.b, 0.45f);
                image.raycastTarget = false;
                ghostImages.Add(image);
            }

            pulseCoroutine = StartCoroutine(PulseGhost());
        }

        public void Hide()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }

            foreach (var image in ghostImages)
            {
                if (image != null)
                {
                    Destroy(image.gameObject);
                }
            }

            ghostImages.Clear();
        }

        private IEnumerator PulseGhost()
        {
            while (true)
            {
                var t = (Mathf.Sin(Time.unscaledTime * 4f) + 1f) * 0.5f;
                var alpha = Mathf.Lerp(0.3f, 0.65f, t);
                foreach (var image in ghostImages)
                {
                    if (image == null)
                    {
                        continue;
                    }

                    var c = image.color;
                    c.a = alpha;
                    image.color = c;
                }

                yield return null;
            }
        }

        private void OnDestroy()
        {
            Hide();
        }
    }
}
