using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GraphNode : MonoBehaviour, IGraphNode
{
    public int EdgeCount() { return edgeTarget.Count; }
    public IGraphNode GetEdge(int index) { return edgeTarget[index]; }
    public float GetEdgeCost(int index) { return edgeCost[index]; }

    public string getNodeKey() { return nodeKey; }

    //記錄圖的資訊
    public List<GraphNode> edgeTarget;
    public List<float> edgeCost;
    public string nodeKey;
    public string description;

    //路徑資訊
    private bool visited;
    private float accumulationCost;
    private IGraphNode comeFrom;

    public void setComeFrom(IGraphNode node) { comeFrom = node; }
    public IGraphNode getComeFrom() { return comeFrom; }

    public bool isVisited() { return visited; }
    public void setVisited() { visited = true; }
    public float getAccumulationCost() { return accumulationCost; }
    public void setAccumulationCost(float pCost) { accumulationCost = pCost; }

    public float getEvaluation(IGraphNode target)
    {
        Vector3 temp = target.getPosition() - transform.position;
        return Mathf.Abs(temp.x) + Mathf.Abs(temp.y);
    }

    public void resetPathInfo()
    {
        visited = false;
        accumulationCost = 0;
        comeFrom = null;
    }

    public Vector3 getPosition()
    {
        return transform.position;
    }

    public bool hasArrow(byte value, Arrow direct)
    {
        return (value & (byte)direct) > 0;
    }

    public void showNode(bool value)
    {
        GetComponent<Renderer>().enabled = value;
    } 
}
