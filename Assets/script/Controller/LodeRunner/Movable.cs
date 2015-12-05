using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public enum MoveCommand { stop, left, right, up, down,wait }
public class Movable : MonoBehaviour {

    //參數
    //public static float MoveEpsilon = 0.001f;
    public static float MoveEpsilon = 0.01f;
    static bool Debug_enter_state = false;
    static bool Debug_do_moveUp = false;
    static bool Debug_adjustY = false;
    static bool Debug_fall_to_align_rope = false;

    //               --
    //   offset     |  |    <--
    // -------------|  |       |
    //              |  |  LongSide
    //               --
    //            ShortSide 
    public static float tooCloseShortSide = 0.1f;
    public static float tooCloseHalfLongSide = 0.41f;
    public static float tooCloseOffset = 0.5f;

    public float onAirVelocity = 1;
    public float velocity = 1;
    public Rigidbody2D rigid;
    public LodeRunnerGraphBuilder graphBuilder;
    static int AIMask = LayerMask.GetMask("AI");

    StateMachine<Movable> sm;
    public StateMachine<Movable> getSM(){ return sm; }

    bool asjustXWhenOnAir =true;
    public void setAdjustXWhenOnAir(bool b) { asjustXWhenOnAir = b; }
    bool getAdjustXWhenOnAir() { return asjustXWhenOnAir; }
    void Awake()
    {
        sm = new StateMachine<Movable>(this, OnAirState.Instance());
    }

    static float rigidHalfWidth=0.4f;//設為0.5的話，會找到上面的tile
    static float rigidHalfHeight = 0.4f;

    bool checkIsOnLadderOrRopeTile()
    {
        Vector2 pos = transform.position;
        Vector2 leftUp = pos + new Vector2(-rigidHalfWidth, rigidHalfHeight);
        Vector2 rightUp = pos + new Vector2(rigidHalfWidth, rigidHalfHeight);
        Vector2 leftDown = pos + new Vector2(-rigidHalfWidth, -rigidHalfHeight);
        Vector2 rightDown = pos + new Vector2(rigidHalfWidth, -rigidHalfHeight);
        Vector2 center = graphBuilder.getTileIndex(pos);

        Debug.DrawLine(leftUp, rightUp, Color.red);
        Debug.DrawLine(leftDown, rightDown, Color.red);
        Debug.DrawLine(leftUp, leftDown, Color.red);
        Debug.DrawLine(rightUp, rightDown, Color.red);

        List<Vector2> list =new List<Vector2>();
        list.Add(graphBuilder.getTileIndex(leftUp));
        list.Add(graphBuilder.getTileIndex(rightUp));
        list.Add(graphBuilder.getTileIndex(leftDown));
        list.Add(graphBuilder.getTileIndex(rightDown));

        bool onRope = false;
        foreach (Vector2 index in list)
        {
            onRope =graphBuilder.isRope((int)index.x, (int)index.y);
            if (onRope)
                break;
        }

        bool onLadder = false;
        foreach (Vector2 index in list)
        {
            onLadder = graphBuilder.isLadder((int)index.x, (int)index.y);
            if (onLadder)
                break;
        }

        if (onRope && onLadder)//跨區塊
        {
            bool centerOnRope = graphBuilder.isRope((int)center.x, (int)center.y);
            //bool centerOnLadder = graphBuilder.isLadder((int)center.x, (int)center.y);
            if (centerOnRope)
            {
                sendMsgOnRope();
                return true;
            }
            else
            {
                sendMsgOnLadder();
                return true;
            }
        }
        else if (onRope)
        {
            sendMsgOnRope();
            return true;
        }
        else if (onLadder)
        {
            sendMsgOnLadder();
            return true;
        }
        else
        {
            sendMsgToNormal();
            return false;
        }
            
    }

    //有了這個機制，可以避免
    //(1)在Brick邊緣就因為重力而往下掉(footArea的width>角色的)
    //        .
    //  口口口
    //
    //(2)在樓梯上不會彈跳
    //    .
    //口口H口口
    //    H
    //但也因此要自行處理
    //(1)玩家降落到rope時，要再發送一次MoveDown(因為Normal狀態重力會關掉)
    // .
    //-----
    void checkFootArea(int mask)
    {
        float hwidth = 0.41f;//比collider box稍寬
        float top = -0.5f;
        float down = -0.65f;
        Vector2 pos = transform.position;
        Vector2 leftUp = pos + new Vector2(-hwidth, top);
        Vector2 rightUp = pos + new Vector2(hwidth, top);
        Vector2 leftDown = pos + new Vector2(-hwidth, down);
        Vector2 rightDown = pos + new Vector2(hwidth, down);

        Debug.DrawLine(leftUp, rightUp,Color.blue);
        Debug.DrawLine(leftDown, rightDown, Color.blue);
        Debug.DrawLine(leftUp, leftDown, Color.blue);
        Debug.DrawLine(rightUp, rightDown, Color.blue);

        Collider2D touchZone = Physics2D.OverlapArea(leftUp, rightDown, mask);//這連trigger都會判定
        if (touchZone != null)
        {
            sendMsgLanding();

            //如果從上面落下碰到rope，要主動往rope移動
            if (touchZone.tag == "Rope")
            {
                bool downIsRope = downTileIsRope();
                bool downIsBlock = downTileIsBlock();

                //掉到繩子上要自動觸發fall to Align Rope
                if (downIsRope && !downIsBlock)
                {
                    //case 1:airToNormal
                    //
                    //          .   
                    //        _____

                    //case 2:ropeToNormal
                    //
                    //     ___.=>
                    //        _____

                    //case 3:LadderToNormal 
                    // 
                    //        =>
                    //      H_____     

                    //case 4:FromBlock
                    // 
                    //        =>
                    //   _  _ ____  
                    //  |_||_| 

                    if(Movable.Debug_fall_to_align_rope)
                        Debug.Log(name + ":fall To Align Rope Case [注意!] ");
                    getSM().handleMessage(new StateMsg((int)MovableMsg.fallToAlignRope));
                }    
            }
            //取消Brick的FadeOut
            else if (touchZone.tag == "Brick")
            {
                if (tag == "Monster")
                {
                    Brick brick = touchZone.GetComponent<Brick>();
                    StateMachine<Brick> sm = brick.getSM();
                    if (sm != null)
                    {
                        Debug.Log("cancel FadeOut");
                        sm.handleMessage(new StateMsg((int)BrickMsg.chancelFadeOut));
                    }
                }
            }
        }
        else
        {
            sendMsgOnAir();//腳沒碰到東西
        }
    }

    public void UpdateMove(int mask)
    {
        if (!checkIsOnLadderOrRopeTile())
        {
            checkFootArea(mask);
        }
        
        sm.execute();
    }

    bool nowTileIsBrick()//因為可能被玩家消去，所以不能只判斷tileValue
    {
        //對齊高度
        Vector2 nowIndex = graphBuilder.getTileIndex(transform.position);
        GameObject obj = graphBuilder.tileCreator.getObj((int)nowIndex.x, (int)nowIndex.y);

        if (obj != null)
        {
            if (obj.tag == "Brick")
                return true;
            else
                return false;
        }
        else return false;  
    }

    bool downTileIsBlock()
    {
        Vector2 left = graphBuilder.getTileIndex(transform.position+new Vector3(-Movable.rigidHalfWidth,0,0));
        bool leftIsBlock = graphBuilder.isBlock((int)left.x, (int)left.y - 1);

        Vector2 right = graphBuilder.getTileIndex(transform.position + new Vector3(Movable.rigidHalfWidth, 0, 0));
        bool rightIsBlock = graphBuilder.isBlock((int)right.x, (int)right.y - 1);
        return leftIsBlock || rightIsBlock;
    }

    bool downTileIsLadder()
    {
        Vector2 left = graphBuilder.getTileIndex(transform.position + new Vector3(-Movable.rigidHalfWidth, 0, 0));
        bool leftIsLadder = graphBuilder.isLadder((int)left.x, (int)left.y - 1);

        Vector2 right = graphBuilder.getTileIndex(transform.position + new Vector3(Movable.rigidHalfWidth, 0, 0));
        bool rightIsLadder = graphBuilder.isLadder((int)right.x, (int)right.y - 1);
        return leftIsLadder || rightIsLadder;
    }

    bool downTileIsRope()
    {
        Vector2 left = graphBuilder.getTileIndex(transform.position + new Vector3(-Movable.rigidHalfWidth, 0, 0));
        bool leftIsRope = graphBuilder.isRope((int)left.x, (int)left.y - 1);

        Vector2 right = graphBuilder.getTileIndex(transform.position + new Vector3(Movable.rigidHalfWidth, 0, 0));
        bool rightIsRope = graphBuilder.isRope((int)right.x, (int)right.y - 1);
        return leftIsRope || rightIsRope;
    }

    void adjustX()
    {
        Vector3 old = transform.position;

        //對齊高度
        float newX = graphBuilder.getTileCenterPositionInWorld(old).x;
        transform.position = new Vector3(newX, old.y, 0);

        //Debug.Log(name+":對齊高度 "+newY);
    }

    void adjustY()
    {
        Vector3 old = transform.position;
       
        //對齊高度
        float newY = graphBuilder.getTileCenterPositionInWorld(old).y ;
        transform.position = new Vector3(old.x, newY, 0);
        
        if(Debug_adjustY)
            Debug.Log(name+":對齊高度 "+newY);
    }

    public void sendMsgOnLadder(){sm.handleMessage(new StateMsg((int)MovableMsg.onLabber));}
    public void sendMsgOnRope(){sm.handleMessage(new StateMsg((int)MovableMsg.onRope));}
    public void sendMsgToNormal(){sm.handleMessage(new StateMsg((int)MovableMsg.toNormal));}
    public void sendMsgLanding(){sm.handleMessage(new StateMsg((int)MovableMsg.landing));}
    public void sendMsgOnAir() { sm.handleMessage(new StateMsg((int)MovableMsg.onAir)); }

    public void sendMsgMoveLeft() { sm.handleMessage(new StateMsg((int)MovableMsg.moveLeft)); }
    public void sendMsgMoveLeft(MonoBehaviour sender) { sm.handleMessage(new StateMsg((int)MovableMsg.moveLeft, sender)); }

    public void sendMsgMoveRight() { sm.handleMessage(new StateMsg((int)MovableMsg.moveRight)); }
    public void sendMsgMoveRight(MonoBehaviour sender) { sm.handleMessage(new StateMsg((int)MovableMsg.moveRight, sender)); }

    public void sendMsgMoveUp() { sm.handleMessage(new StateMsg((int)MovableMsg.moveUp)); }
    public void sendMsgMoveUp(MonoBehaviour sender) { sm.handleMessage(new StateMsg((int)MovableMsg.moveUp, sender)); }

    public void sendMsgMoveDown() { sm.handleMessage(new StateMsg((int)MovableMsg.moveDown)); }
    public void sendMsgMoveDown(MonoBehaviour sender) { sm.handleMessage(new StateMsg((int)MovableMsg.moveDown, sender)); }

    public void sendMsgStopMove() { sm.handleMessage(new StateMsg((int)MovableMsg.stopMove)); }
    public void sendMsgWait() { sm.handleMessage(new StateMsg((int)MovableMsg.wait)); }

    MoveCommand moveCommand = MoveCommand.stop;
    public MoveCommand getMoveCommand() { return moveCommand; }

    void doMoveRight()
    {
        moveCommand = MoveCommand.right;
        float y = (rigid.velocity.y > 0) ? 0 : rigid.velocity.y;
        rigid.velocity = new Vector2(velocity, y);
    }

    void doMoveLeft()
    {
        moveCommand = MoveCommand.left;
        float y = (rigid.velocity.y > 0) ? 0 : rigid.velocity.y;
        rigid.velocity = new Vector2(-velocity, y);
    }

    void doMoveUp()
    {
        moveCommand = MoveCommand.up;
        rigid.velocity = new Vector2(0, velocity);
    }

    void doMoveDown()
    {
        moveCommand = MoveCommand.down;
        rigid.velocity = new Vector2(0, -velocity);
    }

    void doStopMove()
    {
        moveCommand = MoveCommand.stop;
        rigid.velocity = new Vector2(0, 0);
    }

    void doWait()
    {
        moveCommand = MoveCommand.wait;
        rigid.velocity = new Vector2(0, 0);
    }

    void doOnAirMove()
    {
        rigid.velocity = new Vector2(0, -onAirVelocity);
    }

    void adjustWhenMoveOnRope()
    {
        Vector3 nowPosition = transform.position;
        Vector2 nowIndex = graphBuilder.getTileIndex(nowPosition);
        Vector3 nowTileCenter = graphBuilder.getTileCenterPositionInWorld(transform.position);
        if (graphBuilder.isRope((int)nowIndex.x, (int)nowIndex.y))
        {
            float oldX = transform.position.x;
            transform.position = new Vector3(oldX, nowTileCenter.y, 0);
        }
    }

    void adjustWhenFromLadderToRope()
    {
        Vector3 nowPosition = transform.position;
        Vector2 nowIndex = graphBuilder.getTileIndex(nowPosition);
        Vector3 nowTileCenter = graphBuilder.getTileCenterPositionInWorld(transform.position);
        if (graphBuilder.isRope((int)nowIndex.x, (int)nowIndex.y))
        {
            Debug.Log(name+":adjust When From Ladder To Rope");
            float oldX = transform.position.x;
            transform.position = new Vector3(oldX, nowTileCenter.y, 0);
        }
        else
        {
            float currentY = nowPosition.y;
            if (currentY < nowTileCenter.y)//檢查下面一格
            {
                if (graphBuilder.isRope((int)nowIndex.x, (int)nowIndex.y - 1))
                {
                    Debug.Log("adjustWhenFromLadderToRope 下一格");
                    Vector3 downPos = transform.position + new Vector3(0, -1, 0);
                    Vector3 downTileCenter = graphBuilder.getTileCenterPositionInWorld(downPos);
                    float oldX = transform.position.x;
                    transform.position = new Vector3(oldX, downTileCenter.y, 0);
                }
            }
            else if (currentY > nowTileCenter.y)//檢查上面一格
            {
                if (graphBuilder.isRope((int)nowIndex.x, (int)nowIndex.y + 1))
                {
                    Debug.Log("adjustWhenFromLadderToRope 上一格");
                    Vector3 nowPos = transform.position + new Vector3(0, 1, 0);
                    Vector3 upTileCenter = graphBuilder.getTileCenterPositionInWorld(nowPos);
                    float oldX = transform.position.x;
                    transform.position = new Vector3(oldX, upTileCenter.y, 0);
                }
            }
        }  
    }

    bool isJumpFromRope=false;
    bool getIsJumpFromRope() { return isJumpFromRope; }
    float fallTargetY;
    void setFallTargetY(bool bIsJumpFromRope)
    {
        isJumpFromRope = bIsJumpFromRope;
        float oldY = transform.position.y;
        fallTargetY = oldY - 1;//oldY如果有對齊，fallTargetY就會對齊
    }

    bool isFallToTaretReady()
    {
        //加上排隊機制
        Collider2D collider = tooCloseDetect(new Vector2(-Movable.tooCloseHalfLongSide, -Movable.tooCloseOffset - Movable.tooCloseShortSide),
                    new Vector2(Movable.tooCloseHalfLongSide, -Movable.tooCloseOffset - Movable.tooCloseShortSide),
                    new Vector2(-Movable.tooCloseHalfLongSide, -Movable.tooCloseOffset),
                    new Vector2(Movable.tooCloseHalfLongSide, -Movable.tooCloseOffset),
                    AIMask
                    );
        if (collider != null)
        {
            Movable other= collider.GetComponent<Movable>();

            //因為存在這種case
            //LodeRunnerScreenshot\fixed\梯子和繩子相碰case.jpg
            bool fitCommand = other.getMoveCommand() == MoveCommand.up
                || other.getMoveCommand() == MoveCommand.down
                || other.getMoveCommand() == MoveCommand.left
                || other.getMoveCommand() == MoveCommand.right
                || other.getMoveCommand() == MoveCommand.stop;
            if (fitCommand)
                doStopMove();
            else
                doMoveDown();
        }
        else
            doMoveDown();

        if (rigid.position.y <= fallTargetY)
        {
            return true;
        }
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
        Debug.Log(name+":"+msg);
    }

    public Collider2D tooCloseDetect(Vector2 leftUp, Vector2 rightUp, Vector2 leftDown, Vector2 rightDown,int mask)
    {
        Vector2 pos = transform.position;

        Collider2D touchZone = Physics2D.OverlapArea(pos + leftUp, pos + rightDown, mask);
        bool tooClose = touchZone != null;
        if (tooClose)
        {
            Debug.DrawLine(pos + leftUp, pos + rightUp, Color.yellow);
            Debug.DrawLine(pos + leftDown, pos + rightDown, Color.yellow);
            Debug.DrawLine(pos + leftUp, pos + leftDown, Color.yellow);
            Debug.DrawLine(pos + rightUp, pos + rightDown, Color.yellow);
        }

        return touchZone;
    }

    public class OnLabberState : State<Movable>
    {
        private OnLabberState() { }
        static OnLabberState instance;
        public static OnLabberState Instance()
        {
            if (instance == null)
                instance = new OnLabberState();
            return instance;
        }
        public override void enter(Movable obj)
        {
            if (Movable.Debug_enter_state)
                obj.printDebugMsg("Labber");

            obj.doStopMove();
        }
        public override void exit(Movable obj) { }
        public override void execute(Movable obj)
        {
            obj.showNowState("OnLabber ("+obj.getMoveCommand().ToString()+")");
        }

        public override void onMessage(Movable obj, StateMsg msg)
        {
            MovableMsg type = (MovableMsg)msg.type;
            switch (type)
            {
                case MovableMsg.onRope:
                    obj.getSM().changeState(OnRopeState.Instance());
                    break;
                case MovableMsg.toNormal:
                    obj.getSM().changeState(NormalState.Instance());
                    break;
                case MovableMsg.moveLeft:
                    obj.doMoveLeft();
                    break;
                case MovableMsg.moveRight:
                    obj.doMoveRight();
                    break;
                case MovableMsg.moveUp:
                    obj.doMoveUp();
                    break;
                case MovableMsg.moveDown:
                    obj.doMoveDown();
                    break;
                case MovableMsg.stopMove:
                    obj.doStopMove();
                    break;
                case MovableMsg.wait:
                    obj.doWait();
                    break;
            }
        }
    }

    public class FallToTargetState : State<Movable>
    {
        private FallToTargetState() { }
        static FallToTargetState instance;
        public static FallToTargetState Instance()
        {
            if (instance == null)
                instance = new FallToTargetState();
            return instance;
        }

        public override void enter(Movable obj)
        {
            if (Movable.Debug_enter_state)
                obj.printDebugMsg("Fall To Target");
        }

        public override void execute(Movable obj)
        {
            obj.showNowState("Fall To Target (" + obj.getMoveCommand().ToString() + ")");
            if (obj.isFallToTaretReady())
            {
                obj.adjustY();
                if (obj.getIsJumpFromRope())
                    obj.getSM().handleMessage(new StateMsg((int)MovableMsg.jumpFromRope));
                else
                    obj.getSM().handleMessage(new StateMsg((int)MovableMsg.fallToAlignRopeFinish));
            }

        }

        public override void onMessage(Movable obj, StateMsg msg)
        {
            MovableMsg type = (MovableMsg)msg.type;
            switch (type)
            {
                case MovableMsg.jumpFromRope:
                    obj.getSM().changeState(OnAirState.Instance());
                    break;
                case MovableMsg.fallToAlignRopeFinish:
                    obj.getSM().changeState(OnRopeState.Instance());
                    break;
            }
        }
    }

    public class OnRopeState : State<Movable>
    {
        private OnRopeState() { }
        static OnRopeState instance;
        public static OnRopeState Instance()
        {
            if (instance == null)
                instance = new OnRopeState();
            return instance;
        }
        public override void enter(Movable obj)
        {
            if (Movable.Debug_enter_state)
                obj.printDebugMsg("On Rope");

            obj.doStopMove();

            bool fromLadder = obj.getSM().getPreviousState() == OnLabberState.Instance();
            bool fromAir = obj.getSM().getPreviousState() == OnAirState.Instance();

            if (fromLadder)
                obj.adjustWhenFromLadderToRope();
            else if (fromAir)
                obj.adjustY();
        }
        public override void exit(Movable obj) { }
        public override void execute(Movable obj)
        {
            obj.showNowState("On Rope State (" + obj.getMoveCommand().ToString() + ")");
        }

        public override void onMessage(Movable obj, StateMsg msg)
        {
            MovableMsg type = (MovableMsg)msg.type;
            switch (type)
            {
                case MovableMsg.onLabber:
                    obj.getSM().changeState(OnLabberState.Instance());
                    break;
                case MovableMsg.toNormal:
                    obj.getSM().changeState(NormalState.Instance());
                    break;
                case MovableMsg.moveDown://降落

                    if (!obj.downTileIsBlock())
                    {
                        obj.setFallTargetY(true);
                        obj.getSM().changeState(FallToTargetState.Instance());
                    }

                    break;
                case MovableMsg.moveLeft:
                    obj.doMoveLeft();
                    obj.adjustWhenMoveOnRope();
                    break;
                case MovableMsg.moveRight:
                    obj.doMoveRight();
                    obj.adjustWhenMoveOnRope();
                    break;
                case MovableMsg.stopMove:
                    obj.doStopMove();
                    break;
                case MovableMsg.wait:
                    obj.doWait();
                    break;
                case MovableMsg.moveUp:
                    //通知發訊者此動作無效
                    if (msg.sender != null)
                    {
                        //從繩子移到JumpPoint會發動fall to rope，這時目標就停留在JumpPoint
                        //  
                        //--J  
                        //  --
                        if (Movable.Debug_do_moveUp)
                            obj.printDebugMsg("[注意!]do MoveUp on rope");
                        AIMoveController ai = (AIMoveController)msg.sender;
                        ai.getSM().handleMessage(new StateMsg((int)AIMsg.reFindPath));
                    }
                    break;
            }
        }
    }

    public class OnAirState : State<Movable>
    {
        private OnAirState() { }
        static OnAirState instance;
        public static OnAirState Instance()
        {
            if (instance == null)
                instance = new OnAirState();
            return instance;
        }
        public override void enter(Movable obj)
        {
            //   .
            //口口H口口
            //AI在推擠的過程可能導致在空中時沒有對齊，然後在下面有入口時卡住
            if (obj.getAdjustXWhenOnAir())
                obj.adjustX();

            if (Movable.Debug_enter_state)
                obj.printDebugMsg("OnAir");
        }
        public override void exit(Movable obj)
        {
        }
        public override void execute(Movable obj)
        {
            obj.showNowState("OnAir State ("+ obj.getMoveCommand().ToString()+")");
            obj.doOnAirMove();
        }

        public override void onMessage(Movable obj, StateMsg msg)
        {
            MovableMsg type = (MovableMsg)msg.type;
            switch (type)
            {
                case MovableMsg.onRope:
                    obj.getSM().changeState(OnRopeState.Instance());
                    break;

                case MovableMsg.onLabber:
                    obj.getSM().changeState(OnLabberState.Instance());
                    break;

                case MovableMsg.landing:
                    obj.getSM().changeState(NormalState.Instance());
                    break;

                case MovableMsg.toKinematic:
                    obj.getSM().changeState(KinematicState.Instance());
                    break;

            }
        }
    }

    public class NormalState : State<Movable>
    {
        private NormalState() { }
        static NormalState instance;
        public static NormalState Instance()
        {
            if (instance == null)
                instance = new NormalState();
            return instance;
        }
        public override void enter(Movable obj)
        {
            if (Movable.Debug_enter_state)
                obj.printDebugMsg("Normal");

            obj.doStopMove();

            //並且下面的tile是ladder、Rope、Brick、Stone、trap要調整高度
            if (obj.downTileIsLadder() || obj.downTileIsBlock() || obj.downTileIsRope() || obj.nowTileIsBrick())
                obj.adjustY();
        }
        public override void exit(Movable obj) { }
        public override void execute(Movable obj)
        {
            obj.showNowState("Normal Stater(" + obj.getMoveCommand().ToString() + ")");
        }

        public override void onMessage(Movable obj, StateMsg msg)
        {
            MovableMsg type = (MovableMsg)msg.type;
            switch (type)
            {
                case MovableMsg.onLabber:
                    obj.getSM().changeState(OnLabberState.Instance());
                    break;
                case MovableMsg.onRope:
                    obj.getSM().changeState(OnRopeState.Instance());
                    break;
                case MovableMsg.onAir:
                    obj.getSM().changeState(OnAirState.Instance());
                    break;
                case MovableMsg.fallToAlignRope://站在rope上時
                    obj.setFallTargetY(false);
                    obj.getSM().changeState(FallToTargetState.Instance());
                    break;
                case MovableMsg.moveLeft:
                    obj.doMoveLeft();
                    break;
                case MovableMsg.moveRight:
                    obj.doMoveRight();
                    break;
                case MovableMsg.stopMove:
                    obj.doStopMove();
                    break;
                case MovableMsg.wait:
                    obj.doWait();
                    break;
                case MovableMsg.moveDown:
                    if (!obj.downTileIsBlock())
                        obj.doMoveDown();
                    break;
                case MovableMsg.toKinematic:
                    obj.getSM().changeState(KinematicState.Instance());
                    break;
                case MovableMsg.moveUp:
                    //通知發訊者此動作無效
                    if (msg.sender != null)
                    {
                        //解決[在梯子卡點]的問題
                        //從H往下走，但因為有其他AI檔在下面，所以進入stop；之後reFindPath，y就會低於pathNode
                        //  O
                        //口H口口
                        obj.adjustY();

                        //這是為了解決從高處落下，MoveTargetPoint還在上面的問題(在normal狀態如果執行moveUp就觸發reFindPath)
                        //   .
                        //    口口
                        //   .
                        //口口口口

                        //降落在Brick、Stone、ladder時觸發
                        if (Movable.Debug_do_moveUp)
                            obj.printDebugMsg("[注意!]do MoveUp on Normal");
                        AIMoveController ai = (AIMoveController)msg.sender;
                        ai.getSM().handleMessage(new StateMsg((int)AIMsg.reFindPath));
                    }
                    break;
            }
        }
    }

    public class KinematicState : State<Movable>
    {
        private KinematicState() { }
        static KinematicState instance;
        public static KinematicState Instance()
        {
            if (instance == null)
                instance = new KinematicState();
            return instance;
        }

        public override void enter(Movable obj)
        {
            if (Movable.Debug_enter_state)
                obj.printDebugMsg("Kinematic");

            obj.rigid.isKinematic = true;
        }

        public override void exit(Movable obj)
        {
            obj.rigid.isKinematic = false;
        }

        public override void execute(Movable obj)
        {
            obj.showNowState("Kinematic State");
        }

        public override void onMessage(Movable obj, StateMsg msg)
        {
            MovableMsg type = (MovableMsg)msg.type;
            switch (type)
            {
                case MovableMsg.breakKinematic:
                    obj.getSM().changeState(NormalState.Instance());
                    break;
                case MovableMsg.moveLeft:
                    obj.doMoveLeft();
                    break;
                case MovableMsg.moveRight:
                    obj.doMoveRight();
                    break;
                case MovableMsg.moveUp:
                    obj.doMoveUp();
                    break;
                case MovableMsg.stopMove:
                    obj.doStopMove();
                    break;
                case MovableMsg.wait:
                    obj.doWait();
                    break;
            }
        }
    }
}

enum MovableMsg {onAir, landing, onLabber,onRope,toNormal,moveLeft,moveRight,moveUp,moveDown,stopMove,jumpFromRope, toKinematic, breakKinematic, fallToAlignRope, fallToAlignRopeFinish,wait }


