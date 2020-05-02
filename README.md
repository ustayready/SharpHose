# Overview

SharpHose is a C# password spraying tool designed to be fast, safe, and usable over Cobalt Strike's execute-assembly. It provides a flexible way to interact with Active Directory using domain-joined and non-joined contexts, while also being able to target specific domains and domain controllers. SharpHose takes into consideration the domain password policy, including fine grained password policies, in an attempt to avoid account lockouts. Fine grained password policies are enumerated for the users and groups that that the policy applies to. If the policy applied also to groups, the group users are captured. All enabled domain users are then classified according to their password policies, in order of precedence, and marked as safe or unsafe. The remaining users are filtered against an optional user-supplied exclude list.

Besides just spraying, red team operators can view all of the password policies for a domain, all the users affected by the policy, or just view the enabled domain users. Output can be sent directly to the console or to a user-supplied output folder.

Follow me on Twitter for some more tool releases soon!
[@ustayready](https://twitter.com/ustayready)

## Nozzles

Nozzles are built-in methods of spraying. While currently only supporting one Nozzle (LDAP), it's written in a way that makes it easily extendable.

### LDAP

Active Directory spraying nozzle using the LDAP protocol

- Asynchronous spraying for faster, but not too fast, results 

- Domain joined and non-joined spraying

- Tight integration w/ domain password policies and fine grained password policies

- Smart lockout prevention (lockoutThreshold n-1 just to be safe)

- Optionally spray to specific domains and domain controllers

- View password policies and the affected users

### Coming soon!

- MSOL

- OWA/EWS

- Lync

## Compilation

- Built using Visual Studio 2019 Community Edition

- .NET Framework 4.5

## Usage Examples

**Cobalt Strike Users**

Be sure to use the --auto to avoid the interactive prompts in SharpHose. Also, prepare your arguments locally so you can read the description before running. If you don't pass any arguments over execute-assembly, then SharpHose throws a "Missing Argument Exception" and Cobalt Strike won't return any output. You will know this is happening when you see **[-] Invoke_3 on EntryPoint failed**. This will be fixed eventually.

**Domain Joined Spray w/o Interaction**
SharpHose.exe --action SPRAY_USERS --spraypassword Spring2020! --output c:\temp\ --auto

**Domain Joined Spray w/ Exclusions**
SharpHose.exe --action SPRAY_USERS --spraypassword Spring2020! --output c:\temp\ --exclude c:\temp\exclusion_list.txt

**Non-Domain Joined Spray**
SharpHose.exe --action SPRAY_USERS --spraypassword Spring2020! --domain lab.local --username demo --password DemoThePlanet --output c:\temp\

**Domain Joined Show Policies**
Active Directory stores durations in negative large integer values which need to lapse after the last lockoutThreshold is exceeded. In future versions these will be formatted cleaner.
SharpHose.exe --action GET_POLICIES --output c:\temp\

**Domain Joined Show Policy Users**
SharpHose.exe --action GET_POLICY_USERS --policy lab --output c:\temp\

**Domain Joined Show All Users**
SharpHose.exe --action GET_ENABLED_USERS --output c:\temp\

**Domain Joined Spray Using Cobalt Strike**
execute-assembly /path/to/SharpHose.exe --action SPRAY_USERS --spraypassword Spring2020! --output c:\temp\ --auto

## Shout-Outs

- CrowdStrike Red Team Labs.. Stay tuned for new hotness! https://www.crowdstrike.com/blog/author/red-team-labs/
  *pss.. if you didn't know, CrowdStrike offers Red Team Services and the operators have some killer tradecraft :)*
