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
        [SerializeField] private Text expeditionTitleText;
        [SerializeField] private Text expeditionObjectiveText;
        [SerializeField] private Text expeditionStoryText;
        [SerializeField] private Text expeditionStatusText;
        [SerializeField] private Button expeditionPlayButton;

        private int selectedExpeditionIndex;

        public GassyExpeditionCatalog ExpeditionCatalog { get { return expeditionCatalog; } }
        public bool IsExpeditionUiConfigured
        {
            get
            {
                int count = expeditionCatalog != null ? expeditionCatalog.Count : 0;
                return count == 5 &&
                    mainActionsRoot != null &&
                    expeditionPanel != null &&
                    expeditionButtons != null && expeditionButtons.Length == count &&
                    expeditionButtonLabels != null && expeditionButtonLabels.Length == count &&
                    expeditionTitleText != null &&
                    expeditionObjectiveText != null &&
                    expeditionStoryText != null &&
                    expeditionStatusText != null &&
                    expeditionPlayButton != null;
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

            UpdateBestDistance();
            SetMainActionsVisible(true);
            SelectInitialExpedition();
            RefreshExpeditionPanel();
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
                return;
            }

            if (ArcadeRunSession.Mode == ArcadeRunMode.Finite)
            {
                GassyExpeditionDefinition selected = expeditionCatalog.FindById(ArcadeRunSession.ContentId);
                int selectedIndex = expeditionCatalog.IndexOf(selected);
                if (selectedIndex >= 0)
                {
                    selectedExpeditionIndex = selectedIndex;
                    return;
                }
            }

            selectedExpeditionIndex = Mathf.Clamp(
                GassyExpeditionProgressStore.GetHighestUnlockedIndex(),
                0,
                expeditionCatalog.Count - 1);
        }

        private void SelectExpedition(int index)
        {
            NotifyUserGesture();
            if (expeditionCatalog == null || expeditionCatalog.GetByIndex(index) == null ||
                !GassyExpeditionProgressStore.IsUnlocked(index))
            {
                return;
            }

            selectedExpeditionIndex = index;
            RefreshExpeditionPanel();
        }

        private void RefreshExpeditionPanel()
        {
            if (expeditionCatalog == null)
            {
                return;
            }

            for (int i = 0; i < expeditionCatalog.Count; i++)
            {
                GassyExpeditionDefinition definition = expeditionCatalog.GetByIndex(i);
                bool unlocked = GassyExpeditionProgressStore.IsUnlocked(i);
                int stars = definition != null
                    ? GassyExpeditionProgressStore.GetBestStars(definition.ExpeditionId)
                    : 0;

                if (expeditionButtons != null && i < expeditionButtons.Length && expeditionButtons[i] != null)
                {
                    expeditionButtons[i].interactable = unlocked;
                }

                if (expeditionButtonLabels != null && i < expeditionButtonLabels.Length &&
                    expeditionButtonLabels[i] != null && definition != null)
                {
                    string result = !unlocked
                        ? "LOCKED"
                        : (stars > 0 ? new string('*', stars) : "NEW");
                    expeditionButtonLabels[i].text = (i + 1) + "  " +
                        definition.DisplayTitle.ToUpperInvariant() + "   " + result;
                }
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

            if (expeditionStoryText != null)
            {
                expeditionStoryText.text = selected != null ? selected.OpeningStory : string.Empty;
            }

            if (expeditionStatusText != null)
            {
                expeditionStatusText.text = !selectedUnlocked
                    ? "LOCKED"
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
