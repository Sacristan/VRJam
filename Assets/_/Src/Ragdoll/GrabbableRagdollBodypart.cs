using System.Collections.Generic;
using FIMSpace.FProceduralAnimation;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GrabPointData
{
    public readonly Transform RigidbodyPoint;
    public Transform InteractorOffsetPoint;

    public GrabPointData(Transform rigidbodyPoint, Transform interactorOffsetPoint)
    {
        RigidbodyPoint = rigidbodyPoint;
        InteractorOffsetPoint = interactorOffsetPoint;
    }

    public void DoDestroy()
    {
        Object.Destroy(RigidbodyPoint.gameObject);
        Object.Destroy(InteractorOffsetPoint.gameObject);
    }
}

public class GrabbableRagdollBodypart : XRBaseInteractable
{
    [SerializeField] private float detachDist = 1f;

    private GrabbableRagdoll _ragdoll;
    private RA2BoneCollisionHandler _ragdollBone;

    public GrabbableRagdoll Ragdoll => _ragdoll;
    public RA2BoneCollisionHandler RagdollBone => _ragdollBone;
    public RagdollChainBone ChainBone { get; private set; }
    public bool Selected => isSelected;
    public ERagdollBoneID BoneID => ChainBone.BoneID;

    private List<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor> _currentInteractors = new();
    private readonly Dictionary<IXRInteractor, GrabPointData> _grabPoints = new();

    [Header("Grab drive (force-based, uses existing joints)")]
    private float grabPosSpring = 35f;
    private float grabPosDamper = 10f;
    private float grabRotSpring = 25f;
    private float grabRotDamper = 3f;
    private float maxVelChange = 10f; // clamp per FixedUpdate
    private float maxAngVelChange = 8f; // clamp per FixedUpdate

    private void FixedUpdate()
    {
        if (_grabPoints.Count == 0) return;

        var rb = ChainBone.GameRigidbody;
        if (rb == null || rb.isKinematic) return;

        // 1) Build a single target from all grabbing hands (average)
        Vector3 targetPos = Vector3.zero;
        Quaternion targetRot = Quaternion.identity;
        int n = 0;

        foreach (var kv in _grabPoints)
        {
            var gp = kv.Value;
            if (gp?.InteractorOffsetPoint == null) continue;

            targetPos += gp.InteractorOffsetPoint.position;

            // average rotation (cheap): just take first, or do slerp accumulation
            if (n == 0) targetRot = gp.InteractorOffsetPoint.rotation;
            else targetRot = Quaternion.Slerp(targetRot, gp.InteractorOffsetPoint.rotation, 1f / (n + 1));

            n++;
        }

        if (n == 0) return;

        targetPos /= n;

        // 2) Position PD -> velocity change
        float dt = Time.fixedDeltaTime;
        Vector3 posError = targetPos - rb.worldCenterOfMass;
        Vector3 desiredVel = (posError * grabPosSpring);
        Vector3 velError = desiredVel - rb.linearVelocity;

        Vector3 velChange = velError * grabPosDamper * dt;
        velChange = Vector3.ClampMagnitude(velChange, maxVelChange);

        rb.AddForce(velChange, ForceMode.VelocityChange);

        // 3) Rotation PD -> angular velocity change
        Quaternion qError = targetRot * Quaternion.Inverse(rb.rotation);
        qError.ToAngleAxis(out float angleDeg, out Vector3 axis);
        if (angleDeg > 180f) angleDeg -= 360f;

        if (!float.IsNaN(axis.x) && axis.sqrMagnitude > 0.0001f)
        {
            axis.Normalize();
            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector3 desiredAngVel = axis * (angleRad * grabRotSpring);
            Vector3 angVelError = desiredAngVel - rb.angularVelocity;

            Vector3 angVelChange = angVelError * grabRotDamper * dt;
            angVelChange = Vector3.ClampMagnitude(angVelChange, maxAngVelChange);

            rb.AddTorque(angVelChange, ForceMode.VelocityChange);
        }
    }

    public void Init(GrabbableRagdoll ragdoll, RagdollChainBone chainBone)
    {
        _ragdoll = ragdoll;
        ChainBone = chainBone;

        ApplyConfigValues(ragdoll.Config.GetConfigForBone(BoneID));

        colliders.Clear();
        foreach (var t in chainBone.Colliders)
        {
            colliders.Add(t.GameCollider);
        }

        _ragdollBone = GetComponentInChildren<RA2BoneCollisionHandler>();

        selectMode = InteractableSelectMode.Multiple;

        if (_ragdollBone is null)
        {
            Debug.LogError(
                $"Cannot find a {nameof(RA2BoneCollisionHandler)} component for ragdoll dummy bone: {name}", this);
        }
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        Debug.Log($"{nameof(OnSelectEntered)}", gameObject);

        bool handFound = XRPlayer.Instance.Hands.FindHandWithInteractor(
            args.interactorObject, out XRPlayerHand hand
        );
        if (!handFound) return;

        SetJointLoose(true);
        
        AttachHandToBone(hand, out Vector3 attachPos);
        SetupGrabPoint(args.interactorObject, attachPos);
        _ragdoll.OnGrabbed(this);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        Debug.Log($"{nameof(OnSelectExited)}", gameObject);

        DestroyGrabPoint(args.interactorObject);

        if (!Selected) //can still be selected with 2nd hand
        {
            _ragdoll.ReleaseThisBodypart(this);
        }
        
        SetJointLoose(false);
        
        ForceDrop((XRBaseInteractor)args.interactorObject);
        _ragdoll.OnReleased(this);
    }

    private void AttachHandToBone(XRPlayerHand hand, out Vector3 attachPosition)
    {
        HandGraphics handGraphics = hand.PoseController.HandGraphics;
        handGraphics.SetParent(ChainBone.SourceBone);

        attachPosition = FindClosestColliderPoint(handGraphics.Center.position);
        handGraphics.MoveCenterTo(attachPosition);
    }

    private void SetupGrabPoint(IXRSelectInteractor interactor, Vector3 attachPosition)
    {
        if (!interactor.TryGetTransform(out Transform interactorTransform))
        {
            return;
        }

        GameObject rigidbodyGrabPoint = new GameObject("RigidbodyGrabPoint-TEMP");
        Transform rigidbodyPointTransform = rigidbodyGrabPoint.transform;
        rigidbodyPointTransform.SetParent(ChainBone.SourceBone);
        rigidbodyPointTransform.position = attachPosition;

        GameObject interactorOffsetPoint = new GameObject("InteractorOffsetPoint-TEMP");
        Transform interactorPointTransform = interactorOffsetPoint.transform;
        interactorPointTransform.SetParent(interactorTransform);
        interactorPointTransform.position = attachPosition;

        _grabPoints.Add(interactor, new GrabPointData(rigidbodyPointTransform, interactorPointTransform));
    }

    private void DestroyGrabPoint(IXRInteractor interactor)
    {
        if (_grabPoints.TryGetValue(interactor, out GrabPointData grabPoint))
        {
            grabPoint.DoDestroy();
            _grabPoints.Remove(interactor);
        }
    }

    private Vector3 FindClosestColliderPoint(Vector3 handPoint)
    {
        List<RagdollChainBone.ColliderSetup> boneColliders = ChainBone.Colliders;
        if (boneColliders.Count == 0)
        {
            return handPoint;
        }

        Vector3 closestPoint = Vector3.positiveInfinity;
        float minSqrDist = float.MaxValue;

        for (int i = 0, iSize = boneColliders.Count; i < iSize; i++)
        {
            Vector3 closestPointOnCollider = boneColliders[i].GameCollider.ClosestPoint(handPoint);
            float sqrDis = Vector3.SqrMagnitude(handPoint - closestPointOnCollider);
            if (sqrDis < minSqrDist)
            {
                minSqrDist = sqrDis;
                closestPoint = closestPointOnCollider;
            }
        }

        return closestPoint;
    }

    public void ApplyConfigValues(GrabbableRagdollConfig.GrabbableRagdollBoneConfig config)
    {
        if (config.overrideSpring)
        {
            ConfigurableJoint boneJoint = ChainBone.Joint;

            JointDrive slerpDrive = boneJoint.slerpDrive;

            slerpDrive.positionSpring = config.driveSpring;
            slerpDrive.maximumForce = config.driveSpring;
            slerpDrive.positionDamper = config.driveDamper;

            boneJoint.slerpDrive = slerpDrive;
        }
    }

    public static void ForceDrop(XRBaseInteractor baseInteractor)
    {
        ForceDrop(baseInteractor.interactionManager, baseInteractor);
    }

    public static void ForceDrop(XRInteractionManager interactionManager, IXRSelectInteractor interactor)
    {
        interactionManager.CancelInteractorSelection(interactor);
    }

    private JointDrive _origDrive;
    private bool _hasOrigDrive;

    private void SetJointLoose(bool loose)
    {
        var j = ChainBone.Joint;
        if (j == null) return;

        if (!_hasOrigDrive)
        {
            _origDrive = j.slerpDrive;
            _hasOrigDrive = true;
        }

        var d = j.slerpDrive;

        if (loose)
        {
            d.positionSpring = _origDrive.positionSpring * 0.15f;
            d.positionDamper = _origDrive.positionDamper * 0.25f;
            d.maximumForce = _origDrive.maximumForce; // keep force cap or reduce, your call
        }
        else
        {
            d = _origDrive;
        }

        j.slerpDrive = d;
    }
}