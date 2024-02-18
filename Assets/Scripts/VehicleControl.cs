using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class VehicleControl : MonoBehaviour
{
    //0
    private WheelDriveControl wheelDriveControl;
    private float initMaxSpeed = 0f;

    //1
    public struct Target
    {
        public int segment;
        public int waypoint;
    }

    public enum Status
    {
        GO,
        STOP,
        SLOW_DOWN
    }
    public TrafficHeadQuater trafficHeadQuater;
    public float waypointThresh = 2.5f;

    public Transform raycastAnchor;
    public float raycastLength = 3f;
    public int raySpacing = 3;
    public int raysNumber = 8;
    
    public float emergencyBrakeThresh = 1.5f;
    public float slowDownThresh = 5f;

    [HideInInspector] public Status vehicleStatus = Status.GO;

    private int pastTargetSegment = -1;
    private Target currnetTarget;
    private Target nextTarget;
    
    

    // Start is called before the first frame update
    void Start()
    {
        //0
        wheelDriveControl = GetComponent<WheelDriveControl>();
        initMaxSpeed = wheelDriveControl.maxSpeed;
        //1
        if(raycastAnchor == null && transform.Find("Raycast Anchor") != null)
        {
            raycastAnchor = transform.Find("Raycast Anchor");
        }
        
        SetWaypointVehicleIsOn();
    }

    // Update is called once per frames
    void Update()
    {
        //0
        //  float acc = 1f;
        //  float brake = 0f;
        //  float steering = 0f;
        // wheelDriveControl.maxSpeed = initMaxSpeed;
        //
        // wheelDriveControl.Move(acc, steering, brake);
        
        //1
        if (trafficHeadQuater == null)
        {
            return;
        }
        WaypointChecker();
        MoveVehicle();
    }
    
    //1
    int GetNextSegmentID()
    {
        List<TrafficSegment> nextSegments = trafficHeadQuater.segments[currnetTarget.segment].nextSegments;
        if (nextSegments.Count == 0)
        {
            return 0;
        }

        int c = Random.Range(0, nextSegments.Count);
        return nextSegments[c].ID;
    }

    void SetWaypointVehicleIsOn()
    {
        foreach (var segment in trafficHeadQuater.segments)
        {
            if (segment.IsOnSegment(transform.position))
            {
                currnetTarget.segment = segment.ID;

                float minDist = float.MaxValue;
                
                for (int j = 0; j < trafficHeadQuater.segments[currnetTarget.segment].waypoints.Count; j++)
                {
                    float dis = Vector3.Distance(transform.position,
                        trafficHeadQuater.segments[currnetTarget.segment].waypoints[j].transform.position);

                    Vector3 lSpace = transform.InverseTransformPoint(
                        trafficHeadQuater.segments[currnetTarget.segment].waypoints[j].transform.position);
                    if (dis < minDist && lSpace.z > 0f)
                    {
                        minDist = dis;
                        currnetTarget.waypoint = j;
                    }
                    
                }
                break;
            }
        }

        nextTarget.waypoint = currnetTarget.waypoint + 1;
        nextTarget.segment = currnetTarget.segment;

        if (nextTarget.waypoint >= trafficHeadQuater.segments[currnetTarget.segment].waypoints.Count)
        {
            nextTarget.waypoint = 0;
            nextTarget.segment = GetNextSegmentID();
        }
    }

    void WaypointChecker()
    {
        GameObject waypoint = trafficHeadQuater.segments[currnetTarget.segment].waypoints[currnetTarget.waypoint]
            .gameObject;

        Vector3 wpDist = transform.InverseTransformPoint(new Vector3(waypoint.transform.position.x,
            transform.position.y, waypoint.transform.position.z));

        if (wpDist.magnitude < waypointThresh)
        {
            currnetTarget.waypoint++;
            if (currnetTarget.waypoint >= trafficHeadQuater.segments[currnetTarget.segment].waypoints.Count)
            {
                pastTargetSegment = currnetTarget.segment;
                currnetTarget.segment = nextTarget.segment;
                currnetTarget.waypoint = 0;
            }

            nextTarget.waypoint = currnetTarget.waypoint + 1;
            if (nextTarget.waypoint >= trafficHeadQuater.segments[currnetTarget.segment].waypoints.Count)
            {
                nextTarget.waypoint = 0;
                nextTarget.segment = GetNextSegmentID();
            }
        }
    }

    void CastRay(Vector3 anchor, float angle, Vector3 dir, float length,
        out GameObject outObstacle, out float outHitDistance)
    {
        outObstacle = null;
        outHitDistance = -1f;
        
        Debug.DrawRay(anchor, Quaternion.Euler(0f,angle,0f) * dir * length,
            new Color(1f,0f,0f,0.5f));

        int layer = 1 << LayerMask.NameToLayer(TrafficIntersection.VehicleTagLayer);
        int finalMask = layer;

        foreach (var layerName in trafficHeadQuater.collisionLayers)
        {
            int id = 1 << LayerMask.NameToLayer(layerName);
            finalMask = finalMask | id;
        }

        RaycastHit hit;
        if (Physics.Raycast(anchor, Quaternion.Euler(0, angle, 0) * dir, out hit, length, finalMask))
        {
            outObstacle = hit.collider.gameObject;
            outHitDistance = hit.distance;
        }

    }

    GameObject GetDetectObstacles(out float hitDist)
    {
        GameObject detectObstacle = null;
        float minDist = 1000f;
        float initRay = (raysNumber / 2f) * raySpacing;
        float _hitDist = -1f;
        for (float a = -initRay; a <= initRay; a += raySpacing)
        {
            CastRay(raycastAnchor.transform.position, a, transform.forward,
                raycastLength, out detectObstacle, out _hitDist);

            if (detectObstacle == null)
            {
                continue;
            }

            float dist = Vector3.Distance(transform.position, detectObstacle.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                break;
            }
        }

        hitDist = _hitDist;
        return detectObstacle;
    }

    void MoveVehicle()
    {
        float acc = 1f;
        float brake = 0f;
        float steering = 0f;
        wheelDriveControl.maxSpeed = initMaxSpeed;

        Transform targetTransform = trafficHeadQuater.segments[currnetTarget.segment].waypoints[currnetTarget.waypoint]
            .transform;
        Transform nextTargetTransform =
            trafficHeadQuater.segments[nextTarget.segment].waypoints[nextTarget.waypoint].transform;
        Vector3 nextVel = nextTargetTransform.position - targetTransform.position;
        float nextSteering = Mathf.Clamp(transform.InverseTransformDirection(nextVel.normalized).x, -1, 1);

        if (vehicleStatus == Status.STOP)
        {
            acc = 0f;
            brake = 1f;
            wheelDriveControl.maxSpeed = Mathf.Min(wheelDriveControl.maxSpeed / 2f, 5f);
        }
        else
        {
            if (vehicleStatus == Status.SLOW_DOWN)
            {
                acc = 0.3f;
                brake = 0f;
            }

            if (nextSteering > 0.3f || nextSteering < -0.3f)
            {
                wheelDriveControl.maxSpeed = Mathf.Min(wheelDriveControl.maxSpeed, wheelDriveControl.steeringSpeedMax);
            }

            float hitDist;
            GameObject obstacle = GetDetectObstacles(out hitDist);

            if (obstacle != null)
            {
                WheelDriveControl otherVehicle = null;
                otherVehicle = obstacle.GetComponent<WheelDriveControl>();

                if (otherVehicle != null)
                {
                    float dotFront = Vector3.Dot(transform.forward,
                        otherVehicle.transform.forward);
                    if (otherVehicle.maxSpeed < wheelDriveControl.maxSpeed &&
                        dotFront > 0.8f)
                    {
                        float ms = Mathf.Max(wheelDriveControl.GetSpeedMS(otherVehicle.maxSpeed) - 0.5f, 0.1f);
                        wheelDriveControl.maxSpeed = wheelDriveControl.GetSpeedUnit(ms);
                    }

                    if (hitDist < emergencyBrakeThresh && dotFront > 0.8f)
                    {
                        acc = 0f;
                        brake = 1f;
                        wheelDriveControl.maxSpeed = Mathf.Max(wheelDriveControl.maxSpeed / 2f,
                            wheelDriveControl.minSpeed);
                    }
                    
                    else if (hitDist < emergencyBrakeThresh && dotFront <= 0.8f)
                    {
                        acc = -0.3f;
                        brake = 0f;
                        wheelDriveControl.maxSpeed = MathF.Max(wheelDriveControl.maxSpeed / 2f,
                            wheelDriveControl.minSpeed);

                        float dotRight = Vector3.Dot(transform.forward, otherVehicle.transform.forward);
                        //right
                        if (dotRight > 0.1f)
                        {
                            steering = -0.3f;
                        }
                        else if (dotRight < -0.1f)
                        {
                            steering = 0.3f;
                        }
                        else
                        {
                            steering = -0.7f;
                        }
                        
                    }
                    
                    else if (hitDist < slowDownThresh)
                    {
                        acc = 0.5f;
                        brake = 0f;
                    }
                }
                else
                {
                    if (hitDist < emergencyBrakeThresh)
                    {
                        acc = 0f;
                        brake = 1f;
                        wheelDriveControl.maxSpeed = Mathf.Max(wheelDriveControl.maxSpeed / 2f,
                            wheelDriveControl.minSpeed);
                        
                    }
                    else if (hitDist < slowDownThresh)
                    {
                        acc = 0.5f;
                        brake = 0f;
                    }
                }
            }

            if (acc > 0f)
            {
                Vector3 desiredVel =
                    trafficHeadQuater.segments[currnetTarget.segment].waypoints[currnetTarget.waypoint].transform
                        .position - transform.position;
                steering = Mathf.Clamp(transform.InverseTransformDirection(desiredVel.normalized).x, -1, 1f);
                
            }
        }

        wheelDriveControl.Move(acc, steering, brake);
    }

    public int GetSegmentVehicleIsIn()
    {
        int vehicleSegment = currnetTarget.segment;
        bool isOnSegment = trafficHeadQuater.segments[vehicleSegment].IsOnSegment(transform.position);
        if (isOnSegment == false)
        {
            bool isOnPSegment = trafficHeadQuater.segments[pastTargetSegment].IsOnSegment(transform.position);
            if (isOnPSegment)
            {
                vehicleSegment = pastTargetSegment;
            }
        }

        return vehicleSegment;
    }
}
