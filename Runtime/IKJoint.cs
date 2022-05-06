using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ahanlindev
{
    public class IKJoint : MonoBehaviour
    {
        public Vector3 direction;
        public float maxAngle;
        
        #if UNITY_EDITOR
            private Quaternion initialParentRot; 
            private Vector3 initialParentDir;

            [Header("Debug UI options")]
            [SerializeField] private int numLinesOnCone = 50;
            [SerializeField] private float lengthOfCone = 1.5f;
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
            if (transform.parent != null) {
                initialParentRot = transform.parent.rotation;
                initialParentDir = (transform.position - transform.parent.position).normalized;
            } else {
                initialParentRot = Quaternion.identity;
                initialParentDir = Vector3.zero;
            }
        }
        /**
         * Returns the direction vector that marks the center of the mobility cone
         * This vector does not account for any rotations upon the parent of this joint
         * This vector will be normalized.
         */
        public Vector3 GetDirection() {
            return direction.normalized;
        }


        /**
         * Draws a cone that represents the mobility range of the joint.
         * Can only be called in OnDrawGizmos
         */
        private void DrawConeGizmo() {
            if(!isActiveAndEnabled) {return;}

            Vector3 currentDirection;
            if (transform.parent != null) {
                currentDirection = (transform.parent.rotation * Quaternion.Inverse(initialParentRot)) * direction;
            } else {
                currentDirection = direction;
            }
            Vector3 start = transform.position;
            Vector3 end = start + (currentDirection * lengthOfCone);
 
            Vector3 ortho = Vector3.Cross(currentDirection, Vector3.right);
            if (ortho == Vector3.zero) ortho = Vector3.Cross(currentDirection, Vector3.up);

            Vector3 angledDir = Vector3.RotateTowards(currentDirection, -currentDirection, Mathf.Deg2Rad * maxAngle, 0f);
            angledDir = angledDir.normalized;

            Gizmos.color = gizmoColor; 
            Gizmos.DrawLine(start, end);
            
            // Create a pseudo-cone of lines
            for(int i = 0; i < numLinesOnCone; i++) {
                Vector3 prevEnd = (start + angledDir * lengthOfCone);
                angledDir = Quaternion.AngleAxis(360f / numLinesOnCone, currentDirection) * angledDir;
                angledDir = angledDir.normalized;
                Vector3 currEnd = (start + angledDir * lengthOfCone);

                Gizmos.DrawLine(start, prevEnd);
                Gizmos.DrawLine(prevEnd, currEnd);
            }
        }
    }
}
