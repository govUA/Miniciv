using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target;

        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("currentMapType"));
        
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mapSize"));

        // Disable Width and Height if Size is not Custom
        EditorGUI.BeginDisabledGroup(mapGen.mapSize != MapGenerator.MapSizeType.Custom);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mapWidth"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mapHeight"));
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("wrapWorld"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("seed"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useRandomSeed"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("seaLevel"));

        // Disable Fill Percent and Smoothing if Sea Level is not Custom
        EditorGUI.BeginDisabledGroup(mapGen.seaLevel != MapGenerator.SeaLevelType.Custom);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("randomFillPercent"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("smoothingIterations"));
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
        
        // Force update the MapGenerator logic if something was clicked
        if (GUI.changed)
        {
            mapGen.ApplyPresets();
            EditorUtility.SetDirty(mapGen);
        }
    }
}