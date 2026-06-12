using System.Collections.Generic;
using Pitablock.Controllers;
using Pitablock.Data;
using Pitablock.Managers;
using Pitablock.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pitablock.Core
{
    public class PitaBloGameSetup : MonoBehaviour
    {
        private Sprite blockSprite;
        private List<BlockData> runtimeBlockData = new();

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;

            blockSprite = SpriteFactory.CreateRoundedSquareSprite(64, Color.white);

            SetupCamera();
            var managers = SetupManagers();
            SetupUI(managers);
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = PitaBloTheme.BgColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        private GameObject SetupManagers()
        {
            var root = new GameObject("Managers");
            root.AddComponent<GameManager>();
            root.AddComponent<SaveDataManager>();
            root.AddComponent<GridManager>();
            root.AddComponent<InputManager>();
            root.AddComponent<ParticleController>();
            root.AddComponent<BlockSpawner>();
            root.AddComponent<GameModeSession>();
            root.AddComponent<HintManager>();
            root.AddComponent<UIManager>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            return root;
        }

        private BlockController CreateBlockPrefab()
        {
            var prefabGo = new GameObject("BlockPrefab", typeof(RectTransform));
            prefabGo.AddComponent<BlockController>();
            prefabGo.SetActive(false);
            prefabGo.transform.SetParent(transform);
            return prefabGo.GetComponent<BlockController>();
        }

        private List<BlockData> CreateBlockMasterData()
        {
            // 標準テトリミノ7種（各4マス・グリッド整数座標）
            runtimeBlockData = new List<BlockData>
            {
                // I: 横一列4マス
                CreateBlock(1, "I", PitaBloTheme.BlockI, new Vector2Int[]
                {
                    new(-1, 0), new(0, 0), new(1, 0), new(2, 0)
                }),
                // O: 2×2正方形
                CreateBlock(2, "O", PitaBloTheme.BlockO, new Vector2Int[]
                {
                    new(0, 0), new(1, 0), new(0, 1), new(1, 1)
                }),
                // T: 上向きT字
                CreateBlock(3, "T", PitaBloTheme.BlockT, new Vector2Int[]
                {
                    new(-1, 0), new(0, 0), new(1, 0), new(0, 1)
                }),
                // S: S字
                CreateBlock(4, "S", PitaBloTheme.BlockS, new Vector2Int[]
                {
                    new(-1, 1), new(0, 1), new(0, 0), new(1, 0)
                }),
                // Z: Z字
                CreateBlock(5, "Z", PitaBloTheme.BlockZ, new Vector2Int[]
                {
                    new(-1, 0), new(0, 0), new(0, 1), new(1, 1)
                }),
                // J: 左下に1マス
                CreateBlock(6, "J", PitaBloTheme.BlockJ, new Vector2Int[]
                {
                    new(-1, 0), new(0, 0), new(1, 0), new(-1, 1)
                }),
                // L: 右下に1マス
                CreateBlock(7, "L", PitaBloTheme.BlockL, new Vector2Int[]
                {
                    new(-1, 0), new(0, 0), new(1, 0), new(1, 1)
                })
            };

            return runtimeBlockData;
        }

        private BlockData CreateBlock(int id, string name, Color color, Vector2Int[] shape)
        {
            var data = ScriptableObject.CreateInstance<BlockData>();
            data.blockId = id;
            data.blockName = name;
            data.blockColor = color;
            data.blockSprite = blockSprite;
            data.localPositions = shape;
            return data;
        }

        private void SetupUI(GameObject managers)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var titlePanel = CreatePanel(canvasGo.transform, "TitlePanel", PitaBloTheme.TitlePanel);
            AddDecorativeBubbles(titlePanel.transform);
            CreateTitleUI(titlePanel);

            var modePanel = CreatePanel(canvasGo.transform, "ModeSelectPanel", PitaBloTheme.ModePanel);
            AddDecorativeBubbles(modePanel.transform);
            CreateModeSelectUI(modePanel);

            var beginnerSubPanel = CreatePanel(canvasGo.transform, "BeginnerSubPanel", PitaBloTheme.BeginnerPanel);
            AddDecorativeBubbles(beginnerSubPanel.transform);
            CreateBeginnerSubSelectUI(beginnerSubPanel);

            var inGamePanel = CreatePanel(canvasGo.transform, "InGamePanel", new Color(PitaBloTheme.BgColor.r, PitaBloTheme.BgColor.g, PitaBloTheme.BgColor.b, 0f));
            inGamePanel.GetComponent<Image>().raycastTarget = false;
            var gameBoard = inGamePanel.AddComponent<GameBoardUI>();
            gameBoard.Build();
            GridManager.Instance.Configure(gameBoard);
            managers.GetComponent<BlockSpawner>().Configure(
                CreateBlockPrefab(),
                gameBoard,
                CreateBlockMasterData(),
                blockSprite);

            var silhouetteUI = inGamePanel.AddComponent<PuzzleSilhouetteUI>();
            silhouetteUI.Configure(gameBoard);
            var hintGhostUI = inGamePanel.AddComponent<HintGhostUI>();
            hintGhostUI.Configure(gameBoard);
            managers.GetComponent<HintManager>().Configure(hintGhostUI);

            var inGameUi = CreateInGameUI(inGamePanel);

            var resultPanel = CreatePanel(canvasGo.transform, "ResultPanel", PitaBloTheme.ResultPanel);
            AddDecorativeBubbles(resultPanel.transform);
            var resultMessage = CreateResultUI(resultPanel);

            var collectionPanel = CreatePanel(canvasGo.transform, "CollectionPanel", PitaBloTheme.CollectionPanel);
            var collectionController = CreateCollectionUI(collectionPanel);

            var reportPanel = CreatePanel(canvasGo.transform, "ReportPanel", PitaBloTheme.ReportPanel);
            var reportController = CreateReportUI(reportPanel);

            UIManager.Instance.BindPanels(titlePanel, modePanel, beginnerSubPanel, inGamePanel, resultPanel, collectionPanel, reportPanel);
            UIManager.Instance.BindSilhouetteUI(silhouetteUI);
            UIManager.Instance.BindInGameUI(
                inGameUi.change,
                inGameUi.undo,
                inGameUi.pause,
                inGameUi.hint,
                inGameUi.changeText,
                inGameUi.praise,
                inGameUi.teacher,
                inGameUi.modeTitle,
                inGameUi.hintText,
                inGameUi.goal);
            UIManager.Instance.BindResultUI(resultMessage);
            UIManager.Instance.BindControllers(collectionController, reportController);
            UIManager.Instance.InitializeUI();
        }

        private GameObject CreatePanel(Transform parent, string name, Color bgColor)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            Stretch(rect);
            var image = panel.GetComponent<Image>();
            image.color = bgColor;
            panel.SetActive(false);
            return panel;
        }

        private void CreateTitleUI(GameObject panel)
        {
            var title = CreateLabel(panel.transform, "ピタブロ！", 128, new Vector2(0f, 380f), PitaBloTheme.TitleText, true);
            ApplyTextOutline(title, PitaBloTheme.TitleOutline, new Vector2(3f, -3f));

            var startBtn = CreateIconButton(panel.transform, "スタート", new Vector2(0f, -80f), new Vector2(340f, 340f), PitaBloTheme.StartButton);
            startBtn.onClick.AddListener(() => UIManager.Instance.OnStartButtonClicked());

            var parentBtn = CreateIconButton(panel.transform, "🔒", new Vector2(420f, 820f), new Vector2(120f, 120f), PitaBloTheme.BackButton);
            parentBtn.gameObject.AddComponent<ParentGateButton>();
        }

        private void CreateModeSelectUI(GameObject panel)
        {
            var modeTitle = CreateLabel(panel.transform, "あそびかた", 84, new Vector2(0f, 700f), PitaBloTheme.TitleText, true);
            ApplyTextOutline(modeTitle, PitaBloTheme.TitleOutline, new Vector2(2f, -2f));

            CreateModeButton(panel.transform, "はじめて", 0, new Vector2(0f, 300f), PitaBloTheme.ModeBeginner, "はじめての あそび", true);
            CreateModeButton(panel.transform, "ブロック道", 1, new Vector2(0f, 0f), PitaBloTheme.ModeBlockRoad, "もくひょうを たっせい！", false);
            CreateModeButton(panel.transform, "おてほん", 2, new Vector2(0f, -300f), PitaBloTheme.ModeOtehon, "れんしゅう もんだい", false);

            var collectionBtn = CreateIconButton(panel.transform, "ずかん", new Vector2(-250f, -700f), new Vector2(220f, 220f), PitaBloTheme.CollectionTab);
            collectionBtn.onClick.AddListener(() => UIManager.Instance.OnCollectionButtonClicked());

            var backBtn = CreateIconButton(panel.transform, "もどる", new Vector2(250f, -700f), new Vector2(220f, 220f), PitaBloTheme.BackButton);
            backBtn.onClick.AddListener(() => UIManager.Instance.OnBackToTitle());
        }

        private void CreateBeginnerSubSelectUI(GameObject panel)
        {
            var subTitle = CreateLabel(panel.transform, "はじめて あそび", 76, new Vector2(0f, 650f), PitaBloTheme.TitleText, true);
            ApplyTextOutline(subTitle, PitaBloTheme.TitleOutline, new Vector2(2f, -2f));
            CreateModeButton(panel.transform, "かたはめ", 0, new Vector2(0f, 150f), PitaBloTheme.ModePuzzleFit, "かたちに おす", false, true);
            CreateModeButton(panel.transform, "おえかき", 1, new Vector2(0f, -150f), PitaBloTheme.ModeFreeDraw, "じゆうに かく", false, true);
            var back = CreateIconButton(panel.transform, "もどる", new Vector2(0f, -650f), new Vector2(280f, 120f), PitaBloTheme.BackButton);
            back.onClick.AddListener(() => UIManager.Instance.OnBackToModeSelect());
            panel.SetActive(false);
        }

        private void CreateModeButton(
            Transform parent,
            string label,
            int mode,
            Vector2 pos,
            Color color,
            string subtitle,
            bool openBeginnerSub = false,
            bool isBeginnerSubMode = false)
        {
            var btn = CreateIconButton(parent, label, pos, new Vector2(500f, 180f), color);
            if (openBeginnerSub)
            {
                btn.onClick.AddListener(() => UIManager.Instance.OnBeginnerModeSelected());
            }
            else if (isBeginnerSubMode)
            {
                var subMode = mode == 0 ? BeginnerSubMode.PuzzleFit : BeginnerSubMode.FreeDraw;
                btn.onClick.AddListener(() => UIManager.Instance.OnBeginnerSubModeSelected(subMode));
            }
            else
            {
                btn.onClick.AddListener(() => UIManager.Instance.OnModeSelected(mode));
            }

            var subtitleGo = new GameObject("Subtitle", typeof(RectTransform), typeof(Text));
            subtitleGo.transform.SetParent(btn.transform, false);
            var subtitleRect = subtitleGo.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 0f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0f);
            subtitleRect.pivot = new Vector2(0.5f, 0f);
            subtitleRect.anchoredPosition = new Vector2(0f, 18f);
            subtitleRect.sizeDelta = new Vector2(460f, 40f);
            var subtitleText = subtitleGo.GetComponent<Text>();
            subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subtitleText.fontSize = 28;
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = PitaBloTheme.SubtitleText;
            subtitleText.fontStyle = FontStyle.Bold;
            subtitleText.text = subtitle;
        }

        private (Button change, Button undo, Button pause, Button hint, Text changeText, Text praise, Image teacher, Text modeTitle, Text hintText, Text goal) CreateInGameUI(GameObject panel)
        {
            // 左上: やめる
            var pauseBtn = CreateAnchoredButton(
                panel.transform,
                "やめる",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(100f, -100f),
                new Vector2(200f, 200f),
                PitaBloTheme.QuitButton,
                52);

            // 上部中央: どうぶつ先生
            var teacherGo = new GameObject("Teacher", typeof(RectTransform), typeof(Image));
            teacherGo.transform.SetParent(panel.transform, false);
            var teacherRect = teacherGo.GetComponent<RectTransform>();
            teacherRect.anchorMin = new Vector2(0.5f, 1f);
            teacherRect.anchorMax = new Vector2(0.5f, 1f);
            teacherRect.pivot = new Vector2(0.5f, 1f);
            teacherRect.anchoredPosition = new Vector2(0f, -220f);
            teacherRect.sizeDelta = new Vector2(220f, 220f);
            var teacherImage = teacherGo.GetComponent<Image>();
            teacherImage.sprite = SpriteFactory.CreateCircleSprite(128, PitaBloTheme.TeacherBody);
            teacherImage.color = Color.white;

            var teacherFaceGo = new GameObject("TeacherFace", typeof(RectTransform), typeof(Text));
            teacherFaceGo.transform.SetParent(teacherGo.transform, false);
            var teacherFaceRect = teacherFaceGo.GetComponent<RectTransform>();
            Stretch(teacherFaceRect);
            var teacherFace = teacherFaceGo.GetComponent<Text>();
            teacherFace.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            teacherFace.fontSize = 110;
            teacherFace.alignment = TextAnchor.MiddleCenter;
            teacherFace.color = Color.white;
            teacherFace.text = "🦊";
            teacherFace.raycastTarget = false;

            var praiseGo = new GameObject("PraiseText", typeof(RectTransform), typeof(Text));
            praiseGo.transform.SetParent(panel.transform, false);
            var praiseRect = praiseGo.GetComponent<RectTransform>();
            praiseRect.anchorMin = new Vector2(0.5f, 1f);
            praiseRect.anchorMax = new Vector2(0.5f, 1f);
            praiseRect.pivot = new Vector2(0.5f, 1f);
            praiseRect.anchoredPosition = new Vector2(0f, -460f);
            praiseRect.sizeDelta = new Vector2(700f, 100f);
            var praiseText = praiseGo.GetComponent<Text>();
            praiseText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            praiseText.fontSize = 64;
            praiseText.alignment = TextAnchor.MiddleCenter;
            praiseText.color = PitaBloTheme.PraiseText;
            praiseText.fontStyle = FontStyle.Bold;
            ApplyTextOutline(praiseText, PitaBloTheme.TitleOutline, new Vector2(2f, -2f));
            praiseGo.SetActive(false);

            var modeTitleText = CreateAnchoredLabel(panel.transform, "ModeTitle", 46, PitaBloTheme.ModeTitleText);
            modeTitleText.fontStyle = FontStyle.Bold;
            var modeTitleRect = modeTitleText.GetComponent<RectTransform>();
            modeTitleRect.anchorMin = new Vector2(0.5f, 1f);
            modeTitleRect.anchorMax = new Vector2(0.5f, 1f);
            modeTitleRect.pivot = new Vector2(0.5f, 1f);
            modeTitleRect.anchoredPosition = new Vector2(0f, -120f);
            modeTitleRect.sizeDelta = new Vector2(700f, 60f);

            var hintText = CreateAnchoredLabel(panel.transform, "ModeHint", 40, PitaBloTheme.HintText);
            hintText.fontStyle = FontStyle.Bold;
            var hintRect = hintText.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 360f);
            hintRect.sizeDelta = new Vector2(900f, 56f);

            var goalText = CreateAnchoredLabel(panel.transform, "ModeGoal", 36, PitaBloTheme.GoalText);
            goalText.fontStyle = FontStyle.Bold;
            var goalRect = goalText.GetComponent<RectTransform>();
            goalRect.anchorMin = new Vector2(0.5f, 0f);
            goalRect.anchorMax = new Vector2(0.5f, 0f);
            goalRect.pivot = new Vector2(0.5f, 0f);
            goalRect.anchoredPosition = new Vector2(0f, 300f);
            goalRect.sizeDelta = new Vector2(900f, 50f);

            // 左下: チェンジ（×3）
            var changeBtn = CreateAnchoredButton(
                panel.transform,
                "×3",
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(120f, 120f),
                new Vector2(200f, 200f),
                PitaBloTheme.ChangeButton,
                64);
            var changeText = changeBtn.GetComponentInChildren<Text>();

            // 右下: もどす
            var undoBtn = CreateAnchoredButton(
                panel.transform,
                "もどす",
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-120f, 120f),
                new Vector2(200f, 200f),
                PitaBloTheme.UndoButton,
                48);

            // 右端中央: おしえて
            var hintBtn = CreateAnchoredButton(
                panel.transform,
                "？",
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-100f, 0f),
                new Vector2(160f, 160f),
                PitaBloTheme.HintButton,
                72);

            panel.SetActive(false);
            return (changeBtn, undoBtn, pauseBtn, hintBtn, changeText, praiseText, teacherImage, modeTitleText, hintText, goalText);
        }

        private Text CreateAnchoredLabel(Transform parent, string name, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = string.Empty;
            return text;
        }

        private Text CreateResultUI(GameObject panel)
        {
            var message = CreateLabel(panel.transform, "がんばったね！", 96, new Vector2(0f, 200f), PitaBloTheme.TitleText, true);
            ApplyTextOutline(message, PitaBloTheme.TitleOutline, new Vector2(2f, -2f));
            var retry = CreateIconButton(panel.transform, "もういちど", new Vector2(0f, -200f), new Vector2(400f, 200f), PitaBloTheme.RetryButton);
            retry.onClick.AddListener(() => UIManager.Instance.OnResultContinue());
            return message;
        }

        private CollectionUIController CreateCollectionUI(GameObject panel)
        {
            var controller = panel.AddComponent<CollectionUIController>();

            var animalsTab = CreateIconButton(panel.transform, "どうぶつ", new Vector2(-200f, 750f), new Vector2(280f, 120f), PitaBloTheme.CollectionTab);
            var sealsTab = CreateIconButton(panel.transform, "シール", new Vector2(200f, 750f), new Vector2(280f, 120f), PitaBloTheme.SealTab);
            var back = CreateIconButton(panel.transform, "もどる", new Vector2(0f, -820f), new Vector2(220f, 120f), PitaBloTheme.BackButton);

            var animalsContent = CreateScrollArea(panel.transform, "AnimalsContent", new Vector2(0f, -50f));
            var sealsContent = CreateScrollArea(panel.transform, "SealsContent", new Vector2(0f, -50f));
            sealsContent.SetActive(false);

            var animals = CreateAnimalMasterData();
            var seals = CreateSealMasterData();

            controller.Configure(
                animalsTab,
                sealsTab,
                animalsContent,
                sealsContent,
                animalsContent.transform.Find("Viewport/Content"),
                sealsContent.transform.Find("Viewport/Content"),
                null,
                animals,
                seals,
                back);

            return controller;
        }

        private ReportUIController CreateReportUI(GameObject panel)
        {
            var controller = panel.AddComponent<ReportUIController>();

            var playTime = CreateLabel(panel.transform, "", 42, new Vector2(0f, 400f));
            var cleared = CreateLabel(panel.transform, "", 42, new Vector2(0f, 250f));
            var consecutive = CreateLabel(panel.transform, "", 42, new Vector2(0f, 100f));

            var sliderGo = new GameObject("Progress", typeof(RectTransform), typeof(Slider), typeof(Image));
            sliderGo.transform.SetParent(panel.transform, false);
            var sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.anchoredPosition = new Vector2(0f, -100f);
            sliderRect.sizeDelta = new Vector2(700f, 40f);
            var slider = sliderGo.GetComponent<Slider>();

            var back = CreateIconButton(panel.transform, "とじる", new Vector2(0f, -400f), new Vector2(280f, 120f), PitaBloTheme.BackButton);
            controller.Configure(playTime, cleared, consecutive, slider, back);
            return controller;
        }

        private AnimalData[] CreateAnimalMasterData()
        {
            return new[]
            {
                CreateAnimal(1, "うさぎ先生", new Color(1f, 0.62f, 0.78f), 0),
                CreateAnimal(2, "ぞう先生", new Color(0.62f, 0.78f, 1f), 3),
                CreateAnimal(3, "ねこ先生", new Color(1f, 0.82f, 0.42f), 6)
            };
        }

        private AnimalData CreateAnimal(int id, string name, Color color, int unlockLevel)
        {
            var data = ScriptableObject.CreateInstance<AnimalData>();
            data.animalId = id;
            data.animalName = name;
            data.animalSprite = SpriteFactory.CreateCircleSprite(96, color);
            data.unlockRequiredLevel = unlockLevel;
            return data;
        }

        private SealData[] CreateSealMasterData()
        {
            return new[]
            {
                CreateSeal(1, "はじめての1れつ", "1列消した"),
                CreateSeal(2, "2れつセット", "2列同時に消した"),
                CreateSeal(3, "スーパー！", "3列以上消した")
            };
        }

        private SealData CreateSeal(int id, string title, string desc)
        {
            var data = ScriptableObject.CreateInstance<SealData>();
            data.sealId = id;
            data.sealTitle = title;
            data.description = desc;
            data.sealSprite = SpriteFactory.CreateGummySprite(96, PitaBloTheme.SealTab);
            return data;
        }

        private GameObject CreateScrollArea(Transform parent, string name, Vector2 pos)
        {
            var scrollGo = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(parent, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchoredPosition = pos;
            scrollRect.sizeDelta = new Vector2(900f, 1200f);
            scrollGo.GetComponent<Image>().color = new Color(1f, 0.95f, 1f, 0.25f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(160f, 180f);
            grid.spacing = new Vector2(20f, 20f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;

            return scrollGo;
        }

        private Button CreateAnchoredButton(
            Transform parent,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 size,
            Color color,
            int fontSize)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.sprite = SpriteFactory.CreateBubbleButtonSprite(64, color);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            ApplyButtonShadow(image);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            Stretch(textRect);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = PitaBloTheme.ButtonText;
            text.text = label;
            ApplyTextOutline(text, PitaBloTheme.ButtonTextOutline, new Vector2(1.5f, -1.5f));

            return go.GetComponent<Button>();
        }

        private Button CreateIconButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.sprite = SpriteFactory.CreateBubbleButtonSprite(64, color);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            ApplyButtonShadow(image);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            Stretch(textRect);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.Clamp((int)(size.y * 0.22f), 28, 58);
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = PitaBloTheme.ButtonText;
            text.text = label;
            ApplyTextOutline(text, PitaBloTheme.ButtonTextOutline, new Vector2(1.5f, -1.5f));

            return go.GetComponent<Button>();
        }

        private Text CreateLabel(Transform parent, string text, int fontSize, Vector2 pos, Color? color = null, bool bold = false)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(900f, fontSize + 40);
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color ?? PitaBloTheme.LabelText;
            label.text = text;
            return label;
        }

        private static void AddDecorativeBubbles(Transform parent)
        {
            var positions = new[]
            {
                new Vector2(-380f, 520f), new Vector2(400f, 380f), new Vector2(-320f, -200f),
                new Vector2(350f, -450f), new Vector2(0f, 600f), new Vector2(-450f, -600f),
                new Vector2(420f, 700f)
            };
            var sizes = new[] { 180f, 140f, 220f, 160f, 120f, 200f, 100f };

            for (var i = 0; i < positions.Length; i++)
            {
                var color = PitaBloTheme.BubbleColors[i % PitaBloTheme.BubbleColors.Length];
                var size = (int)sizes[i % sizes.Length];
                var bubbleGo = new GameObject($"Bubble_{i}", typeof(RectTransform), typeof(Image));
                bubbleGo.transform.SetParent(parent, false);
                bubbleGo.transform.SetAsFirstSibling();
                var bubbleRect = bubbleGo.GetComponent<RectTransform>();
                bubbleRect.anchoredPosition = positions[i];
                bubbleRect.sizeDelta = new Vector2(size, size);
                var bubbleImage = bubbleGo.GetComponent<Image>();
                bubbleImage.sprite = SpriteFactory.CreateSoftBubbleSprite(size, color);
                bubbleImage.raycastTarget = false;
            }
        }

        private static void ApplyTextOutline(Text text, Color outlineColor, Vector2 effectDistance)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = effectDistance;
        }

        private static void ApplyButtonShadow(Image image)
        {
            var shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.2f, 0.1f, 0.35f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -4f);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
