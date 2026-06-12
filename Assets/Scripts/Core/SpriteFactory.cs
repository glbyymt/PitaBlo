using UnityEngine;

namespace Pitablock.Core
{
    public static class SpriteFactory
    {
        /// <summary>
        /// テトリミノの1マス分となる正方形スプライト（角丸）を生成する。
        /// </summary>
        public static Sprite CreateRoundedSquareSprite(int size, Color color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            var padding = size * 0.06f;
            var cornerRadius = size * 0.14f;
            var left = padding;
            var right = size - padding - 1f;
            var bottom = padding;
            var top = size - padding - 1f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var inside = IsInsideRoundedRect(x, y, left, bottom, right, top, cornerRadius);
                    texture.SetPixel(x, y, inside ? color : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static bool IsInsideRoundedRect(
            float x, float y,
            float left, float bottom, float right, float top,
            float radius)
        {
            if (x < left || x > right || y < bottom || y > top)
            {
                return false;
            }

            if (x < left + radius && y < bottom + radius)
            {
                return Vector2.Distance(new Vector2(x, y), new Vector2(left + radius, bottom + radius)) <= radius;
            }

            if (x > right - radius && y < bottom + radius)
            {
                return Vector2.Distance(new Vector2(x, y), new Vector2(right - radius, bottom + radius)) <= radius;
            }

            if (x < left + radius && y > top - radius)
            {
                return Vector2.Distance(new Vector2(x, y), new Vector2(left + radius, top - radius)) <= radius;
            }

            if (x > right - radius && y > top - radius)
            {
                return Vector2.Distance(new Vector2(x, y), new Vector2(right - radius, top - radius)) <= radius;
            }

            return true;
        }

        public static Sprite CreateCircleSprite(int size, Color color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = size * 0.5f;
            var radius = size * 0.45f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    texture.SetPixel(x, y, dist <= radius ? color : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
