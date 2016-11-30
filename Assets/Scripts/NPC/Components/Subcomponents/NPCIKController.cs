using UnityEngine;
using System.Collections;


namespace NPC {

    [RequireComponent(typeof(Animator))]
    [System.Serializable]
    public class NPCIKController : MonoBehaviour {

        #region Members

        /* Animator gAnimator */
        Animator gAnimator;
        NPCBody g_NPCBody;
        NPCController g_NPCController;
        
        /* Targets */
        public Transform LOOK_AT_TARGET;
        public Transform GRAB_RIGHT_TARGET;

        Vector3 g_RightFootPosition,
            g_LeftFootPosition;

        Quaternion g_RightFootRotation,
            g_LeftFootRotation;

        /* Hints */
        [SerializeField]
        Transform HINT_LEFT_KNEE;
        [SerializeField]
        Transform HINT_RIGHT_KNEE;

        /* Weights*/
        public float IK_WEIGHT;
        public float IK_RIGHT_FOOT_WEIGHT = 0f;
        public float IK_LEFT_FOOT_WEIGHT = 0f;
        public float MAX_LOOK_WEIGHT = 1f;

        private float g_CurrentLookWeight = 0.0f;
        private float g_LookSmoothness = 50.0f;
        private bool g_FeetIK = false;
        RaycastHit g_RayHit;
        private static string m_AnimatorRightFootParam = "IK_Right_Foot";
        private static string m_AnimatorLeftFootParam = "IK_Left_Foot";
        private float g_ColliderRadiusCorrection;

        /* Enable disabled IK and COmponents during runtime */
        public bool IK_ACTIVE;
        public float REACH_DISTANCE = 0.5f;

        /* Bones */
        [SerializeField]
        Transform HEAD;
        [SerializeField]
        Transform RIGHT_HAND;
        [SerializeField]
        Transform LEFT_HAND;
        [SerializeField]
        Transform RIGHT_FOOT;
        [SerializeField]
        Transform LEFT_FOOT;
        [SerializeField]
        Transform LEFT_KNEE;
        [SerializeField]
        Transform RIGHT_KNEE;
        #endregion

        #region Properties
        public Transform Head {
            get {
                return HEAD;
            }
        }
        #endregion

        

        #region Unity_Functions

        public void Reset() {
            gAnimator = gameObject.GetComponent<Animator>();
            if (gAnimator == null) {
                Debug.Log("NPCIKController --> An animator controller is needed for IK");
                this.enabled = false;
            } else {
                gAnimator.applyRootMotion = true;
            }

            // Initialize Bones
            RIGHT_HAND = gAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            LEFT_HAND = gAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            RIGHT_FOOT = gAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            LEFT_FOOT = gAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            HEAD = gAnimator.GetBoneTransform(HumanBodyBones.Head);
            LEFT_KNEE = gAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            RIGHT_KNEE = gAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg);

            // Initialize Hints
            HINT_LEFT_KNEE = new GameObject().transform;
            HINT_RIGHT_KNEE = new GameObject().transform;
            HINT_LEFT_KNEE.gameObject.name = "IK_HINT_Left_Knee";
            HINT_RIGHT_KNEE.gameObject.name = "IK_HINT_Right_Knee";
            HINT_LEFT_KNEE.parent = gAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            HINT_RIGHT_KNEE.parent = gAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            HINT_LEFT_KNEE.localRotation = gAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).rotation;
            HINT_RIGHT_KNEE.localRotation = gAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg).rotation;
            HINT_LEFT_KNEE.localPosition = Vector3.zero;
            HINT_RIGHT_KNEE.localPosition = Vector3.zero;
        }

        // Use this for initialization
        void Start() {

            g_NPCController = GetComponent<NPCController>();
            g_NPCBody = g_NPCController.Body;
            gAnimator = GetComponent<Animator>();

            if (gAnimator == null) {
                g_NPCController.Debug("NPCIKController --> An animator controller is needed for IK, disabling component during runtime");
                this.enabled = false;
            }

            // default weight
            IK_WEIGHT   = IK_WEIGHT < 0.1f ? 1f : IK_WEIGHT;
            g_ColliderRadiusCorrection = GetComponent<CapsuleCollider>().radius;
          
        }

        // Unity's main IK method called every frame
        void OnAnimatorIK() {
            if(g_NPCBody.IKEnabled) {
                
                /* Feet */
                if(g_FeetIK)
                    DoFeetIK();

                /* Look At */
                DoLookAt();
            }
        }

        #endregion

        #region Private_Functions

        private void DoLookAt() {
            /* Do look IK */
            if (LOOK_AT_TARGET != null) {
                gAnimator.SetLookAtPosition(LOOK_AT_TARGET.position);
                g_CurrentLookWeight = Mathf.Max(Mathf.Lerp(0.0f, 1.0f, Time.deltaTime * g_LookSmoothness), MAX_LOOK_WEIGHT);
                gAnimator.SetLookAtWeight(g_CurrentLookWeight);
            } else {
                g_CurrentLookWeight = Mathf.Lerp(1.0f, 0.0f, Time.deltaTime * g_LookSmoothness);
            }
        }

        private void DoFeetIK() {
            
            // Using animation curves - walk and idle
            if(g_NPCBody.Speed == 0 && g_NPCBody.Orientation == 0) {
                IK_RIGHT_FOOT_WEIGHT = IK_LEFT_FOOT_WEIGHT = 0.5f;
            } else {
                IK_RIGHT_FOOT_WEIGHT = gAnimator.GetFloat(m_AnimatorRightFootParam);
                IK_LEFT_FOOT_WEIGHT = gAnimator.GetFloat(m_AnimatorLeftFootParam);
            }

            // Adjust Hints
            if (g_NPCBody.IK_USE_HINTS) {
                gAnimator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 0.5f);
                gAnimator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 0.5f);
                gAnimator.SetIKHintPosition(AvatarIKHint.RightKnee, HINT_RIGHT_KNEE.position);
                gAnimator.SetIKHintPosition(AvatarIKHint.LeftKnee, HINT_LEFT_KNEE.position);
            }

            // IK Feet Position Weight
            gAnimator.SetIKPositionWeight(AvatarIKGoal.RightFoot, IK_RIGHT_FOOT_WEIGHT);
            gAnimator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, IK_LEFT_FOOT_WEIGHT);

            // IK Feet Rotation Weight
            gAnimator.SetIKRotationWeight(AvatarIKGoal.RightFoot, IK_RIGHT_FOOT_WEIGHT);
            gAnimator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, IK_LEFT_FOOT_WEIGHT);

            // Feet Position
            gAnimator.SetIKPosition(AvatarIKGoal.RightFoot, g_RightFootPosition);
            gAnimator.SetIKPosition(AvatarIKGoal.LeftFoot, g_LeftFootPosition);

            // Feet Rotation
            gAnimator.SetIKRotation(AvatarIKGoal.RightFoot, g_RightFootRotation);
            gAnimator.SetIKRotation(AvatarIKGoal.LeftFoot, g_LeftFootRotation);
        }

        #endregion

        #region Public_Functions

        /// <summary>
        /// Body calls UpdateIK when UpdateBody is called from the controller
        /// </summary>
        public void UpdateIK() {

            Vector3 heightCorrection = (Vector3.up * g_NPCBody.IK_FEET_HEIGHT_EFFECTOR_CORRECTOR);

            // Update feet
            g_FeetIK = Physics.Raycast(RIGHT_FOOT.position + heightCorrection, Vector3.down, out g_RayHit);
            g_RightFootPosition = Vector3.Lerp(g_RightFootPosition,
                g_RayHit.point + (Vector3.up * g_NPCBody.IK_FEET_HEIGHT_CORRECTION) + (transform.forward * g_NPCBody.IK_FEET_FORWARD_CORRECTION), Time.deltaTime * 15f);
            g_RightFootRotation = Quaternion.FromToRotation(Vector3.up, g_RayHit.normal) * transform.rotation;

            g_FeetIK = Physics.Raycast(LEFT_FOOT.position + heightCorrection, Vector3.down, out g_RayHit);
            g_LeftFootPosition = Vector3.Lerp(g_LeftFootPosition,
                g_RayHit.point + (Vector3.up * g_NPCBody.IK_FEET_HEIGHT_CORRECTION) + (transform.forward * g_NPCBody.IK_FEET_FORWARD_CORRECTION), Time.deltaTime * 15f);
            g_LeftFootRotation = Quaternion.FromToRotation(Vector3.up, g_RayHit.normal) * transform.rotation;

            Debug.DrawRay(LEFT_FOOT.position + heightCorrection, Vector3.down, Color.red);
            Debug.DrawRay(RIGHT_FOOT.position + heightCorrection, Vector3.down, Color.red);
            Debug.DrawRay(transform.position + heightCorrection + (transform.forward * g_ColliderRadiusCorrection), (transform.forward + Vector3.down * 0.2f));

            if (Physics.Raycast(transform.position + heightCorrection + (transform.forward * g_ColliderRadiusCorrection), (transform.forward + Vector3.down * 0.4f), out g_RayHit, 0.2f)) {
                if(g_NPCBody.Speed > 0f) {
                    transform.position = Vector3.Lerp(transform.position,
                        new Vector3(transform.position.x, transform.position.y +  (g_RayHit.transform.localScale.y), transform.position.z), Time.deltaTime * 10f);
                } else {
                    g_RightFootPosition = new Vector3(g_RayHit.point.x, g_RayHit.transform.position.y + g_RayHit.transform.localScale.y, g_RayHit.point.z);
                }
            } else {
                GetComponent<CapsuleCollider>().height = 1.8f;
            }

        }


        public bool CanBeReached(IPerceivable per) {
            return (Vector3.Distance(per.GetTransform().position, RIGHT_HAND.position) <= REACH_DISTANCE);
        } 

        public bool ReachFor(IPerceivable per) {
            return false;
        }
        
        #endregion
        
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
