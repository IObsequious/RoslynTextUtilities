﻿// -----------------------------------------------------------------------
// <copyright file="SourceHashAlgorithm.cs" company="Ollon, LLC">
//     Copyright (c) 2018 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace System.Text
{
    /// <summary>
    /// Specifies a hash algorithms used for hashing source files.
    /// </summary>
    public enum SourceHashAlgorithm
    {
        /// <summary>
        /// No algorithm specified.
        /// </summary>
        None = 0,
        /// <summary>
        /// Secure Hash Algorithm 1.
        /// </summary>
        Sha1 = 1,
        /// <summary>
        /// Secure Hash Algorithm 2 with a hash size of 256 bits.
        /// </summary>
        Sha256 = 2
    }
}
