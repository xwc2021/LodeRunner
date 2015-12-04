using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(GraphMap))]
public class GraphMapEditor : UnityEditor.Editor
{
    GraphMap graphMap;
    void OnEnable()
    {
        graphMap = (GraphMap)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("show"))
        {
            graphMap.showGraphNode(true);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("hide"))
        {
            graphMap.showGraphNode(false);
        }
    }
}
