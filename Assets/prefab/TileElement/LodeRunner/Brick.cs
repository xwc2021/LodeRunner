using UnityEngine;
using System.Collections;

public class Brick : MonoBehaviour
{
    public static float fadeTime = 0.3f;
    public static float trapTime = 5;

    public Trap trap;
    public TileData tileData;
    
    public int x;
    public int y;
    StateMachine<Brick> sm;
    public StateMachine<Brick> getSM() { return sm; }

    float accumulationTime;
    public void restartClock()
    {
        accumulationTime = 0;
    }

    public void processCatch()
    {
        if (trap != null)
            trap.processCatch();
    }

    public void triggerBecomeTrap()
    {
        if (sm == null)
            sm = new StateMachine<Brick>(this, BrickNormalState.Instance());

        sm.handleMessage(new StateMsg((int)BrickMsg.becomeTrap));
    }

    public void setNone()
    {
        tileData.setTileValue(x, y, (int)TileMapValue.None);
    }

    public void setBrick()
    {
        tileData.setTileValue(x, y, (int)TileMapValue.Brick);
    }

    // Update is called once per frame
    void Update()
    {
        if (sm != null)
            sm.execute();
    }

    void increaseClockTime()
    {
        accumulationTime += Time.deltaTime;
    }

    public bool isFadeOutFinish()
    {
        increaseClockTime();
        if (accumulationTime >= fadeTime)
            return true;
        else
            return false;
    }

    public bool isFadeInFinish()
    {
        increaseClockTime();
        if (accumulationTime >= fadeTime)
            return true;
        else
            return false;
    }

    public bool isTrapTimeFinish()
    {
        increaseClockTime();
        if (accumulationTime >= trapTime)
            return true;
        else
            return false;
    }
}

enum BrickMsg {becomeTrap,fadeOutOk,trapIsOver,fadeInOk,someOneInTrap, chancelFadeOut }

public class BrickNormalState : State<Brick>
{
    private BrickNormalState() { }
    static BrickNormalState instance;
    public static BrickNormalState Instance()
    {
        if (instance == null)
            instance = new BrickNormalState();
        return instance;
    }
    public override void enter(Brick obj)
    {
        obj.processCatch();

        Renderer render = obj.GetComponent<Renderer>();
        render.enabled = true;

        BoxCollider2D boxCollider = obj.GetComponent<BoxCollider2D>();
        boxCollider.enabled = true;

        obj.setBrick();
    }
    public override void execute(Brick obj)
    {
       
    }

    public override void onMessage(Brick obj,StateMsg msg)
    {
        BrickMsg type = (BrickMsg)msg.type;
        switch (type)
        {
            case BrickMsg.becomeTrap:
                obj.getSM().changeState(BrickFadeOutState.Instance());
                break;
        }
    }
}

public class BrickFadeOutState : State<Brick>
{
    private BrickFadeOutState() { }
    static BrickFadeOutState instance;
    public static BrickFadeOutState Instance()
    {
        if (instance == null)
            instance = new BrickFadeOutState();
        return instance;
    }
    public override void enter(Brick obj)
    {
        Renderer render = obj.GetComponent<Renderer>();
        render.enabled = false;

        obj.setNone();
        obj.restartClock();
    }
    public override void execute(Brick obj)
    {
        if (obj.isFadeOutFinish())
            obj.getSM().handleMessage(new StateMsg((int)BrickMsg.fadeOutOk));
    }

    public override void onMessage(Brick obj, StateMsg msg)
    {
        BrickMsg type = (BrickMsg)msg.type;
        switch (type)
        {
            case BrickMsg.fadeOutOk:
                obj.getSM().changeState(BrickTrapState.Instance());
                break;
            case BrickMsg.chancelFadeOut:
                obj.getSM().changeState(BrickNormalState.Instance());
                break;
        }
    }
}

public class BrickTrapState : State<Brick>
{
    private BrickTrapState() { }
    static BrickTrapState instance;
    public static BrickTrapState Instance()
    {
        if (instance == null)
            instance = new BrickTrapState();
        return instance;
    }
    public override void enter(Brick obj)
    {
        BoxCollider2D boxCollider = obj.GetComponent<BoxCollider2D>();
        boxCollider.enabled = false;

        obj.restartClock();
    }
    public override void execute(Brick obj)
    {
        if (obj.isTrapTimeFinish())
            obj.getSM().handleMessage(new StateMsg((int)BrickMsg.trapIsOver));
    }

    public override void onMessage(Brick obj, StateMsg msg)
    {
        BrickMsg type = (BrickMsg)msg.type;
        switch (type)
        {
            case BrickMsg.trapIsOver:
                obj.getSM().changeState(BrickFadeInState.Instance());
                break;
        }
    }
}

public class BrickFadeInState : State<Brick>
{
    private BrickFadeInState() { }
    static BrickFadeInState instance;
    public static BrickFadeInState Instance()
    {
        if (instance == null)
            instance = new BrickFadeInState();
        return instance;
    }
    public override void enter(Brick obj)
    {
        obj.restartClock();
    }
    public override void execute(Brick obj)
    {
        if (obj.isFadeInFinish())
            obj.getSM().handleMessage(new StateMsg((int)BrickMsg.fadeInOk));
    }

    public override void onMessage(Brick obj, StateMsg msg)
    {
        BrickMsg type = (BrickMsg)msg.type;
        switch (type)
        {
            case BrickMsg.fadeInOk:
                obj.getSM().changeState(BrickNormalState.Instance());
                break;
        }
    }
}
