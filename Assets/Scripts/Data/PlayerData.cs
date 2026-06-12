using System;
using System.Collections.Generic;

namespace Pitablock.Data
{
    [Serializable]
    public class PlayerData
    {
        public int totalPlayTimeMinutes;
        public int clearedStageCount;
        public string lastPlayDate = "";
        public int consecutivePlayDays;
        public int[] unlockedAnimalIds = Array.Empty<int>();
        public int[] unlockedSealIds = Array.Empty<int>();

        public List<int> GetUnlockedAnimals()
        {
            return unlockedAnimalIds == null
                ? new List<int>()
                : new List<int>(unlockedAnimalIds);
        }

        public List<int> GetUnlockedSeals()
        {
            return unlockedSealIds == null
                ? new List<int>()
                : new List<int>(unlockedSealIds);
        }

        public void SetUnlockedAnimals(List<int> ids)
        {
            unlockedAnimalIds = ids?.ToArray() ?? Array.Empty<int>();
        }

        public void SetUnlockedSeals(List<int> ids)
        {
            unlockedSealIds = ids?.ToArray() ?? Array.Empty<int>();
        }
    }
}
