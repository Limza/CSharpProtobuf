using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Google.Protobuf;

public class Test : MonoBehaviour
{
    void Start()
    {
        NetworkMgr.GetInstance.Init("77777", "patitest");
    }

    float count = 0;
    void Update()
    {
        count += Time.deltaTime;
        if (count >= 1.5f)
        {
            Debug.Log("a");
            count = 0;
        }
    }
}
