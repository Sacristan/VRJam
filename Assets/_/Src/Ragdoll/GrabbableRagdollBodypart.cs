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

    private Rigidbody Rigidbody => ChainBone.GameRigidbody;

    // Track per interactor
    private readonly Dictionary<IXRSelectInteractor, GrabPointData> _grabPoints = new();

    // -----------------------------
    // Joint presets (Pinned/Unpinned)
    // -----------------------------
    [System.Serializable]
    public struct JointPreset
    {
        public float spring;
        public float damper;
        public float maxForce;

        public JointPreset(float spring, float damper, float maxForce)
        {
            this.spring = spring;
            this.damper = damper;
            this.maxForce = maxForce;
        }
    }

    [Header("Joint drive presets (existing ConfigurableJoint)")]
    [SerializeField] private JointPreset pinnedPreset   = new JointPreset(1000f, 0f, 1000f);
    [SerializeField] private JointPreset unpinnedPreset = new JointPreset(25f,   0f, 25f);

    [Tooltip("If true, we set Unpinned preset while at least one hand holds this bodypart.")]
    [SerializeField] private bool useUnpinnedWhileGrabbed = true;

    // -----------------------------
    // Optional force-based grab assist
    // -----------------------------
    [Header("Grab assist (optional PD force, uses Rigidbody)")]
    [SerializeField] private bool useGrabAssistForces = true;

    [SerializeField] private float grabPosSpring = 35f;
    [SerializeField] private float grabPosDamper = 10f;
    [SerializeField] private float grabRotSpring = 25f;
    [SerializeField] private float grabRotDamper = 3f;
    [SerializeField] private float maxVelChange = 10f;    // clamp per FixedUpdate
    [SerializeField] private float maxAngVelChange = 8f;  // clamp per FixedUpdate

    private void FixedUpdate()
    {
        if (!useGrabAssistForces) return;
        if (_grabPoints.Count == 0) return;
        
        if (Rigidbody == null || Rigidbody.isKinematic) return;

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

        // Default state = pinned (your zombie pinned preset)
        ApplyJointPreset(pinnedPreset);
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        Debug.Log($"{nameof(OnSelectEntered)}", gameObject);

        if (!XRPlayer.Instance.Hands.FindHandWithInteractor(args.interactorObject, out XRPlayerHand hand))
            return;

        // Switch to unpinned as soon as at least one hand grabs
        if (useUnpinnedWhileGrabbed)
            ApplyJointPreset(unpinnedPreset);

        AttachHandToBone(hand, out Vector3 attachPos);
        SetupGrabPoint(args.interactorObject, attachPos);

        _ragdoll.OnGrabbed(this);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        Debug.Log($"{nameof(OnSelectExited)}", gameObject);

        DestroyGrabPoint(args.interactorObject);

        // Only repin when LAST interactor releases (multi-hand safe)
        if (_grabPoints.Count == 0)
        {
            if (useUnpinnedWhileGrabbed)
                ApplyJointPreset(pinnedPreset);
        }

        if (!Selected) // can still be selected with 2nd hand
        {
            _ragdoll.ReleaseThisBodypart(this);
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

    // -------------------------------------------------------
    // Joint drive swapping (this is the "Pinned/Unpinned" core)
    // -------------------------------------------------------
    private void ApplyJointPreset(JointPreset preset)
    {
        var j = ChainBone?.Joint;
        if (j == null) return;

        JointDrive d = j.slerpDrive;
        d.positionSpring = preset.spring;
        d.positionDamper = preset.damper;
        d.maximumForce = preset.maxForce;
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
