using System.Collections;
using System.Collections.Generic;

namespace DiyAStar
{
    public class AStarPathFinder
    {
        private PriorityQueue<IGraphNode> q;

        public AStarPathFinder()
        {
            q = new PriorityQueue<IGraphNode>();
        }

        public List<IGraphNode> findPath(IGraphNode from, IGraphNode destination)
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

                QueueElement<IGraphNode> element = q.popup();
                IGraphNode node = element.obj;

                if (node == destination)
                {
                    findIt = true;
                    break;
                }

                int count = node.EdgeCount();
                for (int i = 0; i < count; i++)
                {
                    IGraphNode neighbor = node.GetEdge(i);
                    float edgeCost = node.GetEdgeCost(i);

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

            List<IGraphNode> list = new List<IGraphNode>();
            if (findIt)
            {
                list.Add(destination);
                IGraphNode ptr = destination.getComeFrom();
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
}


