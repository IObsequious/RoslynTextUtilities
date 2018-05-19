using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static class ConsListExtensions
    {
        public static ConsList<T> Prepend<T>(this ConsList<T> list, T head)
        {
            return new ConsList<T>(head, list ?? ConsList<T>.Empty);
        }

        public static bool ContainsReference<T>(this ConsList<T> list, T element)
        {
            for (; list != ConsList<T>.Empty; list = list.Tail)
            {
                if (ReferenceEquals(list.Head, element))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
