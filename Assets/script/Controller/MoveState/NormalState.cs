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

        //可以從onAir進來
        obj.doStopMove();

        //修正AI踩到AI就瞬移的問題
        if (!obj.isFootTouchAI)
        {
            //並且下面的tile是ladder、Rope、Brick、Stone、trap要調整高度
            if (obj.downTileIsLadder() || obj.downTileIsBlock() || obj.downTileIsRope() || obj.nowTileIsBrick())
                obj.adjustY();

        }
    }
    public override void exit(Movable obj) { }
    public override void execute(Movable obj)
    {
        obj.showNowState("Normal Stater(" + obj.getMoveCommand().ToString() + ")");
    }

    public override void onMessage(Movable obj, StateMsg<Movable> msg)
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
                    AIMoveController ai = msg.sender.myAI;
                    ai.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.reFindPath, null));
                }
                break;
        }
    }
}