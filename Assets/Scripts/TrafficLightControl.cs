using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightControl : MonoBehaviour
{
    public int lightGroupID;
    public TrafficIntersection intersection;

    private Light pointLight;

    private float blink = 0f;

    void SetTrifficLightColor()
    {
        if (intersection.currentRedLightsGroup == lightGroupID)
        {
            pointLight.color = new Color(1, 0, 0);
        }
        else if(intersection.currentRedLightsGroup == 0)
        {
            blink = Mathf.Clamp01(blink + Time.deltaTime * 2f);

            pointLight.color = new Color(blink, 0, 0);
            if(blink >= 1f)
            {
                blink = 0f;
            }
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
