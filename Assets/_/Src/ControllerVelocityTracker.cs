using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// Based on BNG.VelocityTracker
public class ControllerVelocityTracker : MonoBehaviour
{
    [Tooltip(
        "How many frames to use when averaging retrieving velocity using GetAveragedVelocity / GetAveragedAngularVelocity"
    )]
    [SerializeField]
    private float averageVelocityCount = 8;

    [SerializeField] private Transform trackingSpace;
    [SerializeField] private InputActionReference velocityInput;

    // Values used to manually track velocity
    private Vector3 _velocity;

    // Used for manual velocity tracking
    private Vector3 _lastPosition;

    private readonly List<Vector3> _previousVelocities = new();

    private void FixedUpdate()
    {
        UpdateVelocity();

        // Save our last position / rotation so we can use it for velocity calculations
        _lastPosition = transform.position;
    }

    private void UpdateVelocity()
    {
        // Update velocity based on current and previous position
        _velocity = (transform.position - _lastPosition) / Time.deltaTime;

        // Add Linear Velocity
        _previousVelocities.Add(GetVelocity());

        // Shrink list if necessary
        if (_previousVelocities.Count > averageVelocityCount)
        {
            _previousVelocities.RemoveAt(0);
        }
    }

    public Vector3 GetVelocity()
    {
        // Try XR Input Velocity First
        Vector3 vel = velocityInput.action.ReadValue<Vector3>();

        // Fall back to tracking velocity on a per frame basis if current velocity is unknown
        if (vel == Vector3.zero)
        {
            return _velocity;
        }

        // Add the playspace rotation in if necessary
        if (trackingSpace != null)
        {
            return trackingSpace.transform.rotation * vel;
        }

        return vel;
    }

    public Vector3 GetAveragedVelocity()
    {
        return GetAveragedVector(_previousVelocities);
    }

    private static Vector3 GetAveragedVector(List<Vector3> vectors)
    {
        if (vectors != null)
        {
            int count = vectors.Count;
            float x = 0;
            float y = 0;
            float z = 0;

            for (int i = 0; i < count; i++)
            {
                Vector3 v = vectors[i];
                x += v.x;
                y += v.y;
                z += v.z;
            }

            return new Vector3(x / count, y / count, z / count);
        }

        return Vector3.zero;
    }
}