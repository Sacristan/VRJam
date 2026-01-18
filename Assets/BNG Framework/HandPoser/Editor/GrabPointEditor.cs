using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Object = UnityEngine.Object;

namespace BNG
{
    [CustomEditor(typeof(GrabPoint))]
    [CanEditMultipleObjects]
    public class GrabPointEditor : Editor
    {
        private string LeftModelPreviewName => GrabPoint.LeftModelPreviewName;
        private GameObject _leftHandPreview;
        private bool _showingLeftHand = false;

        private string RightModelPreviewName => GrabPoint.RightModelPreviewName;
        private GameObject _rightHandPreview;
        private bool _showingRightHand = false;

        // Define a texture and GUIContent
        private static readonly string[] TexturesFolders = { "Assets/BNG Framework/HandPoser/Editor/Textures" };

        private Texture _buttonLeftTexture;
        private Texture _buttonLeftTextureSelected;
        private GUIContent _buttonLeftContent;

        private Texture _buttonRightTexture;
        private Texture _buttonRightTextureSelected;

        private GUIContent _buttonRightContent;

        private GrabPoint _grabPoint;

        private SerializedProperty _handPoseType;
        private SerializedProperty _selectedHandPose;
        private SerializedProperty _leftHandIsValid;
        private SerializedProperty _leftHandPreviewPrefab;
        private SerializedProperty _rightHandIsValid;
        private SerializedProperty _rightHandPreviewPrefab;
        private SerializedProperty _maxDegreeDifferenceAllowed;

        private SerializedProperty _spreadAlongAxis;
        private SerializedProperty _spreadAnchor;
        private SerializedProperty _spreadAxis;
        private SerializedProperty _spreadCount;
        private SerializedProperty _spreadAngleStep;

        private PoseType _previousType;

        void OnEnable()
        {
            _handPoseType = serializedObject.FindProperty("poseType");
            _selectedHandPose = serializedObject.FindProperty("equipHandPose");
            _leftHandIsValid = serializedObject.FindProperty("leftHandIsValid");
            _leftHandPreviewPrefab = serializedObject.FindProperty("leftHandPreviewPrefab");
            _rightHandIsValid = serializedObject.FindProperty("rightHandIsValid");
            _rightHandPreviewPrefab = serializedObject.FindProperty("rightHandPreviewPrefab");
            _maxDegreeDifferenceAllowed = serializedObject.FindProperty("maxDegreeDifferenceAllowed");
            
            _spreadAlongAxis = serializedObject.FindProperty("spreadAlongAxis");
            _spreadAnchor = serializedObject.FindProperty("spreadAnchor");
            _spreadAxis = serializedObject.FindProperty("spreadAxis");
            _spreadCount = serializedObject.FindProperty("spreadCount");
            _spreadAngleStep = serializedObject.FindProperty("spreadAngleStep");
        }

        public override void OnInspectorGUI()
        {
            _grabPoint = (GrabPoint)target;
            bool inPrefabMode = false;
            inPrefabMode = PrefabStageUtility.GetCurrentPrefabStage() != null;

            // Double check that there wasn't an object left in the scene
            CheckForExistingPreview();

            // Check for change in handpose type
            if (_grabPoint.PoseType != _previousType)
            {
                OnHandPoseTypeChange();
            }

            LoadTextures();

            GUILayout.Label("Toggle Hand Preview : ", EditorStyles.boldLabel);

            if (inPrefabMode)
            {
                GUILayout.Label("(Some preview features disabled in prefab mode)", EditorStyles.largeLabel);
            }

            GUILayout.BeginHorizontal();

            // Show / Hide Left Hand
            if (_showingLeftHand)
            {
                // Define a GUIContent which uses the texture
                _buttonLeftContent = new GUIContent(_buttonLeftTextureSelected);

                if (!_grabPoint.LeftHandIsValid || GUILayout.Button(_buttonLeftContent))
                {
                    DestroyImmediate(_leftHandPreview);
                    _showingLeftHand = false;
                }
            }
            else
            {
                _buttonLeftContent = new GUIContent(_buttonLeftTexture);

                if (_grabPoint.LeftHandIsValid && GUILayout.Button(_buttonLeftContent))
                {
                    // Create and add the Editor preview
                    CreateLeftHandPreview();
                }
            }

            // Show / Hide Right Hand
            if (_showingRightHand)
            {
                // Define a GUIContent which uses the texture
                _buttonRightContent = new GUIContent(_buttonRightTextureSelected);

                if (!_grabPoint.RightHandIsValid || GUILayout.Button(_buttonRightContent))
                {
                    DestroyImmediate(_rightHandPreview);
                    _showingRightHand = false;
                }
            }
            else
            {
                _buttonRightContent = new GUIContent(_buttonRightTexture);

                if (_grabPoint.RightHandIsValid && GUILayout.Button(_buttonRightContent))
                {
                    CreateRightHandPreview();
                }
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(_leftHandIsValid);
            if (_grabPoint.LeftHandIsValid)
            {
                DrawHandPreviewSelector(_leftHandPreviewPrefab, InteractorHandedness.Left);
            }

            EditorGUILayout.PropertyField(_rightHandIsValid);
            if (_grabPoint.RightHandIsValid)
            {
                DrawHandPreviewSelector(_rightHandPreviewPrefab, InteractorHandedness.Right);
            }

            EditorGUILayout.PropertyField(_maxDegreeDifferenceAllowed);

            EditorGUILayout.PropertyField(_handPoseType);

            if (_grabPoint.PoseType == PoseType.HandPose)
            {
                EditorGUILayout.PropertyField(_selectedHandPose);

                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("");
                EditorGUILayout.Space(0, true);

                if (GUILayout.Button("Edit Pose..."))
                {
                    EditHandPose();
                }

                GUILayout.EndHorizontal();
            }
            
            EditorGUILayout.PropertyField(_spreadAlongAxis);
            if (_grabPoint.SpreadAlongAxis)
            {
                EditorGUILayout.PropertyField(_spreadAnchor);
                EditorGUILayout.PropertyField(_spreadAxis);
                EditorGUILayout.PropertyField(_spreadCount);
                EditorGUILayout.PropertyField(_spreadAngleStep);
            }

            serializedObject.ApplyModifiedProperties();
            // base.OnInspectorGUI();

            _grabPoint.UpdatePreviews();
        }

        private void DrawHandPreviewSelector(SerializedProperty previewPrefab, InteractorHandedness side)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(previewPrefab);

                if (EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Passive, GUILayout.Width(20)))
                {
                    ShowHandPreviewSelectorMenu(previewPrefab, side);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ShowHandPreviewSelectorMenu(SerializedProperty previewPrefabProperty, InteractorHandedness side)
        {
            HandsPreviewData handsPreviewData = HandsPreviewData.Instance;
            if (handsPreviewData == null)
            {
                Debug.LogWarning($"Instance of {nameof(HandsPreviewData)} not found.");
                return;
            }

            GameObject[] candidates = handsPreviewData.GetPreviews(side);
            if (candidates.Length == 0)
            {
                Debug.LogWarning($"Candidates for {side} hand are not found.");
                return;
            }

            GenericMenu menu = new GenericMenu();
            for (int i = 0, iSize = candidates.Length; i < iSize; i++)
            {
                GameObject candidate = candidates[i];
                if (candidate == null) continue;

                menu.AddItem(
                    new GUIContent(candidate.name), false,
                    () => SetPreviewPrefab(previewPrefabProperty, candidate)
                );
            }

            menu.ShowAsContext();
        }

        private void SetPreviewPrefab(SerializedProperty previewPrefabProperty, GameObject previewPrefab)
        {
            if (previewPrefab == null) return;

            previewPrefabProperty.objectReferenceValue = previewPrefab;
            serializedObject.ApplyModifiedProperties();
        }

        private void LoadTextures()
        {
            TryToLoadTexture(ref _buttonLeftTexture, "handIconLeft");
            TryToLoadTexture(ref _buttonLeftTextureSelected, "handIconLeftSelected");
            TryToLoadTexture(ref _buttonRightTexture, "handIconRight");
            TryToLoadTexture(ref _buttonRightTextureSelected, "handIconSelectedRight");
        }

        private void TryToLoadTexture(ref Texture texture, string assetName)
        {
            if (texture != null) return;

            texture = AssetDatabase
                .FindAssets("t:Texture", TexturesFolders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Texture>)
                .First(t => t.name == assetName);
        }

        private void OnHandPoseTypeChange()
        {
            if (_grabPoint.PoseType == PoseType.HandPose)
            {
                UpdateHandPosePreview();
            }

            _previousType = _grabPoint.PoseType;
        }

        private void CreateLeftHandPreview()
        {
            _grabPoint.LeftHandPreviewPrefab = FindPreviewPrefab(
                _grabPoint.LeftHandPreviewPrefab, InteractorHandedness.Left
            );
            _leftHandPreview = CreateHandModelPreview(
                _grabPoint.LeftHandPreviewPrefab, LeftModelPreviewName
            );
            _showingLeftHand = true;
        }

        private void CreateRightHandPreview()
        {
            _grabPoint.RightHandPreviewPrefab = FindPreviewPrefab(
                _grabPoint.RightHandPreviewPrefab, InteractorHandedness.Right
            );
            _rightHandPreview = CreateHandModelPreview(
                _grabPoint.RightHandPreviewPrefab, RightModelPreviewName
            );
            _showingRightHand = true;
        }

        private GameObject CreateHandModelPreview(GameObject previewPrefab, string previewName)
        {
            GameObject handPreview = Instantiate(previewPrefab);
            handPreview.name = previewName;
            DestroyExtraObjects(handPreview);
            DestroyExtraComponents(handPreview);
            if (!Application.isPlaying)
            {
                handPreview.AddComponent<DestroyIfPlayMode>();
            }

            const HideFlags previewHideFlags = HideFlags.HideAndDontSave;
            // const HideFlags previewHideFlags = HideFlags.DontSave;
            handPreview.hideFlags = previewHideFlags;

            Transform previewTransform = handPreview.transform;
            previewTransform.parent = _grabPoint.transform;
            previewTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            _grabPoint.UpdatePreviews();

            return handPreview;
        }

        private void DestroyExtraObjects(GameObject handPreview)
        {
            HashSet<Transform> usefulTransforms = CollectUsefulTransforms(handPreview.transform);
            Transform[] allTransforms = handPreview.GetComponentsInChildren<Transform>(true);
            List<Transform> transformsToRemove = new List<Transform>(allTransforms);
            transformsToRemove.RemoveAll(t => usefulTransforms.Contains(t));
            for (int i = 0, iSize = transformsToRemove.Count; i < iSize; i++)
            {
                if (transformsToRemove[i] == null) continue;
                SaveDestroy(transformsToRemove[i].gameObject);
            }
        }

        private HashSet<Transform> CollectUsefulTransforms(Transform root)
        {
            HashSet<Transform> usefulTransforms = new HashSet<Transform>();
            usefulTransforms.Add(root);
            SkinnedMeshRenderer[] skinnedMeshes = root.GetComponentsInChildren<SkinnedMeshRenderer>(false);

            for (int i = 0, iSize = skinnedMeshes.Length; i < iSize; i++)
            {
                Transform skinnedMeshTransform = skinnedMeshes[i].transform;
                usefulTransforms.Add(skinnedMeshTransform);
                Transform skinnedMeshParent = skinnedMeshTransform.parent;
                Transform[] skinnedChildren = skinnedMeshParent.GetComponentsInChildren<Transform>(true);
                foreach (Transform skinnedChild in skinnedChildren)
                {
                    usefulTransforms.Add(skinnedChild);
                }

                Transform skinnedParent = skinnedMeshTransform.transform.parent;
                while (skinnedParent != root)
                {
                    usefulTransforms.Add(skinnedParent);
                    skinnedParent = skinnedParent.parent;
                }
            }

            Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0, iSize = allTransforms.Length; i < iSize; i++)
            {
                Transform t = allTransforms[i];
                if (t.name.StartsWith("hands"))
                {
                    usefulTransforms.Add(t);
                }
            }

            return usefulTransforms;
        }

        private void DestroyExtraComponents(GameObject handPreview)
        {
            Type[] usefulTypes =
            {
                typeof(Transform), typeof(GameObject),
                typeof(HandPoser), typeof(AutoPoser),
                typeof(Renderer), typeof(MeshRenderer), typeof(SkinnedMeshRenderer),
                typeof(Rigidbody)
            };

            Component[] allComponents = handPreview.GetComponentsInChildren<Component>(true);
            List<Component> componentsToRemove = new List<Component>();
            for (int i = 0, iSize = allComponents.Length; i < iSize; i++)
            {
                Component component = allComponents[i];
                if (usefulTypes.Contains(component.GetType())) continue;

                componentsToRemove.Add(component);
            }

            SafeDestroyComponents(componentsToRemove);
        }

        /// Unity provides GetComponents methods,
        /// which return an array of all components attached to a GameObject
        /// in the order they were added.
        /// So, we are destroying components in reverse order to ensure
        /// dependent components are removed before the components they depend on.
        private static void SafeDestroyComponents(List<Component> components)
        {
            for (int i = components.Count - 1; i >= 0; i--)
            {
                SaveDestroy(components[i]);
            }
        }

        private static void SaveDestroy(Object o)
        {
            if (o == null) return;

            if (Application.isPlaying)
            {
                Destroy(o);
            }
            else
            {
                DestroyImmediate(o);
            }
        }

        private GameObject FindPreviewPrefab(GameObject selectedPrefab, InteractorHandedness handSide)
        {
            if (selectedPrefab != null) return selectedPrefab;

            HandPoser[] handPosers = FindObjectsByType<HandPoser>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None
            );
            switch (handSide)
            {
                case InteractorHandedness.Left:
                    return handPosers.First(poser => poser.name.StartsWith("L_")).transform.parent.gameObject;

                case InteractorHandedness.Right:
                    return handPosers.First(poser => poser.name.StartsWith("R_")).transform.parent.gameObject;
            }

            return null;
        }

        public void EditHandPose()
        {
            // Select the Hand Object
            if (_grabPoint.RightHandIsValid)
            {
                if (!_showingRightHand)
                {
                    CreateRightHandPreview();
                }

                _rightHandPreview.gameObject.hideFlags = HideFlags.DontSave;
                HandPoser hp = _rightHandPreview.gameObject.GetComponentInChildren<HandPoser>();
                if (!hp.TryGetComponent(out AutoPoser _))
                {
                    hp.gameObject.AddComponent<AutoPoser>();
                }

                Selection.activeGameObject = hp.gameObject;
            }
            else if (_grabPoint.LeftHandIsValid)
            {
                if (!_showingLeftHand)
                {
                    CreateLeftHandPreview();
                }

                _leftHandPreview.gameObject.hideFlags = HideFlags.DontSave;
                HandPoser hp = _leftHandPreview.gameObject.GetComponentInChildren<HandPoser>();
                if (!hp.TryGetComponent(out AutoPoser _))
                {
                    hp.gameObject.AddComponent<AutoPoser>();
                }

                Selection.activeGameObject = hp.gameObject;
            }
            else
            {
                Debug.Log(
                    "No HandPoser component was found on hand preview prefab. You may need to add one to 'Resources/RightHandModelsEditorPreview'."
                );
            }
        }

        public void UpdateHandPosePreview()
        {
            if (_leftHandPreview)
            {
                var hp = _leftHandPreview.GetComponentInChildren<HandPoser>();
                if (hp)
                {
                    // Trigger a change
                    hp.OnPoseChanged();
                }
            }

            if (_rightHandPreview)
            {
                var hp = _rightHandPreview.GetComponentInChildren<HandPoser>();
                if (hp)
                {
                    hp.OnPoseChanged();
                }
            }
        }

        private void CheckForExistingPreview()
        {
            if (_leftHandPreview == null && !_showingLeftHand)
            {
                Transform lt = _grabPoint.transform.Find(LeftModelPreviewName);
                if (lt)
                {
                    _leftHandPreview = lt.gameObject;
                    _showingLeftHand = true;
                }
            }

            if (_rightHandPreview == null && !_showingRightHand)
            {
                Transform rt = _grabPoint.transform.Find(RightModelPreviewName);
                if (rt)
                {
                    _rightHandPreview = rt.gameObject;
                    _showingRightHand = true;
                }
            }
        }
    }
}