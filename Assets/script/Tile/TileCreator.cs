using UnityEngine;
using System.Collections;
using UnityEditor;

public class TileCreatorGizmoDrawer
{
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmoFor(TileCreator target, GizmoType gizmoType)
    {
        //沒有設定tileData就不畫
        if (target.tileData == null)
            return;

        float scaleX = target.transform.localScale.x;
        float scaleY = target.transform.localScale.y;

        //draw grid
        Vector3 origin = target.transform.position;
        Vector3 top = origin + scaleY*target.transform.up * target.getHeight();
        Vector3 right = origin + scaleX * target.transform.right * target.getWidth();

        for (int i = 0; i <= target.getWidth(); i++)
        {
            Vector3 offsetFrom = origin + scaleX * target.transform.right * i;
            Vector3 offsetTo = top + scaleX * target.transform.right * i;
            Gizmos.DrawLine(offsetFrom, offsetTo);
        }

        for (int i = 0; i <= target.getHeight(); i++)
        {
            Vector3 offsetFrom = origin + scaleY * target.transform.up * i;
            Vector3 offsetTo = right + scaleY * target.transform.up * i;
            Gizmos.DrawLine(offsetFrom, offsetTo);
        }
    }
}

public class TileCreator : MonoBehaviour {

    public TileData tileData;

    //[note]
    //讓user指定tile：這東西的順序要配合GraphBuilder
    public GameObject[] tiles = new GameObject[5];//這1行只有在MonoBehaviour被建立才會執行1次

    [SerializeField]
    [HideInInspector]
    private GameObject[] objMap;

    public int getWidth() { return tileData.getWidth(); }
    public int getHeight() { return tileData.getHeight(); }
    public int injectValue;

    public static float offsetX = 0.5f;
    public static float offsetY = 0.5f;

    //2維 to 1維陣列
    public int remap(int x, int y) { return x * getHeight() + y; }

    public GameObject getObj(int x, int y)
    {
        return objMap[remap(x, y)];
    }
    public void fillBorder(int value)
    {
        int oldValue = injectValue;
        injectValue = value;

        int y = 0;
        for (int x = 0; x < tileData.getWidth(); x++)
            addElement(x, y);

        y = tileData.getHeight() - 1;
        for (int x = 0; x < tileData.getWidth(); x++)
            addElement(x, y);

        int X = 0;
        for (int Y = 0; Y < tileData.getHeight(); Y++)
            addElement(X, Y);

        X = tileData.getWidth()-1;
        for (int Y = 0; Y < tileData.getHeight(); Y++)
            addElement(X, Y);

        injectValue = oldValue;
    }

    public void TriggerResetSize(Vector2 v)
    {
        int size = (int)v.x * (int)v.y;

        //清空gameObject
        if (objMap != null)
        {
            foreach (GameObject obj in objMap)
            {
                if (obj != null)
                    DestroyImmediate(obj);
            }
        }
        objMap = new GameObject[size];

        Debug.Log("TileCreator.TriggerResetSize()");

        //發送訊息
        gameObject.SendMessage("resetSize", v);
    }

    public void addElement(int x, int y)
    {
        if (tileData == null)
        {
            Debug.Log("[失敗]請先設定TileCreator的tileData");
            return;
        }

        if (objMap == null || objMap.Length ==0)
        {
            Debug.Log("[失敗]請先reset map size");
            return;
        }

        bool success = tileData.addElement(x, y, injectValue);
        if (!success)
            return;

        if (objMap[tileData.remap(x, y)] != null)
            DestroyImmediate(objMap[tileData.remap(x, y)]);

        GameObject refObj = tiles[injectValue];
        if (refObj != null)
        {
            GameObject obj = (GameObject)Instantiate(refObj, Vector3.zero, Quaternion.identity);
            obj.transform.parent = this.transform;
            obj.transform.localPosition = new Vector2(x, y)+new Vector2(TileCreator.offsetX, TileCreator.offsetY);

            //設定parent時Unity會自動調整child的localScale
            obj.transform.localScale = new Vector3(1, 1, 1);

            objMap[tileData.remap(x, y)] = obj;
        }
    }
}