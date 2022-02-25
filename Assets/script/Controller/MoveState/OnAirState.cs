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
        obj.showNowState("OnAir State (" + obj.getMoveCommand().ToString() + ")");
        obj.doOnAirMove();
    }

    public override void onMessage(Movable obj, StateMsg<Movable> msg)
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