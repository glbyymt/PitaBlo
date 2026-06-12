using UnityEngine;

namespace Pitablock.Core
{
    public static class SpriteFactory
    {
        /// <summary>
        /// テトリミノの1マス分となる正方形スプライト（角丸・グミ風ハイライト）を生成する。
        /// </summary>
        public static Sprite CreateRoundedSquareSprite(int size, Color color)
        {
            return CreateGummySprite(size, color);
        }

        /// <summary>
        /// グミ風の角丸ブロック（上部ハイライト付き）。
        /// </summary>
        public static Sprite CreateGummySprite(int size, Color color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            var padding = size * 0.05f;
            var cornerRadius = size * 0.18f;
            var left = padding;
            var right = size - padding - 1f;
            var bottom = padding;
            var top = size - padding - 1f;
            var highlight = Color.Lerp(color, Color.white, 0.45f);
            var shadow = Color.Lerp(color, Color.black, 0.12f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    if (!IsInsideRoundedRect(x, y, left, bottom, right, top, cornerRadius))
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    var ny = (y - bottom) / Mathf.Max(1f, top - bottom);
                    var nx = (x - left) / Mathf.Max(1f, right - left);
                    var pixelColor = Color.Lerp(shadow, color, ny);
                    if (ny > 0.55f && nx > 0.15f && nx < 0.7f)
                    {
                        var highlightT = (ny - 0.55f) / 0.45f;
                        pixelColor = Color.Lerp(pixelColor, highlight, highlightT * 0.55f);
                    }

                    texture.SetPixel(x, y, pixelColor);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// ポップなバブルボタン用スプライト（9-slice 対応・光沢付き）。
        /// </summary>
        public static Sprite CreateBubbleButtonSprite(int size, Color baseColor)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            var border = size * 0.22f;
            var cornerRadius = size * 0.28f;
            var left = border * 0.5f;
            var right = size - border * 0.5f - 1f;
            var bottom = border * 0.5f;
            var top = size - border * 0.5f - 1f;
            var highlight = Color.Lerp(baseColor, Color.white, 0.5f);
            var shadow = Color.Lerp(baseColor, new Color(0.2f, 0.1f, 0.35f), 0.25f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    if (!IsInsideRoundedRect(x, y, left, bottom, right, top, cornerRadius))
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    var ny = (y - bottom) / Mathf.Max(1f, top - bottom);
                    var pixelColor = Color.Lerp(shadow, baseColor, ny * 0.85f + 0.15f);
                    if (ny > 0.6f)
                    {
                        var t = (ny - 0.6f) / 0.4f;
                        var nx = (x - left) / Mathf.Max(1f, right - left);
                        if (nx > 0.2f && nx < 0.75f)
                        {
                            pixelColor = Color.Lerp(pixelColor, highlight, t * 0.65f);
                        }
                    }

                    texture.SetPixel(x, y, pixelColor);
                }
            }

            texture.Apply();
            var sliceBorder = border;
            return Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect,
                new Vector4(sliceBorder, sliceBorder, sliceBorder, sliceBorder));
        }

        /// <summary>
        /// 装飾用の半透明バブル円。
        /// </summary>
        public static Sprite CreateSoftBubbleSprite(int size, Color color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var center = size * 0.5f;
            var radius = size * 0.46f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist > radius)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    var edge = 1f - dist / radius;
                    var highlight = dist < radius * 0.35f && x < center && y > center;
                    var c = color;
                    c.a *= edge * (highlight ? 1.15f : 1f);
                    texture.SetPixel(x, y, c);
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
            texture.filterMode = FilterMode.Bilinear;
            var center = size * 0.5f;
            var radius = size * 0.45f;
            var highlight = Color.Lerp(color, Color.white, 0.4f);
            var shadow = Color.Lerp(color, new Color(0.3f, 0.15f, 0.05f), 0.15f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist > radius)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    var ny = (y - (center - radius)) / (radius * 2f);
                    var pixelColor = Color.Lerp(shadow, color, ny * 0.7f + 0.3f);
                    if (dist < radius * 0.4f && x < center && y > center)
                    {
                        pixelColor = Color.Lerp(pixelColor, highlight, 0.5f);
                    }

                    texture.SetPixel(x, y, pixelColor);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
