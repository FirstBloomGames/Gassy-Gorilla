using System;
using UnityEngine;

namespace FirstBloom.ArcadeFramework.Core
{
    public class ArcadeGameStateController : MonoBehaviour
    {
        [SerializeField] private ArcadeGameState startingState = ArcadeGameState.Ready;

        public event Action<ArcadeGameState> StateChanged;

        public ArcadeGameState CurrentState { get; private set; }
        public bool IsRunning { get { return CurrentState == ArcadeGameState.Running; } }

        protected virtual void Awake()
        {
            CurrentState = startingState;
        }

        public void SetState(ArcadeGameState state)
        {
            if (CurrentState == state)
            {
                return;
            }

            CurrentState = state;
            if (StateChanged != null)
            {
                StateChanged.Invoke(CurrentState);
            }
        }
    }
}
