using System;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;

public class Node<TKey, TValue>
{
    public bool Permanent { get; set; }
    public TKey Key { get; set; }
    public TValue Value { get; set; }
    public Node<TKey, TValue> Previous { get; set; }
    public Node<TKey, TValue> Next { get; set; }
}

public class LRUCache<TKey, TValue> where TKey : IComparable
{
    public Action<TValue> OnRemove;
    private readonly int capacity;
    private readonly Dictionary<TKey, Node<TKey, TValue>> cache;
    private Node<TKey, TValue> head;
    private Node<TKey, TValue> tail;
    private int count;

    public LRUCache(int capacity)
    {
        this.capacity = capacity;
        this.cache = new Dictionary<TKey, Node<TKey, TValue>>(capacity);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (!cache.TryGetValue(key, out var node))
        {
            value = default;
            return false;
        }

        value = node.Value;
        MoveToHead(node);
        return true;
    }

    public void Add(TKey key, TValue value,bool permanent = false)
    {
        if (cache.TryGetValue(key, out var node))
        {
            node.Value = value;
            MoveToHead(node);
        }
        else
        {
            var newNode = new Node<TKey, TValue>
            {
                Key = key,
                Value = value,
                Permanent = permanent
            };
            if (count == capacity)
            {
                RemoveTail();
            }

            AddToHead(newNode);
            cache.Add(key, newNode);
            count++;
        }
    }

    private void AddToHead(Node<TKey, TValue> node)
    {
        node.Previous = null;
        node.Next = head;

        if (head == null)
        {
            tail = node;
        }
        else
        {
            head.Previous = node;
        }

        head = node;
    }

    private void MoveToHead(Node<TKey, TValue> node)
    {
        if (node == head)
        {
            return;
        }

        if (node == tail)
        {
            tail = node.Previous;
            tail.Next = null;
        }
        else
        {
            node.Previous.Next = node.Next;
            node.Next.Previous = node.Previous;
        }

        node.Previous = null;
        node.Next = head;
        head.Previous = node;
        head = node;
    }

    private void RemoveTail()
    {
        if (tail == null)
        {
            return;
        }

        do
        {
            if (tail.Permanent)
            {
                MoveToHead(tail);
            }
        } while(tail.Permanent);

        cache.Remove(tail.Key);
        count--;
        
        OnRemove?.Invoke(tail.Value);
        
        if (head == tail)
        {
            head = null;
            tail = null;
        }
        else
        {
            tail = tail.Previous;
            tail.Next = null;
        }
        
    }

    public void Remove(TKey key)
    {
        if (!cache.ContainsKey(key))return;
            
        var curNode = head;
        while (curNode != null)
        {
            if (curNode.Key.CompareTo(key) == 0)
            {
                var previous = curNode.Previous;
                var next = curNode.Next;
                
                if (previous != null)
                {
                    previous.Next = next;    
                }

                if (next != null)
                {
                    next.Previous = previous;    
                }

                curNode.Previous = null;
                curNode.Next = null;
                
                cache.Remove(key);
                count--;
                
                OnRemove?.Invoke(curNode.Value);
                break;
            }

            curNode = curNode.Next;
        }
    }
}