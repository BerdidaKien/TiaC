using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.ServiceModels
{
    public static class PenaltyRules
    {
        public const decimal LateFeePerDay = 20m;
        public const int SettleLateWithinDays = 3;      // late fee deadline; also "not returned" limit
        public const int ReplaceWithinDays = 14;        // damage deadline

        public const string Late = "late";
        public const string Damage = "damage";

        public const string Unresolved = "unresolved";
        public const string Resolved = "resolved";
        public const string Waived = "waived";

        public const string Restricted = "restricted";
        public const string Eligible = "eligible";

        // Return conditions that count as damaged
        public static readonly string[] DamageConditions = { "needs_maintenance" };

        public static bool IsDamage(string condition) =>
            condition != null && DamageConditions.Contains(condition);

        // Every started day counts
        public static int LateDays(DateTime due, DateTime end)
        {
            var late = end - due;
            return late <= TimeSpan.Zero ? 0 : (int)Math.Ceiling(late.TotalDays);
        }
    }
}