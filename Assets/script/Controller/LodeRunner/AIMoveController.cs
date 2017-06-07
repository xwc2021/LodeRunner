using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class AIMoveController : MonoBehaviour
{
    //參數
    static float waitTime = 0.6f;
    static float movingTime = 2;
    static float maxMovingTime = 4;//超過這個時間還沒到fixedPoint就強制切換到reFindPath
    static float inTrapTime = 1.5f;
    static bool Debug_path_timeOut = false;
    static bool Debug_AI_wait = false;
    
    static bool Debug_begin_move_but_notAligh = false;

    public Movable movable;
    public GraphMap graphMap;
    public UserMoveController player;
    public bool debugPath = false;

    public float accumulationTime;
    StateMachine<AIMoveController> sm;
    List<GraphNode> pathList = null;
    int nowPathIndex;
    int getNowPathIndex() { return nowPathIndex; }

    public StateMachine<AIMoveController> getSM() { return sm; }



    public void catchByTrap()
    {
        Debug.Log("ai inTrap");
        DestroyImmediate(this.gameObject);
    }

    bool isFindPath(Vector3 from)
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
            nowPathIndex= 1;
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

    void catchPlayer()
    {
        Debug.Log("AI catch Player");
        if (player != null)
            Destroy(player.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            //Kinematic和Kinematic不會發生碰撞，但Kinematic和RigidBody會
            getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.catchPlayer,null));
        }
    }

    public MoveCommand getMoveCommand() { return movable.getMoveCommand(); }

    AIMoveController waitThisAI=null;
    public AIMoveController getWaitAI() { return waitThisAI; }
    void setWaitThisAI(AIMoveController ai) { waitThisAI = ai; }
    void clearWaitAI( ) { waitThisAI = null; }
 

    bool isPlayerOnTop = false;
    List<Vector3> pathFromTrap;
    bool enterMoveFromTrap()
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
                pathFromTrap.Add(pathList[1].getPosition());
            }
            else
            {
                //玩家就站在上面
                isPlayerOnTop = true;
                float myX = transform.position.x;
                if (player.transform.position.x< myX)
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

    bool isFinishMoveFromTrap()
    {
        if (nowPathIndex == pathFromTrap.Count)
            return true;
        else
            return false;
    }

    void moveFromTrap()//return isFinish
    {
        if (nowPathIndex == pathFromTrap.Count)//到了
            return ;

        Vector3 nowTarget = pathFromTrap[nowPathIndex];

        Vector2 diff =nowTarget - transform.position;
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

    bool isFinshMoveByPath()
    {
        if (nowPathIndex == pathList.Count)
            return true;
        else return false;
    }

    GraphNode nowTarget = null;
    public GraphNode getNowTarget() { return nowTarget; }
    bool moveByPath()//return isFinish
    {
        nowTarget =pathList[nowPathIndex];
        
        Vector2 diff = nowTarget.getPosition() - transform.position;
        //落到地面時可能不成立，因為GraphNode和角色的y有落差(落地觸發reFindPath)
        if (diff.sqrMagnitude < Movable.MoveEpsilon)//非常接近了，瞄準下個節點
        {
            showNowState("到達:"+nowTarget.nodeKey);

            movable.sendMsgStopMove();
            transform.position = nowTarget.getPosition();

            nowPathIndex += 1;

            //這裡要再更新一次，不然fixedUpdate可能用到舊資料
            movable.DefferedMove(Vector2.zero);
            return true;
        }
        else
        {        
            Debug.DrawLine(nowTarget.getPosition(), transform.position, Color.red);
            showNowState("AI Move [" + getNowPathIndex() + "](" + nowTarget.nodeKey + ")");
        }

        movable.DefferedMove(diff);
        return false;
    }

    int footMask;
    int AIMask;
    // Use this for initialization
    void Awake() {
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

    void restartClock()
    {
        accumulationTime = 0;
    }

    void increaseClockTime()
    {
        accumulationTime += Time.deltaTime;
    }

    bool MoveTimeIsOverMaxLimit()
    {
        return accumulationTime >= maxMovingTime;
    }

    bool MoveTimeIsOver()
    {
        increaseClockTime();
        if (accumulationTime >= movingTime)
            return true;
        else
            return false;
    }

    bool WaitTimeIsOver()
    {
        increaseClockTime();
        if (accumulationTime >= waitTime)
            return true;
        else
            return false;
    }

    bool inTrapTimeIsOver()
    {
        increaseClockTime();
        if (accumulationTime >= inTrapTime)
            return true;
        else
            return false;
    }

    public string nowStateStr;
    void showNowState(string msg)
    {
        nowStateStr = msg;
    }

    void printDebugMsg(string msg)
    {
        Debug.Log(name + ":" + msg);
    }

    class AIFindingPathState : State<AIMoveController>
    {
        private AIFindingPathState() { }
        static AIFindingPathState instance;
        public static AIFindingPathState Instance()
        {
            if (instance == null)
                instance = new AIFindingPathState();
            return instance;
        }

        public override void enter(AIMoveController obj)
        {
        }

        public override void execute(AIMoveController obj)
        {
            obj.showNowState("Finding Path");
            if (obj.isFindPath(obj.transform.position))
                obj.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.findPathOk,null));
        }

        public override void onMessage(AIMoveController obj, StateMsg<AIMoveController> msg)
        {
            AIMsg type = (AIMsg)msg.type;
            switch (type)
            {
                case AIMsg.findPathOk:
                    obj.getSM().changeState(AIMoveState.Instance());
                    break;
                case AIMsg.catchByTrap:
                    obj.getSM().changeState(AICatchByTrapState.Instance());
                    break;
                case AIMsg.catchPlayer:
                    obj.getSM().changeState(AICatchPlayerState.Instance());
                    break;
            }
        }
    }

    public class AIWaitState : State<AIMoveController>
    {
        private AIWaitState() { }
        static AIWaitState instance;
        public static AIWaitState Instance()
        {
            if (instance == null)
                instance = new AIWaitState();
            return instance;
        }

        public override void enter(AIMoveController obj)
        {
            obj.movable.sendMsgWait();

            if (AIMoveController.Debug_AI_wait)
                obj.printDebugMsg("進入 AIWait State");

            obj.restartClock();
        }

        public override void exit(AIMoveController obj)
        {
            if (AIMoveController.Debug_AI_wait)
                obj.printDebugMsg("exit AIWait State");

            obj.clearWaitAI();
            obj.movable.sendMsgStopMove();//要記得切換到stop狀態
        }

        public override void execute(AIMoveController obj)
        {
            obj.showNowState("AIWait State("+obj.waitThisAI.name+")");
            if (obj.WaitTimeIsOver())
            {
                obj.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.moveTimeIsOver,null));
                return;
            }
        }

        public override void onMessage(AIMoveController obj, StateMsg<AIMoveController> msg)
        {
            AIMsg type = (AIMsg)msg.type;
            switch (type)
            {
                case AIMsg.moveTimeIsOver:
                    obj.getSM().changeState(AIFindingPathState.Instance());
                    break;
            }
        }
    }

    class AIMoveState : State<AIMoveController>
    {
        private AIMoveState() { }
        static AIMoveState instance;
        public static AIMoveState Instance()
        {
            if (instance == null)
                instance = new AIMoveState();
            return instance;
        }

        public override void enter(AIMoveController obj)
        {
            //obj.printDebugMsg("enter AIMoveState");
            obj.restartClock();
        }

        public override void exit(AIMoveController obj)
        {
            //obj.printDebugMsg("exit AIMoveState");
        }

        public override void execute(AIMoveController obj)
        {
            bool toFixPoint =obj.moveByPath();

            if (obj.isFinshMoveByPath())
            {
                obj.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.reFindPath,null));
                return;
            }

            if (obj.MoveTimeIsOver())
            {
                if (!toFixPoint)
                {
                    if (AIMoveController.Debug_path_timeOut)
                        obj.printDebugMsg("[注意!]wait to target path node " + obj.getNowTarget().nodeKey);

                    //有可能發生這種永遠到不了fixPoint的清況
                    //LodeRunnerScreenshot\fixed\never_to_FixPoint.png
                    if (obj.MoveTimeIsOverMaxLimit())
                    {
                        if (AIMoveController.Debug_path_timeOut)
                            obj.printDebugMsg("[強制切換]to AIFindingPathState");
                        obj.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.moveTimeIsOver, null));
                    }

                }
                else
                {
                    if (AIMoveController.Debug_path_timeOut)
                        obj.printDebugMsg("[即將切換]to AIFindingPathState");

                    obj.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.moveTimeIsOver, null));
                }           
            }
        }

        public override void onMessage(AIMoveController obj, StateMsg<AIMoveController> msg)
        {
            AIMsg type = (AIMsg)msg.type;
            switch (type)
            {
                case AIMsg.reFindPath:
                case AIMsg.moveTimeIsOver:
                    obj.getSM().changeState(AIFindingPathState.Instance());
                    break;
                case AIMsg.catchPlayer:
                    obj.getSM().changeState(AICatchPlayerState.Instance());
                    break;
                case AIMsg.catchByTrap:
                    obj.getSM().changeState(AICatchByTrapState.Instance());
                    break;
                case AIMsg.waitForSomebody:
                    AIMoveController ai = (AIMoveController)msg.sender;
                    obj.setWaitThisAI(ai);
                    obj.getSM().changeState(AIWaitState.Instance());
                    break;
            }
        }
    }

    class AICatchPlayerState : State<AIMoveController>
    {
        private AICatchPlayerState() { }
        static AICatchPlayerState instance;
        public static AICatchPlayerState Instance()
        {
            if (instance == null)
                instance = new AICatchPlayerState();
            return instance;
        }

        public override void enter(AIMoveController obj)
        {
            obj.catchPlayer();
            obj.movable.sendMsgStopMove();
        }

        public override void execute(AIMoveController obj)
        {
            obj.showNowState("Catch Player");
        }
    }

    public class AICatchByTrapState : State<AIMoveController>
    {
        private AICatchByTrapState() { }
        static AICatchByTrapState instance;
        public static AICatchByTrapState Instance()
        {
            if (instance == null)
                instance = new AICatchByTrapState();
            return instance;
        }

        public override void enter(AIMoveController obj)
        {
            obj.printDebugMsg("catch by trap");
            obj.restartClock();
        }

        public override void execute(AIMoveController obj)
        {
            obj.showNowState("Catch By Trap");
            if (obj.inTrapTimeIsOver())
                obj.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.moveFromTrap,null));
        }

        public override void onMessage(AIMoveController obj, StateMsg<AIMoveController> msg)
        {
            AIMsg type = (AIMsg)msg.type;
            switch (type)
            {
                case AIMsg.moveFromTrap:
                    obj.getSM().changeState(AIMoveFromTrapState.Instance());
                    break;
            }
        }
    }

    class AIMoveFromTrapState : State<AIMoveController>
    {
        private AIMoveFromTrapState() { }
        static AIMoveFromTrapState instance;
        public static AIMoveFromTrapState Instance()
        {
            if (instance == null)
                instance = new AIMoveFromTrapState();
            return instance;
        }

        public override void enter(AIMoveController obj)
        {
            obj.movable.getSM().handleMessage(new StateMsg<Movable>((int)MovableMsg.toKinematic,null));
            bool playerOnTop = obj.enterMoveFromTrap();
        }

        public override void execute(AIMoveController obj)
        {
            obj.showNowState("Move From Trap (" + obj.getMoveCommand().ToString() + ")");
            obj.moveFromTrap();
            if (obj.isFinishMoveFromTrap())
                obj.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.reFindPath,null));
        }

        public override void onMessage(AIMoveController obj, StateMsg<AIMoveController> msg)
        {
            AIMsg type = (AIMsg)msg.type;
            switch (type)
            {
                case AIMsg.reFindPath:
                    obj.movable.getSM().handleMessage(new StateMsg<Movable>((int)MovableMsg.breakKinematic,null));
                    obj.getSM().changeState(AIFindingPathState.Instance());
                    break;
                case AIMsg.catchPlayer:
                    obj.getSM().changeState(AICatchPlayerState.Instance());
                    break;
            }
        }
    }
}

enum AIMsg { findPathOk,moveTimeIsOver,catchPlayer,reFindPath,catchByTrap,moveFromTrap, waitForSomebody }

