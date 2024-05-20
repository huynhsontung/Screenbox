#nullable enable

using Screenbox;
using Screenbox.Core.Helpers;
using Screenbox.Core.Services;
using System;
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
        // If the Windows shell indicates a recommended instance, then
        // the app can choose to redirect this activation to that instance instead.
        if (AppInstance.RecommendedInstance != null)
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
            bool isXbox = SystemInformation.IsXbox;
            if ((!isXbox && isFeatureEnabled && isFileActivated) || registeredInstances.Count == 0)
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
}