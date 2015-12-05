using UnityEngine;
using System.Collections;
using UnityEditor;

public class TileCreatorGizmoDrawer
{
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmoFor(TileCreator target, GizmoType gizmoType)
    {
        //沒有設定tileData就不畫
        if (target.tileData == null)
            return;

        float scaleX = target.transform.localScale.x;
        float scaleY = target.transform.localScale.y;

        //draw grid
        Vector3 origin = target.transform.position;
        Vector3 top = origin + scaleY * target.transform.up * target.getHeight();
        Vector3 right = origin + scaleX * target.transform.right * target.getWidth();

        for (int i = 0; i <= target.getWidth(); i++)
        {
            Vector3 offsetFrom = origin + scaleX * target.transform.right * i;
            Vector3 offsetTo = top + scaleX * target.transform.right * i;
            Gizmos.DrawLine(offsetFrom, offsetTo);
        }

        for (int i = 0; i <= target.getHeight(); i++)
        {
            Vector3 offsetFrom = origin + scaleY * target.transform.up * i;
            Vector3 offsetTo = right + scaleY * target.transform.up * i;
            Gizmos.DrawLine(offsetFrom, offsetTo);
        }
    }
}
