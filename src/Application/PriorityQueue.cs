using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EliteBuckyball.Application
{
    public class PriorityQueue<T>
    {
        private SortedList<double, T> list = new SortedList<double, T>(new DuplicateKeyComparer<double>());

        public bool Any() => this.list.Any();

        public int Count => this.list.Count;

        public (T, double) Dequeue()
        {
            var pair = this.list.First();
            this.list.RemoveAt(0);
            return (pair.Value, pair.Key);
        }

        public void Enqueue(T item, double priority)
        {
            this.list.Add(priority, item);
        }

        private class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
        {
            public int Compare(TKey x, TKey y)
            {
                int result = x.CompareTo(y);

                if (result == 0)
                    return 1;   // Handle equality as beeing greater
                else
                    return result;
            }
        }
    }
}
