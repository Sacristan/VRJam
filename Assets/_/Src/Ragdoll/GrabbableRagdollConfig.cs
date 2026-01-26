using System;
using BNG;
using FIMSpace.FProceduralAnimation;
using UnityEngine;

[CreateAssetMenu(fileName = "GrabbableRagdollConfig", menuName = "Ragdoll/GrabbableRagdollConfig")]
public class GrabbableRagdollConfig : ScriptableObject
{
    [Serializable]
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

}