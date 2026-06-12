using Pitablock.Managers;
using UnityEngine;

namespace Pitablock.Core
{
    /// <summary>
    /// 8×12 の正方形マス盤面（黒枠・グリッド線付き）を描画する。
    /// </summary>
    public class GridBoardVisual : MonoBehaviour
    {
        [SerializeField] private Color fillColor = new(0.78f, 0.88f, 0.98f, 1f);
        [SerializeField] private Color lineColor = new(0.12f, 0.12f, 0.14f, 1f);
        [SerializeField] private Color borderColor = new(0.05f, 0.05f, 0.08f, 1f);

        public void Build(Transform origin, float cellSize)
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            var width = GridManager.GridWidth;
            var height = GridManager.GridHeight;
            var boardWidth = width * cellSize;
            var boardHeight = height * cellSize;
            var bottomLeft = origin.position;

            var pixelsPerCell = 40;
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

            var board = new GameObject("GridBoardSprite", typeof(SpriteRenderer));
            board.transform.SetParent(transform, false);
            board.transform.position = bottomLeft;

            var pixelsPerUnit = pixelsPerCell / cellSize;
            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texWidth, texHeight),
                new Vector2(0f, 0f),
                pixelsPerUnit);

            var renderer = board.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 5;
            RenderUtils.ApplySpriteMaterial(renderer);
        }
    }
}
