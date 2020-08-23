using System.Collections.Generic;
using Discord;

namespace GreyOTron.Library.Comparers
{
    public class RoleEqualityComparer : IEqualityComparer<IRole>
    {
        public bool Equals(IRole x, IRole y)
        {
            if (x == null || y == null) { return false; }
            return x.Id.Equals(y.Id);
        }

        public int GetHashCode(IRole obj)
        {
            return 0;
        }
    }
}
