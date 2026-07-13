using FirstBloom.ArcadeFramework.Audio;
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

            UpdateBestDistance();
        }

        public void Play()
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.UiClick);
            }

            SceneManager.LoadScene(gameSceneName);
        }

        public void OpenSettings()
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.UiClick);
            }

            if (settingsMenu != null)
            {
                settingsMenu.Open();
            }
        }

        public void CloseSettings()
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.UiClick);
            }

            if (settingsMenu != null)
            {
                settingsMenu.Close();
            }
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
    }
}
