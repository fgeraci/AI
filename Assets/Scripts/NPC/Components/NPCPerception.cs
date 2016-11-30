using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NPC {

    [System.Serializable]
    public class NPCPerception : MonoBehaviour {

        #region Members
        [SerializeField]
        NPCController g_Controller;
        [SerializeField]
        Transform g_Head;
        private static string PERCEPTION_LAYER = "Ignore Raycast";
        private static string PERCEPTION_FIELD_OBJECT = "PerpcetionField";
        private IPerceivable g_CurrentlyPerceivedTarget;
        private bool g_Perceiving;
        private float g_HalfViewAngle;
        #endregion

        #region Static Fields
        public static float MIN_VIEW_ANGLE = 75f;
        public static float MAX_VIEW_ANGLE = 180f;
        public static float MIN_PERCEPTION_FIELD = 2f;
        public static float MAX_PERCEPTION_FIELD = 10f;
        #endregion

        #region Perception
        [SerializeField]
        private SphereCollider gPerceptionField;

        [SerializeField]
        private float gViewAngle = 135f;
        
        private HashSet<IPerceivable> g_PerceivedObjects;
        private HashSet<IPerceivable> g_PerceivedNPCAgents;
        #endregion

        #region Properties

        public float ViewAngle {
            get { return this.gViewAngle; }
            set { this.gViewAngle = value; }
        }

    
        public float PerceptionRadius {
            get { return gPerceptionField.radius; }
            set { this.gPerceptionField.radius = value; }
        }

    
        public SphereCollider PerceptionField {
            get { return this.gPerceptionField; }
            set { gPerceptionField = value; }
        }

        public bool Perceiving {
            get { return g_Perceiving; }
        }

        public HashSet<IPerceivable> PerceivedEntities {
            get {
                return g_PerceivedObjects;
            }
        }

        public HashSet<IPerceivable> PerceivedAgents {
            get {
                return g_PerceivedNPCAgents;
            }
        }
        #endregion

        #region Unity_Methods
        void Reset() {
            g_Controller = GetComponent<NPCController>();
            g_Controller.Debug("Initializing NPCPerception...");
            // add perception fields
            g_Controller = gameObject.GetComponent<NPCController>();
            GameObject pf;
            Component sCol = g_Controller.GetComponent(PERCEPTION_FIELD_OBJECT);
            if (sCol == null) {
                // take into account not reading a duplicate Sphere Collider in the same layer
                pf = new GameObject();
            } else pf = sCol.gameObject;
            pf.name = PERCEPTION_FIELD_OBJECT;
            pf.layer = LayerMask.NameToLayer(PERCEPTION_LAYER);
            pf.transform.parent = g_Controller.transform;
            // reset transform
            pf.transform.rotation = g_Controller.transform.rotation;
            pf.transform.localPosition = Vector3.zero;
            pf.transform.localScale = Vector3.one;
            gPerceptionField = pf.AddComponent<SphereCollider>();
            gPerceptionField.isTrigger = true;
            gPerceptionField.radius = (MIN_PERCEPTION_FIELD + MAX_PERCEPTION_FIELD) / 4;
            gViewAngle = (MIN_VIEW_ANGLE + MAX_VIEW_ANGLE) / 2;
            // collisions / reach
        }
        void Start() {
            Animator aC = GetComponent<Animator>();
            g_Head = aC.GetBoneTransform(HumanBodyBones.Head);
            g_Controller = GetComponent<NPCController>();
            g_Perceiving = false;
            g_CurrentlyPerceivedTarget = null;
            g_PerceivedObjects = new HashSet<IPerceivable>();
            g_PerceivedNPCAgents = new HashSet<IPerceivable>();
            g_HalfViewAngle = ViewAngle / 2.0f;
        }

        void OnTriggerStay(Collider col) {
            IPerceivable p = col.GetComponent<IPerceivable>();
            if(p != null) {
                Vector3 diff = p.GetTransform().position - transform.position;
                float angle = Vector3.Angle(transform.forward, diff);
                if(!g_PerceivedObjects.Contains(p) && angle <= g_HalfViewAngle) {        
                    g_Controller.Debug("I see an " + p);
                    if (p.GetNPCEntityType() == PERCEIVEABLE_TYPE.NPC) g_PerceivedNPCAgents.Add(p);
                    g_PerceivedObjects.Add(p);
                } else if (angle >= g_HalfViewAngle) {
                    g_Controller.Debug("I can't see the " + p + " no more");
                    g_PerceivedObjects.Remove(p);
                }
            }
        }

        void OnTriggerExit(Collider col) {
            IPerceivable p = col.GetComponent<IPerceivable>();
            if (p != null && g_PerceivedObjects.Contains(p)) {
                g_Controller.Debug(p + " is not in perception range anymore");
                g_PerceivedObjects.Remove(p);
                if (p.GetNPCEntityType() == PERCEIVEABLE_TYPE.NPC) g_PerceivedNPCAgents.Remove(p);
            }
        }

        #endregion

        #region Public_Functions
        public void UpdateHalfViewAngle() {
            g_HalfViewAngle = ViewAngle / 2.0f;
        }

        public void UpdatePerception() {
            g_Perceiving = (g_PerceivedObjects.Count > 0);
            if(g_Controller.DebugMode) {
                foreach(IPerceivable p in g_PerceivedObjects) {
                    Debug.DrawLine(g_Head.position, p.GetTransform().position);
                }
            }
        }
        public float CalculatePerceptionWeight(IPerceivable p) {
            return 0f;
        }
        #endregion
        
    }

}