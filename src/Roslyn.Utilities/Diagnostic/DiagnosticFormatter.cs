using System;
using System.Globalization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public class DiagnosticFormatter
    {
        public virtual string Format(Diagnostic diagnostic, IFormatProvider formatter = null)
        {
            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            CultureInfo culture = formatter as CultureInfo;
            FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
            FileLinePositionSpan mappedSpan = diagnostic.Location.GetMappedLineSpan();
            if (span.IsValid || mappedSpan.IsValid)
            {
                string path, basePath;
                if (mappedSpan.HasMappedPath)
                {
                    path = mappedSpan.Path;
                    basePath = span.Path;
                }
                else
                {
                    path = span.Path;
                    basePath = null;
                }

                return string.Format(formatter,
                    "{0}{1}: {2}: {3}",
                    FormatSourcePath(path, basePath, formatter),
                    FormatSourceSpan(mappedSpan.Span, formatter),
                    GetMessagePrefix(diagnostic),
                    diagnostic.GetMessage(culture));
            }

            return string.Empty;
        }

        public virtual string FormatSourcePath(string path, string basePath, IFormatProvider formatter)
        {
            return path;
        }

        public virtual string FormatSourceSpan(LinePositionSpan span, IFormatProvider formatter)
        {
            return string.Format("({0},{1})", span.Start.Line + 1, span.Start.Character + 1);
        }

        public string GetMessagePrefix(Diagnostic diagnostic)
        {
            string prefix;
            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Hidden:
                    prefix = "hidden";
                    break;
                case DiagnosticSeverity.Info:
                    prefix = "info";
                    break;
                case DiagnosticSeverity.Warning:
                    prefix = "warning";
                    break;
                case DiagnosticSeverity.Error:
                    prefix = "error";
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(diagnostic.Severity);
            }

            return string.Format("{0} {1}", prefix, diagnostic.Id);
        }

        internal static readonly DiagnosticFormatter Instance = new DiagnosticFormatter();
    }
}
