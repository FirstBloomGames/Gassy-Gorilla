using System;
using FirstBloom.ArcadeFramework.Scoring;
using UnityEngine;
using UnityEngine.UI;

namespace FirstBloom.Games.GassyGorilla
{
    public class GassyScoreManager : MonoBehaviour
    {
        [SerializeField] private DistanceScoreTracker distanceTracker;
        [SerializeField] private Text scoreText;

        public event Action<float> DistanceChanged;

        public float Distance
        {
            get { return distanceTracker != null ? distanceTracker.Distance : 0f; }
        }

        private void Awake()
        {
            if (distanceTracker == null)
            {
                distanceTracker = GetComponent<DistanceScoreTracker>();
            }
        }

        private void OnEnable()
        {
            if (distanceTracker != null)
            {
                distanceTracker.DistanceChanged += HandleDistanceChanged;
            }
        }

        private void OnDisable()
        {
            if (distanceTracker != null)
            {
                distanceTracker.DistanceChanged -= HandleDistanceChanged;
            }
        }

        public void SetRunning(bool running)
        {
            if (distanceTracker != null)
            {
                distanceTracker.SetRunning(running);
            }
        }

        public void ResetScore()
        {
            if (distanceTracker != null)
            {
                distanceTracker.ResetDistance();
            }

            HandleDistanceChanged(0f);
        }

        private void HandleDistanceChanged(float distance)
        {
            if (scoreText != null)
            {
                scoreText.text = Mathf.FloorToInt(distance) + " m";
            }

            if (DistanceChanged != null)
            {
                DistanceChanged.Invoke(distance);
            }
        }
    }
}
