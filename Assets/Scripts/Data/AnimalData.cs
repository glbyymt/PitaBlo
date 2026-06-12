using UnityEngine;

namespace Pitablock.Data
{
    [CreateAssetMenu(fileName = "NewAnimalData", menuName = "Pitablock/AnimalData")]
    public class AnimalData : ScriptableObject
    {
        public int animalId;
        public string animalName;
        public Sprite animalSprite;
        public AudioClip voiceClip;
        public int unlockRequiredLevel;
    }
}
