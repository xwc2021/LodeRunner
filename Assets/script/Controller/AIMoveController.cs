using UnityEngine;
using System.Collections.Generic;
using DiyAStar;

public class AIMoveController : MonoBehaviour
{
    // 參數
    static float waitTime = 0.6f;
    static float movingTime = 2;
    static float maxMovingTime = 4; // 超過這個時間還沒到fixedPoint就強制切換到reFindPath
    static float inTrapTime = 1.5f;
    public static bool Debug_path_timeOut = false;
    public static bool Debug_AI_wait = false;

    public Movable movable;
    public GraphMap graphMap;
    public UserMoveController player;
    public bool debugPath = false;

    public float accumulationTime;
    StateMachine<AIMoveController> sm;
    List<IGraphNode> pathList = null;
    int nowPathIndex;
    int getNowPathIndex() { return nowPathIndex; }

    public StateMachine<AIMoveController> getSM() { return sm; }

    public void catchByTrap()
    {
        Debug.Log("ai inTrap");
        DestroyImmediate(this.gameObject);
    }

    public bool isFindPath(Vector3 from)
    {
        if (debugPath)//測試用
            if (pathList != null)
                foreach (GraphNode node in pathList)
                    node.showNode(false);

        if (player == null)
            return false;

        pathList = graphMap.findPath(from, player.transform.position);

        if (pathList == null)
            return false;

        if (pathList.Count == 0)
            return false;

        if (debugPath)//測試用
            foreach (GraphNode node in pathList)
                node.showNode(true);

        if (pathList.Count > 1)
        {
            nowPathIndex = 1;
            //moveByPaht有加上一定要到達FixPoint的機制，下面的檢查就不需要了
            /*
            GraphNode one = pathList[0];
            GraphNode two = pathList[1];

            //[0][1]是horizontal，transfrom.y!=[0].y => nowPathIndex=0
            if (one.getPosition().y == two.getPosition().y)
            {
                if(transform.position.y!= one.getPosition().y)
                {
                    if(Debug_begin_move_but_notAligh)
                        printDebugMsg("horizontal");
                    nowPathIndex = 0;
                }
                else
                    nowPathIndex = 1;
            }
            //[0][1]是vertical，transfrom.x!=[0].x => nowPathIndex=0
            else if (one.getPosition().x == two.getPosition().x)
            {
                if (transform.position.x != one.getPosition().x)
                {
                    if(Debug_begin_move_but_notAligh)
                        printDebugMsg("vertical");
                    nowPathIndex = 0;
                }
                else
                    nowPathIndex = 1;
            }*/
        }
        else
            nowPathIndex = 0;

        return true;
    }

    public void catchPlayer()
    {
        Debug.Log("AI catch Player");
        if (player != null)
            Destroy(player.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            Movable mov = collision.gameObject.GetComponent<Movable>();
            bool isOnAir = OnAirState.Instance() == mov.getSM().getCurrentState();
            if (isOnAir)
                return;

            //Kinematic和Kinematic不會發生碰撞，但Kinematic和RigidBody會
            getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.catchPlayer, null));
        }
    }

    public MoveCommand getMoveCommand() { return movable.getMoveCommand(); }

    AIMoveController waitThisAI = null;
    public AIMoveController getWaitAI() { return waitThisAI; }
    public void setWaitThisAI(AIMoveController ai) { waitThisAI = ai; }
    public void clearWaitAI() { waitThisAI = null; }

    bool isPlayerOnTop = false;
    List<Vector3> pathFromTrap;
    public bool enterMoveFromTrap()
    {
        Vector3 tileIndex = graphMap.getTileIndex(transform.position);
        Vector3 nowPos = tileIndex + new Vector3(TileCreator.offsetX, TileCreator.offsetY, 0);
        nowPos = graphMap.transform.TransformPoint(nowPos);

        transform.position = nowPos;

        if (pathFromTrap == null)
            pathFromTrap = new List<Vector3>();
        pathFromTrap.Clear();

        Vector3 firstStep = nowPos + new Vector3(0, 1, 0);
        pathFromTrap.Add(firstStep);

        if (isFindPath(firstStep))
        {
            if (pathList.Count > 1)
            {
                pathFromTrap.Add(pathList[1].GetPosition());
            }
            else
            {
                //玩家就站在上面
                isPlayerOnTop = true;
                float myX = transform.position.x;
                if (player.transform.position.x < myX)
                    pathFromTrap.Add(nowPos + new Vector3(-1, 1, 0));
                else
                    pathFromTrap.Add(nowPos + new Vector3(1, 1, 0));
            }
        }
        else
        {
            //找不到路就往右爬
            pathFromTrap.Add(nowPos + new Vector3(1, 1, 0));
        }

        nowPathIndex = 0;

        return isPlayerOnTop;
    }

    public bool isFinishMoveFromTrap()
    {
        if (nowPathIndex == pathFromTrap.Count)
            return true;
        else
            return false;
    }
    public void moveFromTrap()//return isFinish
    {
        if (nowPathIndex == pathFromTrap.Count)//到了
            return;

        Vector3 nowTarget = pathFromTrap[nowPathIndex];

        Vector2 diff = nowTarget - transform.position;
        if (diff.sqrMagnitude < Movable.MoveEpsilon)//非常接近了，瞄準下個節點
        {
            movable.sendMsgStopMove();
            transform.position = nowTarget;
            nowPathIndex += 1;
            return;
        }
        else
        {
            Debug.DrawLine(nowTarget, transform.position, Color.red);
        }

        movable.DefferedMove(diff);
    }

    public bool isFinshMoveByPath()
    {
        if (nowPathIndex == pathList.Count)
            return true;
        else return false;
    }

    IGraphNode nowTarget = null;
    public IGraphNode getNowTarget() { return nowTarget; }
    public bool moveByPath()//return isFinish
    {
        nowTarget = pathList[nowPathIndex];
        Vector2 diff = nowTarget.GetPosition() - transform.position;
        //落到地面時可能不成立，因為GraphNode和角色的y有落差(落地觸發reFindPath)
        if (diff.sqrMagnitude < Movable.MoveEpsilon)//非常接近了，瞄準下個節點
        {
            showNowState("到達:" + nowTarget.getNodeKey());

            movable.sendMsgStopMove();
            transform.position = nowTarget.GetPosition();

            nowPathIndex += 1;

            //這裡要再更新一次，不然fixedUpdate可能用到舊資料
            movable.DefferedMove(Vector2.zero);
            return true;
        }
        else
        {
            Debug.DrawLine(nowTarget.GetPosition(), transform.position, Color.red);
            showNowState("AI Move [" + getNowPathIndex() + "](" + nowTarget.getNodeKey() + ")");
        }

        movable.DefferedMove(diff);
        return false;
    }

    int footMask;
    int AIMask;
    // Use this for initialization
    void Awake()
    {
        sm = new StateMachine<AIMoveController>(this, AIFindingPathState.Instance());
        footMask = LayerMask.GetMask("FootCanTouch", "aiFootCanTouch", "AI");
        AIMask = LayerMask.GetMask("AI");
    }

    // Update is called once per frame
    public string nowState;// for debug
    private void Update()
    {
        sm.execute();
        nowState = sm.getCurrentState().ToString();
    }

    void FixedUpdate()
    {
        movable.UpdateMove(footMask);
    }

    public void restartClock() { accumulationTime = 0; }

    void increaseClockTime()
    {
        accumulationTime += Time.deltaTime;
    }

    public bool MoveTimeIsOverMaxLimit()
    {
        return accumulationTime >= maxMovingTime;
    }

    public bool MoveTimeIsOver()
    {
        increaseClockTime();
        if (accumulationTime >= movingTime)
            return true;
        else
            return false;
    }

    public bool WaitTimeIsOver()
    {
        increaseClockTime();
        if (accumulationTime >= waitTime)
            return true;
        else
            return false;
    }

    public bool inTrapTimeIsOver()
    {
        increaseClockTime();
        if (accumulationTime >= inTrapTime)
            return true;
        else
            return false;
    }

    public string nowStateStr;
    public void showNowState(string msg)
    {
        nowStateStr = msg;
    }

    public void printDebugMsg(string msg)
    {
        Debug.Log(name + ":" + msg);
    }
}
