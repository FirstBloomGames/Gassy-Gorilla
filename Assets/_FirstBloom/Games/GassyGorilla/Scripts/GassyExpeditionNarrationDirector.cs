using FirstBloom.ArcadeFramework.Accessibility;
using FirstBloom.ArcadeFramework.Audio;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public sealed class GassyExpeditionNarrationDirector : MonoBehaviour
    {
        private const string OpeningMoment = "opening";
        private const string LessonMoment = "lesson";
        private const string SuccessMoment = "success";
        private const string HintMoment = "hint";

        [SerializeField] private ArcadeSubtitlePresenter subtitlePresenter;

        private GassyExpeditionDefinition definition;

        public bool IsConfigured
        {
            get
            {
                return subtitlePresenter != null &&
                    subtitlePresenter.IsConfigured;
            }
        }

        public void Configure(GassyExpeditionDefinition expedition)
        {
            definition = expedition;
            if (subtitlePresenter != null)
            {
                subtitlePresenter.Hide();
            }
        }

        public void PlayOpeningIfNew()
        {
            PlayOnce(
                definition != null ? definition.OpeningVoice : null,
                OpeningMoment);
        }

        public void ReplayOpening()
        {
            Play(
                definition != null ? definition.OpeningVoice : null,
                OpeningMoment,
                true);
        }

        public void PlayLessonIfNew()
        {
            PlayOnce(
                definition != null ? definition.LessonVoice : null,
                LessonMoment);
        }

        public void PlaySuccessIfNew()
        {
            PlayOnce(
                definition != null ? definition.SuccessVoice : null,
                SuccessMoment);
        }

        public string RecordFailureAndGetAdaptiveHint()
        {
            if (definition == null)
            {
                return string.Empty;
            }

            int attempt =
                GassyExpeditionProgressStore.RecordFailure(definition);
            if (attempt < 2 || (attempt - 2) % 2 != 0)
            {
                return string.Empty;
            }

            return definition.FailureHintText;
        }

        public void PlayAdaptiveFailureHint()
        {
            Play(
                definition != null ? definition.FailureHintVoice : null,
                HintMoment,
                true);
        }

        public void ClearFailureCount()
        {
            if (definition != null)
            {
                GassyExpeditionProgressStore.ClearFailures(definition);
            }
        }

        private void PlayOnce(
            GassyExpeditionVoiceMoment moment,
            string momentId)
        {
            Play(moment, momentId, false);
        }

        private void Play(
            GassyExpeditionVoiceMoment moment,
            string momentId,
            bool force)
        {
            if (definition == null || moment == null)
            {
                return;
            }

            if (!force &&
                GassyExpeditionProgressStore.HasHeardVoice(
                    definition,
                    momentId))
            {
                return;
            }

            float duration = moment.Clip != null
                ? moment.Clip.length + 0.25f
                : moment.TextDuration;
            if (moment.Clip != null &&
                ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayVoice(
                    moment.Clip,
                    moment.Volume);
            }

            if (moment.Clip != null &&
                subtitlePresenter != null)
            {
                subtitlePresenter.Show(moment.Subtitle, duration);
            }

            if (moment.Clip != null)
            {
                GassyExpeditionProgressStore.MarkVoiceHeard(
                    definition,
                    momentId);
            }
        }
    }
}
