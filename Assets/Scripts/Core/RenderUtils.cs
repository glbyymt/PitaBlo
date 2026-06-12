using UnityEngine;

namespace Pitablock.Core
{
    public static class RenderUtils
    {
        /// <summary>
        /// URP 2D では SpriteRenderer のデフォルトマテリアル（Lit + Global Light 2D）を使う。
        /// 独自 Material を割り当てると Built-in 用シェーダーになり、スプライトが表示されない。
        /// </summary>
        public static void ApplySpriteMaterial(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.sortingLayerName = "Default";
            renderer.maskInteraction = SpriteMaskInteraction.None;
        }
    }
}
