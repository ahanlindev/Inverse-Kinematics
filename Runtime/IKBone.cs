using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ahanlindev
{
    public class IKBone : MonoBehaviour
    {
        [SerializeField] private HashSet<IKBone> childBones;
        // Start is called before the first frame update
        void Start()
        {
            childBones = new HashSet<IKBone>();
            updateChildBones();
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        private void updateChildBones()
        {
            foreach (Transform child in transform)
            {
                IKBone childBone = child.GetComponent<IKBone>();
                if (childBone != null && !child.Equals(transform))
                {
                    childBones.Add(childBone);
                }
            }
        }
    }
}
