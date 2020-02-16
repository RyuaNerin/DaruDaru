using System;
using System.Collections.Generic;

namespace DaruDaru.Utilities
{
    internal class UriIEqualityComparer : IEqualityComparer<Uri>
    {
        public bool Equals(Uri x, Uri y) => x.ToString().Equals(y.ToString(), StringComparison.Ordinal);

        public int GetHashCode(Uri obj) => obj.GetHashCode();
    }
}
