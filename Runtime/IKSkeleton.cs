using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ahanlindev
{
    [Serializable]
    public class KinematicChain
    {
        [Tooltip("If gizmos are enabled, represent the chain as lines between the bones")]
        [SerializeField] private bool _drawChain;

        [Tooltip("A string representing the name of this kinematic chain")]
        [SerializeField] private string _name = "Kinematic Chain";

        [Tooltip("The highest bone on the kinematic chain")]
        [SerializeField] private Transform _startJoint;

        [Tooltip("The end effector of the kinematic chain. MUST BE A DESCENDANT OF THE START")]
        [SerializeField] private Transform _endEffector;

        [Tooltip("Object or point in space targeted by the end effector")]
        [SerializeField] private Transform _target;
        [HideInInspector] private bool _isValid = true; // marked false if anything is broken


        /**
         * Checks that each field of this object contains valid data
         * @param warnIfInvalid: If true, prints a warning to the console if 
         * the chain is deemed invalid
         */
        public bool CheckValidity(bool warnIfInvalid) {
            bool MISSING_NAME = _name.Equals("");
            bool MISSING_END_EFFECTOR = _endEffector == null;
            bool MISSING_START_BONE = _startJoint == null;
            bool NON_DESCENDANT_EE = !(MISSING_END_EFFECTOR || MISSING_START_BONE);

            if (!MISSING_START_BONE)
            {
                IKJoint[] children = _startJoint.GetComponentsInChildren<IKJoint>();
                foreach (IKJoint child in children)
                {
                    if (child.Equals(_endEffector))
                    {
                        NON_DESCENDANT_EE = false;
                        break;
                    }
                }
            }
            _isValid = !(MISSING_NAME || MISSING_END_EFFECTOR || MISSING_START_BONE || NON_DESCENDANT_EE);
            if (!_isValid)
            {
                Debug.LogWarning("Invalid Kinematic Chain \"" + _name + "\". \nExpand for details." + 
                    (MISSING_NAME ? "\n  - Missing Name" : " ") + 
                    (MISSING_START_BONE ? "\n  - Missing Start Bone" : " ") + 
                    (MISSING_END_EFFECTOR ? "\n  - Missing End Effector" : " ") +
                    (NON_DESCENDANT_EE ? "\n  - End Effector is not a descendant of Start Bone" : " ")
                );
            } 
            return _isValid;
        }
        
        /** 
         * Draws a magenta line between the joints of this kinematic chain
         * MUST BE CALLED IN OnDrawGizmos or OnDrawGizmosSelected!
         * If showChain is false, this will effectively do nothing
         */
        public void DrawChainGizmo() {
            if (_drawChain && _isValid) {
                Transform last = _endEffector.transform;
                Transform current = last;
                int debugctr = 0;
                while (current != _startJoint.transform && debugctr++ < 500)
                {
                    current = last.parent.transform;
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(last.transform.position, current.transform.position);
                    last = current;
                    if (current == null) Debug.LogError("Null bone in kinematic chain \"" + _name + "\"");
                }
            }
        }

    }

    public class IKSkeleton : MonoBehaviour
    {
        [Tooltip("If true, kinematic chain positions will be updated in FixedUpdate. If false, they will be updated in Update")]
        [SerializeField] private bool _iterateInFixedUpdate = true;

        [Tooltip("If true, prints a debug warning to the console when a chain contains invald parameters")]
        [SerializeField] private bool _logInvalidChains = true;

        [Tooltip("The list of kinematic chains that are managed by this skeleton")]
        [SerializeField] private List<KinematicChain> _chains;


        // Start is called before the first frame update
        void Start()
        {
            foreach (KinematicChain chain in _chains) {
                chain.CheckValidity(_logInvalidChains);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!_iterateInFixedUpdate) {

            }
        }

        private void FixedUpdate() {
            if (_iterateInFixedUpdate) {

            }
        }

        private void OnDrawGizmos()
        {
            foreach (KinematicChain chain in _chains)
            {
                chain.DrawChainGizmo();
            }
        }
    }
}
