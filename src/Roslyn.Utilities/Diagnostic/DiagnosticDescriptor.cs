using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public sealed class DiagnosticDescriptor : IEquatable<DiagnosticDescriptor>
    {
        public string Id { get; }

        public LocalizableString Title { get; }

        public LocalizableString Description { get; }

        public string HelpLinkUri { get; }

        public LocalizableString MessageFormat { get; }

        public string Category { get; }

        public DiagnosticSeverity DefaultSeverity { get; }

        public bool IsEnabledByDefault { get; }

        public IEnumerable<string> CustomTags { get; }

        public DiagnosticDescriptor(
            string id,
            string title,
            string messageFormat,
            string category,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault,
            string description = null,
            string helpLinkUri = null,
            params string[] customTags)
            : this(id,
                title,
                messageFormat,
                category,
                defaultSeverity,
                isEnabledByDefault,
                description,
                helpLinkUri,
                customTags.ToImmutableArray())
        {
        }

        public DiagnosticDescriptor(
            string id,
            LocalizableString title,
            LocalizableString messageFormat,
            string category,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault,
            LocalizableString description = null,
            string helpLinkUri = null,
            params string[] customTags)
            : this(id,
                title,
                messageFormat,
                category,
                defaultSeverity,
                isEnabledByDefault,
                description,
                helpLinkUri,
                customTags.ToImmutableArray())
        {
        }

        internal DiagnosticDescriptor(
            string id,
            LocalizableString title,
            LocalizableString messageFormat,
            string category,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault,
            LocalizableString description,
            string helpLinkUri,
            ImmutableArray<string> customTags)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(CodeAnalysisResources.DiagnosticIdCantBeNullOrWhitespace, nameof(id));
            }

            if (messageFormat == null)
            {
                throw new ArgumentNullException(nameof(messageFormat));
            }

            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            Id = id;
            Title = title;
            Category = category;
            MessageFormat = messageFormat;
            DefaultSeverity = defaultSeverity;
            IsEnabledByDefault = isEnabledByDefault;
            Description = description ?? string.Empty;
            HelpLinkUri = helpLinkUri ?? string.Empty;
            CustomTags = customTags;
        }

        public bool Equals(DiagnosticDescriptor other)
        {
            return
                other != null &&
                Category == other.Category &&
                DefaultSeverity == other.DefaultSeverity &&
                Description.Equals(other.Description) &&
                HelpLinkUri == other.HelpLinkUri &&
                Id == other.Id &&
                IsEnabledByDefault == other.IsEnabledByDefault &&
                MessageFormat.Equals(other.MessageFormat) &&
                Title.Equals(other.Title);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DiagnosticDescriptor);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Category.GetHashCode(),
                Hash.Combine(DefaultSeverity.GetHashCode(),
                    Hash.Combine(Description.GetHashCode(),
                        Hash.Combine(HelpLinkUri.GetHashCode(),
                            Hash.Combine(Id.GetHashCode(),
                                Hash.Combine(IsEnabledByDefault.GetHashCode(),
                                    Hash.Combine(MessageFormat.GetHashCode(),
                                        Title.GetHashCode())))))));
        }

        private static ReportDiagnostic MapSeverityToReport(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Hidden:
                    return ReportDiagnostic.Hidden;
                case DiagnosticSeverity.Info:
                    return ReportDiagnostic.Info;
                case DiagnosticSeverity.Warning:
                    return ReportDiagnostic.Warn;
                case DiagnosticSeverity.Error:
                    return ReportDiagnostic.Error;
                default:
                    throw ExceptionUtilities.UnexpectedValue(severity);
            }
        }

    }
}
