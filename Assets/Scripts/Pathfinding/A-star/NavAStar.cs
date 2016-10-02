﻿using UnityEngine;
using System.Collections;
using Pathfinding;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

using SupportUtils;
using NPC;

[System.Serializable]
public class NavAStar : MonoBehaviour, IPathfinder, INPCModule {

    #region Members
    [SerializeField]
    public bool EnableNPCModule = true;

    [SerializeField]
    public bool ClearPathOnArrival = false;

    [SerializeField]
    public float DiagonalPenalty = 0f;

    [SerializeField]
    public bool UseHeuristic = true;

    [SerializeField]
    public bool WeightHeuristic = false;

    [SerializeField]
    public float HeuristicWeight = 1f;

    private NPCController g_NPCController;
    private Vector3 g_TargetLocation;
    HashSet<NavNode> g_ClosedList;
    HashSet<NavNode> g_OpenList;
    private SortedList<float,NavNode> g_Fringe;
    private NavGrid g_Grid;

    #endregion

    #region Public_Functions

    // f(n) = g(n) + h(n)*e
    public float ComputeNodeCost(NavNode from, NavNode to, GRID_DIRECTION dir) {
        float totalCost = 0f;
        if (GRID_DIRECTION.CURRENT != dir) {
            NavNode.NODE_STATUS fromStatus = from.NodeStatus;
            switch(dir) {
            // diagonals
            case GRID_DIRECTION.NORTH_EAST:
            case GRID_DIRECTION.NORTH_WEST:
            case GRID_DIRECTION.SOUTH_EAST:
            case GRID_DIRECTION.SOUTH_WEST:
                if (from.NodeType == NavNode.NODE_TYPE.WALKABLE) {
                    totalCost = to.NodeType == NavNode.NODE_TYPE.HARD_TO_WALK ? 
                        (Mathf.Sqrt(2f) + Mathf.Sqrt(8f)) / 2f :
                        Mathf.Sqrt(2);
                } else if (from.NodeType == NavNode.NODE_TYPE.HARD_TO_WALK) {
                    totalCost = to.NodeType == NavNode.NODE_TYPE.HARD_TO_WALK ? 
                        Mathf.Sqrt(8) : 
                        Mathf.Sqrt(2);
                }
                if (from.NodeStatus == NavNode.NODE_STATUS.HARD_HIGHWAY) {
                    if (from.NodeStatus == NavNode.NODE_STATUS.HARD_HIGHWAY) {

                    }
                }
                break;
            // straights
            default:
                    // Highways
                    if (from.NodeStatus == NavNode.NODE_STATUS.HARD_HIGHWAY || 
                            to.NodeStatus == NavNode.NODE_STATUS.HARD_HIGHWAY) {
                        totalCost = from.NodeType == NavNode.NODE_TYPE.WALKABLE ?
                            (to.NodeType == NavNode.NODE_TYPE.WALKABLE ? 0.25f : 0.375f) :
                            (to.NodeType == NavNode.NODE_TYPE.WALKABLE ? 0.25f : 0.5f);
                    } 
                    // Regular
                    else {
                        if (from.NodeType == NavNode.NODE_TYPE.WALKABLE) {
                            totalCost = to.NodeType == NavNode.NODE_TYPE.HARD_TO_WALK ?
                                (float)to.NodeType - 0.5f :
                                (float)to.NodeType;
                        } else if (from.NodeType == NavNode.NODE_TYPE.HARD_TO_WALK) {
                            totalCost = (float)to.NodeType - 0.5f;
                        }
                    }
                break;
            }
        }
        Debug.Log("Cost calculated: " + totalCost);
        return totalCost;
    }

    public float ComputeNodeHeuristic(NavNode n) {
        float totalCost = 0f;
        if (UseHeuristic) {
            // euclidean distance heuristic
            totalCost += Vector3.Distance(n.Position, g_TargetLocation)
                * (WeightHeuristic ? HeuristicWeight : 1.0f);
        }
        return totalCost;
    }

    public List<Vector3> ConstructPath(NavNode goal, Dictionary<NavNode,NavNode> parents) {
        List<Vector3> path = new List<Vector3>(parents.Count + 1);
        List<NavNode> pathToPrint = new List<NavNode>(parents.Count + 1);
        path.Insert(0, goal.Position);
        pathToPrint.Insert(0, goal);
        bool done = false;
        NavNode curr = goal;
        while (!done) {
            if (curr == parents[curr]) done = true;
            curr.SetHighlightTile(true, curr == goal ? Color.green : Color.yellow, 0.8f);
            curr = parents[curr];
            path.Insert(0, curr.Position);
            pathToPrint.Insert(0, curr);
        }
        if(g_Grid != null) g_Grid.WritePathToFile(pathToPrint);
        return path;
    }

    public List<Vector3> FindPath(Vector3 from, Vector3 to) {
        ClearPath();
        RaycastHit hit;
        List<Vector3> pathList = new List<Vector3>();
        if (Physics.Raycast(new Ray(transform.position + (transform.up * 0.2f), -1 * transform.up), out hit)) {
            g_Grid = hit.collider.GetComponent<NavGrid>();
            NavNode fromNode = g_Grid.GetOccupiedNode(this);
            if(fromNode == null) {
                Debug.Log("NavAStar --> Agent is currently navigating in between nodes, try again please");
                return pathList;
            }
                
            NavNode goalNode = null;
            g_TargetLocation = to;
            Dictionary<NavNode, float> gVal = new Dictionary<NavNode, float>();
            Dictionary<NavNode, float> fVal = new Dictionary<NavNode, float>();
            g_ClosedList = new HashSet<NavNode>();
            g_OpenList = new HashSet<NavNode>();
            Dictionary<NavNode, NavNode> parents = new Dictionary<NavNode, NavNode>();
            
            g_Fringe = new SortedList<float, NavNode>(new DuplicateKeyComparer<float>());
            parents.Add(fromNode, fromNode);
            gVal.Add(fromNode, 0f);
            fVal.Add(fromNode,ComputeNodeHeuristic(fromNode));
            g_Fringe.Add(fVal[fromNode], fromNode);
            g_OpenList.Add(fromNode);
            g_ClosedList.Add(fromNode);

            while (g_OpenList.Count > 0) {

                // get next best node
                NavNode n = g_Fringe.Values[0];
                g_Fringe.RemoveAt(0);
                g_OpenList.Remove(n);
                g_ClosedList.Add(n);
                
                // test goal
                if (IsCurrentStateGoal(new System.Object[] { n })) {
                    n.SetHighlightTile(true, Color.green, 1f);
                    goalNode = n;
                    pathList = ConstructPath(goalNode, parents);
                    return pathList;
                }

                // loop adjacent
                Dictionary<NavNode, GRID_DIRECTION> neighbors = g_Grid.GetNeighborNodes(n);
                foreach(NavNode neighbor in neighbors.Keys) {
                    if(!g_ClosedList.Contains(neighbor) && neighbor.IsWalkable()) {
                        float val = gVal[n] + ComputeNodeCost(n, neighbor, neighbors[neighbor]);
                        bool inFringe = g_OpenList.Contains(neighbor);
                        if (!inFringe) {
                            gVal.Add(neighbor, val);
                            fVal.Add(neighbor, UseHeuristic ? ComputeNodeHeuristic(neighbor) + val : gVal[neighbor]);
                            parents.Add(neighbor, n);
                            g_OpenList.Add(neighbor);
                            g_Fringe.Add(fVal[neighbor], neighbor);
                            neighbor.DisplayWeight = fVal[neighbor].ToString();
                            neighbor.SetHighlightTile(true, Color.white, 0.4f);
                        }
                        if (val < gVal[n]) {
                            parents.Add(neighbor, n);
                        }
                    }
                }
            }
        } else {
            g_NPCController.Debug("NavAStar --> Pathfinder not on grid");    
        }
        return pathList;
    }

    public void ClearPath() {
        foreach (NavNode n in g_ClosedList) {
            n.SetHighlightTile(false, Color.black, 0f);
        }
        foreach (NavNode n in g_OpenList) {
            n.SetHighlightTile(false, Color.black, 0f);
        }
    }

    public bool IsEnabled() {
        return EnableNPCModule;
    }

    public bool IsReachable(Vector3 from, Vector3 target) {
        throw new NotImplementedException();
    }

    public string NPCModuleName() {
        return "A* Algorithm";
    }

    public NPC_MODULE_TYPE NPCModuleType() {
        return NPC_MODULE_TYPE.PATHFINDER;
    }

    public NPC_MODULE_TARGET NPCModuleTarget() {
        return NPC_MODULE_TARGET.AI;
    }

    public void SetEnable(bool e) {
        EnableNPCModule = e;
    }

    public string ObjectIdentifier() {
        return gameObject.name;
    }

    public void RemoveNPCModule() {
        GetComponent<NPCController>().RemoveNPCModule(this);
    }
    #endregion

    #region Unity_methods

    void Start() {
        g_NPCController = GetComponent<NPCController>();
        g_Fringe = new SortedList<float, NavNode>(new DuplicateKeyComparer<float>());
        g_ClosedList = new HashSet<NavNode>();
        g_OpenList = new HashSet<NavNode>();
    }

    void Update() {
        if(g_TargetLocation != Vector3.zero
            && ClearPathOnArrival 
            && Vector3.Distance(g_NPCController.transform.position, g_TargetLocation) < 0.5f) {
            ClearPath();
            g_TargetLocation = Vector3.zero;
        }
    }

    void OnDestroy() {
        RemoveNPCModule();
    }

    public bool IsCurrentStateGoal(System.Object[] states = null) {
        NavNode curNode = (NavNode)states[0];
        return Vector3.Distance(curNode.Position, g_TargetLocation) < curNode.Radius * 2f;
    }

    #endregion

}
