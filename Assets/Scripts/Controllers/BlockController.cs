using System.Collections;
using System.Collections.Generic;
using Pitablock.Data;
using Pitablock.Managers;
using Pitablock.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Pitablock.Controllers
{
    public enum BlockState
    {
        InHand,
        Dragging,
        Placed
    }

    [RequireComponent(typeof(RectTransform))]
    public class BlockController : MonoBehaviour
    {
        [SerializeField] private float dragLiftPixels = 80f;
        [SerializeField] private float snapLerpSpeed = 18f;
        [SerializeField] private float returnDuration = 0.25f;
        [SerializeField] private float placementSnapCells = 1f;

        private readonly List<RectTransform> squareRects = new();
        private readonly List<Vector2Int> localPositions = new();

        private RectTransform rectTransform;
        private RectTransform gridDragParent;
        private Vector2 initialHandAnchoredPosition;
        private Vector2 dragOffset;
        private Vector2 cellVisualOffset;
        private BlockData data;

        public BlockState CurrentState { get; private set; } = BlockState.InHand;
        public BlockData Data => data;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void ConfigureGridDragParent(RectTransform placedCellsParent)
        {
            gridDragParent = placedCellsParent;
        }

        public void Initialize(BlockData blockData, Sprite fallbackSprite)
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            data = blockData;
            CurrentState = BlockState.InHand;

            RebuildVisuals(blockData, fallbackSprite, true);
        }

        public void SetHandAnchoredPosition(Vector2 anchoredPosition)
        {
            initialHandAnchoredPosition = anchoredPosition;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        public void CenterInHandArea()
        {
            SetHandAnchoredPosition(-rectTransform.sizeDelta * 0.5f);
        }

        public void RestoreHandLayoutForSpawn()
        {
            var board = GridManager.Instance?.BoardUI;
            if (board == null)
            {
                return;
            }

            rectTransform.SetParent(board.HandSpawnRect, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = Vector2.zero;
        }

        public void OnDragStart(Vector2 screenPos)
        {
            if (CurrentState != BlockState.InHand || gridDragParent == null)
            {
                return;
            }

            CurrentState = BlockState.Dragging;
            MoveToGridParentPreserveWorld();

            var localPoint = ScreenToParentLocal(screenPos, gridDragParent);
            dragOffset = rectTransform.anchoredPosition - localPoint;
            dragOffset.y += dragLiftPixels;

            rectTransform.SetAsLastSibling();
        }

        public void OnDragging(Vector2 screenPos)
        {
            if (CurrentState != BlockState.Dragging || gridDragParent == null)
            {
                return;
            }

            var localPoint = ScreenToParentLocal(screenPos, gridDragParent);
            var target = localPoint + dragOffset;
            var follow = 1f - Mathf.Exp(-snapLerpSpeed * Time.deltaTime);
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, target, follow);
        }

        public void OnDragEnd(Vector2 screenPos)
        {
            if (CurrentState != BlockState.Dragging || gridDragParent == null)
            {
                return;
            }

            // 補間で指より遅れているため、離した位置に一度スナップしてから判定する
            SnapToReleasePosition(screenPos);

            if (GridManager.Instance != null && TryFindValidPlacement(out var gridPositions))
            {
                PlaceOnGrid(gridPositions);
            }
            else
            {
                CurrentState = BlockState.InHand;
                StartCoroutine(ReturnToHandPosition());
            }
        }

        private void SnapToReleasePosition(Vector2 screenPos)
        {
            var localPoint = ScreenToParentLocal(screenPos, gridDragParent);
            rectTransform.anchoredPosition = localPoint + dragOffset - new Vector2(0f, dragLiftPixels);
        }

        public void RotateBlock()
        {
            if (CurrentState != BlockState.InHand)
            {
                return;
            }

            for (var i = 0; i < localPositions.Count; i++)
            {
                var p = localPositions[i];
                localPositions[i] = new Vector2Int(p.y, -p.x);
            }

            RebuildVisuals(data, data.blockSprite, false);
            CenterInHandArea();
        }

        public bool CanPlaceAnywhere()
        {
            if (GridManager.Instance == null)
            {
                return true;
            }

            for (var rotation = 0; rotation < 4; rotation++)
            {
                for (var x = 0; x < GridManager.GridWidth; x++)
                {
                    for (var y = 0; y < GridManager.GridHeight; y++)
                    {
                        var anchor = new Vector2Int(x, y);
                        if (GridManager.Instance.CanPlaceBlock(GetGridPositionsForAnchor(anchor)))
                        {
                            return true;
                        }
                    }
                }

                RotateBlock();
            }

            return false;
        }

        public void ReturnToHand()
        {
            StopAllCoroutines();
            CurrentState = BlockState.InHand;
            RestoreHandParent();
            CenterInHandArea();
            SetHandRaycastEnabled(true);
            rectTransform.SetAsLastSibling();

            foreach (var cell in squareRects)
            {
                cell.SetParent(rectTransform, false);
            }
        }

        public void SnapToGridPositions(Vector2Int[] gridPositions)
        {
            ApplyGridAnchoredPosition(gridPositions);
        }

        public void SnapToReferenceCellGrid(Vector2Int gridPosOfRefCell)
        {
            var shapeAnchor = gridPosOfRefCell - localPositions[0];
            ApplyGridAnchoredPosition(GetGridPositionsForAnchor(shapeAnchor));
        }

        public Transform GetReferenceCellTransform()
        {
            return squareRects.Count > 0 ? squareRects[0] : null;
        }

        public void NotifyGridCellRemoved(Transform cellTransform)
        {
            PruneDestroyedSquareCells();
            if (this == null)
            {
                return;
            }

            for (var i = squareRects.Count - 1; i >= 0; i--)
            {
                if (squareRects[i] == cellTransform)
                {
                    squareRects.RemoveAt(i);
                    break;
                }
            }

            if (cellTransform != null)
            {
                Destroy(cellTransform.gameObject);
            }

            if (squareRects.Count == 0)
            {
                Destroy(gameObject);
                return;
            }

            RecomputeBlockBounds();
        }

        /// <summary>破棄済みセル参照を除去する。全セルが無くなったブロックは自身も破棄する。</summary>
        public void PruneDestroyedSquareCells()
        {
            for (var i = squareRects.Count - 1; i >= 0; i--)
            {
                if (squareRects[i] == null)
                {
                    squareRects.RemoveAt(i);
                }
            }

            if (squareRects.Count == 0 && this != null)
            {
                Destroy(gameObject);
            }
        }

        private void RecomputeBlockBounds()
        {
            PruneDestroyedSquareCells();
            if (this == null)
            {
                return;
            }

            var cellSize = GetCellSize();
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;

            foreach (var cell in squareRects)
            {
                if (cell == null)
                {
                    continue;
                }

                var meta = cell.GetComponent<BlockCellMeta>();
                if (meta == null)
                {
                    continue;
                }

                minX = Mathf.Min(minX, meta.ShapeOffset.x);
                minY = Mathf.Min(minY, meta.ShapeOffset.y);
                maxX = Mathf.Max(maxX, meta.ShapeOffset.x);
                maxY = Mathf.Max(maxY, meta.ShapeOffset.y);
            }

            if (minX == int.MaxValue)
            {
                return;
            }

            cellVisualOffset = new Vector2(-minX * cellSize, -minY * cellSize);
            rectTransform.sizeDelta = new Vector2(
                (maxX - minX + 1) * cellSize,
                (maxY - minY + 1) * cellSize);

            foreach (var cell in squareRects)
            {
                if (cell == null)
                {
                    continue;
                }

                var meta = cell.GetComponent<BlockCellMeta>();
                if (meta == null)
                {
                    continue;
                }

                cell.anchoredPosition = new Vector2(
                    meta.ShapeOffset.x * cellSize + cellVisualOffset.x,
                    meta.ShapeOffset.y * cellSize + cellVisualOffset.y);
            }
        }

        public Vector2Int ComputeShapeAnchor(Vector2Int[] gridPositions)
        {
            if (gridPositions == null || gridPositions.Length == 0)
            {
                return Vector2Int.zero;
            }

            var refOffset = GetMinShapeOffset();
            for (var i = 0; i < localPositions.Count && i < gridPositions.Length; i++)
            {
                if (localPositions[i] == refOffset)
                {
                    return gridPositions[i] - refOffset;
                }
            }

            return gridPositions[0] - localPositions[0];
        }

        public Transform GetCellTransformAtOffset(Vector2Int shapeOffset)
        {
            PruneDestroyedSquareCells();
            if (this == null)
            {
                return null;
            }

            foreach (var cell in squareRects)
            {
                if (cell == null)
                {
                    continue;
                }

                var meta = cell.GetComponent<BlockCellMeta>();
                if (meta != null && meta.ShapeOffset == shapeOffset)
                {
                    return cell;
                }
            }

            return null;
        }

        public bool TryGetShapeAnchorFromCellMap(
            IReadOnlyDictionary<Vector2Int, Transform> gridCellToTransform,
            out Vector2Int shapeAnchor)
        {
            shapeAnchor = default;
            var found = false;

            PruneDestroyedSquareCells();
            if (this == null)
            {
                return false;
            }

            foreach (var cellRect in squareRects)
            {
                if (cellRect == null)
                {
                    continue;
                }

                var meta = cellRect.GetComponent<BlockCellMeta>();
                if (meta == null)
                {
                    continue;
                }

                foreach (var kv in gridCellToTransform)
                {
                    if (kv.Value != cellRect)
                    {
                        continue;
                    }

                    var anchor = kv.Key - meta.ShapeOffset;
                    if (!found)
                    {
                        shapeAnchor = anchor;
                        found = true;
                    }
                    else if (shapeAnchor != anchor)
                    {
                        return false;
                    }

                    break;
                }
            }

            return found;
        }

        public void SnapFromGridCellMap(IReadOnlyDictionary<Vector2Int, Transform> gridCellToTransform)
        {
            PruneDestroyedSquareCells();
            if (this == null)
            {
                return;
            }

            foreach (var cellRect in squareRects)
            {
                if (cellRect == null)
                {
                    continue;
                }

                var meta = cellRect.GetComponent<BlockCellMeta>();
                if (meta == null)
                {
                    continue;
                }

                foreach (var kv in gridCellToTransform)
                {
                    if (kv.Value != cellRect)
                    {
                        continue;
                    }

                    ApplyGridAnchoredPositionFromAnchor(kv.Key - meta.ShapeOffset);
                    return;
                }
            }
        }

        public IEnumerable<(Vector2Int GridPos, Transform CellTransform)> GetOccupiedGridCells(GameBoardUI board)
        {
            if (board == null)
            {
                yield break;
            }

            var cellSize = board.CellSize;
            var blockBottomLeft = rectTransform.anchoredPosition;

            foreach (var cell in squareRects)
            {
                if (cell == null)
                {
                    continue;
                }

                var meta = cell.GetComponent<BlockCellMeta>();
                if (meta == null)
                {
                    continue;
                }

                var cellBottomLeft = blockBottomLeft + new Vector2(
                    cellVisualOffset.x + meta.ShapeOffset.x * cellSize,
                    cellVisualOffset.y + meta.ShapeOffset.y * cellSize);

                var gridPos = new Vector2Int(
                    Mathf.RoundToInt(cellBottomLeft.x / cellSize),
                    Mathf.RoundToInt(cellBottomLeft.y / cellSize));

                yield return (gridPos, cell);
            }
        }

        private void RebuildVisuals(BlockData blockData, Sprite fallbackSprite, bool rebuildLocalPositions)
        {
            if (rebuildLocalPositions)
            {
                localPositions.Clear();
                foreach (var localPos in blockData.localPositions)
                {
                    localPositions.Add(localPos);
                }
            }

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            squareRects.Clear();

            var cellSize = GetCellSize();
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;

            foreach (var localPos in localPositions)
            {
                minX = Mathf.Min(minX, localPos.x);
                minY = Mathf.Min(minY, localPos.y);
                maxX = Mathf.Max(maxX, localPos.x);
                maxY = Mathf.Max(maxY, localPos.y);
            }

            cellVisualOffset = new Vector2(-minX * cellSize, -minY * cellSize);
            var blockWidth = (maxX - minX + 1) * cellSize;
            var blockHeight = (maxY - minY + 1) * cellSize;

            rectTransform.pivot = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(blockWidth, blockHeight);

            var sprite = blockData.blockSprite != null ? blockData.blockSprite : fallbackSprite;

            foreach (var localPos in localPositions)
            {
                var cellGo = new GameObject($"Cell_{localPos.x}_{localPos.y}", typeof(RectTransform), typeof(Image));
                var cellRect = cellGo.GetComponent<RectTransform>();
                cellRect.SetParent(rectTransform, false);
                cellRect.anchorMin = cellRect.anchorMax = Vector2.zero;
                cellRect.pivot = Vector2.zero;
                cellRect.sizeDelta = new Vector2(cellSize, cellSize);
                cellRect.anchoredPosition = new Vector2(
                    localPos.x * cellSize + cellVisualOffset.x,
                    localPos.y * cellSize + cellVisualOffset.y);

                var image = cellGo.GetComponent<Image>();
                image.sprite = sprite;
                image.color = blockData.blockColor;
                image.type = Image.Type.Simple;
                image.raycastTarget = true;

                var meta = cellGo.AddComponent<BlockCellMeta>();
                meta.ShapeOffset = localPos;

                squareRects.Add(cellRect);
            }
        }

        private Vector2Int[] GetGridPositionsForAnchor(Vector2Int anchor)
        {
            var result = new Vector2Int[localPositions.Count];
            for (var i = 0; i < localPositions.Count; i++)
            {
                result[i] = anchor + localPositions[i];
            }

            return result;
        }

        public Vector2Int[] GetGridPositionsForAnchorPublic(Vector2Int anchor)
        {
            return GetGridPositionsForAnchor(anchor);
        }

        public Vector2Int ComputeShapeAnchorPublic(Vector2Int[] gridPositions)
        {
            return ComputeShapeAnchor(gridPositions);
        }

        private bool IsPlacementAllowedByMode(Vector2Int[] gridPositions)
        {
            var session = GameModeSession.Instance;
            if (session == null)
            {
                return true;
            }

            return session.ValidatePlacement(data.blockId, gridPositions);
        }

        private bool CanPlaceAt(Vector2Int[] gridPositions)
        {
            return GridManager.Instance != null
                && GridManager.Instance.CanPlaceBlock(gridPositions)
                && IsPlacementAllowedByMode(gridPositions);
        }

        private Vector2Int GetMinShapeOffset()
        {
            var minX = int.MaxValue;
            var minY = int.MaxValue;

            foreach (var cell in squareRects)
            {
                if (cell == null)
                {
                    continue;
                }

                var meta = cell.GetComponent<BlockCellMeta>();
                if (meta == null)
                {
                    continue;
                }

                minX = Mathf.Min(minX, meta.ShapeOffset.x);
                minY = Mathf.Min(minY, meta.ShapeOffset.y);
            }

            if (minX == int.MaxValue)
            {
                foreach (var localPos in localPositions)
                {
                    minX = Mathf.Min(minX, localPos.x);
                    minY = Mathf.Min(minY, localPos.y);
                }
            }

            return new Vector2Int(
                minX == int.MaxValue ? 0 : minX,
                minY == int.MaxValue ? 0 : minY);
        }

        private Vector2 GetReferenceCellBottomLeftInParent()
        {
            var cellSize = GetCellSize();
            var refOffset = GetMinShapeOffset();
            return rectTransform.anchoredPosition + new Vector2(
                cellVisualOffset.x + refOffset.x * cellSize,
                cellVisualOffset.y + refOffset.y * cellSize);
        }

        private void ApplyGridAnchoredPosition(Vector2Int[] gridPositions)
        {
            if (gridPositions == null || gridPositions.Length == 0)
            {
                return;
            }

            var refOffset = GetMinShapeOffset();
            Vector2Int refGridPos = gridPositions[0];
            for (var i = 0; i < localPositions.Count && i < gridPositions.Length; i++)
            {
                if (localPositions[i] == refOffset)
                {
                    refGridPos = gridPositions[i];
                    break;
                }
            }

            ApplyGridAnchoredPositionFromAnchor(refGridPos - refOffset);
        }

        private void ApplyGridAnchoredPositionFromAnchor(Vector2Int shapeAnchor)
        {
            var board = GridManager.Instance.BoardUI;
            if (board == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = Vector2.zero;
            rectTransform.SetParent(board.PlacedCellsRect, false);

            var cellSize = board.CellSize;
            var refOffset = GetMinShapeOffset();
            var refGridPos = shapeAnchor + refOffset;
            var refCellBottomLeft = board.GridToAnchored(refGridPos);
            var blockBottomLeft = refCellBottomLeft - new Vector2(
                cellVisualOffset.x + refOffset.x * cellSize,
                cellVisualOffset.y + refOffset.y * cellSize);
            rectTransform.anchoredPosition = blockBottomLeft;
        }

        private void PlaceOnGrid(Vector2Int[] gridPositions)
        {
            CurrentState = BlockState.Placed;
            var shapeAnchor = ComputeShapeAnchor(gridPositions);
            ApplyGridAnchoredPositionFromAnchor(shapeAnchor);
            SetHandRaycastEnabled(false);

            GridManager.Instance.PlaceBlock(gridPositions, data.blockId, this, shapeAnchor);
            BlockSpawner.Instance?.NotifyHandBlockConsumed();
        }

        private bool TryFindValidPlacement(out Vector2Int[] gridPositions)
        {
            gridPositions = null;
            var board = GridManager.Instance.BoardUI;
            if (board == null)
            {
                return false;
            }

            var cellSize = board.CellSize;
            var actualCells = GetActualCellAnchoredPositions();
            var maxDistance = cellSize * placementSnapCells;
            var relaxedDistance = cellSize * (placementSnapCells + 0.5f);
            var session = GameModeSession.Instance;

            if (session != null && session.TryGetAuthoritativeHint(this, out var authPositions, out _))
            {
                if (TryAcceptPlacementAt(actualCells, board, authPositions, maxDistance, out gridPositions))
                {
                    return true;
                }

                if (TryAcceptPlacementAt(actualCells, board, authPositions, relaxedDistance, out gridPositions))
                {
                    return true;
                }

                if (session.UsesAuthoritativePlacement)
                {
                    return false;
                }
            }
            else if (session != null && session.UsesAuthoritativePlacement)
            {
                return false;
            }

            if (TryGetNearestSnappedPlacement(actualCells, board, maxDistance, out gridPositions))
            {
                return true;
            }

            if (TryGetNearestValidPlacement(actualCells, board, maxDistance, out gridPositions))
            {
                return true;
            }

            if (!IsOverlappingGrid(actualCells, board, cellSize))
            {
                return false;
            }

            if (TryGetNearestSnappedPlacement(actualCells, board, relaxedDistance, out gridPositions))
            {
                return true;
            }

            return TryGetNearestValidPlacement(actualCells, board, relaxedDistance, out gridPositions);
        }

        private bool TryAcceptPlacementAt(
            IReadOnlyList<Vector2> actualCells,
            GameBoardUI board,
            Vector2Int[] candidate,
            float maxDistance,
            out Vector2Int[] gridPositions)
        {
            gridPositions = null;
            if (candidate == null || candidate.Length == 0 || !CanPlaceAt(candidate))
            {
                return false;
            }

            if (ComputePlacementError(actualCells, candidate, board) > maxDistance)
            {
                return false;
            }

            gridPositions = candidate;
            return true;
        }

        private static bool IsOverlappingGrid(IReadOnlyList<Vector2> actualCells, GameBoardUI board, float cellSize)
        {
            var gridWidth = GridManager.GridWidth * cellSize;
            var gridHeight = GridManager.GridHeight * cellSize;

            foreach (var cell in actualCells)
            {
                if (cell.x < gridWidth + cellSize * 0.5f
                    && cell.x + cellSize > -cellSize * 0.5f
                    && cell.y < gridHeight + cellSize * 0.5f
                    && cell.y + cellSize > -cellSize * 0.5f)
                {
                    return true;
                }
            }

            return false;
        }

        private List<Vector2> GetActualCellAnchoredPositions()
        {
            var cellSize = GetCellSize();
            var blockBottomLeft = rectTransform.anchoredPosition;
            var positions = new List<Vector2>(localPositions.Count);

            for (var i = 0; i < localPositions.Count; i++)
            {
                var offset = localPositions[i];
                positions.Add(blockBottomLeft + new Vector2(
                    cellVisualOffset.x + offset.x * cellSize,
                    cellVisualOffset.y + offset.y * cellSize));
            }

            return positions;
        }

        private bool TryGetNearestSnappedPlacement(
            IReadOnlyList<Vector2> actualCells,
            GameBoardUI board,
            float maxDistance,
            out Vector2Int[] gridPositions)
        {
            gridPositions = null;
            var cellSize = board.CellSize;
            var refOffset = GetMinShapeOffset();
            var refIndex = 0;
            for (var i = 0; i < localPositions.Count; i++)
            {
                if (localPositions[i] == refOffset)
                {
                    refIndex = i;
                    break;
                }
            }

            var refGrid = new Vector2Int(
                Mathf.RoundToInt(actualCells[refIndex].x / cellSize),
                Mathf.RoundToInt(actualCells[refIndex].y / cellSize));
            var shapeAnchor = refGrid - refOffset;
            var candidate = GetGridPositionsForAnchor(shapeAnchor);
            return TryAcceptPlacementAt(actualCells, board, candidate, maxDistance, out gridPositions);
        }

        private bool TryGetNearestValidPlacement(
            IReadOnlyList<Vector2> actualCells,
            GameBoardUI board,
            float maxDistance,
            out Vector2Int[] gridPositions)
        {
            gridPositions = null;
            var bestError = float.MaxValue;
            Vector2Int[] bestPositions = null;

            for (var x = 0; x < GridManager.GridWidth; x++)
            {
                for (var y = 0; y < GridManager.GridHeight; y++)
                {
                    var candidate = GetGridPositionsForAnchor(new Vector2Int(x, y));
                    if (!CanPlaceAt(candidate))
                    {
                        continue;
                    }

                    var error = ComputePlacementError(actualCells, candidate, board);
                    if (error < bestError)
                    {
                        bestError = error;
                        bestPositions = candidate;
                    }
                }
            }

            if (bestPositions == null || bestError > maxDistance)
            {
                return false;
            }

            gridPositions = bestPositions;
            return true;
        }

        private static float ComputePlacementError(
            IReadOnlyList<Vector2> actualCells,
            Vector2Int[] candidate,
            GameBoardUI board)
        {
            var maxError = 0f;
            for (var i = 0; i < candidate.Length; i++)
            {
                var expected = board.GridToAnchored(candidate[i]);
                var error = Vector2.Distance(actualCells[i], expected);
                maxError = Mathf.Max(maxError, error);
            }

            return maxError;
        }

        private IEnumerator ReturnToHandPosition()
        {
            RestoreHandParent();
            var start = rectTransform.anchoredPosition;
            var elapsed = 0f;

            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / returnDuration);
                rectTransform.anchoredPosition = Vector2.Lerp(start, initialHandAnchoredPosition, t);
                yield return null;
            }

            rectTransform.anchoredPosition = initialHandAnchoredPosition;
            CurrentState = BlockState.InHand;
            SetHandRaycastEnabled(true);
        }

        private void SetHandRaycastEnabled(bool enabled)
        {
            foreach (var cell in squareRects)
            {
                if (cell == null)
                {
                    continue;
                }

                var image = cell.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = enabled;
                }
            }
        }

        private void MoveToGridParentPreserveWorld()
        {
            if (gridDragParent == null)
            {
                return;
            }

            var bottomLeftWorld = rectTransform.TransformPoint(Vector3.zero);
            rectTransform.SetParent(gridDragParent, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = Vector2.zero;
            rectTransform.position = bottomLeftWorld;
        }

        private void RestoreHandParent()
        {
            var board = GridManager.Instance?.BoardUI;
            if (board == null)
            {
                return;
            }

            var bottomLeftWorld = rectTransform.TransformPoint(Vector3.zero);
            rectTransform.SetParent(board.HandSpawnRect, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = Vector2.zero;
            rectTransform.position = bottomLeftWorld;
        }

        private float GetCellSize()
        {
            return GridManager.Instance != null ? GridManager.Instance.CellSize : 100f;
        }

        private static Vector2 ScreenToParentLocal(Vector2 screenPos, RectTransform parent)
        {
            var canvas = parent.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                screenPos,
                camera,
                out var localPoint);

            return localPoint;
        }
    }
}
