using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;

public class VehicleControlEditor : Editor
{
    [MenuItem("Component/Traffic Simulation/Setup Vehicle")]
    private static void SetupVehicle()
    {
        EditorHelper.SetUndoGroup("Setup Vehicle");

        GameObject selected = Selection.activeGameObject;

        GameObject anchor = EditorHelper.CreateGameObject("RayCast Anchor", selected.transform);

        VehicleControl vehicleControl = EditorHelper.AddComponent<VehicleControl>(selected);
        WheelDriveControl wheelDriveControl = EditorHelper.AddComponent<WheelDriveControl>(selected);

        TrafficHeadQuater headQuater = FindObjectOfType<TrafficHeadQuater>();

        anchor.transform.localPosition = Vector3.zero;
        anchor.transform.localRotation = quaternion.identity;
        vehicleControl.raycastAnchor = anchor.transform;

        if (headQuater != null)
        {
            vehicleControl.trafficHeadQuater = headQuater;
        }

        EditorHelper.CreateLayer(TrafficIntersection.VehicleTagLayer);

        selected.tag = TrafficIntersection.VehicleTagLayer;
        EditorHelper.SetLayer(selected, LayerMask.NameToLayer(TrafficIntersection.VehicleTagLayer), true);
    }
}