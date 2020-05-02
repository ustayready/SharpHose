using SharpHose.Common.Enums;
using SharpHose.Common.Objects;
using System.Collections.Generic;

namespace SharpHose.Common.Interfaces
{
    public interface INozzle
    {
        string Name { get; }
        double Version { get; }

        List<UserInfo> Users { get; }
        SprayState CurrentState { get; }

        void Start();
        void Stop();
        void Pause();
    }
}
