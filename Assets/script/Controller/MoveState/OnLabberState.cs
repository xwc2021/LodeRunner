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

        ////failToTarget才可以停住
        obj.doStopMove();
    }
    public override void exit(Movable obj) { }
    public override void execute(Movable obj)
    {
        obj.showNowState("OnLabber (" + obj.getMoveCommand().ToString() + ")");
    }

    public override void onMessage(Movable obj, StateMsg<Movable> msg)
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