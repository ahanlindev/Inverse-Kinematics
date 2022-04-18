using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ahanlindev
{
    public class IKSkeletonGenerator : EditorWindow
    {
        GameObject skeleParent;
        GameObject skeleRoot;

        [MenuItem("Window/IK Skeleton Generator")]
        public static void ShowWindow()
        {
            GetWindow(typeof(IKSkeletonGenerator));
        }
        private void OnGUI()
        {
            
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(new GUIContent("This allows for easy generation " +
                "of an IK skeleton. The resultant IKSkeleton component will be " +
                "attached to the object in the parent field, while root and each " +
                "of its children will recieve an IKBone component."));
            skeleParent = EditorGUILayout.ObjectField("Parent GameObject:", skeleParent, typeof(GameObject), true) as GameObject;
            skeleRoot = EditorGUILayout.ObjectField("Root GameObject:", skeleRoot, typeof(GameObject), true) as GameObject;
            bool buttonPressed = GUILayout.Button("Generate Skeleton");
            if(buttonPressed)
            {
                Debug.Log("Generated Skeleton at " + skeleRoot.transform.position.ToString());
            }
        }
        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
