// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CSharp.Expressions;

namespace System.Dynamic.Utils
{
    internal abstract class ListProvider<T> : IList<T>
        where T : class
    {
        protected abstract T First { get; }
        protected abstract int ElementCount { get; }
        protected abstract T GetElement(int index);

        #region IList<T> Members

        public int IndexOf(T item)
        {
            if (First == item)
            {
                return 0;
            }

            for (int i = 1, n = ElementCount; i < n; i++)
            {
                if (GetElement(i) == item)
                {
                    return i;
                }
            }

            return -1;
        }
        
        public void Insert(int index, T item)
        {
            throw ContractUtils.Unreachable;
        }
        
        public void RemoveAt(int index)
        {
            throw ContractUtils.Unreachable;
        }

        public T this[int index]
        {
            get
            {
                if (index == 0)
                {
                    return First;
                }

                return GetElement(index);
            }
            
            set
            {
                throw ContractUtils.Unreachable;
            }
        }

        #endregion

        #region ICollection<T> Members
        
        public void Add(T item)
        {
            throw ContractUtils.Unreachable;
        }
        
        public void Clear()
        {
            throw ContractUtils.Unreachable;
        }

        public bool Contains(T item) => IndexOf(item) != -1;

        public void CopyTo(T[] array, int index)
        {
            ContractUtils.RequiresNotNull(array, nameof(array));
            if (index < 0)
            {
                throw LinqError.ArgumentOutOfRange(nameof(index));
            }

            int n = ElementCount;
            Debug.Assert(n > 0);
            if (index + n > array.Length)
            {
                throw new ArgumentException();
            }

            array[index++] = First;
            for (int i = 1; i < n; i++)
            {
                array[index++] = GetElement(i);
            }
        }

        public int Count => ElementCount;
        
        public bool IsReadOnly => true;
        
        public bool Remove(T item)
        {
            throw ContractUtils.Unreachable;
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            yield return First;

            for (int i = 1, n = ElementCount; i < n; i++)
            {
                yield return GetElement(i);
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}
