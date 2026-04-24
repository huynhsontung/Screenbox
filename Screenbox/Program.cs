#nullable enable

using System;
using System.Threading;
using Screenbox;
using Screenbox.Core.Services;
using Screenbox.Helpers;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;

public static class Program
{
    // This example code shows how you could implement the required Main method to
    // support multi-instance redirection. The minimum requirement is to call
    // Application.Start with a new App object. Beyond that, you may delete the
    // rest of the example code and replace it with your custom code if you wish.

    static void Main(string[] args)
    {
        // If the Windows shell indicates a recommended instance, redirect to it — but
        // only when that instance is still alive and accepting activations.
        if (AppInstance.RecommendedInstance != null && IsInstanceReady(AppInstance.RecommendedInstance))
        {
            AppInstance.RecommendedInstance.RedirectActivationTo();
        }
        else
        {
            AppInstance instance;
            var settingsService = new SettingsService();
            var registeredInstances = AppInstance.GetInstances();
            IActivatedEventArgs? activatedArgs = AppInstance.GetActivatedEventArgs();    // This is null on Xbox
            bool isFileActivated = activatedArgs?.Kind == ActivationKind.File;
            bool isFeatureEnabled = settingsService.UseMultipleInstances;
            bool isXbox = DeviceInfoHelper.IsXbox;

            // Start a new instance when:
            //  • multi-instance mode is on and this is a file activation, OR
            //  • there are no existing registrations, OR
            //  • the only registered instance is closing/dead (activation-ready mutex released)
            bool hasReadyInstance = registeredInstances.Count > 0 && IsInstanceReady(registeredInstances[0]);
            if ((!isXbox && isFeatureEnabled && isFileActivated) || !hasReadyInstance)
            {
                string key = Guid.NewGuid().ToString();
                instance = AppInstance.FindOrRegisterInstanceForKey(key);
            }
            else
            {
                instance = registeredInstances[0];
            }

            if (instance.IsCurrentInstance)
            {
                // If we successfully registered this instance, we can now just
                // go ahead and do normal XAML initialization.
                Windows.UI.Xaml.Application.Start(p => new App());
            }
            else
            {
                // Some other instance has registered for this key, so we'll 
                // redirect this activation to that instance instead.
                instance.RedirectActivationTo();
            }
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="instance"/> is alive and
    /// ready to receive an activation redirect.
    /// <para>
    /// Each running <see cref="App"/> instance acquires a named mutex whose name
    /// includes its AppInstance registration key (see <see cref="App.ActivationReadyMutexPrefix"/>).
    /// The mutex is released when the window is closed, so attempting to open it
    /// here tells us whether the target process is still in a usable state.
    /// </para>
    /// </summary>
    private static bool IsInstanceReady(AppInstance instance)
    {
        try
        {
            string mutexName = $"{App.ActivationReadyMutexPrefix}{instance.Key}";

            if (!Mutex.TryOpenExisting(mutexName, out Mutex? mutex))
            {
                // Mutex does not exist — the instance has not finished initialising
                // yet, or it has already cleaned up and is shutting down.
                return false;
            }

            using (mutex)
            {
                // Try to acquire the mutex without blocking.  If we succeed the
                // instance has already released it (window closed) and is dying.
                bool acquired = mutex.WaitOne(0);
                if (acquired)
                {
                    mutex.ReleaseMutex();
                    return false;
                }

                return true;
            }
        }
        catch (AbandonedMutexException)
        {
            // The owning process terminated without releasing — instance is dead.
            return false;
        }
        catch (Exception)
        {
            // Treat any unexpected error as "not ready" to avoid a broken redirect.
            return false;
        }
    }
}
