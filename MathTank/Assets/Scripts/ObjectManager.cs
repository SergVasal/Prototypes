using UnityEngine;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public class ObjectManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject objectPrefab;

        private void Start()
        {
            var obj = Instantiate(objectPrefab, new Vector3(Random.Range(-100, 100), Random.Range(-100, 100), objectPrefab.transform.position.z), Quaternion.identity);
            Debug.Log($"Fuel location: {obj.transform.position}");
        }
    }
}