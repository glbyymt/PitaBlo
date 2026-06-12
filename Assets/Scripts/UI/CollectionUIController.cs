using System.Collections.Generic;
using Pitablock.Data;
using Pitablock.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Pitablock.UI
{
    public class CollectionUIController : MonoBehaviour
    {
        [SerializeField] private Button animalsTabButton;
        [SerializeField] private Button sealsTabButton;
        [SerializeField] private GameObject animalsContent;
        [SerializeField] private GameObject sealsContent;
        [SerializeField] private Transform animalsGrid;
        [SerializeField] private Transform sealsGrid;
        [SerializeField] private GameObject iconPrefab;
        [SerializeField] private AnimalData[] animals;
        [SerializeField] private SealData[] seals;
        [SerializeField] private Button backButton;

        private void Start()
        {
            animalsTabButton?.onClick.AddListener(() => ShowTab(true));
            sealsTabButton?.onClick.AddListener(() => ShowTab(false));
            backButton?.onClick.AddListener(() => UIManager.Instance?.OnBackToModeSelect());
            ShowTab(true);
        }

        public void Configure(
            Button animalsTab,
            Button sealsTab,
            GameObject animalsPanel,
            GameObject sealsPanel,
            Transform animalsRoot,
            Transform sealsRoot,
            GameObject prefab,
            AnimalData[] animalData,
            SealData[] sealData,
            Button back)
        {
            animalsTabButton = animalsTab;
            sealsTabButton = sealsTab;
            animalsContent = animalsPanel;
            sealsContent = sealsPanel;
            animalsGrid = animalsRoot;
            sealsGrid = sealsRoot;
            iconPrefab = prefab;
            animals = animalData;
            seals = sealData;
            backButton = back;
        }

        public void Refresh()
        {
            PopulateAnimals();
            PopulateSeals();
        }

        private void ShowTab(bool showAnimals)
        {
            if (animalsContent != null)
            {
                animalsContent.SetActive(showAnimals);
            }

            if (sealsContent != null)
            {
                sealsContent.SetActive(!showAnimals);
            }
        }

        private void PopulateAnimals()
        {
            if (animalsGrid == null || animals == null)
            {
                return;
            }

            ClearChildren(animalsGrid);

            foreach (var animal in animals)
            {
                if (animal == null)
                {
                    continue;
                }

                var unlocked = SaveDataManager.Instance != null
                    && SaveDataManager.Instance.IsAnimalUnlocked(animal.animalId);

                var capturedAnimal = animal;
                var item = CreateIcon(animalsGrid, animal.animalSprite, animal.animalName, unlocked, null);
                if (unlocked)
                {
                    var icon = item.GetComponent<AnimalIconUI>();
                    var button = item.GetComponent<Button>();
                    button?.onClick.AddListener(() => icon?.PlayAction(capturedAnimal));
                }
            }
        }

        private void PopulateSeals()
        {
            if (sealsGrid == null || seals == null)
            {
                return;
            }

            ClearChildren(sealsGrid);

            foreach (var seal in seals)
            {
                if (seal == null)
                {
                    continue;
                }

                var unlocked = SaveDataManager.Instance != null
                    && SaveDataManager.Instance.IsSealUnlocked(seal.sealId);

                CreateIcon(sealsGrid, seal.sealSprite, seal.sealTitle, unlocked, null);
            }
        }

        private GameObject CreateIcon(Transform parent, Sprite sprite, string label, bool unlocked, UnityEngine.Events.UnityAction onClick)
        {
            GameObject item;
            if (iconPrefab != null)
            {
                item = Instantiate(iconPrefab, parent);
            }
            else
            {
                item = CreateDefaultIcon(parent);
            }

            var image = item.GetComponentInChildren<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.color = unlocked ? Color.white : Color.black;
            }

            var text = item.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = unlocked ? label : "？？？";
            }

            var button = item.GetComponent<Button>();
            if (button != null && onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            var animalIcon = item.GetComponent<AnimalIconUI>();
            if (animalIcon == null)
            {
                animalIcon = item.AddComponent<AnimalIconUI>();
            }

            animalIcon.Bind(item.GetComponent<RectTransform>(), item.GetComponent<AudioSource>());
            return item;
        }

        private static GameObject CreateDefaultIcon(Transform parent)
        {
            var item = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(Button));
            item.transform.SetParent(parent, false);

            var rect = item.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140f, 140f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(item.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return item;
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
