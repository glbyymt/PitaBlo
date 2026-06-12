using Pitablock.Managers;
using UnityEngine;

namespace Pitablock.Core
{
    /// <summary>
    /// 縦画面 9:16 基準で、8×12 の正方形グリッドと操作エリアをレイアウトする。
    /// </summary>
    public static class GameLayoutFitter
    {
        public struct LayoutResult
        {
            public float CellSize;
            public Vector3 GridOriginWorld;
            public Vector3 SpawnPointWorld;
            public Vector3 CameraPosition;
            public float OrthographicSize;
            public float WorldWidth;
            public float WorldHeight;
        }

        private const float GridWidthRatio = 0.84f;
        private const float GridHeightRatio = 0.66f;
        private const float FooterHeightRatio = 0.17f;

        public static LayoutResult Compute(Camera camera)
        {
            var aspect = GetScreenAspect(camera);

            var orthoSize = 9.6f;
            var worldHeight = orthoSize * 2f;
            var worldWidth = worldHeight * aspect;

            var cellSizeByWidth = worldWidth * GridWidthRatio / GridManager.GridWidth;
            var cellSizeByHeight = worldHeight * GridHeightRatio / GridManager.GridHeight;
            var cellSize = Mathf.Min(cellSizeByWidth, cellSizeByHeight);

            var gridWorldWidth = cellSize * GridManager.GridWidth;
            var gridWorldHeight = cellSize * GridManager.GridHeight;

            var gridLeft = (worldWidth - gridWorldWidth) * 0.5f;
            var gridBottom = worldHeight * FooterHeightRatio;

            return new LayoutResult
            {
                CellSize = cellSize,
                GridOriginWorld = new Vector3(gridLeft, gridBottom, 0f),
                SpawnPointWorld = new Vector3(worldWidth * 0.5f, gridBottom * 0.42f, 0f),
                CameraPosition = new Vector3(worldWidth * 0.5f, worldHeight * 0.5f, -10f),
                OrthographicSize = orthoSize,
                WorldWidth = worldWidth,
                WorldHeight = worldHeight
            };
        }

        private static float GetScreenAspect(Camera camera)
        {
            if (Screen.height > 0 && Screen.width > 0)
            {
                return (float)Screen.width / Screen.height;
            }

            if (camera != null && camera.pixelHeight > 0)
            {
                return (float)camera.pixelWidth / camera.pixelHeight;
            }

            return 9f / 16f;
        }
    }
}
