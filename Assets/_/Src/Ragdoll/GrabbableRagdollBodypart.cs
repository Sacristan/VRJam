using System.Collections.Generic;
using FIMSpace.FProceduralAnimation;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrabbableRagdollBodypart : XRGrabInteractable
{
    private GrabbableRagdoll _ragdoll;
    private RA2BoneCollisionHandler _ragdollBone;

    public GrabbableRagdoll Ragdoll => _ragdoll;
    public RA2BoneCollisionHandler RagdollBone => _ragdollBone;
    public RagdollChainBone ChainBone { get; private set; }
    public bool Selected => isSelected;

    public void Init(GrabbableRagdoll ragdoll, RagdollChainBone chainBone)
    {
        _ragdoll = ragdoll;

        colliders.Clear();

        List<RagdollChainBone.ColliderSetup> boneColliders = chainBone.Colliders;

        foreach (var t in boneColliders)
        {
            colliders.Add(t.GameCollider);
        }


        selectMode = InteractableSelectMode.Multiple;

        // ApplyBoneSettings();

        _ragdollBone = GetComponentInChildren<RA2BoneCollisionHandler>();
        if (_ragdollBone is null)
        {
            Debug.LogError(
                $"Cannot find a ${nameof(RA2BoneCollisionHandler)} component for ragdoll dummy bone: ${name}", this);
        }
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        Debug.Log($"{nameof(OnSelectEntered)}", gameObject);

        _ragdoll.OnGrabbed(this);
        
        Debug.Break();
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        Debug.Log($"{nameof(OnSelectExited)}", gameObject);

        _ragdoll.OnReleased(this);
    }

    void ApplyBoneSettings()
    {
        ConfigurableJoint boneJoint = ChainBone.Joint;
        // JointDrive slerpDrive = boneJoint.slerpDrive;
        // slerpDrive.positionSpring = config.driveSpring;
        // slerpDrive.positionDamper = config.driveDamper;
        // boneJoint.slerpDrive = slerpDrive;
    }
}