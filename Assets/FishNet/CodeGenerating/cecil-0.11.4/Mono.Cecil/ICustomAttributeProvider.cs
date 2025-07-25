//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using MonoFN.Collections.Generic;
using System.Threading;

namespace MonoFN.Cecil
{
    public interface ICustomAttributeProvider : IMetadataTokenProvider
    {
        Collection<CustomAttribute> CustomAttributes { get; }
        bool HasCustomAttributes { get; }
    }

    internal static partial class Mixin
    {
        public static bool GetHasCustomAttributes(this ICustomAttributeProvider self, ModuleDefinition module)
        {
            return module.HasImage() && module.Read(self, (provider, reader) => reader.HasCustomAttributes(provider));
        }

        public static Collection<CustomAttribute> GetCustomAttributes(this ICustomAttributeProvider self, ref Collection<CustomAttribute> variable, ModuleDefinition module)
        {
            if (module.HasImage())
                return module.Read(ref variable, self, (provider, reader) => reader.ReadCustomAttributes(provider));

            Interlocked.CompareExchange(ref variable, new(), null);
            return variable;
        }
    }
}