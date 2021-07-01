using UnityEngine;
using Zenject;

public class TestMono : MonoBehaviour
{
    [Inject]
    private string injectedString;

    void Start()
    {
        Debug.Log($"Start TestMono: {injectedString}");
    }
}
