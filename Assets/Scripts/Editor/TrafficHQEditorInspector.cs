using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class TrafficHQEditorInspector
{
    public static void DrawInspector(TrafficHeadQuater trafficHeadQuater, SerializedObject serializedObject,
        out bool restructureSystem)
    {
        InspectorHelper.Header("Gizmo Config");
        InspectorHelper.Toggle("Hide Gizmos", ref trafficHeadQuater.hideGizmos);

        InspectorHelper.DrawArrowTypeSelection(trafficHeadQuater);
        InspectorHelper.FloatField("Waypoint size", ref trafficHeadQuater.wayPointSize);
        EditorGUILayout.Space();
        
        InspectorHelper.Header("System Config");
        InspectorHelper.FloatField("Segment Detection Threshold", ref trafficHeadQuater.segDetectThresh);
        
        InspectorHelper.PropertyField("Collision Layers", "collisionLayers", serializedObject);
        
        EditorGUILayout.Space();
        
        InspectorHelper.HelpBox("Ctrl + Left Click to create a new segment\n Shift + Left Click to create a new waypoint. \n Alt + Left Click to create a new intersection");
        InspectorHelper.HelpBox("Reminder : The Vehicles will follow the point depending on the sequence you added them. (go to the 1st waypoint added, then to the second, etc..)");
        
        EditorGUILayout.Space();
        restructureSystem = InspectorHelper.Button("Re-Structure Traffic System");
    }
}
