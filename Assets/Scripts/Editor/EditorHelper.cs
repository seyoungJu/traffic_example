using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class EditorHelper
{
    public static void SetUndoGroup(string label)
    {
        //Create new Undo Group to collect all changes in one Undo
        Undo.SetCurrentGroupName(label);
    }

    public static void BeginUndoGroup(string undoName, TrafficHeadQuater trafficHeadQuater)
    {
        //Create new Undo Group to collect all changes in one Undo
        Undo.SetCurrentGroupName(undoName);
        //Register all TrafficSystem changes after this (string not relevant here)
        Undo.RegisterFullObjectHierarchyUndo(trafficHeadQuater.gameObject, undoName);
    }

    public static GameObject CreateGameObject(string name, Transform parent = null)
    {
        GameObject newGameObject = new GameObject(name);
        newGameObject.transform.position = Vector3.zero;
        newGameObject.transform.localScale = Vector3.one;
        newGameObject.transform.localRotation = Quaternion.identity;
        
        Undo.RegisterFullObjectHierarchyUndo(newGameObject, "Spawn new GameObject");
        Undo.SetTransformParent(newGameObject.transform, parent, "Set parent");

        return newGameObject;
    }

    public static T AddComponent<T>(GameObject target) where T : Component
    {
        return Undo.AddComponent<T>(target);
    }

    //https://mathsathome.com/the-discriminant-quadratic/
    //https://blog.naver.com/kiseop91/222351977397
    //판별식.
    public static bool SphereHit(Vector3 center, float radius, Ray ray)
    {
        Vector3 originToCenter = ray.origin - center;
        float a = Vector3.Dot(ray.direction, ray.direction);
        float b = 2f * Vector3.Dot(originToCenter, ray.direction);
        float c = Vector3.Dot(originToCenter, originToCenter) - radius * radius;
        float discriminant = b * b - 4f * a * c;

        if (discriminant < 0f)
        {
            return false;
        }

        float sqrt = Mathf.Sqrt(discriminant);
        return -b - sqrt > 0f || -b + sqrt > 0f;
    }

    public static void CreateLayer(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new System.ArgumentException("name", "New Layer name string is either null or empty");
        }

        var targManager =
            new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layersProps = targManager.FindProperty("layers");
        var propCount = layersProps.arraySize;

        SerializedProperty firstEmptyProp = null;

        for (var i = 0; i < propCount; i++)
        {
            var layerProp = layersProps.GetArrayElementAtIndex(i);
            var stringValue = layerProp.stringValue;
            if (stringValue == name)
            {
                return;
            }

            if (i < 8 || stringValue != string.Empty)
            {
                continue;
            }

            if (firstEmptyProp == null)
            {
                firstEmptyProp = layerProp;
                break;
            }
        }

        if (firstEmptyProp == null)
        {
            Debug.LogError($"Maximum Limit of {propCount} layers Exceeded. Layer \" {name} \"not created.");
            return;
        }

        firstEmptyProp.stringValue = name;
        targManager.ApplyModifiedProperties();
    }

    public static void SetLayer(this GameObject gameObject, int layer, bool includeChildren = false)
    {
        if (!includeChildren)
        {
            gameObject.layer = layer;
            return;
        }

        foreach (var child in gameObject.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
        }
    }
}

public static class InspectorHelper
{
    public static void Label(string label)
    {
        EditorGUILayout.LabelField(label);
    }

    public static void Header(string label)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    }

    public static void Toggle(string label, ref bool toggle)
    {
        toggle = EditorGUILayout.Toggle(label, toggle);
    }

    public static void IntField(string label, ref int value)
    {
        value = EditorGUILayout.IntField(label, value);
    }

    public static void IntField(string label, ref int value, int min, int max)
    {
        value = Mathf.Clamp(EditorGUILayout.IntField(label, value), min, max);
    }

    public static void FloatField(string label, ref float value)
    {
        value = EditorGUILayout.FloatField(label, value);
    }

    public static void PropertyField(string label, string value, SerializedObject serializedObject)
    {
        SerializedProperty extra = serializedObject.FindProperty(value);
        EditorGUILayout.PropertyField(extra, new GUIContent(label), true);
    }

    public static void HelpBox(string content)
    {
        EditorGUILayout.HelpBox(content, MessageType.Info);
    }

    public static bool Button(string label)
    {
        return GUILayout.Button(label);
    }

    public static void DrawArrowTypeSelection(TrafficHeadQuater trafficHeadQuater)
    {
        trafficHeadQuater.arrowDrawType =
            (TrafficHeadQuater.ArrowDraw)EditorGUILayout.EnumPopup("Arrow Draw Type", trafficHeadQuater.arrowDrawType);
        EditorGUI.indentLevel++;

        switch (trafficHeadQuater.arrowDrawType)
        {
            case TrafficHeadQuater.ArrowDraw.FixedCount:
                IntField("Count", ref trafficHeadQuater.arrowCount, 1, int.MaxValue);
                break;
            case TrafficHeadQuater.ArrowDraw.ByLength:
                FloatField("distance between arrows", ref trafficHeadQuater.arrowDistance);
                break;
            case TrafficHeadQuater.ArrowDraw.Off:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (trafficHeadQuater.arrowDrawType != TrafficHeadQuater.ArrowDraw.Off)
        {
            FloatField("Arrow Size Waypoint", ref trafficHeadQuater.arrowSizeWaypoint);
            FloatField("Arrow Size Intersection", ref trafficHeadQuater.arrowSizeIntersection);
        }

        EditorGUI.indentLevel--;

    }
}