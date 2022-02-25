public class AIMoveFromTrapState : State<AIMoveController>
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
        obj.movable.getSM().handleMessage(new StateMsg<Movable>((int)MovableMsg.toKinematic, null));
        bool playerOnTop = obj.enterMoveFromTrap();
    }

    public override void execute(AIMoveController obj)
    {
        obj.showNowState("Move From Trap (" + obj.getMoveCommand().ToString() + ")");
        obj.moveFromTrap();
        if (obj.isFinishMoveFromTrap())
            obj.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.reFindPath, null));
    }

    public override void onMessage(AIMoveController obj, StateMsg<AIMoveController> msg)
    {
        AIMsg type = (AIMsg)msg.type;
        switch (type)
        {
            case AIMsg.reFindPath:
                obj.movable.getSM().handleMessage(new StateMsg<Movable>((int)MovableMsg.breakKinematic, null));
                obj.getSM().changeState(AIFindingPathState.Instance());
                break;
            case AIMsg.catchPlayer:
                obj.getSM().changeState(AICatchPlayerState.Instance());
                break;
        }
    }
}