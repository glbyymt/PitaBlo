using UnityEngine;

namespace Pitablock.Controllers
{
    /// <summary>
    /// ブロック内の各マスが持つ、形状上の固定オフセット（回転・ライン消去後も不変）。
    /// </summary>
    public class BlockCellMeta : MonoBehaviour
    {
        public Vector2Int ShapeOffset;
    }
}
