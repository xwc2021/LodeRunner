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

        //failToTarget才可以停住
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

    public override void onMessage(Movable obj, StateMsg<Movable> msg)
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
                    AIMoveController ai = msg.sender.myAI;
                    ai.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.reFindPath, null));
                }
                break;
        }
    }
}