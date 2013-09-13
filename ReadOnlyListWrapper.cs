using System;
using System.Collections;
using System.Collections.Generic;

internal class ReadOnlyListWrapper<T> : ICollection<T>
{
    private readonly List<T> _wrappedList;

    public ReadOnlyListWrapper(List<T> listToWrap)
    {
        _wrappedList = listToWrap;
    }

    #region Implementation of IEnumerable

    public IEnumerator<T> GetEnumerator()
    {
        return _wrappedList.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion

    #region Implementation of ICollection<T>

    public void Add(T item)
    {
        throw new NotSupportedException("The list is read-only.");
    }

    public void Clear()
    {
        throw new NotSupportedException("The list is read-only.");
    }

    public bool Contains(T item)
    {
        return _wrappedList.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _wrappedList.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        throw new NotSupportedException("The list is read-only.");
    }

    public int Count
    {
        get { return _wrappedList.Count; }
    }

    public bool IsReadOnly
    {
        get { return true; }
    }

    #endregion
}