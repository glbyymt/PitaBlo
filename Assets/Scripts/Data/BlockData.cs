using UnityEngine;

namespace Pitablock.Data
{
    [CreateAssetMenu(fileName = "NewBlockData", menuName = "Pitablock/BlockData")]
    public class BlockData : ScriptableObject
    {
        [Header("基本情報")]
        public int blockId;
        public string blockName;

        [Header("見た目")]
        public Sprite blockSprite;
        public Color blockColor = Color.white;

        [Header("形状定義")]
        public Vector2Int[] localPositions = new Vector2Int[4];
    }
}
