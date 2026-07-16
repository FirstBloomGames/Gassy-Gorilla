using System.Collections.Generic;
using FirstBloom.ArcadeFramework.Audio;
using FirstBloom.ArcadeFramework.UI;
using UnityEngine;

namespace FirstBloom.Games.GassyGorilla
{
    public class MilestoneEventManager : MonoBehaviour
    {
        [SerializeField] private GassyScoreManager scoreManager;
        [SerializeField] private TextOverlay textOverlay;
        [SerializeField] private float textDuration = 2.3f;
        [SerializeField] private MilestoneEvent[] milestones;

        private readonly HashSet<int> triggeredMilestones = new HashSet<int>();

        private void OnEnable()
        {
            if (scoreManager != null)
            {
                scoreManager.DistanceChanged += HandleDistanceChanged;
            }
        }

        private void OnDisable()
        {
            if (scoreManager != null)
            {
                scoreManager.DistanceChanged -= HandleDistanceChanged;
            }
        }

        public void ResetMilestones()
        {
            triggeredMilestones.Clear();
        }

        private void HandleDistanceChanged(float distance)
        {
            if (milestones == null)
            {
                return;
            }

            for (int i = 0; i < milestones.Length; i++)
            {
                MilestoneEvent milestone = milestones[i];
                if (milestone == null || triggeredMilestones.Contains(i) || distance < milestone.distance)
                {
                    continue;
                }

                triggeredMilestones.Add(i);

                if (textOverlay != null)
                {
                    textOverlay.Show(milestone.line, textDuration);
                }

                if (ArcadeAudioManager.Instance != null)
                {
                    ArcadeAudioManager.Instance.PlaySfx(ArcadeSfxType.Milestone, 0.66f);
                    ArcadeAudioManager.Instance.PlayVoice(milestone.voiceClip);
                }
            }
        }
    }
}
