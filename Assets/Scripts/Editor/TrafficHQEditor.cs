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
            if (@event.shift)
            {
                if (headQuater.curSegment == null)
                {
                    return;
                }
                EditorHelper.BeginUndoGroup("Add Waypoint",headQuater);
                AddWaypoint(hit.point);
                
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }
            
            else if (@event.control)
            {
                EditorHelper.BeginUndoGroup("Add Segment", headQuater);
                AddSegment(hit.point);
                AddWaypoint(hit.point);
                
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                
            }
            
            else if (@event.alt)
            {
                EditorHelper.BeginUndoGroup("Add Intersection", headQuater);
                AddIntersection(hit.point);
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }
        }

        Selection.activeGameObject = headQuater.gameObject;

        if (lastWaypoint != null)
        {
            Plane plane = new Plane(Vector3.up, lastWaypoint.GetVisualPos());
            plane.Raycast(ray, out float dst);
            Vector3 hitPoint = ray.GetPoint(dst);

            if (@event.type == EventType.MouseDown && @event.button == 0)
            {
                lastPoint = hitPoint;
                startPosition = lastWaypoint.transform.position;
            }

            if (@event.type == EventType.MouseDrag && @event.button == 0)
            {
                Vector3 realPos = new Vector3(hitPoint.x - lastPoint.x, 0, hitPoint.z - lastPoint.z);

                lastWaypoint.transform.position += realPos;
                lastPoint = hitPoint;
;            }

            if (@event.type == EventType.MouseUp && @event.button == 0)
            {
                Vector3 curPos = lastWaypoint.transform.position;
                lastWaypoint.transform.position = startPosition;
                Undo.RegisterFullObjectHierarchyUndo(lastWaypoint, "Move Waypoint");
                lastWaypoint.transform.position = curPos;
            }
            
            Handles.SphereHandleCap(0, lastWaypoint.GetVisualPos(), Quaternion.identity, headQuater.wayPointSize * 2f, EventType.Repaint);
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            SceneView.RepaintAll();
        }

        if (lastWaypoint == null)
        {
            lastWaypoint = headQuater.GetAllWaypoints()
                .FirstOrDefault(i => EditorHelper.SphereHit(i.GetVisualPos(), headQuater.wayPointSize, ray));
        }

        if (lastWaypoint != null && @event.type == EventType.MouseDown)
        {
            headQuater.curSegment = lastWaypoint.segment;
        }
        
        else if (lastWaypoint != null && @event.type == EventType.MouseMove)
        {
            lastWaypoint = null;
        }
    }

    void RestructureSystem()
    {
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

        List<TrafficIntersection> intersectionList = new List<TrafficIntersection>();
        int itInter = 0;
        foreach (Transform transInter in headQuater.transform.GetChild(0).transform)
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

        if (!EditorUtility.IsDirty(target))
        {
            EditorUtility.SetDirty(target);
        }
        
        Debug.Log("[Traffic Simulation] Successfully rebuilt the traffic system.");

    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        
        
        Undo.RecordObject(headQuater, "Traffic Inspector Edit");
        
        TrafficHQEditorInspector.DrawInspector(headQuater, serializedObject, out bool restructureSystem);

        if (restructureSystem)
        {
            RestructureSystem();
        }

        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
