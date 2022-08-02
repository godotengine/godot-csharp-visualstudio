using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Task = System.Threading.Tasks.Task;

namespace GodotAddinVS.Commands {
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CommandRunGodot {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 256;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("d71528ca-92b8-49bb-8655-8b478b495499");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandRunGodot"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private CommandRunGodot(AsyncPackage package, OleMenuCommandService commandService) {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandId);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CommandRunGodot Instance {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider {
            get {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package) {
            // Switch to the main thread - the call to AddCommand in CommandRunGodot's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new CommandRunGodot(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e) {
            ThreadHelper.ThrowIfNotOnUIThread();

            string goDotPath;
            string goDotExecutable;

            var settingsManager = new ShellSettingsManager(package);
            var config = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!config.CollectionExists("External Tools")) {
                config.CreateCollection("External Tools");
            }

            if (!config.PropertyExists("External Tools", "GoDotExecutable")) {
                var ofd = new OpenFileDialog {
                    Filter = @"GoDot executable (.exe)|*.exe"
                };
                var result = ofd.ShowDialog(null);

                if (result == DialogResult.OK) {
                    goDotExecutable = ofd.FileName;
                    goDotPath = Path.GetDirectoryName(goDotExecutable);

                    config.SetString("External Tools", "GoDotExecutable", goDotExecutable);
                    config.SetString("External Tools", "GoDotPath", goDotPath);
                } else return;

            } else {
                goDotPath = config.GetString("External Tools", "GoDotPath");
                goDotExecutable = config.GetString("External Tools", "GoDotExecutable");
            }

            if (string.IsNullOrEmpty(goDotExecutable)) {
                MessageBox.Show("GoDot", "GoDot path not set");
                return;
            }

            if (!File.Exists(goDotExecutable)) {
                MessageBox.Show("GoDot", @$"GoDot does not exist at {goDotExecutable}");
                return;
            }

            Process.Start(goDotExecutable);
        }
    }
}
