using Pitablock.Controllers;
using Pitablock.UI;
using UnityEngine;

namespace Pitablock.Managers
{
    /// <summary>
    /// おしえてシステム：無操作時またはボタン押下でヒントを表示。
    /// </summary>
    public class HintManager : MonoBehaviour
    {
        public static HintManager Instance { get; private set; }

        [SerializeField] private float idleSeconds = 5f;

        private HintGhostUI ghostUI;
        private float idleTimer;
        private bool hintVisible;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Configure(HintGhostUI ghost)
        {
            ghostUI = ghost;
        }

        private void Update()
        {
            if (!CanRunHintLogic())
            {
                ResetIdle();
                return;
            }

            if (IsPlayerHoldingBlock())
            {
                ResetIdle();
                return;
            }

            idleTimer += Time.unscaledDeltaTime;
            if (!hintVisible && idleTimer >= idleSeconds)
            {
                ShowHint();
            }
        }

        public void OnHintButtonPressed()
        {
            if (!CanRunHintLogic())
            {
                return;
            }

            ShowHint();
            ResetIdle();
        }

        public void NotifyPlayerInteraction()
        {
            ResetIdle();
            HideHint();
        }

        private void ShowHint()
        {
            var session = GameModeSession.Instance;
            if (session == null || !session.HintEnabled)
            {
                return;
            }

            var hand = BlockSpawner.Instance?.CurrentHandBlock;
            if (hand == null || hand.CurrentState != BlockState.InHand)
            {
                return;
            }

            ghostUI?.Show(hand);
            hintVisible = true;
        }

        private void HideHint()
        {
            ghostUI?.Hide();
            hintVisible = false;
        }

        private void ResetIdle()
        {
            idleTimer = 0f;
        }

        private static bool CanRunHintLogic()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            {
                return false;
            }

            return GameModeSession.Instance != null && GameModeSession.Instance.HintEnabled;
        }

        private static bool IsPlayerHoldingBlock()
        {
            var hand = BlockSpawner.Instance?.CurrentHandBlock;
            return hand != null && hand.CurrentState == BlockState.Dragging;
        }
    }
}
