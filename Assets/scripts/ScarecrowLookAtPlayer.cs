
using UnityEngine;

public class ScarecrowLookAtPlayer : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Rotation")]
    public float rotationSpeed = 3f;

    private void Update()
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}
