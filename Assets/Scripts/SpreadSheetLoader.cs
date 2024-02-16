using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SpreadSheetLoader : MonoBehaviour
{
    public static string GetTSVAddress(string address, string range, long sheetID)
    {
        return $"{address}/export?format=tsv&range={range}&gid={sheetID}";
    }
    //https://docs.google.com/spreadsheets/d/1aNOsNnagk81TsVBMBiK3n3BKQPhTENnhPqQx1JPFKSM/export?format=tsv&range=A2:B17&gid=0
    public readonly string ADDRESS =
        "https://docs.google.com/spreadsheets/d/1aNOsNnagk81TsVBMBiK3n3BKQPhTENnhPqQx1JPFKSM";

    public readonly string RANGE = "A2:B17";
    public readonly long SHEET_ID = 0;

    private IEnumerator LoadData()
    {
        UnityWebRequest www = UnityWebRequest.Get(GetTSVAddress(ADDRESS, RANGE, SHEET_ID));
        yield return www.SendWebRequest();
        
        Debug.Log(www.downloadHandler.text);
    }
    
    private void Start()
    {
        StartCoroutine(LoadData());
    }

}
