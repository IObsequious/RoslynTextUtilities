using System;
using System.Globalization;
using System.Linq;
using System.Resources;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public sealed class LocalizableResourceString : LocalizableString, IObjectWritable
    {
        private readonly string _nameOfLocalizableResource;
        private readonly ResourceManager _resourceManager;
        private readonly Type _resourceSource;
        private readonly string[] _formatArguments;

        static LocalizableResourceString()
        {
            ObjectBinder.RegisterTypeReader(typeof(LocalizableResourceString), reader => new LocalizableResourceString(reader));
        }

        public LocalizableResourceString(string nameOfLocalizableResource, ResourceManager resourceManager, Type resourceSource)
            : this(nameOfLocalizableResource, resourceManager, resourceSource, Array.Empty<string>())
        {
        }

        public LocalizableResourceString(string nameOfLocalizableResource,
            ResourceManager resourceManager,
            Type resourceSource,
            params string[] formatArguments)
        {
            if (nameOfLocalizableResource == null)
            {
                throw new ArgumentNullException(nameof(nameOfLocalizableResource));
            }

            if (resourceManager == null)
            {
                throw new ArgumentNullException(nameof(resourceManager));
            }

            if (resourceSource == null)
            {
                throw new ArgumentNullException(nameof(resourceSource));
            }

            if (formatArguments == null)
            {
                throw new ArgumentNullException(nameof(formatArguments));
            }

            _resourceManager = resourceManager;
            _nameOfLocalizableResource = nameOfLocalizableResource;
            _resourceSource = resourceSource;
            _formatArguments = formatArguments;
        }

        private LocalizableResourceString(ObjectReader reader)
        {
            _resourceSource = reader.ReadType();
            _nameOfLocalizableResource = reader.ReadString();
            _resourceManager = new ResourceManager(_resourceSource);
            int length = reader.ReadInt32();
            if (length == 0)
            {
                _formatArguments = Array.Empty<string>();
            }
            else
            {
                ArrayBuilder<string> argumentsBuilder = ArrayBuilder<string>.GetInstance(length);
                for (int i = 0; i < length; i++)
                {
                    argumentsBuilder.Add(reader.ReadString());
                }

                _formatArguments = argumentsBuilder.ToArrayAndFree();
            }
        }

        bool IObjectWritable.ShouldReuseInSerialization => false;

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            writer.WriteType(_resourceSource);
            writer.WriteString(_nameOfLocalizableResource);
            int length = _formatArguments.Length;
            writer.WriteInt32(length);
            for (int i = 0; i < length; i++)
            {
                writer.WriteString(_formatArguments[i]);
            }
        }

        protected override string GetText(IFormatProvider formatProvider)
        {
            CultureInfo culture = formatProvider as CultureInfo ?? CultureInfo.CurrentUICulture;
            string resourceString = _resourceManager.GetString(_nameOfLocalizableResource, culture);
            return resourceString != null ?
                (_formatArguments.Length > 0 ? string.Format(resourceString, _formatArguments) : resourceString) :
                string.Empty;
        }

        protected override bool AreEqual(object other)
        {
            LocalizableResourceString otherResourceString = other as LocalizableResourceString;
            return otherResourceString != null &&
                   _nameOfLocalizableResource == otherResourceString._nameOfLocalizableResource &&
                   _resourceManager == otherResourceString._resourceManager &&
                   _resourceSource == otherResourceString._resourceSource &&
                   _formatArguments.SequenceEqual(otherResourceString._formatArguments, (a, b) => a == b);
        }

        protected override int GetHash()
        {
            return Hash.Combine(_nameOfLocalizableResource.GetHashCode(),
                Hash.Combine(_resourceManager.GetHashCode(),
                    Hash.Combine(_resourceSource.GetHashCode(),
                        Hash.CombineValues(_formatArguments))));
        }
    }
}
