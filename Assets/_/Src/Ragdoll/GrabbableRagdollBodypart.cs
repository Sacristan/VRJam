using System.Collections.Generic;
using FIMSpace.FProceduralAnimation;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GrabPointData
{
    public readonly Transform RigidbodyPoint;
    public readonly Transform InteractorOffsetPoint;

    public Vector3 lastPos;
    public Vector3 prevVel;
    public bool hasLast;

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
    public RagdollChainBone ChainBone { get; private set; }
    private bool IsSelected => isSelected;
    private Rigidbody Rigidbody => ChainBone.GameRigidbody;
    private readonly Dictionary<IXRSelectInteractor, GrabPointData> _grabPoints = new();

    [Tooltip("If true, we set Unpinned preset while at least one hand holds this bodypart.")]
    private bool useUnpinnedWhileGrabbed = true;

    private GrabbableRagdollConfig.JointPreset pinnedPreset = new(1000f, 20f, 1000f);
    private GrabbableRagdollConfig.JointPreset unpinnedPreset = new(25f, 200f, 25f);

    private float grabPosSpring = 35f;
    private float grabPosDamper = 10f;
    private float grabRotSpring = 25f;
    private float grabRotDamper = 3f;
    private float maxVelChange = 10f;
    private float maxAngVelChange = 8f;

    private void FixedUpdate()
    {
        if (_grabPoints.Count == 0) return;

        if (Rigidbody == null || Rigidbody.isKinematic) return;
        HandleGrabbedJoints();
    }

    // void HandleThrowing()
    // {
    //     float dt = Time.fixedDeltaTime;
    //
    //     foreach (var kv in _grabPoints)
    //     {
    //         var gp = kv.Value;
    //         if (gp?.InteractorOffsetPoint == null) continue;
    //
    //         var p = gp.InteractorOffsetPoint.position;
    //         var r = gp.InteractorOffsetPoint.rotation;
    //
    //         if (gp.hasLast && dt > 0f)
    //         {
    //             gp.linVel = (p - gp.lastPos) / dt;
    //
    //             // angular velocity from delta rotation
    //             Quaternion dq = r * Quaternion.Inverse(gp.lastRot);
    //             dq.ToAngleAxis(out float angleDeg, out Vector3 axis);
    //             if (angleDeg > 180f) angleDeg -= 360f;
    //
    //             if (!float.IsNaN(axis.x) && axis.sqrMagnitude > 0.0001f)
    //                 gp.angVel = axis.normalized * (angleDeg * Mathf.Deg2Rad / dt);
    //             else
    //                 gp.angVel = Vector3.zero;
    //         }
    //         else
    //         {
    //             gp.linVel = Vector3.zero;
    //             gp.angVel = Vector3.zero;
    //             gp.hasLast = true;
    //         }
    //
    //         gp.lastPos = p;
    //         gp.lastRot = r;
    //     }
    // }

    void HandleGrabbedJoints()
    {
        // 1) Build a single target from all grabbing hands (average)
        Vector3 targetPos = Vector3.zero;
        Quaternion targetRot = Quaternion.identity;
        int n = 0;

        foreach (var kv in _grabPoints)
        {
            var gp = kv.Value;
            if (gp?.InteractorOffsetPoint == null) continue;

            targetPos += gp.InteractorOffsetPoint.position;

            if (n == 0) targetRot = gp.InteractorOffsetPoint.rotation;
            else targetRot = Quaternion.Slerp(targetRot, gp.InteractorOffsetPoint.rotation, 1f / (n + 1));

            n++;
        }

        if (n == 0) return;
        targetPos /= n;

        float dt = Time.fixedDeltaTime;

        // 2) Position PD -> velocity change
        Vector3 posError = targetPos - Rigidbody.worldCenterOfMass;
        Vector3 desiredVel = posError * grabPosSpring;
        Vector3 velError = desiredVel - Rigidbody.linearVelocity;

        Vector3 velChange = velError * (grabPosDamper * dt);
        velChange = Vector3.ClampMagnitude(velChange, maxVelChange);
        Rigidbody.AddForce(velChange, ForceMode.VelocityChange);

        Quaternion qError = targetRot * Quaternion.Inverse(Rigidbody.rotation);
        qError.ToAngleAxis(out float angleDeg, out Vector3 axis);
        if (angleDeg > 180f) angleDeg -= 360f;

        if (!float.IsNaN(axis.x) && axis.sqrMagnitude > 0.0001f)
        {
            axis.Normalize();
            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector3 desiredAngVel = axis * (angleRad * grabRotSpring);
            Vector3 angVelError = desiredAngVel - Rigidbody.angularVelocity;

            Vector3 angVelChange = angVelError * (grabRotDamper * dt);
            angVelChange = Vector3.ClampMagnitude(angVelChange, maxAngVelChange);
            Rigidbody.AddTorque(angVelChange, ForceMode.VelocityChange);
        }
    }


    public void Init(GrabbableRagdoll ragdoll, RagdollChainBone chainBone)
    {
        _ragdoll = ragdoll;
        ChainBone = chainBone;

        // ApplyConfigValues(ragdoll.Config.GetConfigForBone(BoneID));
        colliders.Clear();
        foreach (var t in chainBone.Colliders)
            colliders.Add(t.GameCollider);

        _ragdollBone = GetComponentInChildren<RA2BoneCollisionHandler>();

        selectMode = InteractableSelectMode.Multiple;

        if (_ragdollBone is null)
        {
            Debug.LogError(
                $"Cannot find a {nameof(RA2BoneCollisionHandler)} component for ragdoll dummy bone: {name}", this);
        }

        ApplyPinned();
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        Debug.Log($"{nameof(OnSelectEntered)} {Rigidbody.name}", gameObject);

        if (!XRPlayer.Instance.Hands.FindHandWithInteractor(args.interactorObject, out XRPlayerHand hand))
            return;

        if (useUnpinnedWhileGrabbed)
        {
            ApplyUnpinned();
        }

        AttachHandToBone(hand, out Vector3 attachPos);
        SetupGrabPoint(args.interactorObject, attachPos);

        _ragdoll.OnGrabbed(this);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        Debug.Log($"{nameof(OnSelectExited)}  {Rigidbody.name}", gameObject);

        DestroyGrabPoint(args.interactorObject);

        if (!IsSelected)
        {
            _ragdoll.ReleaseThisBodypart(this);

            if (XRPlayer.Instance.Hands.FindHandWithInteractor(args.interactorObject, out XRPlayerHand hand))
            {
                Vector3 vel = hand.VelocityTracker.GetAveragedVelocity();
                _ragdoll.ThrowRagdoll(vel);
            }

            ApplyPinned();
        }

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
            return;

        GameObject rigidbodyGrabPoint = new GameObject("RigidbodyGrabPoint-TEMP");
        Transform rigidbodyPointTransform = rigidbodyGrabPoint.transform;
        rigidbodyPointTransform.SetParent(ChainBone.SourceBone);
        rigidbodyPointTransform.position = attachPosition;

        GameObject interactorOffsetPoint = new GameObject("InteractorOffsetPoint-TEMP");
        Transform interactorPointTransform = interactorOffsetPoint.transform;
        interactorPointTransform.SetParent(interactorTransform);
        interactorPointTransform.position = attachPosition;

        _grabPoints[interactor] = new GrabPointData(rigidbodyPointTransform, interactorPointTransform);
    }

    private void DestroyGrabPoint(IXRSelectInteractor interactor)
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
        if (boneColliders.Count == 0) return handPoint;

        Vector3 closestPoint = Vector3.positiveInfinity;
        float minSqrDist = float.MaxValue;

        for (int i = 0, iSize = boneColliders.Count; i < iSize; i++)
        {
            Vector3 p = boneColliders[i].GameCollider.ClosestPoint(handPoint);
            float d = (handPoint - p).sqrMagnitude;
            if (d < minSqrDist)
            {
                minSqrDist = d;
                closestPoint = p;
            }
        }

        return closestPoint;
    }

    void ApplyPinned()
    {
        ApplyJointPreset(pinnedPreset, Ragdoll.PinnedMultiplier);
    }

    void ApplyUnpinned()
    {
        ApplyJointPreset(unpinnedPreset);
    }

    private void ApplyJointPreset(GrabbableRagdollConfig.JointPreset preset, float multiplier = 1f)
    {
        var j = ChainBone?.Joint;
        if (j == null) return;

        JointDrive d = j.slerpDrive;
        d.positionSpring = preset.spring * multiplier;
        d.positionDamper = preset.damper;
        d.maximumForce = preset.maxForce * multiplier;
        j.slerpDrive = d;
    }

    public static void ForceDrop(XRBaseInteractor baseInteractor)
    {
        ForceDrop(baseInteractor.interactionManager, baseInteractor);
    }

    public static void ForceDrop(XRInteractionManager interactionManager, IXRSelectInteractor interactor)
    {
        interactionManager.CancelInteractorSelection(interactor);
    }
}