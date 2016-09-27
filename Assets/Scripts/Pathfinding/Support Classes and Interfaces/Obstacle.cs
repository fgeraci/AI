using UnityEngine;
using System.Collections;

namespace Pathfinding {
    
    public enum OBSTACLE_TYPE {
        BLOCKER = 100,
        HARDENER = 10
    }

    public class Obstacle : MonoBehaviour {


        public Vector3          Dimensions;
        public Vector3          Location;
        public OBSTACLE_TYPE    ObstacleType = OBSTACLE_TYPE.BLOCKER;
        public float            Weight;
        
        void Reset() {
            Weight = ObstacleType == OBSTACLE_TYPE.BLOCKER ? 
                (float)OBSTACLE_TYPE.BLOCKER : (float) OBSTACLE_TYPE.HARDENER;
            Location = transform.position;
            Dimensions = transform.localScale;
        }

    }

}
