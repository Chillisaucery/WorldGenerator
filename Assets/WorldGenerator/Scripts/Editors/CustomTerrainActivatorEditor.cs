using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CustomTerrainActivator))]
[CanEditMultipleObjects]
public class CustomTerrainActivatorEditor : Editor
{
    bool showRandom = false;
    bool showPerlin = false;

    public override void OnInspectorGUI()
    {
        CustomTerrainActivator customTerrainActivator = (CustomTerrainActivator)target;

        if (GUILayout.Button("ResetHeight"))
        {
            customTerrainActivator.ResetHeight();
        }



        showRandom = EditorGUILayout.Foldout(showRandom, "Random");

        if (showRandom)
        {
            if (GUILayout.Button("RandomHeight"))
            {
                customTerrainActivator.RandomHeight();
            }
        }

        showPerlin = EditorGUILayout.Foldout(showPerlin, "Perlin");

        if (showPerlin)
        {
            if (GUILayout.Button("PerlinNoise"))
            {
                customTerrainActivator.PerlinNoise();
            }
        }
    }
}
