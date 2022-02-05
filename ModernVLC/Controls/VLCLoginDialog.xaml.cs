using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ModernVLC.Controls
{
    public sealed partial class VLCLoginDialog : ContentDialog
    {
        public static readonly DependencyProperty UsernameProperty = DependencyProperty.Register(
            nameof(Username),
            typeof(string),
            typeof(VLCLoginDialog),
            new PropertyMetadata(null));

        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(
            nameof(Password),
            typeof(string),
            typeof(VLCLoginDialog),
            new PropertyMetadata(null));

        public string Text { get; set; }

        public string Username
        {
            get => (string)GetValue(UsernameProperty);
            set => SetValue(UsernameProperty, value);
        }

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        public bool StoreCredential { get; set; }

        public bool AskStoreCredential { get; set; }

        public VLCLoginDialog()
        {
            this.InitializeComponent();
        }
    }
}
