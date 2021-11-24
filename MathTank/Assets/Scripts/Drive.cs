using DefaultNamespace;
using UnityEngine;
using UnityEngine.UI;

public class Drive : MonoBehaviour
{
    private const float Speed = 5f;
    private const float StoppingDistance = 0.1f;

    [SerializeField]
    private GameObject fuel;

    [SerializeField]
    private Text energyAmt;

    private Vector3 direction;

    private void Start()
    {
        direction = fuel.transform.position - transform.position;
        Coords dirNorm = HolisticMath.GetNormal(new Coords(direction));
        direction = dirNorm.ToVector();

        //transform.up = HolisticMath.LookAt2D(new Coords(transform.up), new Coords(transform.position), new Coords(fuel.transform.position)).ToVector();
    }

    void Update()
    {
        // if (HolisticMath.Distance(new Coords(transform.position), new Coords(fuel.transform.position)) >= StoppingDistance)
        // {
        //     transform.position += direction * Speed * Time.deltaTime;
        // }

        if (float.Parse(energyAmt.text) <= 0) return;

        // Get the horizontal and vertical axis.
        // By default they are mapped to the arrow keys.
        // The value is in the range -1 to 1
        float translation = Input.GetAxis("Vertical") * Speed;

        // Make it move 10 meters per second instead of 10 meters per frame...
        translation *= Time.deltaTime;

        // Move translation along the object's z-axis
        transform.Translate(0, translation, 0);
    }
}