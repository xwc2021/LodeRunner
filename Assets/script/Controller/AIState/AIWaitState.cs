public class AIWaitState : State<AIMoveController>
{
    private AIWaitState() { }
    static AIWaitState instance;
    public static AIWaitState Instance()
    {
        if (instance == null)
            instance = new AIWaitState();
        return instance;
    }

    public override void enter(AIMoveController obj)
    {
        obj.movable.sendMsgWait();

        if (AIMoveController.Debug_AI_wait)
            obj.printDebugMsg("進入 AIWait State");

        obj.restartClock();
    }

    public override void exit(AIMoveController obj)
    {
        if (AIMoveController.Debug_AI_wait)
            obj.printDebugMsg("exit AIWait State");

        obj.clearWaitAI();
        obj.movable.sendMsgStopMove();//要記得切換到stop狀態
    }

    public override void execute(AIMoveController obj)
    {
        obj.showNowState("AIWait State(" + obj.getWaitAI().name + ")");
        if (obj.WaitTimeIsOver())
        {
            obj.getSM().handleMessage(new StateMsg<AIMoveController>((int)AIMsg.moveTimeIsOver, null));
            return;
        }
    }

    public override void onMessage(AIMoveController obj, StateMsg<AIMoveController> msg)
    {
        AIMsg type = (AIMsg)msg.type;
        switch (type)
        {
            case AIMsg.moveTimeIsOver:
                obj.getSM().changeState(AIFindingPathState.Instance());
                break;
        }
    }
}