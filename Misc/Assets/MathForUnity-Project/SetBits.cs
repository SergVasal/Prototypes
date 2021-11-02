using System;
using UnityEngine;

public class SetBits : MonoBehaviour
{
    private int bitBoard = 15;

    void Start()
    {
        int count = 0;
        int bb = bitBoard;

        while (bb != 0)
        {
            bb &= bb - 1;
            count++;
        }

        Debug.Log($"Count: {count}");
    }
}
