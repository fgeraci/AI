using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace NPC {
    
    #region Enums
    public enum LOCO_STATE {
        IDLE,
        FRONT,
        FORWARD,
        BACKWARDS,
        LEFT,
        RIGHT,
        RUN,
        WALK,
        DUCK,
        GROUND,
        JUMP,
        FALL
    }

    /// <summary>
    /// To add gestures, just add it to the animator controller then define it here with the NPCAnimation System.Attribute
    /// </summary>
    public enum GESTURE_CODE {
        [NPCAnimation("Gest_Acknowledge", ANIMATION_PARAM_TYPE.TRIGGER, ANIMATION_LAYER.GESTURE)]
        ACKNOWLEDGE,
        [NPCAnimation("Gest_Angry", ANIMATION_PARAM_TYPE.TRIGGER, ANIMATION_LAYER.GESTURE)]
        ANGRY,
        [NPCAnimation("Gest_Why", ANIMATION_PARAM_TYPE.TRIGGER, ANIMATION_LAYER.GESTURE)]
        WHY,
        [NPCAnimation("Gest_Short_Wave", ANIMATION_PARAM_TYPE.TRIGGER, ANIMATION_LAYER.GESTURE)]
        WAVE_HELLO,
        [NPCAnimation("Gest_Negate", ANIMATION_PARAM_TYPE.TRIGGER, ANIMATION_LAYER.GESTURE)]
        NEGATE,
        [NPCAnimation("Body_Die", ANIMATION_PARAM_TYPE.TRIGGER, ANIMATION_LAYER.FULL_BODY)]
        DIE,
        [NPCAnimation("Gest_Anger", ANIMATION_PARAM_TYPE.TRIGGER, ANIMATION_LAYER.GESTURE)]
        ANGER,
        [NPCAnimation("Gest_Dissapointment", ANIMATION_PARAM_TYPE.TRIGGER, ANIMATION_LAYER.GESTURE)]
        DISSAPOINTMENT,
        [NPCAnimation("Gest_Hurray", ANIMATION_PARAM_TYPE.TRIGGER, ANIMATION_LAYER.GESTURE)]
        HURRAY,
        [NPCAnimation("Gest_Grab_Front", ANIMATION_PARAM_TYPE.TRIGGER, ANIMATION_LAYER.GESTURE)]
        GRAB_FRONT
    }

    public enum NAV_STATE {
        DISABLED = 0,
        STEERING_NAV,
        NAVMESH_NAV
    }

    #endregion

    [System.Serializable]
    public class NPCBody : MonoBehaviour {
        
        #region Members

        [SerializeField]
        NavMeshAgent gNavMeshAgent;
        [SerializeField]
        Animator g_Animator;
        [SerializeField]
        NPCIKController gIKController;
        Rigidbody gRigidBody;
        CapsuleCollider gCapsuleCollider;

        private bool g_LookingAround = false;

        private Vector3 g_Velocity;
        private Vector3 g_Acceleration;

        private Vector3 g_TargetLocation;
        private Vector3 g_LastpdatedPosition;

        private static string g_AnimParamSpeed      = "Speed";
        private static string g_AnimParamDirection  = "Direction";
        private static string g_AnimParamJump       = "Jump";
        
        private static int   SPEED_MOD          =  2;
        private static float MAX_WALK__SPEED    =  1.00f;
        private static float MAX_RUN_SPEED      =  1.00f * SPEED_MOD;
        private static float MIN_WALK_SPEED     =  -1* MAX_WALK__SPEED;
        private static float MIN_RUN_SPEED      =  -1 * MAX_WALK__SPEED;

        private static Dictionary<GESTURE_CODE, NPCAnimation> m_Gestures;
        private static Dictionary<NPCAffordance, string> m_Affordances;

        private LOCO_STATE g_CurrentStateFwd    = LOCO_STATE.IDLE;
        private LOCO_STATE g_CurrentStateGnd    = LOCO_STATE.GROUND;
        private LOCO_STATE g_CurrentStateDir    = LOCO_STATE.FRONT;
        private LOCO_STATE g_CurrentStateMod    = LOCO_STATE.WALK;

        // This correlate with the parameters from the Animator
        private bool g_RunForce                 = false;
        private float g_CurrentSpeed            = 0.0f;
        private float g_CurrentVelocity         = 0.05f;
        private float g_TurningVelocity         = 0.05f;
        private float g_CurrentOrientation      = 0.0f;
        private static string m_ColliderHeight = "COLLIDER_Height";
        private bool g_TargetLocationReached= false;
        private static int gHashJump = Animator.StringToHash("JumpLoco");
        private static int gHashIdle = Animator.StringToHash("Idle");
        private Vector3 g_TargetOrientation;                                // Wheres the NPC currently looking at

        // navigation queue
        List<Vector3> g_NavQueue;

        [System.ComponentModel.DefaultValue(1f)]
        private float MaxWalkSpeed { get; set; }

        [System.ComponentModel.DefaultValue(2f)]
        private float MaxRunSpeed { get; set; }

        [System.ComponentModel.DefaultValue(-1f)]
        private float TurnLeftAngle { get; set; }

        [System.ComponentModel.DefaultValue(1f)]
        private float TurnRightAngle { get; set; }

        private NPCController g_NPCController;
        
        #endregion

        #region Properties

        public float AgentRepulsionWeight = 0.6f;

        public float DistanceTolerance  = 1f;
        
        public float Mass {
            get {
                return gRigidBody.mass;
            }
        }

        [SerializeField]
        public bool EnableSocialForces;

        public float AgentRadius {
            get {
                return gCapsuleCollider.radius;
            }
        }

        public Vector3 Velocity {
            get {
                return (transform.position - g_LastpdatedPosition) * Time.deltaTime;
            }
        }

        public float Speed {
            get {
                return g_CurrentSpeed;
            }
        }

        public float Orientation {
            get {
                return g_CurrentOrientation;
            }
        }

        public bool Navigating;

        [SerializeField]
        public NAV_STATE Navigation;

        [SerializeField]
        public bool UseCurves;

        [SerializeField]
        public bool IKEnabled;

        [SerializeField]
        public float IK_FEET_HEIGHT_CORRECTION;

        [SerializeField]
        public float IK_FEET_FORWARD_CORRECTION;

        [SerializeField]
        public float IK_FEET_HEIGHT_EFFECTOR_CORRECTOR;

        [SerializeField]
        public float IK_FEET_STAIRS_INTERPOLATION;

        [SerializeField]
        public bool IK_USE_HINTS = true;

        [SerializeField]
        public bool UseAnimatorController;

        [SerializeField]
        public float NavDistanceThreshold   = 0.3f;

        public bool LookingAround {
            get {
                return g_LookingAround;
            }
        }

        public Transform TargetObject {
            get {
                return gIKController.LOOK_AT_TARGET;
            }
        }

        public Transform Head {
            get {
                return gIKController.Head;
            }
        }
        
        public bool IsGesturePlaying(GESTURE_CODE gest) {
            return g_Animator.GetCurrentAnimatorStateInfo(0).shortNameHash == m_Gestures[gest].AnimationHash;
        }

        public bool IsAtTargetLocation(Vector3 targetLoc) {
            return g_TargetLocationReached 
                && targetLoc == g_TargetLocation;
        }

        public bool IsIdle {
            get {
                return
                    // We always need to test for a state and a possible active transition
                    g_Animator.GetCurrentAnimatorStateInfo(0).shortNameHash == gHashIdle
                    && g_Animator.GetAnimatorTransitionInfo(0).fullPathHash == 0;

            }
        }
        #endregion

        #region Unity_Methods
        void Reset() {
            g_NPCController = GetComponent<NPCController>();
            g_NPCController.Debug("Initializing NPCBody ... ");
            gNavMeshAgent = gameObject.GetComponent<NavMeshAgent>();
            gRigidBody = gameObject.GetComponent<Rigidbody>();
            g_Animator = gameObject.GetComponent<Animator>();
            gIKController = gameObject.GetComponent<NPCIKController>();
            if(gIKController == null) gIKController = gameObject.AddComponent<NPCIKController>();
            gIKController.hideFlags = HideFlags.HideInInspector;
            gCapsuleCollider = gameObject.GetComponent<CapsuleCollider>();
            if (gNavMeshAgent == null) {
                gNavMeshAgent = gameObject.AddComponent<NavMeshAgent>();
                gNavMeshAgent.autoBraking = true;
                gNavMeshAgent.enabled = false;
                g_NPCController.Debug("NPCBody requires a NavMeshAgent if navigation is on, adding a default one.");
            }
            if (g_Animator == null || g_Animator.runtimeAnimatorController == null) {
                g_NPCController.Debug("NPCBody --> Agent requires an Animator Controller!!! - consider adding the NPCDefaultAnimatorController");
            } else UseAnimatorController = true;
            if(gRigidBody == null) {
                gRigidBody = gameObject.AddComponent<Rigidbody>();
                gRigidBody.useGravity = true;
                gRigidBody.mass = 3;
                gRigidBody.constraints = RigidbodyConstraints.FreezeRotation;
            }
            if(gCapsuleCollider == null) {
                gCapsuleCollider = gameObject.AddComponent<CapsuleCollider>();
                gCapsuleCollider.radius = 0.3f;
                gCapsuleCollider.height = 1.5f;
                gCapsuleCollider.center = new Vector3(0.0f,0.75f,0.0f);
            }
            if(gIKController == null) {
                gIKController = gameObject.AddComponent<NPCIKController>();
            }
            g_NPCController.EntityType = PERCEIVEABLE_TYPE.NPC;
        }

        void Start() {
            g_NPCController = GetComponent<NPCController>();

            // Initialize static members for all NPC
            if(NPCBody.m_Gestures == null) {
                InitializeGestures();
            }
            if(NPCBody.m_Affordances == null) {
                InitializeAffordances();
            }
            g_Animator = gameObject.GetComponent<Animator>();
            gIKController = gameObject.GetComponent<NPCIKController>();
            gNavMeshAgent = gameObject.GetComponent<NavMeshAgent>();
            gCapsuleCollider = gameObject.GetComponent<CapsuleCollider>();
            gRigidBody = GetComponent<Rigidbody>();
            if (g_Animator == null || gNavMeshAgent == null) UseAnimatorController = false;
            if (gIKController == null) IKEnabled = false;
            g_NavQueue = new List<Vector3>();
            if(g_NPCController.TestTargetLocation != null) {
                GoTo( new List<Vector3>() { g_NPCController.TestTargetLocation.position } );
            }
            g_TargetOrientation = transform.position + transform.forward;
        }

        #endregion

        #region Public_Funtions
        public void UpdateBody() {
            
            if(IKEnabled) {
                gIKController.UpdateIK();
            }

            if(!g_NPCController.MainAgent) {
                UpdateNavigation();
                UpdateOrientation();
            }

            g_LastpdatedPosition = transform.position;

            if (UseAnimatorController) {
                
                // If accidentally checked
                if (g_Animator == null) {
                    g_NPCController.Debug("NPCBody --> No Animator in agent, disabling UseAnimatorController");
                    UseAnimatorController = false;
                    return;
                }

                // Is teh agent running while navigating
                if(g_RunForce) Move(LOCO_STATE.RUN);
                
                // handle mod
                float  forth    = g_CurrentStateFwd == LOCO_STATE.FORWARD ? 1.0f : -1.0f;
                float  orient   = g_CurrentStateDir == LOCO_STATE.RIGHT ? 1.0f : -1.0f;
                bool   duck     = (g_CurrentStateMod == LOCO_STATE.DUCK);
                float  topF     = (g_CurrentStateMod == LOCO_STATE.RUN || g_CurrentSpeed > MAX_WALK__SPEED)
                    ? MAX_RUN_SPEED : MAX_WALK__SPEED;


                // update forward
                if (g_CurrentStateFwd != LOCO_STATE.IDLE) {
                    if (g_CurrentSpeed > MAX_WALK__SPEED
                        && g_CurrentStateMod == LOCO_STATE.WALK) g_CurrentSpeed -= g_CurrentVelocity;
                    else g_CurrentSpeed = Mathf.Clamp(g_CurrentSpeed + (g_CurrentVelocity * forth), MIN_WALK_SPEED, topF);
                } else {
                    if(g_CurrentSpeed != 0.0f) {
                        float m = g_CurrentVelocity * (g_CurrentSpeed > 0.0f ? -1.0f : 1.0f);
                        float stopDelta = g_CurrentSpeed + m;
                        g_CurrentSpeed = Mathf.Abs(stopDelta) > 0.05f ? stopDelta : 0.0f;
                    }
                }

                // update direction
                if (g_CurrentStateDir != LOCO_STATE.FRONT) {
                    g_CurrentOrientation = Mathf.Clamp(g_CurrentOrientation + (g_TurningVelocity * orient), -1.0f, 1.0f);
                } else {
                    float m = g_TurningVelocity * (g_CurrentOrientation > 0.0f ? -1.0f : 1.0f);
                    g_CurrentOrientation += m;
                    g_CurrentOrientation = Mathf.Abs(g_CurrentOrientation) > (g_TurningVelocity * 2) ? g_CurrentOrientation : 0.0f;
                }

                // update ground
                if (g_CurrentStateGnd == LOCO_STATE.JUMP) {
                    g_Animator.SetTrigger(g_AnimParamJump);
                    g_CurrentStateGnd = LOCO_STATE.FALL;
                } else if(g_Animator.GetAnimatorTransitionInfo(0).fullPathHash == 0) {
                    // this is as long as we are not in jump state
                    g_CurrentStateGnd = LOCO_STATE.GROUND;
                }

                // apply curves if needed
                if(UseCurves) {
                }
            
                // set animator
            
                g_Animator.SetFloat(g_AnimParamSpeed, g_CurrentSpeed);
                g_Animator.SetFloat(g_AnimParamDirection, g_CurrentOrientation);

                // reset all states until updated again
                SetIdle();
            }
        }
        
        public void Move(LOCO_STATE s) {
            switch (s) {
                case LOCO_STATE.RUN:
                case LOCO_STATE.DUCK:
                case LOCO_STATE.WALK:
                    g_CurrentStateMod = s;
                    break;
                case LOCO_STATE.FORWARD:
                case LOCO_STATE.BACKWARDS:
                case LOCO_STATE.IDLE:
                    g_CurrentStateFwd = s;
                    break;
                case LOCO_STATE.RIGHT:
                case LOCO_STATE.LEFT:
                case LOCO_STATE.FRONT:
                    g_CurrentStateDir = s;
                    break;
                case LOCO_STATE.JUMP:
                    g_CurrentStateGnd = s;
                    break;
                default:
                    g_NPCController.Debug("NPCBody --> Invalid direction especified for ModifyMotion");
                    break;
            }
        }

        #region Affordances

        /// <summary>
        /// No path finding involved.
        /// </summary>
        /// <param name="location"></param>
        [NPCAffordance("WalkTowards")]
        public void WalkTowards(Vector3 location) {
            g_NavQueue.Clear();
            g_NavQueue.Add(location);
        }

        [NPCAffordance("RunTo")]
        public void RunTo(List<Vector3> location) {
            GoTo(location);
            g_RunForce = true;
        }

        /// <summary>
        /// The queue will we checked and followed every UpdateNavigation call
        /// </summary>
        /// <param name="List of locations to follow"></param>
        [NPCAffordance("GoTo")]
        public void GoTo(List<Vector3> location) {
            g_RunForce = false;
            Navigating = true;
            g_NavQueue.Clear();
            g_NavQueue = location;
        }

        [NPCAffordance("OrientTowards")]        
        public void OrientTowards(Vector3 target) {
            g_TargetOrientation = target;
        }

        [NPCAffordance("StartLookAt")]
        public void StartLookAt(Transform t) {
            gIKController.LOOK_AT_TARGET = t;
        }

        [NPCAffordance("StopLookAt")]
        public void StopLookAt() {
            gIKController.LOOK_AT_TARGET = null;
        }

        /// <summary>
        /// The caller might specify an optional parameter depeding on the type of animation.
        /// </summary>
        /// <param name="gesture"></param>
        /// <param name="o"></param>
        [NPCAffordance("DoGesture")]
        public void DoGesture(GESTURE_CODE gesture, System.Object o = null) {
            NPCAnimation anim = m_Gestures[gesture];
            switch(anim.ParamType) {
                case ANIMATION_PARAM_TYPE.TRIGGER:
                    g_Animator.SetTrigger(anim.Name);
                    break;
                case ANIMATION_PARAM_TYPE.BOOLEAN:
                    bool b = (bool) o;
                    g_Animator.SetBool(anim.Name, b);
                    break;
                case ANIMATION_PARAM_TYPE.FLOAT:
                    float f = (float) o;
                    g_Animator.SetFloat(anim.Name, f);
                    break;

            }
        }

        [NPCAffordance("StopNavigation")]
        public void StopNavigation() {
            g_RunForce = false;
            SetIdle();
            g_TargetOrientation = transform.position + transform.forward;
            g_NavQueue.Clear();
            Navigating = false;
        }

        /// <summary>
        /// Used to start and stop looking around
        /// </summary>
        /// <param name="startLooking"></param>
        public void LookAround(bool startLooking) {
            GameObject go;
            if (startLooking && !g_LookingAround) {
                go = new GameObject();
                go.name = "TmpLookAtTarget";
                Func<Vector3, Vector3> pos = np => (np + (1.50f * transform.forward));
                go.transform.position = pos(transform.position);
                go.transform.rotation = transform.rotation;
                go.transform.SetParent(transform);
                StartLookAt(go.transform);
                g_LookingAround = true;
            } else if (g_LookingAround) {
                go = gIKController.LOOK_AT_TARGET.gameObject;
                StopLookAt();
                DestroyImmediate(go);
                g_LookingAround = false;
            }
        }


        #endregion

        #endregion

        #region Private_Functions

        private void UpdateNavigation() {
            if (Navigation != NAV_STATE.DISABLED) {
                if (Navigation == NAV_STATE.STEERING_NAV) {
                    if (g_NavQueue.Count > 0) {
                        g_TargetLocationReached = false;
                        HandleSteering();
                    }
                } else {
                    g_TargetLocation = g_NavQueue[0];
                    g_NavQueue.Clear();
                    HandleNavAgent();
                }
            } else {
                Navigating = false;
            }
        }

        private void HandleNavAgent() {
            if (gNavMeshAgent != null) {
                if (!gNavMeshAgent.enabled)
                    gNavMeshAgent.enabled = true;
                if(g_NavQueue.Count > 0)
                    gNavMeshAgent.SetDestination(g_TargetLocation);
            }
        }

        private void HandleSteering() {
            g_TargetLocation = g_NavQueue[0];
            g_TargetOrientation = g_TargetLocation;
            float distance = Vector3.Distance(transform.position, g_TargetLocation);
            Vector3 targetDirection = g_TargetLocation - transform.position;
            if(EnableSocialForces) {
                ComputeSocialForces(ref targetDirection);
            }
            float angle = Vector3.Angle(targetDirection, transform.forward);
            LOCO_STATE d = Direction(targetDirection) < 1.0f ? LOCO_STATE.LEFT : LOCO_STATE.RIGHT;
            if (distance > NavDistanceThreshold) {
                if (angle > 45.0f
                    && g_CurrentStateFwd != LOCO_STATE.FORWARD) {
                    Move(d);
                } else {
                    Move(LOCO_STATE.FORWARD);
                    if (angle > 5.0f) {
                        Move(d);
                    } else Move(LOCO_STATE.FRONT);
                }
            } else {
                g_TargetOrientation = transform.position + transform.forward;
                g_NavQueue.RemoveAt(0);
                Navigating = g_TargetLocationReached = g_NavQueue.Count > 0;
            }
        }

        private float Direction(Vector3 direction) {
            Vector3 perp = Vector3.Cross(transform.forward, direction);
            float dir = Vector3.Dot(perp, transform.up);
            return dir > 0f ? 1.0f : (dir < 0 ? -1.0f : 0f);
        }

        private void SetIdle() {
            g_CurrentStateFwd = LOCO_STATE.IDLE;
            g_CurrentStateGnd = LOCO_STATE.GROUND;
            g_CurrentStateDir = LOCO_STATE.FRONT;
            g_CurrentStateMod = LOCO_STATE.WALK;
        }

        private void ComputeSocialForces(ref Vector3 currentTarget) {
            currentTarget = Vector3.Normalize(currentTarget);
            Vector3 preferredForce = Mass * ((currentTarget * g_CurrentSpeed) - Velocity) * Time.deltaTime;
            Vector3 repulsionForce = ComputeAgentsRepulsionForce() + ComputeWallsRepulsionForce();
            Vector3 proximityForce = ComputeProximityForce();
            currentTarget += preferredForce + repulsionForce;
        }

        private Vector3 ComputeAgentsRepulsionForce() {
            Vector3 totalForces = Vector3.zero;
            foreach(IPerceivable p in g_NPCController.Perception.PerceivedAgents) {
                float radii = AgentRadius + p.GetAgentRadius();
                float distance = Vector3.Distance(transform.position, p.GetPosition());
                // no collision --> skip
                if (distance >= (radii * DistanceTolerance))  continue;
                Vector3 normal = Vector3.Normalize(transform.position - p.GetPosition());
                // go back and right by default
                totalForces += normal * AgentRepulsionWeight;                               // -forward
                totalForces += Vector3.Cross(Vector3.up, normal) * AgentRepulsionWeight;    // right by default
            }
            return totalForces;
        }

        private Vector3 ComputeWallsRepulsionForce() {
            return Vector3.zero;
        }

        private Vector3 ComputeProximityForce() {
            Vector3 totalForce = Vector3.zero;
            foreach (IPerceivable p in g_NPCController.Perception.PerceivedEntities) {
                float distance = Vector3.Distance(transform.position, p.GetPosition());
                float radii = AgentRadius + p.GetAgentRadius();
                float scale = 0f;
                Vector3 away;
                if (p.GetNPCEntityType() == PERCEIVEABLE_TYPE.NPC) {
                    away = Vector3.Normalize(transform.position - p.GetPosition());
                    scale = Mathf.Exp(radii - distance);
                } else {
                    away = Vector3.Normalize(transform.position - p.GetPosition());
                    scale = Mathf.Exp(radii - distance);
                }
                totalForce += away * scale;
            }
            return totalForce;
        }

        private void UpdateOrientation() {
            if(g_CurrentStateFwd != LOCO_STATE.FORWARD && !Navigating) {
                Vector3 targetDirection = g_TargetOrientation - transform.position;
                Vector3 v1 = new Vector3(targetDirection.x, 0, targetDirection.z);
                float angle = Vector3.Angle(v1, transform.forward);
                LOCO_STATE d = Direction(targetDirection) < 1.0f ? LOCO_STATE.LEFT : LOCO_STATE.RIGHT;
                if (angle > 5.0f) {
                    Move(d);
                }
            }
        }

        /// <summary>
        /// Initialize all defined enum gestures by using reflection
        /// </summary>
        private void InitializeGestures() {
            Array a = Enum.GetValues(typeof(GESTURE_CODE));
            m_Gestures = new Dictionary<GESTURE_CODE, NPCAnimation>();
            foreach(var t in a) {
                Type type = t.GetType();
                var name = Enum.GetName(type, t);
                var att = type.GetField(name).GetCustomAttributes(typeof(NPCAnimation),false);
                NPCAnimation anim = (NPCAnimation)att[0];
                anim.AnimationHash = Animator.StringToHash(anim.Name);
                m_Gestures.Add((GESTURE_CODE)t, anim);
            }
            g_NPCController.Debug("modular NPC GESTURES successfully initialized: " + m_Gestures.Count);
        }

        /// <summary>
        /// Initialize all existing affordances
        /// </summary>
        private void InitializeAffordances() {
            Array a  = typeof(NPCBody).GetMethods();
            m_Affordances = new Dictionary<NPCAffordance, string>();
            foreach(MethodInfo m in a) {
                object[] att = m.GetCustomAttributes(typeof(NPCAffordance),false);
                if(att.Length == 1) {
                    NPCAffordance aff = (NPCAffordance)att[0];
                    m_Affordances.Add(aff, aff.Name);
                }
            }
            g_NPCController.Debug("modular NPC AFFORDANCES successfully initialized: " + m_Affordances.Count);
        }

        #endregion
    }

}
