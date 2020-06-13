using System;

namespace GodotAddinVS.Debugging
{
    public class GodotDebugTarget
    {
        private const string DebugTargetsGuidStr = "4E50788E-B023-4F77-AFE9-797603876907";
        public static readonly Guid DebugTargetsGuid = new Guid(DebugTargetsGuidStr);

        public Guid Guid { get; }

        public uint Id { get; }

        public string Name { get; }

        public ExecutionType ExecutionType { get; }

        public GodotDebugTarget(ExecutionType executionType, string name)
        {
            Guid = DebugTargetsGuid;
            Id = 0x8192 + (uint) executionType;
            ExecutionType = executionType;
            Name = name;
        }
    }

    public enum ExecutionType : uint
    {
        PlayInEditor = 0,
        Launch,
        Attach
    }
}
