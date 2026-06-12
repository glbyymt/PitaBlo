using System.Collections.Generic;
using Pitablock.Controllers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Pitablock.Managers
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [SerializeField] private float tapMaxDuration = 0.25f;
        [SerializeField] private float tapMaxDistance = 20f;

        private BlockController currentDraggingBlock;
        private Vector2 touchStartPos;
        private float touchStartTime;
        private bool dragStarted;
        private readonly List<RaycastResult> raycastResults = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (GameManager.Instance != null
                && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                HandleTouchStart(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && currentDraggingBlock != null)
            {
                HandleTouchMove(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                HandleTouchEnd(Input.mousePosition);
            }
        }

        private void HandleTouchStart(Vector2 screenPos)
        {
            touchStartPos = screenPos;
            touchStartTime = Time.unscaledTime;
            dragStarted = false;
            currentDraggingBlock = RaycastForHandBlock(screenPos);
            HintManager.Instance?.NotifyPlayerInteraction();
        }

        private void HandleTouchMove(Vector2 screenPos)
        {
            if (currentDraggingBlock == null)
            {
                return;
            }

            var distance = Vector2.Distance(screenPos, touchStartPos);
            if (!dragStarted && distance > tapMaxDistance)
            {
                dragStarted = true;
                currentDraggingBlock.OnDragStart(touchStartPos);
            }

            if (dragStarted)
            {
                currentDraggingBlock.OnDragging(screenPos);
            }
        }

        private void HandleTouchEnd(Vector2 screenPos)
        {
            if (currentDraggingBlock == null)
            {
                return;
            }

            var duration = Time.unscaledTime - touchStartTime;
            var distance = Vector2.Distance(screenPos, touchStartPos);
            var isTap = !dragStarted
                        && duration <= tapMaxDuration
                        && distance <= tapMaxDistance;

            if (isTap)
            {
                currentDraggingBlock.RotateBlock();
            }
            else if (dragStarted)
            {
                currentDraggingBlock.OnDragEnd(screenPos);
            }

            currentDraggingBlock = null;
            dragStarted = false;
        }

        private BlockController RaycastForHandBlock(Vector2 screenPos)
        {
            if (EventSystem.current == null)
            {
                return null;
            }

            raycastResults.Clear();
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };
            EventSystem.current.RaycastAll(eventData, raycastResults);

            foreach (var result in raycastResults)
            {
                var block = result.gameObject.GetComponentInParent<BlockController>();
                if (block != null && block.CurrentState == BlockState.InHand)
                {
                    return block;
                }
            }

            return null;
        }
    }
}
