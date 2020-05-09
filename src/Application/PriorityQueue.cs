using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EliteBuckyball.Application
{
    public class PriorityQueue<T>
    {
        private SortedList<double, T> list = new SortedList<double, T>();

        public bool Any() => this.list.Any();

        public int Count => this.list.Count;

        public (T, double) Dequeue()
        {
            var key = this.list.Keys[0];
            var item = this.list[key];
            this.list.RemoveAt(0);
            return (item, key);
        }

        public void Enqueue(T item, double priority)
        {
            this.list.Add(priority, item);
        }
    }
}
