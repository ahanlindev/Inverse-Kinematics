using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ahanlindev
{
    public class IKChain : MonoBehaviour
    {
        [Tooltip("The end effector of the kinematic chain. MUST BE A DESCENDANT OF THE START")]
        [SerializeField] private Transform _endEffector;

        [Tooltip("Object or point in space targeted by the end effector")]
        [SerializeField] private Transform _target;

        [Tooltip("If true, kinematic chain positions will be updated in FixedUpdate. If false, they will be updated in Update")]
        [SerializeField] private bool _iterateInFixedUpdate = true;

        [Tooltip("If gizmos are enabled, represent the chain as lines between the bones")]
        
        [SerializeField] private bool _drawChain;
        
        [HideInInspector] private Transform _startJoint;

        [SerializeField] private List<Transform> _jointPositions; // index 0 is end effector
        
        [HideInInspector] private bool _isValid = true; // marked false if anything is broken

        private void Start() {
            _startJoint = transform;
            CheckValidity();
            initPositionList();
        }

        private void Update() {
            if (!_iterateInFixedUpdate) {
                PerformFABRIK();
            }    
        }

        private void FixedUpdate() {
            if (_iterateInFixedUpdate) {
                PerformFABRIK();
            }
        }

        private void OnDrawGizmos() {
            if (_drawChain) {
                DrawChainGizmo();
            }
        }
        /** 
         * Performs the FABRIK algorithm to determine locations for each joint. 
         * If the chain is invalid, returns immediately.
         */
        public void PerformFABRIK() {
            if (!_isValid) {return;}
            List<Transform> jointPositions = new List<Transform>(); // TODO: put list init in its own method
            Transform current = _endEffector.transform;
            // traverse up hierarchy to the root of the chain
            while(!current.Equals(_startJoint)) {
                jointPositions.Add(current);
                current = current.parent;
            }
                jointPositions.Reverse();
        }
        /**
         * Checks that each field of this object contains valid data
         */
        public bool CheckValidity() {
            bool MISSING_END_EFFECTOR = _endEffector == null;
            bool NON_DESCENDANT_EE = !(MISSING_END_EFFECTOR);

            IKJoint[] children = _startJoint.GetComponentsInChildren<IKJoint>();
            foreach (IKJoint child in children)
            {
                if (child.Equals(_endEffector))
                {
                    NON_DESCENDANT_EE = false;
                    break;
                }
            }

            _isValid = !(MISSING_END_EFFECTOR || NON_DESCENDANT_EE);
            if (!_isValid)
            {
                Debug.LogWarning("Invalid Kinematic Chain. \nExpand for details." + 
                    (MISSING_END_EFFECTOR ? "\n  - Missing End Effector" : "") +
                    (NON_DESCENDANT_EE ? "\n  - End Effector is not a descendant of Start Bone" : "")
                );
            } 
            return _isValid;
        }
        
        /**
         * Initializes the list of transforms that form the joints between this object and the end effector
         */
         private void initPositionList() {
            if (!_isValid) {return;}
            _jointPositions = new List<Transform>();
            Transform current = _endEffector;

            // traverse up hierarchy to the root of the chain
            while(!current.Equals(_startJoint)) {
                _jointPositions.Add(current);
                current = current.parent;
            }
        }

        /** 
         * Draws a magenta line between the joints of this kinematic chain
         * MUST BE CALLED IN OnDrawGizmos or OnDrawGizmosSelected!
         */
        public void DrawChainGizmo() {
            if (_isValid) {
                Transform last = _endEffector.transform;
                Transform current = last;
                while (current != _startJoint.transform)
                {
                    current = last.parent.transform;
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(last.transform.position, current.transform.position);
                    last = current;
                    if (current == null) Debug.LogError("Null bone in kinematic chain");
                }
            }
        }
    }
}
