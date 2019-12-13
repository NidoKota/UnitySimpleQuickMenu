using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// キューブを動かす
/// </summary>
public class MoveCube : MonoBehaviour
{
    public float length;

    void Start()
    {
        transform.position = new Vector3(0, transform.position.y, transform.position.z);
    }

    void Update()
    {
        //Time.timeScaleが0の時、動作が停止する
        transform.position = new Vector3(Mathf.PingPong(Time.time, length) - (length / 2), transform.position.y, transform.position.z);
    }
}
