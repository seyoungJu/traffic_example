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
    public float downForce = 100f;
    public float maxAngle = 30f;
    public float steeringLerp = 5f;
    public float steeringSpeedMax = 20f;
    public float maxTorque = 300f;
    public float brakeTorque = 30000f;

    public UnitType unitType;

    public float minSpeed = 5f;
    public float maxSpeed = 50f;

    public GameObject leftWheelShape;
    public GameObject rightWheelShape;

    //public bool animateWheels = true;
    public DriveType driveType;

    private WheelCollider[] wheels;
    private float currentSteering = 0f;
    private Rigidbody rb;

    private void OnAwake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        wheels = GetComponentsInChildren<WheelCollider>();
        foreach (var wheel in wheels)
        {
            wheel.ConfigureVehicleSubsteps(10, 1, 1);
        }
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

        float angle = maxAngle * nSteering;
        float torque = maxTorque * _acceleration;

        float handBrake = _brake > 0 ? brakeTorque : 0;
        // Steer front wheels only
        foreach (var wheel in wheels)
        {
            if (wheel.transform.localPosition.z > 0)
            {
                wheel.steerAngle = angle;
                if (driveType != DriveType.FrontWheelDrive)
                {
                    wheel.motorTorque = torque;
                }
            }
            else
            {
                wheel.brakeTorque = handBrake;
                if (driveType != DriveType.RearWheelDrive)
                {
                    wheel.motorTorque = torque;
                }
            }
            
        }

        if (rb != null)
        {
            float s = GetSpeedUnit(rb.velocity.magnitude);
            if (s > maxSpeed)
            {
                rb.velocity = GetSpeedMS(maxSpeed) * rb.velocity.normalized;
            }

            rb.AddForce(-transform.up * downForce * rb.velocity.magnitude);
        }

    }
}