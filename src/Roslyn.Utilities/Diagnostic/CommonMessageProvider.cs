using System;
using System.Globalization;

namespace Microsoft.CodeAnalysis
{
    public abstract class CommonMessageProvider
    {
        public abstract DiagnosticSeverity GetSeverity(int code);

        public abstract string LoadMessage(int code, CultureInfo language);

        public abstract LocalizableString GetTitle(int code);

        public abstract LocalizableString GetDescription(int code);

        public abstract LocalizableString GetMessageFormat(int code);

        public abstract string GetHelpLink(int code);

        public abstract string GetCategory(int code);

        public abstract string CodePrefix { get; }

        public abstract int GetWarningLevel(int code);

        public abstract Type ErrorCodeType { get; }

        public Diagnostic CreateDiagnostic(int code, Location location)
        {
            return CreateDiagnostic(code, location, Array.Empty<object>());
        }

        public abstract Diagnostic CreateDiagnostic(DiagnosticInfo info);

        public abstract Diagnostic CreateDiagnostic(int code, Location location, params object[] args);

        public abstract string GetMessagePrefix(string id, DiagnosticSeverity severity, bool isWarningAsError, CultureInfo culture);

        public string GetIdForErrorCode(int errorCode)
        {
            return CodePrefix + errorCode.ToString("0000");
        }

        public abstract ReportDiagnostic GetDiagnosticReport(DiagnosticInfo diagnosticInfo);
    }
}
