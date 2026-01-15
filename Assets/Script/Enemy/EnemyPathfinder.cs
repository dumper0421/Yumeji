// EnemyPathfinder.cs
using System.Collections.Generic;
using UnityEngine;

public class EnemyPathfinder : MonoBehaviour
{
    [System.Serializable]
    public class Node
    {
        public bool isWall;
        public Node parent;
        public int x, y;   // grid coord
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

    public Vector2Int bottomLeft;
    public Vector2Int topRight;

    [HideInInspector] public Vector2Int startPos;
    [HideInInspector] public Vector2Int targetPos;

    public bool allowDiagonal = false;
    public bool dontCrossCorner = true;

    [Header("Grid Mapping")]
    public float cellSize = 1f;

    // 타일맵/그리드가 0.5 올라가있다 했으니 기본값을 (0.5, 0.5)로 둠
    // 씬에 따라 y만 어긋나면 (0.5, 0) 또는 (0.5, 1.0)처럼 여기만 조정하면 됨
    public Vector2 cellOffset = new Vector2(0.5f, 0f);

    [Header("Obstacle Check")]
    public Vector2 overlapBoxSize = new Vector2(0.9f, 0.9f);
    public string obstacleLayerName = "Obstacle";

    [HideInInspector] public List<Node> FinalNodeList = new List<Node>();

    private int sizeX, sizeY;
    private Node[,] nodes;

    private int obstacleMask;

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

    public Vector2 PathFinding()
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

        // 시작/목표가 잘못 벽 판정되면 길이 끊겨서 멈추는 원인이 됨
        start.isWall = false;
        target.isWall = false;

        start.g = 0;
        start.h = (Mathf.Abs(start.x - target.x) + Mathf.Abs(start.y - target.y)) * 10;

        List<Node> open = new List<Node> { start };
        HashSet<Node> closed = new HashSet<Node>();

        while (open.Count > 0)
        {
            Node cur = open[0];
            for (int k = 1; k < open.Count; k++)
            {
                Node n = open[k];
                bool better =
                    n.f < cur.f ||
                    (n.f == cur.f && n.h < cur.h);
                if (better) cur = n;
            }

            open.Remove(cur);
            closed.Add(cur);

            if (cur == target)
            {
                Node p = target;
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

            Vector2Int[] dirs = allowDiagonal
                ? new Vector2Int[]
                {
                    new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1),
                    new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(-1,-1), new Vector2Int(1,-1)
                }
                : new Vector2Int[]
                {
                    new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1)
                };

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

                    // 코너 끼고 비집고 들어가는 걸 막으려면 OR가 맞음
                    if (side1.isWall || side2.isWall)
                        continue;
                }

                int stepCost = (d.x == 0 || d.y == 0) ? 10 : 14;
                int cost = cur.g + stepCost;

                if (cost < nb.g)
                {
                    nb.g = cost;
                    nb.h = (Mathf.Abs(nb.x - target.x) + Mathf.Abs(nb.y - target.y)) * 10;
                    nb.parent = cur;
                    if (!open.Contains(nb)) open.Add(nb);
                }
            }
        }

        // 길이 없으면: 현재 셀 중심으로 유지 (이게 멈춘 것처럼 보였던 케이스)
        return GridToWorld(startPos);
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
