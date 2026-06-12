using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Utils
{
    public class StackLockFree<T>
    {
        private class Node<V>
        {
            public Node<V> Next;
            public V Item;
        }

        private readonly Node<T> Head = new Node<T>();

        public void Push(T Item)
        {
            Node<T> node = new Node<T>();
            node.Item = Item;

            do
            {
                node.Next = Head.Next;
            } while (Interlocked.CompareExchange<Node<T>>(ref Head.Next, node, node.Next) != node.Next);
        }

        public T Pop()
        {
            Node<T> node;

            do
            {
                node = Head.Next;
                if (node == null) return default(T);
            } while (Interlocked.CompareExchange<Node<T>>(ref Head.Next, node.Next, node) != node);

            return node.Item;
        }

        public void Clear()
        {
            Interlocked.Exchange(ref Head.Next, null);
        }
    }
}
