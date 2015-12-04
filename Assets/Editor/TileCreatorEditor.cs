using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(TileCreator))]
public class TileCreatorEditor : UnityEditor.Editor {

    TileCreator tileCreator;
    void OnEnable()
    {
        tileCreator = (TileCreator)target;
    }

    public int width =16;
    public int height =16;

    public override void OnInspectorGUI () {

        DrawDefaultInspector();

        //injectValue放在tileCreator數值才會被記住
        tileCreator.injectValue = EditorGUILayout.IntSlider((int)tileCreator.injectValue, 0, tileCreator.tiles.Length);

        width =  EditorGUILayout.IntField("map width", width);
        height = EditorGUILayout.IntField("map height", height);

        if (GUILayout.Button("reset map size"))
        {
            Vector2 v = new Vector2(width, height);
            tileCreator.TriggerResetSize(v);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("fill border"))
        {
            tileCreator.fillBorder(1);
            SceneView.RepaintAll();
        }
    }

    bool doInject = false;

    public void OnSceneGUI()
    {
        if (!doInject)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 1)//right button
                {
                    doInject = true;
                    doAdd(Event.current.mousePosition);
                }
            }
        }
        else
        {
            if (Event.current.type == EventType.MouseUp)
            {
                if (Event.current.button == 1)//right button
                {
                    Debug.Log("finish Inject");
                    doInject = false;
                }
            }

            if (Event.current.type == EventType.MouseDrag)
            {
                doAdd(Event.current.mousePosition);
                Event.current.Use();//之後Unity就不會繼續接管event，拿掉後，整個tileCreator會跟著動
            }
        }
    }

    public void doAdd(Vector3 mousePos)
    {
        mousePos.y = Camera.current.pixelHeight - mousePos.y;
        mousePos.z = -Camera.current.transform.position.z;
        Vector3 worldPos = Camera.current.ScreenToWorldPoint(mousePos);
        Vector3 localPos = tileCreator.transform.InverseTransformPoint(worldPos);

        tileCreator.addElement((int)localPos.x, (int)localPos.y);
    }
}
