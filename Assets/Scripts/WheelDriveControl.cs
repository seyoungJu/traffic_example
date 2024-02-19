using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum DriveType
{
    RearWheelDrive,
    FrontWheelDrive,
    AllWheelDrive,
}

public enum UnitType
{
    KMH,
    MPH
}

[RequireComponent(typeof(Rigidbody))]
public class WheelDriveControl : MonoBehaviour
{
    [Tooltip("차량에 적용되는 다운포스.")] public float downForce = 100f;

    [Tooltip("바퀴의 최대 조향 각도.")]
    public float maxAngle = 60f;

    [Tooltip("위의 조향각에 도달하는 속도(lerp)")]
    public float steeringLerp = 5f;

    [Tooltip("차량이 방향을 바꾸려고 할 때의 최대 속도(아래에서 선택한 단위)")]
    public float steeringSpeedMax = 8f;

    [Tooltip("구동바퀴에 적용되는 최대 토크.")]
    public float maxTorque = 100f;

    [Tooltip("구동바퀴에 적용되는 최대 브레이크 토크.")]
    public float brakeTorque = 100000f;

    [Tooltip("속도계 단위.")] public UnitType unitType;

    [Tooltip("최소 속도 - 주행 시(정지/브레이크 제외) 위에서 선택한 단위입니다. 0보다 커야 합니다.")]
    public float minSpeed = 2f;

    [Tooltip("위에서 선택한 단위의 최대 속도")]
    public float maxSpeed = 10f;

    [Tooltip("여기에 바퀴 모양을 드래그하세요.")]
    public GameObject leftWheelShape;

    public GameObject rightWheelShape;

    [Tooltip("바퀴에 애니메이션을 적용할지 여부")]
    public bool animateWheels = true;

    [Tooltip("차량의 구동 유형: 후륜 구동, 전륜 구동 또는 4륜 구동.")]
    public DriveType driveType;

    private WheelCollider[] wheels;
    private float currentSteering = 0f;
    private Rigidbody rb;

    public void Init()
    {
        rb = GetComponent<Rigidbody>();
        wheels = GetComponentsInChildren<WheelCollider>();
        
        for (int i = 0; i < wheels.Length; ++i)
        {
            var wheel = wheels[i];
            
            // 필요할 때만 바퀴 모양을 만들자.
            if (leftWheelShape != null && wheel.transform.localPosition.x < 0)
            {
                var ws = Instantiate (leftWheelShape);
                ws.transform.parent = wheel.transform;
            }
            else if(rightWheelShape != null && wheel.transform.localPosition.x > 0)
            {
                var ws = Instantiate(rightWheelShape);
                ws.transform.parent = wheel.transform;
            }

            wheel.ConfigureVehicleSubsteps(10, 1, 1);
        }
    }

    private void OnAwake()
    {
        Init();
    }

    private void OnEnable()
    {
        Init();
    }

    public float GetSpeedMS(float speed)
    {
        if (speed == 0f)
        {
            return 0f;
        }

        return unitType == UnitType.KMH ? speed / 3.6f : speed / 2.237f;
    }

    public float GetSpeedUnit(float speed)
    {
        return unitType == UnitType.KMH ? speed * 3.6f : speed * 2.237f;
    }

    public void Move(float _acceleration, float _steering, float _brake)
    {
        float nSteering = Mathf.Lerp(currentSteering, _steering, Time.deltaTime * steeringLerp);
        currentSteering = nSteering;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        float angle = maxAngle * nSteering;
        float torque = maxTorque * _acceleration;

        float handBrake = _brake > 0 ? brakeTorque : 0;
        
        foreach (var wheel in wheels)
        {
            // // 앞바퀴만 조향
            if (wheel.transform.localPosition.z > 0)
            {
                wheel.steerAngle = angle;
            }

            if (wheel.transform.localPosition.z < 0)
            {
                wheel.brakeTorque = handBrake;
            }

            if (wheel.transform.localPosition.z < 0 && driveType != DriveType.FrontWheelDrive)
            {
                wheel.motorTorque = torque;
            }

            if (wheel.transform.localPosition.z >= 0 && driveType != DriveType.RearWheelDrive)
            {
                wheel.motorTorque = torque;
            }
            // 허용되는 경우 휠 애니메이션.
            if (animateWheels)
            {
                Quaternion quaternion;
                Vector3 pos;
                wheel.GetWorldPose(out pos, out quaternion);

                Transform shapeTransform = wheel.transform.GetChild(0);
                shapeTransform.position = pos;
                shapeTransform.rotation = quaternion;
            }
        }

        if (rb != null)
        {
            //Apply speed
            float s = GetSpeedUnit(rb.velocity.magnitude);
            if (s > maxSpeed)
            {
                rb.velocity = GetSpeedMS(maxSpeed) * rb.velocity.normalized;
            }
            //Apply downforce
            rb.AddForce(-transform.up * downForce * rb.velocity.magnitude);
        }
    }
}