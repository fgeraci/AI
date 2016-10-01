using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Pathfinding;


namespace NPC {
    
    public class NPCAI: MonoBehaviour {
        

        #region NPC_Modules
        private NPCController gNPCController;
        #endregion

        #region NPC_Goals
        private Stack<NPCGoal> gGoals;
        #endregion

        #region Members
        [SerializeField]
        private Dictionary<string, NPCAttribute> gAttributes;

        [SerializeField]
        private Dictionary<string, IPathfinder> gPathfinders;
        #endregion

        #region Properties
        [SerializeField]
        public string SelectedPathfinder = "None";

        [SerializeField]
        public bool NavMeshAgentPathfinding = false;

        [SerializeField]
        public IPathfinder CurrentPathfinder;

        [SerializeField]
        private NavMeshAgent gNavMeshAgent;

        public Dictionary<string,IPathfinder> Pathfinders {
            get {
                if (gPathfinders == null) InitPathfinders();
                return gPathfinders;
            }
        }
        #endregion

        #region Unity_Methods
        void Reset() {
            this.gNPCController = gameObject.GetComponent<NPCController>();
            InitPathfinders();
        }

        void Start() {
            gNPCController = GetComponent<NPCController>();
            CurrentPathfinder = Pathfinders[SelectedPathfinder];
        }
        #endregion

        #region Public_Functions
        public void SetNPCModule(INPCModule mod) {
            if (gPathfinders == null) InitPathfinders();
            switch(mod.NPCModuleType()) {
                case NPC_MODULE_TYPE.PATHFINDER:
                    gPathfinders.Add(mod.NPCModuleName(),mod as IPathfinder);
                    break;
            }
        }

        public List<Vector3> FindPath(Vector3 target) {
            if (NavMeshAgentPathfinding) {
                gNavMeshAgent.enabled = true;
                NavMeshPath navMeshPath = new NavMeshPath();
                gNavMeshAgent.CalculatePath(target,navMeshPath);
                gNavMeshAgent.enabled = false;
                return new List<Vector3>(navMeshPath.corners);
            } else if (CurrentPathfinder == null) {
                List<Vector3> path = new List<Vector3>();
                path.Add(target);
                return path;
            } else {
                return CurrentPathfinder.FindPath(gNPCController.transform.position, target);
            }
        }
        #endregion

        #region Private_Functions

        private void InitPathfinders() {
            gPathfinders = new Dictionary<string, IPathfinder>();
            gPathfinders.Add("None", null);
            gNavMeshAgent = GetComponent<NavMeshAgent>();
        }
        #endregion

        #region Traits


        /* For the purpose of initialization */
        private bool gRandomizeTraits;

        bool RandomizeTraits {

            get {
                return gRandomizeTraits;
            }
            set {
                gRandomizeTraits = value;
            }
        }

        #endregion

        protected void InitializeTraits() {
            foreach (PropertyInfo pi in this.GetType().GetProperties()) {
                object[] attribs = pi.GetCustomAttributes(true);
                if(attribs.Length > 0) {

                }
            }
        }

        #region Attributes
    
        [NPCAttribute("NPC",typeof(bool))]
        public bool NPC { get; set; }

        [NPCAttribute("Charisma",typeof(float))]
        public float Charisma { get; set; }

        [NPCAttribute("Friendliness",typeof(float))]
        public float Friendliness { get; set; }
    
        [NPCAttribute("Strength",typeof(int))]
        public int Strength { get; set; }

        [NPCAttribute("Intelligence",typeof(int))]
        public int Intelligence { get; set; }

        [NPCAttribute("Dexterity",typeof(int))]
        public int Dexterity { get; set; }

        [NPCAttribute("Constitution",typeof(int))]
        public int Constitution { get; set; }

        [NPCAttribute("Hostility", typeof(float))]
        public float Hostility { get; set; }

        [NPCAttribute("Location",typeof(Vector3))]
        public Vector3 Location { get; set; }

        #endregion

    }
}