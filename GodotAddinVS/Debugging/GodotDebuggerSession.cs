using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GodotTools.IdeMessaging;
using GodotTools.IdeMessaging.Requests;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Mono.Debugging.Client;
using Mono.Debugging.Soft;

namespace GodotAddinVS.Debugging
{
    internal class GodotDebuggerSession : SoftDebuggerSession
    {
        private bool _attached;
        private NetworkStream _godotRemoteDebuggerStream;
        private Process _process;

        // TODO: Unused. Find a way to trigger this.
        public void SendReloadScripts()
        {
            var executionType = GodotDebugTargetSelection.Instance.CurrentDebugTarget.ExecutionType;

            switch (executionType)
            {
                case ExecutionType.Launch:
                    GodotVariantEncoder.Encode(
                        new List<GodotVariant> {"reload_scripts"},
                        _godotRemoteDebuggerStream
                    );
                    _godotRemoteDebuggerStream.Flush();
                    break;
                case ExecutionType.PlayInEditor:
                case ExecutionType.Attach:
                    var godotMessagingClient =
                        GodotPackage.Instance.GodotSolutionEventsListener?.GodotMessagingClient;
                    godotMessagingClient?.SendRequest<ReloadScriptsResponse>(new ReloadScriptsRequest());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(executionType.ToString());
            }
        }

        private string GetGodotExecutablePath()
        {
            var options = (GeneralOptionsPage)GodotPackage.Instance.GetDialogPage(typeof(GeneralOptionsPage));

            if (options.AlwaysUseConfiguredExecutable)
                return options.GodotExecutablePath;

            var godotMessagingClient = GodotPackage.Instance.GodotSolutionEventsListener?.GodotMessagingClient;

            string godotPath = godotMessagingClient?.GodotEditorExecutablePath;

            if (!string.IsNullOrEmpty(godotPath) && File.Exists(godotPath))
            {
                // If the setting is not yet assigned any value, set it to the currently connected Godot editor path
                if (string.IsNullOrEmpty(options.GodotExecutablePath))
                    options.GodotExecutablePath = godotPath;
                return godotPath;
            }

            return options.GodotExecutablePath;
        }

        private void EndSessionWithError(string title, string errorMessage)
        {
            _ = GodotPackage.Instance.ShowErrorMessageBoxAsync(title, errorMessage);
            EndSession();
        }

        protected override void OnRun(DebuggerStartInfo startInfo)
        {
            var godotStartInfo = (GodotStartInfo)startInfo;

            var executionType = GodotDebugTargetSelection.Instance.CurrentDebugTarget.ExecutionType;

            switch (executionType)
            {
                case ExecutionType.PlayInEditor:
                {
                    _attached = false;
                    StartListening(godotStartInfo, out var assignedDebugPort);

                    var godotMessagingClient =
                        GodotPackage.Instance.GodotSolutionEventsListener?.GodotMessagingClient;

                    if (godotMessagingClient == null || !godotMessagingClient.IsConnected)
                    {
                        EndSessionWithError("Play Error", "No Godot editor instance connected");
                        return;
                    }

                    const string host = "127.0.0.1";

                    var playRequest = new DebugPlayRequest
                    {
                        DebuggerHost = host,
                        DebuggerPort = assignedDebugPort,
                        BuildBeforePlaying = false
                    };

                    _ = godotMessagingClient.SendRequest<DebugPlayResponse>(playRequest)
                        .ContinueWith(t =>
                        {
                            if (t.Result.Status != MessageStatus.Ok)
                                EndSessionWithError("Play Error", $"Received Play response with status: {MessageStatus.Ok}");
                        }, TaskScheduler.Default);

                    // TODO: Read the editor player stdout and stderr somehow

                    break;
                }
                case ExecutionType.Launch:
                {
                    _attached = false;
                    StartListening(godotStartInfo, out var assignedDebugPort);

                    // Listener to replace the Godot editor remote debugger.
                    // We use it to notify the game when assemblies should be reloaded.
                    var remoteDebugListener = new TcpListener(IPAddress.Any, 0);
                    remoteDebugListener.Start();
                    _ = remoteDebugListener.AcceptTcpClientAsync()
                        .ContinueWith(OnGodotRemoteDebuggerConnectedAsync, TaskScheduler.Default);

                    string workingDir = startInfo.WorkingDirectory;
                    const string host = "127.0.0.1";
                    int remoteDebugPort = ((IPEndPoint)remoteDebugListener.LocalEndpoint).Port;

                    // Launch Godot to run the game and connect to our remote debugger

                    var processStartInfo = new ProcessStartInfo(GetGodotExecutablePath())
                    {
                        Arguments = $"--path {workingDir} --remote-debug {host}:{remoteDebugPort} {godotStartInfo.StartArguments}", // TODO: Doesn't work with 4.0dev. Should be tcp://host:port which doesn't work in 3.2...
                        WorkingDirectory = workingDir,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    // Tells Godot to connect to the mono debugger we just started
                    processStartInfo.EnvironmentVariables["GODOT_MONO_DEBUGGER_AGENT"] =
                        "--debugger-agent=transport=dt_socket" +
                        $",address={host}:{assignedDebugPort}" +
                        ",server=n";

                    _process = new Process {StartInfo = processStartInfo};

                    _process.OutputDataReceived += (sendingProcess, outLine) => OutputData(outLine.Data, false);
                    _process.ErrorDataReceived += (sendingProcess, outLine) => OutputData(outLine.Data, true);

                    if (!_process.Start())
                    {
                        EndSessionWithError("Launch Error", "Failed to start Godot process");
                        return;
                    }

                    _process.BeginOutputReadLine();

                    if (_process.HasExited)
                    {
                        EndSessionWithError("Launch Error", $"Godot process exited with code: {_process.ExitCode}");
                        return;
                    }

                    _process.Exited += (sender, args) => EndSession();

                    OnDebuggerOutput(false, $"Godot PID:{_process.Id}{Environment.NewLine}");

                    break;
                }
                case ExecutionType.Attach:
                {
                    _attached = true;
                    StartConnecting(godotStartInfo);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(executionType.ToString());
            }

            if (!_attached)
            {
                var options = (GeneralOptionsPage)GodotPackage.Instance.GetDialogPage(typeof(GeneralOptionsPage));

                // If a connection is never established and we try to stop debugging, Visual Studio will freeze
                // for a long time for some reason. I have no idea why this happens. There may be something
                // we're doing wrong. For now we'll limit the time we wait for incoming connections.
                _ = Task.Delay(options.DebuggerListenTimeout).ContinueWith(r =>
                {
                    if (!HasExited && !IsConnected)
                    {
                        EndSession();

                        if (_process != null && !_process.HasExited)
                            _process.Kill();
                    }
                }, TaskScheduler.Default);
            }
        }

        protected override void OnExit()
        {
            if (_attached)
            {
                base.OnDetach();
            }
            else
            {
                base.OnExit();

                if (_process != null && !_process.HasExited)
                    _process.Kill();
            }
        }

        [SuppressMessage("ReSharper", "VSTHRD103")]
        private async Task OnGodotRemoteDebuggerConnectedAsync(Task<TcpClient> task)
        {
            var tcpClient = task.Result;
            _godotRemoteDebuggerStream = tcpClient.GetStream();
            var buffer = new byte[1000];
            while (tcpClient.Connected)
            {
                // There is no library to decode this messages, so
                // we just pump buffer so it doesn't go out of memory
                var readBytes = await _godotRemoteDebuggerStream.ReadAsync(buffer, 0, buffer.Length);
                _ = readBytes;
            }
        }

        private void OutputData(string data, bool isStdErr)
        {
            try
            {
                OnTargetOutput(isStdErr, data + Environment.NewLine);
                _ = Task.Run(async () =>
                {
                    await GodotPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
                    IVsOutputWindowPane outputPane = GodotPackage.Instance.GetOutputPane(VSConstants.OutputWindowPaneGuid.DebugPane_guid, "Output");
                    outputPane.OutputStringThreadSafe(data + Environment.NewLine);
                });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }
}
