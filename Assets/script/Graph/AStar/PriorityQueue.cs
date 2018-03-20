using System.Collections;
using System.Collections.Generic;

namespace DiyAStar
{
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

        public void Add(T obj, float pripriorityValue)
        {
            QueueElement<T> element = new QueueElement<T>(obj, pripriorityValue);
            addElement(element);
        }

        void addElement(QueueElement<T> element)
        {
            heap.Add(element);//放到尾巴

            if (!isEmpty())
                moveUp(heap.Count - 1);//向上浮
        }

        public List<QueueElement<T>>.Enumerator getEnumerator()
        {
            return heap.GetEnumerator();
        }

        public bool isEmpty() { return heap.Count == 0; }

        public QueueElement<T> popup()
        {
            if (!isEmpty())
            {
                QueueElement<T> head = heap[0];

                //把尾巴的元素放到最上面
                if (heap.Count > 1)
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

        int getParentIndex(int index) { return (index - 1) / 2; }
        int getLeftChildLindex(int index) { return 2 * index + 1; }
        int getRightChildLindex(int index) { return 2 * index + 2; }

        void moveUp(int nowIndex)
        {
            int parentIndex = getParentIndex(nowIndex);
            QueueElement<T> parent = heap[parentIndex];
            QueueElement<T> now = heap[nowIndex];

            if (now.pripriorityValue < parent.pripriorityValue)
            {
                swap(nowIndex, parentIndex);

                if (parentIndex != 0)//head沒得交換了
                    moveUp(parentIndex);
            }
        }

        void moveDown(int nowIndex)
        {
            int leftChildIndex = getLeftChildLindex(nowIndex);
            int rightChildIndex = getRightChildLindex(nowIndex);
            QueueElement<T> leftChild = (leftChildIndex < heap.Count) ? heap[leftChildIndex] : null;
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
            else if (leftChild != null && now.pripriorityValue > leftChild.pripriorityValue)
            {
                swap(nowIndex, leftChildIndex);
                moveDown(leftChildIndex);
            }
        }

        void swap(int x, int y)
        {
            QueueElement<T> temp = heap[x];
            heap[x] = heap[y];
            heap[y] = temp;
        }

        public void showPripriorityValue()
        {
            string message = "inside queue";
            foreach (QueueElement<T> ele in heap)
            {
                //message += "["+ele.pripriorityValue+":"+ele.obj.ToString()+"]";
                message += "[" + ele.pripriorityValue + "]";
            }
            //UnityEngine.Debug.Log(message);
        }
    }
}

