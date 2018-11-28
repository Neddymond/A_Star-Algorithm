using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class Grids: MonoBehaviour
    {
        public LayerMask unwalkableMask;
        public float nodeRadius;
        public Vector2 gridWorldSize;
        public bool displayGridGizmos;
        public TerrainType[] walkableRegions;
        LayerMask walkableMask;
        Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>(); 

        Node[,] grid;

        float nodeDiameter;
        int gridSizeX, gridSizeY;

        public void Awake()
        {
            nodeDiameter = nodeRadius * 2;
            gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
            gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

            foreach (TerrainType region in walkableRegions)
            {
                walkableMask.value += region.terrainMask.value;
                walkableRegionsDictionary.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
            }

            CreateGrid();
        }

        public int MaxSize
        {
            get
            {
                return gridSizeX * gridSizeY;
            }
        }

        void CreateGrid()
        {
            grid = new Node[gridSizeX, gridSizeX];
            Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                    bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));

                    int movementPenalty = 0;

                    if (walkable)
                    {
                        Ray ray = new Ray(worldPoint + Vector3.up * 50, Vector3.down);
                        RaycastHit hit;
                        if (Physics.Raycast(ray, out hit, 100, walkableMask))
                        {
                            walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                        }
                    }

                    grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty);
                }
            }
        }

        public List<Node> GetNeighbours(Node node)
        {
            List<Node> neighbours = new List<Node>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;

                    if(checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                    {
                        neighbours.Add(grid[checkX, checkY]);
                    }
                }
            }

            return neighbours;
        }

        public Node NodeFromWorldPoint(Vector3 worldPosition)
        {
            float PercentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
            float PercentY = (worldPosition.z  + gridWorldSize.y / 2) / gridWorldSize.y;
            PercentX = Mathf.Clamp01(PercentX);
            PercentY = Mathf.Clamp01(PercentY);

            int x = Mathf.RoundToInt((gridSizeX - 1) * PercentX);
            int y = Mathf.RoundToInt((gridSizeY - 1) * PercentY);
            return grid[x, y];
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

            if (grid != null && displayGridGizmos)
            {
                foreach (Node n in grid)
                {
                    Gizmos.color = (n.walkable) ? Color.white : Color.red;
                    Gizmos.DrawCube(n.worldPostion, Vector3.one * (nodeDiameter - .1f));
                }
            }
        }

        [System.Serializable] 
        public class TerrainType
        {
            public LayerMask terrainMask;
            public int terrainPenalty;
        }
    }
}
