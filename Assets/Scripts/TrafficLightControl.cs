using System.Collections;
using System.Collections.Generic;
using TrafficSimulation;
using UnityEngine;

public class TrafficLightControl : MonoBehaviour
{
    public int lightGroupID;
    public TrafficIntersection intersection;

    private Light pointLight;

    void SetTrifficLightColor()
    {
        if (lightGroupID == intersection.currentRedLightsGroup)
        {
            pointLight.color = new Color(1, 0, 0);
        }
        else
        {
            pointLight.color = new Color(0, 1, 0);
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        pointLight = transform.GetComponentInChildren<Light>();
        SetTrifficLightColor();

    }

    // Update is called once per frame
    void Update()
    {
        SetTrifficLightColor();
    }
}
