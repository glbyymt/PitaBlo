using UnityEngine;

namespace Pitablock.Core
{
    /// <summary>
    /// ピタブロのビジュアルテーマ（ポップ＆モダン・子供向け）。
    /// </summary>
    public static class PitaBloTheme
    {
        // 背景・スカイ
        public static readonly Color SkyTop = new(0.52f, 0.78f, 1f, 1f);
        public static readonly Color SkyBottom = new(0.88f, 0.72f, 1f, 1f);
        public static readonly Color BgColor = new(0.75f, 0.9f, 1f, 1f);

        // パネル（半透明グラデーション風）
        public static readonly Color TitlePanel = new(0.55f, 0.78f, 1f, 0.55f);
        public static readonly Color ModePanel = new(0.65f, 0.95f, 0.82f, 0.5f);
        public static readonly Color BeginnerPanel = new(1f, 0.88f, 0.72f, 0.55f);
        public static readonly Color ResultPanel = new(1f, 0.82f, 0.55f, 0.6f);
        public static readonly Color CollectionPanel = new(0.92f, 0.85f, 1f, 0.65f);
        public static readonly Color ReportPanel = new(0.9f, 0.92f, 1f, 0.88f);

        // テキスト
        public static readonly Color TitleText = new(1f, 0.32f, 0.55f, 1f);
        public static readonly Color TitleOutline = new(1f, 1f, 1f, 0.95f);
        public static readonly Color LabelText = new(0.28f, 0.22f, 0.48f, 1f);
        public static readonly Color SubtitleText = new(0.35f, 0.3f, 0.5f, 0.95f);
        public static readonly Color ButtonText = Color.white;
        public static readonly Color ButtonTextOutline = new(0.15f, 0.1f, 0.3f, 0.6f);
        public static readonly Color PraiseText = new(1f, 0.2f, 0.45f, 1f);
        public static readonly Color ModeTitleText = new(0.3f, 0.25f, 0.55f, 1f);
        public static readonly Color HintText = new(0.2f, 0.45f, 0.75f, 1f);
        public static readonly Color GoalText = new(1f, 0.42f, 0.25f, 1f);

        // ボタン
        public static readonly Color StartButton = new(1f, 0.45f, 0.62f, 1f);
        public static readonly Color QuitButton = new(0.95f, 0.55f, 0.45f, 1f);
        public static readonly Color ChangeButton = new(1f, 0.42f, 0.55f, 1f);
        public static readonly Color UndoButton = new(0.45f, 0.72f, 0.95f, 1f);
        public static readonly Color HintButton = new(1f, 0.82f, 0.25f, 1f);
        public static readonly Color BackButton = new(0.82f, 0.78f, 0.95f, 1f);
        public static readonly Color TeacherBody = new(1f, 0.68f, 0.28f, 1f);

        // モード選択
        public static readonly Color ModeBeginner = new(1f, 0.68f, 0.45f, 1f);
        public static readonly Color ModeBlockRoad = new(0.42f, 0.78f, 1f, 1f);
        public static readonly Color ModeOtehon = new(0.62f, 0.92f, 0.48f, 1f);
        public static readonly Color ModePuzzleFit = new(1f, 0.75f, 0.35f, 1f);
        public static readonly Color ModeFreeDraw = new(0.95f, 0.55f, 0.82f, 1f);
        public static readonly Color CollectionTab = new(0.88f, 0.62f, 1f, 1f);
        public static readonly Color SealTab = new(1f, 0.78f, 0.42f, 1f);
        public static readonly Color RetryButton = new(0.45f, 0.92f, 0.68f, 1f);

        // 盤面
        public static readonly Color GridCellA = new(0.92f, 0.96f, 1f, 1f);
        public static readonly Color GridCellB = new(0.88f, 0.92f, 1f, 1f);
        public static readonly Color GridLine = new(0.72f, 0.78f, 0.95f, 1f);
        public static readonly Color GridBorder = new(0.55f, 0.45f, 0.95f, 1f);
        public static readonly Color Silhouette = new(0.45f, 0.55f, 1f, 0.35f);

        // ブロック（鮮やかなキャンディカラー）
        public static readonly Color BlockI = new(0.3f, 0.85f, 1f, 1f);
        public static readonly Color BlockO = new(1f, 0.88f, 0.28f, 1f);
        public static readonly Color BlockT = new(0.82f, 0.42f, 1f, 1f);
        public static readonly Color BlockS = new(0.38f, 0.95f, 0.58f, 1f);
        public static readonly Color BlockZ = new(1f, 0.42f, 0.48f, 1f);
        public static readonly Color BlockJ = new(0.38f, 0.58f, 1f, 1f);
        public static readonly Color BlockL = new(1f, 0.65f, 0.28f, 1f);

        // 装飾バブル
        public static readonly Color[] BubbleColors =
        {
            new(1f, 0.55f, 0.72f, 0.25f),
            new(0.55f, 0.82f, 1f, 0.3f),
            new(1f, 0.85f, 0.45f, 0.28f),
            new(0.72f, 0.55f, 1f, 0.22f),
            new(0.55f, 1f, 0.78f, 0.25f)
        };
    }
}
