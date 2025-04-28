using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPathfinder : MonoBehaviour
{
    [System.Serializable]
    public class Node
    {
        public Node(bool _isWall, int _x, int _y)
        {
            isWall = _isWall;
            x = _x;
            y = _y;
            G = int.MaxValue; // 최소 비용 초기화
            H = 0;
        }

        public bool isWall;
        public Node ParentNode;

        public int x, y;
        public int G, H;
        public int F { get { return G + H; } }
    }

    public Vector2Int bottomLeft, topRight, startPos, targetPos, endPos;
    public List<Node> FinalNodeList;
    public bool allowDiagonal, dontCrossCorner;

    int sizeX, sizeY;
    Node[,] NodeArray;
    Node StartNode, TargetNode, CurNode;
    List<Node> OpenList, ClosedList;

    private void Start()
    {
        // PathFinding을 Start에서 호출하거나 필요할 때 호출하세요.
        PathFinding();
    }

    public Vector2 PathFinding()
    {
        FinalNodeList = new List<Node>();

        // NodeArray의 크기 결정
        sizeX = topRight.x - bottomLeft.x + 1;
        sizeY = topRight.y - bottomLeft.y + 1;
        NodeArray = new Node[sizeX, sizeY];

        // 노드 생성 및 isWall 판별
        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                bool isWall = false;
                Vector2 nodePos = new Vector2(i + bottomLeft.x, j + bottomLeft.y);
                foreach (Collider2D col in Physics2D.OverlapCircleAll(nodePos, 0.4f))
                {
                    if (col.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
                    {
                        isWall = true;
                        break;
                    }
                }
                NodeArray[i, j] = new Node(isWall, i + bottomLeft.x, j + bottomLeft.y);
            }
        }

        // 시작과 목표 위치가 영역 내에 있는지 확인
        if (startPos.x < bottomLeft.x || startPos.y < bottomLeft.y || startPos.x > topRight.x || startPos.y > topRight.y)
        {
            Debug.LogError("Start position is out of bounds.");
            return new Vector2(endPos.x, endPos.y);
        }
        if (targetPos.x < bottomLeft.x || targetPos.y < bottomLeft.y || targetPos.x > topRight.x || targetPos.y > topRight.y)
        {
            Debug.LogError("Target position is out of bounds.");
            return new Vector2(endPos.x, endPos.y);
        }

        // 시작, 목표 노드 설정
        StartNode = NodeArray[startPos.x - bottomLeft.x, startPos.y - bottomLeft.y];
        StartNode.G = 0; // 시작 노드 비용 0
        TargetNode = NodeArray[targetPos.x - bottomLeft.x, targetPos.y - bottomLeft.y];

        OpenList = new List<Node>() { StartNode };
        ClosedList = new List<Node>();

        while (OpenList.Count > 0)
        {
            // 열린리스트 중 F 값이 가장 작은 노드를 선택 (F가 같으면 H가 작은 노드 선택)
            CurNode = OpenList[0];
            for (int i = 1; i < OpenList.Count; i++)
            {
                if (OpenList[i].F < CurNode.F || (OpenList[i].F == CurNode.F && OpenList[i].H < CurNode.H))
                {
                    CurNode = OpenList[i];
                }
            }

            OpenList.Remove(CurNode);
            ClosedList.Add(CurNode);

            // 목표 노드에 도달한 경우
            if (CurNode == TargetNode)
            {
                Node pathNode = TargetNode;
                while (pathNode != StartNode)
                {
                    FinalNodeList.Add(pathNode);
                    pathNode = pathNode.ParentNode;
                    if (pathNode == null) break; // 안전 장치
                }
                FinalNodeList.Add(StartNode);
                FinalNodeList.Reverse();

                // 경로의 노드가 2개 이상이면 다음 이동할 위치(인덱스 1)를 반환, 그렇지 않으면 시작 노드를 반환
                if (FinalNodeList.Count >= 2)
                {
                    return new Vector2(FinalNodeList[1].x, FinalNodeList[1].y);
                }
                else if (FinalNodeList.Count == 1)
                {
                    return new Vector2(FinalNodeList[0].x, FinalNodeList[0].y);
                }
            }

            // 인접 노드 추가
            if (allowDiagonal)
            {
                OpenListAdd(CurNode.x + 1, CurNode.y + 1);
                OpenListAdd(CurNode.x - 1, CurNode.y + 1);
                OpenListAdd(CurNode.x - 1, CurNode.y - 1);
                OpenListAdd(CurNode.x + 1, CurNode.y - 1);
            }
            OpenListAdd(CurNode.x + 1, CurNode.y);
            OpenListAdd(CurNode.x, CurNode.y + 1);
            OpenListAdd(CurNode.x, CurNode.y - 1);
            OpenListAdd(CurNode.x - 1, CurNode.y);
        }

        // 경로를 찾지 못한 경우
        Debug.LogWarning("No path found.");
        return transform.position;
    }

    void OpenListAdd(int checkX, int checkY)
    {
        // 범위 체크
        if (checkX < bottomLeft.x || checkX > topRight.x || checkY < bottomLeft.y || checkY > topRight.y)
            return;

        Node neighbor = NodeArray[checkX - bottomLeft.x, checkY - bottomLeft.y];
        if (neighbor.isWall || ClosedList.Contains(neighbor))
            return;

        // 대각선 이동 시 코너 통과 제한
        if (allowDiagonal)
        {
            if (dontCrossCorner)
            {
                if (checkX != CurNode.x && checkY != CurNode.y)
                {
                    Node nodeHorizontal = NodeArray[checkX - bottomLeft.x, CurNode.y - bottomLeft.y];
                    Node nodeVertical = NodeArray[CurNode.x - bottomLeft.x, checkY - bottomLeft.y];
                    if (nodeHorizontal.isWall || nodeVertical.isWall)
                        return;
                }
            }
            else
            {
                if (checkX != CurNode.x && checkY != CurNode.y)
                {
                    Node nodeHorizontal = NodeArray[checkX - bottomLeft.x, CurNode.y - bottomLeft.y];
                    Node nodeVertical = NodeArray[CurNode.x - bottomLeft.x, checkY - bottomLeft.y];
                    if (nodeHorizontal.isWall && nodeVertical.isWall)
                        return;
                }
            }
        }

        int cost = CurNode.G + ((CurNode.x == checkX || CurNode.y == checkY) ? 10 : 14);
        if (cost < neighbor.G || !OpenList.Contains(neighbor))
        {
            neighbor.G = cost;
            neighbor.H = (Mathf.Abs(neighbor.x - TargetNode.x) + Mathf.Abs(neighbor.y - TargetNode.y)) * 10;
            neighbor.ParentNode = CurNode;
            if (!OpenList.Contains(neighbor))
                OpenList.Add(neighbor);
        }
    }

    void OnDrawGizmos()
    {
        if (FinalNodeList != null && FinalNodeList.Count > 0)
        {
            for (int i = 0; i < FinalNodeList.Count - 1; i++)
            {
                Gizmos.DrawLine(new Vector2(FinalNodeList[i].x, FinalNodeList[i].y), new Vector2(FinalNodeList[i + 1].x, FinalNodeList[i + 1].y));
            }
        }
    }
}
