﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Tlabs.Misc;

namespace Tlabs.Sync {

  /// <summary>Collection with synchronized access.</summary>
  /// <remarks>Any <see cref="IEnumerable{T}"/> returned from this class operates on a copy of the colletion.
  /// For this it is possible to iterate this collection with foreach while modifying with Add() or Remove()...
  /// </remarks>
  public class SyncCollection<T> : ICollection<T>, IReadOnlyCollection<T>, IEnumerable<T> {
    readonly ICollection<T> coll;

    /// <summary>Default ctor.</summary>
    public SyncCollection() { coll= new LinkedList<T>(); }

    /// <summary>Default ctor.</summary>
    public SyncCollection(IEnumerable<T> enm) { coll= new LinkedList<T>(enm); }

    /// <summary>Synchronization root.</summary>
    public object SyncRoot => coll;

    ///<inheritdoc/>
    public int Count => coll.Count;

    ///<inheritdoc/>
    public bool IsReadOnly => false;

    ///<inheritdoc/>
    public void Add(T item) {
      lock (SyncRoot) coll.Add(item);
    }

    /// <summary>Add <paramref name="range"/> to the end of this collection.</summary>
    public void AddRange(IEnumerable<T> range) {
      lock(coll) coll.AddRange(range);
    }

    ///<inheritdoc/>
    public void Clear() {
      lock (SyncRoot) coll.Clear();
    }

    ///<inheritdoc/>
    public bool Contains(T item) {
      lock (SyncRoot) return coll.Contains(item);
    }

    /// <summary>Returns true if collection contains item matching <paramref name="predicate"/>.</summary>
    public bool Contains(Func<T, bool> predicate) {
      lock (SyncRoot) return coll.Any(predicate);
    }

    ///<inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex) {
      lock (SyncRoot) coll.CopyTo(array, arrayIndex);
    }

    ///<inheritdoc/>
    public bool Remove(T item) {
      lock (SyncRoot) return coll.Remove(item);
    }

    ///<inheritdoc/>
    public IEnumerator<T> GetEnumerator() {
      lock (SyncRoot) return new LinkedList<T>(coll).GetEnumerator();
    }

    ///<inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Returns a <see cref="ICollection{T}"/> of items matching <paramref name="predicate"/>.</summary>
    public ICollection<T> CollectionOf(Func<T, bool> predicate) {
      lock (SyncRoot) return new LinkedList<T>(coll.Where(predicate));
    }

  }
}

