
public class AIMoveState : State<AIMoveController>
{
    private AIMoveState() { }
    static AIMoveState instance;
    public static AIMoveState Instance()
    {
        if (instance == null)
            instance = new AIMoveState();
        return instance;
    }

    public override void enter(AIMoveController obj)
    {
        //obj.printDebugMsg("enter AIMoveState");
        obj.restartClock();
    }

    public override void exit(AIMoveController obj)
    {
        //obj.printDebugMsg("exit AIMoveState");
    }

    public override void execute(AIMoveController obj)
    {
        bool toFixPoint = obj.moveByPath();

        if (obj.isFinshMoveByPath())
        {
            obj.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.reFindPath, null));
            return;
        }

        if (obj.MoveTimeIsOver())
        {
            if (!toFixPoint)
            {
                if (AIMoveController.Debug_path_timeOut)
                    obj.printDebugMsg("[注意!]wait to target path node " + obj.getNowTarget().getNodeKey());

                //有可能發生這種永遠到不了fixPoint的清況
                //LodeRunnerScreenshot\fixed\never_to_FixPoint.png
                if (obj.MoveTimeIsOverMaxLimit())
                {
                    if (AIMoveController.Debug_path_timeOut)
                        obj.printDebugMsg("[強制切換]to AIFindingPathState");
                    obj.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.moveTimeIsOver, null));
                }

            }
            else
            {
                if (AIMoveController.Debug_path_timeOut)
                    obj.printDebugMsg("[即將切換]to AIFindingPathState");

                obj.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.moveTimeIsOver, null));
            }
        }
    }

    public override void onMessage(AIMoveController obj, StateMsg<AIMoveController> msg)
    {
        AIMsg type = (AIMsg)msg.type;
        switch (type)
        {
            case AIMsg.reFindPath:
            case AIMsg.moveTimeIsOver:
                obj.getSM().changeState(AIFindingPathState.Instance());
                break;
            case AIMsg.catchPlayer:
                obj.getSM().changeState(AICatchPlayerState.Instance());
                break;
            case AIMsg.catchByTrap:
                obj.getSM().changeState(AICatchByTrapState.Instance());
                break;
            case AIMsg.waitForSomebody:
                AIMoveController ai = (AIMoveController)msg.sender;
                obj.setWaitThisAI(ai);
                obj.getSM().changeState(AIWaitState.Instance());
                break;
        }
    }
}
