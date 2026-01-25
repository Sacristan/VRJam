using FIMSpace.FProceduralAnimation;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrabbableRagdollBodypartInteractable : XRBaseInteractable
{
    private GrabbableRagdoll _ragdoll;
    private RA2BoneCollisionHandler _ragdollBone;

    public GrabbableRagdoll Ragdoll => _ragdoll;
    public RA2BoneCollisionHandler RagdollBone => _ragdollBone;

    public void Init(GrabbableRagdoll ragdoll)
    {
        _ragdoll = ragdoll;

        _ragdollBone = GetComponentInChildren<RA2BoneCollisionHandler>();
        if (_ragdollBone is null)
        {
            Debug.LogError($"Cannot find a ${nameof(RA2BoneCollisionHandler)} component for ragdoll dummy bone: ${name}", this);
        }
    }
}
