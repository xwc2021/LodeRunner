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
                obj.getSM().handleMessage(new StateMsg<Movable>((int)MovableMsg.jumpFromRope, null));
            else
                obj.getSM().handleMessage(new StateMsg<Movable>((int)MovableMsg.fallToAlignRopeFinish, null));
        }

    }

    public override void onMessage(Movable obj, StateMsg<Movable> msg)
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