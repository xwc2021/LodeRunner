using UnityEngine;
using System.Collections;
using System;

public class UserMoveController : MonoBehaviour
{
    public Movable movable;
    public LodeRunnerGraphBuilder graphBuilder;
    public TileCreator tileCreator;

    public static int footMask;

    void Awake()
    {
        movable.setAdjustXWhenOnAir(false);
        footMask = LayerMask.GetMask("FootCanTouch", "AI");
    }

    bool doMoving=false;
    void Update()
    {
        bool needPushCommand = doMoving
            && movable.getMoveCommand() == MoveCommand.stop;
        
        if (!doMoving || needPushCommand)
        {
            if (Input.GetKey(KeyCode.A))
            {
                movable.DefferedMoveLeft();
                doMoving = true;
            }

            if (Input.GetKey(KeyCode.D))
            {
                movable.DefferedMoveRight();
                doMoving = true;
            }

            if (Input.GetKey(KeyCode.W))
            { 
                movable.DefferedMoveUp();
                doMoving = true;
            }

            if (Input.GetKey(KeyCode.S))
            { 
                movable.DefferedMoveDown();
                doMoving = true;
            }
        }

        bool keyUp = Input.GetKeyUp(KeyCode.A)
            || Input.GetKeyUp(KeyCode.D)
            || Input.GetKeyUp(KeyCode.W)
            || Input.GetKeyUp(KeyCode.S);

        if (keyUp)
            movable.DefferedStop();
       
       
        //挖洞
        if (Input.GetKeyDown(KeyCode.J))
            digHole(-1,-1);

        if (Input.GetKeyDown(KeyCode.K))
            digHole(1,-1);
    }

    private void FixedUpdate()
    {
        movable.UpdateMove(footMask);
    }

    void digHole(int offsetX,int offsetY)
    {
        Vector2 tileIndex =graphBuilder.getTileIndex(transform.position);

        int targetX = (int)tileIndex.x + offsetX;
        int targetY = (int)tileIndex.y + offsetY;
        if (graphBuilder.canDig(targetX, targetY))
        {
            Brick brick = tileCreator.getObj(targetX, targetY).GetComponentInChildren<Brick>();
            if (brick != null)
                brick.triggerBecomeTrap();
        }
    }

    public void catchByTrap()
    {
        Debug.Log("player inTrap");
        DestroyImmediate(this.gameObject);
    }
}
