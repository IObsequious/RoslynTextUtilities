namespace Microsoft.CodeAnalysis
{
    public sealed class NoLocation : Location
    {
        public static readonly Location Singleton = new NoLocation();

        private NoLocation()
        {
        }

        public override bool Equals(object obj)
        {
            return this == obj;
        }

        public override int GetHashCode()
        {
            return 0x16487756;
        }
    }
}
