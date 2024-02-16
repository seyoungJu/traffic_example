using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficWaypoint : MonoBehaviour
{
    [HideInInspector] public TrafficSegment segment;

    public void RemoveCollider()
    {
        Debug.Log("Remove Collider");
        if (GetComponent<SphereCollider>())
        {
            DestroyImmediate(gameObject.GetComponent<SphereCollider>());
        }
    }
    
    public void Refresh(int newID, TrafficSegment newSegment)
    {
        this.segment = newSegment;
        name = "Waypoint-" + newID.ToString();
        tag = "Waypoint";

        gameObject.layer = LayerMask.NameToLayer("Default");
        
        RemoveCollider();
        
    }


    public Vector3 GetVisualPos()
    {
        return transform.position + new Vector3(0, 0.5f, 0f);
    }
    
    
    

}
