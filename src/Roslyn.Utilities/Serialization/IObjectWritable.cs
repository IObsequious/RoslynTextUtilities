﻿namespace Roslyn.Utilities
{
    public interface IObjectWritable
    {
        void WriteTo(ObjectWriter writer);
    }
}