using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NPC;
using Pathfinding;
using System;

public class NPCExplorer : MonoBehaviour, INPCModule {

    #region Enums
    enum MOVE_POLICY {
        UP,
        DOWN,
        RIGHT,
        LEFT
    }

    #endregion

    #region Members
    private List<NavNode.NODE_TYPE> g_TestReadings;
    private List<MOVE_POLICY> g_TestPolicies;
    public float UpdateInSeconds = 1;
    private long g_UpdateCycle;
    private Stopwatch g_Stopwatch;
    private NPCController g_NPCController;
    private int g_TotalNodesCount;
    private Dictionary<NavNode.NODE_TYPE, List<NavNodeData>> g_NodeTypes;

    private NavNode.NODE_TYPE g_LastReadType;
    private MOVE_POLICY g_LastMovePolicy;
    private NavNode g_LastAgentNode;

    /// <summary>
    /// g_TestReadings must be populated
    /// </summary>
    [SerializeField]
    public bool ForceInitialStateReading = false;
    
    [SerializeField]
    public bool AnimatedExploration = false;

    [SerializeField]
    public bool RandomizeStart = false;

    [SerializeField]
    public bool Enabled = true;

    [SerializeField]
    public float SuccessRate = 0.9f;

    [SerializeField]
    public float SensorSuccess = 0.9f;

    private Dictionary<NavNode, NavNodeData> g_NodeValues;
    private NavGrid g_Grid;
    #endregion

    #region Public_Functions
    #endregion

    #region Unity_Methods
    // Use this for initialization
    void Start () {

        /* providing hard coded readings for testing */
        g_TestReadings = new List<NavNode.NODE_TYPE>();
        g_TestReadings.Add(NavNode.NODE_TYPE.WALKABLE);
        g_TestReadings.Add(NavNode.NODE_TYPE.WALKABLE);
        g_TestReadings.Add(NavNode.NODE_TYPE.HIGHWAY);
        g_TestReadings.Add(NavNode.NODE_TYPE.HIGHWAY);

        g_TestPolicies = new List<MOVE_POLICY>();
        g_TestPolicies.Add(MOVE_POLICY.RIGHT);
        g_TestPolicies.Add(MOVE_POLICY.RIGHT);
        g_TestPolicies.Add(MOVE_POLICY.DOWN);
        g_TestPolicies.Add(MOVE_POLICY.DOWN);
        /* ---------------------------------------- */

        // we move first
        if(ForceInitialStateReading) {
            g_LastMovePolicy = g_TestPolicies[0];
            g_TestPolicies.Remove(0);
            AnimatedExploration = false;
        }

        g_NPCController = GetComponent<NPCController>();
        g_UpdateCycle = (long) (UpdateInSeconds * 1000);
        g_Stopwatch = System.Diagnostics.Stopwatch.StartNew();
        g_Stopwatch.Start();
        g_NodeTypes = new Dictionary<NavNode.NODE_TYPE, List<NavNodeData>>();
        RaycastHit hit;
        if(Physics.Raycast(new Ray(transform.position + (transform.up * 0.2f), -1 * transform.up), out hit)) {
            g_Grid = hit.collider.GetComponent<NavGrid>();
            g_NPCController.Debug(this + " - Grid Initialized ok");
        }
        if (g_Grid == null) this.enabled = false;
        g_NodeValues = new Dictionary<NavNode, NavNodeData>();
        if(RandomizeStart) {
            NavNode n;
            do {
                n = g_Grid.GetRandomNode();
            } while (!n.IsWalkable());
            transform.position = n.Position;
        }
        g_TotalNodesCount = g_Grid.NodesCount;
        float freeTiles = g_TotalNodesCount - g_Grid.GetTotalBlockedTiles();
        foreach(NavNode.NODE_TYPE t in Enum.GetValues(typeof(NavNode.NODE_TYPE))) {
            g_NodeTypes.Add(t,new List<NavNodeData>());
        }
        // Initialize prior probabilities
        NavNode agentNode = g_LastAgentNode = g_Grid.FindOccupiedNode(transform);
        foreach (NavNode n in g_Grid.NodesList()) {
            NavNodeData nd = new NavNodeData();
            nd.Node = n;
            if(n.IsWalkable() || agentNode == n) {
                nd.Probability = 1f / freeTiles;
                nd.MovePolicy = MOVE_POLICY.UP;
            } else {
                nd.Probability = 0f;
            }
            g_NodeValues.Add(n, nd);
            switch(n.NodeType) {
                case NavNode.NODE_TYPE.HIGHWAY:
                    n.SetHighlightTile(true, Color.blue, 0.8f);
                    break;
                case NavNode.NODE_TYPE.WALKABLE:
                    n.SetHighlightTile(true, Color.green, 0.8f);
                    break;
                case NavNode.NODE_TYPE.HARD_TO_WALK:
                    n.SetHighlightTile(true, Color.yellow, 0.8f);
                    break;
                case NavNode.NODE_TYPE.NONWALKABLE:
                    n.SetHighlightTile(true, Color.red, 0.8f);
                    break;
            }
            g_NPCController.Debug("Discovered new Node " + nd);
            g_NodeTypes[n.NodeType].Add(nd);
        }
        g_NPCController.Debug("NPCExplorer initialized");
    }
	
	/// <summary>
    /// No npc module should be updated from here but from its TickModule method
    /// which will be only called from the NPCController on FixedUpdate
    /// </summary>
	void Update () { }

    #endregion

    #region NPCModule
    public bool IsEnabled() {
        return Enabled;
    }

    public string NPCModuleName() {
        return "NavGrid Exporation";
    }

    public NPC_MODULE_TARGET NPCModuleTarget() {
        return NPC_MODULE_TARGET.AI;
    }

    public NPC_MODULE_TYPE NPCModuleType() {
        return NPC_MODULE_TYPE.EXPLORATION;
    }

    public void RemoveNPCModule() {
        // destroy components in memroy here
    }

    public void SetEnable(bool e) {
        Enabled = e;
    }

    public bool IsUpdateable() {
        return true;
    }

    public void TickModule() {
        if(Enabled) { 
            if(Tick()) {

                g_NPCController.Debug("Updating NPC Module: " + NPCModuleName());

                if (g_TestPolicies.Count > 0) {
                    g_LastMovePolicy = g_TestPolicies[0];
                    g_TestPolicies.Remove(g_LastMovePolicy);
                    NavNode t = GetNextNode(g_LastAgentNode, g_LastMovePolicy);
                    g_LastAgentNode = t == null ? g_LastAgentNode : t;
                } else {
                    g_NPCController.Debug("NPCExplorer -> Finished execution test!");
                    Enabled = false;
                }

                UpdateNodes(g_LastAgentNode);

                if (g_LastAgentNode != null) {
                    if (AnimatedExploration && !g_NPCController.Body.Navigating) {
                        // go to
                    } else {
                        g_NPCController.OrientTowards((g_LastAgentNode.Position - transform.position).normalized);
                        transform.position = g_LastAgentNode.Position;
                    }
                }
                
                // sense after we move - first time is unknown
            }
        }
    }
    #endregion

    #region Private_Functions
    
    /// <summary>
    /// Add a new node, compute its probability
    /// </summary>
    /// <param name="n"></param>
    private void UpdateNodes(NavNode n) {
        NavNodeData nd = g_NodeValues[n];
        // TODO - P(E|X) * SUM(P(X | X-1) P(E | E-1))
        nd.MovePolicy = MOVE_POLICY.UP;
        // List<NavNode.NODE_TYPE> nodes = Sense(n, nd);
        foreach(List<NavNodeData> l in g_NodeTypes.Values) {
            foreach(NavNodeData d in l) {
                d.Node.TileText = d.Probability.ToString();
            }
        }
        g_NodeValues.Remove(n);
        g_NodeValues.Add(n, nd);
    }

    /// <summary>
    /// Returns node if valid and clear, null is non existing or blocked
    /// </summary>
    /// <param name="curr"></param>
    /// <param name="p"></param>
    /// <returns>Node or null</returns>
    private NavNode GetNextNode(NavNode curr, MOVE_POLICY p) {
        NavNode n = null;
        Vector2 pos = Vector2.zero; 
        switch (p) {
            case MOVE_POLICY.UP:
                pos = new Vector2(curr.GridPosition.x, curr.GridPosition.y + 1);
                break;
            case MOVE_POLICY.DOWN:
                pos = new Vector2(curr.GridPosition.x, curr.GridPosition.y - 1);
                break;
            case MOVE_POLICY.LEFT:
                pos = new Vector2(curr.GridPosition.x - 1, curr.GridPosition.y);
                break;
            case MOVE_POLICY.RIGHT:
                pos = new Vector2(curr.GridPosition.x + 1, curr.GridPosition.y);
                break;
        }
        if (g_Grid.IsValid(pos)) {
            n = g_Grid.GetGridNode((int) pos.x, (int) pos.y);
        }
        return n;
    }

    private List<NavNode.NODE_TYPE> Sense(NavNode n, NavNodeData nd) {
        List<NavNode.NODE_TYPE> nodes = new List<NavNode.NODE_TYPE>();
        double p = (new System.Random()).NextDouble();
        NavNode.NODE_TYPE trueType = n.NodeType;
        Array l = Enum.GetValues(typeof(NavNode.NODE_TYPE));
        float threshold = 1f / (l.Length - 1);
        if (p <= 1f - SensorSuccess) {
            foreach (NavNode.NODE_TYPE nt in l) {
                if (nt == trueType) continue;
                nodes.Add(nt);
                threshold += threshold;
            }
        } else {
            nd.NodeType = trueType;
        }
        return nodes;
    }

    private bool Tick() {
        if(g_Stopwatch.ElapsedMilliseconds > g_UpdateCycle) {
            g_Stopwatch.Reset();
            g_Stopwatch.Start();
            return true;
        }
        return false;
    }
    #endregion


    #region Support_Classes
    class NavNodeData {
        private float g_Probability;
        public NavNode Node;
        public float Probability {
            get {
                return (float)(Math.Round(g_Probability * 1000) / 1000f);
            }
            set {
                g_Probability = value;
            }
        }
        public MOVE_POLICY MovePolicy;
        public NavNode.NODE_TYPE NodeType;
        public override string ToString() {
            return Node + " val: " + Probability + " Policy: " + MovePolicy;
        }
    }
    #endregion

}
