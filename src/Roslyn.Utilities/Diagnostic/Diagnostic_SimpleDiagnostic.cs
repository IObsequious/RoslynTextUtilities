using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public abstract partial class Diagnostic
    {
        public sealed class SimpleDiagnostic : Diagnostic
        {
            private readonly object[] _messageArgs;

            private SimpleDiagnostic(
                DiagnosticDescriptor descriptor,
                DiagnosticSeverity severity,
                int warningLevel,
                Location location,
                IEnumerable<Location> additionalLocations,
                object[] messageArgs,
                ImmutableDictionary<string, string> properties,
                bool isSuppressed)
            {
                if ((warningLevel == 0 && severity != DiagnosticSeverity.Error)
                    || (warningLevel != 0 && severity == DiagnosticSeverity.Error))
                {
                    throw new ArgumentException(
                        $"{nameof(warningLevel)} ({warningLevel}) and {nameof(severity)} ({severity}) are not compatible.",
                        nameof(warningLevel));
                }

                Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
                Severity = severity;
                WarningLevel = warningLevel;
                Location = location ?? Location.None;
                AdditionalLocations = additionalLocations?.ToImmutableArray() ?? SpecializedCollections.EmptyReadOnlyList<Location>();
                _messageArgs = messageArgs ?? Array.Empty<object>();
                Properties = properties ?? ImmutableDictionary<string, string>.Empty;
                IsSuppressed = isSuppressed;
            }

            public static SimpleDiagnostic Create(
                DiagnosticDescriptor descriptor,
                DiagnosticSeverity severity,
                int warningLevel,
                Location location,
                IEnumerable<Location> additionalLocations,
                object[] messageArgs,
                ImmutableDictionary<string, string> properties,
                bool isSuppressed = false)
            {
                return new SimpleDiagnostic(descriptor,
                    severity,
                    warningLevel,
                    location,
                    additionalLocations,
                    messageArgs,
                    properties,
                    isSuppressed);
            }

            public static SimpleDiagnostic Create(string id,
                LocalizableString title,
                string category,
                LocalizableString message,
                LocalizableString description,
                string helpLink,
                DiagnosticSeverity severity,
                DiagnosticSeverity defaultSeverity,
                bool isEnabledByDefault,
                int warningLevel,
                Location location,
                IEnumerable<Location> additionalLocations,
                IEnumerable<string> customTags,
                ImmutableDictionary<string, string> properties,
                bool isSuppressed = false)
            {
                DiagnosticDescriptor descriptor = new DiagnosticDescriptor(id,
                    title,
                    message,
                    category,
                    defaultSeverity,
                    isEnabledByDefault,
                    description,
                    helpLink,
                    customTags.ToImmutableArray());
                return new SimpleDiagnostic(descriptor,
                    severity,
                    warningLevel,
                    location,
                    additionalLocations,
                    null,
                    properties,
                    isSuppressed);
            }

            public override DiagnosticDescriptor Descriptor { get; }

            public override string Id
            {
                get
                {
                    return Descriptor.Id;
                }
            }

            public override string GetMessage(IFormatProvider formatProvider = null)
            {
                if (_messageArgs.Length == 0)
                {
                    return Descriptor.MessageFormat.ToString(formatProvider);
                }

                string localizedMessageFormat = Descriptor.MessageFormat.ToString(formatProvider);
                try
                {
                    return string.Format(formatProvider, localizedMessageFormat, _messageArgs);
                }
                catch (Exception)
                {
                    return localizedMessageFormat;
                }
            }

            internal override IReadOnlyList<object> Arguments
            {
                get
                {
                    return _messageArgs;
                }
            }

            public override DiagnosticSeverity Severity { get; }

            public override bool IsSuppressed { get; }

            public override int WarningLevel { get; }

            public override Location Location { get; }

            public override IReadOnlyList<Location> AdditionalLocations { get; }

            public override ImmutableDictionary<string, string> Properties { get; }

            public override bool Equals(Diagnostic other)
            {
                if (!(other is SimpleDiagnostic otherDiagnostic))
                {
                    return false;
                }

                return Descriptor.Equals(otherDiagnostic.Descriptor)
                       && _messageArgs.SequenceEqual(otherDiagnostic._messageArgs, (a, b) => a == b)
                       && Location == otherDiagnostic.Location
                       && Severity == otherDiagnostic.Severity
                       && WarningLevel == otherDiagnostic.WarningLevel;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Diagnostic);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(Descriptor,
                    Hash.CombineValues(_messageArgs,
                        Hash.Combine(WarningLevel,
                            Hash.Combine(Location, (int) Severity))));
            }

            public override Diagnostic WithLocation(Location location)
            {
                if (location == null)
                {
                    throw new ArgumentNullException(nameof(location));
                }

                if (location != Location)
                {
                    return new SimpleDiagnostic(Descriptor,
                        Severity,
                        WarningLevel,
                        location,
                        AdditionalLocations,
                        _messageArgs,
                        Properties,
                        IsSuppressed);
                }

                return this;
            }

            public override Diagnostic WithSeverity(DiagnosticSeverity severity)
            {
                if (Severity != severity)
                {
                    int warningLevel = GetDefaultWarningLevel(severity);
                    return new SimpleDiagnostic(Descriptor,
                        severity,
                        warningLevel,
                        Location,
                        AdditionalLocations,
                        _messageArgs,
                        Properties,
                        IsSuppressed);
                }

                return this;
            }

            public override Diagnostic WithIsSuppressed(bool isSuppressed)
            {
                if (IsSuppressed != isSuppressed)
                {
                    return new SimpleDiagnostic(Descriptor,
                        Severity,
                        WarningLevel,
                        Location,
                        AdditionalLocations,
                        _messageArgs,
                        Properties,
                        isSuppressed);
                }

                return this;
            }
        }
    }
}
