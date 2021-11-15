using UnityEngine;

namespace DefaultNamespace
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField]
        private Transform player;

        private void Update()
        {
            var playerPosition = player.transform.position;
            var cameraTransform = transform;
            cameraTransform.position = new Vector3(playerPosition.x, playerPosition.y, cameraTransform.position.z);
        }
    }
}