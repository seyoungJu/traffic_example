using System.Collections;
using System.Collections.Generic;
using TrafficSimulation;
using UnityEngine;

public class TrafficHeadQuater : MonoBehaviour
{
    //0
    public float segDetectThresh = 0.1f;
    public float wayPointSize = 0.5f;

    public string[] collisionLayers;

    //1
    public List<TrafficSegment> segments = new List<TrafficSegment>();
    public TrafficSegment curSegment = null;
    public List<TrafficIntersection> intersections = new List<TrafficIntersection>();


    //2
    public List<TrafficWaypoint> GetAllWaypoints()
    {
        List<TrafficWaypoint> points = new List<TrafficWaypoint>();

        foreach (var segment in segments)
        {
            points.AddRange(segment.waypoints);
        }

        return points;
    }

    public enum ArrowDraw
    {
        FixedCount,
        ByLength,
        Off
    }

    public bool hideGizmos = false;
    public ArrowDraw arrowDrawType = ArrowDraw.ByLength;
    public int arrowCount = 1;
    public float arrowDistance = 5f;
    public float arrowSizeWaypoint = 1;
    public float arrowSizeIntersection = 0.5f;
    
}