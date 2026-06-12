using System.Collections.Generic;
using Pitablock.Data;
using Pitablock.Managers;
using Pitablock.UI;
using UnityEngine;

namespace Pitablock.Controllers
{
    public class BlockSpawner : MonoBehaviour
    {
        public static BlockSpawner Instance { get; private set; }

        [SerializeField] private BlockController blockPrefab;
        [SerializeField] private List<BlockData> availableBlocks = new();
        [SerializeField] private Sprite fallbackSprite;
        [SerializeField] private int initialChangeCount = 3;

        private GameBoardUI boardUI;
        private BlockController currentHandBlock;

        public int RemainingChangeCount { get; private set; }
        public bool HasHandBlock => currentHandBlock != null;
        public BlockController CurrentHandBlock => currentHandBlock;
        public Sprite FallbackSprite => fallbackSprite;
        public IReadOnlyList<BlockData> AvailableBlocks => availableBlocks;

        public event System.Action<int> OnChangeCountChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Configure(
            BlockController prefab,
            GameBoardUI board,
            List<BlockData> blocks,
            Sprite sprite)
        {
            blockPrefab = prefab;
            boardUI = board;
            availableBlocks = blocks;
            fallbackSprite = sprite;
        }

        public void ResetForNewGame()
        {
            if (currentHandBlock != null)
            {
                Destroy(currentHandBlock.gameObject);
                currentHandBlock = null;
            }

            var session = GameModeSession.Instance;
            RemainingChangeCount = session != null ? session.GetChangeCount() : initialChangeCount;
            OnChangeCountChanged?.Invoke(RemainingChangeCount);
            SpawnHandBlock();
        }

        public void ResetHandBlockOnly()
        {
            DiscardCurrentHandBlock();
            SpawnHandBlock();
        }

        public void SpawnRandomBlock()
        {
            DiscardCurrentHandBlock();
            SpawnHandBlock();
        }

        private void SpawnHandBlock()
        {
            var data = PickHandBlockData();
            if (data == null)
            {
                return;
            }

            CreateHandBlock(data);
        }

        private BlockData PickHandBlockData()
        {
            if (availableBlocks == null || availableBlocks.Count == 0)
            {
                return null;
            }

            var session = GameModeSession.Instance;
            if (session != null && session.TryGetFixedHandBlockId(out var fixedId))
            {
                return FindBlockData(fixedId);
            }

            return availableBlocks[Random.Range(0, availableBlocks.Count)];
        }

        private BlockData FindBlockData(int blockId)
        {
            foreach (var data in availableBlocks)
            {
                if (data != null && data.blockId == blockId)
                {
                    return data;
                }
            }

            return null;
        }

        public void NotifyHandBlockConsumed()
        {
            currentHandBlock = null;
            if (GameModeSession.Instance == null || GameModeSession.Instance.ShouldSpawnNextHandBlock)
            {
                SpawnHandBlock();
            }
        }

        private void CreateHandBlock(BlockData data)
        {
            if (data == null || blockPrefab == null || boardUI == null)
            {
                return;
            }

            currentHandBlock = Instantiate(blockPrefab, boardUI.HandSpawnRect);
            currentHandBlock.ConfigureGridDragParent(boardUI.PlacedCellsRect);
            currentHandBlock.gameObject.SetActive(true);
            currentHandBlock.Initialize(data, fallbackSprite);
            currentHandBlock.RestoreHandLayoutForSpawn();
            currentHandBlock.CenterInHandArea();
            currentHandBlock.transform.SetAsLastSibling();
            GridManager.Instance?.RegisterSpawnedHandBlockForUndo(currentHandBlock);
        }

        private void DiscardCurrentHandBlock()
        {
            if (currentHandBlock == null)
            {
                return;
            }

            if (currentHandBlock.CurrentState == BlockState.InHand)
            {
                Destroy(currentHandBlock.gameObject);
            }

            currentHandBlock = null;
        }

        public void SetHandBlock(BlockController block)
        {
            currentHandBlock = block;
        }

        public void ClearHandBlockReference()
        {
            currentHandBlock = null;
        }

        public void OnChangeButtonPressed()
        {
            if (GameModeSession.Instance != null && !GameModeSession.Instance.ChangeEnabled)
            {
                return;
            }

            if (currentHandBlock == null)
            {
                return;
            }

            var unlimited = GameModeSession.Instance != null && GameModeSession.Instance.HasUnlimitedChange;
            if (!unlimited && RemainingChangeCount <= 0)
            {
                return;
            }

            var spawnWorld = boardUI.HandSpawnRect.position;
            ParticleController.Instance?.PlayMagicEffect(spawnWorld);
            Destroy(currentHandBlock.gameObject);
            currentHandBlock = null;
            if (!unlimited)
            {
                RemainingChangeCount--;
                OnChangeCountChanged?.Invoke(RemainingChangeCount);
            }

            SpawnRandomBlock();
            HintManager.Instance?.NotifyPlayerInteraction();
        }
    }
}
