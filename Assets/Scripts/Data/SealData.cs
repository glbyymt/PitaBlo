using UnityEngine;

namespace Pitablock.Data
{
    [CreateAssetMenu(fileName = "NewSealData", menuName = "Pitablock/SealData")]
    public class SealData : ScriptableObject
    {
        public int sealId;
        public string sealTitle;
        public string description;
        public Sprite sealSprite;
    }
}
