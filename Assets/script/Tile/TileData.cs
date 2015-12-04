using UnityEngine;
using System.Collections;

public class TileData : MonoBehaviour {

    [SerializeField]
    [HideInInspector]
    private int width;

    [SerializeField]
    [HideInInspector]
    private int height;

    //Unity不支持多維陣列序列化(所以改成1維陣列模擬)
    [SerializeField]
    [HideInInspector]
    private byte[] tileMap;

    public int getWidth() { return width; }
    public int getHeight() { return height; }
    public byte getTileMapValue(int x, int y){return tileMap[remap(x, y)];}

    //2維 to 1維陣列
    public int remap(int x, int y){return x * height + y;}

    //防止出界
    public bool isInMap(int x, int y){return xInRange(x) && yInRange(y);}
    bool xInRange(int x) { return (x < width) && (x >= 0); }
    bool yInRange(int y) { return (y < height) && (y >= 0); }

    public void resetSize(Vector2 v)
    {
        width = (int)v.x;
        height = (int)v.y;

        int size = width * height;
        tileMap = new byte[size];

        Debug.Log("TileData.resetSize() finish");
    }

    public bool addElement(int x, int y,int injectValue)
    {
        if (tileMap == null)
        {
            Debug.Log("reset size first.");
            return false;
        }

        if(xInRange(x)&&yInRange(y))
        {
            tileMap[remap(x, y)] = (byte)injectValue;
            return true;
        }

        return false;
    }

    public void setTileValue(int x, int y, int tempValue)//用於遊戲中
    {
        tileMap[remap(x, y)] = (byte)tempValue;
    }
}
