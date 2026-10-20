
using UnityEngine;

public class FloatingArrow : MonoBehaviour
{
    [Header("Floating Settings")]
    public bool enableFloating = true;

    public float floatHeight = 0.2f;
    public float floatSpeed = 2f;

    [Header("Rotation Settings")]
    public bool rotateArrow = true;

    public float rotationSpeed = 50f;

    [Header("Rotation Axis")]
    public Vector3 rotationAxis = Vector3.up;

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;

    private void OnEnable()
    {
        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        // FLOATING
        // Only changes the vertical position.
        if (enableFloating)
        {
            float offset =
                Mathf.Sin(Time.time * floatSpeed) *
                floatHeight;

            transform.localPosition =
                startLocalPosition +
                Vector3.up * offset;
        }

        // ROTATION
        // Rotate around the object's own local axis.
        if (rotateArrow)
        {
            transform.Rotate(
                rotationAxis.normalized,
                rotationSpeed * Time.deltaTime,
                Space.Self
            );
        }
    }
}
