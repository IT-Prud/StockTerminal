using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Utils
{
    public class QueueLockFree<T>
    {
        private class Node<V>
        {
            public Node<V> Next;
            public V Item;
        }

        Node<T> Head;
        Node<T> Tail;

        public int Count = 0;

        public QueueLockFree()
        {
            Head = new Node<T>();
            Tail = Head;
        }

        public void Enqueue(T item)
        {
            Node<T> oldTail = null;
            Node<T> oldNext = null;

            // create and initialize the new node
            Node<T> node = new Node<T>();
            node.Item = item;

            // loop until we have managed to update the tail's Next link 
            // to point to our new node
            bool UpdatedNewLink = false;
            while (!UpdatedNewLink)
            {
                // make local copies of the tail and its Next link, but in 
                // getting the latter use the local copy of the tail since
                // another thread may have changed the value of tail
                oldTail = Tail;
                oldNext = oldTail.Next;

                // providing that the tail field has not changed...
                if (Tail == oldTail)
                {
                    // ...and its Next field is null
                    if (oldNext == null)
                    {
                        // ...try to update the tail's Next field
                        UpdatedNewLink = Interlocked.CompareExchange<Node<T>>(
                            ref Tail.Next, node, null) == null;
                    }

                    // if the tail's Next field was non-null, another thread
                    // is in the middle of enqueuing a new node, so try and 
                    // advance the tail to point to its Next node
                    else
                    {
                        Interlocked.CompareExchange<Node<T>>(ref Tail, oldNext, oldTail);
                    }
                }
            }

            // try and update the tail field to point to our node; don't
            // worry if we can't, another thread will update it for us on
            // the next call to Enqueue()
            Interlocked.CompareExchange<Node<T>>(ref Tail, node, oldTail);

            Interlocked.Increment(ref Count);
        }

        public T Dequeue()
        {
            T result = default(T);

            // loop until we manage to advance the head, removing 
            // a node (if there are no nodes to dequeue, we'll exit
            // the method instead)
            bool HaveAdvancedHead = false;
            while (!HaveAdvancedHead)
            {
                // make local copies of the head, the tail, and the head's Next 
                // reference
                Node<T> oldHead = Head;
                Node<T> oldTail = Tail;
                Node<T> oldHeadNext = oldHead.Next;

                // providing that the head field has not changed...
                if (oldHead == Head)
                {
                    // ...and it is equal to the tail field
                    if (oldHead == oldTail)
                    {

                        // ...and the head's Next field is null
                        if (oldHeadNext == null)
                        {

                            // ...then there is nothing to dequeue
                            return default(T);
                        }

                        // if the head's Next field is non-null and head was equal to the tail
                        // then we have a lagging tail: try and update it
                        Interlocked.CompareExchange<Node<T>>(ref Tail, oldHeadNext, oldTail);
                    }

                    // otherwise the head and tail fields are different
                    else
                    {
                        // grab the item to dequeue, and then try to advance the head reference
                        result = oldHeadNext.Item;
                        HaveAdvancedHead = Interlocked.CompareExchange<Node<T>>(
                            ref Head, oldHeadNext, oldHead) == oldHead;
                    }
                }
            }

            Interlocked.Decrement(ref Count);

            return result;
        }


        public void Clear()
        {
            // loop until we manage to advance the head, removing 
            // all node up to current tail (if there are no nodes to clear, we'll exit
            // the method instead)
            while (true)
            {
                // make local copies of the head, the tail, and the head's Next 
                // reference
                Node<T> oldHead = Head;
                Node<T> oldTail = Tail;
                Node<T> oldHeadNext = oldHead.Next;

                // providing that the head field has not changed...
                if (oldHead == Head)
                {
                    // ...and it is equal to the tail field
                    if (oldHead == oldTail)
                    {

                        // ...and the head's Next field is null
                        if (oldHeadNext == null)
                        {

                            // ...then there is nothing to clear
                            return;
                        }

                        // if the head's Next field is non-null and head was equal to the tail
                        // then we have a lagging tail: try and update it
                        Interlocked.CompareExchange<Node<T>>(ref Tail, oldHeadNext, oldTail);
                    }

                    // otherwise the head and tail fields are different
                    else
                    {
                        // try to advance the head reference
                        Interlocked.CompareExchange<Node<T>>(ref Head, Tail, oldHead);
                    }
                }
            }
        }
    }
}
