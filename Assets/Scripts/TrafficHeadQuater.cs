using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

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


    //3
    public TMPro.TextMeshProUGUI stateText;
    public SpreadSheetLoader dataLoader;
    private TrafficData trafficData;
    public class EmergencyData
    {
        public int ID = -1;
        public bool IsEmergency = false;
        public EmergencyData(string id, string emergency)
        {
            ID = int.Parse(id);
            IsEmergency = emergency.Contains("1");
        }
    }

    public class TrafficData
    {
        public List<EmergencyData> datas = new List<EmergencyData>();
    }

    private void Start()
    {
        dataLoader = GetComponentInChildren<SpreadSheetLoader>();
        stateText = GameObject.FindWithTag("Player").GetComponent<TextMeshProUGUI>();
        InvokeRepeating("CallLoader", 5f, 5f);
    }

    private void CallLoader()
    {
        string loadedData = dataLoader.StartLoader();
        stateText.text = "Traffic Status\n" + loadedData;

        if(string.IsNullOrEmpty(loadedData))
        {
            return;
        }

        trafficData = new TrafficData();
        string[] AllRow = loadedData.Split('\n');
        foreach (string oneRow in AllRow)
        {
            string[] datas = oneRow.Split('\t');
            //Debug.Log($"id: {datas[0]} / IsEmergency : {datas[1]}");

            EmergencyData emergencyData = new EmergencyData(datas[0], datas[1]);
            trafficData.datas.Add(emergencyData);
        }

        CheckData();
    }

    private void CheckData()
    {
        for(int i =0; i< trafficData.datas.Count; i++)
        {
            EmergencyData data = trafficData.datas[i];
            if(intersections.Count <= i || intersections[i] == null)
            {
                return;
            }
            if(data.IsEmergency == true)
            {
                intersections[i].intersectionType = IntersectionType.EMERGENCY;
            }
            else
            {
                intersections[i].intersectionType = IntersectionType.TRAFFIC_LIGHT;
            }
            
        }
    }


    

}