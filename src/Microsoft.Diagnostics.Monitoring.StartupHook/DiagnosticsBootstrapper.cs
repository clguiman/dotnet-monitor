﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions;
using Microsoft.Diagnostics.Monitoring.StartupHook.Monitoring;
using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.HostingStartup;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using System;
using System.IO;
using MessageDispatcher = Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher;

namespace Microsoft.Diagnostics.Monitoring.StartupHook
{
    internal sealed class DiagnosticsBootstrapper :
        IDisposable
    {
        private readonly CurrentAppDomainExceptionProcessor _exceptionProcessor;
        private readonly AspNetHostingStartupLoader? _hostingStartupLoader;
        private readonly ParameterCapturingService? _parameterCapturingService;

        private long _disposedState;

        public DiagnosticsBootstrapper()
        {
            _exceptionProcessor = new(ToolIdentifiers.IsEnvVarEnabled(InProcessFeaturesIdentifiers.EnvironmentVariables.Exceptions.IncludeMonitorExceptions));
            _exceptionProcessor.Start();

            using IDisposable _ = MonitorExecutionContextTracker.MonitorScope();

            string? hostingStartupPath = Environment.GetEnvironmentVariable(StartupHookIdentifiers.EnvironmentVariables.HostingStartupPath);
            // TODO: Log if specified hosting startup assembly doesn't exist
            if (File.Exists(hostingStartupPath))
            {
                _hostingStartupLoader = new AspNetHostingStartupLoader(hostingStartupPath);
            }

            try
            {
                // Check that the profiler is loaded before establishing the dispatcher, which has a dependency on the existence of the profiler
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ProfilerIdentifiers.NotifyOnlyProfiler.EnvironmentVariables.ProductVersion)))
                {
                    SharedInternals.MessageDispatcher = new MessageDispatcher.MonitorMessageDispatcher(
                        new MessageDispatcher.ProfilerMessageSource(CommandSet.StartupHook));
                    ToolIdentifiers.EnableEnvVar(InProcessFeaturesIdentifiers.EnvironmentVariables.AvailableInfrastructure.ManagedMessaging);
                }

                if (ToolIdentifiers.IsEnvVarEnabled(InProcessFeaturesIdentifiers.EnvironmentVariables.ParameterCapturing.Enable))
                {
                    _parameterCapturingService = new();
                    _parameterCapturingService.Start();
                }
            }
            catch
            {
            }

            ToolIdentifiers.EnableEnvVar(InProcessFeaturesIdentifiers.EnvironmentVariables.AvailableInfrastructure.StartupHook);
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            _exceptionProcessor.Dispose();
            _parameterCapturingService?.Stop();
            _parameterCapturingService?.Dispose();
            _hostingStartupLoader?.Dispose();
            SharedInternals.MessageDispatcher?.Dispose();
        }
    }
}
