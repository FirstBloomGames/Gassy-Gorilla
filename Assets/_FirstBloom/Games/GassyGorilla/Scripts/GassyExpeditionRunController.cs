using UnityEngine;
using UnityEngine.UI;

namespace FirstBloom.Games.GassyGorilla
{
    public sealed class GassyExpeditionRunController : MonoBehaviour
    {
        [SerializeField] private GorillaController player;
        [SerializeField] private ExpeditionFinishLine finishLine;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text remainingText;

        private GassyExpeditionDefinition definition;
        private int currentCount;
        private bool subscribed;

        public GassyExpeditionDefinition Definition { get { return definition; } }
        public int CurrentCount { get { return currentCount; } }
        public ExpeditionFinishLine FinishLine { get { return finishLine; } }
        public bool IsConfigured
        {
            get
            {
                return player != null && finishLine != null && finishLine.IsConfigured &&
                    hudRoot != null && objectiveText != null && remainingText != null;
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (definition == null || player == null || finishLine == null || !finishLine.gameObject.activeSelf)
            {
                return;
            }

            if (remainingText != null)
            {
                float remaining = Mathf.Max(0f, finishLine.WorldX - player.transform.position.x);
                remainingText.text = Mathf.CeilToInt(remaining) + " m TO FINISH";
            }

            if (definition.ObjectiveType == GassyExpeditionObjectiveType.FinishWithFuel)
            {
                RefreshObjectiveText();
            }
        }

        public void Configure(
            GassyExpeditionDefinition expedition,
            float finishWorldX)
        {
            definition = expedition;
            currentCount = 0;
            Subscribe();

            if (hudRoot != null)
            {
                hudRoot.SetActive(definition != null);
            }

            if (finishLine != null)
            {
                if (definition != null)
                {
                    finishLine.Configure(finishWorldX);
                }
                else
                {
                    finishLine.gameObject.SetActive(false);
                }
            }

            RefreshObjectiveText();
        }

        public void ConfigureEndless()
        {
            definition = null;
            currentCount = 0;
            SetHudVisible(false);

            if (finishLine != null)
            {
                finishLine.gameObject.SetActive(false);
            }
        }

        public void SetHudVisible(bool visible)
        {
            if (hudRoot != null)
            {
                hudRoot.SetActive(visible && definition != null);
            }
        }

        public bool IsObjectiveSatisfiedAtFinish()
        {
            if (definition == null)
            {
                return false;
            }

            switch (definition.ObjectiveType)
            {
                case GassyExpeditionObjectiveType.ReachFinish:
                    return true;
                case GassyExpeditionObjectiveType.CollectFood:
                case GassyExpeditionObjectiveType.VineReleases:
                case GassyExpeditionObjectiveType.CrocodileDodges:
                    return currentCount >= definition.TargetCount;
                case GassyExpeditionObjectiveType.FinishWithFuel:
                    return player != null && player.CurrentFuel + 0.01f >= definition.TargetFuel;
                default:
                    return false;
            }
        }

        public string GetProgressSummary()
        {
            if (definition == null)
            {
                return string.Empty;
            }

            switch (definition.ObjectiveType)
            {
                case GassyExpeditionObjectiveType.CollectFood:
                    return "FOOD  " + Mathf.Min(currentCount, definition.TargetCount) + " / " + definition.TargetCount;
                case GassyExpeditionObjectiveType.VineReleases:
                    return "VINES  " + Mathf.Min(currentCount, definition.TargetCount) + " / " + definition.TargetCount;
                case GassyExpeditionObjectiveType.CrocodileDodges:
                    return "CROCODILES  " + Mathf.Min(currentCount, definition.TargetCount) + " / " + definition.TargetCount;
                case GassyExpeditionObjectiveType.FinishWithFuel:
                    int fuel = player != null ? Mathf.FloorToInt(player.CurrentFuel) : 0;
                    return "FUEL  " + fuel + " / " + Mathf.CeilToInt(definition.TargetFuel);
                default:
                    return "REACH THE FINISH";
            }
        }

        public void CompleteObjectiveForQa()
        {
            if (definition == null)
            {
                return;
            }

            switch (definition.ObjectiveType)
            {
                case GassyExpeditionObjectiveType.CollectFood:
                case GassyExpeditionObjectiveType.VineReleases:
                case GassyExpeditionObjectiveType.CrocodileDodges:
                    currentCount = definition.TargetCount;
                    break;
                case GassyExpeditionObjectiveType.FinishWithFuel:
                    if (player != null)
                    {
                        player.RefillFuel(100f, false);
                    }
                    break;
            }

            RefreshObjectiveText();
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            GassyRunEvents.FoodCollected += HandleFoodCollected;
            GassyRunEvents.CrocodileDodged += HandleCrocodileDodged;
            if (player != null)
            {
                player.VineReleased += HandleVineReleased;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            GassyRunEvents.FoodCollected -= HandleFoodCollected;
            GassyRunEvents.CrocodileDodged -= HandleCrocodileDodged;
            if (player != null)
            {
                player.VineReleased -= HandleVineReleased;
            }

            subscribed = false;
        }

        private void HandleFoodCollected(FoodPickupType pickupType)
        {
            if (definition != null &&
                definition.ObjectiveType == GassyExpeditionObjectiveType.CollectFood)
            {
                currentCount++;
                RefreshObjectiveText();
            }
        }

        private void HandleVineReleased()
        {
            if (definition != null &&
                definition.ObjectiveType == GassyExpeditionObjectiveType.VineReleases)
            {
                currentCount++;
                RefreshObjectiveText();
            }
        }

        private void HandleCrocodileDodged()
        {
            if (definition != null &&
                definition.ObjectiveType == GassyExpeditionObjectiveType.CrocodileDodges)
            {
                currentCount++;
                RefreshObjectiveText();
            }
        }

        private void RefreshObjectiveText()
        {
            if (objectiveText == null)
            {
                return;
            }

            objectiveText.text = definition == null
                ? string.Empty
                : definition.ObjectiveText + "   " + GetProgressSummary();
        }
    }
}
