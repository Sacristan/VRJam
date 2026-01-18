using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace BNG
{
    /// A helper component you can place on grabbable object to decide which hand pose method and definition to use
    /// For example : When grabbing an object, you can use GetComponent<PoseableObject> to check whether to apply a specific HandPose to the HandPoser, or to enable AutoPose, set an ID on a hand animator, or implement your own custom solution
    [HelpURL("https://wiki.beardedninjagames.com/en/General/HandPoses")]
    [RequireComponent(typeof(XRBaseInteractable))]
    public class PoseableObject : MonoBehaviour
    {
        [SerializeField] private GameObject grabPointsHolder;

        private GrabPoint[] _grabPoints;
        private XRBaseInteractable _interactable;

        public GrabPoint[] GrabPoints
        {
            get => _grabPoints;
            set => _grabPoints = value;
        }

        public Transform AttachTransform { get; private set; }

        private void Awake()
        {
            if (grabPointsHolder == null) grabPointsHolder = gameObject;
            ApplyGrabPoints(grabPointsHolder);

            _interactable = GetComponent<XRBaseInteractable>();
            InitAttachTransform();
        }

        private void InitAttachTransform()
        {
            if (_interactable is XRGrabInteractable xrGrabInteractable)
            {
                if (xrGrabInteractable.attachTransform == null)
                {
                    Transform interTransform = _interactable.transform;

                    AttachTransform = new GameObject("AttachTransform").transform;
                    AttachTransform.SetParent(interTransform, worldPositionStays: false);

                    if (_grabPoints.Length > 0)
                    {
                        Transform grabTransform = _grabPoints[0].transform;
                        AttachTransform.SetPositionAndRotation(grabTransform.position, grabTransform.rotation);
                    }
                    else
                    {
                        AttachTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    }

                    xrGrabInteractable.attachTransform = AttachTransform;
                }
                else
                {
                    AttachTransform = xrGrabInteractable.attachTransform;
                }
            }
            else
            {
                AttachTransform = _interactable.transform;
            }
        }

        public bool FindGrabPose(IXRSelectInteractor interactor, InteractorHandedness handSide,
            out GrabPoint heldObjectPoint)
        {
            heldObjectPoint = GetClosestGrabPoint(interactor, handSide);
            return heldObjectPoint != null;
        }

        private GrabPoint GetClosestGrabPoint(IXRSelectInteractor interactor, InteractorHandedness handSide)
        {
            Transform interactorTransform;
            if (interactor is Component interactorComponent)
            {
                interactorTransform = interactorComponent.transform;
            }
            else
            {
                interactorTransform = transform;
            }

            Vector3 interactorPosition = interactorTransform.position;
            Quaternion interactorRotation = interactorTransform.rotation;

            GrabPoint[] grabPoints = _grabPoints;

            using (ListPool<WeightedGrabPoint>.Get(out List<WeightedGrabPoint> weightedGrabPoints))
            {
                float maxDistance = float.MinValue;

                for (int i = 0, iSize = grabPoints.Length; i < iSize; i++)
                {
                    GrabPoint gp = grabPoints[i];

                    if (gp == null) continue;
                    if (!gp.gameObject.activeInHierarchy) continue;
                    if (!gp.IsValidForHandSide(handSide)) continue;

                    Transform gpTransform = gp.transform;

                    float angle = Quaternion.Angle(interactorRotation, gpTransform.rotation);
                    if (angle > gp.MaxDegreeDifferenceAllowed) continue; // Angle is too great

                    float dist = Vector3.Distance(gpTransform.position, interactorPosition);
                    if (dist > maxDistance)
                    {
                        maxDistance = dist;
                    }

                    weightedGrabPoints.Add(new WeightedGrabPoint(gp, dist, angle));
                }

                return GetClosestGrabPoint(weightedGrabPoints, maxDistance);
            }
        }

        private GrabPoint GetClosestGrabPoint(List<WeightedGrabPoint> weightedGrabPoints, float maxDistance)
        {
            GrabPoint closestGrabPoint = null;
            float maxWeight = float.MinValue;

            for (int i = 0, iSize = weightedGrabPoints.Count; i < iSize; i++)
            {
                float weight = weightedGrabPoints[i].CalculateWeight(maxDistance);
                if (weight > maxWeight)
                {
                    closestGrabPoint = weightedGrabPoints[i].GrabPoint;
                    maxWeight = weight;
                }
            }

            return closestGrabPoint;
        }

        public void ApplyGrabPoints(GameObject holder)
        {
            grabPointsHolder = holder;

            using (ListPool<GrabPoint>.Get(out List<GrabPoint> tempGrabPoints))
            {
                tempGrabPoints.Clear();
                GrabPoint[] baseGrabPoints = holder.GetComponentsInChildren<GrabPoint>();

                for (int i = 0, iSize = baseGrabPoints.Length; i < iSize; i++)
                {
                    baseGrabPoints[i].TryToSpread(tempGrabPoints);
                }

                GrabPoints = tempGrabPoints.ToArray();
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            GrabPoint grabPoint = GetComponentInChildren<GrabPoint>();
            if (grabPoint != null)
            {
                grabPointsHolder = grabPoint.transform.parent.gameObject;
            }
        }
#endif
    }
}