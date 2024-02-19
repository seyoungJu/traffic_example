using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TrafficHeadQuater))]
public class TrafficHQEditor : Editor
{
    private TrafficHeadQuater headQuater;
    //웨이포인트 이동 참고사항.
    private Vector3 startPosition;
    private Vector3 lastPoint;
    private TrafficWaypoint lastWaypoint;

    [MenuItem("Component/Traffic Simulation/Create Traffic Objects")]
    private static void CreateTraffic()
    {
        EditorHelper.SetUndoGroup("Create Traffic Objects");

        GameObject mainGO = EditorHelper.CreateGameObject("Traffic System");
        EditorHelper.AddComponent<TrafficHeadQuater>(mainGO);

        GameObject segmentsGO = EditorHelper.CreateGameObject("Segments", mainGO.transform);
        GameObject intersectionGO = EditorHelper.CreateGameObject("Intersection", mainGO.transform);
        //Close Undo Operation
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
    }

    private void OnEnable()
    {
        headQuater = target as TrafficHeadQuater;
    }

    private void AddWaypoint(Vector3 position)
    {
        GameObject go = EditorHelper.CreateGameObject("Waypoint-" + headQuater.curSegment.waypoints.Count,
            headQuater.curSegment.transform);
        go.transform.position = position;

        TrafficWaypoint waypoint = EditorHelper.AddComponent<TrafficWaypoint>(go);
        waypoint.Refresh(headQuater.curSegment.waypoints.Count, headQuater.curSegment);
        //TrafficHeadQuater에 대한 변경 사항을 기록합니다(여기서는 관련 없는 문자열).
        Undo.RecordObject(headQuater.curSegment, "");
        headQuater.curSegment.waypoints.Add(waypoint);
    }

    private void AddSegment(Vector3 position)
    {
        int segID = headQuater.segments.Count;
        GameObject segGameObject =
            EditorHelper.CreateGameObject("Segment-" + segID.ToString(), headQuater.transform.GetChild(0).transform);
        segGameObject.transform.position = position;

        headQuater.curSegment = EditorHelper.AddComponent<TrafficSegment>(segGameObject);
        headQuater.curSegment.ID = segID;
        headQuater.curSegment.waypoints = new List<TrafficWaypoint>();
        headQuater.curSegment.nextSegments = new List<TrafficSegment>();
        //TrafficHeadQuater에 대한 변경 사항을 기록합니다(여기서는 관련 없는 문자열).
        Undo.RecordObject(headQuater, "");
        headQuater.segments.Add(headQuater.curSegment);

    }

    private void AddIntersection(Vector3 position)
    {
        int intID = headQuater.intersections.Count;
        GameObject interGO =
            EditorHelper.CreateGameObject("Intersection-" + intID, headQuater.transform.GetChild(1).transform);
        interGO.transform.position = position;

        BoxCollider boxCollider = EditorHelper.AddComponent<BoxCollider>(interGO);
        boxCollider.isTrigger = true;
        TrafficIntersection intersection = EditorHelper.AddComponent<TrafficIntersection>(interGO);
        intersection.ID = intID;
        //TrafficHeadQuater에 대한 변경 사항을 기록합니다(여기서는 관련 없는 문자열).
        Undo.RecordObject(headQuater, "");
        headQuater.intersections.Add(intersection);
    }
    

    private void OnSceneGUI()
    {
        Event @event = Event.current;
        if(@event == null)
            return;

        Ray ray = HandleUtility.GUIPointToWorldRay(@event.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit) && @event.type == EventType.MouseDown && @event.button == 0)
        {
            //마우스 클릭 + Shift에 새로운 웨이포인트 추가.
            if (@event.shift)
            {
                if (headQuater.curSegment == null)
                {
                    return;
                }
                EditorHelper.BeginUndoGroup("Add Waypoint",headQuater);
                AddWaypoint(hit.point);
                //Close Undo Group
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }
            //세그먼트 생성 + 마우스 클릭 + Ctrl에 새 waypoint 추가.
            else if (@event.control)
            {
                EditorHelper.BeginUndoGroup("Add Segment", headQuater);
                AddSegment(hit.point);
                AddWaypoint(hit.point);
                //Close Undo Group
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                
            }
            //교차점 생성.
            else if (@event.alt)
            {
                EditorHelper.BeginUndoGroup("Add Intersection", headQuater);
                AddIntersection(hit.point);
                //Close Undo Group
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }
        }
        //웨이포인트 시스템을 계층 구조에서 선택한 게임 개체로 설정.
        Selection.activeGameObject = headQuater.gameObject;
        //선택한 웨이포인트를 처리합니다.
        if (lastWaypoint != null)
        {
            //광선이 충돌할 수 있도록 Plane을 사용합니다.
            Plane plane = new Plane(Vector3.up, lastWaypoint.GetVisualPos());
            plane.Raycast(ray, out float dst);
            Vector3 hitPoint = ray.GetPoint(dst);
            //마우스 버튼을 처음 눌렀을 때 lastPoint 재설정.
            if (@event.type == EventType.MouseDown && @event.button == 0)
            {
                lastPoint = hitPoint;
                startPosition = lastWaypoint.transform.position;
            }
            //선택한 웨이포인트 이동.
            if (@event.type == EventType.MouseDrag && @event.button == 0)
            {
                Vector3 realPos = new Vector3(hitPoint.x - lastPoint.x, 0, hitPoint.z - lastPoint.z);

                lastWaypoint.transform.position += realPos;
                lastPoint = hitPoint;
;            }
            //선택한 웨이포인트를 해제.
            if (@event.type == EventType.MouseUp && @event.button == 0)
            {
                Vector3 curPos = lastWaypoint.transform.position;
                lastWaypoint.transform.position = startPosition;
                Undo.RegisterFullObjectHierarchyUndo(lastWaypoint, "Move Waypoint");
                lastWaypoint.transform.position = curPos;
            }
            //Draw a Sphere
            Handles.SphereHandleCap(0, lastWaypoint.GetVisualPos(), Quaternion.identity, headQuater.wayPointSize * 2f, EventType.Repaint);
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            SceneView.RepaintAll();
        }
        //모든 웨이포인트로부터 판별식을 통해 충돌되는 웨이포인트를 세팅.
        if (lastWaypoint == null)
        {
            lastWaypoint = headQuater.GetAllWaypoints()
                .FirstOrDefault(i => EditorHelper.SphereHit(i.GetVisualPos(), headQuater.wayPointSize, ray));
        }
        //현재 세그먼트를 현재 상호 작용하는 세그먼트로 업데이트합니다.
        if (lastWaypoint != null && @event.type == EventType.MouseDown)
        {
            headQuater.curSegment = lastWaypoint.segment;
        }
        //현재 웨이포인트 재설정.
        else if (lastWaypoint != null && @event.type == EventType.MouseMove)
        {
            lastWaypoint = null;
        }
    }

    void RestructureSystem()
    {
        //구간과 웨이포인트의 이름 바꾸기 및 구조 조정.
        List<TrafficSegment> segmentsList = new List<TrafficSegment>();
        int itSeg = 0;
        foreach (Transform trans in headQuater.transform.GetChild(0).transform)
        {
            TrafficSegment segment = trans.GetComponent<TrafficSegment>();
            if (segment != null)
            {
                List<TrafficWaypoint> waypointsList = new List<TrafficWaypoint>();
                segment.ID = itSeg;
                segment.gameObject.name = "Segment-" + itSeg.ToString();

                int itWay = 0;
                foreach (Transform trans2 in segment.transform)
                {
                    TrafficWaypoint waypoint = trans2.GetComponent<TrafficWaypoint>();
                    if (waypoint != null)
                    {
                        waypoint.Refresh(itWay, segment);
                        waypointsList.Add(waypoint);
                        itWay++;
                    }
                }

                segment.waypoints = waypointsList;
                segmentsList.Add(segment);
                itSeg++;

            }
        }
        //다음 세그먼트가 아직 존재하는지 확인.
        foreach (TrafficSegment segment in segmentsList)
        {
            List<TrafficSegment> nextSegmentsList = new List<TrafficSegment>();
            foreach (TrafficSegment nextSegment in segment.nextSegments)
            {
                if (nextSegment != null)
                {
                    nextSegmentsList.Add(nextSegment);
                }
            }

            segment.nextSegments = nextSegmentsList;
            
        }

        headQuater.segments = segmentsList;
        //Check intersections
        List<TrafficIntersection> intersectionList = new List<TrafficIntersection>();
        int itInter = 0;
        foreach (Transform transInter in headQuater.transform.GetChild(1).transform)
        {
            TrafficIntersection intersection = transInter.GetComponent<TrafficIntersection>();
            if (intersection != null)
            {
                intersection.ID = itInter;
                intersection.gameObject.name = "InterSection-" + itInter;
                intersectionList.Add(intersection);
                itInter++;
            }
        }

        headQuater.intersections = intersectionList;
        //Unity에 무언가 변경되었으며 장면을 저장해야 한다고 알립니다.
        if (!EditorUtility.IsDirty(target))
        {
            EditorUtility.SetDirty(target);
        }
        
        Debug.Log("[Traffic Simulation] Successfully rebuilt the traffic system.");

    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        
        //이 호출 후에 변경 사항이 있으면 실행 취소를 등록하세요.
        Undo.RecordObject(headQuater, "Traffic Inspector Edit");
        //Draw the Inspector 
        TrafficHQEditorInspector.DrawInspector(headQuater, serializedObject, out bool restructureSystem);
        //웨이포인트가 삭제된 경우 웨이포인트 이름 바꾸기.
        if (restructureSystem)
        {
            RestructureSystem();
        }
        //값이 편집된 경우 Scene을 다시 그림.
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
