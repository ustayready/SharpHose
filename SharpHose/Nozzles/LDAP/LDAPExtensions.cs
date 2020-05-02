using SharpHose.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpHose.Nozzles.LDAP
{
    public static class LDAPExtensions
    {
        public static LDAPPasswordPolicy GetUserPolicy(this LDAPUserInfo user, List<LDAPPasswordPolicy> Policies)
        {
            var policy = Policies
                .Where(x => x.IsFineGrained && x.AppliesToUsers.Contains(user.Username, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(y => y.PasswordPrecendence);

            if (policy.Count() > 0)
            {
                return policy.First();
            }
            else
            {
                return Policies.First(x => !x.IsFineGrained);
            }
        }


        public static LDAPUserInfo ClassifyUser(this LDAPUserInfo user, LDAPPasswordPolicy policy)
        {
            user.PolicyName = policy.Name;

            var now = DateTime.Now;
            var start = new DateTime(1900, 01, 01);
            var badPasswordCount = user.BadPasswordCount;
            var lockoutDurationTime = user.LockoutTime.AddTicks((policy.LockoutDuration * -1));
            var observationTime = user.BadPasswordTime.AddTicks((policy.LockoutObservationWindow * -1));

            if ((badPasswordCount == policy.LockoutThreshold) && (observationTime > now))
            {
                user.UserState = UserState.LOCKED_OUT;
            }
            else if (badPasswordCount == (policy.LockoutThreshold - 1))
            {
                user.UserState = UserState.PENDING_LOCK_OUT;
            }

            if (observationTime < now)
            {
                user.UserState = UserState.SAFE_TO_SPRAY;
                var diff = (policy.LockoutThreshold - 1);
            }
            else if ((badPasswordCount < (policy.LockoutThreshold - 1)) && (observationTime > now))
            {
                user.UserState = UserState.SAFE_TO_SPRAY;
                var diff = (policy.LockoutThreshold - 1) - badPasswordCount;
            }

            if (lockoutDurationTime < start)
            {
                // Never locked out
            }
            if ((lockoutDurationTime > start) && (observationTime < now))
            {
                // Was locked out
            }
            if ((badPasswordCount == (policy.LockoutThreshold - 1)) && (lockoutDurationTime < start) && (observationTime < now))
            {
                // Almost locked out
            }
            if ((badPasswordCount > 0) && (badPasswordCount < (policy.LockoutThreshold - 1)) && (observationTime < now))
            {
                // Prior failed attempts
            }
            return user;
        }
    }
}
