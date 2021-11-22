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
            energyAmount.text = amount;
        }
    }
}