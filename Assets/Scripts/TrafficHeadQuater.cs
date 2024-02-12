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
    
        public enum ArrowDraw
        {
            FixedCount,
            ByLength,
            Off
        }

}
