using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class pierTest : MonoBehaviour
{
    public Toggle tg;
    public GameObject ythis;

    void Update()
    {
        if (!tg.isOn)
        {
            ythis.SetActive(true);
        }
        else
        {
            ythis.SetActive(false);
        }
    }
}
