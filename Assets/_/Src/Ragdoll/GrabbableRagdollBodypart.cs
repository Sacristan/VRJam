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
}
