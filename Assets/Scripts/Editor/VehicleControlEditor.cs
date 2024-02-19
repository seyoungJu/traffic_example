using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;

public class VehicleControlEditor : Editor
{
    private static void SetupWheelCollder(WheelCollider collider)
    {
        collider.mass = 20;
        collider.radius = 0.175f;
        collider.wheelDampingRate = 0.25f;
        collider.suspensionDistance = 0.06f;
        collider.forceAppPointDistance = 0f;
        
        JointSpring jointSpring = new JointSpring();
        jointSpring.spring = 70000f;
        jointSpring.damper = 3500f;
        jointSpring.targetPosition = 1f;
        collider.suspensionSpring = jointSpring;

        WheelFrictionCurve frictionCurve = new WheelFrictionCurve();
        frictionCurve.extremumSlip = 1f;
        frictionCurve.extremumValue = 1f;
        frictionCurve.asymptoteSlip = 1f;
        frictionCurve.asymptoteValue = 1f;
        frictionCurve.stiffness = 1f;
        collider.forwardFriction = frictionCurve;
        collider.sidewaysFriction = frictionCurve;
    }

    [MenuItem("Component/Traffic Simulation/Setup Vehicle")]
    private static void SetupVehicle()
    {
        EditorHelper.SetUndoGroup("Setup Vehicle");

        GameObject selected = Selection.activeGameObject;
        //레이캐스트 앵커 만들고.
        GameObject anchor = EditorHelper.CreateGameObject("RayCast Anchor", selected.transform);
        
        anchor.transform.localPosition = Vector3.zero;
        anchor.transform.localRotation = quaternion.identity;
        //자동차 조종 스크립트를 설정.
        VehicleControl vehicleControl = EditorHelper.AddComponent<VehicleControl>(selected);
        vehicleControl.raycastAnchor = anchor.transform;

        Transform tirebackleft = selected.transform.Find("Tire BackLeft");
        Transform tirebackRight = selected.transform.Find("Tire BackRight");
        Transform tirefrontLeft = selected.transform.Find("Tire FrontLeft");
        Transform tirefrontRight = selected.transform.Find("Tire FrontRight");

        WheelCollider wheelCollider1 = EditorHelper.AddComponent<WheelCollider>(tirebackleft.gameObject);
        WheelCollider wheelCollider2 = EditorHelper.AddComponent<WheelCollider>(tirebackRight.gameObject);
        WheelCollider wheelCollider3 = EditorHelper.AddComponent<WheelCollider>(tirefrontLeft.gameObject);
        WheelCollider wheelCollider4 = EditorHelper.AddComponent<WheelCollider>(tirefrontRight.gameObject);
        SetupWheelCollder(wheelCollider1);
        SetupWheelCollder(wheelCollider2);
        SetupWheelCollder(wheelCollider3);
        SetupWheelCollder(wheelCollider4);


         WheelDriveControl wheelDriveControl = EditorHelper.AddComponent<WheelDriveControl>(selected);
        wheelDriveControl.leftWheelShape = selected.transform.Find("Tire BackLeft").gameObject;
        wheelDriveControl.rightWheelShape = selected.transform.Find("Tire BackRight").gameObject;
        wheelDriveControl.Init();

        Rigidbody rb = selected.GetComponent<Rigidbody>();
        rb.mass = 900f;
        rb.drag = 0.1f;
        rb.angularDrag = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        TrafficHeadQuater headQuater = FindObjectOfType<TrafficHeadQuater>();

        if (headQuater != null)
        {
            vehicleControl.trafficHeadQuater = headQuater;
        }

        BoxCollider boxCollider = EditorHelper.AddComponent<BoxCollider>(selected);
        boxCollider.isTrigger = true;

        GameObject Colliders = EditorHelper.CreateGameObject("Colliders", selected.transform);
        Colliders.transform.localPosition = Vector3.zero;
        Colliders.transform.localRotation = Quaternion.identity;
        Colliders.transform.localScale = Vector3.one;
        GameObject Body = EditorHelper.CreateGameObject("Body", Colliders.transform);
        Body.transform.localPosition = Vector3.zero;
        Body.transform.localRotation = Quaternion.identity;
        Body.transform.localScale = Vector3.one;

        BoxCollider bodyCollider = EditorHelper.AddComponent<BoxCollider>(Body);
        bodyCollider.center = new Vector3(0f, 0.4f, 0f);
        bodyCollider.size = new Vector3(0.95f, 0.54f, 2.0f);
        //Create layer AutonomousVehicle if it doesn't exist
        EditorHelper.CreateLayer(TrafficIntersection.VehicleTagLayer);
        //Set the tag and layer name
        selected.tag = TrafficIntersection.VehicleTagLayer;
        EditorHelper.SetLayer(selected, LayerMask.NameToLayer(TrafficIntersection.VehicleTagLayer), true);

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
    }
    
}