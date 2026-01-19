using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public static class XRInteractionUtils
{
    public static void ForceDrop(this XRBaseInteractor baseInteractor)
    {
        ForceDrop(baseInteractor.interactionManager, baseInteractor);
    }

    public static void ForceDrop(this XRInteractionManager interactionManager, IXRSelectInteractor interactor)
    {
        interactionManager.CancelInteractorSelection(interactor);
    }

    public static void ForceDrop(this XRBaseInteractable baseInteractable)
    {
        ForceDrop(baseInteractable.interactionManager, baseInteractable);
    }

    public static void ForceDrop(this XRInteractionManager interactionManager, IXRSelectInteractable interactable)
    {
        interactionManager.CancelInteractableSelection(interactable);
    }

    public static bool TryGetTransform([CanBeNull] this IXRInteractable interactable, out Transform result)
    {
        if (interactable == null)
        {
            result = default;
            return false;
        }

        if (interactable is Component component && component != null)
        {
            result = component.transform;
            return result != null;
        }

        result = default;
        return false;
    }

    public static bool TryGetTransform([CanBeNull] this IXRInteractor interactor, out Transform result)
    {
        if (interactor == null)
        {
            result = default;
            return false;
        }

        if (interactor is Component component && component != null)
        {
            result = component.transform;
            return result != null;
        }

        result = default;
        return false;
    }

    /// Makes sure that the behavior-based interactable is a valid object (not destroyed or disabled). 
    public static bool CheckAliveAndActive([CanBeNull] this IXRInteractable interactable)
    {
        if (interactable == null)
            return false;

        // For non-behavior based interactable, always consider it's valid.
        if (interactable is not Behaviour behavior)
            return true;

        if (behavior == null)
            return false;

        if (!behavior.isActiveAndEnabled)
            return false;

        if (!behavior.gameObject.activeInHierarchy)
            return false;

        return true;
    }

    public static void AlignWithInteractorOnSelect(
        MonoBehaviour target,
        SelectEnterEventArgs args)
    {
        //TODO: This is dirty, but it seem to work reliably (for now?).
        static IEnumerator InnerCoroutine(
            MonoBehaviour target,
            IXRSelectInteractable interactable,
            IXRSelectInteractor interactor)
        {
            yield return new WaitForSeconds(0.1f);
            
            // A lot of safety measures follows...
            if (target == null)
                yield break;
            
            if (!interactable.isSelected)
                yield break;
            if (interactable is Component compInteractable &&
                compInteractable == null)
                yield break;
            
            if (!interactor.isSelectActive ||
                !interactor.IsSelecting(interactable))
                yield break;
            if (interactable is Component compInteractor &&
                compInteractor == null)
                yield break;

            target.transform.rotation = interactor.transform.rotation;
        }
        target.StartCoroutine(InnerCoroutine(target, args.interactableObject, args.interactorObject));
    }

    #region Simulate enter events.

    private static readonly ObjectPool<SelectEnterEventArgs> s_selectEnterArgsPool = new(
        createFunc: () => new SelectEnterEventArgs(),
        actionOnRelease: args =>
        {
            args.manager = default;
            args.interactorObject = default;
            args.interactableObject = default;
        });

    private static readonly ObjectPool<HoverEnterEventArgs> s_hoverEnterArgsPool = new(
        createFunc: () => new HoverEnterEventArgs(),
        actionOnRelease: args =>
        {
            args.manager = default;
            args.interactorObject = default;
            args.interactableObject = default;
        });

    /// If the interactable is selected, the handler will be called ONCE with the first interactor selecting. 
    public static bool CallOnceIfSelected(XRBaseInteractable interactable, Action<SelectEnterEventArgs> handler)
    {
        if (!interactable.isSelected)
            return false;

        using (s_selectEnterArgsPool.Get(out SelectEnterEventArgs args))
        {
            args.manager = interactable.interactionManager;
            args.interactorObject = interactable.interactorsSelecting[0];
            args.interactableObject = interactable;

            handler.Invoke(args);
        }
        return true;
    }

    /// If the interactable is hovered, the handler will be called ONCE with the first interactor hovering.
    public static bool CallOnceIfHovered(XRBaseInteractable interactable, Action<HoverEnterEventArgs> handler)
    {
        if (!interactable.isHovered)
            return false;

        using (s_hoverEnterArgsPool.Get(out HoverEnterEventArgs args))
        {
            args.manager = interactable.interactionManager;
            args.interactorObject = interactable.interactorsHovering[0];
            args.interactableObject = interactable;

            handler.Invoke(args);
        }
        return true;
    }

    #endregion
}