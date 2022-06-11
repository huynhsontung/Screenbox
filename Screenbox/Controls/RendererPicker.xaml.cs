#nullable enable

using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Screenbox.ViewModels;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Controls
{
    public sealed partial class RendererPicker : ContentDialog
    {
        internal RendererPickerViewModel ViewModel => (RendererPickerViewModel)DataContext;

        private RendererPicker()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetRequiredService<RendererPickerViewModel>();
            ViewModel.StartDiscovering();
        }

        public static async Task StartCastingAsync()
        {
            RendererPicker picker = new();
            await picker.ShowAsync();
            picker.ViewModel.StartCasting();
            picker.ViewModel.StopDiscovering();
        }
    }
}
