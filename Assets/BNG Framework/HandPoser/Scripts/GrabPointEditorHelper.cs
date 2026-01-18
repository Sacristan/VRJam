#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BNG
{
    public partial class GrabPoint
    {
        [SerializeField] private GameObject leftHandPreviewPrefab;
        [SerializeField] private GameObject rightHandPreviewPrefab;

        public GameObject LeftHandPreviewPrefab
        {
            get => leftHandPreviewPrefab;
            set => leftHandPreviewPrefab = value;
        }

        public GameObject RightHandPreviewPrefab
        {
            get => rightHandPreviewPrefab;
            set => rightHandPreviewPrefab = value;
        }

        public const string LeftModelPreviewName = "LeftHandModelsEditorPreview";
        public const string RightModelPreviewName = "RightHandModelsEditorPreview";

        private Transform _leftHandPreview;
        private HandPoser _leftHandPreviewPoser;
        private AutoPoser _leftHandPreviewAutoPoser;

        private Transform _rightHandPreview;
        private HandPoser _rightHandPreviewPoser;
        private AutoPoser _rightHandPreviewAutoPoser;

        public void UpdatePreviews()
        {
            FillPreviewObjects();
            UpdateHandPosePreview();
            UpdateAutoPoserPreview();
        }

        private void FillPreviewObjects()
        {
            FillPreviewObjects(
                LeftModelPreviewName, ref _leftHandPreview, ref _leftHandPreviewPoser, ref _leftHandPreviewAutoPoser
            );
            FillPreviewObjects(
                RightModelPreviewName, ref _rightHandPreview, ref _rightHandPreviewPoser, ref _rightHandPreviewAutoPoser
            );
        }

        private void FillPreviewObjects(
            string handPreviewName,
            ref Transform handPreview, ref HandPoser handPoser, ref AutoPoser autoPoser
        )
        {
            if (handPreview == null)
            {
                handPreview = transform.Find(handPreviewName);
            }
            if (handPreview != null)
            {
                if (handPoser == null)
                {
                    handPoser = handPreview.GetComponentInChildren<HandPoser>();
                }
                if (autoPoser == null)
                {
                    autoPoser = handPreview.GetComponentInChildren<AutoPoser>();
                }
            }
        }

        private void UpdateHandPosePreview()
        {
            if (PoseType == PoseType.HandPose)
            {
                if (_leftHandPreviewPoser != null)
                {
                    _leftHandPreviewPoser.CurrentPose = EquipHandPose;
                }

                if (_rightHandPreviewPoser != null)
                {
                    _rightHandPreviewPoser.CurrentPose = EquipHandPose;
                }
            }
        }

        private void UpdateAutoPoserPreview()
        {
            if (PoseType == PoseType.AutoPoseContinuous || PoseType == PoseType.AutoPoseOnce)
            {
                if (_leftHandPreviewAutoPoser != null)
                {
                    _leftHandPreviewAutoPoser.UpdateContinuously = true;
                }

                if (_rightHandPreviewAutoPoser != null)
                {
                    _rightHandPreviewAutoPoser.UpdateContinuously = true;
                }
            }
            else
            {
                // Update in editor
                if (_leftHandPreviewAutoPoser != null)
                {
                    _leftHandPreviewAutoPoser.UpdateContinuously = false;
                }

                if (_rightHandPreviewAutoPoser != null)
                {
                    _rightHandPreviewAutoPoser.UpdateContinuously = false;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawEditorArc();
        }

        private void DrawEditorArc()
        {
            float degreeDifferenceAllowed = MaxDegreeDifferenceAllowed;
            if (degreeDifferenceAllowed != 0 && Math.Abs(MaxDegreeDifferenceAllowed - 360) > float.Epsilon)
            {
                Transform t = transform;
                Vector3 up = t.up;
                Vector3 forward = t.forward;

                Vector3 from = Quaternion.AngleAxis(-0.5f * MaxDegreeDifferenceAllowed, up) *
                               (-forward - Vector3.Dot(-forward, up) * up);

                Handles.color = new Color(0, 1, 0, 0.1f);
                Handles.DrawSolidArc(
                    t.position, up, from, MaxDegreeDifferenceAllowed, 0.05f
                );
            }
        }
    }
}
#endif