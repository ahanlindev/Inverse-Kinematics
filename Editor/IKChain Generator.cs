using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ahanlindev
{
    public class IKChainGenerator : EditorWindow
    {
        [HideInInspector] private GameObject _desiredRoot;
        [HideInInspector] private GameObject _desiredEndEffector;
        [HideInInspector] private int timesRemoveClicked = 0;

        [MenuItem("Window/IK Chain Generator")]
        public static void ShowWindow()
        {
            GetWindow(typeof(IKChainGenerator));
        }
        private void OnGUI()
        {
            
            GUILayout.Label("Kinematic Chain Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(new GUIContent(
                "Pressing Generate will add an IKChain component to the supplied root if " +
                "it does not already have one, and add IKJoint components to the root, " +
                "end effector, and all GameObjects  between them if they do not already " +
                "have one.\n\n" + "Pressing Remove will perform the inverse, removing all " + 
                "IKJoint components from these GameObjects. WARNING: THIS IS IRREVERSIBE"
            ));
            _desiredRoot = EditorGUILayout.ObjectField("Root GameObject:", _desiredRoot, typeof(GameObject), true) as GameObject;
            _desiredEndEffector = EditorGUILayout.ObjectField("End Effector GameObject:", _desiredEndEffector, typeof(GameObject), true) as GameObject;
            EditorGUILayout.BeginHorizontal();
                bool generateButtonPressed = GUILayout.Button("Generate IKJoints");
                bool removeButtonPressed = GUILayout.Button((timesRemoveClicked == 0) ? "Remove IKJoints" : "Are you Sure?");
            EditorGUILayout.EndHorizontal();
            if(generateButtonPressed)
            {
                GenerateJoints();
            }
            if (removeButtonPressed) {
                timesRemoveClicked++;
                if (timesRemoveClicked++ >= 2) { 
                    RemoveJoints();
                    timesRemoveClicked = 0;
                }
            }
        }
        
        private void GenerateJoints() {
            if (inputsAreValid()) {
                
                if (_desiredRoot.GetComponent<IKChain>() == null) {
                    IKChain newChain = _desiredRoot.AddComponent<IKChain>();
                    newChain.endEffector = _desiredEndEffector.transform;
                }    

                GameObject current = _desiredEndEffector;
                while (!current.Equals(_desiredRoot)) {
                    if (current.GetComponent<IKJoint>() == null) {
                        current.AddComponent<IKJoint>();
                    }
                    current = current.transform.parent.gameObject;
                }
            } else {
                Debug.LogError("IK Joint Generation failed: Desired End Effector is not a descendant of desired Root");
            }
            
        }

        private void RemoveJoints() {
            if (inputsAreValid()) {
                IKChain chain = _desiredRoot.GetComponent<IKChain>();
                if (chain != null) {
                    DestroyImmediate(chain);
                }    

                GameObject current = _desiredEndEffector;
                while (!current.Equals(_desiredRoot)) {
                    IKJoint[] joints = current.GetComponents<IKJoint>(); 
                    foreach(var joint in joints) {
                        DestroyImmediate(joint);
                    }
                    current = current.transform.parent.gameObject;
                }
            } else {
                Debug.LogError("IK Joint Removal failed: Desired End Effector is not a descendant of desired Root");
            }
            
        }
        private bool inputsAreValid() {
            // Want to include indirect descendants
            Transform[] children = _desiredRoot.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.gameObject.Equals(_desiredEndEffector))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
