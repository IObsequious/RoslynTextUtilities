using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(), nq}")]
    public class DiagnosticInfo : IFormattable, IObjectWritable
    {
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

        public DiagnosticInfo(CommonMessageProvider messageProvider, int errorCode)
        {
            MessageProvider = messageProvider;
            Code = errorCode;
            DefaultSeverity = messageProvider.GetSeverity(errorCode);
            Severity = DefaultSeverity;
        }

        public DiagnosticInfo(CommonMessageProvider messageProvider, int errorCode, params object[] arguments)
            : this(messageProvider, errorCode)
        {
            Arguments = arguments;
        }

        private DiagnosticInfo(DiagnosticInfo original, DiagnosticSeverity overriddenSeverity)
        {
            MessageProvider = original.MessageProvider;
            Code = original.Code;
            DefaultSeverity = original.DefaultSeverity;
            Arguments = original.Arguments;
            Severity = overriddenSeverity;
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
            ImmutableArray<string> customTags = GetCustomTags(defaultSeverity);
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

        public DiagnosticInfo(CommonMessageProvider messageProvider, bool isWarningAsError, int errorCode, params object[] arguments)
            : this(messageProvider, errorCode, arguments)
        {
            Debug.Assert(!isWarningAsError || DefaultSeverity == DiagnosticSeverity.Warning);
            if (isWarningAsError)
            {
                Severity = DiagnosticSeverity.Error;
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
            writer.WriteValue(MessageProvider);
            writer.WriteUInt32((uint) Code);
            writer.WriteInt32((int) Severity);
            writer.WriteInt32((int) DefaultSeverity);
            int count = Arguments?.Length ?? 0;
            writer.WriteUInt32((uint) count);
            if (count > 0)
            {
                foreach (object arg in Arguments)
                {
                    writer.WriteString(arg.ToString());
                }
            }
        }

        protected DiagnosticInfo(ObjectReader reader)
        {
            MessageProvider = (CommonMessageProvider) reader.ReadValue();
            Code = (int) reader.ReadUInt32();
            Severity = (DiagnosticSeverity) reader.ReadInt32();
            DefaultSeverity = (DiagnosticSeverity) reader.ReadInt32();
            int count = (int) reader.ReadUInt32();
            if (count == 0)
            {
                Arguments = Array.Empty<object>();
            }
            else if (count > 0)
            {
                Arguments = new object[count];
                for (int i = 0; i < count; i++)
                {
                    Arguments[i] = reader.ReadString();
                }
            }
        }

        #endregion

        public int Code { get; }

        public DiagnosticDescriptor Descriptor
        {
            get
            {
                return GetOrCreateDescriptor(Code, DefaultSeverity, MessageProvider);
            }
        }

        public DiagnosticSeverity Severity { get; }

        public DiagnosticSeverity DefaultSeverity { get; }

        public int WarningLevel
        {
            get
            {
                if (Severity != DefaultSeverity)
                {
                    return Diagnostic.GetDefaultWarningLevel(Severity);
                }

                return MessageProvider.GetWarningLevel(Code);
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

        public string Category
        {
            get
            {
                return MessageProvider.GetCategory(Code);
            }
        }

        internal ImmutableArray<string> CustomTags
        {
            get
            {
                return GetCustomTags(DefaultSeverity);
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
            return DefaultSeverity == DiagnosticSeverity.Error;
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
                return MessageProvider.GetIdForErrorCode(Code);
            }
        }

        public virtual string GetMessage(IFormatProvider formatProvider = null)
        {
            string message = MessageProvider.LoadMessage(Code, formatProvider as CultureInfo);
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            if (Arguments == null || Arguments.Length == 0)
            {
                return message;
            }

            return string.Format(formatProvider, message, GetArgumentsToUse(formatProvider));
        }

        protected object[] GetArgumentsToUse(IFormatProvider formatProvider)
        {
            object[] argumentsToUse = null;
            for (int i = 0; i < Arguments.Length; i++)
            {
                if (Arguments[i] is DiagnosticInfo embedded)
                {
                    argumentsToUse = InitializeArgumentListIfNeeded(argumentsToUse);
                    argumentsToUse[i] = embedded.GetMessage(formatProvider);
                    continue;
                }
            }

            return argumentsToUse ?? Arguments;
        }

        private object[] InitializeArgumentListIfNeeded(object[] argumentsToUse)
        {
            if (argumentsToUse != null)
            {
                return argumentsToUse;
            }

            object[] newArguments = new object[Arguments.Length];
            Array.Copy(Arguments, newArguments, newArguments.Length);
            return newArguments;
        }

        internal object[] Arguments { get; }

        internal CommonMessageProvider MessageProvider { get; }

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
                MessageProvider.GetMessagePrefix(MessageIdentifier, Severity, IsWarningAsError, formatProvider as CultureInfo),
                GetMessage(formatProvider));
        }

        public sealed override int GetHashCode()
        {
            int hashCode = Code;
            if (Arguments != null)
            {
                for (int i = 0; i < Arguments.Length; i++)
                {
                    hashCode = Hash.Combine(Arguments[i], hashCode);
                }
            }

            return hashCode;
        }

        public sealed override bool Equals(object obj)
        {
            DiagnosticInfo other = obj as DiagnosticInfo;
            bool result = false;
            if (other != null
                && other.Code == Code
                && GetType() == obj.GetType())
            {
                if (Arguments == null && other.Arguments == null)
                {
                    result = true;
                }
                else if (Arguments != null && other.Arguments != null && Arguments.Length == other.Arguments.Length)
                {
                    result = true;
                    for (int i = 0; i < Arguments.Length; i++)
                    {
                        if (!Equals(Arguments[i], other.Arguments[i]))
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
