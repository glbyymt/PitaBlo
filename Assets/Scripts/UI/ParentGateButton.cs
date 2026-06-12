using UnityEngine;
using UnityEngine.EventSystems;

namespace Pitablock.UI
{
    public class ParentGateButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float holdDuration = 3f;

        private float holdTimer;
        private bool isHolding;

        public void OnPointerDown(PointerEventData eventData)
        {
            isHolding = true;
            holdTimer = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isHolding = false;
            holdTimer = 0f;
        }

        private void Update()
        {
            if (!isHolding)
            {
                return;
            }

            holdTimer += Time.unscaledDeltaTime;
            if (holdTimer >= holdDuration)
            {
                isHolding = false;
                UIManager.Instance?.OnParentGateOpened();
            }
        }
    }
}
