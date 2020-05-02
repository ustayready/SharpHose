using System;
using System.Collections.Generic;

namespace SharpHose.Common.Helpers
{
    public static class PasswordHelper
    {
        public static string GetCurrentSeason()
        {
            var date = DateTime.Now;
            var value = (float)date.Month + date.Day / 100;
            if (value < 3.21 || value >= 12.22) return "Winter";
            if (value < 6.21) return "Spring";
            if (value < 9.23) return "Summer";
            return "Autumn";
        }

        public static List<string> GeneratePasswords(List<string> SeedList, int minPwdLength)
        {
            if (SeedList != null)
            {
                List<string> PasswordList = new List<string>();
                List<string> AppendList = new List<string>();

                DateTime Today = DateTime.Today;

                AppendList.Add(Today.ToString("yy"));
                AppendList.Add(Today.ToString("yy") + "!");
                AppendList.Add(Today.ToString("yyyy"));
                AppendList.Add(Today.ToString("yyyy") + "!");
                AppendList.Add("1");
                AppendList.Add("2");
                AppendList.Add("3");
                AppendList.Add("1!");
                AppendList.Add("2!");
                AppendList.Add("3!");
                AppendList.Add("123");
                AppendList.Add("1234");
                AppendList.Add("123!");
                AppendList.Add("1234!");

                foreach (string Seed in SeedList)
                {
                    foreach (string Item in AppendList)
                    {
                        string Candidate = Seed + Item;
                        if (Candidate.Length >= minPwdLength)
                        {
                            PasswordList.Add(Candidate);
                        }
                    }
                }
                return PasswordList;
            }
            else
            {
                Console.WriteLine("[-] The SeedList variable is empty; the GetSeason() function will return null.");
                return null;
            }
        }
    }
}
