// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents parse options common to C# and VB.
    /// </summary>
    public abstract class ParseOptions
    {
        private readonly Lazy<ImmutableArray<Diagnostic>> _lazyErrors;

        internal ParseOptions()
        {
            _lazyErrors = new Lazy<ImmutableArray<Diagnostic>>(() =>
            {
                var builder = ArrayBuilder<Diagnostic>.GetInstance();
                ValidateOptions(builder);
                return builder.ToImmutableAndFree();
            });
        }

        /// <summary>
        /// Gets the source language ("C#" or "Visual Basic").
        /// </summary>
        public abstract string Language { get; }

        /// <summary>
        /// Errors collection related to an incompatible set of parse options
        /// </summary>
        public ImmutableArray<Diagnostic> Errors
        {
            get { return _lazyErrors.Value; }
        }

        /// <summary>
        /// Performs validation of options compatibilities and generates diagnostics if needed
        /// </summary>
        internal abstract void ValidateOptions(ArrayBuilder<Diagnostic> builder);

        /// <summary>
        /// Enable some experimental language features for testing.
        /// </summary>
        public ParseOptions WithFeatures(IEnumerable<KeyValuePair<string, string>> features)
        {
            return CommonWithFeatures(features);
        }

        protected abstract ParseOptions CommonWithFeatures(IEnumerable<KeyValuePair<string, string>> features);

        /// <summary>
        /// Returns the experimental features.
        /// </summary>
        public abstract IReadOnlyDictionary<string, string> Features
        {
            get;
        }

        /// <summary>
        /// Names of defined preprocessor symbols.
        /// </summary>
        public abstract IEnumerable<string> PreprocessorSymbolNames { get; }

        public abstract override bool Equals(object obj);

        protected bool EqualsHelper(ParseOptions other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return
                Features.SequenceEqual(other.Features) &&
                (PreprocessorSymbolNames == null ? other.PreprocessorSymbolNames == null : PreprocessorSymbolNames.SequenceEqual(other.PreprocessorSymbolNames, StringComparer.Ordinal));
        }

        public abstract override int GetHashCode();

        protected int GetHashCodeHelper()
        {
            return
                Hash.Combine(HashFeatures(Features),
                Hash.Combine(Hash.CombineValues(PreprocessorSymbolNames, StringComparer.Ordinal), 0));
        }

        private static int HashFeatures(IReadOnlyDictionary<string, string> features)
        {
            int value = 0;
            foreach (var kv in features)
            {
                value = Hash.Combine(kv.Key.GetHashCode(),
                        Hash.Combine(kv.Value.GetHashCode(), value));
            }

            return value;
        }

        public static bool operator ==(ParseOptions left, ParseOptions right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ParseOptions left, ParseOptions right)
        {
            return !Equals(left, right);
        }
    }
}
