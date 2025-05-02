using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D 격자 기반 A* Pathfinding 컴포넌트
/// 모든 셀이 (n+0.5,m+0.5) 중심을 가지며, 장애물 감지와 경로 계산을 셀 중심 기준으로 수행합니다.
/// </summary>
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

    [Header("Grid Bounds Inclusive")]
    public Vector2Int bottomLeft;
    public Vector2Int topRight;

    [HideInInspector] public Vector2Int startPos;
    [HideInInspector] public Vector2Int targetPos;
    public bool allowDiagonal = false;
    public bool dontCrossCorner = true;

    [HideInInspector] public List<Node> FinalNodeList = new List<Node>();

    private int sizeX, sizeY;
    private Node[,] nodes;

    /// <summary>
    /// A* 탐색 수행. 다음 셀 중심 월드 좌표 (n+0.5, m+0.5) 반환.
    /// </summary>
    public Vector2 PathFinding()
    {
        // 1. Clamp grid indices
        startPos.x = Mathf.Clamp(startPos.x, bottomLeft.x, topRight.x);
        startPos.y = Mathf.Clamp(startPos.y, bottomLeft.y, topRight.y);
        targetPos.x = Mathf.Clamp(targetPos.x, bottomLeft.x, topRight.x);
        targetPos.y = Mathf.Clamp(targetPos.y, bottomLeft.y, topRight.y);

        FinalNodeList.Clear();

        // 2. Initialize grid
        sizeX = topRight.x - bottomLeft.x + 1;
        sizeY = topRight.y - bottomLeft.y + 1;
        nodes = new Node[sizeX, sizeY];
        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                Vector2 center = new Vector2(bottomLeft.x + i + 0.5f, bottomLeft.y + j + 0.5f);
                bool isWall = Physics2D.OverlapCircle(center, 0.3f, LayerMask.GetMask("Obstacle")) != null;
                nodes[i, j] = new Node(isWall, bottomLeft.x + i, bottomLeft.y + j);
            }
        }

        // 3. Setup start/target nodes
        Node start = nodes[startPos.x - bottomLeft.x, startPos.y - bottomLeft.y];
        Node target = nodes[targetPos.x - bottomLeft.x, targetPos.y - bottomLeft.y];
        start.g = 0;
        start.h = (Mathf.Abs(start.x - target.x) + Mathf.Abs(start.y - target.y)) * 10;

        List<Node> open = new List<Node> { start };
        HashSet<Node> closed = new HashSet<Node>();

        // 4. A* Loop
        while (open.Count > 0)
        {
            Node cur = open[0];
            for (int k = 1; k < open.Count; k++)
            {
                Node n = open[k];
                if (n.f < cur.f || (n.f == cur.f && n.h < cur.h)) cur = n;
            }
            open.Remove(cur);
            closed.Add(cur);

            // Check target reached
            if (cur == target)
            {
                // Retrace path
                Node p = target;
                while (p != start)
                {
                    FinalNodeList.Add(p);
                    p = p.parent;
                }
                FinalNodeList.Add(start);
                FinalNodeList.Reverse();

                // Return next center
                if (FinalNodeList.Count >= 2)
                {
                    Node nxt = FinalNodeList[1];
                    return new Vector2(nxt.x + 0.5f, nxt.y + 0.5f);
                }
                return new Vector2(start.x + 0.5f, start.y + 0.5f);
            }

            // Explore neighbors
            Vector2Int[] dirs = allowDiagonal
                ? new Vector2Int[] { new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1),
                                      new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(-1,-1), new Vector2Int(1,-1) }
                : new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

            foreach (var d in dirs)
            {
                int nx = cur.x + d.x;
                int ny = cur.y + d.y;
                if (nx < bottomLeft.x || nx > topRight.x || ny < bottomLeft.y || ny > topRight.y) continue;

                Node nb = nodes[nx - bottomLeft.x, ny - bottomLeft.y];
                if (nb.isWall || closed.Contains(nb)) continue;

                if (allowDiagonal && dontCrossCorner && Mathf.Abs(d.x) == 1 && Mathf.Abs(d.y) == 1)
                {
                    Node h = nodes[nx - bottomLeft.x, cur.y - bottomLeft.y];
                    Node v = nodes[cur.x - bottomLeft.x, ny - bottomLeft.y];
                    if (h.isWall && v.isWall) continue;
                }

                int cost = cur.g + ((d.x == 0 || d.y == 0) ? 10 : 14);
                if (cost < nb.g)
                {
                    nb.g = cost;
                    nb.h = (Mathf.Abs(nb.x - target.x) + Mathf.Abs(nb.y - target.y)) * 10;
                    nb.parent = cur;
                    if (!open.Contains(nb)) open.Add(nb);
                }
            }
        }

        // No path: return start cell center
        return new Vector2(startPos.x + 0.5f, startPos.y + 0.5f);
    }

    private void OnDrawGizmos()
    {
        if (nodes == null) return;
        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                Node c = nodes[i, j];
                Vector3 pos = new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);
                Gizmos.color = c.isWall ? Color.red : Color.green;
                Gizmos.DrawWireCube(pos, Vector3.one * 0.9f);
            }
        }
        if (FinalNodeList != null && FinalNodeList.Count > 1)
        {
            Gizmos.color = Color.blue;
            for (int k = 0; k < FinalNodeList.Count - 1; k++)
            {
                var a = FinalNodeList[k];
                var b = FinalNodeList[k + 1];
                Gizmos.DrawLine(new Vector3(a.x + 0.5f, a.y + 0.5f, 0f), new Vector3(b.x + 0.5f, b.y + 0.5f, 0f));
            }
        }
    }
}
