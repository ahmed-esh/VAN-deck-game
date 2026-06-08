using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using VanGame;
using VanGame.Audio;
using VanGame.Data;
using VanGame.UI;

/// <summary>
/// Detects mouse hover over the hand area and tweens child cards into a fanned semicircle.
/// When the fan is open, the card under the pointer is brought forward, straightened, and highlighted.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(RectTransform))]
public class CardHandHoverFan : MonoBehaviour
{
    [Header("Cards")]
    [Tooltip("Card RectTransforms to animate. Leave empty to use direct children (excluding this transform).")]
    [SerializeField] RectTransform[] cards;

    [SerializeField] bool collectCardsFromChildren = true;

    [Header("Hidden / revealed Y")]
    [SerializeField] float hiddenAnchoredY = -100f;
    [SerializeField] float revealedBaseY = 0f;

    [Header("Fan layout")]
    [SerializeField] float horizontalSpacing = 70f;
    [SerializeField] float arcHeight = 35f;
    [SerializeField] float maxRotation = 22f;

    [Header("Card focus (hover one card in the fan)")]
    [SerializeField] float focusSpreadPush = 30f;
    [SerializeField] float focusLift = 45f;
    [SerializeField] float focusScale = 1.18f;
    [SerializeField] float focusTweenDuration = 0.275f;
    [SerializeField] Ease focusTweenEase = Ease.OutCubic;

    [Header("Long hover magnify")]
    [SerializeField] float longHoverDelay = 1f;
    [SerializeField] float longHoverScale = 2.5f;
    [FormerlySerializedAs("longHoverTweenDuration")]
    [SerializeField] float longHoverGrowDuration = 0.45f;
    [SerializeField] float longHoverShrinkDuration = 0.22f;
    [FormerlySerializedAs("longHoverEase")]
    [SerializeField] Ease longHoverGrowEase = Ease.OutBack;
    [SerializeField] Ease longHoverShrinkEase = Ease.OutCubic;

    [Header("Animation")]
    [SerializeField] float tweenDuration = 0.44f;
    [SerializeField] Ease tweenEase = Ease.OutCubic;

    [Tooltip("When fanning, later siblings draw on top (right-over-left overlap).")]
    [SerializeField] bool raiseSiblingOrderWhenFanned = true;

    [Header("Hover detection")]
    [SerializeField] Vector2 hoverPadding = Vector2.zero;
    [SerializeField] bool syncBoxColliderToRect = true;

    [Header("Driving wheel")]
    [Tooltip("Steering wheel sprite to animate while the pointer is over the hand area.")]
    [SerializeField] RectTransform drivingWheel;
    [SerializeField] DrivingWheelAnimationMode drivingWheelMode = DrivingWheelAnimationMode.Rock;
    [SerializeField] float drivingWheelSpinDuration = 0.35f;
    [SerializeField] float drivingWheelRockAngle = 14f;
    [SerializeField] float drivingWheelRockHalfDuration = 0.2f;
    [SerializeField] Ease drivingWheelEase = Ease.InOutSine;

    public enum DrivingWheelAnimationMode
    {
        Rock = 0,
        Spin = 1
    }

    RectTransform _handRect;
    BoxCollider2D _boxCollider;
    Camera _eventCamera;
    bool _pointerInside;
    bool _isFanned;
    bool _inspectMode;
    bool _focusEnabled;
    bool _awaitingDrawnCard;
    int _focusedCardIndex = -1;
    int _pendingLayoutTweens;
    int _layoutGeneration;
    int _maxHandSlots = 8;

    float _longHoverTimer;
    int _longHoverTrackedIndex = -1;
    bool _longHoverMagnified;
    RectTransform _longHoverCard;

    Vector2[] _restAnchoredPositions;
    Vector3[] _restLocalEulerAngles;
    Vector2[] _fanAnchoredPositions;
    Vector3[] _fanLocalEulerAngles;
    int[] _originalSiblingIndices;
    Vector2[] _slotFanPositions;
    Vector3[] _slotFanRotations;

    Vector3 _drivingWheelRestRotation;
    Tween _drivingWheelTween;
    bool _drivingWheelAnimating;
    GameFlowController _gameFlow;

    public bool IsFanned => _isFanned;
    public bool CanFocusCards => _focusEnabled && _isFanned && !_inspectMode && !_awaitingDrawnCard;
    public int FocusedCardIndex => _focusedCardIndex;
    public float HiddenAnchoredY => hiddenAnchoredY;
    public float HorizontalSpacing => horizontalSpacing;

    public CardView GetFocusedCardView()
    {
        if (!CanFocusCards || _focusedCardIndex < 0 || cards == null || _focusedCardIndex >= cards.Length)
            return null;

        RectTransform rt = cards[_focusedCardIndex];
        return rt != null ? rt.GetComponent<CardView>() : null;
    }

    public void SetInspectMode(bool inspecting)
    {
        _inspectMode = inspecting;
        if (inspecting)
        {
            ResetLongHoverMagnify(immediate: true);
            _focusedCardIndex = -1;
        }
    }

    /// <summary>Fixed slot count for the current driving leg; fan positions are keyed by HandSlot, not sibling order.</summary>
    public void BeginRound(int maxSlots)
    {
        _maxHandSlots = Mathf.Max(1, maxSlots);
        BuildSlotLayoutTable();
    }

    /// <summary>Blocks per-card fan highlight until the replacement card has finished drawing in.</summary>
    public void SetAwaitingDrawnCard(bool awaiting)
    {
        _awaitingDrawnCard = awaiting;

        if (!awaiting)
        {
            RefreshFocusEnabled();
            TryApplyFocusAfterDraw();
            return;
        }

        ResetLongHoverMagnify(immediate: true);
        _focusedCardIndex = -1;
    }

    void RefreshFocusEnabled()
    {
        _focusEnabled = _isFanned && !_inspectMode && !_awaitingDrawnCard;
    }

    void TryApplyFocusAfterDraw()
    {
        CollectCardsFromChildren();
        CacheLayoutTargets();
        RefreshFocusEnabled();

        if (!CanFocusCards)
            return;

        SanitizeFocusedIndex();

        int underPointer = GetCardIndexUnderPointer();
        _focusedCardIndex = underPointer;
        _longHoverTimer = 0f;
        _longHoverTrackedIndex = underPointer;
        if (_focusedCardIndex >= 0)
            ApplyCurrentFanLayout(false, lockFocusDuringTween: false);
    }

    void Awake()
    {
        if (_gameFlow == null)
            _gameFlow = FindFirstObjectByType<GameFlowController>();

        _handRect = (RectTransform)transform;
        _boxCollider = GetComponent<BoxCollider2D>();
        Canvas rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            _eventCamera = rootCanvas.worldCamera;

        if (syncBoxColliderToRect)
            SyncBoxColliderToRect();

        if (collectCardsFromChildren || cards == null || cards.Length == 0)
            CollectCardsFromChildren();

        BuildSlotLayoutTable();
        CacheLayoutTargets();
        ApplyLayout(_restAnchoredPositions, _restLocalEulerAngles, null, immediate: true, lockFocusDuringTween: false);
        _focusEnabled = false;
        CacheDrivingWheelRestRotation();
    }

    void Update()
    {
        if (_inspectMode)
            return;

        bool hoverAllowed = IsHandHoverAllowed();
        if (_boxCollider != null)
            _boxCollider.enabled = hoverAllowed;

        if (!hoverAllowed)
        {
            ForceCollapseIfActive();
            return;
        }

        UpdateHover();
        UpdateCardFocus();
        UpdateLongHoverMagnify();
    }

    bool IsHandHoverAllowed()
    {
        if (_gameFlow?.RunState == null)
            return true;

        GamePhase phase = _gameFlow.RunState.Phase;
        return phase != GamePhase.MapOpen && phase != GamePhase.MapSelectingDestination;
    }

    void ForceCollapseIfActive()
    {
        if (!_pointerInside && !_isFanned)
            return;

        _pointerInside = false;
        _focusedCardIndex = -1;
        ResetLongHoverMagnify(immediate: false);
        StopDrivingWheelAnimation();

        if (_isFanned)
            Collapse();
    }

    void UpdateHover()
    {
        if (_handRect == null)
            return;

        bool inside = IsPointerOverHandRect();

        if (inside && !_pointerInside)
        {
        _pointerInside = true;
        GameSfxController.TryPlayCardShuffle();
        FanOut();
        }
        else if (!inside && _pointerInside)
        {
            _pointerInside = false;
            ResetLongHoverMagnify(immediate: false);
            _focusedCardIndex = -1;
            Collapse();
        }
    }

    void UpdateCardFocus()
    {
        if (!CanFocusCards || cards == null || cards.Length == 0)
        {
            ResetLongHoverMagnify(immediate: false);
            return;
        }

        SanitizeFocusedIndex();

        int underPointer = GetCardIndexUnderPointer();
        if (underPointer == _focusedCardIndex)
            return;

        ResetLongHoverMagnify(immediate: false);
        _focusedCardIndex = underPointer;
        _longHoverTimer = 0f;
        _longHoverTrackedIndex = underPointer;
        ApplyCurrentFanLayout(false, lockFocusDuringTween: false);
    }

    void UpdateLongHoverMagnify()
    {
        if (!CanFocusCards || !IsFocusedIndexValid())
        {
            if (_longHoverMagnified)
                ResetLongHoverMagnify(immediate: false);
            return;
        }

        if (_focusedCardIndex != _longHoverTrackedIndex)
        {
            _longHoverTimer = 0f;
            _longHoverTrackedIndex = _focusedCardIndex;
            _longHoverMagnified = false;
            _longHoverCard = null;
        }

        _longHoverTimer += Time.deltaTime;
        if (_longHoverTimer < longHoverDelay || _longHoverMagnified)
            return;

        RectTransform card = cards[_focusedCardIndex];
        if (card == null)
            return;

        _longHoverMagnified = true;
        _longHoverCard = card;
        card.DOKill(false);
        card.DOScale(longHoverScale, longHoverGrowDuration)
            .SetEase(longHoverGrowEase)
            .SetLink(card.gameObject, LinkBehaviour.KillOnDestroy);
    }

    void ResetLongHoverMagnify(bool immediate)
    {
        if (_longHoverCard != null)
        {
            _longHoverCard.DOKill(false);
            float targetScale = GetLayoutScaleForCard(_longHoverCard);
            if (immediate)
                _longHoverCard.localScale = Vector3.one * targetScale;
            else
            {
                _longHoverCard.DOScale(targetScale, longHoverShrinkDuration)
                    .SetEase(longHoverShrinkEase)
                    .SetLink(_longHoverCard.gameObject, LinkBehaviour.KillOnDestroy);
            }
        }

        _longHoverMagnified = false;
        _longHoverTimer = 0f;
        _longHoverTrackedIndex = -1;
        _longHoverCard = null;
    }

    float GetLayoutScaleForCard(RectTransform card)
    {
        if (!IsFocusedIndexValid() || cards[_focusedCardIndex] != card)
            return 1f;

        return focusScale;
    }

    bool IsFocusedIndexValid()
    {
        return _focusedCardIndex >= 0
            && cards != null
            && _focusedCardIndex < cards.Length
            && _fanAnchoredPositions != null
            && _focusedCardIndex < _fanAnchoredPositions.Length;
    }

    void SanitizeFocusedIndex()
    {
        if (!IsFocusedIndexValid())
            _focusedCardIndex = -1;
    }

    void EnsureLayoutCacheMatchesCards()
    {
        if (cards == null)
            return;

        if (_fanAnchoredPositions == null
            || _restAnchoredPositions == null
            || _fanAnchoredPositions.Length != cards.Length
            || _restAnchoredPositions.Length != cards.Length)
        {
            CacheLayoutTargets();
        }
    }

    int GetCardIndexUnderPointer()
    {
        if (cards == null || cards.Length == 0)
            return -1;

        Vector2 screenPoint = Input.mousePosition;

        for (int i = cards.Length - 1; i >= 0; i--)
        {
            RectTransform card = cards[i];
            if (card == null || !card.gameObject.activeInHierarchy)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(card, screenPoint, _eventCamera))
                return i;
        }

        return -1;
    }

    bool IsPointerOverHandRect()
    {
        Vector2 screenPoint = Input.mousePosition;

        if (hoverPadding.sqrMagnitude > 0f)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _handRect, screenPoint, _eventCamera, out Vector2 localPoint))
                return false;

            Rect rect = _handRect.rect;
            rect.xMin -= hoverPadding.x;
            rect.xMax += hoverPadding.x;
            rect.yMin -= hoverPadding.y;
            rect.yMax += hoverPadding.y;
            return rect.Contains(localPoint);
        }

        return RectTransformUtility.RectangleContainsScreenPoint(_handRect, screenPoint, _eventCamera);
    }

    void CacheDrivingWheelRestRotation()
    {
        if (drivingWheel != null)
            _drivingWheelRestRotation = drivingWheel.localEulerAngles;
    }

    void StartDrivingWheelAnimation()
    {
        if (drivingWheel == null || _drivingWheelAnimating)
            return;

        _drivingWheelAnimating = true;
        drivingWheel.DOKill();
        CacheDrivingWheelRestRotation();
        drivingWheel.localEulerAngles = _drivingWheelRestRotation;

        if (drivingWheelMode == DrivingWheelAnimationMode.Spin)
        {
            _drivingWheelTween = drivingWheel
                .DORotate(
                    new Vector3(_drivingWheelRestRotation.x, _drivingWheelRestRotation.y, _drivingWheelRestRotation.z - 360f),
                    drivingWheelSpinDuration,
                    RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetLink(drivingWheel.gameObject, LinkBehaviour.KillOnDestroy);
            return;
        }

        Vector3 rocked = _drivingWheelRestRotation + new Vector3(0f, 0f, drivingWheelRockAngle);
        _drivingWheelTween = drivingWheel
            .DOLocalRotate(rocked, drivingWheelRockHalfDuration)
            .SetEase(drivingWheelEase)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(drivingWheel.gameObject, LinkBehaviour.KillOnDestroy);
    }

    void StopDrivingWheelAnimation()
    {
        if (drivingWheel == null)
            return;

        _drivingWheelAnimating = false;

        if (_drivingWheelTween != null)
        {
            _drivingWheelTween.Kill();
            _drivingWheelTween = null;
        }

        drivingWheel.DOKill();
        drivingWheel.localEulerAngles = _drivingWheelRestRotation;
    }

    void SyncBoxColliderToRect()
    {
        if (_boxCollider == null || _handRect == null)
            return;

        Rect rect = _handRect.rect;
        _boxCollider.size = rect.size;

        Vector2 pivot = _handRect.pivot;
        _boxCollider.offset = new Vector2(
            (0.5f - pivot.x) * rect.width,
            (0.5f - pivot.y) * rect.height);
    }

    void OnDisable()
    {
        _pointerInside = false;
        _focusedCardIndex = -1;
        _focusEnabled = false;
        ResetLongHoverMagnify(immediate: true);
        StopDrivingWheelAnimation();
        KillCardTweens();

        if (!_isFanned)
            return;

        _isFanned = false;
        ApplyLayout(_restAnchoredPositions, _restLocalEulerAngles, null, immediate: true, lockFocusDuringTween: false);
    }

    public void RefreshFromChildren()
    {
        CollectCardsFromChildren();
        CacheLayoutTargets();
        _focusedCardIndex = -1;
        ApplyCurrentFanLayout(_isFanned ? false : true, lockFocusDuringTween: true);
    }

    public void SyncCardListOnly()
    {
        CollectCardsFromChildren();
        CacheLayoutTargets();
    }

    /// <summary>After a card leaves the hand, refresh the card list but keep remaining cards at their fixed slots.</summary>
    public void SyncSlotsAfterRemoval()
    {
        ResetLongHoverMagnify(immediate: true);
        _focusedCardIndex = -1;
        CollectCardsFromChildren();
        CacheLayoutTargets();

        if (_isFanned)
            ApplyLayout(_fanAnchoredPositions, _fanLocalEulerAngles, null, immediate: false, lockFocusDuringTween: true);
    }

    /// <summary>Legacy alias — slot positions no longer reflow when a card is played.</summary>
    public void ReflowAfterCardRemoved() => SyncSlotsAfterRemoval();

    public bool TryGetLayoutTargetForCard(RectTransform card, out Vector2 anchoredPosition, out Vector3 localEulerAngles)
    {
        CollectCardsFromChildren();
        CacheLayoutTargets();

        anchoredPosition = new Vector2(0f, hiddenAnchoredY);
        localEulerAngles = Vector3.zero;

        if (cards == null || card == null)
            return false;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] != card)
                continue;

            if (_isFanned)
            {
                anchoredPosition = _fanAnchoredPositions[i];
                localEulerAngles = _fanLocalEulerAngles[i];
            }
            else
            {
                anchoredPosition = _restAnchoredPositions[i];
                localEulerAngles = _restLocalEulerAngles[i];
            }

            return true;
        }

        return false;
    }

    /// <summary>Moves one card into its slot for the current fan or collapsed layout.</summary>
    public void EnjoinCardToCurrentLayout(RectTransform card, bool immediate, System.Action onComplete = null)
    {
        if (card == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (!TryGetLayoutTargetForCard(card, out Vector2 pos, out Vector3 rot))
        {
            onComplete?.Invoke();
            return;
        }

        card.DOKill(true);

        if (immediate)
        {
            card.anchoredPosition = pos;
            card.localEulerAngles = rot;
            card.localScale = Vector3.one;
            onComplete?.Invoke();
            return;
        }

        float duration = tweenDuration;
        card.DOAnchorPos(pos, duration).SetEase(tweenEase).SetLink(card.gameObject, LinkBehaviour.KillOnDestroy)
            .OnComplete(() => onComplete?.Invoke());
        card.DOLocalRotate(rot, duration, RotateMode.Fast).SetEase(tweenEase).SetLink(card.gameObject, LinkBehaviour.KillOnDestroy);
        card.DOScale(1f, duration).SetEase(tweenEase).SetLink(card.gameObject, LinkBehaviour.KillOnDestroy);
    }

    void CollectCardsFromChildren()
    {
        var list = new System.Collections.Generic.List<RectTransform>();
        Transform hand = transform;

        for (int i = 0; i < hand.childCount; i++)
        {
            Transform child = hand.GetChild(i);
            if (child == null)
                continue;

            var rt = child as RectTransform;
            if (rt == null || !rt.gameObject.activeInHierarchy)
                continue;

            list.Add(rt);
        }

        list.Sort((a, b) =>
        {
            int slotA = GetHandSlot(a);
            int slotB = GetHandSlot(b);
            if (slotA != slotB)
                return slotA.CompareTo(slotB);

            return a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
        });
        cards = list.ToArray();
    }

    static int GetHandSlot(RectTransform card)
    {
        if (card == null)
            return -1;

        CardView view = card.GetComponent<CardView>();
        return view != null ? view.HandSlot : -1;
    }

    static bool IsValidHandCard(RectTransform card, Transform hand)
    {
        return card != null && hand != null && card.parent == hand;
    }

    static bool CanReorderChildSiblings(Transform hand)
    {
        return hand != null && hand.gameObject.activeInHierarchy;
    }

    void BuildSlotLayoutTable()
    {
        int slots = Mathf.Max(1, _maxHandSlots);
        _slotFanPositions = new Vector2[slots];
        _slotFanRotations = new Vector3[slots];

        float centerIndex = (slots - 1) * 0.5f;

        for (int slot = 0; slot < slots; slot++)
        {
            float offsetFromCenter = slot - centerIndex;
            float normalized = centerIndex > 0f ? offsetFromCenter / centerIndex : 0f;

            float fanX = offsetFromCenter * horizontalSpacing;
            float fanY = revealedBaseY + arcHeight * (1f - normalized * normalized);
            float fanRotZ = -maxRotation * normalized;

            _slotFanPositions[slot] = new Vector2(fanX, fanY);
            _slotFanRotations[slot] = new Vector3(0f, 0f, fanRotZ);
        }
    }

    void CacheLayoutTargets()
    {
        int count = cards != null ? cards.Length : 0;
        _restAnchoredPositions = new Vector2[count];
        _restLocalEulerAngles = new Vector3[count];
        _fanAnchoredPositions = new Vector2[count];
        _fanLocalEulerAngles = new Vector3[count];
        _originalSiblingIndices = new int[count];

        if (_slotFanPositions == null || _slotFanPositions.Length != _maxHandSlots)
            BuildSlotLayoutTable();

        if (count == 0)
            return;

        for (int i = 0; i < count; i++)
        {
            RectTransform card = cards[i];
            if (card == null)
                continue;

            _originalSiblingIndices[i] = card.GetSiblingIndex();
            _restAnchoredPositions[i] = new Vector2(0f, hiddenAnchoredY);
            _restLocalEulerAngles[i] = Vector3.zero;

            int slot = GetHandSlot(card);
            if (slot < 0 || slot >= _maxHandSlots)
                slot = Mathf.Clamp(i, 0, _maxHandSlots - 1);

            _fanAnchoredPositions[i] = _slotFanPositions[slot];
            _fanLocalEulerAngles[i] = _slotFanRotations[slot];
        }
    }

    public void FanOut()
    {
        if (_isFanned || cards == null || cards.Length == 0)
            return;

        _isFanned = true;
        _focusedCardIndex = -1;
        StartDrivingWheelAnimation();
        ApplyCurrentFanLayout(false, lockFocusDuringTween: true);

        if (raiseSiblingOrderWhenFanned && CanReorderChildSiblings(transform))
            ApplySiblingOrderBySlot();
    }

    void ApplySiblingOrderBySlot()
    {
        Transform hand = transform;
        for (int i = 0; i < cards.Length; i++)
        {
            RectTransform card = cards[i];
            if (!IsValidHandCard(card, hand))
                continue;

            int slot = GetHandSlot(card);
            if (slot < 0)
                slot = i;

            card.SetSiblingIndex(slot);
        }
    }

    public void Collapse()
    {
        if (!_isFanned || cards == null || cards.Length == 0)
            return;

        _isFanned = false;
        StopDrivingWheelAnimation();
        ResetLongHoverMagnify(immediate: false);
        _focusedCardIndex = -1;
        _focusEnabled = false;
        ApplyLayout(_restAnchoredPositions, _restLocalEulerAngles, null, immediate: false, lockFocusDuringTween: false);

        if (raiseSiblingOrderWhenFanned && CanReorderChildSiblings(transform))
        {
            Transform hand = transform;
            for (int i = 0; i < cards.Length; i++)
            {
                if (IsValidHandCard(cards[i], hand))
                    cards[i].SetSiblingIndex(_originalSiblingIndices[i]);
            }
        }
    }

    void ApplyCurrentFanLayout(bool immediate, bool lockFocusDuringTween)
    {
        if (cards == null || cards.Length == 0)
            return;

        EnsureLayoutCacheMatchesCards();
        SanitizeFocusedIndex();

        if (!_isFanned)
        {
            ApplyLayout(_restAnchoredPositions, _restLocalEulerAngles, null, immediate, lockFocusDuringTween);
            return;
        }

        if (!IsFocusedIndexValid() || !CanFocusCards)
        {
            ApplyLayout(_fanAnchoredPositions, _fanLocalEulerAngles, null, immediate, lockFocusDuringTween);
            return;
        }

        BuildFocusedLayout(out Vector2[] positions, out Vector3[] rotations, out float[] scales);
        ApplyLayout(positions, rotations, scales, immediate, lockFocusDuringTween: false);

        if (CanReorderChildSiblings(transform) && IsValidHandCard(cards[_focusedCardIndex], transform))
            cards[_focusedCardIndex].SetAsLastSibling();
    }

    void BuildFocusedLayout(out Vector2[] positions, out Vector3[] rotations, out float[] scales)
    {
        int count = cards != null ? cards.Length : 0;
        positions = new Vector2[count];
        rotations = new Vector3[count];
        scales = new float[count];

        if (count == 0 || !IsFocusedIndexValid())
            return;

        int layoutCount = Mathf.Min(count, _fanAnchoredPositions.Length, _fanLocalEulerAngles.Length);
        for (int i = 0; i < layoutCount; i++)
        {
            positions[i] = _fanAnchoredPositions[i];
            rotations[i] = _fanLocalEulerAngles[i];
            scales[i] = 1f;
        }

        int focus = _focusedCardIndex;
        if (focus >= count)
            return;

        int focusSlot = GetHandSlot(cards[focus]);
        if (focusSlot < 0)
            focusSlot = focus;

        for (int i = 0; i < count; i++)
        {
            if (i == focus)
                continue;

            int slot = GetHandSlot(cards[i]);
            if (slot < 0)
                slot = i;

            if (slot < focusSlot)
                positions[i].x -= focusSpreadPush;
            else if (slot > focusSlot)
                positions[i].x += focusSpreadPush;
        }

        positions[focus].y += focusLift;
        rotations[focus] = Vector3.zero;
        scales[focus] = focusScale;
    }

    void ApplyLayout(Vector2[] positions, Vector3[] rotations, float[] scales, bool immediate, bool lockFocusDuringTween)
    {
        if (cards == null || positions == null || rotations == null)
            return;

        Transform hand = transform;
        int count = Mathf.Min(cards.Length, positions.Length, rotations.Length);
        float duration = IsFocusedIndexValid() && CanFocusCards ? focusTweenDuration : tweenDuration;
        Ease ease = IsFocusedIndexValid() && CanFocusCards ? focusTweenEase : tweenEase;

        if (lockFocusDuringTween)
            _focusEnabled = false;

        _layoutGeneration++;
        int layoutGeneration = _layoutGeneration;

        if (immediate)
        {
            _pendingLayoutTweens = 0;
            for (int i = 0; i < count; i++)
            {
                RectTransform card = cards[i];
                if (!IsValidHandCard(card, hand))
                    continue;

                card.DOKill(true);
                float scale = scales != null && i < scales.Length ? scales[i] : 1f;
                if (_longHoverMagnified && card == _longHoverCard)
                    scale = longHoverScale;

                card.anchoredPosition = positions[i];
                card.localEulerAngles = rotations[i];
                card.localScale = Vector3.one * scale;
            }

            FinishLayoutTween();
            return;
        }

        _pendingLayoutTweens = 0;
        for (int i = 0; i < count; i++)
        {
            RectTransform card = cards[i];
            if (!IsValidHandCard(card, hand))
                continue;

            _pendingLayoutTweens++;
            bool preserveLongHoverScale = _longHoverMagnified && card == _longHoverCard;
            card.DOKill(preserveLongHoverScale);

            float scale = scales != null && i < scales.Length ? scales[i] : 1f;
            if (preserveLongHoverScale)
                scale = longHoverScale;

            card.DOAnchorPos(positions[i], duration)
                .SetEase(ease)
                .SetLink(card.gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => OnLayoutTweenFinished(layoutGeneration));
            card.DOLocalRotate(rotations[i], duration, RotateMode.Fast)
                .SetEase(ease)
                .SetLink(card.gameObject, LinkBehaviour.KillOnDestroy);
            card.DOScale(scale, duration)
                .SetEase(ease)
                .SetLink(card.gameObject, LinkBehaviour.KillOnDestroy);
        }

        if (_pendingLayoutTweens == 0)
            FinishLayoutTween();
    }

    void OnLayoutTweenFinished(int layoutGeneration)
    {
        if (layoutGeneration != _layoutGeneration)
            return;

        if (--_pendingLayoutTweens > 0)
            return;

        FinishLayoutTween();
    }

    void FinishLayoutTween()
    {
        RefreshFocusEnabled();

        if (!CanFocusCards)
            return;

        SanitizeFocusedIndex();
        EnsureLayoutCacheMatchesCards();

        int underPointer = GetCardIndexUnderPointer();
        if (underPointer == _focusedCardIndex)
            return;

        _focusedCardIndex = underPointer;
        if (IsFocusedIndexValid())
            ApplyCurrentFanLayout(false, lockFocusDuringTween: false);
    }

    void KillCardTweens()
    {
        if (cards == null)
            return;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] != null)
                cards[i].DOKill();
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        _handRect = transform as RectTransform;
        _boxCollider = GetComponent<BoxCollider2D>();

        if (syncBoxColliderToRect)
            SyncBoxColliderToRect();

        if (!Application.isPlaying && cards != null && cards.Length > 0)
            CacheLayoutTargets();
    }
#endif
}
