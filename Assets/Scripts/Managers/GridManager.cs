using System;
using System.Collections;
using System.Collections.Generic;
using Pitablock.Controllers;
using Pitablock.Data;
using Pitablock.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Pitablock.Managers
{
    public class GridManager : MonoBehaviour
    {
        public const int GridWidth = 8;
        public const int GridHeight = 12;

        public static GridManager Instance { get; private set; }

        [SerializeField] private float cellSize = 100f;
        [SerializeField] private GameBoardUI boardUI;
        [SerializeField] private Transform placedCellsParent;

        private int[,] gridArray = new int[GridWidth, GridHeight];
        private readonly Dictionary<Vector2Int, CellVisual> cellVisuals = new();
        private GridStateSnapshot? pendingUndo;
        private bool canUndoOnce;
        private Coroutine lineClearCoroutine;

        public float CellSize => cellSize;
        public GameBoardUI BoardUI => boardUI;
        public Transform PlacedCellsParent => placedCellsParent;

        public event Action<int> OnLineCleared;
        public event Action OnGridChanged;
        public event Action OnGameOver;

        public readonly struct PresetCell
        {
            public readonly int X;
            public readonly int Y;
            public readonly int BlockId;

            public PresetCell(int x, int y, int blockId)
            {
                X = x;
                Y = y;
                BlockId = blockId;
            }

            public Vector2Int Position => new(X, Y);
        }

        private struct CellVisual
        {
            public Transform Transform;
            public int BlockId;
            public BlockController Block;
        }

        private struct CellSnapshot
        {
            public Vector2Int Position;
            public Transform Transform;
            public int BlockId;
        }

        private struct GridStateSnapshot
        {
            public int[,] GridCopy;
            public List<CellSnapshot> CellSnapshots;
            public BlockController PlacedBlock;
            public BlockController SpawnedHandBlock;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (placedCellsParent == null)
            {
                var parent = new GameObject("PlacedCells");
                parent.transform.SetParent(transform);
                placedCellsParent = parent.transform;
            }
        }

        public void Configure(GameBoardUI board)
        {
            boardUI = board;
            placedCellsParent = board.PlacedCellsRect;
            cellSize = board.CellSize;
        }

        public void ResetGrid()
        {
            gridArray = new int[GridWidth, GridHeight];
            pendingUndo = null;
            canUndoOnce = false;

            if (lineClearCoroutine != null)
            {
                StopCoroutine(lineClearCoroutine);
                lineClearCoroutine = null;
            }

            var destroyedBlocks = new HashSet<BlockController>();
            foreach (var kv in cellVisuals)
            {
                if (kv.Value.Block != null)
                {
                    if (destroyedBlocks.Add(kv.Value.Block))
                    {
                        Destroy(kv.Value.Block.gameObject);
                    }
                }
                else if (kv.Value.Transform != null)
                {
                    Destroy(kv.Value.Transform.gameObject);
                }
            }

            cellVisuals.Clear();
            OnGridChanged?.Invoke();
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return boardUI != null
                ? boardUI.GridToWorld(gridPos)
                : new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0f);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return boardUI != null
                ? boardUI.WorldToGrid(worldPos)
                : new Vector2Int(
                    Mathf.RoundToInt(worldPos.x / cellSize),
                    Mathf.RoundToInt(worldPos.y / cellSize));
        }

        public bool CanPlaceBlock(Vector2Int[] gridPositions)
        {
            if (gridPositions == null || gridPositions.Length == 0)
            {
                return false;
            }

            PruneInvalidCellVisuals();

            var used = new HashSet<Vector2Int>();
            foreach (var pos in gridPositions)
            {
                if (pos.x < 0 || pos.x >= GridWidth || pos.y < 0 || pos.y >= GridHeight)
                {
                    return false;
                }

                if (!used.Add(pos))
                {
                    return false;
                }

                if (cellVisuals.ContainsKey(pos))
                {
                    return false;
                }
            }

            return true;
        }

        public void PlaceBlock(
            Vector2Int[] gridPositions,
            int blockId,
            BlockController blockInstance,
            Vector2Int shapeAnchor)
        {
            SaveUndoSnapshot(blockInstance);
            canUndoOnce = true;

            foreach (var pos in gridPositions)
            {
                var offset = pos - shapeAnchor;
                var cellTransform = blockInstance.GetCellTransformAtOffset(offset);
                if (cellTransform == null)
                {
                    Debug.LogWarning($"Pitablock: セル {pos} に対応する Transform が見つかりません。");
                    continue;
                }

                gridArray[pos.x, pos.y] = blockId;
                cellVisuals[pos] = new CellVisual
                {
                    Transform = cellTransform,
                    BlockId = blockId,
                    Block = blockInstance
                };
            }

            SyncGridArrayFromCellVisuals();
            OnGridChanged?.Invoke();
            GameModeSession.Instance?.NotifyBlockPlaced(blockId);

            if (GameModeSession.Instance == null || GameModeSession.Instance.IsLineClearEnabled)
            {
                if (lineClearCoroutine == null)
                {
                    lineClearCoroutine = StartCoroutine(CheckAndClearLinesCoroutine());
                }
            }
            else
            {
                CheckGameOver();
            }
        }

        public bool IsCellOccupied(Vector2Int pos)
        {
            return cellVisuals.ContainsKey(pos);
        }

        private void SaveUndoSnapshot(BlockController placedBlock)
        {
            var cellSnapshots = new List<CellSnapshot>();
            foreach (var kv in cellVisuals)
            {
                cellSnapshots.Add(new CellSnapshot
                {
                    Position = kv.Key,
                    Transform = kv.Value.Transform,
                    BlockId = kv.Value.BlockId,
                });
            }

            pendingUndo = new GridStateSnapshot
            {
                GridCopy = (int[,])gridArray.Clone(),
                CellSnapshots = cellSnapshots,
                PlacedBlock = placedBlock,
                SpawnedHandBlock = null
            };
        }

        public bool CanUndo => canUndoOnce
            && (GameModeSession.Instance == null || GameModeSession.Instance.UndoEnabled);

        private void InvalidateUndo()
        {
            canUndoOnce = false;
            pendingUndo = null;
        }

        public void UndoLastMove()
        {
            if (!canUndoOnce || pendingUndo == null)
            {
                return;
            }

            var snapshot = pendingUndo.Value;
            pendingUndo = null;
            canUndoOnce = false;

            cellVisuals.Clear();
            gridArray = (int[,])snapshot.GridCopy.Clone();

            foreach (var cell in snapshot.CellSnapshots)
            {
                if (cell.Transform == null)
                {
                    continue;
                }

                cellVisuals[cell.Position] = new CellVisual
                {
                    Transform = cell.Transform,
                    BlockId = cell.BlockId,
                    Block = cell.Transform != null
                        ? cell.Transform.GetComponentInParent<BlockController>()
                        : null
                };
            }

            if (snapshot.SpawnedHandBlock != null)
            {
                Destroy(snapshot.SpawnedHandBlock.gameObject);
                BlockSpawner.Instance?.ClearHandBlockReference();
            }

            if (snapshot.PlacedBlock != null)
            {
                snapshot.PlacedBlock.ReturnToHand();
                BlockSpawner.Instance?.SetHandBlock(snapshot.PlacedBlock);
            }

            SyncGridArrayFromCellVisuals();
            OnGridChanged?.Invoke();
        }

        public void RegisterSpawnedHandBlockForUndo(BlockController handBlock)
        {
            if (pendingUndo == null)
            {
                return;
            }

            var snapshot = pendingUndo.Value;
            snapshot.SpawnedHandBlock = handBlock;
            pendingUndo = snapshot;
        }

        private IEnumerator CheckAndClearLinesCoroutine()
        {
            yield return null;

            while (true)
            {
                SyncGridArrayFromCellVisuals();

                var linesToClear = CollectFullRows();
                if (linesToClear.Count == 0)
                {
                    CheckGameOver();
                    lineClearCoroutine = null;
                    yield break;
                }

                InvalidateUndo();
                OnGridChanged?.Invoke();
                OnLineCleared?.Invoke(linesToClear.Count);

                foreach (var row in linesToClear)
                {
                    ParticleController.Instance?.PlayLineClearEffect(row, GridToWorld(new Vector2Int(GridWidth / 2, row)));
                }

                yield return new WaitForSeconds(0.15f);

                SyncGridArrayFromCellVisuals();
                linesToClear = CollectFullRows();
                if (linesToClear.Count == 0)
                {
                    CheckGameOver();
                    lineClearCoroutine = null;
                    yield break;
                }

                linesToClear.Sort();
                foreach (var row in linesToClear)
                {
                    ClearRow(row);
                }

                ApplyGravity(linesToClear);
                RepositionAllPlacedBlocks();
                ReconcileCellVisualsFromPlacedBlocks();
                SyncGridArrayFromCellVisuals();
                OnGridChanged?.Invoke();

                GameModeSession.Instance?.NotifyLinesCleared(linesToClear.Count);
                TryUnlockSeals(linesToClear.Count);

                yield return null;
            }
        }

        private List<int> CollectFullRows()
        {
            var linesToClear = new List<int>();
            for (var y = 0; y < GridHeight; y++)
            {
                if (IsRowFull(y))
                {
                    linesToClear.Add(y);
                }
            }

            return linesToClear;
        }

        public void ApplyPresetCells(
            IReadOnlyList<PresetCell> presets,
            Sprite cellSprite,
            IReadOnlyList<BlockData> blockMaster)
        {
            if (boardUI == null || presets == null || presets.Count == 0)
            {
                return;
            }

            var colorById = new Dictionary<int, Color>();
            if (blockMaster != null)
            {
                foreach (var data in blockMaster)
                {
                    if (data != null)
                    {
                        colorById[data.blockId] = data.blockColor;
                    }
                }
            }

            var cellSize = boardUI.CellSize;
            var sprite = cellSprite;

            foreach (var preset in presets)
            {
                var pos = preset.Position;
                if (pos.x < 0 || pos.x >= GridWidth || pos.y < 0 || pos.y >= GridHeight)
                {
                    continue;
                }

                if (cellVisuals.ContainsKey(pos))
                {
                    continue;
                }

                var color = colorById.TryGetValue(preset.BlockId, out var c) ? c : Color.white;
                var cellGo = new GameObject($"Preset_{pos.x}_{pos.y}", typeof(RectTransform), typeof(Image));
                var cellRect = cellGo.GetComponent<RectTransform>();
                cellRect.SetParent(boardUI.PlacedCellsRect, false);
                cellRect.anchorMin = Vector2.zero;
                cellRect.anchorMax = Vector2.zero;
                cellRect.pivot = Vector2.zero;
                cellRect.sizeDelta = new Vector2(cellSize, cellSize);
                cellRect.anchoredPosition = boardUI.GridToAnchored(pos);

                var image = cellGo.GetComponent<Image>();
                image.sprite = sprite;
                image.color = color;
                image.type = Image.Type.Simple;
                image.raycastTarget = false;

                gridArray[pos.x, pos.y] = preset.BlockId;
                cellVisuals[pos] = new CellVisual
                {
                    Transform = cellRect,
                    BlockId = preset.BlockId,
                    Block = null
                };
            }

            SyncGridArrayFromCellVisuals();
            OnGridChanged?.Invoke();
        }

        private void ClearRow(int rowY)
        {
            for (var x = 0; x < GridWidth; x++)
            {
                var pos = new Vector2Int(x, rowY);
                if (!cellVisuals.TryGetValue(pos, out var visual))
                {
                    gridArray[x, rowY] = 0;
                    continue;
                }

                if (visual.Transform == null)
                {
                    cellVisuals.Remove(pos);
                    gridArray[x, rowY] = 0;
                    continue;
                }

                if (visual.Block != null)
                {
                    visual.Block.NotifyGridCellRemoved(visual.Transform);
                }
                else
                {
                    Destroy(visual.Transform.gameObject);
                }

                cellVisuals.Remove(pos);
                gridArray[x, rowY] = 0;
            }
        }

        private void ApplyGravity(IReadOnlyList<int> clearedRows)
        {
            if (clearedRows == null || clearedRows.Count == 0)
            {
                return;
            }

            var clearedSet = new HashSet<int>(clearedRows);
            var movedEntries = new List<(Vector2Int OldPos, CellVisual Visual)>(cellVisuals.Count);

            foreach (var kv in cellVisuals)
            {
                movedEntries.Add((kv.Key, kv.Value));
            }

            var newVisuals = new Dictionary<Vector2Int, CellVisual>();

            foreach (var (oldPos, visual) in movedEntries)
            {
                if (visual.Transform == null)
                {
                    continue;
                }

                var drop = CountClearedRowsBelow(oldPos.y, clearedSet);
                var newY = oldPos.y - drop;
                if (newY < 0)
                {
                    if (visual.Block != null)
                    {
                        var block = visual.Block;
                        block.NotifyGridCellRemoved(visual.Transform);
                    }
                    else if (visual.Transform != null)
                    {
                        Destroy(visual.Transform.gameObject);
                    }

                    continue;
                }

                var newPos = new Vector2Int(oldPos.x, newY);
                if (!newVisuals.ContainsKey(newPos))
                {
                    newVisuals[newPos] = visual;
                }
            }

            cellVisuals.Clear();
            foreach (var kv in newVisuals)
            {
                cellVisuals[kv.Key] = kv.Value;
            }
        }

        private bool IsRowFull(int rowY)
        {
            for (var x = 0; x < GridWidth; x++)
            {
                if (!cellVisuals.ContainsKey(new Vector2Int(x, rowY)))
                {
                    return false;
                }
            }

            return true;
        }

        private void SyncGridArrayFromCellVisuals()
        {
            gridArray = new int[GridWidth, GridHeight];
            var staleKeys = new List<Vector2Int>();

            foreach (var kv in cellVisuals)
            {
                if (kv.Value.Transform == null)
                {
                    staleKeys.Add(kv.Key);
                    continue;
                }

                gridArray[kv.Key.x, kv.Key.y] = kv.Value.BlockId;
            }

            foreach (var key in staleKeys)
            {
                cellVisuals.Remove(key);
            }
        }

        private static int CountClearedRowsBelow(int rowY, HashSet<int> clearedRows)
        {
            var count = 0;
            foreach (var clearedRow in clearedRows)
            {
                if (clearedRow < rowY)
                {
                    count++;
                }
            }

            return count;
        }

        private void RepositionAllPlacedBlocks()
        {
            var cellMap = new Dictionary<Vector2Int, Transform>();
            foreach (var kv in cellVisuals)
            {
                if (kv.Value.Transform != null)
                {
                    cellMap[kv.Key] = kv.Value.Transform;
                }
            }

            var processed = new HashSet<BlockController>();
            foreach (var block in CollectPlacedBlocks())
            {
                if (block == null || !processed.Add(block))
                {
                    continue;
                }

                block.SnapFromGridCellMap(cellMap);
            }
        }

        private void ReconcileCellVisualsFromPlacedBlocks()
        {
            if (boardUI == null)
            {
                return;
            }

            var rebuilt = new Dictionary<Vector2Int, CellVisual>();

            foreach (var kv in cellVisuals)
            {
                if (IsPresetCell(kv.Value))
                {
                    rebuilt[kv.Key] = kv.Value;
                }
            }

            foreach (var block in CollectPlacedBlocks())
            {
                if (block.CurrentState != BlockState.Placed || block.Data == null)
                {
                    continue;
                }

                foreach (var (gridPos, cellTransform) in block.GetOccupiedGridCells(boardUI))
                {
                    if (gridPos.x < 0 || gridPos.x >= GridWidth || gridPos.y < 0 || gridPos.y >= GridHeight)
                    {
                        continue;
                    }

                    if (rebuilt.ContainsKey(gridPos))
                    {
                        continue;
                    }

                    rebuilt[gridPos] = new CellVisual
                    {
                        Transform = cellTransform,
                        BlockId = block.Data.blockId,
                        Block = block
                    };
                }
            }

            cellVisuals.Clear();
            foreach (var kv in rebuilt)
            {
                cellVisuals[kv.Key] = kv.Value;
            }
        }

        private HashSet<BlockController> CollectPlacedBlocks()
        {
            var blocks = new HashSet<BlockController>();

            foreach (var kv in cellVisuals)
            {
                TryAddPlacedBlock(blocks, kv.Value.Block);
            }

            if (placedCellsParent != null)
            {
                foreach (var block in placedCellsParent.GetComponentsInChildren<BlockController>(true))
                {
                    TryAddPlacedBlock(blocks, block);
                }
            }

            return blocks;
        }

        private static bool TryAddPlacedBlock(HashSet<BlockController> blocks, BlockController block)
        {
            if (block == null || block.CurrentState != BlockState.Placed)
            {
                return false;
            }

            block.PruneDestroyedSquareCells();
            if (block == null || block.CurrentState != BlockState.Placed)
            {
                return false;
            }

            blocks.Add(block);
            return true;
        }

        private static bool IsPresetCell(CellVisual visual)
        {
            return visual.Transform != null && visual.Block == null;
        }

        private void PruneInvalidCellVisuals()
        {
            var staleKeys = new List<Vector2Int>();

            foreach (var kv in cellVisuals)
            {
                if (kv.Value.Transform == null)
                {
                    staleKeys.Add(kv.Key);
                    continue;
                }

                if (IsPresetCell(kv.Value))
                {
                    continue;
                }

                if (kv.Value.Block == null || kv.Value.Block.CurrentState != BlockState.Placed)
                {
                    staleKeys.Add(kv.Key);
                }
            }

            foreach (var key in staleKeys)
            {
                cellVisuals.Remove(key);
            }

            if (staleKeys.Count > 0)
            {
                SyncGridArrayFromCellVisuals();
            }
        }

        private void TryUnlockSeals(int lineCount)
        {
            if (lineCount >= 1)
            {
                SaveDataManager.Instance?.UnlockSeal(1);
            }

            if (lineCount >= 2)
            {
                SaveDataManager.Instance?.UnlockSeal(2);
            }

            if (lineCount >= 3)
            {
                SaveDataManager.Instance?.UnlockSeal(3);
            }
        }

        private void CheckGameOver()
        {
            if (GameModeSession.Instance != null && !GameModeSession.Instance.IsGameOverEnabled)
            {
                return;
            }

            if (BlockSpawner.Instance == null || !BlockSpawner.Instance.HasHandBlock)
            {
                return;
            }

            var handBlock = BlockSpawner.Instance.CurrentHandBlock;
            if (handBlock == null || handBlock.CurrentState != BlockState.InHand)
            {
                return;
            }

            if (!handBlock.CanPlaceAnywhere())
            {
                OnGameOver?.Invoke();
            }
        }
    }
}
