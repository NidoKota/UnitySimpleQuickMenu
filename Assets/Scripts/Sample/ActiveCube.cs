using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// キューブの有効/無効を切り替える
/// </summary>
public class ActiveCube : MonoBehaviour
{
    public void Active(GameObject cube)
    {
        if (cube.activeSelf)
        {
            cube.SetActive(false);
        }
        else
        {
            cube.SetActive(true);
        }
    }
}
