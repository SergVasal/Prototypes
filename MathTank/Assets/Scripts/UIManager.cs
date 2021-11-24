using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject tank;

        [SerializeField]
        private GameObject fuel;

        [SerializeField]
        private Text tankPosition;

        [SerializeField]
        private Text fuelPosition;

        [SerializeField]
        private Text energyAmount;

        private void Start()
        {
            tankPosition.text = tank.transform.position + "";
            fuelPosition.text = fuel.GetComponent<ObjectManager>().ObjPosition + "";
        }

        public void AddEnergy(string amount)
        {
            float n;
            if (float.TryParse(amount, out n))
            {
                energyAmount.text = amount;
            }
        }

        public void SetRotation(string amount)
        {
            var rotation = float.Parse(amount) * Mathf.Deg2Rad;
            tank.transform.up = new Vector3(0f, 1f, 0f);
            tank.transform.up = HolisticMath.Rotate(new Coords(tank.transform.up), rotation, false).ToVector();
        }
    }
}