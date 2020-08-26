using SharpHose.Common.Enums;
using SharpHose.Common.Objects;
using System;
using System.DirectoryServices;

namespace SharpHose.Nozzles.LDAP
{
    public class LDAPUserInfo : UserInfo
    {
        public override string Username { get; set; }
        public override UserState UserState { get; set; }

        public int BadPasswordCount { get; set; }
        public DateTime BadPasswordTime { get; set; }
        public DateTime LockoutTime { get; set; }
        public int LockoutDuration { get; set; }
        public DateTime PasswordLastSet { get; set; }
        public string PolicyName { get; set; }

        public LDAPUserInfo(SearchResult result)
        {
            UserState = UserState.NOT_YET_KNOWN;
            
            Username = result.Properties["sAMAccountname"][0].ToString().ToLower();

            int badPwdCount = 0;
            if (result.Properties.Contains("badPwdCount"))
                int.TryParse(result.Properties["badPwdCount"][0].ToString(), out badPwdCount);
            BadPasswordCount = badPwdCount;

            long badPasswordTime = 0;
            if (result.Properties.Contains("badPasswordTime"))
                long.TryParse(result.Properties["badPasswordTime"][0].ToString(), out badPasswordTime);

            try
            {
                if(badPasswordTime != -1)
                    BadPasswordTime = DateTime.FromFileTime(badPasswordTime);
            } catch
            {
                throw new Exception($"Bad password time: {badPasswordTime} for {Username}");
            }

            long lockoutTime = 0;
            if (result.Properties.Contains("lockoutTime"))
                long.TryParse(result.Properties["lockoutTime"][0].ToString(), out lockoutTime);

            try
            {
                if(lockoutTime != -1)
                    LockoutTime = DateTime.FromFileTime(lockoutTime);
            }
            catch
            {
                throw new Exception($"Bad lockout time: {lockoutTime} for {Username}");
            }
            

            int lockoutDuration = 0;
            if (result.Properties.Contains("lockoutDuration"))
                int.TryParse(result.Properties["lockoutDuration"][0].ToString(), out lockoutDuration);
            LockoutDuration = lockoutDuration;

            long pwdLastSet = 0;
            if (result.Properties.Contains("pwdLastSet"))
                long.TryParse(result.Properties["pwdLastSet"][0].ToString(), out pwdLastSet);

            try
            {
                if (pwdLastSet != -1)
                    PasswordLastSet = DateTime.FromFileTime(pwdLastSet);
            }
            catch
            {
                throw new Exception($"Bad password last set time: {pwdLastSet} for {Username}");
            }
            
        }
    }
}
