using System;
using System.Collections.Generic;

namespace Trucks
{
    class SettlementHistoryComparer : IEqualityComparer<SettlementHistory>
    {
        public bool Equals(SettlementHistory s1, SettlementHistory s2)
        {
            if (s2 == null && s1 == null)
                return true;
            else if (s1 == null || s2 == null)
                return false;
            else 
                return (s1.SettlementId == s2.SettlementId);
        }

        public int GetHashCode(SettlementHistory s)
        {
            return s.SettlementId.GetHashCode();
        }
    }
}
