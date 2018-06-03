using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public class DiagnosticInfo : IFormattable, IObjectWritable
    {
        private readonly CommonMessageProvider _messageProvider;
        private readonly int _errorCode;
        private readonly DiagnosticSeverity _defaultSeverity;
        private readonly DiagnosticSeverity _effectiveSeverity;
        private readonly object[] _arguments;
        private static ImmutableDictionary<int, DiagnosticDescriptor> s_errorCodeToDescriptorMap =
            ImmutableDictionary<int, DiagnosticDescriptor>.Empty;
        private static readonly ImmutableArray<string> s_compilerErrorCustomTags = ImmutableArray.Create(WellKnownDiagnosticTags.Compiler,
            WellKnownDiagnosticTags.Telemetry,
            WellKnownDiagnosticTags.NotConfigurable);
        private static readonly ImmutableArray<string> s_compilerNonErrorCustomTags =
            ImmutableArray.Create(WellKnownDiagnosticTags.Compiler, WellKnownDiagnosticTags.Telemetry);

        static DiagnosticInfo()
        {
            ObjectBinder.RegisterTypeReader(typeof(DiagnosticInfo), r => new DiagnosticInfo(r));
        }

        internal DiagnosticInfo(CommonMessageProvider messageProvider, int errorCode)
        {
            _messageProvider = messageProvider;
            _errorCode = errorCode;
            _defaultSeverity = messageProvider.GetSeverity(errorCode);
            _effectiveSeverity = _defaultSeverity;
        }

        internal DiagnosticInfo(CommonMessageProvider messageProvider, int errorCode, params object[] arguments)
            : this(messageProvider, errorCode)
        {
            _arguments = arguments;
        }

        private DiagnosticInfo(DiagnosticInfo original, DiagnosticSeverity overriddenSeverity)
        {
            _messageProvider = original.MessageProvider;
            _errorCode = original._errorCode;
            _defaultSeverity = original.DefaultSeverity;
            _arguments = original._arguments;
            _effectiveSeverity = overriddenSeverity;
        }

        public static DiagnosticDescriptor GetDescriptor(int errorCode, CommonMessageProvider messageProvider)
        {
            DiagnosticSeverity defaultSeverity = messageProvider.GetSeverity(errorCode);
            return GetOrCreateDescriptor(errorCode, defaultSeverity, messageProvider);
        }

        private static DiagnosticDescriptor GetOrCreateDescriptor(int errorCode,
            DiagnosticSeverity defaultSeverity,
            CommonMessageProvider messageProvider)
        {
            return ImmutableInterlocked.GetOrAdd(ref s_errorCodeToDescriptorMap,
                errorCode,
                code => CreateDescriptor(code, defaultSeverity, messageProvider));
        }

        private static DiagnosticDescriptor CreateDescriptor(int errorCode,
            DiagnosticSeverity defaultSeverity,
            CommonMessageProvider messageProvider)
        {
            string id = messageProvider.GetIdForErrorCode(errorCode);
            LocalizableString title = messageProvider.GetTitle(errorCode);
            LocalizableString description = messageProvider.GetDescription(errorCode);
            LocalizableString messageFormat = messageProvider.GetMessageFormat(errorCode);
            string helpLink = messageProvider.GetHelpLink(errorCode);
            string category = messageProvider.GetCategory(errorCode);
            var customTags = GetCustomTags(defaultSeverity);
            return new DiagnosticDescriptor(id,
                title,
                messageFormat,
                category,
                defaultSeverity,
                true,
                description,
                helpLink,
                customTags);
        }


        internal DiagnosticInfo(CommonMessageProvider messageProvider, bool isWarningAsError, int errorCode, params object[] arguments)
            : this(messageProvider, errorCode, arguments)
        {
            Debug.Assert(!isWarningAsError || _defaultSeverity == DiagnosticSeverity.Warning);
            if (isWarningAsError)
            {
                _effectiveSeverity = DiagnosticSeverity.Error;
            }
        }

        public DiagnosticInfo GetInstanceWithSeverity(DiagnosticSeverity severity)
        {
            return new DiagnosticInfo(this, severity);
        }

        #region Serialization

        bool IObjectWritable.ShouldReuseInSerialization => false;

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            WriteTo(writer);
        }

        protected virtual void WriteTo(ObjectWriter writer)
        {
            writer.WriteValue(_messageProvider);
            writer.WriteUInt32((uint) _errorCode);
            writer.WriteInt32((int) _effectiveSeverity);
            writer.WriteInt32((int) _defaultSeverity);
            int count = _arguments?.Length ?? 0;
            writer.WriteUInt32((uint) count);
            if (count > 0)
            {
                foreach (object arg in _arguments)
                {
                    writer.WriteString(arg.ToString());
                }
            }
        }

        protected DiagnosticInfo(ObjectReader reader)
        {
            _messageProvider = (CommonMessageProvider) reader.ReadValue();
            _errorCode = (int) reader.ReadUInt32();
            _effectiveSeverity = (DiagnosticSeverity) reader.ReadInt32();
            _defaultSeverity = (DiagnosticSeverity) reader.ReadInt32();
            int count = (int) reader.ReadUInt32();
            if (count == 0)
            {
                _arguments = Array.Empty<object>();
            }
            else if (count > 0)
            {
                _arguments = new string[count];
                for (int i = 0; i < count; i++)
                {
                    _arguments[i] = reader.ReadString();
                }
            }
        }

        #endregion

        public int Code
        {
            get
            {
                return _errorCode;
            }
        }

        public DiagnosticDescriptor Descriptor
        {
            get
            {
                return GetOrCreateDescriptor(_errorCode, _defaultSeverity, _messageProvider);
            }
        }

        public DiagnosticSeverity Severity
        {
            get
            {
                return _effectiveSeverity;
            }
        }

        public DiagnosticSeverity DefaultSeverity
        {
            get
            {
                return _defaultSeverity;
            }
        }

        public int WarningLevel
        {
            get
            {
                if (_effectiveSeverity != _defaultSeverity)
                {
                    return Diagnostic.GetDefaultWarningLevel(_effectiveSeverity);
                }

                return _messageProvider.GetWarningLevel(_errorCode);
            }
        }

        public bool IsWarningAsError
        {
            get
            {
                return DefaultSeverity == DiagnosticSeverity.Warning &&
                       Severity == DiagnosticSeverity.Error;
            }
        }

        public string Category
        {
            get
            {
                return _messageProvider.GetCategory(_errorCode);
            }
        }

        internal ImmutableArray<string> CustomTags
        {
            get
            {
                return GetCustomTags(_defaultSeverity);
            }
        }

        private static ImmutableArray<string> GetCustomTags(DiagnosticSeverity defaultSeverity)
        {
            return defaultSeverity == DiagnosticSeverity.Error ?
                s_compilerErrorCustomTags :
                s_compilerNonErrorCustomTags;
        }

        public bool IsNotConfigurable()
        {
            return _defaultSeverity == DiagnosticSeverity.Error;
        }

        public virtual IReadOnlyList<Location> AdditionalLocations
        {
            get
            {
                return SpecializedCollections.EmptyReadOnlyList<Location>();
            }
        }

        public string MessageIdentifier
        {
            get
            {
                return _messageProvider.GetIdForErrorCode(_errorCode);
            }
        }

        public virtual string GetMessage(IFormatProvider formatProvider = null)
        {
            string message = _messageProvider.LoadMessage(_errorCode, formatProvider as CultureInfo);
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            if (_arguments == null || _arguments.Length == 0)
            {
                return message;
            }

            return string.Format(formatProvider, message, GetArgumentsToUse(formatProvider));
        }

        protected object[] GetArgumentsToUse(IFormatProvider formatProvider)
        {
            object[] argumentsToUse = null;
            for (int i = 0; i < _arguments.Length; i++)
            {
                if (_arguments[i] is DiagnosticInfo embedded)
                {
                    argumentsToUse = InitializeArgumentListIfNeeded(argumentsToUse);
                    argumentsToUse[i] = embedded.GetMessage(formatProvider);
                    continue;
                }
            }

            return argumentsToUse ?? _arguments;
        }

        private object[] InitializeArgumentListIfNeeded(object[] argumentsToUse)
        {
            if (argumentsToUse != null)
            {
                return argumentsToUse;
            }

            object[] newArguments = new object[_arguments.Length];
            Array.Copy(_arguments, newArguments, newArguments.Length);
            return newArguments;
        }

        internal object[] Arguments
        {
            get
            {
                return _arguments;
            }
        }

        internal CommonMessageProvider MessageProvider
        {
            get
            {
                return _messageProvider;
            }
        }

        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(IFormatProvider formatProvider)
        {
            return ((IFormattable) this).ToString(null, formatProvider);
        }

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider,
                "{0}: {1}",
                _messageProvider.GetMessagePrefix(MessageIdentifier, Severity, IsWarningAsError, formatProvider as CultureInfo),
                GetMessage(formatProvider));
        }

        public sealed override int GetHashCode()
        {
            int hashCode = _errorCode;
            if (_arguments != null)
            {
                for (int i = 0; i < _arguments.Length; i++)
                {
                    hashCode = Hash.Combine(_arguments[i], hashCode);
                }
            }

            return hashCode;
        }

        public sealed override bool Equals(object obj)
        {
            DiagnosticInfo other = obj as DiagnosticInfo;
            bool result = false;
            if (other != null &&
                other._errorCode == _errorCode &&
                GetType() == obj.GetType())
            {
                if (_arguments == null && other._arguments == null)
                {
                    result = true;
                }
                else if (_arguments != null && other._arguments != null && _arguments.Length == other._arguments.Length)
                {
                    result = true;
                    for (int i = 0; i < _arguments.Length; i++)
                    {
                        if (!Equals(_arguments[i], other._arguments[i]))
                        {
                            result = false;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private string GetDebuggerDisplay()
        {
            switch (Code)
            {
                case InternalErrorCode.Unknown:
                    return "Unresolved DiagnosticInfo";
                case InternalErrorCode.Void:
                    return "Void DiagnosticInfo";
                default:
                    return ToString();
            }
        }

        public virtual DiagnosticInfo GetResolvedInfo()
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
