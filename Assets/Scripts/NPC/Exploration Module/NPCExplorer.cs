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
        LEFT,
        STAY
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
    private int g_ViterbiRoundCount;

    private NavNode.NODE_TYPE g_LastReadType;
    private MOVE_POLICY g_LastMovePolicy = MOVE_POLICY.STAY;
    private NavNode g_LastAgentNode;

    /// <summary>
    /// g_TestReadings must be populated
    /// </summary>
    [SerializeField]
    public bool ForceInitialStateReading = false;

    [SerializeField]
    public bool PrintViterbiPath = false;

    [SerializeField]
    public bool AnimatedExploration = false;

    [SerializeField]
    public int DecimalPrecision = 5;

    [SerializeField]
    public bool RandomizeStart = false;

    [SerializeField]
    public bool Enabled = true;

    [SerializeField]
    public float SuccessRate = 0.9f;

    [SerializeField]
    public float SensorSuccess = 0.9f;

    [SerializeField]
    public bool PauseExecution = false;

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
                nd.Probability = nd.ViterbiProbability = 1f / freeTiles;
                nd.MovePolicy = MOVE_POLICY.UP;
            } else {
                nd.Probability = nd.ViterbiProbability = 0f;
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
            nd.NodeType = n.NodeType;
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
                g_ViterbiRoundCount++;
                g_NPCController.Debug("Updating NPC Module: " + NPCModuleName());

                if (g_TestPolicies.Count > 0) {
                    g_LastMovePolicy = g_TestPolicies[0];
                    g_TestPolicies.Remove(g_LastMovePolicy);
                    NavNode t = GetNextNode(g_LastAgentNode, g_LastMovePolicy);
                    g_LastAgentNode = t == null ? g_LastAgentNode : t;
                } else {
                    g_NPCController.Debug("NPCExplorer -> Finished execution test!");
                    Enabled = false;
                    g_ViterbiRoundCount = 0;
                }

                g_LastReadType = Sense(g_LastAgentNode, g_NodeValues[g_LastAgentNode]);

                UpdateNodes(g_LastAgentNode);

                if (g_LastAgentNode != null) {
                    if (AnimatedExploration && !g_NPCController.Body.Navigating) {
                        // go to
                    } else {
                        g_NPCController.OrientTowards((g_LastAgentNode.Position - transform.position).normalized);
                        transform.position = g_LastAgentNode.Position;
                    }
                }
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
        Dictionary<NavNode, NavNodeData> updatedNodes = new Dictionary<NavNode, NavNodeData>();
        float alpha = 0, viterbiAlpha = 0f; ;    
        int validTiles = (Enum.GetValues(typeof(NavNode.NODE_TYPE)).Length - 2);
        // P(E|X) * SUM(P(X | X-1) P(E | E-1))
        foreach (List<NavNodeData> l in g_NodeTypes.Values) {
            // for each node
            foreach(NavNodeData d in l) {

                updatedNodes.Add(d.Node, new NavNodeData());

                float sense,                                //  P(E|E-1)
                    sum = 0f,                               //  P(X|X-1)
                    PreviousBelief = d.Probability,         //  P(E|X-1)
                    
                    maxSum = 
                    (1f - SuccessRate) * PreviousBelief,    //  Viterbi - assume minimums first
                    maxTrans = (1f - SuccessRate),          //  argmax(P(X|X-1)
                    maxPrevBel = PreviousBelief;            //  argmax(P(E|X-1)

                // 0.9 Success, 0.05 on sensing some wrong other type
                if(d.NodeType == NavNode.NODE_TYPE.NONWALKABLE) {

                    d.Probability = 0;

                } else {

                    // determine sensor noise
                    if (d.NodeType == g_LastReadType) {
                        sense = SensorSuccess;
                    } else
                        // dont count self and blocked
                        sense = (1f - SensorSuccess) / validTiles;
                    
                    // where I am coming from
                    switch(g_LastMovePolicy) {

                        case MOVE_POLICY.UP:

                            // there is always the chance of staying in place
                            sum += (1f - SuccessRate) * PreviousBelief;
                            
                            if (GetNextNode(d.Node, MOVE_POLICY.UP) == null) {
                                // 90% chances of staying in this cell if blocked / unavailable
                                sum += SuccessRate * PreviousBelief;
                            }

                            NavNode nextNode = GetNextNode(d.Node, MOVE_POLICY.DOWN);
                            if (nextNode != null) {
                                float prevProb = g_NodeValues[nextNode].Probability;
                                // we came from the one before successfully
                                sum += (SuccessRate * prevProb);
                            }

                            break;

                        case MOVE_POLICY.DOWN:

                            // there is always the chance of staying in place
                            sum += (1f - SuccessRate) * PreviousBelief;

                            if (GetNextNode(d.Node, MOVE_POLICY.DOWN) == null) {
                                // 90% chances of staying in this cell if blocked / unavailable
                                sum += SuccessRate * PreviousBelief;
                            }
                            nextNode = GetNextNode(d.Node, MOVE_POLICY.UP);
                            if (nextNode != null) {
                                float prevProb = g_NodeValues[nextNode].Probability;
                                // we came from the one before successfully
                                sum += (SuccessRate * prevProb);
                            }

                            break;

                        case MOVE_POLICY.LEFT:

                            // there is always the chance of staying in place
                            sum += (1f - SuccessRate) * PreviousBelief;

                            if (GetNextNode(d.Node, MOVE_POLICY.LEFT) == null) {
                                // 90% chances of staying in this cell if blocked / unavailable
                                sum += SuccessRate * PreviousBelief;
                            }
                            nextNode = GetNextNode(d.Node, MOVE_POLICY.RIGHT);
                            if (nextNode != null) {
                                float prevProb = g_NodeValues[nextNode].Probability;
                                // we came from the one before successfully
                                sum += (SuccessRate * prevProb);
                            }

                            break;

                        case MOVE_POLICY.RIGHT:
                            
                            // there is always the chance of staying in place
                            sum += (1f - SuccessRate) * PreviousBelief;

                            if (GetNextNode(d.Node, MOVE_POLICY.RIGHT) == null) {
                                // 90% chances of staying in this cell if blocked / unavailable
                                sum += SuccessRate * PreviousBelief;
                            }

                            nextNode = GetNextNode(d.Node, MOVE_POLICY.LEFT);
                            if (nextNode != null) {
                                float prevProb = g_NodeValues[nextNode].Probability;
                                // we came from the one before successfully
                                sum += (SuccessRate * prevProb);
                            }

                            break;
                    }

                    // do not update the nodes until the round is over
                    updatedNodes[d.Node].Probability = sense * sum;
                    alpha += updatedNodes[d.Node].Probability;

                }
            }
        }

        
        // normalize
        int decimals = (int) Math.Pow(10, DecimalPrecision);
        NavNodeData maxNode = nd;
        foreach (List<NavNodeData> l in g_NodeTypes.Values) {
            foreach (NavNodeData d in l) {
                d.Probability = updatedNodes[d.Node].Probability;
                d.Probability /= alpha;
                d.Node.TileText = 
                    ((float)Math.Round((d.Probability) * decimals) / decimals).ToString();
            }
        }
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

    private NavNode.NODE_TYPE Sense(NavNode n, NavNodeData nd) {
        if(g_TestReadings.Count > 0) {
            NavNode.NODE_TYPE t = g_TestReadings[0];
            g_TestReadings.Remove(t);
            return t;
        }
        List<NavNode.NODE_TYPE> nodes = new List<NavNode.NODE_TYPE>();
        double p = (new System.Random()).NextDouble();
        NavNode.NODE_TYPE trueType = n.NodeType;
        Array l = Enum.GetValues(typeof(NavNode.NODE_TYPE));
        float threshold = 1f / (l.Length - 1);
        if (p <= 1f - SensorSuccess) {
            foreach (NavNode.NODE_TYPE nt in l) {
                if (nt == trueType) continue;
                return nt;
            }
        } else {
            nd.NodeType = trueType;
        }
        return NavNode.NODE_TYPE.NONWALKABLE;
    }

    private bool Tick() {
        if(g_Stopwatch.ElapsedMilliseconds > g_UpdateCycle) {
            g_Stopwatch.Reset();
            g_Stopwatch.Start();
            return !PauseExecution;
        }
        return false;
    }
    #endregion


    #region Support_Classes
    class NavNodeData {
        public NavNode Node;
        public float Probability;
        public float ViterbiProbability;
        public MOVE_POLICY MovePolicy;
        public NavNode.NODE_TYPE NodeType;
        public override string ToString() {
            return Node + " val: " + Probability + " Policy: " + MovePolicy;
        }
    }
    #endregion

}
