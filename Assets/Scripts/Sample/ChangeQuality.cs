using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Qualityを変更する
/// </summary>
public class ChangeQuality : MonoBehaviour
{
    public void Quality(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }
}