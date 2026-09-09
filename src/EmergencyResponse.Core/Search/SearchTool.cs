namespace EmergencyResponse.Core.Search;

/// <summary>
/// Reusable filtering over any sequence.
/// </summary>
/// <remarks>
/// Nothing here names a domain type, so the same method searches responders,
/// incidents, or anything else.
/// </remarks>
public static class SearchTool
{
    /// <summary>
    /// Returns every item that satisfies the condition, in source order.
    /// </summary>
    /// <typeparam name="T">The kind of item being searched.</typeparam>
    /// <param name="items">The sequence to search.</param>
    /// <param name="condition">The test each item must pass.</param>
    /// <returns>
    /// The matching items, in source order, evaluated lazily as the result is
    /// enumerated. Never <see langword="null"/>: when nothing matches the
    /// result is an empty sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items"/> or <paramref name="condition"/> is null. This is
    /// thrown by the call itself, not deferred to the first enumeration.
    /// </exception>
    public static IEnumerable<T> FindMatches<T>(IEnumerable<T> items, Func<T, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(condition);

        // The validation above has to sit in a normal method. An iterator's
        // body does not run until the first MoveNext, so a caller who never
        // enumerated the result would never see the exception.
        return Iterate(items, condition);
    }

    /// <summary>
    /// Returns the first item that satisfies the condition, or
    /// <see langword="null"/> if none does.
    /// </summary>
    /// <typeparam name="T">The kind of item being searched.</typeparam>
    /// <param name="items">The sequence to search.</param>
    /// <param name="condition">The test each item must pass.</param>
    /// <returns>
    /// The first matching item, or <see langword="null"/> if none does.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="items"/> or <paramref name="condition"/> is null.
    /// </exception>
    public static T? FindFirstMatch<T>(IEnumerable<T> items, Func<T, bool> condition)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(condition);

        foreach (T item in items)
        {
            if (condition(item))
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>Walks the sequence, yielding the items that pass the test.</summary>
    /// <typeparam name="T">The kind of item being searched.</typeparam>
    /// <param name="items">The sequence to search. Already checked for null.</param>
    /// <param name="condition">The test each item must pass. Already checked for null.</param>
    /// <returns>The matching items.</returns>
    private static IEnumerable<T> Iterate<T>(IEnumerable<T> items, Func<T, bool> condition)
    {
        foreach (T item in items)
        {
            if (condition(item))
            {
                yield return item;
            }
        }
    }
}
