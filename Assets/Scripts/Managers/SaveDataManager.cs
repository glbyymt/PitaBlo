using System;
using System.Collections.Generic;
using System.IO;
using Pitablock.Data;
using UnityEngine;

namespace Pitablock.Managers
{
    public class SaveDataManager : MonoBehaviour
    {
        public static SaveDataManager Instance { get; private set; }

        public PlayerData CurrentData { get; private set; }

        private string saveFilePath;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            saveFilePath = Path.Combine(Application.persistentDataPath, "savedata.json");
        }

        public void LoadData()
        {
            if (File.Exists(saveFilePath))
            {
                try
                {
                    var json = File.ReadAllText(saveFilePath);
                    CurrentData = JsonUtility.FromJson<PlayerData>(json) ?? new PlayerData();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"セーブデータの読み込みに失敗: {e.Message}");
                    CurrentData = new PlayerData();
                }
            }
            else
            {
                CurrentData = new PlayerData();
            }

            UpdateConsecutiveDays();
        }

        public void SaveData()
        {
            if (CurrentData == null)
            {
                CurrentData = new PlayerData();
            }

            var json = JsonUtility.ToJson(CurrentData, true);
            File.WriteAllText(saveFilePath, json);
        }

        public void AddPlayTime(int minutes)
        {
            CurrentData.totalPlayTimeMinutes += minutes;
            SaveData();
        }

        public void IncrementClearedStage()
        {
            CurrentData.clearedStageCount++;
            SaveData();
            CheckAnimalUnlocks();
        }

        public void UnlockAnimal(int animalId)
        {
            var list = CurrentData.GetUnlockedAnimals();
            if (!list.Contains(animalId))
            {
                list.Add(animalId);
                CurrentData.SetUnlockedAnimals(list);
                SaveData();
            }
        }

        public void UnlockSeal(int sealId)
        {
            var list = CurrentData.GetUnlockedSeals();
            if (!list.Contains(sealId))
            {
                list.Add(sealId);
                CurrentData.SetUnlockedSeals(list);
                SaveData();
            }
        }

        public bool IsAnimalUnlocked(int animalId)
        {
            return CurrentData.GetUnlockedAnimals().Contains(animalId);
        }

        public bool IsSealUnlocked(int sealId)
        {
            return CurrentData.GetUnlockedSeals().Contains(sealId);
        }

        private void UpdateConsecutiveDays()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            if (string.IsNullOrEmpty(CurrentData.lastPlayDate))
            {
                CurrentData.consecutivePlayDays = 1;
            }
            else if (CurrentData.lastPlayDate != today)
            {
                var last = DateTime.Parse(CurrentData.lastPlayDate);
                var diff = (DateTime.Now.Date - last.Date).Days;
                CurrentData.consecutivePlayDays = diff == 1
                    ? CurrentData.consecutivePlayDays + 1
                    : 1;
            }

            CurrentData.lastPlayDate = today;
            SaveData();
        }

        private void CheckAnimalUnlocks()
        {
            var animals = Resources.LoadAll<AnimalData>("Animals");
            foreach (var animal in animals)
            {
                if (animal != null
                    && CurrentData.clearedStageCount >= animal.unlockRequiredLevel
                    && !IsAnimalUnlocked(animal.animalId))
                {
                    UnlockAnimal(animal.animalId);
                }
            }
        }
    }
}
