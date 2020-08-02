using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EliteBuckyball.Application
{
    public class PriorityQueue<T>
    {
        private SortedDictionary<double, List<T>> dictionary = new SortedDictionary<double, List<T>>();

        public bool Any() => this.Count != 0;

        public int Count { get; private set; } = 0;

        public (T, double) Dequeue()
        {
            this.Count -= 1;

            var pair = this.dictionary.First();
            if (pair.Value.Count == 1)
            {
                this.dictionary.Remove(pair.Key);
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
            this.Count += 1;

            if (this.dictionary.ContainsKey(priority))
            {
                this.dictionary[priority].Add(item);
            }
            else
            {
                this.dictionary[priority] = new List<T> { item };
            }
        }
    }
}
