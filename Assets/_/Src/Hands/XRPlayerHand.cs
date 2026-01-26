using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRPlayerHand : MonoBehaviour
{
    // [SerializeField] private XRHandHaptic haptic;
    [SerializeField] private XRHandPoseController poseController;
    [SerializeField] private NearFarInteractorPoseable nearFarInteractor;
    // [SerializeField] private HandInteractorShaker shaker;
    [SerializeField] private XRInteractionGroup interactionGroup;
    [SerializeField] private FingersController fingersController;
    [SerializeField] private HandGraphics graphics;
    [SerializeField] private HandInputs inputs;
    [SerializeField] private ControllerVelocityTracker velocityTracker;
    // [SerializeField] private UniversalControllerTooltip controllerTooltip;
    //Default Value 0.5 based on BNG.HandCollision.cs:90 value, which feels ok in most cases.
    [Tooltip("Delay after deselect any intractable")]
    [SerializeField] private float enableCollidersDelay = 0.5f;

    /// The interactable that was released earlier this frame.
    /// This value gets reset each frame in late update.
    private IXRSelectInteractable _justReleasedInteractable;

    public InteractorHandedness Side { get; private set; }

    // public XRHandHaptic Haptic => haptic;
    public XRHandPoseController PoseController => poseController;
    public NearFarInteractorPoseable NearFarInteractor => nearFarInteractor;
    public FingersController FingersController => fingersController;
    public HandGraphics HandGraphics => graphics;
    public HandInputs HandInputs => inputs;
    public ControllerVelocityTracker VelocityTracker => velocityTracker;
    // public UniversalControllerTooltip ControllerTooltip => controllerTooltip;
    public IXRSelectInteractable JustReleasedInteractable => _justReleasedInteractable;

    private XRInteractionManager _interactionManager;
    public XRInteractionManager InteractionManager => LazyComponentHelper.FindLazyComponent(ref _interactionManager);
    // public HandInteractorShaker Shaker => shaker;

    public void Initialize(InteractorHandedness side)
    {
        Side = side;
        fingersController.Init();
        poseController.Init(this);
        NearFarInteractor.selectEntered.AddListener(_ =>
        {
            fingersController.SetCollidersActive(false);
        });
        NearFarInteractor.selectExited.AddListener(args =>
        {
            if (fingersController != null)
            {
                fingersController.SetCollidersActive(true, enableCollidersDelay);
            }

            _justReleasedInteractable = args.interactableObject;
        });
    }

    public bool ContainsInteractor(IXRInteractor interactor)
    {
        if (interactor is XRBaseInteractor baseInteractor)
        {
            return interactionGroup.startingGroupMembers.Contains(baseInteractor);
        }

        return false;
    }

    public bool ContainsCollider(Collider col)
    {
        return fingersController.HasFingerCollider(col);
        // Add other colliders if the hand has any...
    }

    public void ForceDrop()
    {
        XRInteractionUtils.ForceDrop(NearFarInteractor);
    }

    private void LateUpdate()
    {
        _justReleasedInteractable = null;
    }
}