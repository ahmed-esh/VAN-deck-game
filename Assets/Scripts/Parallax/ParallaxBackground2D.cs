using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fixed camera + car: environment <see cref="SpriteRenderer"/> layers scroll at different
/// parallax factors. Optional infinite tiling duplicates tiles and wraps them at the view edge.
/// </summary>
[DisallowMultipleComponent]
public class ParallaxBackground2D : MonoBehaviour
{
    public enum ParallaxScrollSource
    {
        AutoScroll = 0,
        FollowTransform = 1,
        Camera = 2
    }

    [Serializable]
    public class Layer
    {
        public string label = string.Empty;
        public SpriteRenderer spriteRenderer;
        [Range(0f, 2f)] public float parallaxFactor = 0.5f;
        public bool lockY = true;
        public bool infiniteTiling = true;
        [Min(0)] public int duplicateCount = 1;
        public float tileWidth;
        public float wrapViewPadding;
        public Vector3 initialWorldPosition;
        public bool hasCachedInitialPosition;
    }

    [SerializeField] ParallaxScrollSource scrollSource = ParallaxScrollSource.AutoScroll;
    [SerializeField] bool autoScrollEnabled = true;
    [SerializeField] float autoScrollSpeed = 2f;
    [SerializeField] float speedDecayPerSecond = 1.5f;
    [SerializeField] Vector2 autoScrollDirection = new Vector2(-1f, 0f);
    [SerializeField] Transform cameraTransform;
    [SerializeField] float viewBoundsPadding = 0.5f;
    [SerializeField] Transform scrollReference;
    [SerializeField] List<Layer> layers = new List<Layer>();

    readonly List<LayerRuntime> _runtimes = new List<LayerRuntime>();
    readonly List<GameObject> _spawnedClones = new List<GameObject>();

    Camera _camera;
    Vector2 _scrollDirectionNormalized;
    Vector2 _scrollAxis;
    float _scrollOffset;
    float _runtimeScrollSpeed;
    Vector3 _referenceStartPosition;
    Vector3 _cameraStartPosition;
    bool _initialized;

    public IReadOnlyList<Layer> Layers => layers;
    public float CurrentScrollOffset => _scrollOffset;
    public float BaseScrollSpeed => autoScrollSpeed;
    public float RuntimeScrollSpeed => _runtimeScrollSpeed;

    public void SetSpeedDecayPerSecond(float decayPerSecond)
    {
        speedDecayPerSecond = Mathf.Max(0f, decayPerSecond);
    }

    public void ResetScrollSpeed()
    {
        _runtimeScrollSpeed = autoScrollSpeed;
    }

    public void BoostScrollSpeed(float amount)
    {
        if (amount <= 0f)
            return;

        _runtimeScrollSpeed += amount;
    }

    void Awake()
    {
        ResolveCamera();
        CacheScrollDirection();
    }

    void OnEnable()
    {
        ResolveCamera();
        CacheScrollDirection();
        ResetScrollSpeed();
        InitializeIfNeeded();
    }

    void OnDisable()
    {
        DestroySpawnedClones();
        _runtimes.Clear();
        _initialized = false;
    }

    void Update()
    {
        if (scrollSource != ParallaxScrollSource.AutoScroll || !autoScrollEnabled)
            return;

        _runtimeScrollSpeed = Mathf.MoveTowards(
            _runtimeScrollSpeed,
            autoScrollSpeed,
            speedDecayPerSecond * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (!_initialized || _runtimes.Count == 0)
            return;

        float deltaScroll = ComputeScrollDelta();
        if (Mathf.Approximately(deltaScroll, 0f))
            return;

        _scrollOffset += deltaScroll;

        for (int i = 0; i < _runtimes.Count; i++)
            ApplyLayerScroll(_runtimes[i], deltaScroll);
    }

    public void RecacheFromCurrentPositions()
    {
        for (int i = 0; i < layers.Count; i++)
        {
            Layer layer = layers[i];
            if (layer?.spriteRenderer == null)
                continue;

            layer.initialWorldPosition = layer.spriteRenderer.transform.position;
            layer.hasCachedInitialPosition = true;
        }

        if (_initialized)
        {
            DestroySpawnedClones();
            _runtimes.Clear();
            _initialized = false;
            InitializeIfNeeded();
        }
    }

    void InitializeIfNeeded()
    {
        if (_initialized)
            return;

        ResolveCamera();
        CacheScrollDirection();

        if (scrollSource == ParallaxScrollSource.FollowTransform && scrollReference != null)
            _referenceStartPosition = scrollReference.position;

        if (scrollSource == ParallaxScrollSource.Camera && _camera != null)
            _cameraStartPosition = _camera.transform.position;

        _scrollOffset = 0f;
        _runtimes.Clear();

        for (int i = 0; i < layers.Count; i++)
        {
            Layer layer = layers[i];
            if (layer?.spriteRenderer == null)
                continue;

            if (!layer.hasCachedInitialPosition)
            {
                layer.initialWorldPosition = layer.spriteRenderer.transform.position;
                layer.hasCachedInitialPosition = true;
            }

            LayerRuntime runtime = BuildLayerRuntime(layer, i);
            if (runtime != null)
                _runtimes.Add(runtime);
        }

        _initialized = _runtimes.Count > 0;
    }

    LayerRuntime BuildLayerRuntime(Layer layer, int layerIndex)
    {
        SpriteRenderer sourceRenderer = layer.spriteRenderer;
        Transform sourceTransform = sourceRenderer.transform;
        float tileWidth = ResolveTileWidth(layer, sourceRenderer);

        var runtime = new LayerRuntime
        {
            config = layer,
            layerIndex = layerIndex,
            tileWidth = tileWidth,
            tiles = new List<TileInstance>()
        };

        runtime.tiles.Add(CreateTileInstance(sourceRenderer, false, layer.initialWorldPosition));

        if (layer.infiniteTiling && layer.duplicateCount > 0)
        {
            Vector3 axis = ScrollAxis3D;
            for (int i = 1; i <= layer.duplicateCount; i++)
            {
                float offset = tileWidth * i;
                Vector3 leftPos = layer.initialWorldPosition - axis * offset;
                Vector3 rightPos = layer.initialWorldPosition + axis * offset;

                runtime.tiles.Add(SpawnClone(sourceRenderer, leftPos, layerIndex));
                runtime.tiles.Add(SpawnClone(sourceRenderer, rightPos, layerIndex));
            }
        }

        return runtime;
    }

    TileInstance SpawnClone(SpriteRenderer source, Vector3 worldPosition, int layerIndex)
    {
        GameObject cloneObject = Instantiate(source.gameObject, source.transform.parent);
        cloneObject.name = source.name + "_parallax_clone";
        cloneObject.transform.SetPositionAndRotation(worldPosition, source.transform.rotation);
        cloneObject.transform.localScale = source.transform.localScale;

        var marker = cloneObject.GetComponent<ParallaxTileCloneMarker>();
        if (marker == null)
            marker = cloneObject.AddComponent<ParallaxTileCloneMarker>();
        marker.Initialize(this, layerIndex);

        _spawnedClones.Add(cloneObject);

        SpriteRenderer cloneRenderer = cloneObject.GetComponent<SpriteRenderer>();
        return CreateTileInstance(cloneRenderer, true, worldPosition);
    }

    static TileInstance CreateTileInstance(SpriteRenderer renderer, bool isClone, Vector3 position)
    {
        renderer.transform.position = position;
        return new TileInstance
        {
            transform = renderer.transform,
            renderer = renderer,
            isClone = isClone
        };
    }

    void ApplyLayerScroll(LayerRuntime runtime, float deltaScroll)
    {
        Layer layer = runtime.config;
        float layerDelta = deltaScroll * layer.parallaxFactor;
        Vector3 motion = ScrollDeltaToWorld(layerDelta, layer.lockY);

        for (int t = 0; t < runtime.tiles.Count; t++)
            runtime.tiles[t].transform.position += motion;

        if (!layer.infiniteTiling || _camera == null)
            return;

        WrapLayerTiles(runtime);
    }

    void WrapLayerTiles(LayerRuntime runtime)
    {
        Bounds viewBounds = GetViewBounds(runtime.config.wrapViewPadding);
        Vector3 stripJump = ScrollAxis3D * (runtime.tileWidth * runtime.tiles.Count);

        for (int pass = 0; pass < runtime.tiles.Count; pass++)
        {
            for (int t = 0; t < runtime.tiles.Count; t++)
            {
                TileInstance tile = runtime.tiles[t];
                if (tile.renderer == null)
                    continue;

                Bounds tileBounds = tile.renderer.bounds;

                if (_scrollAxis.x < -0.5f)
                {
                    if (tileBounds.max.x < viewBounds.min.x)
                        tile.transform.position -= stripJump;
                    else if (tileBounds.min.x > viewBounds.max.x)
                        tile.transform.position += stripJump;
                }
                else if (_scrollAxis.x > 0.5f)
                {
                    if (tileBounds.min.x > viewBounds.max.x)
                        tile.transform.position -= stripJump;
                    else if (tileBounds.max.x < viewBounds.min.x)
                        tile.transform.position += stripJump;
                }
                else if (_scrollAxis.y < -0.5f)
                {
                    if (tileBounds.max.y < viewBounds.min.y)
                        tile.transform.position -= stripJump;
                    else if (tileBounds.min.y > viewBounds.max.y)
                        tile.transform.position += stripJump;
                }
                else if (_scrollAxis.y > 0.5f)
                {
                    if (tileBounds.min.y > viewBounds.max.y)
                        tile.transform.position -= stripJump;
                    else if (tileBounds.max.y < viewBounds.min.y)
                        tile.transform.position += stripJump;
                }
            }
        }
    }

    float ComputeScrollDelta()
    {
        switch (scrollSource)
        {
            case ParallaxScrollSource.AutoScroll:
                if (!autoScrollEnabled || _runtimeScrollSpeed <= 0f)
                    return 0f;
                return _runtimeScrollSpeed * Time.deltaTime;

            case ParallaxScrollSource.FollowTransform:
                if (scrollReference == null)
                    return 0f;
                float refDelta = Vector3.Dot(
                    scrollReference.position - _referenceStartPosition,
                    ScrollAxis3D);
                float refScroll = refDelta;
                float frameRef = refScroll - _scrollOffset;
                return frameRef;

            case ParallaxScrollSource.Camera:
                if (_camera == null)
                    return 0f;
                float camDelta = Vector3.Dot(
                    _camera.transform.position - _cameraStartPosition,
                    ScrollAxis3D);
                float frameCam = camDelta - _scrollOffset;
                return frameCam;

            default:
                return 0f;
        }
    }

    Vector3 ScrollDeltaToWorld(float scrollAmount, bool lockYAxis)
    {
        Vector3 delta = ScrollAxis3D * scrollAmount;
        if (lockYAxis)
            delta.y = 0f;
        return delta;
    }

    Bounds GetViewBounds(float extraPadding)
    {
        float pad = viewBoundsPadding + extraPadding;
        if (_camera == null)
            return new Bounds(transform.position, Vector3.one * 20f);

        if (_camera.orthographic)
        {
            float height = _camera.orthographicSize * 2f;
            float width = height * _camera.aspect;
            Vector3 center = _camera.transform.position;
            Vector3 size = new Vector3(width + pad * 2f, height + pad * 2f, 10f);
            return new Bounds(center, size);
        }

        float depth = Mathf.Abs(_camera.transform.position.z);
        Vector3 bl = _camera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        Vector3 tr = _camera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
        Vector3 min = Vector3.Min(bl, tr) - Vector3.one * pad;
        Vector3 max = Vector3.Max(bl, tr) + Vector3.one * pad;
        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    static float ResolveTileWidth(Layer layer, SpriteRenderer renderer)
    {
        if (layer.tileWidth > 0.01f)
            return layer.tileWidth;

        if (renderer.sprite != null)
        {
            float spriteWidth = renderer.sprite.rect.width / renderer.sprite.pixelsPerUnit;
            return spriteWidth * Mathf.Abs(renderer.transform.lossyScale.x);
        }

        return renderer.bounds.size.x;
    }

    void ResolveCamera()
    {
        if (cameraTransform != null)
            _camera = cameraTransform.GetComponent<Camera>();

        if (_camera == null)
            _camera = Camera.main;
    }

    void CacheScrollDirection()
    {
        _scrollDirectionNormalized = autoScrollDirection.sqrMagnitude > 0.0001f
            ? autoScrollDirection.normalized
            : Vector2.left;

        if (Mathf.Abs(_scrollDirectionNormalized.x) >= Mathf.Abs(_scrollDirectionNormalized.y))
            _scrollAxis = new Vector2(Mathf.Sign(_scrollDirectionNormalized.x), 0f);
        else
            _scrollAxis = new Vector2(0f, Mathf.Sign(_scrollDirectionNormalized.y));
    }

    Vector3 ScrollAxis3D => new Vector3(_scrollAxis.x, _scrollAxis.y, 0f);

    void DestroySpawnedClones()
    {
        for (int i = _spawnedClones.Count - 1; i >= 0; i--)
        {
            if (_spawnedClones[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(_spawnedClones[i]);
                else
                    DestroyImmediate(_spawnedClones[i]);
            }
        }

        _spawnedClones.Clear();
    }

    class LayerRuntime
    {
        public Layer config;
        public int layerIndex;
        public float tileWidth;
        public List<TileInstance> tiles;
    }

    class TileInstance
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public bool isClone;
    }
}
