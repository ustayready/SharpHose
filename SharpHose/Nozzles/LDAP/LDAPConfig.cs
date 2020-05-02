using SharpHose.Common.Enums;
using SharpHose.Common.Objects;
using System.Collections.Generic;

namespace SharpHose.Nozzles.LDAP
{
    public class LDAPConfig : NozzleConfig
    {
        public string DomainController { get; set; }
        public string DomainName { get; set; }
        public string DomainUsername { get; set; }
        public string DomainPassword { get; set; }
        public string SprayPassword { get; set; }
        public string FilterLDAP { get; set; }
        public bool Auto { get; set; }
        public string OutputPath { get; set; }
        public bool SaveOutput { get; set; }
        public string ExcludeFilePath { get; set; }
        public bool ExcludeUsers { get; set; }
        public LDAPConfig()
        {
        }
    }
}
