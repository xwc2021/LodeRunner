using UnityEngine;
using System.Collections;
using UnityEditor;

public class LodeRunnerGraphBuilderGizmoDrawer
{
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmoFor(LodeRunnerGraphBuilder target, GizmoType gizmoType)
    {
        //draw arrow map
        if (target.showArrowMap)
        {
            for (int x = 0; x < target.getWidth(); x++)
            {
                for (int y = target.getHeight() - 1; y >= 0; y--)
                {
                    byte arrow = target.getArrowMapValue(x, y);
                    if (arrow == (byte)Arrow.Dot)
                    {
                        Gizmos.DrawIcon(target.getTileCenterPositionInWorld(x, y), "arrow_dot.png", true);
                        continue;
                    }
                    if (arrow == (byte)Arrow.JumpPoint)
                    {
                        Gizmos.DrawIcon(target.getTileCenterPositionInWorld(x, y), "arrow_JP.png", true);
                        continue;
                    }
                    if (target.hasArrow(arrow, Arrow.Up))
                        Gizmos.DrawIcon(target.getTileCenterPositionInWorld(x, y), "arrow_up.png", true);
                    if (target.hasArrow(arrow, Arrow.Down))
                        Gizmos.DrawIcon(target.getTileCenterPositionInWorld(x, y), "arrow_down.png", true);
                    if (target.hasArrow(arrow, Arrow.Left))
                        Gizmos.DrawIcon(target.getTileCenterPositionInWorld(x, y), "arrow_left.png", true);
                    if (target.hasArrow(arrow, Arrow.Right))
                        Gizmos.DrawIcon(target.getTileCenterPositionInWorld(x, y), "arrow_right.png", true);
                }
            }
        }
    }
}
