using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterfaceToClass
{
    class MyList<T> : IList<T>
    {
        List<T> list = new List<T>();

        public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int Count { get; set; }
        public bool IsReadOnly { get; set; }

        public void Add(T item)
        {
            list.Add(item);
        }

        public void Clear()
        {
            list.Clear();
        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            list.Insert(index,item);
        }

        public bool Remove(T item)
        {
            return list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }
}
