namespace Roslyn.Utilities
{
    public enum PathKind
    {
        Empty,
        Relative,
        RelativeToCurrentDirectory,
        RelativeToCurrentParent,
        RelativeToCurrentRoot,
        RelativeToDriveDirectory,
        Absolute
    }
}
