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
            obj.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.moveFromTrap, null));
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