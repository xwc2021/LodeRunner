using UnityEngine;

public class StateMachine<T>
{
    T obj;
    State<T> current;
    State<T> previous;

    public StateMachine(T pObj, State<T> pCurrent)
    {
        obj = pObj;
        changeState(pCurrent);
    }

    public void handleMessage(StateMsg msg)
    {
        current.onMessage(obj,msg);
    }

    public State<T> getCurrentState()
    {
        return current;
    }

    public State<T> getPreviousState()
    {
        return previous;
    }

    public void changeState(State<T> newState)
    {
        if(current!=null)
            current.exit(obj);

        previous = current;
        current = newState;
        current.enter(obj);
    }

    public void execute()
    {
        current.execute(obj);
    }
}

public class StateMsg
{
    public int type;
    public MonoBehaviour sender;
    public object obj;
    public StateMsg(int pType, MonoBehaviour pSender=null,object pObj = null)
    {
        type = pType;
        obj = pObj;
        sender = pSender;
    }
}

public class State<T>
{
    public virtual void onMessage(T obj, StateMsg msg) { }
    public virtual void enter(T obj) { }
    public virtual void exit(T obj) { }
    public virtual void execute(T obj) { }
}
