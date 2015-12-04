using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

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

    public List<GraphNode> findPath(Vector3 vFrom ,Vector3 vDestination)
    {
        Vector2 f = this.transform.InverseTransformPoint(vFrom);
        Vector2 d = this.transform.InverseTransformPoint(vDestination);

        GraphNode tempFrom = graphNodeHash[remap((int)f.x,(int)f.y)];
        GraphNode tempDestination = graphNodeHash[remap((int)d.x, (int)d.y)];

        if (tempFrom == null || tempDestination == null)
            return null;

        return findPath(tempFrom, tempDestination);
    }

    public List<GraphNode> findPath(GraphNode from, GraphNode destination)
    {
        if (pathFinder == null)
            pathFinder = new AStarPathFinder();

        foreach (GraphNode node in aliveNode)
            node.resetPathInfo();
        return pathFinder.findPath(from, destination);
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

public class AStarPathFinder
{
    private PriorityQueue<GraphNode> q;

    public AStarPathFinder()
    {
        q = new PriorityQueue<GraphNode>();
    }

    public List<GraphNode> findPath(GraphNode from,GraphNode destination)
    {
        //清空queue
        q.clearAll();

        //把from(設為已訪問)並放入queue
        from.setAccumulationCost(0);

        from.setVisited();
        q.Add(from, 0);

        bool findIt = false;
        //在queue裡取出cost最小的node
        //把node的edgeTarget(設為已訪問)並放queue
        while (!q.isEmpty())
        {
            //印出來
            //q.showPripriorityValue();

            QueueElement<GraphNode> element = q.popup();
            GraphNode node = element.obj;

            if (node == destination)
            {
                findIt = true;
                break;
            }

            int count = node.edgeTarget.Count;
            for (int i=0;i< count;i++)
            {
                GraphNode neighbor = node.edgeTarget[i];
                float edgeCost = node.edgeCost[i];

                if (!neighbor.isVisited())//沒有訪問過的才加
                {
                    float evaluation = neighbor.getEvaluation(destination);
                    float cost = node.getAccumulationCost() + edgeCost;
                    neighbor.setAccumulationCost(cost);

                    neighbor.setVisited();
                    neighbor.setComeFrom(node);
                    q.Add(neighbor, cost + evaluation);
                }
            }
        }

        List<GraphNode> list = new List<GraphNode>();
        if (findIt)
        {
            list.Add(destination);
            GraphNode ptr = destination.getComeFrom();
            while (ptr != null)
            {
                list.Add(ptr);
                ptr = ptr.getComeFrom();
            }

            list.Reverse();
        }
        return list;
    }
}

public class QueueElement<T>
{
    public T obj;
    public float pripriorityValue;

    public QueueElement(T pObj, float pPripriorityValue)
    {
        obj = pObj;
        pripriorityValue = pPripriorityValue;
    }
}

public class PriorityQueue<T>
{
    List<QueueElement<T>> heap;

    public PriorityQueue()
    {
        heap = new List<QueueElement<T>>();
    }

    public void clearAll()
    {
        heap.Clear();
    }

    public void Add(T obj,float pripriorityValue)
    {
        QueueElement<T> element = new QueueElement<T>(obj, pripriorityValue);
        addElement(element);
    }

    void addElement(QueueElement<T> element)
    {
        heap.Add(element);//放到尾巴

        if(!isEmpty())
            moveUp(heap.Count-1);//向上浮
    }

    public List<QueueElement<T>>.Enumerator getEnumerator()
    {
        return heap.GetEnumerator();
    }

    public bool isEmpty(){return heap.Count==0;}

    public QueueElement<T> popup()
    {
        if (!isEmpty())
        {
            QueueElement<T> head = heap[0];

            //把尾巴的元素放到最上面
            if(heap.Count>1)
            {
                heap[0] = heap[heap.Count - 1];
                heap.RemoveAt(heap.Count - 1);

                //向下沈
                if (heap.Count > 1)
                    moveDown(0);
            }
            else
                heap.RemoveAt(0);

            return head;
         }

        return null;
    }

    int getParentIndex(int index){return (index - 1) / 2;}
    int getLeftChildLindex(int index){return 2 * index + 1;}
    int getRightChildLindex(int index) { return 2 * index + 2; }

    void moveUp(int nowIndex)
    {
        int parentIndex = getParentIndex(nowIndex);
        QueueElement<T> parent = heap[parentIndex];
        QueueElement<T> now = heap[nowIndex];

        if (now.pripriorityValue < parent.pripriorityValue)
        {
            swap(nowIndex, parentIndex);

            if(parentIndex!=0)//head沒得交換了
                moveUp(parentIndex);
        }      
    }

    void moveDown(int nowIndex)
    {
        int leftChildIndex = getLeftChildLindex(nowIndex);
        int rightChildIndex = getRightChildLindex(nowIndex);
        QueueElement<T> leftChild =( leftChildIndex<heap.Count)? heap[leftChildIndex]:null;
        QueueElement<T> rightChild = (rightChildIndex < heap.Count) ? heap[rightChildIndex] : null;
        QueueElement<T> now = heap[nowIndex];

        //找最小的往下沈

        if (leftChild != null && rightChild != null)
        {
            //左邊較小
            if (leftChild.pripriorityValue < rightChild.pripriorityValue)
            {
                //和左邊比
                if (now.pripriorityValue > leftChild.pripriorityValue)
                {
                    swap(nowIndex, leftChildIndex);
                    moveDown(leftChildIndex);
                }
            }
            else //右邊較小
            {
                //和右邊比
                if (now.pripriorityValue > rightChild.pripriorityValue)
                {
                    swap(nowIndex, rightChildIndex);
                    moveDown(rightChildIndex);
                }
            }
        }
        else if (leftChild!=null && now.pripriorityValue > leftChild.pripriorityValue)
        {
            swap(nowIndex, leftChildIndex);
            moveDown(leftChildIndex);
        }
    }

    void swap(int x, int y)
    {
        QueueElement<T> temp = heap[x];
        heap[x]= heap[y];
        heap[y]=temp;
    }

    public void showPripriorityValue()
    {
        string message = "inside queue";
        foreach (QueueElement<T> ele in heap)
        {
            //message += "["+ele.pripriorityValue+":"+ele.obj.ToString()+"]";
            message += "[" + ele.pripriorityValue  + "]";
        }
        Debug.Log(message);
    }
}