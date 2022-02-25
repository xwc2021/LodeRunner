public class AIFindingPathState : State<AIMoveController>
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
            obj.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.findPathOk, null));
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
