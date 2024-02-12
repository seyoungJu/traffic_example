using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficSegment : MonoBehaviour
{
    public List<TrafficSegment> nextSegments;

    [HideInInspector]
    public int ID;

    public List<TrafficWaypoint> waypoints;


    public bool IsOnSegment(Vector3 pos)
    {
        TrafficHeadQuater trafficHeadQuater = GetComponentInParent<TrafficHeadQuater>();

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 pos1 = waypoints[i].transform.position;
            Vector3 pos2 = waypoints[i + 1].transform.position;
            float d1 = Vector3.Distance(pos1, pos);
            float d2 = Vector3.Distance(pos2, pos);
            float d3 = Vector3.Distance(pos1, pos2);
            float a = (d1 + d2) - d3;
            if (a < trafficHeadQuater.segDetectThresh && a > -trafficHeadQuater.segDetectThresh)
            {
                return true;
            }
        }

        return false;
    }
}
