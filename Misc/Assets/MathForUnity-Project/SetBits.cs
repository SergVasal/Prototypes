using System;
using UnityEngine;

public class SetBits : MonoBehaviour
{
    private int bSequance = 8 + 1 + 2;

    void Start()
    {
        Debug.Log(Convert.ToString(bSequance, 2));
    }
}
