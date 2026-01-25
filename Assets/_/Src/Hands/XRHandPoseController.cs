using System;
using BNG;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// based on BNG>SampleHandController
/// Controls HandPose, position, rotation and hand view parenting
[HelpURL("https://wiki.beardedninjagames.com/en/General/HandPoses")]
public class XRHandPoseController : MonoBehaviour
{
    [SerializeField] private Transform alternativeHandViewParent;

    private GrabPoint _activeGrabPoint;
    [CanBeNull] public GrabPoint ActiveGrabPoint => _activeGrabPoint;

    private bool _wasHoldingObject = false;

    private XRPlayerHand _parentHand;
    private InteractorHandedness HandSide => _parentHand.Side;
    private HandPoser HandPoser => _parentHand.HandGraphics.HandPoser;
    private HandPoseBlender PoseBlender => _parentHand.HandGraphics.PoseBlender;
    private AutoPoser AutoPoser => _parentHand.HandGraphics.AutoPoser;

    public HandGraphics HandGraphics => _parentHand.HandGraphics;
    private Transform HandGraphicsTransform => _parentHand.HandGraphics.Transform;
    private Transform _defaultHandGraphicsParent;
    private Pose _defaultHandGraphicsPose;

    public void Init(XRPlayerHand parentHand)
    {
        _parentHand = parentHand;

        Transform handGraphicsTransform = HandGraphicsTransform;
        _defaultHandGraphicsParent = handGraphicsTransform.parent;
        _defaultHandGraphicsPose = handGraphicsTransform.GetLocalPose();
    }

    public void OnSelectEntering(IXRSelectInteractor interactor, IXRInteractable interactable)
    {
        if (!SetHeldObjectPose(interactor, interactable))
        {
            ClearCurrentlyHeldObject();
        }
    }

    public void OnSelectExiting()
    {
        
        ClearCurrentlyHeldObject();
    }

    private void Update()
    {
        DoHandControllerUpdate();
    }

    private void DoHandControllerUpdate()
    {
        UpdateFingerInputs();

        if (HoldingObject())
        {
            DoHeldItemPose();
        }
        else
        {
            DoIdlePose();
        }
    }

    private bool SetHeldObjectPose(IXRSelectInteractor interactor, IXRInteractable interactable)
    {
        if (!FindPoseableObject(interactable, out PoseableObject poseableObject)) return false;
        if (!poseableObject.FindGrabPose(interactor, HandSide, out _activeGrabPoint)) return false;

        Transform grabPointTransform = _activeGrabPoint.transform;
        Vector3 attachTransformRelativePosition = grabPointTransform.localPosition;
        Quaternion attachTransformRelativeRotation = grabPointTransform.localRotation;

        Transform poseableObjectTransform = poseableObject.transform;
        Transform interactableAttachTransform = poseableObject.AttachTransform;
        interactableAttachTransform.position = poseableObjectTransform.TransformPoint(attachTransformRelativePosition);
        interactableAttachTransform.rotation = poseableObjectTransform.rotation * attachTransformRelativeRotation;
        return true;
    }

    private static bool FindPoseableObject(IXRInteractable interactable, out PoseableObject poseableObject)
    {
        Transform interactableTransform = interactable.transform;
        if (interactableTransform != null)
        {
            return interactableTransform.TryGetComponent(out poseableObject);
        }

        poseableObject = default;
        return false;
    }

    private void ClearCurrentlyHeldObject()
    {
        if (_activeGrabPoint != null)
        {
            _activeGrabPoint = null;
            ResetToIdleComponents();
        }

        ResetDefaultGraphicsPose();
        _wasHoldingObject = false;
    }

    private void ResetDefaultGraphicsPose()
    {
        Transform t = HandGraphicsTransform;
        Transform newParent = _defaultHandGraphicsParent.gameObject.activeSelf
            ? _defaultHandGraphicsParent
            : alternativeHandViewParent;
        t.SetParent(newParent);
        t.SetLocalPose(_defaultHandGraphicsPose);
    }

    private void ResetToIdleComponents()
    {
        HandPoser.enabled = true;
        HandPoser.CurrentPose = null;

        if (AutoPoser)
        {
            AutoPoser.enabled = false;
        }
    }

    private void UpdateFingerInputs()
    {
        PoseBlender.ThumbValue = Mathf.Lerp(
            PoseBlender.ThumbValue,
            GetThumbIsNear() ? 1 : 0,
            Time.deltaTime * HandPoser.AnimationSpeed
        );

        float targetIndexValue = CorrectValue(GetIndexValue());
        PoseBlender.IndexValue = Mathf.Lerp(
            PoseBlender.IndexValue, targetIndexValue,
            Time.deltaTime * HandPoser.AnimationSpeed
        );

        PoseBlender.GripValue = CorrectValue(GetGripValue());
    }

    private void DoHeldItemPose(bool force = false)
    {
        if (force || !_wasHoldingObject)
        {
            if ((_activeGrabPoint.PoseType == PoseType.AutoPoseContinuous ||
                 _activeGrabPoint.PoseType == PoseType.AutoPoseOnce) && AutoPoser != null)
            {
                HandPoser.CurrentPose = null;
                Invoke(nameof(DisableContinuousAutoPose), _activeGrabPoint.AutoPoseDuration); //todo optimize
            }

            else if (_activeGrabPoint.PoseType == PoseType.HandPose)
            {
                HandPoser.CurrentPose = _activeGrabPoint.EquipHandPose;
            }
            else if (_activeGrabPoint.PoseType == PoseType.HandPose)
            {
                HandPoser.enabled = false;
            }
        }

        _wasHoldingObject = true;
    }

    private void DisableContinuousAutoPose()
    {
        if (AutoPoser)
        {
            AutoPoser.UpdateContinuously = false;
        }
    }

    private void DoIdlePose()
    {
        PoseBlender.DoIdleBlendPose();
    }

    private bool HoldingObject()
    {
        return _activeGrabPoint != null;
    }

    private static float CorrectValue(float inputValue)
    {
        return (float)Math.Round(inputValue * 1000f) / 1000f;
    }

    private float GetIndexValue()
    {
        return _parentHand.HandInputs.TriggerValue;
    }

    private float GetGripValue()
    {
        return _parentHand.HandInputs.GripValue;
    }

    private bool GetThumbIsNear()
    {
        return _parentHand.HandInputs.ThumbTouched;
    }

    public void SetAlternativePose(HandPose gripHandPose)
    {
        HandPoser.CurrentPose = gripHandPose;
    }

    public void ResetAlternativePose()
    {
        if (HoldingObject())
        {
            DoHeldItemPose(true);
        }
    }
}