using UnityEngine;

public class Main : MonoBehaviour
{
    void Start()
    {
        int[,,] array3d = new int[2,3,4];

        var allLength = array3d.Length;
        Debug.Log($"allLength: {allLength}");
        var total = 1;
        for (int i = 0; i < array3d.Rank; i++)
        {
            total *= array3d.GetLength(i);
            Debug.Log($"Element count in rank {i}: {array3d.GetLength(i)} Current total: {total}");
        }

    }
}
