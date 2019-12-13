using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SimpleQuickMenuのCallBackを表示する
/// </summary>
public class DebugCallBack : MonoBehaviour
{
    void Start()
    {
        FindObjectOfType<SimpleQuickMenu.SimpleQuickMenu>().InvokeMenuCallBack += (x) => Debug.Log(x);
    }
}
