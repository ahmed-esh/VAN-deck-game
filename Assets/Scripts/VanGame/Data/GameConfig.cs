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

        [Header("Driving time")]
        public float drivingDayRealTimeSeconds = 60f;
        public float idleTimeMultiplier = 0.05f;
        public float dailyFuelDrainPercent = 5f;
        public float unfedMoralePenaltyPercent = 50f;
        public float dietErUnfedMoralePenaltyPercent = 25f;

        [Header("Card play (DOTween)")]
        public float cardPlayOutDuration = 0.3f;
        public float cardDrawInDuration = 0.35f;
        public float cardDrawStartScale = 0.2f;
        public Ease cardPlayEase = Ease.InBack;
        public Ease cardDrawEase = Ease.OutBack;

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
