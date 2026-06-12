using Pitablock.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Pitablock.UI
{
    public class ReportUIController : MonoBehaviour
    {
        [SerializeField] private Text playTimeText;
        [SerializeField] private Text clearedCountText;
        [SerializeField] private Text consecutiveDaysText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Button backButton;

        private void Start()
        {
            backButton?.onClick.AddListener(() => UIManager.Instance?.OnBackToTitle());
        }

        public void Configure(Text playTime, Text cleared, Text consecutive, Slider slider, Button back)
        {
            playTimeText = playTime;
            clearedCountText = cleared;
            consecutiveDaysText = consecutive;
            progressSlider = slider;
            backButton = back;
        }

        public void UpdateView()
        {
            if (SaveDataManager.Instance?.CurrentData == null)
            {
                return;
            }

            var data = SaveDataManager.Instance.CurrentData;
            var hours = data.totalPlayTimeMinutes / 60;
            var minutes = data.totalPlayTimeMinutes % 60;

            if (playTimeText != null)
            {
                playTimeText.text = $"累計：{hours}時間{minutes}分";
            }

            if (clearedCountText != null)
            {
                clearedCountText.text = $"クリア課題数：{data.clearedStageCount}";
            }

            if (consecutiveDaysText != null)
            {
                consecutiveDaysText.text = $"連続プレイ：{data.consecutivePlayDays}日";
            }

            if (progressSlider != null)
            {
                progressSlider.maxValue = 10f;
                progressSlider.value = Mathf.Min(data.clearedStageCount, 10);
            }
        }
    }
}
