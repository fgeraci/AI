using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NPC {

    [System.Serializable]
    public class NPCPerception : MonoBehaviour {

        #region Members
        NPCController g_Controller;
        private static string PERCEPTION_LAYER = "Ignore Raycast";
        private static string PERCEPTION_FIELD_OBJECT = "PerpcetionField";
        private IPerceivable g_CurrentlyPerceivedTarget;
        private bool g_Perceiving;
        private Dictionary<GameObject,IPerceivable> g_PerceivingMap;
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
        #endregion

        #region Unity_Methods
        void Reset() {
            Debug.Log("Initializing NPCPerception...");
            // add perception fields
            g_Controller = gameObject.GetComponent<NPCController>();
            GameObject pf;
            Component sCol = g_Controller.GetComponent(PERCEPTION_FIELD_OBJECT);
            if (sCol == null) {
                // take into account not readding a duplicate Sphere Collider in the same layer
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
            g_Perceiving = false;
            g_CurrentlyPerceivedTarget = null;
        }

        void OnTriggerEnter(Collider col) {
            IPerceivable p = col as IPerceivable;
            if (p != null) {
                Debug.Log("I see an " + col.name);
                g_PerceivingMap.Add(col.gameObject, p);
            }
        }

        void OnTriggerExit(Collider col) {
            IPerceivable p = col as IPerceivable;
            if (p != null && g_PerceivingMap.ContainsValue(p)) {
                Debug.Log("I can't see the " + col.name + " no more");
                g_PerceivingMap.Remove(col.gameObject);
            }
        }

        #endregion

        #region Public_Functions
        public void UpdatePerception() {
            // we will be throwing rays here
        }
        public float CalculatePerceptionWeight(IPerceivable p) {
            return 0f;
        }
        #endregion
        
    }

}