using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

/// <summary>
/// Continuous move provider that blends head/hand forward sources like DynamicMoveProvider,
/// and contributes to the comfort vignette while there is nonzero move input.
/// </summary>
public class DynamicMoveProviderHVR : ContinuousMoveProvider
{
    public enum MovementDirection
    {
        HeadRelative,
        HandRelative
    }

    [Space, Header("Movement Direction")] [SerializeField]
    Transform m_HeadTransform;

    public Transform headTransform
    {
        get => m_HeadTransform;
        set => m_HeadTransform = value;
    }

    [SerializeField] Transform m_LeftControllerTransform;

    public Transform leftControllerTransform
    {
        get => m_LeftControllerTransform;
        set => m_LeftControllerTransform = value;
    }

    [SerializeField] Transform m_RightControllerTransform;

    public Transform rightControllerTransform
    {
        get => m_RightControllerTransform;
        set => m_RightControllerTransform = value;
    }

    [SerializeField] MovementDirection m_LeftHandMovementDirection = MovementDirection.HeadRelative;

    public MovementDirection leftHandMovementDirection
    {
        get => m_LeftHandMovementDirection;
        set => m_LeftHandMovementDirection = value;
    }

    [SerializeField] MovementDirection m_RightHandMovementDirection = MovementDirection.HeadRelative;

    public MovementDirection rightHandMovementDirection
    {
        get => m_RightHandMovementDirection;
        set => m_RightHandMovementDirection = value;
    }

    private Transform m_CombinedTransform;
    private Pose m_LeftMovementPose = Pose.identity;
    private Pose m_RightMovementPose = Pose.identity;

    // ---- Comfort vignette additions ----
    [Header("Comfort Vignette")] [SerializeField, Tooltip("0 = linear. >1 = slow start; <1 = quick ramp.")]
    private float rampExponent = 1.2f;

    [Range(0, 1), SerializeField, Tooltip("Minimum target while any non-zero input is present.")]
    private float minWhenMoving = 0.05f;

    [Range(0, 1), SerializeField, Tooltip("Ignore tiny stick noise below this before showing vignette.")]
    private float displayDeadzone = 0.05f;

    private int _vigHandle = -1;
    private float _lastSent = -1f; // last normalized value sent to vignette
    private const float SEND_EPS = 0.01f; // threshold to avoid redundant updates

    protected override void Awake()
    {
        base.Awake();
        m_CombinedTransform = new GameObject("[Dynamic Move Provider] Combined Forward Source").transform;
        m_CombinedTransform.SetParent(transform, false);
        m_CombinedTransform.localPosition = Vector3.zero;
        m_CombinedTransform.localRotation = Quaternion.identity;
        forwardSource = m_CombinedTransform;
    }

    protected new void OnEnable()
    {
        base.OnEnable();
        _lastSent = 0f;
    }

    protected new void OnDisable()
    {
        base.OnDisable();
    }

    protected override Vector3 ComputeDesiredMove(Vector2 input)
    {
        if (input == Vector2.zero)
            return Vector3.zero;

        if (m_HeadTransform == null)
        {
            XROrigin xrOrigin = mediator.xrOrigin;
            if (xrOrigin != null && xrOrigin.Camera != null)
                m_HeadTransform = xrOrigin.Camera.transform;
        }

        switch (m_LeftHandMovementDirection)
        {
            case MovementDirection.HeadRelative:
                if (m_HeadTransform != null)
                    m_LeftMovementPose = m_HeadTransform.GetWorldPose();
                break;
            case MovementDirection.HandRelative:
                if (m_LeftControllerTransform != null)
                    m_LeftMovementPose = m_LeftControllerTransform.GetWorldPose();
                break;
            default:
                Assert.IsTrue(false, $"Unhandled {nameof(MovementDirection)}={m_LeftHandMovementDirection}");
                break;
        }

        switch (m_RightHandMovementDirection)
        {
            case MovementDirection.HeadRelative:
                if (m_HeadTransform != null)
                    m_RightMovementPose = m_HeadTransform.GetWorldPose();
                break;
            case MovementDirection.HandRelative:
                if (m_RightControllerTransform != null)
                    m_RightMovementPose = m_RightControllerTransform.GetWorldPose();
                break;
            default:
                Assert.IsTrue(false, $"Unhandled {nameof(MovementDirection)}={m_RightHandMovementDirection}");
                break;
        }

        Vector2 leftHandValue = leftHandMoveInput.ReadValue();
        Vector2 rightHandValue = rightHandMoveInput.ReadValue();

        float totalSqrMagnitude = leftHandValue.sqrMagnitude + rightHandValue.sqrMagnitude;
        float leftHandBlend = 0.5f;
        if (totalSqrMagnitude > Mathf.Epsilon)
            leftHandBlend = leftHandValue.sqrMagnitude / totalSqrMagnitude;

        Vector3 combinedPosition =
            Vector3.Lerp(m_RightMovementPose.position, m_LeftMovementPose.position, leftHandBlend);
        Quaternion combinedRotation =
            Quaternion.Slerp(m_RightMovementPose.rotation, m_LeftMovementPose.rotation, leftHandBlend);
        m_CombinedTransform.SetPositionAndRotation(combinedPosition, combinedRotation);

        return base.ComputeDesiredMove(input);
    }

    private void UpdateVignetting(Vector2 input)
    {
        // --- Vignette contribution (only when it meaningfully changes) ---
        if (_vigHandle >= 0)
        {
            float normalized = 0f;
            float mag = input.magnitude;

            if (mag > displayDeadzone)
            {
                float t = Mathf.InverseLerp(displayDeadzone, 1f, Mathf.Clamp01(mag));
                float shaped = Mathf.Pow(t, Mathf.Max(0.0001f, rampExponent));
                normalized = Mathf.Lerp(minWhenMoving, 1f, shaped);
            }

            if (Mathf.Abs(normalized - _lastSent) > SEND_EPS)
            {
                _lastSent = normalized;
            }
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (rampExponent < 0.0001f) rampExponent = 0.0001f;
    }
#endif
}