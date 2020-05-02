using SharpHose.Common.Enums;

namespace SharpHose.Common.Objects
{
    public class UserInfo
    {
        public virtual string Username { get; set; }
        public virtual UserState UserState { get; set; }
    }
}
