using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRPlayerHands : MonoBehaviour
{
    [SerializeField] private XRPlayerHand leftHand;
    [SerializeField] private XRPlayerHand rightHand;

    public XRPlayerHand LeftHand => leftHand;
    public XRPlayerHand RightHand => rightHand;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        LeftHand.Initialize(InteractorHandedness.Left);
        RightHand.Initialize(InteractorHandedness.Right);
    }

    public bool FindHandBySide(InteractorHandedness handedness, out XRPlayerHand hand)
    {
        switch (handedness)
        {
            case InteractorHandedness.Left:
            {
                hand = leftHand;
                return true;
            }
            case InteractorHandedness.Right:
            {
                hand = rightHand;
                return true;
            }
            default:
            {
                hand = default;
                return false;
            }
        }
    }

    public bool FindHandWithInteractor(IXRInteractor interactor, out XRPlayerHand targetHand)
    {
        if (interactor.handedness == InteractorHandedness.None)
        {
            targetHand = null;
            return false;
        }

        if (ReferenceEquals(leftHand.NearFarInteractor, interactor))
        {
            targetHand = leftHand;
            return true;
        }

        if (ReferenceEquals(rightHand.NearFarInteractor, interactor))
        {
            targetHand = rightHand;
            return true;
        }

        targetHand = default;
        return false;
    }

    public bool FindHandWithInteractable(IXRSelectInteractable interactable, out XRPlayerHand targetHand)
    {
        if (!interactable.isSelected)
        {
            targetHand = null;
            return false;
        }

        IXRSelectInteractor interactor = interactable.interactorsSelecting[0];
        return FindHandWithInteractor(interactor, out targetHand);
    }

    public bool FindHandWithCollider(Collider collider, out XRPlayerHand targetHand)
    {
        if (leftHand.ContainsCollider(collider))
        {
            targetHand = leftHand;
            return true;
        }

        if (rightHand.ContainsCollider(collider))
        {
            targetHand = rightHand;
            return true;
        }

        targetHand = default;
        return false;
    }

    public bool IsGrabbing(IXRSelectInteractable interactable)
    {
        if (!interactable.isSelected)
            return false;

        List<IXRSelectInteractor> interactors = interactable.interactorsSelecting;
        for (int i = 0; i < interactors.Count; i++)
        {
            IXRSelectInteractor interactor = interactors[i];
            if (ReferenceEquals(interactor, leftHand.NearFarInteractor) ||
                ReferenceEquals(interactor, rightHand.NearFarInteractor))
                return true;
        }

        return false;
    }

    public void ForceDrop()
    {
        LeftHand.ForceDrop();
        RightHand.ForceDrop();
    }
}