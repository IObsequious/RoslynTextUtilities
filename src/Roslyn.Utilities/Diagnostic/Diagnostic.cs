using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(), nq}")]
    public abstract partial class Diagnostic : IEquatable<Diagnostic>, IFormattable
    {
        internal const string CompilerDiagnosticCategory = "Compiler";
        internal const int HighestValidWarningLevel = 4;

        public static Diagnostic Create(
            DiagnosticDescriptor descriptor,
            Location location,
            params object[] messageArgs)
        {
            return Create(descriptor, location, null, null, messageArgs);
        }

        public static Diagnostic Create(
            DiagnosticDescriptor descriptor,
            Location location,
            ImmutableDictionary<string, string> properties,
            params object[] messageArgs)
        {
            return Create(descriptor, location, null, properties, messageArgs);
        }

        public static Diagnostic Create(
            DiagnosticDescriptor descriptor,
            Location location,
            IEnumerable<Location> additionalLocations,
            params object[] messageArgs)
        {
            return Create(descriptor, location, additionalLocations, null, messageArgs);
        }

        public static Diagnostic Create(
            DiagnosticDescriptor descriptor,
            Location location,
            IEnumerable<Location> additionalLocations,
            ImmutableDictionary<string, string> properties,
            params object[] messageArgs)
        {
            return Create(descriptor,
                location,
                descriptor.DefaultSeverity,
                additionalLocations,
                properties,
                messageArgs);
        }

        public static Diagnostic Create(
            DiagnosticDescriptor descriptor,
            Location location,
            DiagnosticSeverity effectiveSeverity,
            IEnumerable<Location> additionalLocations,
            ImmutableDictionary<string, string> properties,
            params object[] messageArgs)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            int warningLevel = GetDefaultWarningLevel(effectiveSeverity);
            return SimpleDiagnostic.Create(
                descriptor,
                effectiveSeverity,
                warningLevel,
                location ?? Location.None,
                additionalLocations,
                messageArgs,
                properties);
        }

        public static Diagnostic Create(
            string id,
            string category,
            LocalizableString message,
            DiagnosticSeverity severity,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault,
            int warningLevel,
            LocalizableString title = null,
            LocalizableString description = null,
            string helpLink = null,
            Location location = null,
            IEnumerable<Location> additionalLocations = null,
            IEnumerable<string> customTags = null,
            ImmutableDictionary<string, string> properties = null)
        {
            return Create(id,
                category,
                message,
                severity,
                defaultSeverity,
                isEnabledByDefault,
                warningLevel,
                false,
                title,
                description,
                helpLink,
                location,
                additionalLocations,
                customTags,
                properties);
        }

        public static Diagnostic Create(
            string id,
            string category,
            LocalizableString message,
            DiagnosticSeverity severity,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault,
            int warningLevel,
            bool isSuppressed,
            LocalizableString title = null,
            LocalizableString description = null,
            string helpLink = null,
            Location location = null,
            IEnumerable<Location> additionalLocations = null,
            IEnumerable<string> customTags = null,
            ImmutableDictionary<string, string> properties = null)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return SimpleDiagnostic.Create(id,
                title ?? string.Empty,
                category,
                message,
                description ?? string.Empty,
                helpLink ?? string.Empty,
                severity,
                defaultSeverity,
                isEnabledByDefault,
                warningLevel,
                location ?? Location.None,
                additionalLocations,
                customTags,
                properties,
                isSuppressed);
        }

        public static Diagnostic Create(CommonMessageProvider messageProvider, int errorCode)
        {
            return Create(new DiagnosticInfo(messageProvider, errorCode));
        }

        public static Diagnostic Create(CommonMessageProvider messageProvider, int errorCode, params object[] arguments)
        {
            return Create(new DiagnosticInfo(messageProvider, errorCode, arguments));
        }

        public static Diagnostic Create(DiagnosticInfo info)
        {
            return new DiagnosticWithInfo(info, Location.None);
        }

        public abstract DiagnosticDescriptor Descriptor { get; }

        public abstract string Id { get; }

        internal virtual string Category
        {
            get
            {
                return Descriptor.Category;
            }
        }

        public abstract string GetMessage(IFormatProvider formatProvider = null);

        public virtual DiagnosticSeverity DefaultSeverity
        {
            get
            {
                return Descriptor.DefaultSeverity;
            }
        }

        public abstract DiagnosticSeverity Severity { get; }

        public abstract int WarningLevel { get; }

        public abstract bool IsSuppressed { get; }

        //public SuppressionInfo GetSuppressionInfo(Compilation compilation)
        //{
        //    if (!IsSuppressed)
        //    {
        //        return null;
        //    }

        //    AttributeData attribute;
        //    var suppressMessageState = new SuppressMessageAttributeState(compilation);
        //    if (!suppressMessageState.IsDiagnosticSuppressed(this, out attribute))
        //    {
        //        attribute = null;
        //    }

        //    return new SuppressionInfo(Id, attribute);
        //}

        internal virtual bool IsEnabledByDefault
        {
            get
            {
                return Descriptor.IsEnabledByDefault;
            }
        }

        public bool IsWarningAsError
        {
            get
            {
                return DefaultSeverity == DiagnosticSeverity.Warning
                       && Severity == DiagnosticSeverity.Error;
            }
        }

        public abstract Location Location { get; }

        public abstract IReadOnlyList<Location> AdditionalLocations { get; }

        internal virtual IReadOnlyList<string> CustomTags
        {
            get
            {
                return (IReadOnlyList<string>) Descriptor.CustomTags;
            }
        }

        public virtual ImmutableDictionary<string, string> Properties
        {
            get
            {
                return ImmutableDictionary<string, string>.Empty;
            }
        }

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return DiagnosticFormatter.Instance.Format(this, formatProvider);
        }

        public override string ToString()
        {
            return DiagnosticFormatter.Instance.Format(this, CultureInfo.CurrentUICulture);
        }

        public abstract override bool Equals(object obj);

        public abstract override int GetHashCode();

        public abstract bool Equals(Diagnostic other);

        private string GetDebuggerDisplay()
        {
            switch (Severity)
            {
                case InternalDiagnosticSeverity.Unknown:
                    return "Unresolved diagnostic at " + Location;
                case InternalDiagnosticSeverity.Void:
                    return "Void diagnostic at " + Location;
                default:
                    return ToString();
            }
        }

        public abstract Diagnostic WithLocation(Location location);

        public abstract Diagnostic WithSeverity(DiagnosticSeverity severity);

        public abstract Diagnostic WithIsSuppressed(bool isSuppressed);

        internal virtual int Code
        {
            get
            {
                return 0;
            }
        }

        internal virtual IReadOnlyList<object> Arguments
        {
            get
            {
                return SpecializedCollections.EmptyReadOnlyList<object>();
            }
        }

        //public bool HasIntersectingLocation(SyntaxTree tree, TextSpan? filterSpanWithinTree = null)
        //{
        //    IEnumerable<Location> locations = GetDiagnosticLocationsWithinTree(tree);
        //    foreach (Location location in locations)
        //    {
        //        if (!filterSpanWithinTree.HasValue || filterSpanWithinTree.Value.IntersectsWith(location.SourceSpan))
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        //private IEnumerable<Location> GetDiagnosticLocationsWithinTree(SyntaxTree tree)
        //{
        //    if (Location.SourceTree == tree)
        //    {
        //        yield return Location;
        //    }

        //    if (AdditionalLocations != null)
        //    {
        //        foreach (Location additionalLocation in AdditionalLocations)
        //        {
        //            if (additionalLocation.SourceTree == tree)
        //            {
        //                yield return additionalLocation;
        //            }
        //        }
        //    }
        //}

        public Diagnostic WithReportDiagnostic(ReportDiagnostic reportAction)
        {
            switch (reportAction)
            {
                case ReportDiagnostic.Suppress:
                    return null;
                case ReportDiagnostic.Error:
                    return WithSeverity(DiagnosticSeverity.Error);
                case ReportDiagnostic.Default:
                    return this;
                case ReportDiagnostic.Warn:
                    return WithSeverity(DiagnosticSeverity.Warning);
                case ReportDiagnostic.Info:
                    return WithSeverity(DiagnosticSeverity.Info);
                case ReportDiagnostic.Hidden:
                    return WithSeverity(DiagnosticSeverity.Hidden);
                default:
                    throw ExceptionUtilities.UnexpectedValue(reportAction);
            }
        }

        public static int GetDefaultWarningLevel(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Error:
                    return 0;
                case DiagnosticSeverity.Warning:
                default:
                    return 1;
            }
        }
    }

    public abstract class RequiredLanguageVersion : IFormattable
    {
        public abstract override string ToString();

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return ToString();
        }
    }
}
