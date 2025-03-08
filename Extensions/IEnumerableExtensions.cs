using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RoflanArchives.Core.Extensions;

internal static class IEnumerableExtensions
{
    public static ObservableCollection<TSource> ToObservableCollection<TSource>(
        this IEnumerable<TSource> source)
    {
        return new ObservableCollection<TSource>(source);
    }
}
