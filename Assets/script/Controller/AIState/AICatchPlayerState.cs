public class AICatchPlayerState : State<AIMoveController>
{
    private AICatchPlayerState() { }
    static AICatchPlayerState instance;
    public static AICatchPlayerState Instance()
    {
        if (instance == null)
            instance = new AICatchPlayerState();
        return instance;
    }

    public override void enter(AIMoveController obj)
    {
        obj.catchPlayer();
        obj.movable.sendMsgStopMove();
    }

    public override void execute(AIMoveController obj)
    {
        obj.showNowState("Catch Player");
    }
}