// -----------------------------------------------------------------------
// <copyright file="AutoRestoreAppContextSwitch.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Ubiquity.NET.Versioning.UT
{
    internal static class AutoRestoreAppContextSwitch
    {
        public static IDisposable Configure(string name, bool state)
        {
            AppContext.TryGetSwitch(name, out bool oldState);
            AppContext.SetSwitch(name, state);
            return new DisposableAction(()=>AppContext.SetSwitch(name, oldState));
        }
    }

    [SuppressMessage( "StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "DUH, it's file scoped!" )]
    file class DisposableAction
        : IDisposable
    {
        public DisposableAction(Action? action)
        {
            Disposer = action;
        }

        public void Dispose()
        {
            // as a thread safety measure, atomically set it to null
            var raiiCleanup = Interlocked.Exchange(ref Disposer, null);
            ObjectDisposedException.ThrowIf(raiiCleanup is null, this);
            raiiCleanup.Invoke();
        }

        private Action? Disposer;
    }
}
