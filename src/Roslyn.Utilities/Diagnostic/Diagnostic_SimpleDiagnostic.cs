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
            private readonly DiagnosticDescriptor _descriptor;
            private readonly DiagnosticSeverity _severity;
            private readonly int _warningLevel;
            private readonly Location _location;
            private readonly IReadOnlyList<Location> _additionalLocations;
            private readonly object[] _messageArgs;
            private readonly ImmutableDictionary<string, string> _properties;
            private readonly bool _isSuppressed;

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
                if (warningLevel == 0 && severity != DiagnosticSeverity.Error ||
                    warningLevel != 0 && severity == DiagnosticSeverity.Error)
                {
                    throw new ArgumentException(
                        $"{nameof(warningLevel)} ({warningLevel}) and {nameof(severity)} ({severity}) are not compatible.",
                        nameof(warningLevel));
                }

                _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
                _severity = severity;
                _warningLevel = warningLevel;
                _location = location ?? Location.None;
                _additionalLocations = additionalLocations?.ToImmutableArray() ?? SpecializedCollections.EmptyReadOnlyList<Location>();
                _messageArgs = messageArgs ?? Array.Empty<object>();
                _properties = properties ?? ImmutableDictionary<string, string>.Empty;
                _isSuppressed = isSuppressed;
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

            public override DiagnosticDescriptor Descriptor
            {
                get
                {
                    return _descriptor;
                }
            }

            public override string Id
            {
                get
                {
                    return _descriptor.Id;
                }
            }

            public override string GetMessage(IFormatProvider formatProvider = null)
            {
                if (_messageArgs.Length == 0)
                {
                    return _descriptor.MessageFormat.ToString(formatProvider);
                }

                string localizedMessageFormat = _descriptor.MessageFormat.ToString(formatProvider);
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

            public override DiagnosticSeverity Severity
            {
                get
                {
                    return _severity;
                }
            }

            public override bool IsSuppressed
            {
                get
                {
                    return _isSuppressed;
                }
            }

            public override int WarningLevel
            {
                get
                {
                    return _warningLevel;
                }
            }

            public override Location Location
            {
                get
                {
                    return _location;
                }
            }

            public override IReadOnlyList<Location> AdditionalLocations
            {
                get
                {
                    return _additionalLocations;
                }
            }

            public override ImmutableDictionary<string, string> Properties
            {
                get
                {
                    return _properties;
                }
            }

            public override bool Equals(Diagnostic obj)
            {
                if (!(obj is SimpleDiagnostic other))
                {
                    return false;
                }

                return _descriptor.Equals(other._descriptor) &&
                       _messageArgs.SequenceEqual(other._messageArgs, (a, b) => a == b) &&
                       _location == other._location &&
                       _severity == other._severity &&
                       _warningLevel == other._warningLevel;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Diagnostic);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_descriptor,
                    Hash.CombineValues(_messageArgs,
                        Hash.Combine(_warningLevel,
                            Hash.Combine(_location, (int) _severity))));
            }

            public override Diagnostic WithLocation(Location location)
            {
                if (location == null)
                {
                    throw new ArgumentNullException(nameof(location));
                }

                if (location != _location)
                {
                    return new SimpleDiagnostic(_descriptor,
                        _severity,
                        _warningLevel,
                        location,
                        _additionalLocations,
                        _messageArgs,
                        _properties,
                        _isSuppressed);
                }

                return this;
            }

            public override Diagnostic WithSeverity(DiagnosticSeverity severity)
            {
                if (Severity != severity)
                {
                    int warningLevel = GetDefaultWarningLevel(severity);
                    return new SimpleDiagnostic(_descriptor,
                        severity,
                        warningLevel,
                        _location,
                        _additionalLocations,
                        _messageArgs,
                        _properties,
                        _isSuppressed);
                }

                return this;
            }

            public override Diagnostic WithIsSuppressed(bool isSuppressed)
            {
                if (IsSuppressed != isSuppressed)
                {
                    return new SimpleDiagnostic(_descriptor,
                        _severity,
                        _warningLevel,
                        _location,
                        _additionalLocations,
                        _messageArgs,
                        _properties,
                        isSuppressed);
                }

                return this;
            }
        }
    }
}
