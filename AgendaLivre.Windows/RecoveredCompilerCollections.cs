using System.Collections;

internal sealed class _003C_003Ez__ReadOnlyArray<T> : IList, IReadOnlyList<T>, IList<T>
{
    private readonly T[] _items;

    public _003C_003Ez__ReadOnlyArray(T[] items) => _items = items;

    int ICollection.Count => _items.Length;
    bool ICollection.IsSynchronized => false;
    object ICollection.SyncRoot => this;
    bool IList.IsFixedSize => true;
    bool IList.IsReadOnly => true;
    int IReadOnlyCollection<T>.Count => _items.Length;
    T IReadOnlyList<T>.this[int index] => _items[index];
    int ICollection<T>.Count => _items.Length;
    bool ICollection<T>.IsReadOnly => true;
    object? IList.this[int index] { get => _items[index]; set => throw new NotSupportedException(); }
    T IList<T>.this[int index] { get => _items[index]; set => throw new NotSupportedException(); }
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
    void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
    int IList.Add(object? value) => throw new NotSupportedException();
    void IList.Clear() => throw new NotSupportedException();
    bool IList.Contains(object? value) => ((IList)_items).Contains(value);
    int IList.IndexOf(object? value) => ((IList)_items).IndexOf(value);
    void IList.Insert(int index, object? value) => throw new NotSupportedException();
    void IList.Remove(object? value) => throw new NotSupportedException();
    void IList.RemoveAt(int index) => throw new NotSupportedException();
    void ICollection<T>.Add(T item) => throw new NotSupportedException();
    void ICollection<T>.Clear() => throw new NotSupportedException();
    bool ICollection<T>.Contains(T item) => ((ICollection<T>)_items).Contains(item);
    void ICollection<T>.CopyTo(T[] array, int index) => _items.CopyTo(array, index);
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
    int IList<T>.IndexOf(T item) => Array.IndexOf(_items, item);
    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
    void IList<T>.RemoveAt(int index) => throw new NotSupportedException();
}

internal sealed class _003C_003Ez__ReadOnlySingleElementList<T> : IList, IReadOnlyList<T>, IList<T>
{
    private readonly T _item;

    public _003C_003Ez__ReadOnlySingleElementList(T item) => _item = item;

    int ICollection.Count => 1;
    bool ICollection.IsSynchronized => false;
    object ICollection.SyncRoot => this;
    bool IList.IsFixedSize => true;
    bool IList.IsReadOnly => true;
    int IReadOnlyCollection<T>.Count => 1;
    int ICollection<T>.Count => 1;
    bool ICollection<T>.IsReadOnly => true;
    object? IList.this[int index] { get => ItemAt(index); set => throw new NotSupportedException(); }
    T IReadOnlyList<T>.this[int index] => ItemAt(index);
    T IList<T>.this[int index] { get => ItemAt(index); set => throw new NotSupportedException(); }
    private T ItemAt(int index) => index == 0 ? _item : throw new IndexOutOfRangeException();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    private IEnumerator<T> GetEnumerator() { yield return _item; }
    void ICollection.CopyTo(Array array, int index) => array.SetValue(_item, index);
    int IList.Add(object? value) => throw new NotSupportedException();
    void IList.Clear() => throw new NotSupportedException();
    bool IList.Contains(object? value) => value is T typed && EqualityComparer<T>.Default.Equals(_item, typed);
    int IList.IndexOf(object? value) => ((IList)this).Contains(value) ? 0 : -1;
    void IList.Insert(int index, object? value) => throw new NotSupportedException();
    void IList.Remove(object? value) => throw new NotSupportedException();
    void IList.RemoveAt(int index) => throw new NotSupportedException();
    void ICollection<T>.Add(T item) => throw new NotSupportedException();
    void ICollection<T>.Clear() => throw new NotSupportedException();
    bool ICollection<T>.Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);
    void ICollection<T>.CopyTo(T[] array, int index) => array[index] = _item;
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
    int IList<T>.IndexOf(T item) => EqualityComparer<T>.Default.Equals(_item, item) ? 0 : -1;
    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
    void IList<T>.RemoveAt(int index) => throw new NotSupportedException();
}
