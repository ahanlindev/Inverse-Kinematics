using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ahanlindev
{
    public class IKChain : MonoBehaviour
    {
        [Tooltip("The end effector of the kinematic chain. MUST BE A DESCENDANT OF THIS GAMEOBJECT")]
        public Transform endEffector;

        [Tooltip("Object targeted by the end effector")]
        public Transform target;

        [Tooltip("Acceptable distance from target if target is reachable")]
        public float tolerance = 1e-6f;

        [Tooltip("If true, joint positions will be updated in FixedUpdate. If false, they will be updated in Update")]
        [SerializeField] private bool _iterateInFixedUpdate = true;

        [Tooltip("If gizmos are enabled, represent the chain as lines between the joints")]
        [SerializeField] private bool _drawChain;
        
        // Transform of this GameObject
        [HideInInspector] private Transform _rootJoint; 

        // index 0 is the position of the root
        [HideInInspector] private List<Transform> _jointTransforms; 

        // index 0 is distance between root and its child in the chain. Updated in FABRIK
        [HideInInspector] private List<float> _jointDistances;

        // TODO: make list of IKJoints representing constraints. Null if none exists
        
        // marked false if anything is broken
        [HideInInspector] private bool _isValid = true; 
    
        private void Awake() {
            _rootJoint = transform;
            CheckValidity();
            initTransformList();
        }

        [ExecuteInEditMode]
        private void OnValidate() {
            _rootJoint = transform;
            CheckValidity();
        }

        private void Update() {
            if (!_iterateInFixedUpdate && _isValid) {
                PerformFABRIK();
            }    
        }

        private void FixedUpdate() {
            if (_iterateInFixedUpdate && _isValid) {
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
        private void PerformFABRIK() {
            // Get distance from root and make sure its reachable with sum of joint distances
            float targetDist = Vector3.Distance(_rootJoint.position, target.position);
            float reachableDist = 0;
            _jointDistances = new List<float>(); // clear list of distances and rebuild it
            for(int i = 0; i < _jointTransforms.Count - 1; i++) {
                float dist = Vector3.Distance(_jointTransforms[i].position, _jointTransforms[i+1].position);
                _jointDistances.Add(dist);
                reachableDist += dist; 
            }
            if (reachableDist < targetDist) {
                HandleUnreachableTarget();
            } else {
                HandleReachableTarget();
            }
        }   

        /**
         * 
         * 
         */
        private void HandleUnreachableTarget() {
            for(int i = 0; i < _jointDistances.Count; i++) {
                Transform parent = _jointTransforms[i];
                Transform child = _jointTransforms[i+1];
                // get distance between target and joint i
                float dist = Vector3.Distance(target.position, parent.position);
                float lambda = _jointDistances[i] / dist;

                // set position of (i+1)th joint based on distance
                child.position = (1 - lambda) * parent.position + lambda * target.position;
            }
        }


        private void HandleReachableTarget() {

        }

        /**
         * Checks that each field of this object contains valid data
         */
        private bool CheckValidity() {
            bool MISSING_END_EFFECTOR = endEffector == null;
            bool NON_DESCENDANT_EE = !(MISSING_END_EFFECTOR);
            
            if (!MISSING_END_EFFECTOR) {
                Transform current = endEffector;
                while (current != null && !current.Equals(transform))
                {
                    current = current.parent;
                    if (transform.Equals(current))
                    {
                        NON_DESCENDANT_EE = false;
                    }
                }
            }

            _isValid = !(MISSING_END_EFFECTOR || NON_DESCENDANT_EE);
            if (!_isValid)
            {
                Debug.LogWarning("Invalid Kinematic Chain. " + 
                    (MISSING_END_EFFECTOR ? "- Missing End Effector" : "") +
                    (NON_DESCENDANT_EE ? "- End Effector is not a descendant of Start Bone" : "")
                );
            } 
            return _isValid;
        }
        
        /**
         * Initializes the list of transforms and distances that form the joints 
         * between this object and the end effector
         * Prerequisite: This IKChain must be valid
         */
         private void initTransformList() {
            if (!_isValid) {return;}
            _jointTransforms = new List<Transform>();
            Transform current = endEffector;
            Transform prev = endEffector;
            // traverse up hierarchy to the root of the chain
            while(current != null && !current.Equals(_rootJoint.parent)) {
                _jointTransforms.Add(current);
                prev = current;
                current = current.parent;
            }
            // loop puts end effector at index 0. Algorithm is easier if root is at 0
            _jointTransforms.Reverse();
        }

        /** 
         * Draws a magenta line between the joints of this kinematic chain
         * MUST BE CALLED IN OnDrawGizmos or OnDrawGizmosSelected!
         */
        private void DrawChainGizmo() {
            if (_isValid) {
                Transform last = endEffector.transform;
                Transform current = last;
                while (current != _rootJoint.transform)
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
