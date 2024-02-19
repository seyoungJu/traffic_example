using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum IntersectionType
{
    NONE,
    STOP,
    TRAFFIC_LIGHT,
    TRAFFIC_SLOW,
    EMERGENCY,
}
public class TrafficIntersection : MonoBehaviour
{
    public IntersectionType intersectionType = IntersectionType.NONE;
    public int ID;
    //우선 멈춤 구간들.
    public List<TrafficSegment> prioritySegments;
    //신호등 구간.
    public float lightDuration = 8f;
    private float lastChangeLightTime = 0f;
    private Coroutine lightRoutine;
    public float lightRepeatRate = 8f;
    public float orangeLightDuration = 2;
    //빨간 불 구간.
    public List<TrafficSegment> lightsNBr1;
    public List<TrafficSegment> lightsNBr2;

    private List<GameObject> vehiclesQueue;
    private List<GameObject> vehiclesInIntersection;
    private TrafficHeadQuater trafficHeadQuater;
    public const string VehicleTagLayer = "AutonomousVehicle";

    [HideInInspector] public int currentRedLightsGroup = 1;

    bool IsRedLightSegment(int vehicleSegment)
    {
        if (currentRedLightsGroup == 1)
        {
            foreach (var segment in lightsNBr1)
            {
                if (segment.ID == vehicleSegment)
                {
                    return true;
                }
            }
        }
        else if (currentRedLightsGroup == 2)
        {
            foreach (var segment in lightsNBr2)
            {
                if (segment.ID == vehicleSegment)
                {
                    return true;
                }
            }
        }

        return false;
    }

    void MoveVehiclesQueue()
    {
        //큐에 있는 모든 자동차를 이동시킴.
        List<GameObject> nVehicleQueue = new List<GameObject>(vehiclesQueue);
        foreach (var vehicle in vehiclesQueue)
        {
            VehicleControl vehicleControl = vehicle.GetComponent<VehicleControl>();

            int vehicleSegment = vehicleControl.GetSegmentVehicleIsIn();
            if (IsRedLightSegment(vehicleSegment) == false)
            {
                vehicleControl.vehicleStatus = VehicleControl.Status.GO;
                nVehicleQueue.Remove(vehicle);
            }
        }

        vehiclesQueue = nVehicleQueue;
    }

    void SwitchLights()
    {
        if (currentRedLightsGroup == 1)
        {
            currentRedLightsGroup = 2;
        }
        else if (currentRedLightsGroup == 2)
        {
            currentRedLightsGroup = 1;
        }
        else
        {
            currentRedLightsGroup = 1;
        }
            
        //다른 차량을 움직이게 하기 전에 조명 전환 후 몇 초 동안 기다리십시오(= 주황색 조명).
        Invoke("MoveVehiclesQueue", orangeLightDuration);
    }
    
    void Start()
    {
        vehiclesQueue = new List<GameObject>();
        vehiclesInIntersection = new List<GameObject>();
        lastChangeLightTime = Time.time;
    }

    private void Update()
    {
        switch(intersectionType)
        {
            case IntersectionType.EMERGENCY:
                if(lightRoutine != null)
                {
                    StopCoroutine(lightRoutine);
                    currentRedLightsGroup = 0;
                }
                break;
            case IntersectionType.TRAFFIC_LIGHT:

                if(Time.time > lastChangeLightTime + lightDuration)
                {
                    lastChangeLightTime = Time.time;
                    lightRoutine = StartCoroutine("OnTrafficLight");
                }                
                break;

        }
        
        
    }

    private IEnumerator OnTrafficLight()
    {        
        SwitchLights();
        yield return new WaitForSeconds(lightRepeatRate);
    }

    bool IsAlreadyInIntersection(GameObject target)
    {
        foreach (var vehicle in vehiclesInIntersection)
        {
            if (vehicle.GetInstanceID() == target.GetInstanceID())
            {
                return true;
            }
        }

        foreach (var vehicle in vehiclesQueue)
        {
            if (vehicle.GetInstanceID() == target.GetInstanceID())
            {
                return true;
            }
        }

        return false;
    }

    bool IsPrioritySegment(int vehicleSegment)
    {
        foreach (var segment in prioritySegments)
        {
            if (vehicleSegment == segment.ID)
            {
                return true;
            }
        }

        return false;
    }

    void TriggerStop(GameObject vehicle)
    {
        VehicleControl vehicleControl = vehicle.GetComponent<VehicleControl>();
        //웨이포인트 임계값에 따라 자동차는 대상 구간 또는 지난 구간에 있을 수 있습니다.
        int vehicleSegment = vehicleControl.GetSegmentVehicleIsIn();

        if (IsPrioritySegment(vehicleSegment) == false)
        {
            if (vehiclesQueue.Count > 0 || vehiclesInIntersection.Count > 0)
            {
                vehicleControl.vehicleStatus = VehicleControl.Status.STOP;
                vehiclesQueue.Add(vehicle);
            }
            else
            {
                vehiclesInIntersection.Add(vehicle);
                vehicleControl.vehicleStatus = VehicleControl.Status.SLOW_DOWN;
            }
        }
        else
        {
            vehicleControl.vehicleStatus = VehicleControl.Status.SLOW_DOWN;
            vehiclesInIntersection.Add(vehicle);
        }
        
    }

    void ExitStop(GameObject vehicle)
    {
        vehicle.GetComponent<VehicleControl>().vehicleStatus = VehicleControl.Status.GO;
        vehiclesInIntersection.Remove(vehicle);
        vehiclesQueue.Remove(vehicle);

        if (vehiclesQueue.Count > 0 && vehiclesInIntersection.Count == 0)
        {
            vehiclesQueue[0].GetComponent<VehicleControl>().vehicleStatus = VehicleControl.Status.GO;
        }
    }

    void TriggerLight(GameObject vehicle)
    {
        VehicleControl vehicleControl = vehicle.GetComponent<VehicleControl>();
        int vehicleSegment = vehicleControl.GetSegmentVehicleIsIn();

        if (IsRedLightSegment(vehicleSegment))
        {
            vehicleControl.vehicleStatus = VehicleControl.Status.STOP;
            vehiclesQueue.Add(vehicle);
        }
        else
        {
            vehicleControl.vehicleStatus = VehicleControl.Status.GO;
        }
    }

    void ExitLight(GameObject vehicle)
    {
        vehicle.GetComponent<VehicleControl>().vehicleStatus = VehicleControl.Status.GO;
    }

    void TriggerEmergency(GameObject vehicle)
    {
        VehicleControl vehicleControl = vehicle.GetComponent<VehicleControl>();
        int vehicleSegment = vehicleControl.GetSegmentVehicleIsIn();

        vehicleControl.vehicleStatus = VehicleControl.Status.STOP;
        vehiclesQueue.Add(vehicle);

    }
    void ExitEmergency(GameObject vehicle)
    {
        vehicle.GetComponent<VehicleControl>().vehicleStatus = VehicleControl.Status.GO;
    }

    private void OnTriggerEnter(Collider other)
    {
        //차량이 이미 목록에 있는지 확인하고, 그렇다면 중단.
        //방금 장면을 시작한 경우에도 중단합니다(시작 시 충돌체 내부의 차량이 있는 경우).
        if (IsAlreadyInIntersection(other.gameObject) || Time.timeSinceLevelLoad < .5f)
        {
            return;
        }

        if(other.tag != VehicleTagLayer)
        {
            return;
        }
        switch(intersectionType)
        {
            case IntersectionType.STOP:
                TriggerStop(other.gameObject);
                break;
            case IntersectionType.TRAFFIC_LIGHT:
                TriggerLight(other.gameObject);
                break;
            case IntersectionType.EMERGENCY:
                TriggerEmergency(other.gameObject);
                break;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag != VehicleTagLayer)
        {
            return;
        }
        switch (intersectionType)
        {
            case IntersectionType.STOP:
                ExitStop(other.gameObject);
                break;
            case IntersectionType.TRAFFIC_LIGHT:
                ExitLight(other.gameObject);
                break;
            case IntersectionType.EMERGENCY:
                ExitEmergency(other.gameObject);
                break;
        }
    }
}
