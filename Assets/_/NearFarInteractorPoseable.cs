using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class NearFarInteractorPoseable : NearFarInteractor
{
    [SerializeField] private XRHandPoseController poseController;
    [SerializeField] private ConfigurableJoint joint;

    public ConfigurableJoint Joint => joint;

    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        base.OnSelectEntering(args);
        if (poseController != null)
        {
            poseController.OnSelectEntering(args.interactorObject, args.interactableObject);
        }
    }

    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        base.OnSelectExiting(args);
        if (poseController != null)
        {
            poseController.OnSelectExiting();
        }
    }
}