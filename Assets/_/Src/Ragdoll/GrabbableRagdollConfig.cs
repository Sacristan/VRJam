using System;
using BNG;
using FIMSpace.FProceduralAnimation;
using UnityEngine;

[CreateAssetMenu(fileName = "GrabbableRagdollConfig", menuName = "Ragdoll/GrabbableRagdollConfig")]
public class GrabbableRagdollConfig : ScriptableObject
{
    [Serializable]
    public class GrabbableRagdollBoneConfig
    {
        public GrabbableRagdollBoneConfig()
        {
        }

        public GrabbableRagdollBoneConfig(ERagdollBoneID boneID)
        {
            this.boneId = boneID;
        }

        [SerializeField] public ERagdollBoneID boneId = ERagdollBoneID.Unknown;

        [Header("DRAG POWER")] [SerializeField]
        public bool overrideDragPower;

        [SerializeField] public float dragPower = 1f;

        [Header("DRIVE SPRING")] [SerializeField]
        public bool overrideSpring;

        [SerializeField] public float driveSpring = 1000f;
        [SerializeField] public float driveDamper = 100f;

        [Header("STABILIZATION")] [SerializeField]
        public bool allowStabilization;

        [SerializeField] public float stabilizationDriveSpring = 200;

        [SerializeField] public float stabilizationDriveDamper = 200;

        public static GrabbableRagdollBoneConfig Default = new();
        public bool IsDefault => boneId == ERagdollBoneID.Unknown;
    }

    [Header("Grabbing")] [SerializeField] public HandPose grabHandPose;
    [SerializeField] public bool allowFallOnGrab = true;
    [SerializeField] public float grabFallDist = 0.3f;

    [Header("Collisions")] [SerializeField]
    public bool allowFallOnCollision = false;

    [SerializeField] public float collisionFallForce = 25f;
    [SerializeField] public float collisionIgnoreAfterGetUp = 1.5f;
    [SerializeField] public float collisionImpulseMultiplier = 3f;

    [Space] [SerializeField] public GrabbableRagdollBoneConfig[] boneConfigs = new[]
    {
        new GrabbableRagdollBoneConfig(ERagdollBoneID.Head),
        new GrabbableRagdollBoneConfig(ERagdollBoneID.Chest),
        new GrabbableRagdollBoneConfig(ERagdollBoneID.LeftUpperArm),
        new GrabbableRagdollBoneConfig(ERagdollBoneID.LeftLowerArm),
        new GrabbableRagdollBoneConfig(ERagdollBoneID.LeftHand),
        new GrabbableRagdollBoneConfig(ERagdollBoneID.RightUpperArm),
        new GrabbableRagdollBoneConfig(ERagdollBoneID.RightLowerArm),
        new GrabbableRagdollBoneConfig(ERagdollBoneID.RightHand),
        new GrabbableRagdollBoneConfig(ERagdollBoneID.LeftUpperLeg),
        new GrabbableRagdollBoneConfig(ERagdollBoneID.LeftLowerLeg),
        new GrabbableRagdollBoneConfig(ERagdollBoneID.LeftFoot),
        new GrabbableRagdollBoneConfig(ERagdollBoneID.RightUpperLeg),
        new GrabbableRagdollBoneConfig(ERagdollBoneID.RightLowerLeg),
        new GrabbableRagdollBoneConfig(ERagdollBoneID.RightFoot)
    };

    public GrabbableRagdollBoneConfig GetConfigForBone(ERagdollBoneID boneID)
    {
        for (int i = 0, iSize = boneConfigs.Length; i < iSize; i++)
        {
            if (boneConfigs[i].boneId == boneID)
            {
                return boneConfigs[i];
            }
        }

        return GrabbableRagdollBoneConfig.Default;
    }
}