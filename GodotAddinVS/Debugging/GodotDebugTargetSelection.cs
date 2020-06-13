using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shell.Interop;

namespace GodotAddinVS.Debugging
{
    public class GodotDebugTargetSelection : IVsProjectCfgDebugTargetSelection
    {
        private static readonly List<GodotDebugTarget> DebugTargets = new List<GodotDebugTarget>()
        {
            new GodotDebugTarget(ExecutionType.PlayInEditor, "Play in Editor"),
            new GodotDebugTarget(ExecutionType.Launch, "Launch"),
            new GodotDebugTarget(ExecutionType.Attach, "Attach")
        };

        public static readonly GodotDebugTargetSelection Instance = new GodotDebugTargetSelection();

        private IVsDebugTargetSelectionService _debugTargetSelectionService;

        public GodotDebugTarget CurrentDebugTarget { get; private set; } = DebugTargets.First();

        public void GetCurrentDebugTarget(out Guid pguidDebugTargetType,
            out uint pDebugTargetTypeId, out string pbstrCurrentDebugTarget)
        {
            pguidDebugTargetType = CurrentDebugTarget.Guid;
            pDebugTargetTypeId = CurrentDebugTarget.Id;
            pbstrCurrentDebugTarget = CurrentDebugTarget.Name;
        }

        public Array GetDebugTargetListOfType(Guid guidDebugTargetType, uint debugTargetTypeId)
        {
            return DebugTargets
                .Where(t => t.Guid == guidDebugTargetType && t.Id == debugTargetTypeId)
                .Select(t => t.Name).ToArray();
        }

        public bool HasDebugTargets(
            IVsDebugTargetSelectionService pDebugTargetSelectionService, out Array pbstrSupportedTargetCommandIDs)
        {
            _debugTargetSelectionService = pDebugTargetSelectionService;
            pbstrSupportedTargetCommandIDs = DebugTargets
                .Select(t => $"{t.Guid}:{t.Id}").ToArray();
            return true;
        }

        public void SetCurrentDebugTarget(Guid guidDebugTargetType,
            uint debugTargetTypeId, string bstrCurrentDebugTarget)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            CurrentDebugTarget = DebugTargets
                .First(t => t.Guid == guidDebugTargetType && t.Id == debugTargetTypeId);
            _debugTargetSelectionService?.UpdateDebugTargets();
        }
    }
}
