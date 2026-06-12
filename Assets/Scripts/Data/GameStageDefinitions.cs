using System.Collections.Generic;
using Pitablock.Managers;
using UnityEngine;

namespace Pitablock.Data
{
    public readonly struct PuzzleFitStage
    {
        public readonly Vector2Int[] SilhouetteCells;
        public readonly int BlockId;
        public readonly Vector2Int ShapeAnchor;
        public readonly string HintMessage;

        public PuzzleFitStage(Vector2Int[] silhouette, int blockId, Vector2Int anchor, string hint)
        {
            SilhouetteCells = silhouette;
            BlockId = blockId;
            ShapeAnchor = anchor;
            HintMessage = hint;
        }
    }

    public readonly struct TutorialStage
    {
        public readonly GridManager.PresetCell[] PresetCells;
        public readonly int HandBlockId;
        public readonly Vector2Int? SolutionAnchor;
        public readonly string GoalMessage;
        public readonly string HintMessage;

        public TutorialStage(
            GridManager.PresetCell[] presets,
            int handBlockId,
            Vector2Int? solutionAnchor,
            string goal,
            string hint)
        {
            PresetCells = presets;
            HandBlockId = handBlockId;
            SolutionAnchor = solutionAnchor;
            GoalMessage = goal;
            HintMessage = hint;
        }
    }

    public readonly struct BlockRoadStage
    {
        public readonly BlockRoadMissionType Mission;
        public readonly int TargetLineCount;
        public readonly int? RequiredBlockId;
        public readonly GridManager.PresetCell[] PresetCells;
        public readonly string GoalMessage;
        public readonly string HintMessage;

        public BlockRoadStage(
            BlockRoadMissionType mission,
            int targetLines,
            int? requiredBlockId,
            GridManager.PresetCell[] presets,
            string goal,
            string hint)
        {
            Mission = mission;
            TargetLineCount = targetLines;
            RequiredBlockId = requiredBlockId;
            PresetCells = presets;
            GoalMessage = goal;
            HintMessage = hint;
        }
    }

    /// <summary>
    /// 各モードのステージ・課題データ（ランタイム定義）。
    /// </summary>
    public static class GameStageDefinitions
    {
        public static readonly PuzzleFitStage[] PuzzleFitStages =
        {
            new(
                new[] { new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(3, 1), new Vector2Int(4, 1) },
                2,
                new Vector2Int(3, 0),
                "しかくの かたちに おしてね！"),
            new(
                new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(3, 1) },
                7,
                new Vector2Int(2, 0),
                "Lの かたちに おしてね！"),
            new(
                new[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1) },
                1,
                new Vector2Int(1, 1),
                "ながい かたちに おしてね！")
        };

        public static readonly TutorialStage[] TutorialStages =
        {
            new(
                new[]
                {
                    new GridManager.PresetCell(4, 0, 3),
                    new GridManager.PresetCell(5, 0, 3),
                    new GridManager.PresetCell(6, 0, 3),
                    new GridManager.PresetCell(7, 0, 3)
                },
                1,
                new Vector2Int(1, 0),
                "よこを そろえて 1れつ けす",
                "よこながを ひだりから うめよう！"),
            new(
                new[]
                {
                    new GridManager.PresetCell(0, 0, 2),
                    new GridManager.PresetCell(1, 0, 2),
                    new GridManager.PresetCell(4, 0, 4),
                    new GridManager.PresetCell(5, 0, 4),
                    new GridManager.PresetCell(6, 0, 4),
                    new GridManager.PresetCell(7, 0, 4)
                },
                2,
                new Vector2Int(2, 0),
                "きいろい しかくで 1れつ けす",
                "あいている ところに しかくを おこう！"),
            new(
                new[]
                {
                    new GridManager.PresetCell(0, 0, 6),
                    new GridManager.PresetCell(1, 0, 6),
                    new GridManager.PresetCell(5, 0, 5),
                    new GridManager.PresetCell(6, 0, 5),
                    new GridManager.PresetCell(7, 0, 5)
                },
                3,
                new Vector2Int(3, 0),
                "ピンクの ブロックで 1れつ けす",
                "ピンクを まんなかに おこう！")
        };

        private static readonly BlockRoadStage[] BlockRoadStages =
        {
            new(BlockRoadMissionType.ClearLines, 1, null, null,
                "1れつ けしてみよう！",
                "よこを そろえて けそう！"),
            new(BlockRoadMissionType.ClearWithBlockId, 1, 3, null,
                "ピンクのブロックをつかって けしてみよう！",
                "ピンクの ブロックを おこう！"),
            new(BlockRoadMissionType.ClearLines, 2, null, null,
                "2れつ まとめて けそう！",
                "つぎの よこも そろえよう！"),
            new(BlockRoadMissionType.ClearLines, 1,
                null,
                new[]
                {
                    new GridManager.PresetCell(0, 0, 2),
                    new GridManager.PresetCell(1, 0, 2),
                    new GridManager.PresetCell(0, 1, 2),
                    new GridManager.PresetCell(1, 1, 2)
                },
                "1れつ けしてみよう！",
                "うえの すきまを うめよう！"),
            new(BlockRoadMissionType.ClearLines, 3, null, null,
                "3れつ けしてみよう！",
                "どんどん そろえていこう！"),
            new(BlockRoadMissionType.ClearWithBlockId, 1, 3, null,
                "ピンクで けしてみよう！",
                "ピンクの ブロックを つかおう！")
        };

        public static BlockRoadStage GetBlockRoadStage(int stageIndex)
        {
            if (BlockRoadStages.Length == 0)
            {
                return new BlockRoadStage(
                    BlockRoadMissionType.ClearLines, 1, null, null, "1れつ けそう！", string.Empty);
            }

            return BlockRoadStages[stageIndex % BlockRoadStages.Length];
        }

        public static Vector2Int[] GetShapePositions(int blockId, Vector2Int anchor, IReadOnlyList<BlockData> blocks)
        {
            foreach (var data in blocks)
            {
                if (data == null || data.blockId != blockId)
                {
                    continue;
                }

                var result = new Vector2Int[data.localPositions.Length];
                for (var i = 0; i < data.localPositions.Length; i++)
                {
                    result[i] = anchor + data.localPositions[i];
                }

                return result;
            }

            return System.Array.Empty<Vector2Int>();
        }
    }
}
