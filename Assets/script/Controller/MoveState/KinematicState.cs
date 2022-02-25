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

    public override void onMessage(Movable obj, StateMsg<Movable> msg)
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