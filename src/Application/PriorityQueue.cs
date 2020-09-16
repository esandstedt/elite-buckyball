using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EliteBuckyball.Application
{
    public class PriorityQueue<T>
    {
        private SortedDictionary<double, List<T>> queue = new SortedDictionary<double, List<T>>();
        private Dictionary<T, double> dictionary = new Dictionary<T, double>();

        public bool Any() => this.Count != 0;

        public int Count  => this.dictionary.Count;

        public (T, double) Dequeue()
        {
            while (this.queue.Any())
            {
                var (item, priority) = this.DequeueFromQueue();
                if (this.dictionary.ContainsKey(item) && 
                    this.dictionary[item].Equals(priority))
                {
                    this.dictionary.Remove(item);
                    return (item, priority);
                }
            }

            throw new InvalidOperationException("Queue is empty.");
        }

        private (T, double) DequeueFromQueue()
        {
            var pair = this.queue.First();
            if (pair.Value.Count == 1)
            {
                this.queue.Remove(pair.Key);
                return (pair.Value[0], pair.Key);
            }
            else
            {
                var index = pair.Value.Count - 1;
                var value = pair.Value[index];
                pair.Value.RemoveAt(index);
                return (value, pair.Key);
            }
        }

        public void Enqueue(T item, double priority)
        {

            if (this.dictionary.ContainsKey(item))
            {
                this.dictionary[item] = Math.Min(this.dictionary[item], priority);
            }
            else
            {
                this.dictionary[item] = priority;
            }

            if (this.queue.ContainsKey(priority))
            {
                this.queue[priority].Add(item);
            }
            else
            {
                this.queue[priority] = new List<T> { item };
            }
        }
    }
}
