using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Screenbox.Controls;
public sealed partial class SetOptionsDialog : ContentDialog
{
    public static readonly DependencyProperty OptionsProperty = DependencyProperty.Register(
        nameof(Options), typeof(string), typeof(SetOptionsDialog), new PropertyMetadata(default(string)));

    public string Options
    {
        get { return (string)GetValue(OptionsProperty); }
        set { SetValue(OptionsProperty, value); }
    }

    public SetOptionsDialog(string existingOptions)
    {
        this.InitializeComponent();
        Options = existingOptions;
        OptionsTextBox.Text = Options;
    }
}
