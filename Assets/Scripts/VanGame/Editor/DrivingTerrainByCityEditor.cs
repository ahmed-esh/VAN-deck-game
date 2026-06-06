#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VanGame.Visual;

[CustomEditor(typeof(DrivingTerrainByCity))]
public class DrivingTerrainByCityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(6f);
        EditorGUILayout.HelpBox(
            "Assign one entry per destination city. Drag terrain roots (parallax camera rigs, animated roads, etc.) " +
            "into Terrain Roots. Only the matching city is enabled when the player picks that region on the map.\n\n" +
            "Keep every terrain root listed here (or under Default Terrain Roots) so other cities stay hidden.",
            MessageType.Info);

        var controller = (DrivingTerrainByCity)target;
        if (Application.isPlaying)
          EditorGUILayout.LabelField("Active city", controller.ActiveCity != null ? controller.ActiveCity.displayName : "(none)");
    }
}
#endif
