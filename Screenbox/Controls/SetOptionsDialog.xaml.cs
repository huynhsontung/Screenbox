using System.Linq;
using Screenbox.Helpers;
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

    private string OptionTextBoxPlaceholder { get; }

    private string[] VlcCommandLineHelpTextParts { get; }

    public SetOptionsDialog(string existingOptions, bool global = false)
    {
        this.InitializeComponent();
        FlowDirection = GlobalizationHelper.GetFlowDirection();
        RequestedTheme = ((FrameworkElement)Window.Current.Content).RequestedTheme;
        OptionTextBoxPlaceholder = global ? "--option=value" : ":option=value";
        Options = existingOptions;
        OptionsTextBox.Text = Options;
        var helpText = Strings.Resources.VlcCommandLineHelpText;
        VlcCommandLineHelpTextParts = helpText.Contains("{0}")
            ? helpText.Split("{0}").Select(s => s.Trim()).Take(2).ToArray()
            : new[] { helpText, string.Empty };

        if (global)
        {
            SecondaryButtonText = string.Empty;

            // Remove the first two inlines
            HelpText.Inlines.RemoveAt(0);
            HelpText.Inlines.RemoveAt(0);
        }
    }
}
