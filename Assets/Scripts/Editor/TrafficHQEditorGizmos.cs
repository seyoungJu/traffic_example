using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class TrafficHQEditorGizmos
{
    private static void DrawArrow(Vector3 point, Vector3 forward, float size)
    {
        forward = forward.normalized * size;
        Vector3 left = Quaternion.Euler(0, 45, 0) * forward;
        Vector3 right = Quaternion.Euler(0, -45, 0) * forward;
        
        Gizmos.DrawLine(point, point + left);
        Gizmos.DrawLine(point, point + right);
    }

    private static int GetArrowCount(Vector3 pointA, Vector3 pointB, TrafficHeadQuater headQuater)
    {
        switch (headQuater.arrowDrawType)
        {
            case TrafficHeadQuater.ArrowDraw.FixedCount:
                return headQuater.arrowCount;
            case TrafficHeadQuater.ArrowDraw.ByLength:
                return Mathf.Max(1, (int)(Vector3.Distance(pointA, pointB) / headQuater.arrowDistance));
            case TrafficHeadQuater.ArrowDraw.Off:
                return 0;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    //Custom Gizmo function
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
    private static void DrawGizmo(TrafficHeadQuater headQuater, GizmoType gizmoType)
    {
        //기즈모 안그린다면 그냥 리턴.
        if (headQuater.hideGizmos)
        {
            return;
        }

        foreach (TrafficSegment segment in headQuater.segments)
        {
            //세그먼트 이름 출력.
            GUIStyle style = new GUIStyle {normal = {textColor = new Color(1,0,0)}, fontSize = 15};
            Handles.Label(segment.transform.position, segment.name, style);
            //웨이포인트 그리기.
            for (int j = 0; j < segment.waypoints.Count; j++)
            {
                //현재 웨이포인트 위치 찾아서.
                Vector3 pos = segment.waypoints[j].GetVisualPos();
                //구 그리고, 방향을 표시하려면 색상을 변경.
                Gizmos.color = new Color(0, 0, 0, (j + 1) / (float)segment.waypoints.Count);
                Gizmos.DrawSphere(pos, headQuater.wayPointSize);
                //다음 웨이포인트 위치 얻기.
                Vector3 pNext = Vector3.zero;

                if (j < segment.waypoints.Count - 1 && segment.waypoints[j + 1] != null)
                {
                    pNext = segment.waypoints[j + 1].GetVisualPos();
                }

                if (pNext != Vector3.zero)
                {
                    if (segment == headQuater.curSegment)
                    {
                        Gizmos.color = new Color(1f, 0.3f, 0.1f);
                        
                    }
                    else
                    {
                        Gizmos.color = new Color(1f, 0f, 0f);
                    }
                    //두 웨이포인트의 연결선 그리기.
                    Gizmos.DrawLine(pos, pNext);
                    //arrowDrawType을 기반으로 화살표 수 설정.
                    int arrows = GetArrowCount(pos, pNext, headQuater);
                    //Draw arrows
                    for (int i = 1; i < arrows + 1; i++)
                    {
                        Vector3 point = Vector3.Lerp(pos, pNext, (float)i / (arrows + 1));
                        DrawArrow(point, pos - pNext, headQuater.arrowSizeWaypoint);
                    }
                }
            }
            //세그먼트를 연결하는 선 그리기.
            foreach (TrafficSegment nextSegment in segment.nextSegments)
            {
                if (nextSegment != null)
                {
                    Vector3 p1 = segment.waypoints.Last().GetVisualPos();
                    Vector3 p2 = nextSegment.waypoints.First().GetVisualPos();

                    Gizmos.color = new Color(1f, 1f, 0);
                    Gizmos.DrawLine(p1, p2);

                    if (headQuater.arrowDrawType != TrafficHeadQuater.ArrowDraw.Off)
                    {
                        DrawArrow((p1 + p2) / 2f, p1 - p2, headQuater.arrowSizeIntersection);
                    }
                }
            }
        }
        
    }
}
