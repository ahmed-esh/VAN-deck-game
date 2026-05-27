using DG.Tweening;
using UnityEngine;

/// <summary>
/// Attach to the card-hand parent (RectTransform + BoxCollider2D). Detects mouse hover over the
/// hand area and tweens child cards into a fanned semicircle. UI hover uses the RectTransform
/// (works with Screen Space Overlay). BoxCollider2D is auto-sized to match the rect for reference.
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
    [Tooltip("Horizontal distance between neighbouring card centers in the fan.")]
    [SerializeField] float horizontalSpacing = 70f;

    [Tooltip("Extra Y for the centre card; edge cards sit at revealedBaseY.")]
    [SerializeField] float arcHeight = 35f;

    [Tooltip("Max Z rotation (degrees) on the outermost cards.")]
    [SerializeField] float maxRotation = 22f;

    [Header("Animation")]
    [SerializeField] float tweenDuration = 0.35f;
    [SerializeField] Ease tweenEase = Ease.OutCubic;

    [Tooltip("When fanning, later siblings draw on top (right-over-left overlap).")]
    [SerializeField] bool raiseSiblingOrderWhenFanned = true;

    [Header("Hover detection")]
    [Tooltip("Extra screen-space padding around the hand rect for easier hover.")]
    [SerializeField] Vector2 hoverPadding = Vector2.zero;

    [Tooltip("Keeps BoxCollider2D size/offset aligned with this RectTransform.")]
    [SerializeField] bool syncBoxColliderToRect = true;

    RectTransform _handRect;
    BoxCollider2D _boxCollider;
    Canvas _rootCanvas;
    Camera _eventCamera;
    bool _pointerInside;

    Vector2[] _restAnchoredPositions;
    Vector3[] _restLocalEulerAngles;
    Vector2[] _fanAnchoredPositions;
    Vector3[] _fanLocalEulerAngles;
    int[] _originalSiblingIndices;
    bool _isFanned;

    void Awake()
    {
        _handRect = (RectTransform)transform;
        _boxCollider = GetComponent<BoxCollider2D>();
        _rootCanvas = GetComponentInParent<Canvas>();

        if (_rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            _eventCamera = _rootCanvas.worldCamera;

        if (syncBoxColliderToRect)
            SyncBoxColliderToRect();

        if (collectCardsFromChildren || cards == null || cards.Length == 0)
            CollectCardsFromChildren();

        CacheLayoutTargets();
        ApplyLayout(_restAnchoredPositions, _restLocalEulerAngles, immediate: true);
    }

    void Update()
    {
        UpdateHover();
    }

    void UpdateHover()
    {
        if (_handRect == null)
            return;

        bool inside = IsPointerOverHandRect();

        if (inside && !_pointerInside)
        {
            _pointerInside = true;
            FanOut();
        }
        else if (!inside && _pointerInside)
        {
            _pointerInside = false;
            Collapse();
        }
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
        KillCardTweens();

        if (!_isFanned)
            return;

        _isFanned = false;
        // Cannot SetSiblingIndex while the hand parent is activating/deactivating.
        ApplyLayout(_restAnchoredPositions, _restLocalEulerAngles, immediate: true);
    }

    /// <summary>Re-scan direct child card slots after the hand is rebuilt (M4 dynamic cards).</summary>
    public void RefreshFromChildren()
    {
        CollectCardsFromChildren();
        CacheLayoutTargets();

        if (_isFanned)
            ApplyLayout(_fanAnchoredPositions, _fanLocalEulerAngles, immediate: false);
        else
            ApplyLayout(_restAnchoredPositions, _restLocalEulerAngles, immediate: true);
    }

    /// <summary>Collect direct child RectTransforms (skips this object's own RectTransform).</summary>
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

        list.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));
        cards = list.ToArray();
    }

    static bool IsValidHandCard(RectTransform card, Transform hand)
    {
        return card != null && hand != null && card.parent == hand;
    }

    static bool CanReorderChildSiblings(Transform hand)
    {
        return hand != null && hand.gameObject.activeInHierarchy;
    }

    void CacheLayoutTargets()
    {
        int count = cards != null ? cards.Length : 0;
        _restAnchoredPositions = new Vector2[count];
        _restLocalEulerAngles = new Vector3[count];
        _fanAnchoredPositions = new Vector2[count];
        _fanLocalEulerAngles = new Vector3[count];
        _originalSiblingIndices = new int[count];

        if (count == 0)
            return;

        float centerIndex = (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            RectTransform card = cards[i];
            if (card == null)
                continue;

            _originalSiblingIndices[i] = card.GetSiblingIndex();

            Vector2 current = card.anchoredPosition;
            _restAnchoredPositions[i] = new Vector2(current.x, hiddenAnchoredY);
            _restLocalEulerAngles[i] = card.localEulerAngles;

            float offsetFromCenter = i - centerIndex;
            float normalized = centerIndex > 0f ? offsetFromCenter / centerIndex : 0f;

            float fanX = offsetFromCenter * horizontalSpacing;
            float fanY = revealedBaseY + arcHeight * (1f - normalized * normalized);
            float fanRotZ = -maxRotation * normalized;

            _fanAnchoredPositions[i] = new Vector2(fanX, fanY);
            _fanLocalEulerAngles[i] = new Vector3(0f, 0f, fanRotZ);
        }
    }

    public void FanOut()
    {
        if (_isFanned || cards == null || cards.Length == 0)
            return;

        _isFanned = true;
        ApplyLayout(_fanAnchoredPositions, _fanLocalEulerAngles, immediate: false);

        if (raiseSiblingOrderWhenFanned && CanReorderChildSiblings(transform))
        {
            Transform hand = transform;
            for (int i = 0; i < cards.Length; i++)
            {
                if (IsValidHandCard(cards[i], hand))
                    cards[i].SetSiblingIndex(i);
            }
        }
    }

    public void Collapse()
    {
        if (!_isFanned || cards == null || cards.Length == 0)
            return;

        _isFanned = false;
        ApplyLayout(_restAnchoredPositions, _restLocalEulerAngles, immediate: false);

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

    void ApplyLayout(Vector2[] positions, Vector3[] rotations, bool immediate)
    {
        if (cards == null || positions == null || rotations == null)
            return;

        Transform hand = transform;
        int count = Mathf.Min(cards.Length, positions.Length, rotations.Length);
        for (int i = 0; i < count; i++)
        {
            RectTransform card = cards[i];
            if (!IsValidHandCard(card, hand))
                continue;

            card.DOKill(true);

            if (immediate)
            {
                card.anchoredPosition = positions[i];
                card.localEulerAngles = rotations[i];
                continue;
            }

            card.DOAnchorPos(positions[i], tweenDuration)
                .SetEase(tweenEase)
                .SetLink(card.gameObject, LinkBehaviour.KillOnDestroy);
            card.DOLocalRotate(rotations[i], tweenDuration, RotateMode.Fast)
                .SetEase(tweenEase)
                .SetLink(card.gameObject, LinkBehaviour.KillOnDestroy);
        }
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
