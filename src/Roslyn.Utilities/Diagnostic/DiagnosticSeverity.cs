namespace Microsoft.CodeAnalysis
{
    public static class InternalDiagnosticSeverity
    {
        public const DiagnosticSeverity Unknown = (DiagnosticSeverity) InternalErrorCode.Unknown;
        public const DiagnosticSeverity Void = (DiagnosticSeverity) InternalErrorCode.Void;
    }

    public static class InternalErrorCode
    {
        public const int Unknown = -1;
        public const int Void = -2;
    }

    public enum DiagnosticSeverity
    {
        Hidden = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }
}
