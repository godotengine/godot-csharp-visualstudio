using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace GodotAddinVS
{
    public class GeneralOptionsPage : DialogPage
    {
        [Category("Debugging")]
        [DisplayName("Always Use Configured Executable")]
        [Description("When disabled, Visual Studio will attempt to get the Godot executable path from a running Godot editor instance")]
        public bool AlwaysUseConfiguredExecutable { get; set; } = false;

        [Category("Debugging")]
        [DisplayName("Godot Executable Path")]
        [Description("Path to the Godot executable to use when launching the application for debugging")]
        public string GodotExecutablePath { get; set; } = "";

        [Category("Debugging")]
        [DisplayName("Debugger Listen Timeout")]
        [Description("Time in milliseconds after which the debugging session will end if no debugger is connected")]
        public int DebuggerListenTimeout { get; set; } = 10000;

        [Category("Code Completion")]
        [DisplayName("Provide Node Path Completions")]
        [Description("Whether to provide code completion for node paths when a Godot editor is connected")]
        public bool ProvideNodePathCompletions { get; set; } = true;

        [Category("Code Completion")]
        [DisplayName("Provide Input Action Completions")]
        [Description("Whether to provide code completion for input actions when a Godot editor is connected")]
        public bool ProvideInputActionCompletions { get; set; } = true;

        [Category("Code Completion")]
        [DisplayName("Provide Resource Path Completions")]
        [Description("Whether to provide code completion for resource paths when a Godot editor is connected")]
        public bool ProvideResourcePathCompletions { get; set; } = true;

        [Category("Code Completion")]
        [DisplayName("Provide Scene Path Completions")]
        [Description("Whether to provide code completion for scene paths when a Godot editor is connected")]
        public bool ProvideScenePathCompletions { get; set; } = true;

        [Category("Code Completion")]
        [DisplayName("Provide Signal Name Completions")]
        [Description("Whether to provide code completion for signal names when a Godot editor is connected")]
        public bool ProvideSignalNameCompletions { get; set; } = true;
    }
}
