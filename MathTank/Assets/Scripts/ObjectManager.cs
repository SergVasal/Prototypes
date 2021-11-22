using UnityEngine;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public class ObjectManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject objectPrefab;

        public Vector3 ObjPosition { get; private set; }

        private void Awake()
        {
            var obj = Instantiate(objectPrefab, new Vector3(Random.Range(-100, 100), Random.Range(-100, 100), objectPrefab.transform.position.z), Quaternion.identity);
            var position = obj.transform.position;
            //Debug.Log($"Fuel location: {position}");
            ObjPosition = position;
        }
    }
}