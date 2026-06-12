using System;
using System.Collections.Generic;
using Pitablock.Controllers;
using Pitablock.Data;
using UnityEngine;

namespace Pitablock.Managers
{
    /// <summary>
    /// 仕様書に基づくモード別ルール・進行管理。
    /// </summary>
    public class GameModeSession : MonoBehaviour
    {
        public static GameModeSession Instance { get; private set; }

        public GameMode ActiveMode { get; private set; }
        public BeginnerSubMode BeginnerSubMode { get; private set; }
        public string ModeTitle { get; private set; } = string.Empty;
        public string HintMessage { get; private set; } = string.Empty;
        public string GoalMessage { get; private set; } = string.Empty;

        public bool ChangeEnabled { get; private set; } = true;
        public bool HasUnlimitedChange { get; private set; }
        public bool IsLineClearEnabled { get; private set; } = true;
        public bool IsGameOverEnabled { get; private set; } = true;
        public bool ShouldSpawnNextHandBlock { get; private set; } = true;
        public bool UndoEnabled { get; private set; } = true;
        public bool HintEnabled { get; private set; } = true;

        public event Action OnSessionChanged;
        public event Action OnStageCleared;
        public event Action OnTutorialCompleted;
        public event Action OnTutorialStageCleared;
        public event Action OnPuzzleFitCleared;
        public event Action OnAllPuzzleFitCompleted;

        private int puzzleFitIndex;
        private int tutorialStageIndex;
        private int blockRoadStageIndex;
        private int blockRoadLinesCleared;
        private int lastPlacedBlockId;
        private bool tutorialHandConsumed;
        private HashSet<Vector2Int> silhouetteCells = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void BeginSession(GameMode mode, BeginnerSubMode? beginnerSub = null)
        {
            ActiveMode = mode;
            puzzleFitIndex = 0;
            tutorialStageIndex = 0;
            blockRoadLinesCleared = 0;
            tutorialHandConsumed = false;
            lastPlacedBlockId = 0;
            silhouetteCells.Clear();

            switch (mode)
            {
                case GameMode.Beginner:
                    BeginnerSubMode = beginnerSub ?? BeginnerSubMode.PuzzleFit;
                    ApplyBeginnerRules();
                    break;
                case GameMode.BlockRoad:
                    blockRoadStageIndex = SaveDataManager.Instance != null
                        ? SaveDataManager.Instance.CurrentData.clearedStageCount
                        : 0;
                    ApplyBlockRoadStage(blockRoadStageIndex);
                    break;
                case GameMode.Tutorial:
                    ApplyTutorialStage(0);
                    break;
            }

            OnSessionChanged?.Invoke();
        }

        public int GetChangeCount()
        {
            if (HasUnlimitedChange)
            {
                return 999;
            }

            return ActiveMode switch
            {
                GameMode.BlockRoad => 3,
                GameMode.Tutorial => 0,
                _ => 3
            };
        }

        public bool TryGetFixedHandBlockId(out int blockId)
        {
            blockId = 0;

            if (ActiveMode == GameMode.Beginner && BeginnerSubMode == BeginnerSubMode.PuzzleFit)
            {
                if (puzzleFitIndex < GameStageDefinitions.PuzzleFitStages.Length)
                {
                    blockId = GameStageDefinitions.PuzzleFitStages[puzzleFitIndex].BlockId;
                    return true;
                }

                return false;
            }

            if (ActiveMode == GameMode.Tutorial)
            {
                if (tutorialStageIndex < GameStageDefinitions.TutorialStages.Length)
                {
                    blockId = GameStageDefinitions.TutorialStages[tutorialStageIndex].HandBlockId;
                    return true;
                }

                return false;
            }

            return false;
        }

        public IReadOnlyCollection<Vector2Int> GetSilhouetteCells() => silhouetteCells;

        /// <summary>正解位置のみ配置可能なモード（型はめ・おてほん）。</summary>
        public bool UsesAuthoritativePlacement =>
            ActiveMode == GameMode.Tutorial
            || (ActiveMode == GameMode.Beginner && BeginnerSubMode == BeginnerSubMode.PuzzleFit);

        public bool ValidatePlacement(int blockId, Vector2Int[] gridPositions)
        {
            if (gridPositions == null || gridPositions.Length == 0)
            {
                return false;
            }

            if (ActiveMode == GameMode.Beginner && BeginnerSubMode == BeginnerSubMode.PuzzleFit)
            {
                if (puzzleFitIndex >= GameStageDefinitions.PuzzleFitStages.Length)
                {
                    return false;
                }

                var stage = GameStageDefinitions.PuzzleFitStages[puzzleFitIndex];
                if (blockId != stage.BlockId)
                {
                    return false;
                }

                var expected = GameStageDefinitions.GetShapePositions(
                    stage.BlockId,
                    stage.ShapeAnchor,
                    BlockSpawner.Instance?.AvailableBlocks);

                if (expected.Length != gridPositions.Length)
                {
                    return false;
                }

                var placed = new HashSet<Vector2Int>(gridPositions);
                foreach (var cell in expected)
                {
                    if (!placed.Contains(cell))
                    {
                        return false;
                    }
                }

                foreach (var pos in gridPositions)
                {
                    if (!silhouetteCells.Contains(pos))
                    {
                        return false;
                    }
                }

                return true;
            }

            return true;
        }

        public bool TryGetAuthoritativeHint(
            BlockController handBlock,
            out Vector2Int[] gridPositions,
            out Vector2Int shapeAnchor)
        {
            gridPositions = null;
            shapeAnchor = default;

            if (handBlock == null)
            {
                return false;
            }

            if (ActiveMode == GameMode.Beginner && BeginnerSubMode == BeginnerSubMode.PuzzleFit)
            {
                if (puzzleFitIndex >= GameStageDefinitions.PuzzleFitStages.Length)
                {
                    return false;
                }

                var stage = GameStageDefinitions.PuzzleFitStages[puzzleFitIndex];
                shapeAnchor = stage.ShapeAnchor;
                gridPositions = GameStageDefinitions.GetShapePositions(
                    stage.BlockId,
                    stage.ShapeAnchor,
                    BlockSpawner.Instance?.AvailableBlocks);
                return gridPositions.Length > 0;
            }

            if (ActiveMode == GameMode.Tutorial)
            {
                if (tutorialStageIndex >= GameStageDefinitions.TutorialStages.Length)
                {
                    return false;
                }

                var stage = GameStageDefinitions.TutorialStages[tutorialStageIndex];
                if (!stage.SolutionAnchor.HasValue)
                {
                    return false;
                }

                shapeAnchor = stage.SolutionAnchor.Value;
                gridPositions = GameStageDefinitions.GetShapePositions(
                    stage.HandBlockId,
                    shapeAnchor,
                    BlockSpawner.Instance?.AvailableBlocks);
                return gridPositions.Length > 0
                       && GridManager.Instance != null
                       && GridManager.Instance.CanPlaceBlock(gridPositions);
            }

            return false;
        }

        public void ApplyInitialBoard(GridManager grid, Sprite cellSprite, IReadOnlyList<BlockData> blockMaster)
        {
            if (grid == null)
            {
                return;
            }

            var presets = BuildPresetCells();
            if (presets.Count > 0)
            {
                grid.ApplyPresetCells(presets, cellSprite, blockMaster);
            }
        }

        public void NotifyBlockPlaced(int blockId)
        {
            lastPlacedBlockId = blockId;

            if (ActiveMode == GameMode.Beginner && BeginnerSubMode == BeginnerSubMode.PuzzleFit)
            {
                ShouldSpawnNextHandBlock = false;
                if (IsPuzzleSilhouetteFilled())
                {
                    AdvancePuzzleFit();
                    OnPuzzleFitCleared?.Invoke();
                }
            }
            else if (ActiveMode == GameMode.Tutorial)
            {
                tutorialHandConsumed = true;
                ShouldSpawnNextHandBlock = false;
            }
        }

        public void NotifyLinesCleared(int lineCount)
        {
            if (lineCount <= 0)
            {
                return;
            }

            switch (ActiveMode)
            {
                case GameMode.BlockRoad:
                    HandleBlockRoadLineClear(lineCount);
                    break;
                case GameMode.Tutorial:
                    if (!tutorialHandConsumed)
                    {
                        break;
                    }

                    tutorialStageIndex++;
                    if (tutorialStageIndex >= GameStageDefinitions.TutorialStages.Length)
                    {
                        OnTutorialCompleted?.Invoke();
                    }
                    else
                    {
                        OnTutorialStageCleared?.Invoke();
                    }

                    break;
            }
        }

        public void LoadCurrentTutorialStage()
        {
            tutorialHandConsumed = false;
            ApplyTutorialStage(tutorialStageIndex);
            GridManager.Instance?.ResetGrid();
            ApplyInitialBoard(
                GridManager.Instance,
                BlockSpawner.Instance?.FallbackSprite,
                BlockSpawner.Instance?.AvailableBlocks);
            BlockSpawner.Instance?.ResetHandBlockOnly();
            OnSessionChanged?.Invoke();
        }

        public void RestartBlockRoadStage()
        {
            blockRoadLinesCleared = 0;
            blockRoadStageIndex = SaveDataManager.Instance != null
                ? SaveDataManager.Instance.CurrentData.clearedStageCount
                : 0;
            ApplyBlockRoadStage(blockRoadStageIndex);
            OnSessionChanged?.Invoke();
        }

        private void ApplyBeginnerRules()
        {
            if (BeginnerSubMode == BeginnerSubMode.FreeDraw)
            {
                ModeTitle = "はじめて・おえかき";
                HintMessage = "すきな いろで えを かこう！";
                GoalMessage = string.Empty;
                ChangeEnabled = true;
                HasUnlimitedChange = true;
                IsLineClearEnabled = false;
                IsGameOverEnabled = false;
                ShouldSpawnNextHandBlock = true;
                UndoEnabled = true;
                silhouetteCells.Clear();
                return;
            }

            ModeTitle = "はじめて・かたはめ";
            ApplyPuzzleFitStage(puzzleFitIndex);
            ChangeEnabled = false;
            HasUnlimitedChange = false;
            IsLineClearEnabled = false;
            IsGameOverEnabled = false;
            ShouldSpawnNextHandBlock = true;
            UndoEnabled = true;
        }

        private void ApplyPuzzleFitStage(int index)
        {
            if (index >= GameStageDefinitions.PuzzleFitStages.Length)
            {
                GoalMessage = "ぜんぶ できたね！";
                HintMessage = "おえかき あそびも してみよう！";
                silhouetteCells.Clear();
                ShouldSpawnNextHandBlock = false;
                return;
            }

            var stage = GameStageDefinitions.PuzzleFitStages[index];
            GoalMessage = "かたちに おしてね！";
            HintMessage = stage.HintMessage;
            silhouetteCells = new HashSet<Vector2Int>(stage.SilhouetteCells);
        }

        private void AdvancePuzzleFit()
        {
            puzzleFitIndex++;
            if (puzzleFitIndex >= GameStageDefinitions.PuzzleFitStages.Length)
            {
                OnAllPuzzleFitCompleted?.Invoke();
                ApplyPuzzleFitStage(puzzleFitIndex);
                OnSessionChanged?.Invoke();
                return;
            }

            ApplyPuzzleFitStage(puzzleFitIndex);
            GridManager.Instance?.ResetGrid();
            BlockSpawner.Instance?.ResetHandBlockOnly();
            ShouldSpawnNextHandBlock = true;
            OnSessionChanged?.Invoke();
        }

        private bool IsPuzzleSilhouetteFilled()
        {
            if (silhouetteCells.Count == 0)
            {
                return false;
            }

            foreach (var cell in silhouetteCells)
            {
                if (!GridManager.Instance.IsCellOccupied(cell))
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyBlockRoadStage(int stageIndex)
        {
            var stage = GameStageDefinitions.GetBlockRoadStage(stageIndex);
            ModeTitle = "ブロック道";
            GoalMessage = stage.GoalMessage;
            HintMessage = stage.HintMessage;
            ChangeEnabled = true;
            HasUnlimitedChange = false;
            IsLineClearEnabled = true;
            IsGameOverEnabled = true;
            ShouldSpawnNextHandBlock = true;
            UndoEnabled = true;
            blockRoadLinesCleared = 0;
        }

        private void HandleBlockRoadLineClear(int lineCount)
        {
            var stage = GameStageDefinitions.GetBlockRoadStage(blockRoadStageIndex);
            blockRoadLinesCleared += lineCount;

            var missionOk = stage.Mission switch
            {
                BlockRoadMissionType.ClearLines => blockRoadLinesCleared >= stage.TargetLineCount,
                BlockRoadMissionType.ClearWithBlockId => blockRoadLinesCleared >= stage.TargetLineCount
                    && (!stage.RequiredBlockId.HasValue || lastPlacedBlockId == stage.RequiredBlockId.Value),
                _ => false
            };

            if (missionOk)
            {
                SaveDataManager.Instance?.IncrementClearedStage();
                OnStageCleared?.Invoke();
            }
        }

        private void ApplyTutorialStage(int index)
        {
            ModeTitle = "おてほん";
            ChangeEnabled = false;
            HasUnlimitedChange = false;
            IsLineClearEnabled = true;
            IsGameOverEnabled = false;
            ShouldSpawnNextHandBlock = true;
            UndoEnabled = true;
            tutorialHandConsumed = false;

            if (index >= GameStageDefinitions.TutorialStages.Length)
            {
                GoalMessage = "おてほん おわり！";
                HintMessage = string.Empty;
                return;
            }

            var stage = GameStageDefinitions.TutorialStages[index];
            GoalMessage = stage.GoalMessage;
            HintMessage = stage.HintMessage;
        }

        private List<GridManager.PresetCell> BuildPresetCells()
        {
            var presets = new List<GridManager.PresetCell>();

            if (ActiveMode == GameMode.Tutorial && tutorialStageIndex < GameStageDefinitions.TutorialStages.Length)
            {
                var stage = GameStageDefinitions.TutorialStages[tutorialStageIndex];
                if (stage.PresetCells != null)
                {
                    presets.AddRange(stage.PresetCells);
                }

                return presets;
            }

            if (ActiveMode == GameMode.BlockRoad)
            {
                var stage = GameStageDefinitions.GetBlockRoadStage(blockRoadStageIndex);
                if (stage.PresetCells != null)
                {
                    presets.AddRange(stage.PresetCells);
                }
            }

            return presets;
        }
    }
}
