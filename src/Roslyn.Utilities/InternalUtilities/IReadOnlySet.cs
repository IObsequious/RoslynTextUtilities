namespace Roslyn.Utilities
{
    public interface IReadOnlySet<T>
    {
        int Count { get; }

        bool Contains(T item);
    }
}
