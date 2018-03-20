using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGraphNode
{
    Vector3 getPosition();
    void setVisited();
    bool isVisited();
    int EdgeCount();
    IGraphNode GetEdge(int index);
    float GetEdgeCost(int index);
    float getEvaluation(IGraphNode target);
    float getAccumulationCost();
    void setAccumulationCost(float pCost);
    void setComeFrom(IGraphNode node);
    IGraphNode getComeFrom();
    string getNodeKey();
}
