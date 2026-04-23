using System.Collections.Generic;
using UnityEngine;

public class EnemyPathfinder : MonoBehaviour
{
    [System.Serializable]
    public class Node
    {
        public bool isWall;
        public Node parent;
        public int x, y;
        public int g, h;
        public int f => g + h;

        public Node(bool isWall, int x, int y)
        {
            this.isWall = isWall;
            this.x = x;
            this.y = y;
            g = int.MaxValue;
            h = 0;
            parent = null;
        }
    }

    [SerializeField] private Vector2Int bottomLeft;
    [SerializeField] private Vector2Int topRight;

    [HideInInspector] public Vector2Int startPos;
    [HideInInspector] public Vector2Int targetPos;

    [SerializeField] private bool allowDiagonal = false;
    [SerializeField] private bool dontCrossCorner = true;

    [Header("Grid Mapping")]
    public float cellSize = 1f;

    [SerializeField] private Vector2 cellOffset = new Vector2(0.5f, 0f);

    [Header("Obstacle Check")]
    [SerializeField] private Vector2 overlapBoxSize = new Vector2(0.9f, 0.9f);
    [SerializeField] private string obstacleLayerName = "Obstacle";

    [HideInInspector] public List<Node> FinalNodeList = new List<Node>();

    private int sizeX, sizeY;
    private Node[,] nodes;
    private int obstacleMask;

    private static readonly Vector2Int[] CardinalDirs =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private static readonly Vector2Int[] AllDirs =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1),
        new Vector2Int(1, -1)
    };

    private void Awake()
    {
        obstacleMask = LayerMask.GetMask(obstacleLayerName);
    }

    public Vector2Int WorldToGrid(Vector2 world)
    {
        float gx = (world.x - cellOffset.x) / cellSize;
        float gy = (world.y - cellOffset.y) / cellSize;
        return new Vector2Int(Mathf.RoundToInt(gx), Mathf.RoundToInt(gy));
    }

    public Vector2 GridToWorld(Vector2Int grid)
    {
        return new Vector2(grid.x * cellSize + cellOffset.x, grid.y * cellSize + cellOffset.y);
    }

    public bool TryGetNextWorld(out Vector2 nextWorld)
    {
        startPos.x = Mathf.Clamp(startPos.x, bottomLeft.x, topRight.x);
        startPos.y = Mathf.Clamp(startPos.y, bottomLeft.y, topRight.y);
        targetPos.x = Mathf.Clamp(targetPos.x, bottomLeft.x, topRight.x);
        targetPos.y = Mathf.Clamp(targetPos.y, bottomLeft.y, topRight.y);

        FinalNodeList.Clear();

        sizeX = topRight.x - bottomLeft.x + 1;
        sizeY = topRight.y - bottomLeft.y + 1;

        nodes = new Node[sizeX, sizeY];

        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                int gx = bottomLeft.x + i;
                int gy = bottomLeft.y + j;

                Vector2 center = GridToWorld(new Vector2Int(gx, gy));
                bool isWall = Physics2D.OverlapBox(center, overlapBoxSize, 0f, obstacleMask) != null;

                nodes[i, j] = new Node(isWall, gx, gy);
            }
        }

        Node start = nodes[startPos.x - bottomLeft.x, startPos.y - bottomLeft.y];
        Node target = nodes[targetPos.x - bottomLeft.x, targetPos.y - bottomLeft.y];

        start.isWall = false;
        target.isWall = false;

        start.g = 0;
        start.h = Heuristic(start, target);

        List<Node> open = new List<Node> { start };
        HashSet<Node> closed = new HashSet<Node>();

        Node bestNode = start;

        while (open.Count > 0)
        {
            Node cur = open[0];
            for (int k = 1; k < open.Count; k++)
            {
                Node n = open[k];
                bool better = n.f < cur.f || (n.f == cur.f && n.h < cur.h);
                if (better)
                    cur = n;
            }

            open.Remove(cur);
            closed.Add(cur);

            if (cur.h < bestNode.h || (cur.h == bestNode.h && cur.g < bestNode.g))
                bestNode = cur;

            if (cur == target)
            {
                nextWorld = BuildNextStepWorld(start, target);
                return true;
            }

            Vector2Int[] dirs = allowDiagonal ? AllDirs : CardinalDirs;

            foreach (var d in dirs)
            {
                int nx = cur.x + d.x;
                int ny = cur.y + d.y;

                if (nx < bottomLeft.x || nx > topRight.x || ny < bottomLeft.y || ny > topRight.y)
                    continue;

                Node nb = nodes[nx - bottomLeft.x, ny - bottomLeft.y];
                if (nb.isWall || closed.Contains(nb))
                    continue;

                if (allowDiagonal && dontCrossCorner && Mathf.Abs(d.x) == 1 && Mathf.Abs(d.y) == 1)
                {
                    Node side1 = nodes[nx - bottomLeft.x, cur.y - bottomLeft.y];
                    Node side2 = nodes[cur.x - bottomLeft.x, ny - bottomLeft.y];

                    if (side1.isWall || side2.isWall)
                        continue;
                }

                int stepCost = (d.x == 0 || d.y == 0) ? 10 : 14;
                int cost = cur.g + stepCost;

                if (cost < nb.g)
                {
                    nb.g = cost;
                    nb.h = Heuristic(nb, target);
                    nb.parent = cur;

                    if (!open.Contains(nb))
                        open.Add(nb);
                }
            }
        }

        if (bestNode != null && bestNode != start)
        {
            nextWorld = BuildNextStepWorld(start, bestNode);
            return true;
        }

        nextWorld = GridToWorld(startPos);
        return false;
    }

    public Vector2 PathFinding()
    {
        if (TryGetNextWorld(out Vector2 nextWorld))
            return nextWorld;

        return GridToWorld(startPos);
    }

    private int Heuristic(Node a, Node b)
    {
        return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y)) * 10;
    }

    private Vector2 BuildNextStepWorld(Node start, Node endNode)
    {
        FinalNodeList.Clear();

        Node p = endNode;
        while (p != null && p != start)
        {
            FinalNodeList.Add(p);
            p = p.parent;
        }

        FinalNodeList.Add(start);
        FinalNodeList.Reverse();

        if (FinalNodeList.Count >= 2)
        {
            Node nxt = FinalNodeList[1];
            return GridToWorld(new Vector2Int(nxt.x, nxt.y));
        }

        return GridToWorld(new Vector2Int(start.x, start.y));
    }

    private void OnDrawGizmos()
    {
        if (nodes == null) return;

        Vector3 cubeSize = new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0.9f);

        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                Node c = nodes[i, j];
                Vector2 wpos2 = GridToWorld(new Vector2Int(c.x, c.y));
                Vector3 wpos = new Vector3(wpos2.x, wpos2.y, 0f);

                Gizmos.color = c.isWall ? Color.red : Color.green;
                Gizmos.DrawWireCube(wpos, cubeSize);
            }
        }

        if (FinalNodeList != null && FinalNodeList.Count > 1)
        {
            Gizmos.color = Color.blue;
            for (int k = 0; k < FinalNodeList.Count - 1; k++)
            {
                Vector2 a2 = GridToWorld(new Vector2Int(FinalNodeList[k].x, FinalNodeList[k].y));
                Vector2 b2 = GridToWorld(new Vector2Int(FinalNodeList[k + 1].x, FinalNodeList[k + 1].y));
                Gizmos.DrawLine(new Vector3(a2.x, a2.y, 0f), new Vector3(b2.x, b2.y, 0f));
            }
        }
    }
}