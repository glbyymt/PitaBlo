using System.Collections;
using Pitablock.Data;
using UnityEngine;

namespace Pitablock.UI
{
    [RequireComponent(typeof(AudioSource))]
    public class AnimalIconUI : MonoBehaviour
    {
        private RectTransform rectTransform;
        private AudioSource audioSource;

        public void Bind(RectTransform rect, AudioSource source)
        {
            rectTransform = rect;
            audioSource = source ?? GetComponent<AudioSource>();
        }

        public void PlayAction(AnimalData animal)
        {
            if (rectTransform != null)
            {
                StartCoroutine(PunchAnimation());
            }

            if (animal?.voiceClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(animal.voiceClip);
            }
        }

        private IEnumerator PunchAnimation()
        {
            var origin = rectTransform.anchoredPosition;
            var elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                var offset = Mathf.Sin(elapsed / 0.35f * Mathf.PI * 3f) * 12f;
                rectTransform.anchoredPosition = origin + new Vector2(0f, offset);
                yield return null;
            }

            rectTransform.anchoredPosition = origin;
        }
    }
}
