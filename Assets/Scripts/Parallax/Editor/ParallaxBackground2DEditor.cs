#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ParallaxBackground2D))]
public class ParallaxBackground2DEditor : Editor
{
    static readonly Color FarthestColor = new Color(0.45f, 0.75f, 1f, 0.35f);
    static readonly Color ClosestColor = new Color(1f, 0.45f, 0.35f, 0.35f);
    static readonly Color MidColor = new Color(0.85f, 0.85f, 0.85f, 0.25f);

    SerializedProperty _scrollSource;
    SerializedProperty _cameraTransform;
    SerializedProperty _scrollReference;
    SerializedProperty _autoScrollEnabled;
    SerializedProperty _autoScrollSpeed;
    SerializedProperty _autoScrollDirection;
    SerializedProperty _layers;
    bool _showAdvanced;

    void OnEnable()
    {
        _scrollSource = serializedObject.FindProperty("scrollSource");
        _cameraTransform = serializedObject.FindProperty("cameraTransform");
        _scrollReference = serializedObject.FindProperty("scrollReference");
        _autoScrollEnabled = serializedObject.FindProperty("autoScrollEnabled");
        _autoScrollSpeed = serializedObject.FindProperty("autoScrollSpeed");
        _autoScrollDirection = serializedObject.FindProperty("autoScrollDirection");
        _layers = serializedObject.FindProperty("layers");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Camera stays still. Auto Scroll moves layers at different speeds (parallax factor).\n\n" +
            "Infinite Tiling (per layer): spawns touching duplicates on both sides, same speed, teleports when off-screen.\n" +
            "Direction (-1, 0) = backgrounds move left (classic forward drive). (+1, 0) = move right.",
            MessageType.Info);

        var source = (ParallaxBackground2D.ParallaxScrollSource)_scrollSource.enumValueIndex;

        if (source != ParallaxBackground2D.ParallaxScrollSource.AutoScroll)
        {
            EditorGUILayout.HelpBox(
                "Scroll Source is not Auto Scroll. For a fixed camera, set it to Auto Scroll.",
                MessageType.Warning);

            if (GUILayout.Button("Use Auto Scroll (recommended)"))
            {
                _scrollSource.enumValueIndex = (int)ParallaxBackground2D.ParallaxScrollSource.AutoScroll;
                _autoScrollEnabled.boolValue = true;
            }
        }

        EditorGUILayout.PropertyField(_autoScrollEnabled, new GUIContent("Scrolling"));
        EditorGUILayout.PropertyField(_autoScrollSpeed, new GUIContent("Speed"));
        EditorGUILayout.PropertyField(_autoScrollDirection, new GUIContent("Direction"));

        _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced scroll source", true);
        if (_showAdvanced)
        {
            EditorGUILayout.PropertyField(_scrollSource, new GUIContent("Scroll Source"));

            source = (ParallaxBackground2D.ParallaxScrollSource)_scrollSource.enumValueIndex;

            if (source == ParallaxBackground2D.ParallaxScrollSource.FollowTransform)
                EditorGUILayout.PropertyField(_scrollReference, new GUIContent("Scroll Reference"));

            if (source == ParallaxBackground2D.ParallaxScrollSource.Camera)
                EditorGUILayout.PropertyField(_cameraTransform, new GUIContent("Camera"));
        }

        if (Application.isPlaying)
        {
            var parallax = (ParallaxBackground2D)target;
            EditorGUILayout.LabelField("Live scroll offset", parallax.CurrentScrollOffset.ToString("F2"));
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.PropertyField(_layers, new GUIContent("Parallax Layers"), true);

        DrawDepthSummary();
        DrawActions();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawDepthSummary()
    {
        if (_layers == null || !_layers.isArray || _layers.arraySize == 0)
            return;

        var entries = new List<DepthEntry>();
        for (int i = 0; i < _layers.arraySize; i++)
        {
            SerializedProperty element = _layers.GetArrayElementAtIndex(i);
            SerializedProperty factorProp = element.FindPropertyRelative("parallaxFactor");
            SerializedProperty labelProp = element.FindPropertyRelative("label");
            SerializedProperty spriteProp = element.FindPropertyRelative("spriteRenderer");

            string label = labelProp.stringValue;
            if (string.IsNullOrWhiteSpace(label))
            {
                Object spriteRef = spriteProp.objectReferenceValue;
                label = spriteRef != null ? spriteRef.name : $"Layer {i}";
            }

            entries.Add(new DepthEntry
            {
                listIndex = i,
                factor = factorProp.floatValue,
                label = label
            });
        }

        entries.Sort((a, b) => a.factor.CompareTo(b.factor));

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Depth order (far → near)", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            for (int rank = 0; rank < entries.Count; rank++)
            {
                DepthEntry entry = entries[rank];
                string depthLabel = GetDepthLabel(rank, entries.Count);
                Color rowColor = GetDepthColor(rank, entries.Count);

                Rect rowRect = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rowRect, rowColor);

                Rect labelRect = new Rect(rowRect.x + 6f, rowRect.y + 3f, rowRect.width - 12f, rowRect.height);
                string text = $"{depthLabel}  ·  {entry.label}  (factor {entry.factor:0.##}, list #{entry.listIndex})";
                EditorGUI.LabelField(labelRect, text, EditorStyles.miniLabel);
            }
        }
    }

    void DrawActions()
    {
        var parallax = (ParallaxBackground2D)target;

        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Recache positions"))
            {
                Undo.RecordObject(parallax, "Recache Parallax Positions");
                parallax.RecacheFromCurrentPositions();
                EditorUtility.SetDirty(parallax);
            }

            if (GUILayout.Button("Log tile widths"))
            {
                LogTileWidths(parallax);
            }

            if (GUILayout.Button("Sort list far → near"))
            {
                SortLayersByFactor(parallax);
            }
        }
    }

    static void LogTileWidths(ParallaxBackground2D parallax)
    {
        for (int i = 0; i < parallax.Layers.Count; i++)
        {
            ParallaxBackground2D.Layer layer = parallax.Layers[i];
            if (layer?.spriteRenderer == null)
                continue;

            Sprite sprite = layer.spriteRenderer.sprite;
            float autoWidth = 0f;
            if (sprite != null)
            {
                float widthUnits = sprite.rect.width / sprite.pixelsPerUnit;
                autoWidth = widthUnits * Mathf.Abs(layer.spriteRenderer.transform.lossyScale.x);
            }

            float used = layer.tileWidth > 0.01f ? layer.tileWidth : autoWidth;
            Debug.Log(
                $"Parallax layer '{layer.label}': tile width = {used:0.###} (auto {autoWidth:0.###}, bounds {layer.spriteRenderer.bounds.size.x:0.###})",
                layer.spriteRenderer);
        }
    }

    static void SortLayersByFactor(ParallaxBackground2D parallax)
    {
        Undo.RecordObject(parallax, "Sort Parallax Layers");
        var sorted = new List<ParallaxBackground2D.Layer>(parallax.Layers);
        sorted.Sort((a, b) => a.parallaxFactor.CompareTo(b.parallaxFactor));

        SerializedObject so = new SerializedObject(parallax);
        SerializedProperty layersProp = so.FindProperty("layers");
        layersProp.ClearArray();
        for (int i = 0; i < sorted.Count; i++)
        {
            layersProp.InsertArrayElementAtIndex(i);
            SerializedProperty element = layersProp.GetArrayElementAtIndex(i);
            CopyLayerToSerialized(sorted[i], element);
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(parallax);
    }

    static void CopyLayerToSerialized(ParallaxBackground2D.Layer source, SerializedProperty element)
    {
        element.FindPropertyRelative("label").stringValue = source.label;
        element.FindPropertyRelative("spriteRenderer").objectReferenceValue = source.spriteRenderer;
        element.FindPropertyRelative("parallaxFactor").floatValue = source.parallaxFactor;
        element.FindPropertyRelative("lockY").boolValue = source.lockY;
        element.FindPropertyRelative("infiniteTiling").boolValue = source.infiniteTiling;
        element.FindPropertyRelative("duplicateCount").intValue = source.duplicateCount;
        element.FindPropertyRelative("tileWidth").floatValue = source.tileWidth;
        element.FindPropertyRelative("wrapViewPadding").floatValue = source.wrapViewPadding;
        element.FindPropertyRelative("initialWorldPosition").vector3Value = source.initialWorldPosition;
        element.FindPropertyRelative("hasCachedInitialPosition").boolValue = source.hasCachedInitialPosition;
    }

    static string GetDepthLabel(int rank, int count)
    {
        if (count <= 1)
            return "Only layer";

        if (rank == 0)
            return "FARTHEST";

        if (rank == count - 1)
            return "CLOSEST";

        if (count == 3 && rank == 1)
            return "MIDDLE";

        return $"MID {rank}/{count - 1}";
    }

    static Color GetDepthColor(int rank, int count)
    {
        if (count <= 1)
            return MidColor;

        float t = count > 1 ? rank / (float)(count - 1) : 0f;
        return Color.Lerp(FarthestColor, ClosestColor, t);
    }

    void OnSceneGUI()
    {
        var parallax = (ParallaxBackground2D)target;
        if (parallax.Layers == null || parallax.Layers.Count == 0)
            return;

        var sorted = new List<(ParallaxBackground2D.Layer layer, int rank)>();
        for (int i = 0; i < parallax.Layers.Count; i++)
        {
            ParallaxBackground2D.Layer layer = parallax.Layers[i];
            if (layer?.spriteRenderer == null)
                continue;

            sorted.Add((layer, i));
        }

        sorted.Sort((a, b) => a.layer.parallaxFactor.CompareTo(b.layer.parallaxFactor));

        for (int rank = 0; rank < sorted.Count; rank++)
        {
            ParallaxBackground2D.Layer layer = sorted[rank].layer;
            Transform t = layer.spriteRenderer.transform;
            Vector3 worldPos = t.position;
            string depth = GetDepthLabel(rank, sorted.Count);
            string name = string.IsNullOrWhiteSpace(layer.label) ? t.name : layer.label;
            Handles.Label(worldPos + Vector3.up * 0.5f, $"{depth}\n{name} ({layer.parallaxFactor:0.##})");
        }
    }

    struct DepthEntry
    {
        public int listIndex;
        public float factor;
        public string label;
    }
}
#endif
