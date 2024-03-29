using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ahanlindev
{
    /// <summary>
    /// Helper component for IKChain. Describes custom joint constraints for a joint within an inverse kinematics chain.
    /// </summary>
    public class IKJoint : MonoBehaviour
    {
        [Tooltip("The direction that denotes the orientation of the constraint cone")]
        public Vector3 direction = Vector3.right;

        [Tooltip("The maximum angle away from the direction that this joint is allowed to face")]
        public float maxAngle = 45;
        
        #if UNITY_EDITOR
            private Quaternion initialParentRot; 
            private Vector3 initialParentDir;

            [Header("Debug UI options")]
            [Tooltip("Number of lines that compose the debug cone gizmo")]
            [SerializeField] private int numLinesOnCone = 50;

            [Tooltip("Slant height of the debug cone gizzmo")]
            [SerializeField] private float slantHeightOfCone = 1.5f;

            [Tooltip("Color of the debug cone gizmo")]
            [SerializeField] private Color gizmoColor = new Color(.9f, .7f, .3f);
        #endif

        // Start is called before the first frame update
        void Start()
        {
            Init();
        }

        private void OnValidate() {
            Init();
        }

        private void OnDrawGizmos() {
            #if UNITY_EDITOR
                DrawConeGizmo();
            #endif
        }

        private void Init() {
            #if UNITY_EDITOR
            if (transform.parent != null) {
                initialParentRot = transform.parent.rotation;
                initialParentDir = (transform.position - transform.parent.position).normalized;
            } else {
                initialParentRot = Quaternion.identity;
                initialParentDir = Vector3.zero;
            }
            #endif
        }
        /// <summary>
        /// Returns the direction vector that marks the center of the mobility cone
        /// This vector does not account for any rotations upon the parent of this joint
        /// This vector will be normalized.
        /// <summary>
        public Vector3 GetDirection() {
            return direction.normalized;
        }


        /// <summary>
        /// Draws a cone that represents the mobility range of the joint.
        /// Can only be called in OnDrawGizmos
        /// </summary>
        private void DrawConeGizmo() {
            if(!isActiveAndEnabled) {return;}

            Vector3 currentDirection;
            if (transform.parent != null) {
                currentDirection = (transform.parent.rotation * Quaternion.Inverse(initialParentRot)) * direction.normalized;
            } else {
                currentDirection = direction.normalized;
            }
            Vector3 start = transform.position;
            Vector3 end = start + (currentDirection * slantHeightOfCone);
 
            Vector3 ortho = Vector3.Cross(currentDirection, Vector3.right);
            if (ortho == Vector3.zero) ortho = Vector3.Cross(currentDirection, Vector3.up);

            Vector3 angledDir = Vector3.RotateTowards(currentDirection, -currentDirection, Mathf.Deg2Rad * maxAngle, 0f);
            angledDir = angledDir.normalized;

            Gizmos.color = gizmoColor; 
            Gizmos.DrawLine(start, end);
            
            // Create a pseudo-cone of lines
            for(int i = 0; i < numLinesOnCone; i++) {
                Vector3 prevEnd = (start + angledDir * slantHeightOfCone);
                angledDir = Quaternion.AngleAxis(360f / numLinesOnCone, currentDirection) * angledDir;
                angledDir = angledDir.normalized;
                Vector3 currEnd = (start + angledDir * slantHeightOfCone);

                Gizmos.DrawLine(start, prevEnd);
                Gizmos.DrawLine(prevEnd, currEnd);
            }
        }
    }
}
