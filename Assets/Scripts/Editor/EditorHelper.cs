using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class EditorHelper
{
    public static void SetUndoGroup(string label)
    {
        Undo.SetCurrentGroupName(label);
    }

    public static void BeginUndoGroup(string undoName, TrafficHeadQuater trafficHeadQuater)
    {
        Undo.SetCurrentGroupName(undoName);
        Undo.RegisterFullObjectHierarchyUndo(trafficHeadQuater.gameObject, undoName);
    }

    public static GameObject CreateGameObject(string name, Transform parent = null)
    {
        GameObject newGameObject = new GameObject(name);

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