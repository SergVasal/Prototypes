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

        transform.up = HolisticMath.LookAt2D(new Coords(transform.up), new Coords(transform.position), new Coords(fuel.transform.position)).ToVector();
    }

    void Update()
    {
        if (HolisticMath.Distance(new Coords(transform.position), new Coords(fuel.transform.position)) >= StoppingDistance)
        {
            transform.position += direction * Speed * Time.deltaTime;
        }
    }
}