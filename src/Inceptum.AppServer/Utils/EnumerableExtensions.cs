using System;
using System.Collections.Generic;

namespace Inceptum.AppServer.Utils
{
    static class EnumerableExtensions
    {
        public static IEnumerable<TI> Flatten<TI>(this IEnumerable<TI> items, Func<TI, IEnumerable<TI>> selector)
        {
            foreach (var item in items)
            {
                yield return item;
                foreach (var subItem in Flatten<TI>(selector(item), selector))
                {
                    yield return subItem;
                }
            }
        }
    }
}