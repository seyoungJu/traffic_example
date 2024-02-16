using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TrafficIntersectionEditor : Editor
{
    private TrafficIntersection intersection;

    private void OnEnable()
    {
        intersection = target as TrafficIntersection;
    }

    public override void OnInspectorGUI()
    {
        intersection.intersectionType =
            (IntersectionType)EditorGUILayout.EnumPopup("Intersection type", intersection.intersectionType);

        EditorGUI.BeginDisabledGroup(intersection.intersectionType != IntersectionType.STOP);
        {
            InspectorHelper.Header("Stop");
            InspectorHelper.PropertyField("priority segments", "prioritySegments", serializedObject);
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(intersection.intersectionType != IntersectionType.TRAFFIC_LIGHT);
        {
            InspectorHelper.Header("Traffic Lights");
            InspectorHelper.FloatField("Light Duration (in s.)", ref intersection.lightDuration);
            InspectorHelper.FloatField("Orange Light Duration (in s.)", ref intersection.orangeLightDuration);
            InspectorHelper.PropertyField("Lights #1 (first to be red)", "lightsNBr1", serializedObject);
            InspectorHelper.PropertyField("Lights #2", "lightsNBr2", serializedObject);
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUI.EndDisabledGroup();
    }
}