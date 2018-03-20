using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DiyAStar;


public enum TileMapValue { None, Stone, Brick, Rope, Ladder, Player, Monster,Money,FakeBick, Destination }

//用位元值存方向
//上下左右
// 0 0 0 0 =>無
// 0 1 0 1 =>下、右
public enum Arrow { Up = 8, Down = 4, Left = 2, Right = 1, Dot = 0, JumpPoint = 16, None = 32 };

public class LodeRunnerGraphBuilder : MonoBehaviour {

    public TileData tileData;
    public TileCreator tileCreator;
    public GraphMap graphMap;

    [SerializeField]
    [HideInInspector]
    private int width;

    [SerializeField]
    [HideInInspector]
    private int height;

    [SerializeField]
    [HideInInspector]
    private byte[] arrowMap;

    [SerializeField]
    [HideInInspector]
    private int[] jumpToMap;

    public bool showArrowMap = false;
    public byte getArrowMapValue(int x, int y) { return arrowMap[remap(x, y)]; }
    public int getWidth() { return width; }
    public int getHeight() { return height; }

    //2維 to 1維陣列
    public int remap(int x, int y) { return x * height + y; }

    //防止出界
    public bool isInMap(int x, int y) { return xInRange(x) && yInRange(y); }
    bool xInRange(int x) { return (x < width) && (x >= 0); }
    bool yInRange(int y) { return (y < height) && (y >= 0); }


    public Vector2 getTileIndex(Vector3 pos)
    {
        Vector3 local = transform.InverseTransformPoint(pos);
        return new Vector2((int)local.x, (int)local.y);
    }

    public Vector2 getTileCenterPositionInWorld(Vector3 pos)
    {
        Vector2 local = transform.InverseTransformPoint(pos);
        local = new Vector2((int)local.x, (int)local.y);

        return getTileCenterPositionInWorld((int)local.x, (int)local.y);
    }

    public Vector2 getTileCenterPositionInWorld(int tileX, int tileY)
    {
        Vector2 local = new Vector2((int)tileX, (int)tileY) + new Vector2(TileCreator.offsetX, TileCreator.offsetY);
        return transform.TransformPoint(local);
    }

    public bool canDig(int x, int y)
    {
        bool brick = isBrick(x, y);
        bool notIsBlock = !isBlock(x, y + 1);
        if (brick && notIsBlock)
            return true;
        else return false;
    }

    public bool isJumpPoint(int x, int y)
    {
        bool b = arrowMap[remap(x, y)] == (byte)(Arrow.JumpPoint);
        return b;
    }

    public void resetSize(Vector2 v)
    {
        width = (int)v.x;
        height = (int)v.y;
        int size = width * height;
        arrowMap = new byte[size];
        jumpToMap = new int[size];
        Debug.Log("LodeRunnerGraphBuilder.resetSize() finish");
    }

    //[疑問？]
    //指向TileData的資料，但發現這樣用會出錯
    //byte[] tileMap;

    public bool hasArrow(byte value, Arrow direct)
    {
        return (value & (byte)direct) > 0;
    }

    public bool isBlock(int x, int y)
    {
        TileMapValue tile = (TileMapValue)tileData.getTileMapValue(x, y);
        return (tile == TileMapValue.Brick) || (tile == TileMapValue.Stone);
    }

    public bool isBrick(int x, int y)
    {
        TileMapValue tile = (TileMapValue)tileData.getTileMapValue(x, y);
        return (tile == TileMapValue.Brick);
    }

    public bool isLadder(int x, int y)
    {
        TileMapValue tile = (TileMapValue)tileData.getTileMapValue(x, y);
        return (tile == TileMapValue.Ladder);
    }

    public bool isRope(int x, int y)
    {
        TileMapValue tile = (TileMapValue)tileData.getTileMapValue(x, y);
        return (tile == TileMapValue.Rope);
    }

    public bool isNull(int x, int y)
    {
        TileMapValue tile = (TileMapValue)tileData.getTileMapValue(x, y);
        return (tile == TileMapValue.None);
    }

    //是否為空
    bool isEmpty(int x, int y)
    {
        TileMapValue tile = (TileMapValue)tileData.getTileMapValue(x, y);
        return tile == TileMapValue.None
            || tile == TileMapValue.Player
            || tile == TileMapValue.Monster
            || tile == TileMapValue.Money
            || tile == TileMapValue.FakeBick
            || tile == TileMapValue.Destination;
    }

    //回傳JumpPoint到達的Node
    Vector2 findJumpTo(int x, int y)
    {
        while (true)
        {
            //一定要有地板，不然會超出陣列
            if (y < 0) Debug.Log("Jump Point找不到地板(" + x + "," + y + ")");
            byte value = arrowMap[remap(x, y)];
            //JumpPoint一定會碰到CrossNode
            if (value != (byte)Arrow.None && value != (byte)Arrow.JumpPoint)//排除JumpPoint
                return new Vector2(x, y);
            y = y - 1;
        }
    }

    void buildJumpToMap()
    {
        Debug.Log("buildJumpToMap()");

        for (int x = 0; x < width; x++)
        {
            for (int y = height - 1; y >= 0; y--)
            {
                byte value = arrowMap[remap(x, y)];
                if (value == (byte)Arrow.JumpPoint)
                {
                    Vector2 Index = findJumpTo(x, y - 1);
                    jumpToMap[remap(x, y)] = remap((int)Index.x, (int)Index.y);//直接存成1維索引
                }

            }
        }
    }

    bool checkIsJumpPoint(int x, int y)
    {
        bool jumpPoint = isInMap(x, y) && isEmpty(x, y)
                            && (isEmpty(x, y - 1) || isRope(x, y - 1));
        return jumpPoint;
    }

    //建立「箭頭圖」
    void buildArrowMap()
    {
        Debug.Log("buildArrowMap()");

        //初始化為None
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                arrowMap[remap(x, y)] = (byte)Arrow.None;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                TileMapValue tile = (TileMapValue)tileData.getTileMapValue(x, y);
                switch (tile)
                {
                    case TileMapValue.Brick:
                    case TileMapValue.Stone:
                        //方塊的上面是否為通道
                        if (isInMap(x, y + 1) && !isBlock(x, y + 1))
                        {
                            int t3 = (byte)Arrow.Dot;
                            if (isInMap(x + 1, y + 1) && !isBlock(x + 1, y + 1))
                                t3 = t3 + (byte)Arrow.Right;
                            if (isInMap(x - 1, y + 1) && !isBlock(x - 1, y + 1))
                                t3 = t3 + (byte)Arrow.Left;
                            arrowMap[remap(x, y + 1)] = (byte)t3;

                            //右上是否為JumpPoint
                            if (checkIsJumpPoint(x + 1, y + 1))
                                arrowMap[remap(x + 1, y + 1)] = (byte)(Arrow.JumpPoint);

                            //左上是否為JumpPoint
                            if (checkIsJumpPoint(x - 1, y + 1))
                                arrowMap[remap(x - 1, y + 1)] = (byte)(Arrow.JumpPoint);
                        }
                        break;
                    case TileMapValue.Rope:
                        //下邊是否為JP
                        if (checkIsJumpPoint(x, y - 1))
                            arrowMap[remap(x, y - 1)] = (byte)(Arrow.JumpPoint);

                        //右邊是否為JP
                        if (checkIsJumpPoint(x + 1, y))
                            arrowMap[remap(x + 1, y)] = (byte)(Arrow.JumpPoint);

                        //左邊是否為JP
                        if (checkIsJumpPoint(x - 1, y))
                            arrowMap[remap(x - 1, y)] = (byte)(Arrow.JumpPoint);

                        //中間為那種通道
                        int t1 = (byte)Arrow.Dot;
                        if (isInMap(x, y - 1) && !isBlock(x, y - 1))
                            t1 = t1 + (byte)Arrow.Down;
                        if (isInMap(x + 1, y) && !isBlock(x + 1, y))
                            t1 = t1 + (byte)Arrow.Right;
                        if (isInMap(x - 1, y) && !isBlock(x - 1, y))
                            t1 = t1 + (byte)Arrow.Left;
                        arrowMap[remap(x, y)] = (byte)t1;

                        break;
                    case TileMapValue.Ladder:
                        //右邊是否為JumpPoint
                        if (checkIsJumpPoint(x + 1, y))
                            arrowMap[remap(x + 1, y)] = (byte)(Arrow.JumpPoint);

                        //左邊是否為JumpPoint
                        if (checkIsJumpPoint(x - 1, y))
                            arrowMap[remap(x - 1, y)] = (byte)(Arrow.JumpPoint);


                        //上邊是否為通道
                        if (isInMap(x, y + 1) && !isBlock(x, y + 1))
                        {
                            int temp = (byte)Arrow.Down;
                            if (isInMap(x + 1, y + 1) && !isBlock(x + 1, y + 1))
                                temp = temp + (byte)Arrow.Right;
                            if (isInMap(x - 1, y + 1) && !isBlock(x - 1, y + 1))
                                temp = temp + (byte)Arrow.Left;
                            arrowMap[remap(x, y + 1)] = (byte)temp;

                            //右上是否為JP
                            if (checkIsJumpPoint(x + 1, y + 1))
                                arrowMap[remap(x + 1, y + 1)] = (byte)Arrow.JumpPoint;
                            //左上是否為JP
                            if (checkIsJumpPoint(x - 1, y + 1))
                                arrowMap[remap(x - 1, y + 1)] = (byte)Arrow.JumpPoint;
                        }

                        //下邊是否為JP
                        if (isInMap(x, y - 1) && !isBlock(x, y - 1))
                        {
                            if (checkIsJumpPoint(x, y - 1))
                                arrowMap[remap(x, y - 1)] = (byte)Arrow.JumpPoint;

                            //右下是否為JP
                            if (checkIsJumpPoint(x + 1, y - 1))
                                arrowMap[remap(x + 1, y - 1)] = (byte)Arrow.JumpPoint;

                            //左下是否為JP
                            if (checkIsJumpPoint(x - 1, y - 1))
                                arrowMap[remap(x - 1, y - 1)] = (byte)Arrow.JumpPoint;
                        }

                        //中間為那種通道
                        int t2 = (byte)Arrow.Dot;
                        if (isInMap(x, y + 1) && !isBlock(x, y + 1))
                            t2 = t2 + (byte)Arrow.Up;
                        if (isInMap(x, y - 1) && !isBlock(x, y - 1))
                            t2 = t2 + (byte)Arrow.Down;
                        if (isInMap(x + 1, y) && !isBlock(x + 1, y))
                            t2 = t2 + (byte)Arrow.Right;
                        if (isInMap(x - 1, y) && !isBlock(x - 1, y))
                            t2 = t2 + (byte)Arrow.Left;
                        arrowMap[remap(x, y)] = (byte)t2;

                        break;
                }
            }
        }
    }

    UserMoveController setPlayerContext()
    {
        UserMoveController player = null;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject obj = tileCreator.getObj(x, y);
                if (obj != null)
                {
                    player = obj.GetComponentInChildren<UserMoveController>();
                    if (player != null)
                    {
                        player.graphBuilder = this;
                        player.tileCreator = tileCreator;
                        return player;
                    }
                }
            }
        }

        return player;
    }

    void setAIContext(UserMoveController player)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject obj = tileCreator.getObj(x, y);
                if (obj != null)
                {
                    Movable movable = obj.GetComponentInChildren<Movable>();
                    if (movable != null)
                        movable.graphBuilder = this;

                    AIMoveController ai = obj.GetComponentInChildren<AIMoveController>();
                    if (ai != null)
                    {
                        ai.player = player;
                        ai.graphMap = graphMap;
                    }
                }
            }
        }
    }

    //產生將來用作生成Graph的資料
    void buildGraph()
    {
        Debug.Log("buildGraph()");

        bool success = graphMap.resetMap(width, height);
        if (!success)
            return;

        //綁定Brick得tileData
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject obj=tileCreator.getObj(x, y);
                if (obj != null)
                {
                    Brick brick = obj.GetComponentInChildren<Brick>();
                    if (brick != null)
                    {
                        brick.tileData = tileData;
                        brick.x = x;
                        brick.y = y;
                    }
                        
                } 
            }
        }

        //建立node
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte arrow = arrowMap[remap(x, y)];
                if (arrow != (byte)Arrow.None)
                {
                    string description="";
                    if (arrow == (byte)Arrow.JumpPoint)
                        description="JumpPoint";
                    else if (isLadder(x, y))
                        description = "Ladder";
                    else if(isRope(x,y))
                        description = "Rope";
                    graphMap.addGraphNode(x, y, description);
                }
            }
        }

        //建立connection
        for (int y = 0; y < height; y++)
        { 
            for (int x = 0; x < width; x++)
            {
                int from = remap(x, y);
                byte arrow = arrowMap[from];
                if (arrow != (byte)Arrow.None && arrow != (byte)Arrow.Dot)
                {                
                    
                    if (arrow == (byte)Arrow.JumpPoint)
                    {
                        int to = jumpToMap[from];
                        float cost = (from-to);
                        graphMap.addConnection(from, to,cost);
                        continue;
                    }

                    if (hasArrow(arrow, Arrow.Up))
                    {
                        int to = tileData.remap(x, y+1);
                        graphMap.addConnection(from, to,1);
                    }
                    if (hasArrow(arrow, Arrow.Down))
                    {
                        int to = tileData.remap(x, y - 1);
                        graphMap.addConnection(from, to, 1);
                    }
                    if (hasArrow(arrow, Arrow.Left))
                    {
                        //讓JumpPointCost大一點
                        int to = tileData.remap(x-1, y );
                        byte nextArrow = arrowMap[to];

                        float cost = 1;
                        if (nextArrow == (byte)Arrow.JumpPoint)//如果next是JumpPoint，cost稍大，這樣會優先走樓梯
                            cost += 1;

                        graphMap.addConnection(from, to, cost);
                    }
                    if (hasArrow(arrow, Arrow.Right))
                    {
                        int to = tileData.remap(x + 1, y);

                        byte nextArrow = arrowMap[to];

                        float cost = 1;
                        if (nextArrow == (byte)Arrow.JumpPoint)//如果next是JumpPoint，cost稍大，這樣會優先走樓梯
                            cost += 1;
                        graphMap.addConnection(from, to, cost);
                    }
                }
            }   
        }
    }

    public void buildAll()
    {
        if (tileData == null)
        {
            Debug.Log("[失敗]請先設定LodeRunnerGraphBuilder的tileData");
            return;
        }

        if (graphMap == null)
        {
            Debug.Log("[失敗]請先設定LodeRunnerGraphBuilder的graphMap");
            return;
        }

        if (width == 0 || height == 0)
        {
            Vector2 size = new Vector2(tileData.getWidth(), tileData.getHeight());
            resetSize(size);
        }

        buildArrowMap();
        buildJumpToMap();

        buildGraph();
        UserMoveController player = setPlayerContext();
        setAIContext(player);

        Debug.Log("buildAll() finish");
    }

    public void testCsharp()
    {
        testCase4();
    }

    public void testCase4()
    {
        float value = 7.52f;
        int temp = (int)(value * 10);
        float vF= temp / 10.0f;
        Debug.Log(vF);
    }

    public void testCase3()
    {
        //測試PripriorityQueue
        AsiaBoy[] b = new AsiaBoy[10];  
        b[0] = new AsiaBoy(20);
        b[1] = new AsiaBoy(14);
        b[2] = new AsiaBoy(0);
        b[3] = new AsiaBoy(3);
        b[4] = new AsiaBoy(6);
        b[5] = new AsiaBoy(8);
        b[6] = new AsiaBoy(12);
        b[7] = new AsiaBoy(11);
        b[8] = new AsiaBoy(4);
        b[9] = new AsiaBoy(6);
        PriorityQueue<AsiaBoy> q = new PriorityQueue<AsiaBoy>();

        foreach (AsiaBoy boy in b)
            q.Add(boy, boy.length);

        q.popup();

        List<QueueElement<AsiaBoy>>.Enumerator it = q.getEnumerator();
        string message="";
        while (it.MoveNext())
        {
            AsiaBoy boy = it.Current.obj;
            message += "["+boy.length+ "]";
        }

        Debug.Log("[inside PripriorityQueue] = "+message);
    }

    public void testCase2()
    {
        //測試List insert
        List<int> a= new List<int>();
        a.Add(2);
        a.Add(4);
        a.Insert(0, 12);
        a.Insert(a.Count, 6);
        foreach (int i in a)
            Debug.Log(i);
    }

    public void testCase1()
    {
        //測試array是否傳指標
        TestArray tArray=new TestArray();
        int[] a =tArray.getA();
        a[0] = 60;
        tArray.show();
        tArray.reset();
        tArray.show();

        Debug.Log("outside");
        foreach (int i in a)
            Debug.Log(i);
    }
}

public class AsiaBoy
{
    public float length;
    public AsiaBoy( float pLength)
    {
        length = pLength;
    }
}

//測試用
public class TestArray
{
    private int[] a;
    public TestArray()
    {
        a = new int[2];
        a[0] = 10;
        a[1] = 20;
    }

    public int[] getA()
    {
        return a;
    }

    public void reset()
    {
        a[0] = 6;
        a[1] = 6;
    }

    public void show()
    {
        Debug.Log("show()");
        foreach (int i in a)
            Debug.Log(i);
    }
}
