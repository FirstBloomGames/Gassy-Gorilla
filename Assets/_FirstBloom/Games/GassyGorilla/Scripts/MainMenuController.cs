using System;
using FirstBloom.ArcadeFramework.Audio;
using FirstBloom.ArcadeFramework.Core;
using FirstBloom.ArcadeFramework.Save;
using FirstBloom.ArcadeFramework.UI;
using FirstBloom.ArcadeFramework.VFX;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FirstBloom.Games.GassyGorilla
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "Game";
        [SerializeField] private Text bestDistanceText;
        [SerializeField] private ArcadeSettingsMenu settingsMenu;
        [SerializeField] private GameObject mainActionsRoot;

        [Header("Expeditions")]
        [SerializeField] private GassyExpeditionCatalog expeditionCatalog;
        [SerializeField] private CanvasGroupPanel expeditionPanel;
        [SerializeField] private Button[] expeditionButtons;
        [SerializeField] private Text[] expeditionButtonLabels;
        [SerializeField] private Text expeditionChapterText;
        [SerializeField] private Button expeditionPreviousChapterButton;
        [SerializeField] private Button expeditionNextChapterButton;
        [SerializeField] private Text expeditionTitleText;
        [SerializeField] private Text expeditionObjectiveText;
        [SerializeField] private Text expeditionLessonText;
        [SerializeField] private Text expeditionStoryText;
        [SerializeField] private Text expeditionStatusText;
        [SerializeField] private Button expeditionPlayButton;

        [Header("Jungle Badges")]
        [SerializeField] private CanvasGroupPanel badgePanel;
        [SerializeField] private Text badgeSummaryText;
        [SerializeField] private Text[] badgeEntryTexts;
        [SerializeField] private Text badgeButtonText;

        private int selectedExpeditionIndex;
        private int selectedChapterIndex;

        public GassyExpeditionCatalog ExpeditionCatalog { get { return expeditionCatalog; } }
        public bool IsExpeditionUiConfigured
        {
            get
            {
                int count = expeditionCatalog != null ? expeditionCatalog.Count : 0;
                return count == GassyExpeditionCatalog.VersionOneExpeditionCount &&
                    mainActionsRoot != null &&
                    expeditionPanel != null &&
                    expeditionButtons != null &&
                    expeditionButtons.Length == GassyExpeditionCatalog.LevelsPerChapter &&
                    expeditionButtonLabels != null &&
                    expeditionButtonLabels.Length == GassyExpeditionCatalog.LevelsPerChapter &&
                    expeditionChapterText != null &&
                    expeditionPreviousChapterButton != null &&
                    expeditionNextChapterButton != null &&
                    expeditionTitleText != null &&
                    expeditionObjectiveText != null &&
                    expeditionLessonText != null &&
                    expeditionStoryText != null &&
                    expeditionStatusText != null &&
                    expeditionPlayButton != null;
            }
        }

        public bool IsBadgeUiConfigured
        {
            get
            {
                return badgePanel != null &&
                    badgeSummaryText != null &&
                    badgeEntryTexts != null &&
                    badgeEntryTexts.Length == GassyBadgeService.Count &&
                    badgeButtonText != null;
            }
        }

        private void Start()
        {
            if (ArcadeTimeController.Instance != null)
            {
                ArcadeTimeController.Instance.ResetTimeScale();
            }
            else
            {
                Time.timeScale = 1f;
            }

            if (TryLaunchWebQaExpedition())
            {
                return;
            }

            GassyBadgeService.Reconcile(expeditionCatalog, false);
            UpdateBestDistance();
            RefreshBadgePanel();
            SetMainActionsVisible(true);
            SelectInitialExpedition();
            RefreshExpeditionPanel();

            if (ArcadeRunSession.Mode == ArcadeRunMode.Finite && expeditionPanel != null)
            {
                expeditionPanel.Show();
                SetMainActionsVisible(false);
            }
        }

        public void Play()
        {
            PlayEndless();
        }

        public void PlayEndless()
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.NotifyUserGesture();
            }

            ArcadeRunSession.SelectEndless();
            SceneManager.LoadScene(gameSceneName);
        }

        public void OpenExpeditions()
        {
            NotifyUserGesture();
            SelectInitialExpedition();
            RefreshExpeditionPanel();
            if (expeditionPanel != null)
            {
                expeditionPanel.Show();
            }

            SetMainActionsVisible(false);
        }

        public void CloseExpeditions()
        {
            NotifyUserGesture();
            if (expeditionPanel != null)
            {
                expeditionPanel.Hide();
            }

            SetMainActionsVisible(true);
        }

        public void SelectExpedition1() { SelectExpedition(0); }
        public void SelectExpedition2() { SelectExpedition(1); }
        public void SelectExpedition3() { SelectExpedition(2); }
        public void SelectExpedition4() { SelectExpedition(3); }
        public void SelectExpedition5() { SelectExpedition(4); }

        public void PreviousExpeditionChapter()
        {
            ChangeExpeditionChapter(-1);
        }

        public void NextExpeditionChapter()
        {
            ChangeExpeditionChapter(1);
        }

        public void PlaySelectedExpedition()
        {
            NotifyUserGesture();
            GassyExpeditionDefinition definition = expeditionCatalog != null
                ? expeditionCatalog.GetByIndex(selectedExpeditionIndex)
                : null;
            if (definition == null || !GassyExpeditionProgressStore.IsUnlocked(selectedExpeditionIndex))
            {
                return;
            }

            if (ArcadeRunSession.SelectFinite(definition.ExpeditionId))
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }

        public void OpenSettings()
        {
            NotifyUserGesture();

            if (settingsMenu != null)
            {
                settingsMenu.Open();
            }

            SetMainActionsVisible(false);
        }

        public void CloseSettings()
        {
            NotifyUserGesture();

            if (settingsMenu != null)
            {
                settingsMenu.Close();
            }

            SetMainActionsVisible(true);
        }

        public void OpenBadges()
        {
            NotifyUserGesture();
            RefreshBadgePanel();
            if (badgePanel != null)
            {
                badgePanel.Show();
            }

            SetMainActionsVisible(false);
        }

        public void CloseBadges()
        {
            NotifyUserGesture();
            if (badgePanel != null)
            {
                badgePanel.Hide();
            }

            RefreshBadgePanel();
            SetMainActionsVisible(true);
        }

        public void ResetBestDistance()
        {
            HighScoreStore.ResetBestDistance(GassyGorillaGameManager.BestDistanceKey);
            UpdateBestDistance();
        }

        private void UpdateBestDistance()
        {
            if (bestDistanceText == null)
            {
                return;
            }

            float best = HighScoreStore.GetBestDistance(GassyGorillaGameManager.BestDistanceKey);
            bestDistanceText.text = "Best Distance: " + Mathf.FloorToInt(best) + " m";
        }

        private void SelectInitialExpedition()
        {
            if (expeditionCatalog == null || expeditionCatalog.Count == 0)
            {
                selectedExpeditionIndex = 0;
                selectedChapterIndex = 0;
                return;
            }

            if (ArcadeRunSession.Mode == ArcadeRunMode.Finite)
            {
                GassyExpeditionDefinition selected = expeditionCatalog.FindById(ArcadeRunSession.ContentId);
                int selectedIndex = expeditionCatalog.IndexOf(selected);
                if (selectedIndex >= 0)
                {
                    selectedExpeditionIndex = selectedIndex;
                    selectedChapterIndex =
                        selectedIndex / GassyExpeditionCatalog.LevelsPerChapter;
                    return;
                }
            }

            selectedExpeditionIndex = Mathf.Clamp(
                GassyExpeditionProgressStore.GetHighestUnlockedIndex(),
                0,
                expeditionCatalog.Count - 1);
            selectedChapterIndex =
                selectedExpeditionIndex / GassyExpeditionCatalog.LevelsPerChapter;
        }

        private void SelectExpedition(int slotIndex)
        {
            NotifyUserGesture();
            int index = selectedChapterIndex * GassyExpeditionCatalog.LevelsPerChapter +
                slotIndex;
            if (expeditionCatalog == null || expeditionCatalog.GetByIndex(index) == null)
            {
                return;
            }

            selectedExpeditionIndex = index;
            RefreshExpeditionPanel();
        }

        private void ChangeExpeditionChapter(int direction)
        {
            NotifyUserGesture();
            if (expeditionCatalog == null || expeditionCatalog.ChapterCount <= 0)
            {
                return;
            }

            int chapter = Mathf.Clamp(
                selectedChapterIndex + direction,
                0,
                expeditionCatalog.ChapterCount - 1);
            if (chapter == selectedChapterIndex)
            {
                return;
            }

            selectedChapterIndex = chapter;
            selectedExpeditionIndex =
                selectedChapterIndex * GassyExpeditionCatalog.LevelsPerChapter;
            RefreshExpeditionPanel();
        }

        private void RefreshExpeditionPanel()
        {
            if (expeditionCatalog == null)
            {
                return;
            }

            selectedChapterIndex = Mathf.Clamp(
                selectedChapterIndex,
                0,
                Mathf.Max(0, expeditionCatalog.ChapterCount - 1));
            int chapterStart =
                selectedChapterIndex * GassyExpeditionCatalog.LevelsPerChapter;
            int chapterEnd = Mathf.Min(
                chapterStart + GassyExpeditionCatalog.LevelsPerChapter,
                expeditionCatalog.Count);
            if (selectedExpeditionIndex < chapterStart ||
                selectedExpeditionIndex >= chapterEnd)
            {
                selectedExpeditionIndex = chapterStart;
            }

            for (int slotIndex = 0;
                slotIndex < GassyExpeditionCatalog.LevelsPerChapter;
                slotIndex++)
            {
                int expeditionIndex = chapterStart + slotIndex;
                GassyExpeditionDefinition definition =
                    expeditionCatalog.GetByIndex(expeditionIndex);
                bool unlocked = definition != null &&
                    GassyExpeditionProgressStore.IsUnlocked(expeditionIndex);
                int stars = definition != null
                    ? GassyExpeditionProgressStore.GetBestStars(definition.ExpeditionId)
                    : 0;

                if (expeditionButtons != null &&
                    slotIndex < expeditionButtons.Length &&
                    expeditionButtons[slotIndex] != null)
                {
                    Button button = expeditionButtons[slotIndex];
                    button.gameObject.SetActive(definition != null);
                    button.interactable = definition != null;
                    Image buttonImage = button.GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = expeditionIndex == selectedExpeditionIndex
                            ? new Color(0.88f, 0.56f, 0.14f, 1f)
                            : (unlocked
                                ? new Color(0.22f, 0.52f, 0.27f, 1f)
                                : new Color(0.12f, 0.23f, 0.2f, 1f));
                    }
                }

                if (expeditionButtonLabels != null &&
                    slotIndex < expeditionButtonLabels.Length &&
                    expeditionButtonLabels[slotIndex] != null &&
                    definition != null)
                {
                    string result = !unlocked
                        ? "LOCKED"
                        : (stars > 0 ? new string('*', stars) : "NEW");
                    expeditionButtonLabels[slotIndex].text =
                        (expeditionIndex + 1) + "  " +
                        definition.DisplayTitle.ToUpperInvariant() + "   " + result;
                }
            }

            if (expeditionChapterText != null)
            {
                expeditionChapterText.text =
                    "CHAPTER " + (selectedChapterIndex + 1) + "  " +
                    expeditionCatalog.GetChapterTitle(selectedChapterIndex).ToUpperInvariant();
            }

            if (expeditionPreviousChapterButton != null)
            {
                expeditionPreviousChapterButton.interactable = selectedChapterIndex > 0;
            }

            if (expeditionNextChapterButton != null)
            {
                expeditionNextChapterButton.interactable =
                    selectedChapterIndex < expeditionCatalog.ChapterCount - 1;
            }

            GassyExpeditionDefinition selected = expeditionCatalog.GetByIndex(selectedExpeditionIndex);
            bool selectedUnlocked = selected != null &&
                GassyExpeditionProgressStore.IsUnlocked(selectedExpeditionIndex);
            int selectedStars = selected != null
                ? GassyExpeditionProgressStore.GetBestStars(selected.ExpeditionId)
                : 0;

            if (expeditionTitleText != null)
            {
                expeditionTitleText.text = selected != null ? selected.DisplayTitle.ToUpperInvariant() : "EXPEDITIONS";
            }

            if (expeditionObjectiveText != null)
            {
                expeditionObjectiveText.text = selected != null ? "OBJECTIVE  " + selected.ObjectiveText : string.Empty;
            }

            if (expeditionLessonText != null)
            {
                expeditionLessonText.text = selected != null
                    ? "LESSON  " + selected.LessonText
                    : string.Empty;
            }

            if (expeditionStoryText != null)
            {
                expeditionStoryText.text = selected != null ? selected.OpeningStory : string.Empty;
            }

            if (expeditionStatusText != null)
            {
                expeditionStatusText.text = !selectedUnlocked
                    ? "COMPLETE THE PREVIOUS EXPEDITION TO UNLOCK"
                    : (selectedStars > 0 ? "BEST  " + selectedStars + " / 3 STARS" : "NOT YET COMPLETED");
            }

            if (expeditionPlayButton != null)
            {
                expeditionPlayButton.interactable = selectedUnlocked;
                Text playLabel = expeditionPlayButton.GetComponentInChildren<Text>();
                if (playLabel != null)
                {
                    playLabel.text = selectedUnlocked ? "START EXPEDITION" : "LOCKED";
                }
            }
        }

        private void RefreshBadgePanel()
        {
            int unlockedCount = GassyBadgeService.GetUnlockedCount();
            if (badgeSummaryText != null)
            {
                badgeSummaryText.text = unlockedCount + " / " + GassyBadgeService.Count + " EARNED";
            }

            if (badgeButtonText != null)
            {
                badgeButtonText.text = "BADGES  " + unlockedCount + "/" + GassyBadgeService.Count;
            }

            GassyBadgeDefinition[] definitions = GassyBadgeService.Definitions;
            if (badgeEntryTexts == null)
            {
                return;
            }

            for (int i = 0; i < badgeEntryTexts.Length && i < definitions.Length; i++)
            {
                Text entryText = badgeEntryTexts[i];
                GassyBadgeDefinition definition = definitions[i];
                if (entryText == null || definition == null)
                {
                    continue;
                }

                bool unlocked = GassyBadgeService.IsUnlocked(definition);
                int progress = GassyBadgeService.GetProgress(definition);
                entryText.text = definition.DisplayTitle.ToUpperInvariant() + "\n" +
                    (unlocked ? "EARNED" : definition.FormatProgress(progress));
                entryText.color = unlocked
                    ? new Color(0.78f, 1f, 0.68f, 1f)
                    : new Color(1f, 1f, 1f, 0.76f);
            }
        }

        private static void NotifyUserGesture()
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.NotifyUserGesture();
            }
        }

        private void SetMainActionsVisible(bool visible)
        {
            if (mainActionsRoot != null)
            {
                mainActionsRoot.SetActive(visible);
            }
        }

        private bool TryLaunchWebQaExpedition()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string url = Application.absoluteURL;
            if (url.IndexOf("qa-expedition-auto", StringComparison.OrdinalIgnoreCase) < 0 ||
                expeditionCatalog == null)
            {
                return false;
            }

            string requested = GetQueryValue(url, "qa-expedition");
            GassyExpeditionDefinition definition = expeditionCatalog.FindById(requested);
            int oneBasedIndex;
            if (definition == null && int.TryParse(requested, out oneBasedIndex))
            {
                definition = expeditionCatalog.GetByIndex(oneBasedIndex - 1);
            }

            if (definition == null || !ArcadeRunSession.SelectFinite(definition.ExpeditionId))
            {
                return false;
            }

            SceneManager.LoadScene(gameSceneName);
            return true;
#else
            return false;
#endif
        }

        private static string GetQueryValue(string url, string key)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            int queryStart = url.IndexOf('?');
            if (queryStart < 0 || queryStart >= url.Length - 1)
            {
                return string.Empty;
            }

            string[] pairs = url.Substring(queryStart + 1).Split('&');
            for (int i = 0; i < pairs.Length; i++)
            {
                string[] parts = pairs[i].Split(new[] { '=' }, 2);
                if (parts.Length > 0 &&
                    string.Equals(parts[0], key, StringComparison.OrdinalIgnoreCase))
                {
                    return parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "1";
                }
            }

            return string.Empty;
        }
    }
}
