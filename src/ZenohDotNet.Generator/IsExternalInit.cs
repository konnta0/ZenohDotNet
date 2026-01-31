// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file enables record types in .NET Standard 2.0 projects
// by providing the IsExternalInit type that the compiler requires.

#if NETSTANDARD2_0

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

#endif
