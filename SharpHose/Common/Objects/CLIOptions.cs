using CommandLine;
using SharpHose.Common.Enums;
using SharpHose.Nozzles.LDAP;

namespace SharpHose.Common.Objects
{
    public class CLIOptions
    {
        [Option("action", Required = true,
            HelpText = "Select what you would like to do")]
        public LDAPAction Action { get; set; }

        [Option("nozzle", Default = NozzleType.LDAP,
            HelpText = "The nozzle you would like to use for spraying.")]
        public NozzleType Nozzle { get; set; }

        [Option("auto", Default = false,
            HelpText = "Automatically start spraying without user interaction.")]
        public bool Auto { get; set; }

        [Option("domain", Required = false,
            HelpText = "The domain name you would like to spray. If no domain is supplied, assumes domain joined context.")]
        public string DomainName { get; set; }

        [Option("username", Required = false,
            HelpText = "The AD username for authentication. If no username/password is supplied, assumes domain joined context.")]
        public string DomainUsername { get; set; }

        [Option("password", Required = false,
            HelpText = "The AD password for authentication. If no username/password is supplied, assumes domain joined context.")]
        public string DomainPassword { get; set; }

        [Option("controller", Required = false,
            HelpText = "Optional domain controller to spray against.")]
        public string DomainController { get; set; }

        [Option("spraypassword", Required = false,
            HelpText = "The password to spray.")]
        public string SprayPassword { get; set; }

        [Option("policy",
            HelpText = "Policy name used to gather the enabled users it applies to, do NOT spray.")]
        public string PolicyName { get; set; }

        [Option("exclude", Required = false,
            HelpText = "The file path of usernames to EXCLUDE from the spray, single sAMAccountName per line. (automatically loads from AD otherwise)")]
        public string ExcludeFilePath { get; set; }

        [Option("quiet", Default = false,
            HelpText = "Silently runs and generates output without messages.")]
        public bool Quiet { get; set; }

        [Option("output", Required = false,
            HelpText = "The directory path for saving the results. If not provided, only console results are displayed unless --quiet is supplied.")]
        public string OutputPath { get; set; }
    }
}
