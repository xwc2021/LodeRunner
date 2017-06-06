using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class AIMoveController : MonoBehaviour
{
    //參數
    static float waitTime = 0.6f;
    static float movingTime = 2;
    static float inTrapTime = 1.5f;
    static bool Debug_path_timeOut = false;
    static bool Debug_AI_wait = false;
    static bool Debug_Oncoming = false;//迎面而來
    static bool Debug_begin_move_but_notAligh = false;

    public Movable movable;
    public GraphMap graphMap;
    public UserMoveController player;
    public bool debugPath = false;

    float accumulationTime;
    StateMachine<AIMoveController> sm;
    List<GraphNode> pathList = null;
    int nowPathIndex;
    int getNowPathIndex() { return nowPathIndex; }

    public StateMachine<AIMoveController> getSM() { return sm; }

    delegate void funPtr(MonoBehaviour sender);
    void handleTooClose(AIMoveController ai, funPtr doFunction)
    {
        //迎面而來的情況 => <=
        //比較距離，近者勝出；距離一樣，先搶先贏
        Vector2 v1 = transform.position - nowTarget.getPosition();
        Vector2 v2 = ai.transform.position - nowTarget.getPosition();
        float myD = v1.sqrMagnitude;
        float d = v2.sqrMagnitude;

        if (name == "A")
            print((myD < d) + "," + (myD > d));

        if (myD < d)
        {
            if (Debug_Oncoming)
                printDebugMsg("我比" + ai.name + "近");
            doFunction(this);
        }
        else if (myD > d)
        {
            if (Debug_Oncoming)
                printDebugMsg("我比" + ai.name + "遠他的行動(" + ai.movable.getMoveCommand().ToString() + ")");

            getSM().handleMessage(new StateMsg((int)AIMsg.waitForSomebody, null, ai));
        }
        else //看來是平手了
        {
            if (Debug_Oncoming)
                printDebugMsg(ai.name + "等我");
            ai.getSM().handleMessage(new StateMsg((int)AIMsg.waitForSomebody, null, this));
            doFunction(this);
        }
    }

    AIMoveController tooCloseDetect(Vector2 leftUp,Vector2 rightUp, Vector2 leftDown, Vector2 rightDown)
    {
        Collider2D touchZone = movable.tooCloseDetect(leftUp, rightUp, leftDown, rightDown, AIMask);

        if (touchZone != null)
            return touchZone.GetComponent<AIMoveController>();
        else
            return null;
    }

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

        if (pathList.Count > 1)//1個以上，就直接從第2個開始當目標
        {
            //[待辦]加入判斷
            
            
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
            }
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
            getSM().handleMessage(new StateMsg((int)AIMsg.catchPlayer));
        }
    }

    MoveCommand getMoveCommand() { return movable.getMoveCommand(); }

    AIMoveController waitThisAI=null;
    AIMoveController getWaitAI() { return waitThisAI; }
    void setWaitThisAI(AIMoveController ai) { waitThisAI = ai; }
    void clearWaitAI( ) { waitThisAI = null; }
    void sendMove(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            //左右移動
            if (dir.x > 0)
            {
                AIMoveController ai = tooCloseDetect(
                    new Vector2(Movable.tooCloseOffset, Movable.tooCloseHalfLongSide),
                    new Vector2(Movable.tooCloseOffset + Movable.tooCloseShortSide,Movable.tooCloseHalfLongSide),
                    new Vector2(Movable.tooCloseOffset,-Movable.tooCloseHalfLongSide), 
                    new Vector2(Movable.tooCloseOffset + Movable.tooCloseShortSide, -Movable.tooCloseHalfLongSide)
                    );
                if (ai != null)
                {
                    bool aiWaitButNotForMe = ai.getSM().getCurrentState() == AIWaitState.Instance() && ai.getWaitAI()!=this;
                    bool aiOnAir = ai.movable.getSM().getCurrentState() == Movable.OnAirState.Instance();
                    bool isBothHorizontal = Mathf.Abs(ai.transform.position.y - this.transform.position.y)<0.5f;
                    bool fitCommand = ai.getMoveCommand() == MoveCommand.right
                        || ai.getMoveCommand() == MoveCommand.up
                        || ai.getMoveCommand() == MoveCommand.down
                    || ( ai.getMoveCommand() == MoveCommand.stop && isBothHorizontal);
                    bool stopMoveSituation = fitCommand || aiOnAir || aiWaitButNotForMe ;
                    if (stopMoveSituation)
                        movable.sendMsgStopMove();
                    else
                        handleTooClose(ai, movable.sendMsgMoveRight);
                }
                else
                    movable.sendMsgMoveRight();
            }
            else if (dir.x < 0)
            {
                AIMoveController ai = tooCloseDetect(
                    new Vector2(-Movable.tooCloseOffset, Movable.tooCloseHalfLongSide),
                    new Vector2(-Movable.tooCloseOffset - Movable.tooCloseShortSide, Movable.tooCloseHalfLongSide),
                    new Vector2(-Movable.tooCloseOffset, -Movable.tooCloseHalfLongSide), 
                    new Vector2(-Movable.tooCloseOffset - Movable.tooCloseShortSide, -Movable.tooCloseHalfLongSide)
                    );
                if (ai != null)
                {
                    bool aiWaitButNotForMe = ai.getSM().getCurrentState() == AIWaitState.Instance() && ai.getWaitAI() != this;
                    bool aiOnAir = ai.movable.getSM().getCurrentState() == Movable.OnAirState.Instance();
                    bool isBothHorizontal = Mathf.Abs(ai.transform.position.y - this.transform.position.y) < 0.5f;
                    bool fitCommand = ai.getMoveCommand() == MoveCommand.left
                        || ai.getMoveCommand() == MoveCommand.up
                        || ai.getMoveCommand() == MoveCommand.down
                    ||( ai.getMoveCommand() == MoveCommand.stop && isBothHorizontal);
                    bool stopMoveSituation = fitCommand || aiOnAir || aiWaitButNotForMe  ;
                    if (stopMoveSituation)
                        movable.sendMsgStopMove();
                    else
                        handleTooClose(ai, movable.sendMsgMoveLeft);
                }
                else
                    movable.sendMsgMoveLeft();
            }
        }
        else
        {
            //上下移動
            if (dir.y > 0)
            {
                AIMoveController ai = tooCloseDetect(new Vector2(-Movable.tooCloseHalfLongSide, Movable.tooCloseOffset + Movable.tooCloseShortSide),
                    new Vector2(Movable.tooCloseHalfLongSide, Movable.tooCloseOffset + Movable.tooCloseShortSide), 
                    new Vector2(-Movable.tooCloseHalfLongSide, Movable.tooCloseOffset ), 
                    new Vector2(Movable.tooCloseHalfLongSide, Movable.tooCloseOffset )
                    );
                if (ai != null)
                {
                    bool aiWaitButNotForMe = ai.getSM().getCurrentState() == AIWaitState.Instance() && ai.getWaitAI() != this;
  
                    bool fitCommand = ai.getMoveCommand() == MoveCommand.up
                        || ai.getMoveCommand() == MoveCommand.left
                        || ai.getMoveCommand() == MoveCommand.right;

                    //LodeRunnerScreenshot\fixed\互等的情況.jpg
                    //爬梯子往上，碰到上面的AI是stop時，自己才要stop         
                    bool aiStopAndOnLadder = ai.getMoveCommand() == MoveCommand.stop && ai.movable.getSM().getCurrentState() == Movable.OnLabberState.Instance();

                    bool stopMoveSituation = fitCommand || aiWaitButNotForMe || aiStopAndOnLadder;

                    if (Debug_Oncoming)
                    {
                        if (aiStopAndOnLadder)
                            printDebugMsg("[注意]aiStopAndOnLadder");
                        else
                            printDebugMsg("[注意]aiStop but not OnLadder");
                    }

                    if (stopMoveSituation)
                        movable.sendMsgStopMove();
                    else
                        handleTooClose(ai, movable.sendMsgMoveUp);
                }
                else
                    movable.sendMsgMoveUp(this);
            }
            else if (dir.y < 0)
            {
                AIMoveController ai = tooCloseDetect(new Vector2(-Movable.tooCloseHalfLongSide, -Movable.tooCloseOffset - Movable.tooCloseShortSide),
                    new Vector2(Movable.tooCloseHalfLongSide, -Movable.tooCloseOffset - Movable.tooCloseShortSide), 
                    new Vector2(-Movable.tooCloseHalfLongSide, -Movable.tooCloseOffset), 
                    new Vector2(Movable.tooCloseHalfLongSide, -Movable.tooCloseOffset)
                    );
                if (ai != null)
                {
                    bool aiWaitButNotForMe = ai.getSM().getCurrentState() == AIWaitState.Instance() && ai.getWaitAI() != this;
                    bool fitCommand = ai.getMoveCommand() == MoveCommand.down
                        || ai.getMoveCommand() == MoveCommand.left
                        || ai.getMoveCommand() == MoveCommand.right
                        || ai.getMoveCommand() == MoveCommand.stop;
                    bool stopMoveSituation = fitCommand || aiWaitButNotForMe;

                    if (stopMoveSituation)
                        movable.sendMsgStopMove();   
                    else
                        handleTooClose(ai, movable.sendMsgMoveDown);
                }
                else 
                    movable.sendMsgMoveDown();
            }  
        }
    }

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
        sendMove(diff);

    }

    bool isFinshMoveByPath()
    {
        if (nowPathIndex == pathList.Count)
            return true;
        else return false;
    }

    GraphNode nowTarget = null;
    GraphNode getNowTarget() { return nowTarget; }
    void moveByPath()//return isFinish
    {
        if (nowPathIndex == pathList.Count)//到了
            return;

        nowTarget =pathList[nowPathIndex];
        
        Vector2 diff = nowTarget.getPosition() - transform.position;
        //落到地面時可能不成立，因為GraphNode和角色的y有落差(落地觸發reFindPath)
        if (diff.sqrMagnitude < Movable.MoveEpsilon)//非常接近了，瞄準下個節點
        {
            //Debug.Log("到達:"+nowTarget.nodeKey);

            movable.sendMsgStopMove();
            transform.position = nowTarget.getPosition();

            nowPathIndex += 1;
            return;
        }
        else
        {        
            Debug.DrawLine(nowTarget.getPosition(), transform.position, Color.red);
        }

        sendMove(diff);
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
    void FixedUpdate () {

        sm.execute();
        nowState = sm.getCurrentState().ToString();

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
                obj.getSM().handleMessage(new StateMsg((int)AIMsg.findPathOk));
        }

        public override void onMessage(AIMoveController obj, StateMsg msg)
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

    class AIWaitState : State<AIMoveController>
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
                obj.getSM().handleMessage(new StateMsg((int)AIMsg.moveTimeIsOver));
                return;
            }
        }

        public override void onMessage(AIMoveController obj, StateMsg msg)
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
            GraphNode node = obj.getNowTarget(); ;
            if(node!=null)
                obj.showNowState("AI Move ["+obj.getNowPathIndex ()+ "]("+ node.nodeKey+")");
            else
                obj.showNowState("AI Move[" + obj.getNowPathIndex() + "]");

            obj.moveByPath();
            if (obj.getSM().getCurrentState() != this)//moveByPath有可能觸發狀態的改變(由Movable通知)
                return;

            if (obj.isFinshMoveByPath())
            {
                obj.getSM().handleMessage(new StateMsg((int)AIMsg.reFindPath));
                return;
            }

            if (obj.MoveTimeIsOver())
            {
                if (obj.getMoveCommand() == MoveCommand.stop)
                {
                    if (AIMoveController.Debug_path_timeOut)
                        obj.printDebugMsg("[注意!]move Time Is Over");
                    obj.getSM().handleMessage(new StateMsg((int)AIMsg.moveTimeIsOver));
                    return;
                }
                else
                {
                    if (AIMoveController.Debug_path_timeOut)
                        obj.printDebugMsg("[注意!]wait to target path node " + obj.getNowTarget().nodeKey);
                }
            }
        }

        public override void onMessage(AIMoveController obj, StateMsg msg)
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

                    AIMoveController ai = (AIMoveController)msg.obj;
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

    class AICatchByTrapState : State<AIMoveController>
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
                obj.getSM().handleMessage(new StateMsg((int)AIMsg.moveFromTrap));
        }

        public override void onMessage(AIMoveController obj, StateMsg msg)
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
            obj.movable.getSM().handleMessage(new StateMsg((int)MovableMsg.toKinematic));
            bool playerOnTop = obj.enterMoveFromTrap();
        }

        public override void execute(AIMoveController obj)
        {
            obj.showNowState("Move From Trap (" + obj.getMoveCommand().ToString() + ")");
            obj.moveFromTrap();
            if (obj.isFinishMoveFromTrap())
                obj.getSM().handleMessage(new StateMsg((int)AIMsg.reFindPath));
        }

        public override void onMessage(AIMoveController obj, StateMsg msg)
        {
            AIMsg type = (AIMsg)msg.type;
            switch (type)
            {
                case AIMsg.reFindPath:
                    obj.movable.getSM().handleMessage(new StateMsg((int)MovableMsg.breakKinematic));
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

