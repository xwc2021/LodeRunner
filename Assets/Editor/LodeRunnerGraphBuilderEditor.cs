using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(LodeRunnerGraphBuilder))]
public class LodeRunnerGraphBuilderEditor : UnityEditor.Editor
{
    LodeRunnerGraphBuilder graphBuilder;
    void OnEnable()
    {
        graphBuilder = (LodeRunnerGraphBuilder)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("build graph"))
        {
            graphBuilder.buildAll();
            graphBuilder.graphMap.showGraphNode(false);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("test 語法"))
        {
            graphBuilder.testCsharp();
        }
    }
}
