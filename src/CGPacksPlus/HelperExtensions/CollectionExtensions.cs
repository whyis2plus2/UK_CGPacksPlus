namespace CGPacksPlus.HelperExtensions;

using System;
using System.Collections.Generic;
using System.Linq;

public static class CollectionExtensions
{
    /// <summary>
    /// Perform an action on every item of a(n) <see cref="ICollection{T}" />
    /// </summary>
    /// <typeparam name="T">The underlying type of each item in the collection</typeparam>
    /// <param name="self">The <see cref="ICollection{T}" /> in question</param>
    /// <param name="action">The action that will be performed on the items of the <see cref="ICollection{T}" /></param>
    /// <exception cref="ArgumentNullException" />
    public static void ForEach<T>(this ICollection<T> self, Action<T> action)
    {
        if (self == null || action == null) throw new ArgumentNullException();
        foreach (T x in self) action(x);
    }

    /// <summary>
    /// Adds multiple items to the end of an <see cref="ICollection{T}" />.
    /// </summary>
    /// <typeparam name="T">The underlying type of each item in the collection</typeparam>
    /// <param name="self">The collection to add the items to</param>
    /// <param name="items">The items to add</param>
    /// <exception cref="NotSupportedException" />
    public static void Add<T>(this ICollection<T> self, params T[] items) => items.ForEach(self.Add);

    /// <summary>
    /// Adds the content of multiple collections to an <see cref="ICollection{T}" />.
    /// </summary>
    /// <typeparam name="T">The underlying type of each item in the collection</typeparam>
    /// <param name="self">The collection to add the items to</param>
    /// <param name="collections">The collections that will have their contents appended</param>
    /// <exception cref="NotSupportedException" />
    public static void Add<T>(this ICollection<T> self, params ICollection<T>[] collections) => collections.ForEach(c => self.Add(c.ToArray()));

    /// <summary>
    /// Add items to the end of an <see cref="ICollection{T}" />, while
    /// preventing any additional duplicates from being added to the <see cref="ICollection{T}" />.
    /// </summary>
    /// <typeparam name="T">The underlying type of the items in the collection</typeparam>
    /// <param name="self">The collection to add the items to</param>
    /// <param name="items">The items to add to the collection</param>
    /// <exception cref="NotSupportedException" />
    public static void AddDistinct<T>(this ICollection<T> self, params T[] items) => self.Add(items.Except(self).ToArray());

    /// <summary>
    /// Add the contents of multiple collections to the end of an <see cref="ICollection{T}" />, while
    /// preventing any additional duplicates from being added to the <see cref="ICollection{T}" />.
    /// </summary>
    /// <typeparam name="T">The underlying type of the items in the collection</typeparam>
    /// <param name="self">The collection to add the items to</param>
    /// <param name="collections">The collections that will have their contents appended</param>
    /// <exception cref="NotSupportedException" />
    public static void AddDistinct<T>(this ICollection<T> self, params ICollection<T>[] collections) =>
        self.Add(collections.SelectMany(x => x).Distinct().Except(self).ToArray());
}