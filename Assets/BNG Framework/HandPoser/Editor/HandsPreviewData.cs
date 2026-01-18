using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace BNG
{
    [CreateAssetMenu(fileName = "HandsPreviewData", menuName = "HandPoser/HandsPreviewData")]
    public class HandsPreviewData : ScriptableObject
    {
        private static HandsPreviewData _instance;
        public static HandsPreviewData Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDatabase.FindAssets("t:" + nameof(ScriptableObject))
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<HandsPreviewData>)
                        .FirstOrDefault(data => data != null);
                }

                return _instance;
            }
        }

        [SerializeField] private GameObject[] leftHandPreviews = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] rightHandPreviews = Array.Empty<GameObject>();

        public GameObject[] LeftHandPreviews => leftHandPreviews;
        public GameObject[] RightHandPreviews => rightHandPreviews;

        public GameObject[] GetPreviews(InteractorHandedness side)
        {
            switch (side)
            {
                case InteractorHandedness.Left: return LeftHandPreviews;
                case InteractorHandedness.Right: return RightHandPreviews;
            }

            return Array.Empty<GameObject>();
        }
    }
}