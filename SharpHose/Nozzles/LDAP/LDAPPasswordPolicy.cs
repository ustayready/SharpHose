using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;

namespace SharpHose.Nozzles.LDAP
{
    public class LDAPPasswordPolicy
    {
        public int LockoutThreshold { get; set; }
        public long LockoutDuration { get; set; }
        public long LockoutObservationWindow { get; set; }
        public int MinimumPasswordLength { get; set; }
        public long MinimumPasswordAge { get; set; }
        public long MaximumPasswordAge { get; set; }
        public bool PossiblyCustomized { get; set; }
        public string Name { get; set; }
        public bool IsFineGrained { get; set; }
        public int PasswordHistoryLength { get; set; }
        public List<string> AppliesToDN { get; set; }
        public int PasswordPrecendence { get; set; }
        public bool ComplexityEnabled { get; set; }
        public bool ReversibleEncryptionEnabled { get; set; }
        public string ADSPath { get; set; }
        public List<string> AppliesToUsers { get; set; }

        public LDAPPasswordPolicy(SearchResult result, bool isFineGrained)
        {
            AppliesToDN = new List<string>();
            AppliesToUsers = new List<string>();
            IsFineGrained = isFineGrained;

            if (isFineGrained)
            {
                LoadFineGrainedPolicy(result);
            }
            else
            {
                LoadDomainPolicy(result);
            }
        }

        private void LoadFineGrainedPolicy(SearchResult result)
        {
            foreach (DictionaryEntry prop in result.Properties)
            {
                var property = (string)prop.Key;
                var value = (ResultPropertyValueCollection)prop.Value;

                if (property == "msds-psoappliesto")
                {
                    foreach (string applies in (ResultPropertyValueCollection)value)
                    {
                        AppliesToDN.Add(applies);
                    }
                }
                else if (new string[] { "name", "adspath" }.Any(x => x == property))
                {
                    switch (property)
                    {
                        case "name":
                            Name = (string)value[0];
                            break;
                        case "adspath":
                            ADSPath = (string)value[0];
                            break;
                    }
                }
                else if (new string[] { "msds-passwordcomplexityenabled", "msds-passwordreversibleencryptionenabled" }.Any(x => x == property))
                {
                    bool placeHolder = (bool)value[0];
                    switch (property)
                    {
                        case "msds-passwordcomplexityenabled":
                            ComplexityEnabled = placeHolder;
                            break;
                        case "msds-passwordreversibleencryptionenabled":
                            ReversibleEncryptionEnabled = placeHolder;
                            break;
                    }
                }
                else if (new string[] { "msds-lockoutobservationwindow", "msds-lockoutduration", "msds-minimumpasswordage", "msds-maximumpasswordage" }.Any(x => x == property))
                {
                    long placeHolder = (long)value[0];
                    switch (property)
                    {
                        case "msds-lockoutobservationwindow":
                            LockoutObservationWindow = placeHolder;
                            break;
                        case "msds-lockoutduration":
                            LockoutDuration = placeHolder;
                            break;
                        case "msds-minimumpasswordage":
                            MinimumPasswordAge = placeHolder;
                            break;
                        case "msds-maximumpasswordage":
                            MaximumPasswordAge = placeHolder;
                            break;
                    }
                }
                else
                {
                    int placeHolder;
                    int.TryParse(value[0].ToString(), out placeHolder);

                    switch (property)
                    {
                        case "msds-lockoutthreshold":
                            LockoutThreshold = placeHolder;
                            break;
                        case "msds-minimumpasswordlength":
                            MinimumPasswordLength = placeHolder;
                            break;
                        case "msds-passwordhistorylength":
                            PasswordHistoryLength = placeHolder;
                            break;
                        case "msds-passwordsettingsprecedence":
                            PasswordPrecendence = placeHolder;
                            break;
                    }
                }
            }
        }

        public void LoadDomainPolicy(SearchResult result)
        {
            PasswordPrecendence = 0;

            foreach (DictionaryEntry prop in result.Properties)
            {
                var property = (string)prop.Key;
                var value = (ResultPropertyValueCollection)prop.Value;

                if (new string[] { "name", "adspath" }.Any(x => x == property))
                {
                    switch (property)
                    {
                        case "name":
                            Name = (string)value[0];
                            break;
                        case "adspath":
                            ADSPath = (string)value[0];
                            break;
                    }
                }
                else if (new string[] { "lockoutobservationwindow", "lockoutduration", "minpwdage", "maxpwdage" }.Any(x => x == property))
                {
                    long placeHolder = (long)value[0];
                    switch (property)
                    {
                        case "lockoutobservationwindow":
                            LockoutObservationWindow = placeHolder;
                            break;
                        case "lockoutduration":
                            LockoutDuration = placeHolder;
                            break;
                        case "minpwdage":
                            MinimumPasswordAge = placeHolder;
                            break;
                        case "maxpwdage":
                            MaximumPasswordAge = placeHolder;
                            break;
                    }
                }
                else
                {
                    int placeHolder;
                    int.TryParse(value[0].ToString(), out placeHolder);

                    switch (property)
                    {
                        case "lockoutthreshold":
                            LockoutThreshold = placeHolder;
                            break;
                        case "minpwdlength":
                            MinimumPasswordLength = placeHolder;
                            break;
                        case "pwdhistorylength":
                            PasswordHistoryLength = placeHolder;
                            break;
                        case "passwordsettingsprecedence":
                            PasswordPrecendence = placeHolder;
                            break;
                        case "msds-behavior-version":
                            PossiblyCustomized = placeHolder >= 3 ? true : false; ;
                            break;
                    }
                }
            }
        }
    }
}
