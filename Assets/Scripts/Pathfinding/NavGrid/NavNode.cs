using UnityEngine;
using System.Collections;

namespace Pathfinding {

    public class NavNode {

        #region Enums
        public enum NODE_TYPE {
            WALKABLE = 1,
            HARD_TO_WALK = 2,
            NONWALKABLE = 3
        }
        #endregion

        #region Constructor
        public NavNode(Vector3 position, Vector2 gridPos, bool walkable, float radius, NavGrid grid) {
            g_Radius = radius;
            g_Position = position;
            g_Walkable = walkable;
            g_NodeType = walkable ? NODE_TYPE.WALKABLE  // hard to walk is also walkable at this point
                : NODE_TYPE.NONWALKABLE;
            g_GridPosition = gridPos;
            g_Grid = grid;
        }
        public NavNode(Vector3 position, Vector2 gridPos, Vector3 up, bool walkable, float radius, NavGrid grid) 
            : this(position,gridPos, walkable, radius, grid) {
            g_Up = up;
        }
        public NavNode(Vector3 position, Vector2 gridPos, Vector3 up, float blockingHeight, bool walkable, float radius, NavGrid grid) 
            : this(position, gridPos, up, walkable, radius, grid) {
            g_BlockingHeight = blockingHeight;
        }
        #endregion

        #region Properties

        public NODE_TYPE NodeType {
            get { return g_NodeType; }
            set { g_NodeType = value; }
        }

        public bool Available;

        public float Radius {
            get { return g_Radius; }
        }
        public Vector3 Position {
            get { return g_Position; }
        }
        public Vector2 GridPosition {
            get {  return g_GridPosition; }             
        }
        public float BlockingHeight {
            get { return g_BlockingHeight; }
        }
        public Vector3 Up {
            get { return g_Up; }
        }
        public bool Walkable {
            get { return g_Walkable; }
        }
        public float Weight {
            get { return (float) g_NodeType; }
            set { g_Weight = value;  }
        }
        public bool Selected;
        #endregion

        #region Members
        private NODE_TYPE   g_NodeType;
        private GameObject  g_Tile;
        private GameObject  g_TileText;
        private NavGrid     g_Grid;
        private float       g_Radius;
        private Vector3     g_Position;
        private Vector3     g_Up;
        private float       g_BlockingHeight;
        private bool        g_Walkable;
        private float       g_Weight= 1;
        private Vector2     g_GridPosition;
        #endregion

        #region Public_Functions

        public void SetHighlightTile(bool h, Color c, float alpha) {
            if(h) {
                if (g_Tile != null) goto destroy_tile;
                g_Tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g_Tile.GetComponent<BoxCollider>().isTrigger = true;
                CreateTileText();
                Material m = new Material(Shader.Find("Standard"));
                c.a = alpha;
                m.SetColor("_Color", c);
                /* all these just to make the color's alpha */
                m.SetFloat("_Mode", 3);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = 3000;
                /* ---------------------- */
                g_Tile.GetComponent<Renderer>().material = m;
                g_Tile.layer = LayerMask.NameToLayer("Ignore Raycast");
                g_Tile.transform.position = Position + Up * 0.05f;
                g_Tile.transform.localScale = new Vector3(Radius * 2f, 0.01f, Radius * 2f);
                g_Tile.transform.parent = g_Grid.transform;
                return;
            }
destroy_tile:
            if(g_Tile != null) {
                GameObject.DestroyImmediate(g_Tile);
                g_Tile = null;
            }
            if (h) SetHighlightTile(h, c, alpha);
        }

        public override string ToString() {
            return "NavNode @ ["+g_Position+"]";
        }

        public void SetActiveTileText(bool active) {
            if (g_TileText != null)
                g_TileText.SetActive(active);
        }



        public bool IsWalkable() {
            
            if (g_Up != null) {
                Ray ray = new Ray(g_Position, g_Up);
                RaycastHit hit;
                // nothing is on top of the node nor colliding by its radius
                Available =  !(Physics.Raycast(Position, Up, out hit, g_BlockingHeight) ||
                          Physics.Raycast(Position + new Vector3(Radius,0,0), Up, g_BlockingHeight) ||
                          Physics.Raycast(Position + new Vector3(-Radius, 0, 0), Up, g_BlockingHeight) ||
                          Physics.Raycast(Position + new Vector3(0, 0, Radius), Up, g_BlockingHeight) ||
                          Physics.Raycast(Position + new Vector3(0, 0, -Radius), Up, g_BlockingHeight));
                if(hit.collider) {
                    IPathfinder ipf = hit.collider.GetComponent<IPathfinder>();
                    if (ipf != null) {
                        g_Grid.SetIPathfinderNode(ipf, this);
                    }
                }
                return Available;
            } else return true;
        }
        #endregion

        #region Private_Functions
        private void CreateTileText() {
            g_TileText = new GameObject();
            g_TileText.name = "TileText";
            g_TileText.transform.Rotate(Up, 90f);
            g_TileText.transform.Rotate(g_Tile.transform.right, 90f);
            g_TileText.transform.localScale = new Vector3(Radius, Radius, Radius);
            g_TileText.transform.localPosition = g_Tile.transform.position + (Up * 0.2f);
            TextMesh tm = g_TileText.AddComponent<TextMesh>();
            tm.color = Color.green;
            tm.fontSize = 20;
            tm.characterSize = 0.2f;
            tm.anchor = TextAnchor.UpperCenter;
            g_TileText.transform.parent = g_Tile.transform;
            tm.text = "Weight: " + Weight;
        }
        #endregion

    }
}