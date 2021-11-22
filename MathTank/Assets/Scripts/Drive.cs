using DefaultNamespace;
using UnityEngine;

public class Drive : MonoBehaviour
{
    private const float Speed = 5f;
    private const float StoppingDistance = 0.1f;

    [SerializeField]
    private GameObject fuel;

    private Vector3 direction;


    private void Start()
    {
        direction = fuel.transform.position - transform.position;
        Coords dirNorm = HolisticMath.GetNormal(new Coords(direction));
        direction = dirNorm.ToVector();
        float angle = HolisticMath.Angle(new Coords(0, 1, 0), new Coords(direction)) * 180f / Mathf.PI;
        Debug.Log($"Angle to fuel: {angle}");
    }

    void Update()
    {
        if (HolisticMath.Distance(new Coords(transform.position), new Coords(fuel.transform.position)) >= StoppingDistance)
        {
            transform.position += direction * Speed * Time.deltaTime;
        }
    }
}