using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleControl : MonoBehaviour
{
    private WheelDriveControl wheelDriveControl;
    private float initMaxSpeed = 0f;

    

    // Start is called before the first frame update
    void Start()
    {
        wheelDriveControl = GetComponent<WheelDriveControl>();
        initMaxSpeed = wheelDriveControl.maxSpeed;
    }

    // Update is called once per frame
    void Update()
    {
         float acc = 1f;
         float brake = 0f;
         float steering = 0f;
        wheelDriveControl.maxSpeed = initMaxSpeed;
        
        //wheelDriveControl.Move(acc, steering, brake);
        // float acc = Input.GetAxis("Vertical");
        // float steering = Input.GetAxis("Horizontal");
        // float brake = Input.GetKey(KeyCode.Space) ? 1 : 0;

        wheelDriveControl.Move(acc, steering, brake);

    }
}
