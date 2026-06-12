using Pitablock.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Pitablock.UI
{
    /// <summary>
    /// 8×12 盤面を Unity UI で描画し、グリッド座標と UI 座標を変換する。
    /// </summary>
    public class GameBoardUI : MonoBehaviour
    {
        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;
        private const float HeaderRatio = 0.17f;
        private const float FooterRatio = 0.17f;
        private const float GridWidthRatio = 0.92f;
        private const float GridHeightRatio = 0.95f;

        [SerializeField] private Color fillColor = new(0.78f, 0.88f, 0.98f, 1f);
        [SerializeField] private Color lineColor = new(0.12f, 0.12f, 0.14f, 1f);
        [SerializeField] private Color borderColor = new(0.05f, 0.05f, 0.08f, 1f);

        private RectTransform gridRect;
        private RectTransform placedCellsRect;
        private RectTransform handSpawnRect;
        private Image gridImage;
        private float cellSize;

        public RectTransform GridRect => gridRect;
        public RectTransform PlacedCellsRect => placedCellsRect;
        public RectTransform HandSpawnRect => handSpawnRect;
        public float CellSize => cellSize;

        public void Build()
        {
            var root = GetComponent<RectTransform>();
            Stretch(root);

            var headerHeight = RefHeight * HeaderRatio;
            var footerHeight = RefHeight * FooterRatio;
            var playHeight = RefHeight - headerHeight - footerHeight;

            var cellByWidth = RefWidth * GridWidthRatio / GridManager.GridWidth;
            var cellByHeight = playHeight * GridHeightRatio / GridManager.GridHeight;
            cellSize = Mathf.Min(cellByWidth, cellByHeight);

            var gridWidth = cellSize * GridManager.GridWidth;
            var gridHeight = cellSize * GridManager.GridHeight;
            var playCenterY = footerHeight + playHeight * 0.5f - RefHeight * 0.5f;

            gridRect = CreateRect("GridArea", root);
            gridRect.anchorMin = gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.sizeDelta = new Vector2(gridWidth, gridHeight);
            gridRect.anchoredPosition = new Vector2(0f, playCenterY);

            gridImage = gridRect.gameObject.AddComponent<Image>();
            gridImage.sprite = CreateGridSprite(cellSize);
            gridImage.type = Image.Type.Simple;
            gridImage.raycastTarget = false;

            placedCellsRect = CreateRect("PlacedCells", gridRect);
            Stretch(placedCellsRect);

            handSpawnRect = CreateRect("HandSpawn", root);
            handSpawnRect.anchorMin = handSpawnRect.anchorMax = new Vector2(0.5f, 0f);
            handSpawnRect.pivot = new Vector2(0.5f, 0.5f);
            handSpawnRect.sizeDelta = new Vector2(gridWidth * 0.5f, cellSize * 2.5f);
            handSpawnRect.anchoredPosition = new Vector2(0f, footerHeight * 0.5f);
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            var local = new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0f);
            return gridRect.TransformPoint(local);
        }

        public Vector2Int WorldToGrid(Vector3 worldPoint)
        {
            var local = gridRect.InverseTransformPoint(worldPoint);
            return new Vector2Int(
                Mathf.RoundToInt(local.x / cellSize),
                Mathf.RoundToInt(local.y / cellSize));
        }

        public Vector2 GridToAnchored(Vector2Int gridPos)
        {
            return new Vector2(gridPos.x * cellSize, gridPos.y * cellSize);
        }

        public Vector2 ScreenToCanvasLocal(Vector2 screenPos, RectTransform targetRect)
        {
            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRect,
                screenPos,
                camera,
                out var localPoint);

            return localPoint;
        }

        public Vector2 GetHandSpawnAnchoredPosition()
        {
            var canvasRoot = transform as RectTransform;
            var world = handSpawnRect.TransformPoint(Vector3.zero);
            return canvasRoot.InverseTransformPoint(world);
        }

        private Sprite CreateGridSprite(float size)
        {
            var width = GridManager.GridWidth;
            var height = GridManager.GridHeight;
            const int pixelsPerCell = 40;
            var texWidth = width * pixelsPerCell;
            var texHeight = height * pixelsPerCell;
            var texture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            const int borderPx = 6;
            const int linePx = 2;

            for (var y = 0; y < texHeight; y++)
            {
                for (var x = 0; x < texWidth; x++)
                {
                    var onOuterBorder = x < borderPx || y < borderPx
                                        || x >= texWidth - borderPx || y >= texHeight - borderPx;
                    var localX = x % pixelsPerCell;
                    var localY = y % pixelsPerCell;
                    var onGridLine = localX < linePx || localY < linePx;

                    Color color;
                    if (onOuterBorder)
                    {
                        color = borderColor;
                    }
                    else if (onGridLine)
                    {
                        color = lineColor;
                    }
                    else
                    {
                        color = fillColor;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();

            var pixelsPerUnit = pixelsPerCell / size;
            return Sprite.Create(
                texture,
                new Rect(0, 0, texWidth, texHeight),
                new Vector2(0f, 0f),
                pixelsPerUnit);
        }

        private static RectTransform CreateRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
