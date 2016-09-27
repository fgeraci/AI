using UnityEngine;
using System.Collections;


namespace NPC {

    [RequireComponent(typeof(Animator))]
    [System.Serializable]
    public class NPCIKController : MonoBehaviour {

        //   Animator gAnimator;
        Animator gAnimator;
        NPCBody g_NPCBody;

        /* Test Targets */
        public Transform IK_TARGET_LEFT_FOOT;
        public Transform IK_TARGET_RIGHT_FOOT;
        public Transform LOOK_AT_TARGET;
        public Transform GRAB_RIGHT_TARGET;

        /* Hints */
        public Transform HINT_LEFT_KNEE;
        public Transform HINT_RIGHT_KNEE;

        /* Weights*/
        public float IK_WEIGHT;

        private float g_CurrentLookWeight = 0.0f;
        private float g_LookSmoothness = 50.0f;

        /* Enable disabled IK and COmponents during runtime */
        public bool IK_ACTIVE;
        public bool USE_HINTS;

        public Transform Head {
            get {
                return HEAD;
            }
        }

        /* Bones */
        Transform HEAD;
        Transform RIGHT_HAND;
        Transform LEFT_HAND;
        Transform RIGHT_FOOT;
        Transform LEFT_FOOT;


        public void Reset() {
            gAnimator = gameObject.GetComponent<Animator>();
            if (gAnimator == null) {
                Debug.Log("NPCIKController --> An animator controller is needed for IK");
                this.enabled = false;
            } else {
                gAnimator.applyRootMotion = true;
            }
        }

        // Use this for initialization
        void Start() {
            g_NPCBody = GetComponent<NPCBody>();
            gAnimator = GetComponent<Animator>();

            if (gAnimator == null) {
                Debug.Log("NPCIKController --> An animator controller is needed for IK, disabling component during runtime");
                this.enabled = false;
            }

            // Find Bones
            RIGHT_HAND = gAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            LEFT_HAND = gAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            RIGHT_FOOT = gAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            LEFT_FOOT = gAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            HEAD = gAnimator.GetBoneTransform(HumanBodyBones.Head);

            // default weight
            IK_WEIGHT = IK_WEIGHT < 0.1f ? 1f : IK_WEIGHT;
        }

        // Unity's main IK method called every frame
        void OnAnimatorIK() {
            if(g_NPCBody.IKEnabled) {
                /* Feet */
                // DoFeetIK();

                /* Do look IK */
                if (LOOK_AT_TARGET != null) {
                    gAnimator.SetLookAtPosition(LOOK_AT_TARGET.position);
                    g_CurrentLookWeight = Mathf.Lerp(0.0f, 1.0f, Time.deltaTime * g_LookSmoothness);
                    gAnimator.SetLookAtWeight(g_CurrentLookWeight);
                } else {
                    g_CurrentLookWeight = Mathf.Lerp(1.0f, 0.0f, Time.deltaTime * g_LookSmoothness);
                }
            }
        }

        private void DoFeetIK() {
            // set: Weights, Positions and Hints (of joints) and Rorations

            // Main weights
            gAnimator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, IK_WEIGHT);
            gAnimator.SetIKPositionWeight(AvatarIKGoal.RightFoot, IK_WEIGHT);
            // position
            gAnimator.SetIKPosition(AvatarIKGoal.LeftFoot, IK_TARGET_LEFT_FOOT.position);
            gAnimator.SetIKPosition(AvatarIKGoal.RightFoot, IK_TARGET_RIGHT_FOOT.position);
            // hints
            if (USE_HINTS) {
                gAnimator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, IK_WEIGHT);
                gAnimator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, IK_WEIGHT);
                gAnimator.SetIKHintPosition(AvatarIKHint.LeftKnee, HINT_LEFT_KNEE.position);
                gAnimator.SetIKHintPosition(AvatarIKHint.RightKnee, HINT_RIGHT_KNEE.position);
            }
            // rotation
            gAnimator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, IK_WEIGHT);
            gAnimator.SetIKRotationWeight(AvatarIKGoal.RightFoot, IK_WEIGHT);
            gAnimator.SetIKRotation(AvatarIKGoal.LeftFoot, IK_TARGET_LEFT_FOOT.rotation);
            gAnimator.SetIKRotation(AvatarIKGoal.RightFoot, IK_TARGET_RIGHT_FOOT.rotation);
        }

        Transform LOOK_AT {
            get {
                return this.LOOK_AT_TARGET;
            }
            set {
                this.LOOK_AT_TARGET = value;
            }
        }

        Transform GRAB_RIGHT {
            get {
                return this.GRAB_RIGHT_TARGET;
            }
            set {
                this.GRAB_RIGHT_TARGET = value;
            }
        }
    }

}
