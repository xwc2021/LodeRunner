using UnityEngine;
public class DefferedMsg
{
    public MovableMsg type;
    public Vector2 diff;
    public DefferedMsg(MovableMsg pType, Vector2 pDiff)
    {
        type = pType;
        diff = pDiff;
    }
}