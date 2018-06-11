using System;

namespace Microsoft.CodeAnalysis
{
    public abstract partial class LocalizableString : IFormattable, IEquatable<LocalizableString>
    {
        public event EventHandler<Exception> OnException;

        public string ToString(IFormatProvider formatProvider)
        {
            try
            {
                return GetText(formatProvider);
            }
            catch (Exception ex)
            {
                RaiseOnException(ex);
                return string.Empty;
            }
        }

        public static explicit operator string(LocalizableString localizableResource)
        {
            return localizableResource.ToString(null);
        }

        public static implicit operator LocalizableString(string fixedResource)
        {
            return FixedLocalizableString.Create(fixedResource);
        }

        public sealed override string ToString()
        {
            return ToString(null);
        }

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return ToString(formatProvider);
        }

        public sealed override int GetHashCode()
        {
            try
            {
                return GetHash();
            }
            catch (Exception ex)
            {
                RaiseOnException(ex);
                return 0;
            }
        }

        public sealed override bool Equals(object obj)
        {
            try
            {
                return AreEqual(obj);
            }
            catch (Exception ex)
            {
                RaiseOnException(ex);
                return false;
            }
        }

        public bool Equals(LocalizableString other)
        {
            return Equals((object) other);
        }

        protected abstract string GetText(IFormatProvider formatProvider);

        protected abstract int GetHash();

        protected abstract bool AreEqual(object other);

        private void RaiseOnException(Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                return;
            }

            try
            {
                OnException?.Invoke(this, ex);
            }
            catch
            {
            }
        }

        internal virtual bool CanThrowExceptions
        {
            get
            {
                return true;
            }
        }
    }
}
