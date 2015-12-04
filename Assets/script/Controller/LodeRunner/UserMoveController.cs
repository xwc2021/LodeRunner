using UnityEngine;
using System.Collections;
using System;

public class UserMoveController : MonoBehaviour
{
    public Movable movable;
    public LodeRunnerGraphBuilder graphBuilder;
    public TileCreator tileCreator;

    public static int footMask = LayerMask.GetMask("FootCanTouch", "AI");

    void Awake()
    {
        movable.setAdjustXWhenOnAir(false);
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.A))
            movable.sendMsgMoveLeft();
        else if (Input.GetKeyUp(KeyCode.A))
            movable.sendMsgStopMove();

        if (Input.GetKey(KeyCode.D))
            movable.sendMsgMoveRight();
        else if (Input.GetKeyUp(KeyCode.D))
            movable.sendMsgStopMove();

        if (Input.GetKey(KeyCode.W))
            movable.sendMsgMoveUp();
        else if (Input.GetKeyUp(KeyCode.W))
            movable.sendMsgStopMove();

        if (Input.GetKey(KeyCode.S))
            movable.sendMsgMoveDown();
        else if (Input.GetKeyUp(KeyCode.S))
            movable.sendMsgStopMove();

        movable.UpdateMove(footMask);

        //挖洞
        if (Input.GetKeyDown(KeyCode.J))
            digHole(-1,-1);

        if (Input.GetKeyDown(KeyCode.K))
            digHole(1,-1);
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
