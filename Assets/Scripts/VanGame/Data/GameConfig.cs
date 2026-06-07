using DG.Tweening;
using UnityEngine;

namespace VanGame.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Van Game/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Starting stats")]
        public int startingMoney = 500;
        [Range(0f, 100f)] public float startingFuelPercent = 100f;
        [Range(0f, 100f)] public float startingMoralePercent = 100f;
        [Range(0f, 100f)] public float startingVanConditionPercent = 100f;

        [Header("Trip limits")]
        public int maxTripDays = 20;

        [Header("Driving day (8-section bar)")]
        [Tooltip("Sections on the day timer bar. A full bar ends the driving day.")]
        public int drivingDaySectionCount = 8;

        [Tooltip("Real minutes for the bar to fill from idle drift only (no cards played).")]
        public float drivingDayIdleFillMinutes = 30f;

        public float IdleSectionsPerSecond =>
            drivingDaySectionCount / Mathf.Max(1f, drivingDayIdleFillMinutes * 60f);
        public float dailyFuelDrainPercent = 15f;
        public float dailyVanConditionDrainPercent = 5f;
        public float unfedMoralePenaltyPercent = 50f;
        public float dietErUnfedMoralePenaltyPercent = 25f;

        [Header("Driving parallax")]
        [Tooltip("Added to the active terrain parallax speed each time a card is played.")]
        public float parallaxCardPlaySpeedBoost = 3f;
        [Tooltip("How quickly parallax speed returns toward each terrain's base speed.")]
        public float parallaxSpeedDecayPerSecond = 1.5f;

        [Header("Card play (DOTween)")]
        [Tooltip("Multiplies card animation durations. 1.25 = 25% slower.")]
        public float cardAnimationDurationScale = 1.25f;
        public float cardPlayMoveToCenterDuration = 0.5f;
        public float cardPlayCenterHoldDuration = 0.15f;
        public float cardPlayVanishDuration = 0.56f;
        public float cardPlayCenterScale = 1.35f;
        public float cardPlaySpinDegrees = 360f;
        [Tooltip("Legacy alias; unused if Card Play Vanish Duration is set.")]
        public float cardPlayOutDuration = 0.3f;
        public float cardDrawInDuration = 0.44f;
        public float cardDrawStartScale = 0.2f;
        public Ease cardPlayEase = Ease.InBack;
        public Ease cardDrawEase = Ease.OutBack;

        [Header("Card deal (dealer throw)")]
        public float cardDealDuration = 0.56f;
        public float cardDealStagger = 0.088f;
        public Vector2 cardDealStartOffset = new Vector2(420f, 320f);
        public float cardDealStartRotation = 35f;
        public float cardDealStartScale = 0.25f;
        public Ease cardDealEase = Ease.OutCubic;

        [Header("Card draw from left (after play)")]
        public float cardDrawFromLeftDuration = 0.85f;
        public float cardDrawFromLeftOffset = 320f;
        public float cardDrawFromLeftStartScale = 0.55f;
        public Ease cardDrawFromLeftEase = Ease.OutCubic;

        [Header("Card inspect (press X)")]
        public float cardInspectScale = 2.1f;
        public float cardInspectDuration = 0.44f;
        public Ease cardInspectEase = Ease.OutBack;

        [Header("Canvas transitions")]
        public float canvasTransitionDuration = 0.4f;
        public Ease canvasTransitionEase = Ease.OutCubic;
        public float mapOpenFadeDuration = 0.35f;
        public float mapCloseFadeDuration = 0.3f;

        [Header("City selection cinematic")]
        public float citySelectShadeAlpha = 0.65f;
        public float citySelectShadeDuration = 0.35f;
        public float citySelectZoomScale = 1.35f;
        public float citySelectZoomDuration = 0.5f;
        public float citySelectHoldDuration = 0.4f;
        public Ease citySelectEase = Ease.InOutCubic;

        [Header("City arrival")]
        public int minRandomEventsPerCity = 1;
        public int maxRandomEventsPerCity = 2;
        public int abilityChoicesOffered = 3;

        [Header("City arrival UI (DOTween)")]
        public float eventLogLineFadeDuration = 0.35f;
        public float eventLogLineStagger = 0.12f;
        public float abilityCardInDuration = 0.4f;
        public float abilityCardInStartScale = 0.15f;
        public Ease abilityCardInEase = Ease.OutBack;
        public float winLoseFadeDuration = 0.45f;
        public Ease winLoseFadeEase = Ease.OutCubic;
    }
}
