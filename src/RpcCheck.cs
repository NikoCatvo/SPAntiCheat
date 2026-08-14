using Hazel;
using System;

namespace NitroShield
{
    internal class RpcCheck
    {
        public virtual string Name { get; set; }
        public virtual string DisplayName { get; set; }
        public virtual bool Enabled { get; set; } = true;
        public virtual void Validate(PlayerControl player, MessageReader reader, ref bool blockRpc) { }
        public virtual bool IsHostOnly() => false;
        public virtual Type GetExpectedNetObject() => typeof(PlayerControl);
    }
}
