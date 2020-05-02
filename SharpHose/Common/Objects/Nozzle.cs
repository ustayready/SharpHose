using SharpHose.Common.Enums;
using SharpHose.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace SharpHose.Common.Objects
{
    public abstract class Nozzle : INozzle
    {
        public virtual string Name { get; }
        public virtual double Version { get; }
        public virtual SprayState CurrentState { get; set; }
        public virtual List<UserInfo> Users { get; set; }

        public virtual void Start() { throw new NotImplementedException(); }
        public virtual void Stop() { throw new NotImplementedException(); }
        public virtual void Pause() { throw new NotImplementedException(); }

        public Nozzle() { Users = new List<UserInfo>(); }
    }
}
