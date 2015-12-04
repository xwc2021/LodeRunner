using UnityEngine;
using System.Collections;

public class Trap : MonoBehaviour {

    public Brick brick;
    AIMoveController catchAI = null;
    UserMoveController catchPlayer = null;

    public void processCatch()
    {
        if (catchPlayer != null)
            catchPlayer.catchByTrap();

        if (catchAI != null)
            catchAI.catchByTrap();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("enter trap");
        catchPlayer = other.gameObject.GetComponent<UserMoveController>();
        catchAI = other.gameObject.GetComponent<AIMoveController>();

        if (catchAI != null)
        {
            catchAI.getSM().handleMessage(new StateMsg((int)AIMsg.catchByTrap));
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("exit trap");

        if (catchAI != null)
        {
            catchAI = null;
        }
 
        catchPlayer = null;
    }
}
