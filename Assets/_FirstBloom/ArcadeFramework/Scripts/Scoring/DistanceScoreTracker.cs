using System;
using UnityEngine;

namespace FirstBloom.ArcadeFramework.Scoring
{
    public class DistanceScoreTracker : MonoBehaviour
    {
        [SerializeField] private Transform distanceTarget;
        [SerializeField] private float distanceScale = 1f;
        [SerializeField] private bool useTargetX = true;
        [SerializeField] private float simulatedSpeed = 4f;
        [SerializeField] private bool runningOnStart;

        private float startX;
        private float simulatedDistance;

        public event Action<float> DistanceChanged;

        public float Distance { get; private set; }
        public bool IsRunning { get; private set; }

        public Transform DistanceTarget
        {
            get { return distanceTarget; }
            set { distanceTarget = value; }
        }

        private void Awake()
        {
            ResetDistance();
            IsRunning = runningOnStart;
        }

        private void Start()
        {
            if (DistanceChanged != null)
            {
                DistanceChanged.Invoke(Distance);
            }
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            if (useTargetX && distanceTarget != null)
            {
                Distance = Mathf.Max(0f, (distanceTarget.position.x - startX) * distanceScale);
            }
            else
            {
                simulatedDistance += simulatedSpeed * Time.deltaTime * distanceScale;
                Distance = simulatedDistance;
            }

            if (DistanceChanged != null)
            {
                DistanceChanged.Invoke(Distance);
            }
        }

        public void SetRunning(bool running)
        {
            IsRunning = running;
        }

        public void ResetDistance()
        {
            startX = distanceTarget != null ? distanceTarget.position.x : 0f;
            simulatedDistance = 0f;
            Distance = 0f;

            if (DistanceChanged != null)
            {
                DistanceChanged.Invoke(Distance);
            }
        }
    }
}
