using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ahanlindev
{
    public class IKJointGenerator : EditorWindow
    {
        [HideInInspector] private GameObject _desiredRoot;
        [HideInInspector] private GameObject _desiredEndEffector;
        

        [MenuItem("Window/IK Joint Generator")]
        public static void ShowWindow()
        {
            GetWindow(typeof(IKJointGenerator));
        }
        private void OnGUI()
        {
            
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(new GUIContent("This allows for easy generation " +
                "of a set of joints for use in an Kinematic Chain.\n" +
                "After pressing Generate Joints, " +
                "the supplied root, end effector, and all GameObjects between them will be given an " +
                "IKJoint component if they do not already have one."));
            _desiredRoot = EditorGUILayout.ObjectField("Root GameObject:", _desiredRoot, typeof(GameObject), true) as GameObject;
            _desiredEndEffector = EditorGUILayout.ObjectField("End Effector GameObject:", _desiredEndEffector, typeof(GameObject), true) as GameObject;
            bool buttonPressed = GUILayout.Button("Generate IKJoints");
            if(buttonPressed)
            {
                Debug.Log("Generated Kinematic Chain at " + _desiredEndEffector.transform.position.ToString());
            }
        }
        
        private void GenerateJoints() {
            if (inputsAreValid()) {
                GameObject current = _desiredEndEffector;
                while (current != _desiredRoot) {
                    if (current.GetComponent<IKJoint>() == null) {
                        current.AddComponent<IKJoint>();
                    }
                }
            } else {
                Debug.LogError("IK Joint Generation failed: Desired End Effector is not a descendant of desired Root");
            }
            
        }

        private bool inputsAreValid() {
            // Want to include indirect descendants
            Transform[] children = _desiredRoot.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.Equals(_desiredEndEffector))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
