using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GodotCompletionProviders;
using GodotTools.IdeMessaging;
using GodotTools.IdeMessaging.Requests;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ILogger = GodotCompletionProviders.ILogger;
using Task = System.Threading.Tasks.Task;

namespace GodotAddinVS
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideProjectFactory(typeof(GodotFlavoredProjectFactory), "Godot.Project", null, "csproj", "csproj", null,
        LanguageVsTemplate = "CSharp", TemplateGroupIDsVsTemplate = "Godot")]
    [ProvideOptionPage(typeof(GeneralOptionsPage),
        "Godot", "General", 0, 0, true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class GodotPackage : AsyncPackage
    {
        /// <summary>
        /// GodotPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "fbf828da-088b-482a-a550-befaed4b5d25";

        public const string GodotProjectGuid = "8F3E2DF0-C35C-4265-82FC-BEA011F4A7ED";

        #region Package Members

        public static GodotPackage Instance { get; private set; }

        public GodotPackage()
        {
            Instance = this;
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Commands
            await Commands.CommandRunGodot.InitializeAsync(this);
            await Commands.CommandResetGodot.InitializeAsync(this);

            RegisterProjectFactory(new GodotFlavoredProjectFactory(this));

            GodotSolutionEventsListener = new GodotSolutionEventsListener(this);

            var completionProviderContext = new GodotVsProviderContext(this);
            BaseCompletionProvider.Context = completionProviderContext;
        }

        internal GodotSolutionEventsListener GodotSolutionEventsListener { get; private set; }

        public GodotVSLogger Logger { get; } = new GodotVSLogger();

        public async Task ShowErrorMessageBoxAsync(string title, string message)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            // ReSharper disable once SuspiciousTypeConversion.Global
            var uiShell = (IVsUIShell)await GetServiceAsync(typeof(SVsUIShell));

            if (uiShell == null)
                throw new ServiceUnavailableException(typeof(SVsUIShell));

            var clsid = Guid.Empty;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                0,
                ref clsid,
                title,
                message,
                string.Empty,
                0,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                0,
                pnResult: out _));
        }

        #endregion
    }
}
