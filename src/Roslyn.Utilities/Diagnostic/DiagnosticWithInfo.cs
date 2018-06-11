﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(), nq}")]
    public class DiagnosticWithInfo : Diagnostic
    {
        private readonly DiagnosticInfo _info;

        public DiagnosticWithInfo(DiagnosticInfo info, Location location, bool isSuppressed = false)
        {
            Debug.Assert(info != null);
            Debug.Assert(location != null);
            _info = info;
            Location = location;
            IsSuppressed = isSuppressed;
        }

        public override Location Location { get; }

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

        public override bool IsSuppressed { get; }

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
                return _info.Severity == InternalDiagnosticSeverity.Unknown
                       || _info.Severity == InternalDiagnosticSeverity.Void;
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

        public override bool Equals(Diagnostic other)
        {
            if (Equals(this, other))
            {
                return true;
            }

            DiagnosticWithInfo o = other as DiagnosticWithInfo;
            if (o == null || GetType() != o.GetType())
            {
                return false;
            }

            return
                Location.Equals(o.Location)
                && Info.Equals(o.Info)
                && AdditionalLocations.SequenceEqual(other.AdditionalLocations);
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

            if (location != Location)
            {
                return new DiagnosticWithInfo(_info, location, IsSuppressed);
            }

            return this;
        }

        public override Diagnostic WithSeverity(DiagnosticSeverity severity)
        {
            if (Severity != severity)
            {
                return new DiagnosticWithInfo(Info.GetInstanceWithSeverity(severity), Location, IsSuppressed);
            }

            return this;
        }

        public override Diagnostic WithIsSuppressed(bool isSuppressed)
        {
            if (IsSuppressed != isSuppressed)
            {
                return new DiagnosticWithInfo(Info, Location, isSuppressed);
            }

            return this;
        }
    }
}
