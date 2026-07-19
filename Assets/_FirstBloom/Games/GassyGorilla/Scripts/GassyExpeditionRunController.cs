using System.Collections;
using FirstBloom.ArcadeFramework.Accessibility;
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
        [SerializeField] private GameObject coachRoot;
        [SerializeField] private CanvasGroup coachGroup;
        [SerializeField] private Text coachText;
        [Min(1f)] [SerializeField] private float lessonCoachDuration = 4.2f;

        private GassyExpeditionDefinition definition;
        private int currentCount;
        private GassyInteractionType completedInteractions;
        private bool subscribed;
        private Coroutine coachRoutine;
        private bool coachIsUrgent;

        public GassyExpeditionDefinition Definition { get { return definition; } }
        public int CurrentCount { get { return currentCount; } }
        public ExpeditionFinishLine FinishLine { get { return finishLine; } }
        public bool IsConfigured
        {
            get
            {
                return player != null && finishLine != null && finishLine.IsConfigured &&
                    hudRoot != null && objectiveText != null && remainingText != null &&
                    coachRoot != null && coachGroup != null && coachText != null;
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            HideCoach();
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
            completedInteractions = GassyInteractionType.None;
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
            HideCoach();
        }

        public void ConfigureEndless()
        {
            definition = null;
            currentCount = 0;
            completedInteractions = GassyInteractionType.None;
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

            if (!visible)
            {
                HideCoach();
            }
        }

        public void BeginRunLesson()
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.LessonText))
            {
                return;
            }

            ShowCoach("LESSON  " + definition.LessonText, lessonCoachDuration, false);
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
                case GassyExpeditionObjectiveType.CompleteInteraction:
                    return currentCount >= definition.TargetCount;
                case GassyExpeditionObjectiveType.CompleteInteractionSet:
                    return (completedInteractions & definition.RequiredInteractions) ==
                        definition.RequiredInteractions;
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
                case GassyExpeditionObjectiveType.CompleteInteraction:
                    return InteractionLabel(definition.TargetInteraction) + "  " +
                        Mathf.Min(currentCount, definition.TargetCount) + " / " +
                        definition.TargetCount;
                case GassyExpeditionObjectiveType.CompleteInteractionSet:
                    int completed = CountInteractions(
                        completedInteractions & definition.RequiredInteractions);
                    int required = CountInteractions(definition.RequiredInteractions);
                    return "SKILLS  " + completed + " / " + required;
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
                case GassyExpeditionObjectiveType.CompleteInteraction:
                    currentCount = definition.TargetCount;
                    break;
                case GassyExpeditionObjectiveType.CompleteInteractionSet:
                    completedInteractions = definition.RequiredInteractions;
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
            GassyRunEvents.InteractionStarted += HandleInteractionStarted;
            GassyRunEvents.InteractionCompleted += HandleInteractionCompleted;
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
            GassyRunEvents.InteractionStarted -= HandleInteractionStarted;
            GassyRunEvents.InteractionCompleted -= HandleInteractionCompleted;
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

        private void HandleInteractionStarted(GassyInteractionType interactionType)
        {
            if (definition == null || interactionType != GassyInteractionType.SapEscape)
            {
                return;
            }

            ShowCoach("STUCK IN SAP  -  TAP TO POP FREE", 0f, true);
        }

        private void HandleInteractionCompleted(GassyInteractionType interactionType)
        {
            if (definition == null || interactionType == GassyInteractionType.None)
            {
                return;
            }

            bool changed = false;
            if (definition.ObjectiveType == GassyExpeditionObjectiveType.CompleteInteraction &&
                interactionType == definition.TargetInteraction)
            {
                currentCount++;
                changed = true;
            }
            else if (definition.ObjectiveType == GassyExpeditionObjectiveType.CompleteInteractionSet &&
                (definition.RequiredInteractions & interactionType) != 0)
            {
                GassyInteractionType before = completedInteractions;
                completedInteractions |= interactionType;
                changed = before != completedInteractions;
            }

            if (interactionType == GassyInteractionType.SapEscape && coachIsUrgent)
            {
                ShowCoach("POP!  FREE AND FLYING", 1.15f, false);
            }
            else if (changed)
            {
                ShowCoach(InteractionSuccessText(interactionType), 1.05f, false);
            }

            if (changed)
            {
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

        private void ShowCoach(string message, float duration, bool urgent)
        {
            if (coachRoot == null || coachGroup == null || coachText == null)
            {
                return;
            }

            if (coachRoutine != null)
            {
                StopCoroutine(coachRoutine);
                coachRoutine = null;
            }

            coachRoot.SetActive(true);
            coachText.text = message;
            coachIsUrgent = urgent;
            if (urgent || duration <= 0f)
            {
                coachGroup.alpha = 1f;
                return;
            }

            coachRoutine = StartCoroutine(CoachRoutine(duration));
        }

        private IEnumerator CoachRoutine(float duration)
        {
            float transition = ArcadeAccessibilitySettings.ReducedMotion ? 0f : 0.14f;
            coachGroup.alpha = transition <= 0f ? 1f : 0f;

            float elapsed = 0f;
            while (elapsed < transition)
            {
                elapsed += Time.deltaTime;
                coachGroup.alpha = Mathf.Clamp01(elapsed / transition);
                yield return null;
            }

            coachGroup.alpha = 1f;
            yield return new WaitForSeconds(Mathf.Max(0.1f, duration));

            elapsed = 0f;
            while (elapsed < transition)
            {
                elapsed += Time.deltaTime;
                coachGroup.alpha = 1f - Mathf.Clamp01(elapsed / transition);
                yield return null;
            }

            HideCoach();
        }

        private void HideCoach()
        {
            if (coachRoutine != null)
            {
                StopCoroutine(coachRoutine);
                coachRoutine = null;
            }

            coachIsUrgent = false;
            if (coachGroup != null)
            {
                coachGroup.alpha = 0f;
            }

            if (coachRoot != null)
            {
                coachRoot.SetActive(false);
            }
        }

        private static int CountInteractions(GassyInteractionType interactions)
        {
            int value = (int)interactions;
            int count = 0;
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }

            return count;
        }

        private static string InteractionLabel(GassyInteractionType interactionType)
        {
            switch (interactionType)
            {
                case GassyInteractionType.ThornDodge:
                    return "THORNS";
                case GassyInteractionType.GeyserDodge:
                    return "GEYSERS";
                case GassyInteractionType.SapEscape:
                    return "SAP ESCAPES";
                case GassyInteractionType.UpdraftRide:
                    return "UPDRAFTS";
                default:
                    return "SKILLS";
            }
        }

        private static string InteractionSuccessText(GassyInteractionType interactionType)
        {
            switch (interactionType)
            {
                case GassyInteractionType.ThornDodge:
                    return "CLEAN STUMP CLEAR";
                case GassyInteractionType.GeyserDodge:
                    return "GEYSER DODGED";
                case GassyInteractionType.SapEscape:
                    return "SAP ESCAPED";
                case GassyInteractionType.UpdraftRide:
                    return "UPDRAFT CAUGHT";
                default:
                    return "LESSON COMPLETE";
            }
        }
    }
}
