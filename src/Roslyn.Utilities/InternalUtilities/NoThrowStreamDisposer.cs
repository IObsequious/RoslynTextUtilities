using System;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    public class NoThrowStreamDisposer : IDisposable
    {
        private bool? _failed;
        private readonly string _filePath;
        private readonly DiagnosticBag _diagnostics;
        private readonly CommonMessageProvider _messageProvider;

        public Stream Stream { get; }

        public bool HasFailedToDispose
        {
            get
            {
                Debug.Assert(_failed != null);
                return _failed.GetValueOrDefault();
            }
        }

        public NoThrowStreamDisposer(
            Stream stream,
            string filePath,
            DiagnosticBag diagnostics,
            CommonMessageProvider messageProvider)
        {
            Stream = stream;
            _failed = null;
            _filePath = filePath;
            _diagnostics = diagnostics;
            _messageProvider = messageProvider;
        }

        public void Dispose()
        {
            Debug.Assert(_failed == null);
            try
            {
                Stream.Dispose();
                if (_failed == null)
                {
                    _failed = false;
                }
            }
            catch (Exception e)
            {
                _failed = true;
            }
        }
    }
}
