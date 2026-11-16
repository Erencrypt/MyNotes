using System.Collections.ObjectModel;

namespace MyNotes.Helpers
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// Makes target contain exactly the items of source, in the same order, using as few
        /// Insert/Move/Remove calls as possible so the ListView doesn't redraw unchanged rows.
        /// </summary>
        public static void SyncWith<T, TKey>(this ObservableCollection<T> target, IReadOnlyList<T> source, Func<T, TKey> key)
            where TKey : notnull
        {
            // Remove items that are no longer in the results first, in place, so the list animates them sliding out instead of moving matches around
            // (O(1) lookups by key rather than O(n) list scans)
            var sourceKeys = source.Select(key).ToHashSet();
            for (int i = target.Count - 1; i >= 0; i--)
            {
                if (!sourceKeys.Contains(key(target[i])))
                {
                    target.RemoveAt(i);
                }
            }

            var comparer = EqualityComparer<TKey>.Default;
            for (int i = 0; i < source.Count; i++)
            {
                var id = key(source[i]);
                int j = i;
                while (j < target.Count && !comparer.Equals(key(target[j]), id))
                {
                    j++;
                }

                if (j == target.Count)
                {
                    target.Insert(i, source[i]); // new result
                }
                else if (j != i)
                {
                    target.Move(j, i); // wrong position
                }
            }
        }
    }
}
