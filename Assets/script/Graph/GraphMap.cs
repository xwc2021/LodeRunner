using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GraphMap : MonoBehaviour {

    public TileData tileData;
    public GraphNode refObj;

    [SerializeField]
    [HideInInspector]
    private GraphNode[] graphNodeHash;

    [SerializeField]
    [HideInInspector]
    private List<GraphNode> aliveNode;

    [SerializeField]
    [HideInInspector]
    private int width;

    [SerializeField]
    [HideInInspector]
    private int height;

    private AStarPathFinder pathFinder;

    public List<IGraphNode> findPath(Vector3 vFrom ,Vector3 vDestination)
    {
        Vector2 f = this.transform.InverseTransformPoint(vFrom);
        Vector2 d = this.transform.InverseTransformPoint(vDestination);

        GraphNode tempFrom = graphNodeHash[remap((int)f.x,(int)f.y)];
        GraphNode tempDestination = graphNodeHash[remap((int)d.x, (int)d.y)];

        if (tempFrom == null || tempDestination == null)
            return null;

        return findPath(tempFrom, tempDestination);
    }

    public List<IGraphNode> findPath(GraphNode from, GraphNode destination)
    {
        if (pathFinder == null)
            pathFinder = new AStarPathFinder();

        foreach (GraphNode node in aliveNode)
            node.resetPathInfo();
        return pathFinder.findPath(from , destination );
    }

    public GraphNode getGraphNode(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        int x = (int)localPos.x;
        int y = (int)localPos.y;
        int index =remap(x, y);
        return graphNodeHash[index];
    }

    //加入node的連結
    public void addConnection(int from, int to,float cost)
    {
        GraphNode node = graphNodeHash[from];
        GraphNode target = graphNodeHash[to];

        node.edgeCost.Add(cost);
        node.edgeTarget.Add(target);
    }

    public void addGraphNode(int x, int y,string description="")
    {
        float offsetX = TileCreator.offsetX;
        float offsetY = TileCreator.offsetY;

        GraphNode node = (GraphNode)Instantiate(refObj, Vector3.zero, Quaternion.identity);
        string key = "(" + x + "," + y + ")";
        node.nodeKey = key;
        node.name = "graph"+key;
        node.description = description;
        

        node.transform.parent = this.transform;
        node.transform.localPosition = new Vector3(x+ offsetX, y+ offsetY, 0);
        node.transform.localScale = new Vector3(1, 1, 1);

        aliveNode.Add(node);
        graphNodeHash[remap(x, y)] = node;
    }

    public Vector2 getTileIndex(Vector3 pos)
    {
        Vector3 local = transform.InverseTransformPoint(pos);
        return new Vector2((int)local.x, (int)local.y);
    }

    public int remap(int x, int y) { return x * height + y; }

    public bool resetMap(int w,int h)
    {
        width = w;
        height = h;
        graphNodeHash = new GraphNode[width * height];

        if (aliveNode == null)
            aliveNode = new List<GraphNode>();

        foreach (GraphNode node in aliveNode)
        {
            DestroyImmediate(node.gameObject);
        }

        aliveNode.Clear();

        if (refObj == null)
        {
            Debug.Log("請先設定GraphMap的refObj");
            return false;
        }

        Debug.Log("Graph.resetSize() finish");

        return true;
    }

    public void showGraphNode(bool isShow)
    {
        foreach (GraphNode node in aliveNode)
        {
            node.showNode(isShow);
        }
    }
}