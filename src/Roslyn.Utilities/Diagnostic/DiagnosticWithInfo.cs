using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public class DiagnosticWithInfo : Diagnostic
    {
        private readonly DiagnosticInfo _info;
        private readonly Location _location;
        private readonly bool _isSuppressed;

        public DiagnosticWithInfo(DiagnosticInfo info, Location location, bool isSuppressed = false)
        {
            Debug.Assert(info != null);
            Debug.Assert(location != null);
            _info = info;
            _location = location;
            _isSuppressed = isSuppressed;
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
                return Info.AdditionalLocations;
            }
        }

        internal override IReadOnlyList<string> CustomTags
        {
            get
            {
                return Info.CustomTags;
            }
        }

        public override DiagnosticDescriptor Descriptor
        {
            get
            {
                return Info.Descriptor;
            }
        }

        public override string Id
        {
            get
            {
                return Info.MessageIdentifier;
            }
        }

        internal override string Category
        {
            get
            {
                return Info.Category;
            }
        }

        internal sealed override int Code
        {
            get
            {
                return Info.Code;
            }
        }

        public sealed override DiagnosticSeverity Severity
        {
            get
            {
                return Info.Severity;
            }
        }

        public sealed override DiagnosticSeverity DefaultSeverity
        {
            get
            {
                return Info.DefaultSeverity;
            }
        }

        internal sealed override bool IsEnabledByDefault
        {
            get
            {
                return true;
            }
        }

        public override bool IsSuppressed
        {
            get
            {
                return _isSuppressed;
            }
        }

        public sealed override int WarningLevel
        {
            get
            {
                return Info.WarningLevel;
            }
        }

        public override string GetMessage(IFormatProvider formatProvider = null)
        {
            return Info.GetMessage(formatProvider);
        }

        internal override IReadOnlyList<object> Arguments
        {
            get
            {
                return Info.Arguments;
            }
        }

        public DiagnosticInfo Info
        {
            get
            {
                if (_info.Severity == InternalDiagnosticSeverity.Unknown)
                {
                    return _info.GetResolvedInfo();
                }

                return _info;
            }
        }

        internal bool HasLazyInfo
        {
            get
            {
                return _info.Severity == InternalDiagnosticSeverity.Unknown ||
                       _info.Severity == InternalDiagnosticSeverity.Void;
            }
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Location.GetHashCode(), Info.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Diagnostic);
        }

        public override bool Equals(Diagnostic obj)
        {
            if (this == obj)
            {
                return true;
            }

            DiagnosticWithInfo other = obj as DiagnosticWithInfo;
            if (other == null || GetType() != other.GetType())
            {
                return false;
            }

            return
                Location.Equals(other._location) &&
                Info.Equals(other.Info) &&
                AdditionalLocations.SequenceEqual(other.AdditionalLocations);
        }

        private string GetDebuggerDisplay()
        {
            switch (_info.Severity)
            {
                case InternalDiagnosticSeverity.Unknown:
                    return "Unresolved diagnostic at " + Location;
                case InternalDiagnosticSeverity.Void:
                    return "Void diagnostic at " + Location;
                default:
                    return ToString();
            }
        }

        public override Diagnostic WithLocation(Location location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (location != _location)
            {
                return new DiagnosticWithInfo(_info, location, _isSuppressed);
            }

            return this;
        }

        public override Diagnostic WithSeverity(DiagnosticSeverity severity)
        {
            if (Severity != severity)
            {
                return new DiagnosticWithInfo(Info.GetInstanceWithSeverity(severity), _location, _isSuppressed);
            }

            return this;
        }

        public override Diagnostic WithIsSuppressed(bool isSuppressed)
        {
            if (IsSuppressed != isSuppressed)
            {
                return new DiagnosticWithInfo(Info, _location, isSuppressed);
            }

            return this;
        }
    }
}
