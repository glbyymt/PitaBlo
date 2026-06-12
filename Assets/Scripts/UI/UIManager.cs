using System.Collections;
using Pitablock.Controllers;
using Pitablock.Data;
using Pitablock.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Pitablock.UI
{
    public enum ResultContext
    {
        GameOver,
        BlockRoadCleared,
        TutorialCompleted
    }

    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject modeSelectPanel;
        [SerializeField] private GameObject beginnerSubPanel;
        [SerializeField] private GameObject inGamePanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private GameObject collectionPanel;
        [SerializeField] private GameObject reportPanel;

        [Header("InGame")]
        [SerializeField] private Button changeButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Text changeCountText;
        [SerializeField] private Text praiseText;
        [SerializeField] private Image teacherImage;
        [SerializeField] private Text modeTitleText;
        [SerializeField] private Text modeHintText;
        [SerializeField] private Text modeGoalText;
        [SerializeField] private Text resultMessageText;

        private ResultContext lastResultContext = ResultContext.GameOver;

        [Header("Controllers")]
        [SerializeField] private CollectionUIController collectionController;
        [SerializeField] private ReportUIController reportController;
        [SerializeField] private PuzzleSilhouetteUI silhouetteUI;

        private Coroutine praiseCoroutine;
        private Coroutine tutorialAdvanceCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SubscribeEvents();
        }

        public void InitializeUI()
        {
            SubscribeEvents();
            UpdatePanels(GameState.Title);
        }

        private void SubscribeEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= UpdatePanels;
                GameManager.Instance.OnStateChanged += UpdatePanels;
            }

            if (GridManager.Instance != null)
            {
                GridManager.Instance.OnLineCleared -= OnLineCleared;
                GridManager.Instance.OnGridChanged -= UpdateUndoButton;
                GridManager.Instance.OnGameOver -= OnGameOver;
                GridManager.Instance.OnLineCleared += OnLineCleared;
                GridManager.Instance.OnGridChanged += UpdateUndoButton;
                GridManager.Instance.OnGameOver += OnGameOver;
            }

            if (BlockSpawner.Instance != null)
            {
                BlockSpawner.Instance.OnChangeCountChanged -= UpdateChangeCount;
                BlockSpawner.Instance.OnChangeCountChanged += UpdateChangeCount;
            }

            if (GameModeSession.Instance != null)
            {
                GameModeSession.Instance.OnSessionChanged -= UpdateModeSessionUI;
                GameModeSession.Instance.OnStageCleared -= OnBlockRoadStageCleared;
                GameModeSession.Instance.OnTutorialCompleted -= OnTutorialCompleted;
                GameModeSession.Instance.OnTutorialStageCleared -= OnTutorialStageCleared;
                GameModeSession.Instance.OnPuzzleFitCleared -= OnPuzzleFitCleared;
                GameModeSession.Instance.OnAllPuzzleFitCompleted -= OnAllPuzzleFitCompleted;
                GameModeSession.Instance.OnSessionChanged += UpdateModeSessionUI;
                GameModeSession.Instance.OnStageCleared += OnBlockRoadStageCleared;
                GameModeSession.Instance.OnTutorialCompleted += OnTutorialCompleted;
                GameModeSession.Instance.OnTutorialStageCleared += OnTutorialStageCleared;
                GameModeSession.Instance.OnPuzzleFitCleared += OnPuzzleFitCleared;
                GameModeSession.Instance.OnAllPuzzleFitCompleted += OnAllPuzzleFitCompleted;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= UpdatePanels;
            }

            if (GridManager.Instance != null)
            {
                GridManager.Instance.OnLineCleared -= OnLineCleared;
                GridManager.Instance.OnGridChanged -= UpdateUndoButton;
                GridManager.Instance.OnGameOver -= OnGameOver;
            }

            if (BlockSpawner.Instance != null)
            {
                BlockSpawner.Instance.OnChangeCountChanged -= UpdateChangeCount;
            }

            if (GameModeSession.Instance != null)
            {
                GameModeSession.Instance.OnSessionChanged -= UpdateModeSessionUI;
                GameModeSession.Instance.OnStageCleared -= OnBlockRoadStageCleared;
                GameModeSession.Instance.OnTutorialCompleted -= OnTutorialCompleted;
                GameModeSession.Instance.OnTutorialStageCleared -= OnTutorialStageCleared;
                GameModeSession.Instance.OnPuzzleFitCleared -= OnPuzzleFitCleared;
                GameModeSession.Instance.OnAllPuzzleFitCompleted -= OnAllPuzzleFitCompleted;
            }
        }

        public void BindPanels(
            GameObject title,
            GameObject modeSelect,
            GameObject beginnerSub,
            GameObject inGame,
            GameObject result,
            GameObject collection,
            GameObject report)
        {
            titlePanel = title;
            modeSelectPanel = modeSelect;
            beginnerSubPanel = beginnerSub;
            inGamePanel = inGame;
            resultPanel = result;
            collectionPanel = collection;
            reportPanel = report;
        }

        public void BindInGameUI(
            Button change,
            Button undo,
            Button pause,
            Button hint,
            Text changeText,
            Text praise,
            Image teacher,
            Text modeTitle,
            Text hintLabel,
            Text goal)
        {
            changeButton = change;
            undoButton = undo;
            pauseButton = pause;
            hintButton = hint;
            changeCountText = changeText;
            praiseText = praise;
            teacherImage = teacher;
            modeTitleText = modeTitle;
            modeHintText = hintLabel;
            modeGoalText = goal;

            changeButton?.onClick.AddListener(OnChangeButtonClicked);
            undoButton?.onClick.AddListener(OnUndoButtonClicked);
            pauseButton?.onClick.AddListener(OnPauseButtonClicked);
            hintButton?.onClick.AddListener(OnHintButtonClicked);
        }

        public void BindSilhouetteUI(PuzzleSilhouetteUI silhouette)
        {
            silhouetteUI = silhouette;
        }

        public void BindResultUI(Text resultMessage)
        {
            resultMessageText = resultMessage;
        }

        public void BindControllers(CollectionUIController collection, ReportUIController report)
        {
            collectionController = collection;
            reportController = report;
        }

        private void UpdatePanels(GameState state)
        {
            SetActive(titlePanel, state == GameState.Title);
            SetActive(modeSelectPanel, state == GameState.ModeSelect);
            SetActive(beginnerSubPanel, state == GameState.BeginnerSubSelect);
            SetActive(inGamePanel, state == GameState.Playing || state == GameState.Paused);
            SetActive(resultPanel, state == GameState.Result);
            SetActive(collectionPanel, state == GameState.Collection);
            SetActive(reportPanel, state == GameState.Report);

            if (state == GameState.Collection)
            {
                collectionController?.Refresh();
            }

            if (state == GameState.Report)
            {
                reportController?.UpdateView();
            }

            if (state == GameState.Playing)
            {
                UpdateUndoButton();
                UpdateChangeCount(BlockSpawner.Instance != null ? BlockSpawner.Instance.RemainingChangeCount : 0);
                UpdateModeSessionUI();
                silhouetteUI?.Refresh();
            }
            else
            {
                silhouetteUI?.Clear();
                HintManager.Instance?.NotifyPlayerInteraction();
            }
        }

        private static void SetActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        public void OnStartButtonClicked()
        {
            GameManager.Instance?.ChangeState(GameState.ModeSelect);
        }

        public void OnModeSelected(int modeIndex)
        {
            var mode = (GameMode)modeIndex;
            GameManager.Instance?.StartGame(mode);
        }

        public void OnBeginnerModeSelected()
        {
            GameManager.Instance?.ChangeState(GameState.BeginnerSubSelect);
        }

        public void OnBeginnerSubModeSelected(BeginnerSubMode subMode)
        {
            GameManager.Instance?.StartGame(GameMode.Beginner, subMode);
        }

        public void OnHintButtonClicked()
        {
            HintManager.Instance?.OnHintButtonPressed();
        }

        public void OnCollectionButtonClicked()
        {
            GameManager.Instance?.ChangeState(GameState.Collection);
        }

        public void OnBackToModeSelect()
        {
            GameManager.Instance?.ChangeState(GameState.ModeSelect);
        }

        public void OnBackToTitle()
        {
            GameManager.Instance?.ChangeState(GameState.Title);
        }

        public void OnParentGateOpened()
        {
            GameManager.Instance?.ChangeState(GameState.Report);
        }

        public void OnChangeButtonClicked()
        {
            BlockSpawner.Instance?.OnChangeButtonPressed();
        }

        public void OnUndoButtonClicked()
        {
            GridManager.Instance?.UndoLastMove();
            UpdateUndoButton();
        }

        private void OnPauseButtonClicked()
        {
            GameManager.Instance?.ChangeState(GameState.ModeSelect);
        }

        private void OnLineCleared(int lineCount)
        {
            ShowPraiseEffect(lineCount);
        }

        public void ShowPraiseEffect(int lineCount)
        {
            if (praiseText == null)
            {
                return;
            }

            praiseText.text = lineCount switch
            {
                1 => "すごい！",
                2 => "大技だ！",
                _ => "てんさい！"
            };

            if (praiseCoroutine != null)
            {
                StopCoroutine(praiseCoroutine);
            }

            praiseCoroutine = StartCoroutine(PraiseAnimation());
        }

        private IEnumerator PraiseAnimation()
        {
            praiseText.gameObject.SetActive(true);
            var rect = praiseText.rectTransform;
            var baseScale = Vector3.one;
            rect.localScale = baseScale * 0.5f;

            var elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                rect.localScale = Vector3.Lerp(baseScale * 0.5f, baseScale * 1.2f, elapsed / 0.3f);
                yield return null;
            }

            if (teacherImage != null)
            {
                StartCoroutine(BounceImage(teacherImage.rectTransform));
            }

            yield return new WaitForSeconds(1.2f);
            praiseText.gameObject.SetActive(false);
        }

        private static IEnumerator BounceImage(RectTransform rect)
        {
            var origin = rect.anchoredPosition;
            var elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                var y = Mathf.Sin(elapsed / 0.4f * Mathf.PI) * 30f;
                rect.anchoredPosition = origin + new Vector2(0f, y);
                yield return null;
            }

            rect.anchoredPosition = origin;
        }

        private void UpdateUndoButton()
        {
            if (undoButton != null && GridManager.Instance != null)
            {
                undoButton.interactable = GridManager.Instance.CanUndo;
            }
        }

        private void UpdateChangeCount(int count)
        {
            var session = GameModeSession.Instance;
            var changeEnabled = session == null || session.ChangeEnabled;
            var unlimited = session != null && session.HasUnlimitedChange;

            if (changeCountText != null)
            {
                if (!changeEnabled)
                {
                    changeCountText.text = string.Empty;
                }
                else if (unlimited)
                {
                    changeCountText.text = "∞";
                }
                else
                {
                    changeCountText.text = $"×{count}";
                }
            }

            if (changeButton != null)
            {
                changeButton.gameObject.SetActive(changeEnabled);
                changeButton.interactable = changeEnabled && (unlimited || count > 0);
            }

            if (hintButton != null)
            {
                hintButton.gameObject.SetActive(session == null || session.HintEnabled);
            }
        }

        private void UpdateModeSessionUI()
        {
            var session = GameModeSession.Instance;
            if (session == null)
            {
                return;
            }

            if (modeTitleText != null)
            {
                modeTitleText.text = session.ModeTitle;
            }

            if (modeHintText != null)
            {
                modeHintText.text = session.HintMessage;
            }

            if (modeGoalText != null)
            {
                modeGoalText.text = session.GoalMessage;
            }

            UpdateChangeCount(BlockSpawner.Instance != null ? BlockSpawner.Instance.RemainingChangeCount : 0);

            if (session.ActiveMode == GameMode.Beginner
                && session.BeginnerSubMode == BeginnerSubMode.PuzzleFit)
            {
                silhouetteUI?.Refresh();
            }
        }

        private void OnGameOver()
        {
            ShowResult(ResultContext.GameOver, "がんばったね！");
        }

        private void OnBlockRoadStageCleared()
        {
            ShowResult(ResultContext.BlockRoadCleared, "クリア！つぎの みちへ！");
        }

        private void OnTutorialCompleted()
        {
            ShowResult(ResultContext.TutorialCompleted, "おてほん おわり！");
        }

        private void OnTutorialStageCleared()
        {
            if (tutorialAdvanceCoroutine != null)
            {
                StopCoroutine(tutorialAdvanceCoroutine);
            }

            tutorialAdvanceCoroutine = StartCoroutine(AdvanceTutorialStageRoutine());
        }

        private IEnumerator AdvanceTutorialStageRoutine()
        {
            ShowCustomPraise("すごい！");
            yield return new WaitForSeconds(1.4f);
            GameModeSession.Instance?.LoadCurrentTutorialStage();
            silhouetteUI?.Refresh();
            tutorialAdvanceCoroutine = null;
        }

        private void OnPuzzleFitCleared()
        {
            ShowCustomPraise("できたね！");
            ParticleController.Instance?.PlayLineClearEffect(0, GridManager.Instance.GridToWorld(new Vector2Int(4, 1)));
            silhouetteUI?.Refresh();
        }

        private void OnAllPuzzleFitCompleted()
        {
            ShowCustomPraise("ぜんぶ できたね！");
            silhouetteUI?.Refresh();
        }

        public void ShowCustomPraise(string message)
        {
            if (praiseText == null)
            {
                return;
            }

            praiseText.text = message;
            if (praiseCoroutine != null)
            {
                StopCoroutine(praiseCoroutine);
            }

            praiseCoroutine = StartCoroutine(PraiseAnimation());
        }

        private void ShowResult(ResultContext context, string message)
        {
            lastResultContext = context;
            if (resultMessageText != null)
            {
                resultMessageText.text = message;
            }

            GameManager.Instance?.ChangeState(GameState.Result);
        }

        public void OnResultContinue()
        {
            if (GameManager.Instance == null)
            {
                OnBackToModeSelect();
                return;
            }

            if (lastResultContext == ResultContext.BlockRoadCleared
                && GameManager.Instance.CurrentMode == GameMode.BlockRoad)
            {
                GameManager.Instance.StartGame(GameMode.BlockRoad);
                return;
            }

            if (lastResultContext == ResultContext.TutorialCompleted
                && GameManager.Instance.CurrentMode == GameMode.Tutorial)
            {
                OnBackToModeSelect();
                return;
            }

            OnBackToModeSelect();
        }
    }
}
