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
    [Header("교통 시스템.")]
    [Tooltip("현재 활성화 된 교통 시스템.")]
    public TrafficHeadQuater trafficHeadQuater;
    [Tooltip("차량이 목표에 도달한 시기를 확인합니다. 다음 웨이포인트를 더 일찍 예상하는 데 사용할 수 있습니다(이 숫자가 높을수록 다음 웨이포인트가 더 빨리 예상됩니다).")]
    public float waypointThresh = 2.5f;
    
    [Header("감지레이더.")]
    [Tooltip("레이를 쏠 앵커.")]
    public Transform raycastAnchor;
    [Tooltip("레이 길이.")]
    public float raycastLength = 3f;
    [Tooltip("레이 사이의 간격.")]
    public int raySpacing = 3;
    [Tooltip("생성될 레이 수")]
    public int raysNumber = 8;
    [Tooltip("감지된 차량이 이 거리 미만이면 자율주행차가 정지합니다.")]
    public float emergencyBrakeThresh = 1.5f;
    [Tooltip("감지된 차량이 이 거리보다 낮거나 거리보다 높을 경우 자아 차량의 속도가 느려집니다.")]
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
        //Find current target
        foreach (var segment in trafficHeadQuater.segments)
        {
            if (segment.IsOnSegment(transform.position))
            {
                currnetTarget.segment = segment.ID;
                //구간 내에서 시작할 가장 가까운 웨이포인트 찾기.
                float minDist = float.MaxValue;
                
                for (int j = 0; j < trafficHeadQuater.segments[currnetTarget.segment].waypoints.Count; j++)
                {
                    float dis = Vector3.Distance(transform.position,
                        trafficHeadQuater.segments[currnetTarget.segment].waypoints[j].transform.position);
                    //앞쪽 포인트만 가져가자.
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
        //다음 target 찾기.
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
        //차량을 기준으로 한 다음 웨이포인트의 위치.
        Vector3 wpDist = transform.InverseTransformPoint(new Vector3(waypoint.transform.position.x,
            transform.position.y, waypoint.transform.position.z));
        //현재 지점에 도착하면 다음 웨이포인트로 이동.
        if (wpDist.magnitude < waypointThresh)
        {
            //다음 목표 찾기
            currnetTarget.waypoint++;
            if (currnetTarget.waypoint >= trafficHeadQuater.segments[currnetTarget.segment].waypoints.Count)
            {
                pastTargetSegment = currnetTarget.segment;
                currnetTarget.segment = nextTarget.segment;
                currnetTarget.waypoint = 0;
            }
            //다음 타겟의 웨이 포인트.
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
        //Draw raycast
        Debug.DrawRay(anchor, Quaternion.Euler(0f,angle,0f) * dir * length,
            new Color(1f,0f,0f,0.5f));
        //자동차만 검출.
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
        //기본적으로 풀 엑셀, 노 브레이크, 노 조향.
        float acc = 1f;
        float brake = 0f;
        float steering = 0f;
        wheelDriveControl.maxSpeed = initMaxSpeed;
        
        //계획된 회전이 있는지 계산.
        Transform targetTransform = trafficHeadQuater.segments[currnetTarget.segment].waypoints[currnetTarget.waypoint]
            .transform;
        Transform nextTargetTransform =
            trafficHeadQuater.segments[nextTarget.segment].waypoints[nextTarget.waypoint].transform;
        Vector3 nextVel = nextTargetTransform.position - targetTransform.position;
        float nextSteering = Mathf.Clamp(transform.InverseTransformDirection(nextVel.normalized).x, -1, 1);
        
        //만약 차가 서야 한다면.
        if (vehicleStatus == Status.STOP)
        {
            acc = 0f;
            brake = 1f;
            wheelDriveControl.maxSpeed = Mathf.Min(wheelDriveControl.maxSpeed / 2f, 5f);
        }
        else
        {
            //속도를 줄여야 하는 경우.
            if (vehicleStatus == Status.SLOW_DOWN)
            {
                acc = 0.3f;
                brake = 0f;
            }
            //조향을 한다면 속도 조절.
            if (nextSteering > 0.3f || nextSteering < -0.3f)
            {
                wheelDriveControl.maxSpeed = Mathf.Min(wheelDriveControl.maxSpeed, wheelDriveControl.steeringSpeedMax);
            }
            //2. 레이더에 감지된 장애물이 있는지 확인.
            float hitDist;
            GameObject obstacle = GetDetectObstacles(out hitDist);
            //몬가 충돌되었다면.
            if (obstacle != null)
            {
                WheelDriveControl otherVehicle = null;
                otherVehicle = obstacle.GetComponent<WheelDriveControl>();
                
                ///////////////////////////////////////////////////////////////
                //다른 차량과 일반 장애물(제어 차량 포함)을 구별합니다.
                if (otherVehicle != null)
                {
                    //앞차인지 확인.
                    float dotFront = Vector3.Dot(transform.forward,
                        otherVehicle.transform.forward);
                    //감지된 앞 차량의 최대 속도가 자차의 최대 속도보다 낮으면 자차의 최대 속도를 줄입니다.
                    if (otherVehicle.maxSpeed < wheelDriveControl.maxSpeed &&
                        dotFront > 0.8f)
                    {
                        float ms = Mathf.Max(wheelDriveControl.GetSpeedMS(otherVehicle.maxSpeed) - 0.5f, 0.1f);
                        wheelDriveControl.maxSpeed = wheelDriveControl.GetSpeedUnit(ms);
                    }
                    //두 차량이 너무 가깝고 같은 방향을 향하고 있으면 멈춘다.
                    if (hitDist < emergencyBrakeThresh && dotFront > 0.8f)
                    {
                        acc = 0f;
                        brake = 1f;
                        wheelDriveControl.maxSpeed = Mathf.Max(wheelDriveControl.maxSpeed / 2f,
                            wheelDriveControl.minSpeed);
                    }
                    // 두 차량이 너무 가깝고 같은 방향을 향하지 않는 경우 자아 차량이 약간 뒤로 이동하게 만듭니다.
                    else if (hitDist < emergencyBrakeThresh && dotFront <= 0.8f)
                    {
                        acc = -0.3f;
                        brake = 0f;
                        wheelDriveControl.maxSpeed = MathF.Max(wheelDriveControl.maxSpeed / 2f,
                            wheelDriveControl.minSpeed);
                        //우리가 가까이에 있는 차량이 오른쪽에 있는지 왼쪽에 있는지 확인하고 그에 따라 조향을 적용하여 움직이게.
                        float dotRight = Vector3.Dot(transform.forward, otherVehicle.transform.forward);
                        //right
                        if (dotRight > 0.1f)
                        {
                            steering = -0.3f;
                        }
                        //Left
                        else if (dotRight < -0.1f)
                        {
                            steering = 0.3f;
                        }
                        //Middle
                        else
                        {
                            steering = -0.7f;
                        }
                        
                    }
                    //두 차량이 가까워지면 속도를 줄이자.
                    else if (hitDist < slowDownThresh)
                    {
                        acc = 0.5f;
                        brake = 0f;
                    }
                }
                ///////////////////////////////////////////////////////////////////
                // 일반 장애물.
                else
                {
                    //너무 가까우면 긴급 제동.
                    if (hitDist < emergencyBrakeThresh)
                    {
                        acc = 0f;
                        brake = 1f;
                        wheelDriveControl.maxSpeed = Mathf.Max(wheelDriveControl.maxSpeed / 2f,
                            wheelDriveControl.minSpeed);
                        
                    }
                    //그렇지 않으면 상대적으로 가까워지면 속도가 감소.
                    else if (hitDist < slowDownThresh)
                    {
                        acc = 0.5f;
                        brake = 0f;
                    }
                }
            }
            //경로를 따르도록 방향을 조정해야 하는지 확인.
            if (acc > 0f)
            {
                Vector3 desiredVel =
                    trafficHeadQuater.segments[currnetTarget.segment].waypoints[currnetTarget.waypoint].transform
                        .position - transform.position;
                steering = Mathf.Clamp(transform.InverseTransformDirection(desiredVel.normalized).x, -1, 1f);
                
            }
        }
        //차량 이동.
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
