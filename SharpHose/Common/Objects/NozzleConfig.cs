using SharpHose.Common.Helpers;
using SharpHose.Common.Interfaces;

namespace SharpHose.Common.Objects
{
    public abstract class NozzleConfig : INozzleConfig
    {
        public virtual BaseLoggerHelper Logger { get; set; }
        public NozzleConfig() { Logger = new ConsoleLogger(false); }
    }
}
