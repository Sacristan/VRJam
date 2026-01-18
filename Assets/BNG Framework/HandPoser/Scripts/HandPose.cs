using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    [HelpURL("https://wiki.beardedninjagames.com/en/General/HandPoses")]
    [System.Serializable]
    public class HandPose : ScriptableObject {

        // Used to help identify name of the hand pose
        [Header("Pose Name")]
        public string PoseName;

        [SerializeField]
        [Header("Joint Definitions")]
        public HandPoseDefinition Joints;
    }
}

