using System.Collections.Generic;
using FIMSpace.FProceduralAnimation;
using UnityEngine;

public class GrabbableRagdollBones
{
    private readonly Dictionary<ERagdollBoneID, Bone> _bones = new();

    public void Add(ERagdollBoneID boneSetupBoneID, Bone bone) =>
        _bones.Add(boneSetupBoneID, bone);

    public Bone FindBoneByBoneID(ERagdollBoneID boneID) =>
        _bones.GetValueOrDefault(boneID);

    public Bone RootBone => FindBoneByBoneID(boneID: ERagdollBoneID.Hips);

    public class Bone
    {
        private readonly RagdollChainBone _chainBone;
        private readonly GrabbableRagdollBodypart _interactable;
        private readonly Rigidbody _rigidbody;

        public RagdollChainBone ChainBone => _chainBone;
        public GrabbableRagdollBodypart Interactable => _interactable;
        public Rigidbody RigidBody => _rigidbody;

        public Bone(RagdollChainBone chainBone, GrabbableRagdollBodypart interactable)
        {
            _chainBone = chainBone;
            _interactable = interactable;

            _rigidbody = _interactable.GetComponent<Rigidbody>();
        }
    }
}