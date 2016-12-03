using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NPC;
using System.IO;
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
    private static char letterA = 'A';
    private List<NavNode.NODE_TYPE> g_TestReadings;
    private List<MOVE_POLICY> g_TestPolicies;
    public float UpdateInSeconds = 1;
    private long g_UpdateCycle;
    private Stopwatch g_Stopwatch;
    private NPCController g_NPCController;
    private int g_TotalNodesCount;
    private Dictionary<NavNode.NODE_TYPE, List<NavNodeData>> g_NodeTypes;
    private int g_ExploringRoundCount;
    private GroundTruthData g_GroundTruthData;
    private NavNode.NODE_TYPE g_LastReadType;
    private MOVE_POLICY g_LastMovePolicy = MOVE_POLICY.STAY;
    private NavNode g_LastAgentNode;
    private List<NavNodeData> g_ViterbiPath;

    /// <summary>
    /// g_TestReadings must be populated
    /// </summary>
    [SerializeField]
    public string FileName;

    [SerializeField]
    public bool ForceInitialStateReading = false;

    [SerializeField]
    public bool PaintAllTiles = false;

    [SerializeField]
    public int ExplorationsRounds = 1;

    [SerializeField]
    public bool LoadGroundTruth = false;

    [SerializeField]
    public bool RecordExplorations = false;
    
    [SerializeField]
    public bool PrintViterbiPath = false;

    [SerializeField]
    public bool GenerateGroundTruthData = false;

    [SerializeField]
    public int RandomStateValues = 100;

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
        UnityEngine.Debug.Log("Starting exploration");
        RandomizeStart = !RandomizeStart ? 
            GenerateGroundTruthData : RandomizeStart;
        GenerateGroundTruthData = !LoadGroundTruth;
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
        if (ForceInitialStateReading) {
            g_LastMovePolicy = g_TestPolicies[0];
            g_TestPolicies.Remove(0);
            AnimatedExploration = false;
        }

        g_NPCController = GetComponent<NPCController>();
        g_UpdateCycle = (long) (UpdateInSeconds * 1000);
        g_Stopwatch = System.Diagnostics.Stopwatch.StartNew();
        g_Stopwatch.Start();
        g_NodeTypes = new Dictionary<NavNode.NODE_TYPE, List<NavNodeData>>();
        if(g_Grid == null) {
            RaycastHit hit;
            if(Physics.Raycast(new Ray(transform.position + (transform.up * 0.2f), -1 * transform.up), out hit)) {
                g_Grid = hit.collider.GetComponent<NavGrid>();
                g_NPCController.Debug(this + " - Grid Initialized ok");
            }
            if (g_Grid == null) this.enabled = false;
        }

        g_NodeValues = new Dictionary<NavNode, NavNodeData>();

        NavNode agentNode = null;
        if (RandomizeStart) {
            do {
                agentNode = g_Grid.GetRandomNode();
            } while (!agentNode.IsWalkable());
        } else {
            agentNode = g_Grid.FindOccupiedNode(transform);
        }

        g_LastAgentNode = agentNode;

        g_TotalNodesCount = g_Grid.NodesCount;
        float freeTiles = g_TotalNodesCount - (g_TotalNodesCount * g_Grid.MinimunBlocked);
        foreach(NavNode.NODE_TYPE t in Enum.GetValues(typeof(NavNode.NODE_TYPE))) {
            g_NodeTypes.Add(t,new List<NavNodeData>());
        }
        // Initialize prior probabilities
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
                    n.SetHighlightTile(PaintAllTiles, Color.blue, 0.8f);
                    break;
                case NavNode.NODE_TYPE.WALKABLE:
                    n.SetHighlightTile(PaintAllTiles, Color.green, 0.8f);
                    break;
                case NavNode.NODE_TYPE.HARD_TO_WALK:
                    n.SetHighlightTile(PaintAllTiles, Color.yellow, 0.8f);
                    break;
                case NavNode.NODE_TYPE.NONWALKABLE:
                    n.SetHighlightTile(PaintAllTiles, Color.red, 0.8f);
                    break;
            }
            nd.NodeType = n.NodeType;
            g_NPCController.Debug("Discovered new Node " + nd);
            g_NodeTypes[n.NodeType].Add(nd);
        }

        if (GenerateGroundTruthData) {
            GenerateGroundTruth();
        } else if (LoadGroundTruth) {
            LoadGroundTruthFile();
        }

        g_ViterbiPath = new List<NavNodeData>();
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

                    g_LastReadType = Sense(g_LastAgentNode, g_NodeValues[g_LastAgentNode]);

                    UpdateNodes(g_LastAgentNode);

                    if (g_LastAgentNode != null) {
                        if (AnimatedExploration && !g_NPCController.Body.Navigating) {
                            // go to
                            g_NPCController.OrientTowards((g_LastAgentNode.Position - transform.position).normalized);
                        } else {
                            transform.position = g_LastAgentNode.Position;
                        }
                    }
                    if(GenerateGroundTruthData)
                        g_NPCController.Debug(g_GroundTruthData.GetGroundTruth(g_ExploringRoundCount).ToString());
                    g_ExploringRoundCount++;

                } else {
                    g_NPCController.Debug("NPCExplorer -> Finished execution test!");
                    if (RecordExplorations)
                        WriteResultsToFile();
                    ExplorationsRounds--;
                    Enabled = ExplorationsRounds > 0; 
                    g_ExploringRoundCount = 0;
                    g_ViterbiPath.Clear();
                    g_GroundTruthData = null;
                    g_LastAgentNode = null;
                    g_TestPolicies.Clear();
                    g_TestReadings.Clear();
                    if (Enabled)
                        Start();
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
        float alpha = 0, viterbiAlpha = 0f;   
        int validTiles = (Enum.GetValues(typeof(NavNode.NODE_TYPE)).Length - 2);
        // P(E|X) * SUM(P(X | X-1) P(E | E-1))
        foreach (List<NavNodeData> l in g_NodeTypes.Values) {
            // for each node
            foreach(NavNodeData d in l) {

                updatedNodes.Add(d.Node, new NavNodeData());

                float sense,                                //  P(E|E-1)
                    sum = 0f,                               //  P(X|X-1)
                    PreviousBelief = PrintViterbiPath ?     //  P(E|X-1)
                    d.ViterbiProbability : d.Probability,         
                    maxSum =                                // argmax(Tr * Pb)
                    (1f - SuccessRate) * PreviousBelief;

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

                    // Update Ground Truth Data
                    if(GenerateGroundTruthData)
                        g_GroundTruthData.SetGroudnTruth(g_LastMovePolicy, g_LastReadType, sense, n, g_ExploringRoundCount);

                    // where I am coming from
                    switch(g_LastMovePolicy) {

                        case MOVE_POLICY.UP:

                            // there is always the chance of staying in place
                            sum += (1f - SuccessRate) * PreviousBelief;
                            
                            if (GetNextNode(d.Node, MOVE_POLICY.UP) == null) {
                                // 90% chances of staying in this cell if blocked / unavailable
                                maxSum += SuccessRate * PreviousBelief; // for Viterbi
                                sum += SuccessRate * PreviousBelief;

                            }

                            NavNode nextNode = GetNextNode(d.Node, MOVE_POLICY.DOWN);
                            if (nextNode != null) {
                                float prevProb = PrintViterbiPath ? g_NodeValues[nextNode].ViterbiProbability : g_NodeValues[nextNode].Probability;
                                // we came from the one before successfully
                                maxSum = Math.Max(maxSum, (SuccessRate * prevProb));
                                sum += (SuccessRate * prevProb);
                            }

                            break;

                        case MOVE_POLICY.DOWN:

                            // there is always the chance of staying in place
                            sum += (1f - SuccessRate) * PreviousBelief;

                            if (GetNextNode(d.Node, MOVE_POLICY.DOWN) == null) {
                                // 90% chances of staying in this cell if blocked / unavailable
                                maxSum += SuccessRate * PreviousBelief; // for Viterbi
                                sum += SuccessRate * PreviousBelief;
                            }
                            nextNode = GetNextNode(d.Node, MOVE_POLICY.UP);
                            if (nextNode != null) {
                                float prevProb = PrintViterbiPath ? g_NodeValues[nextNode].ViterbiProbability : g_NodeValues[nextNode].Probability;
                                // we came from the one before successfully
                                maxSum = Math.Max(maxSum, (SuccessRate * prevProb));
                                sum += (SuccessRate * prevProb);
                            }

                            break;

                        case MOVE_POLICY.LEFT:

                            // there is always the chance of staying in place
                            sum += (1f - SuccessRate) * PreviousBelief;

                            if (GetNextNode(d.Node, MOVE_POLICY.LEFT) == null) {
                                // 90% chances of staying in this cell if blocked / unavailable
                                maxSum += SuccessRate * PreviousBelief; // for Viterbi
                                sum += SuccessRate * PreviousBelief;
                            }
                            nextNode = GetNextNode(d.Node, MOVE_POLICY.RIGHT);
                            if (nextNode != null) {
                                float prevProb = PrintViterbiPath ? g_NodeValues[nextNode].ViterbiProbability : g_NodeValues[nextNode].Probability;
                                // we came from the one before successfully
                                maxSum = Math.Max(maxSum, (SuccessRate * prevProb));
                                sum += (SuccessRate * prevProb);
                            }

                            break;

                        case MOVE_POLICY.RIGHT:
                            
                            // there is always the chance of staying in place
                            sum += (1f - SuccessRate) * PreviousBelief;

                            if (GetNextNode(d.Node, MOVE_POLICY.RIGHT) == null) {
                                // 90% chances of staying in this cell if blocked / unavailable
                                maxSum += SuccessRate * PreviousBelief; // for Viterbi
                                sum += SuccessRate * PreviousBelief;
                            }

                            nextNode = GetNextNode(d.Node, MOVE_POLICY.LEFT);
                            if (nextNode != null) {
                                float prevProb = PrintViterbiPath ? g_NodeValues[nextNode].ViterbiProbability : g_NodeValues[nextNode].Probability;
                                // we came from the one before successfully
                                maxSum = Math.Max(maxSum, (SuccessRate * prevProb));
                                sum += (SuccessRate * prevProb);
                            }

                            break;
                    }

                    // do not update the nodes until the round is over
                    updatedNodes[d.Node].ViterbiProbability = sense * maxSum;
                    updatedNodes[d.Node].Probability = sense * sum;
                    // compute alphas
                    viterbiAlpha += updatedNodes[d.Node].ViterbiProbability;
                    alpha += updatedNodes[d.Node].Probability;

                }
            }
        }

        
        // normalize
        int decimals = (int) Math.Pow(10, DecimalPrecision);
        NavNodeData maxNode = nd;
        foreach (List<NavNodeData> l in g_NodeTypes.Values) {
            foreach (NavNodeData d in l) {
                d.Node.TileColor = Color.green;
                d.ViterbiProbability = updatedNodes[d.Node].ViterbiProbability;
                d.ViterbiProbability /= viterbiAlpha;
                maxNode = d.ViterbiProbability > maxNode.ViterbiProbability ? d : maxNode;
                d.Probability = updatedNodes[d.Node].Probability;
                d.Probability /= alpha;
                d.Node.TileText = PrintViterbiPath ?
                    ((float)Math.Round((d.ViterbiProbability) * decimals) / decimals).ToString() :
                    ((float)Math.Round((d.Probability) * decimals) / decimals).ToString();
            }
        }
        g_ViterbiPath.Add(maxNode);
        if (PrintViterbiPath) {
            maxNode.Node.TileColor = Color.red;
            maxNode.Node.TileText = "Node " + g_ExploringRoundCount + "\n" + maxNode.Node.TileText;
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
        System.Random random = new System.Random();
        List<NavNode.NODE_TYPE> nodes = new List<NavNode.NODE_TYPE>();
        double p = random.NextDouble();
        NavNode.NODE_TYPE trueType = n.NodeType;
        Array l = Enum.GetValues(typeof(NavNode.NODE_TYPE));
        if (p > SensorSuccess) {
            float rP = 1f / (l.Length - 2);
            p = random.NextDouble();
            foreach (NavNode.NODE_TYPE nt in l) {
                if (nt == trueType) continue;
                if (p > rP) {
                    return nt;
                } else p += p; 
            }
        } else {
            return trueType;
        }
        return NavNode.NODE_TYPE.NONWALKABLE;
    }

    private bool Tick() {
        if(g_UpdateCycle < 0) return true;
        if(g_Stopwatch.ElapsedMilliseconds > g_UpdateCycle) {
            g_Stopwatch.Reset();
            g_Stopwatch.Start();
            return !PauseExecution;
        }
        return false;
    }

    private void WriteResultsToFile() {
        string fileName = letterA + " - GroundTruth - " + FileName + ".txt";
        letterA++;
        if (File.Exists(fileName)) {
            fileName = fileName.Substring(0, fileName.IndexOf(".txt")) + "_copy.txt";
        }
        StreamWriter sw = File.CreateText(fileName);
        sw.WriteLine(g_GroundTruthData.InitialPosition.x+","+g_GroundTruthData.InitialPosition.y);
        for (int i = 0; i < RandomStateValues; ++i) {
            sw.WriteLine(
                g_GroundTruthData.GetGroundTruth(i).Node.GridPosition.x + "," + g_GroundTruthData.GetGroundTruth(i).Node.GridPosition.y);
        }
        for (int i = 0; i < RandomStateValues; ++i) {
            char v = g_GroundTruthData.GetGroundTruth(i).Policy == MOVE_POLICY.UP ? 'U' :
                        (g_GroundTruthData.GetGroundTruth(i).Policy == MOVE_POLICY.DOWN ? 'D' :
                            g_GroundTruthData.GetGroundTruth(i).Policy == MOVE_POLICY.RIGHT ? 'R' : 'L');
            sw.WriteLine(v);
        }
        for (int i = 0; i < RandomStateValues; ++i) {
            char v = g_GroundTruthData.GetGroundTruth(i).Read == NavNode.NODE_TYPE.WALKABLE ? 'N' :
                        (g_GroundTruthData.GetGroundTruth(i).Read == NavNode.NODE_TYPE.HARD_TO_WALK ? 'T' : 'H'); 
            sw.WriteLine(v);
        }
        sw.Close();
    }

    private void GenerateGroundTruth() {
        g_TestReadings.Clear();
        g_TestPolicies.Clear();
        g_GroundTruthData = new GroundTruthData(RandomStateValues);
        g_GroundTruthData.InitialPosition = new Vector2(g_LastAgentNode.GridPosition.x, g_LastAgentNode.GridPosition.y);
        System.Random random = new System.Random();
        int arrayLenght = Enum.GetValues(typeof(MOVE_POLICY)).Length;
        MOVE_POLICY[] policies = (MOVE_POLICY[])Enum.GetValues(typeof(MOVE_POLICY));
        for (int i = 0; i < RandomStateValues; ++i) {
            // generate random policy i
            int rand = random.Next(0, arrayLenght-1);
            MOVE_POLICY randomPolicy = policies[rand];
            g_TestPolicies.Add(randomPolicy);
        }
    }

    private void LoadGroundTruthFile() {
        string fileName = letterA + " - GroundTruth - " + FileName + ".txt";
        if(File.Exists(fileName)) {
            StreamReader sr = File.OpenText(fileName);
            string line = null;
            while ((line = sr.ReadLine()) != null) {
                switch (line) {
                    case "U":
                        g_TestPolicies.Add(MOVE_POLICY.UP);
                        break;
                    case "D":
                        g_TestPolicies.Add(MOVE_POLICY.DOWN);
                        break;
                    case "L":
                        g_TestPolicies.Add(MOVE_POLICY.LEFT);
                        break;
                    case "R":
                        g_TestPolicies.Add(MOVE_POLICY.RIGHT);
                        break;
                    case "N":
                        g_TestReadings.Add(NavNode.NODE_TYPE.WALKABLE);
                        break;
                    case "H":
                        g_TestReadings.Add(NavNode.NODE_TYPE.HIGHWAY);
                        break;
                    case "T":
                        g_TestReadings.Add(NavNode.NODE_TYPE.HARD_TO_WALK);
                        break;
                    default:
                        g_NPCController.Debug("Fucked up: " + line);
                        break;
                }
            }
            sr.Close();
        }
    }

    #endregion


    #region Support_Classes
    class GroundTruthData {

        public Vector2 InitialPosition;
        NavNode[] ActualNode;
        MOVE_POLICY[] Policies;
        NavNode.NODE_TYPE[] Readings;
        float[] Transitions;

        public GroundTruthData(int size) {
            ActualNode = new NavNode[size];
            Policies = new MOVE_POLICY[size];
            Readings = new NavNode.NODE_TYPE[size];
            Transitions = new float[size];
        }

        public GroundTruth GetGroundTruth(int index) {
            GroundTruth gt = new GroundTruth();
            gt.Policy = Policies[index];
            gt.Read = Readings[index];
            gt.Transition = Transitions[index];
            gt.Node = ActualNode[index];
            return gt;
        }

        public void SetGroudnTruth(MOVE_POLICY p, NavNode.NODE_TYPE type, float t, NavNode n, int index) {
            Policies[index] = p;
            Readings[index] = type;
            Transitions[index] = t;
            ActualNode[index] = n;
        }

        public class GroundTruth {
            public NavNode Node;
            public MOVE_POLICY Policy;
            public NavNode.NODE_TYPE Read;
            public float Transition;
            public float Observations;
            public override string ToString() {
                return "POLICY: " + Policy + " SENSED: " + Read + " TrMod: " + Transition + " ActualNode: " + Node + " ActualType: " + Node.NodeType;
            }
        }
    }

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
