using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;


namespace Pathfinding {

    public enum GRID_SCALE {
        ONE = 1,
        HALF = 2,
        QUARTER = 4
    }

    public enum GRID_DIRECTION {
        CURRENT,
        NORTH,
        SOUTH,
        EAST,
        WEST,
        NORTH_EAST,
        NORTH_WEST,
        SOUTH_EAST,
        SOUTH_WEST
    }

    public class NavGrid : MonoBehaviour {

        #region Properties
        #endregion

        #region Members
        private Dictionary<IPathfinder, NavNode> g_WalkedOnNodes;
        public bool         WriteGridToFile = false;
        public string       FileName = "Grid_Description.txt";
        public bool         RedrawGrid;
        public bool         CreateRivers;
        public LayerMask    UnwalkableMask;
        public Vector2      GridDimensions;
        public GRID_SCALE   GridScale = GRID_SCALE.ONE;
        private float       g_GridScale = 1.0f;
        public bool         PaintGridOnScene = false;
        public bool         PaintPathdOnPlay = false;
        public bool         DisplayTileText = false;
        public float        GridTransparency = 1.0f;
        private float       g_NodeRadius = 0.5f;
        public float        BlockingHeight = 2.0f;
        NavNode[,]          g_Grid;
        public float        NormalWeight = 1;
        public float        MediumWeight = 50;
        public float        NotAvailableWeight = 100;
        private bool        g_TileSelected = false;
        private NavNode     g_SelectedTile;
        public float        SelectedTileWeight = 1;
        public Vector2      SelectedTile;
        #endregion

        #region Private_Functions

        private void PopulateGrid() {
            g_WalkedOnNodes = new Dictionary<IPathfinder, NavNode>();
            g_GridScale = 1f / (float) GridScale;
            float nodeDiameter = g_NodeRadius * 2 * g_GridScale;
            GridDimensions.x = Mathf.RoundToInt(transform.localScale.x / nodeDiameter);
            GridDimensions.y = Mathf.RoundToInt(transform.localScale.z / nodeDiameter);
            int tilesX = (int) GridDimensions.x,
                tilesY = (int) GridDimensions.y;
            g_Grid = new NavNode[tilesX, tilesY];
            Vector3 gridWorldBottom = (transform.position - (transform.right * GridDimensions.x / 2) -
                (transform.forward * GridDimensions.y / 2) + new Vector3(g_NodeRadius,0.0f,g_NodeRadius)) * g_GridScale;
            for(int row = 0; row < tilesX; ++row) {
                for (int col = 0; col < tilesY; ++col) {
                    NavNode node = new NavNode(
                        gridWorldBottom + (transform.right * (nodeDiameter) * row) + transform.forward * (nodeDiameter) * col,
                        new Vector2(row,col),
                        transform.up,
                        BlockingHeight,
                        true,
                        g_NodeRadius * g_GridScale,
                        this);
                    g_Grid[row, col] = node;
                }
            }

        }

        #endregion Private_Functions

        #region Unity_Methods

        // Use this for initialization
        void Reset() {
            PopulateGrid();
        }
        
        void Awake() {
            PopulateGrid();
            g_WalkedOnNodes = new Dictionary<IPathfinder, NavNode>();
            if(WriteGridToFile) {
                if(File.Exists(FileName)) {
                    FileName = FileName.Substring(0, FileName.IndexOf(".txt")) + "_copy.txt";
                    Debug.Log("File " + FileName + " already exists - creating copy: " + FileName);
                    // TODO - create file here
                }
                StreamWriter sw = File.CreateText(FileName);
                sw.WriteLine("Grid Specs");
                sw.WriteLine("-----------\n\n");
                sw.Close();
            }
        }

        void Update() {
            SelectedTile.x = Mathf.Clamp(SelectedTile.x, 0, g_Grid.GetLength(0) - 1);
            SelectedTile.y = Mathf.Clamp(SelectedTile.y, 0, g_Grid.GetLength(1) - 1);
            foreach (NavNode node in g_Grid) {
                node.IsWalkable();
                node.SetActiveTileText(DisplayTileText);
                
            }
        }

        void OnDestroy() {
            foreach(Transform t in transform) {
                GameObject.DestroyImmediate(t.gameObject);
            }
        }

        // Update is called once per frame
        
        void OnDrawGizmos() {

            if (NotAvailableWeight < MediumWeight) {
                NotAvailableWeight = MediumWeight + 1;
            } else if (MediumWeight < NormalWeight) {
                MediumWeight = NormalWeight + 1;
            } else if (NormalWeight <= 0)
                NormalWeight = 1f;

            // x for rows, y for cols
            Gizmos.DrawWireCube(transform.position, new Vector3(GridDimensions.x * g_GridScale, transform.position.y , GridDimensions.y * g_GridScale));
            if(g_Grid == null || g_Grid.GetLength(0) != GridDimensions.x || g_Grid.GetLength(1) != GridDimensions.y || RedrawGrid) {
                Reset();
                RedrawGrid = false;
            } else if (PaintGridOnScene) {

                SelectedTile.x = Mathf.Clamp(SelectedTile.x, 0, g_Grid.GetLength(0) - 1);
                SelectedTile.y = Mathf.Clamp(SelectedTile.y, 0, g_Grid.GetLength(1) - 1);

                if (g_SelectedTile != null) {
                    g_SelectedTile.Weight = SelectedTileWeight > 0 ? SelectedTileWeight : 1;
                    g_SelectedTile.Selected = false;
                }
                NavNode tmp = g_Grid[(int)SelectedTile.x, (int)SelectedTile.y];
                if (g_SelectedTile != tmp) {
                    SelectedTileWeight = tmp.Weight;
                }
                g_SelectedTile = tmp;
                g_SelectedTile.Selected = true;
                g_SelectedTile = tmp;

                foreach(NavNode node in g_Grid) {
                    if(!Application.isPlaying)
                        node.IsWalkable();
                    Color c;
                    if(node.Selected) {
                        c = Color.white;
                        c.a = 1.0f * GridTransparency;
                    } else {
                        if (node.Weight >= NotAvailableWeight || !node.Available) {
                            c = Color.red;
                            c.a = 0.5f * GridTransparency;
                        } else if (node.Weight >= MediumWeight) {
                            c = Color.yellow;
                            c.a = 0.3f * GridTransparency;
                        } else if (node.Weight >= NormalWeight) {
                            c = Color.green;
                            c.a = 0.1f * GridTransparency;
                        } else {
                            c = Color.blue;
                            c.a = 0.8f * GridTransparency;
                        }
                    }

                    Gizmos.color = c;
                    float diam = node.Radius * 2;
                    Gizmos.DrawWireCube(node.Position, new Vector3(diam, transform.position.y, diam));
                    Gizmos.color = Color.white;
                }
            }
        }
        #endregion

        #region Public_Functions
        
        public void SetIPathfinderNode(IPathfinder ipf, NavNode node) {
            if(g_WalkedOnNodes.ContainsKey(ipf) && g_WalkedOnNodes[ipf] != node) {
                if(Application.isPlaying) g_WalkedOnNodes[ipf].SetHighlightTile(false, Color.grey, 0.5f);
                g_WalkedOnNodes.Remove(ipf);
            } else if(!g_WalkedOnNodes.ContainsKey(ipf)) {
                g_WalkedOnNodes.Add(ipf, node);
                if (Application.isPlaying) node.SetHighlightTile(true, Color.red, 0.5f);
            }
        }

        public NavNode GetOccupiedNode(IPathfinder ipf) {
            return g_WalkedOnNodes[ipf];
        }

        public bool IsValid(Vector2 coord) {
            return 
                !(coord.x < 0 || coord.y < 0) &&
                (coord.x < GridDimensions.x) && 
                (coord.y < GridDimensions.y) && 
                (g_Grid[(int) coord.x, (int) coord.y].IsWalkable());
        }
        public NavNode GetNeighborNode(NavNode current, GRID_DIRECTION dir) {
            return null;
        }

        /// <summary>
        /// Returns all exisitng neighbors
        /// </summary>
        /// <param name="current node"></param>
        /// <returns></returns>
        public Dictionary<NavNode,GRID_DIRECTION> GetNeighborNodes(NavNode current) {

            Dictionary<NavNode, GRID_DIRECTION> neighbors = new Dictionary<NavNode, GRID_DIRECTION>();
            int x = (int) current.GridPosition.x, y = (int) current.GridPosition.y;
            
            for(int i = -1; i < 2; ++i) {

                if (x + i >= g_Grid.GetLength(0)) continue;             // skip east
                else if (x + i < 0) continue;                           // skip west

                for (int j = 1; j > -2; --j) {

                    if (i == 0 && j == 0) continue;                     // skip center
                    else if (y + j >= g_Grid.GetLength(1)) continue;    // skip north
                    else if (y + j < 0) continue;                       // skip south 

                    GRID_DIRECTION dir = GRID_DIRECTION.CURRENT;        // dummy
                    if      (i == -1 && j ==  1) dir = GRID_DIRECTION.NORTH_WEST;
                    else if (i ==  0 && j ==  1) dir = GRID_DIRECTION.NORTH;
                    else if (i ==  1 && j ==  1) dir = GRID_DIRECTION.NORTH_EAST;
                    else if (i == -1 && j ==  0) dir = GRID_DIRECTION.WEST;
                    else if (i ==  1 && j ==  0) dir = GRID_DIRECTION.EAST;
                    else if (i ==  0 && j == -1) dir = GRID_DIRECTION.SOUTH;
                    else if (i == -1 && j == -1) dir = GRID_DIRECTION.SOUTH_WEST;
                    else if (i ==  1 && j == -1) dir = GRID_DIRECTION.SOUTH_EAST;
                    neighbors.Add(g_Grid[x + i, y + j], dir);

                }
            }

            return neighbors;
        }
        #endregion
    }
}
