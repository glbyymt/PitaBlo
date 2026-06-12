using System;
using Pitablock.Controllers;
using Pitablock.Data;
using UnityEngine;

namespace Pitablock.Managers
{
    public enum GameState
    {
        Title,
        ModeSelect,
        BeginnerSubSelect,
        Playing,
        Paused,
        Result,
        Collection,
        Report
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Title;
        public GameMode CurrentMode { get; private set; } = GameMode.Beginner;

        public event Action<GameState> OnStateChanged;

        private float playSessionStartTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            SaveDataManager.Instance?.LoadData();
            ChangeState(GameState.Title);
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                FlushPlayTime();
                SaveDataManager.Instance?.SaveData();
            }
        }

        private void OnApplicationQuit()
        {
            FlushPlayTime();
            SaveDataManager.Instance?.SaveData();
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == GameState.Playing && newState != GameState.Playing)
            {
                FlushPlayTime();
            }

            if (newState == GameState.Playing && CurrentState != GameState.Playing)
            {
                playSessionStartTime = Time.unscaledTime;
            }

            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        public void StartGame(GameMode mode, BeginnerSubMode? beginnerSub = null)
        {
            CurrentMode = mode;
            GameModeSession.Instance?.BeginSession(mode, beginnerSub);
            ChangeState(GameState.Playing);
            GridManager.Instance?.ResetGrid();
            GameModeSession.Instance?.ApplyInitialBoard(
                GridManager.Instance,
                BlockSpawner.Instance?.FallbackSprite,
                BlockSpawner.Instance?.AvailableBlocks);
            BlockSpawner.Instance?.ResetForNewGame();
        }

        public void FlushPlayTime()
        {
            if (CurrentState != GameState.Playing || SaveDataManager.Instance == null)
            {
                return;
            }

            var elapsedMinutes = Mathf.Max(0, Mathf.FloorToInt((Time.unscaledTime - playSessionStartTime) / 60f));
            if (elapsedMinutes > 0)
            {
                SaveDataManager.Instance.AddPlayTime(elapsedMinutes);
                playSessionStartTime = Time.unscaledTime;
            }
        }
    }
}
