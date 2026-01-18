using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace BNG
{
    [HelpURL("https://wiki.beardedninjagames.com/en/General/HandPoses")]
    public partial class GrabPoint : MonoBehaviour
    {
        [Header("Pose Type")]
        [SerializeField] private PoseType poseType = PoseType.HandPose;

        [Header("Hand Pose Properties")]
        [Tooltip("Set this HandPose on the HandPoser when PoseType is set to 'HandPose'")] [SerializeField]
        private HandPose equipHandPose;

        [Header("Auto Pose Properties")]
        [Tooltip("If PoseType = AutoPoseOnce, AutoPose will be run for this many seconds")] [SerializeField]
        private float autoPoseDuration = 0.15f;

        [Header("Valid Hands")]
        [Tooltip("Can this Grab Point be used by a left-handed Grabber?")] [SerializeField]
        private bool leftHandIsValid = true;

        [Tooltip("Can this Grab Point be used by a right-handed Grabber?")] [SerializeField]
        private bool rightHandIsValid = true;

        [Tooltip("GrabPoint is not considered valid if the angle between the GrabPoint " +
                 "and Grabber is greater than this amount")]
        [Range(0.0f, 360.0f)]
        public float maxDegreeDifferenceAllowed = 360;

        [Header("Spreading")]
        [SerializeField] private bool spreadAlongAxis;
        [SerializeField] private Transform spreadAnchor;
        [SerializeField] private Vector3 spreadAxis = new Vector3(0f, 1f, 0f);
        [SerializeField] private int spreadAngleStep = 45;
        [SerializeField] private int spreadCount = 7;

        private bool _spreadPerformed;

        public PoseType PoseType => poseType;
        public HandPose EquipHandPose => equipHandPose;
        public float AutoPoseDuration => autoPoseDuration;
        public bool LeftHandIsValid => leftHandIsValid;
        public bool RightHandIsValid => rightHandIsValid;
        public float MaxDegreeDifferenceAllowed => maxDegreeDifferenceAllowed;
        public bool SpreadAlongAxis => spreadAlongAxis;

        public bool IsValidForHandSide(InteractorHandedness handSide)
        {
            if (handSide == InteractorHandedness.Left && LeftHandIsValid) return true;
            if (handSide == InteractorHandedness.Right && RightHandIsValid) return true;
            return false;
        }

        public void TryToSpread(List<GrabPoint> resultPoints)
        {
            resultPoints.Add(this);

            if (_spreadPerformed || !spreadAlongAxis)
            {
                return;
            }

            Transform thisTransform = transform;

            Vector3 spreadAnchorPos = spreadAnchor.position;
            Quaternion spreadAnchorRot = spreadAnchor.rotation;

            //create temporary rotation anchor point to avoid complex 3d math
            GameObject tempRotationAnchorGo = new GameObject("TempRotationAnchor");
            Transform tempRotationAnchor = tempRotationAnchorGo.transform;
            tempRotationAnchor.SetParent(spreadAnchor.parent);

            try
            {
                for (int i = 0, iSize = spreadCount; i < iSize; i++)
                {
                    tempRotationAnchor.SetPositionAndRotation(spreadAnchorPos, spreadAnchorRot);

                    //create new grab point under the rotation anchor
                    GrabPoint clone = Instantiate(
                        this, thisTransform.position, thisTransform.rotation,
                        tempRotationAnchor
                    );
                    clone.name = $"{name}-Spread-{i}";
                    clone.spreadAlongAxis = false;

                    //rotate anchor around local spread axis
                    Transform cloneTransform = clone.transform;
                    tempRotationAnchor.localEulerAngles += spreadAxis * (spreadAngleStep * (i + 1));

                    //move new grab point to the same parent as original grab point has
                    cloneTransform.SetParent(thisTransform.parent);

                    resultPoints.Add(clone);
                }
            }
            finally
            {
                Destroy(tempRotationAnchorGo);
            }
        }
    }
}