#nullable enable

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Screenbox.Controls;

public sealed partial class SelectionBarControl : UserControl
{
    /// <summary>
    /// Identifies the <see cref="CommandParameter"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter),
        typeof(object),
        typeof(SelectionBarControl),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the parameter to pass to the command for the <see cref="SelectionBarControl"/> buttons.
    /// </summary>
    /// <value>The parameter to pass to the command for the <<see cref="SelectionBarControl"/> buttons.
    /// The default is <see langword="null"/>.</value>
    public object CommandParameter
    {
        get { return (object)GetValue(CommandParameterProperty); }
        set { SetValue(CommandParameterProperty, value); }
    }

    #region Play Button properties

    /// <summary>
    /// Identifies the <see cref="PlayButtonCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PlayButtonCommandProperty = DependencyProperty.Register(
        nameof(PlayButtonCommand),
        typeof(ICommand),
        typeof(SelectionBarControl),
        new PropertyMetadata(default(ICommand)));

    /// <summary>
    /// Gets or sets the command to invoke when the play button is tapped.
    /// </summary>
    /// <value>The command to invoke when the play button is tapped.</value>
    public ICommand PlayButtonCommand
    {
        get { return (ICommand)GetValue(PlayButtonCommandProperty); }
        set { SetValue(PlayButtonCommandProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="IsPlayButtonVisible"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsPlayButtonVisibleProperty = DependencyProperty.Register(
        nameof(IsPlayButtonVisible),
        typeof(bool),
        typeof(SelectionBarControl),
        new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets a value that indicates whether the play button is shown.
    /// </summary>
    /// <value><see langword="true"/> to show the play button. <see langword="false"/>
    /// to hide the play button. The default is <b>true</b>.</value>
    public bool IsPlayButtonVisible
    {
        get { return (bool)GetValue(IsPlayButtonVisibleProperty); }
        set { SetValue(IsPlayButtonVisibleProperty, value); }
    }

    #endregion

    #region PlayNext Button properties

    /// <summary>
    /// Identifies the <see cref="PlayNextButtonCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PlayNextButtonCommandProperty = DependencyProperty.Register(
        nameof(PlayNextButtonCommand),
        typeof(ICommand),
        typeof(SelectionBarControl),
        new PropertyMetadata(default(ICommand)));

    /// <summary>
    /// Gets or sets the command to invoke when the play next button is tapped.
    /// </summary>
    /// <value>The command to invoke when the play next button is tapped.</value>
    public ICommand PlayNextButtonCommand
    {
        get { return (ICommand)GetValue(PlayNextButtonCommandProperty); }
        set { SetValue(PlayNextButtonCommandProperty, value); }
    }

    #endregion

    #region AddToQueue Button properties

    /// <summary>
    /// Identifies the <see cref="AddToQueueButtonCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty AddToQueueButtonCommandProperty = DependencyProperty.Register(
        nameof(AddToQueueButtonCommand),
        typeof(ICommand),
        typeof(SelectionBarControl),
        new PropertyMetadata(default(ICommand)));

    /// <summary>
    /// Gets or sets the command to invoke when the add to queue button is tapped.
    /// </summary>
    /// <value>The command to invoke when the add to queue button is tapped.</value>
    public ICommand AddToQueueButtonCommand
    {
        get { return (ICommand)GetValue(AddToQueueButtonCommandProperty); }
        set { SetValue(AddToQueueButtonCommandProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="IsAddToQueueButtonVisible"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsAddToQueueButtonVisibleProperty = DependencyProperty.Register(
        nameof(IsAddToQueueButtonVisible),
        typeof(bool),
        typeof(SelectionBarControl),
        new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets a value that indicates whether the add to queue button is shown.
    /// </summary>
    /// <value><see langword="true"/> to show the add to queue button. <see langword="false"/>
    /// to hide the add to queue button. The default is <b>true</b>.</value>
    public bool IsAddToQueueButtonVisible
    {
        get { return (bool)GetValue(IsAddToQueueButtonVisibleProperty); }
        set { SetValue(IsAddToQueueButtonVisibleProperty, value); }
    }

    #endregion

    #region Remove Button properties

    /// <summary>
    /// Identifies the <see cref="RemoveButtonCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty RemoveButtonCommandProperty = DependencyProperty.Register(
        nameof(RemoveButtonCommand),
        typeof(ICommand),
        typeof(SelectionBarControl),
        new PropertyMetadata(default(ICommand)));

    /// <summary>
    /// Gets or sets the command to invoke when the remove button is tapped.
    /// </summary>
    /// <value>The command to invoke when the remove button is tapped.</value>
    public ICommand RemoveButtonCommand
    {
        get { return (ICommand)GetValue(RemoveButtonCommandProperty); }
        set { SetValue(RemoveButtonCommandProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="IsRemoveButtonVisible"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsRemoveButtonVisibleProperty = DependencyProperty.Register(
        nameof(IsRemoveButtonVisible),
        typeof(bool),
        typeof(SelectionBarControl),
        new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets a value that indicates whether the remove button is shown.
    /// </summary>
    /// <value><see langword="true"/> to show the remove button. <see langword="false"/>
    /// to hide the remove button. The default is <b>true</b>.</value>
    public bool IsRemoveButtonVisible
    {
        get { return (bool)GetValue(IsRemoveButtonVisibleProperty); }
        set { SetValue(IsRemoveButtonVisibleProperty, value); }
    }

    #endregion

    #region Selection CheckBox properties

    /// <summary>
    /// Identifies the <see cref="CheckBoxText"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CheckBoxTextProperty = DependencyProperty.Register(
        nameof(CheckBoxText),
        typeof(string),
        typeof(SelectionBarControl),
        new PropertyMetadata(default(string)));

    /// <summary>
    /// Gets or sets the text to display on the <see cref="CheckBox"/> content.
    /// </summary>
    /// <value>The text to display on the <see cref="CheckBox"/> content.
    /// The default is an empty string.</value>
    public string CheckBoxText
    {
        get { return (string)GetValue(CheckBoxTextProperty); }
        set { SetValue(CheckBoxTextProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="CheckBoxIsChecked"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CheckBoxIsCheckedProperty = DependencyProperty.Register(
        nameof(CheckBoxIsChecked),
        typeof(bool?),
        typeof(SelectionBarControl),
        new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether the selection <see cref="CheckBox"/> is checked.
    /// </summary>
    /// <value><see langword="true"/> if the CheckBox is checked; <see langword="false"/> if the
    /// CheckBox is unchecked; otherwise <see langword="null"/>. The default is <b>false</b>.</value>
    public bool? CheckBoxIsChecked
    {
        get { return (bool?)GetValue(CheckBoxIsCheckedProperty); }
        set { SetValue(CheckBoxIsCheckedProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="CheckBoxCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CheckBoxCommandProperty = DependencyProperty.Register(
        nameof(CheckBoxCommand),
        typeof(ICommand),
        typeof(SelectionBarControl),
        new PropertyMetadata(default(ICommand)));

    /// <summary>
    /// Gets or sets the command to invoke when the check box is pressed.
    /// </summary>
    /// <value>The command to invoke when the check box is pressed.</value>
    public ICommand CheckBoxCommand
    {
        get { return (ICommand)GetValue(CheckBoxCommandProperty); }
        set { SetValue(CheckBoxCommandProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="CheckBoxCommandParameter"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CheckBoxCommandParameterProperty = DependencyProperty.Register(
        nameof(CheckBoxCommandParameter),
        typeof(object),
        typeof(SelectionBarControl),
        new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the parameter to pass to the command for the selection check box.
    /// </summary>
    /// <value>The parameter to pass to the command for the selection check box.
    /// The default is <see langword="null"/>.</value>
    public object CheckBoxCommandParameter
    {
        get { return (object)GetValue(CheckBoxCommandParameterProperty); }
        set { SetValue(CheckBoxCommandParameterProperty, value); }
    }

    #endregion

    #region Close Button Properties

    /// <summary>
    /// Identifies the <see cref="CloseButtonCommand"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty CloseButtonCommandProperty = DependencyProperty.Register(
        nameof(CloseButtonCommand),
        typeof(ICommand),
        typeof(SelectionBarControl),
        new PropertyMetadata(default(ICommand)));

    /// <summary>
    /// Gets or sets the command to invoke when the close button is tapped.
    /// </summary>
    /// <value>The command to invoke when the close button is tapped.</value>
    public ICommand CloseButtonCommand
    {
        get { return (ICommand)GetValue(CloseButtonCommandProperty); }
        set { SetValue(CloseButtonCommandProperty, value); }
    }

    #endregion

    /// <inheritdoc cref="CommandBar.DefaultLabelPositionProperty"/>
    public static readonly DependencyProperty DefaultLabelPositionProperty = DependencyProperty.Register(
        nameof(DefaultLabelPosition),
        typeof(CommandBarDefaultLabelPosition),
        typeof(SelectionBarControl),
        new PropertyMetadata(CommandBarDefaultLabelPosition.Right, OnDefaultLabelPositionChanged));

    /// <inheritdoc cref="CommandBar.DefaultLabelPosition"/>
    public CommandBarDefaultLabelPosition DefaultLabelPosition
    {
        get { return (CommandBarDefaultLabelPosition)GetValue(DefaultLabelPositionProperty); }
        set { SetValue(DefaultLabelPositionProperty, value); }
    }

    /// <summary>
    /// Gets the collection of additional primary command elements for the
    /// <see cref="SelectionBarControl"/>.
    /// </summary>
    /// <value>The collection of additional primary command elements for the
    /// <see cref="SelectionBarControl"/>. The default is an empty collection.</value>
    public ObservableCollection<ICommandBarElement>? AdditionalCommands { get; }

    ///// <inheritdoc cref="CommandBar.SecondaryCommands"/>
    //public ObservableCollection<ICommandBarElement>? SecondaryCommands { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionBarControl"/> class.
    /// </summary>
    public SelectionBarControl()
    {
        this.InitializeComponent();

        AdditionalCommands = new ObservableCollection<ICommandBarElement>();
        AdditionalCommands.CollectionChanged += AdditionalCommands_OnCollectionChanged;
        //SecondaryCommands = new ObservableCollection<ICommandBarElement>();
        //SecondaryCommands.CollectionChanged += SecondaryCommands_OnCollectionChanged;

        UpdateToolTips();
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        switch (e.OriginalKey)
        {
            case VirtualKey.GamepadB:
                if (CloseButtonCommand is { } closeCmd)
                {
                    closeCmd.Execute(null);
                    e.Handled = true;
                }
                break;
        }

        base.OnKeyDown(e);
    }

    private static void OnDefaultLabelPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (SelectionBarControl)d;
        if ((CommandBarDefaultLabelPosition)e.NewValue != (CommandBarDefaultLabelPosition)e.OldValue)
        {
            control.UpdateToolTips();
        }
    }

    private void AdditionalCommands_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (SelectionCommandBar is null) return;

        CommandBarCollection_OnCollectionChanged(SelectionCommandBar.PrimaryCommands, e);
    }

    //private void SecondaryCommands_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    //{
    //    if (SelectionCommandBar is null) return;

    //    CommandBarCollection_OnCollectionChanged(SelectionCommandBar.SecondaryCommands, e);
    //}

    private void DeleteKeyboardAccelerator_OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (!IsRemoveButtonVisible) return;

        if (RemoveButtonCommand is { } cmd && CommandParameter is { } parameter)
        {
            if (cmd.CanExecute(parameter))
            {
                cmd.Execute(parameter);
                args.Handled = true;
            }
        }
    }

    private void CommandBarCollection_OnCollectionChanged(IObservableVector<ICommandBarElement> collection, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (ICommandBarElement item in e.NewItems)
                {
                    collection.Add(item);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (ICommandBarElement item in e.OldItems)
                {
                    collection.Remove(item);
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                foreach (ICommandBarElement oldItem in e.OldItems)
                {
                    collection.Remove(oldItem);
                }
                foreach (ICommandBarElement newItem in e.NewItems)
                {
                    collection.Add(newItem);
                }
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Reset:
                collection.Clear();
                break;
        }
    }

    private void UpdateToolTips()
    {
        if (DefaultLabelPosition == CommandBarDefaultLabelPosition.Collapsed)
        {
            if (IsPlayButtonVisible)
            {
                ToolTipService.SetToolTip(PlayButton, Strings.Resources.Play);
            }

            ToolTipService.SetToolTip(PlayNextButton, Strings.Resources.PlayNext);

            if (IsAddToQueueButtonVisible)
            {
                ToolTipService.SetToolTip(AddToQueueButton, Strings.Resources.AddToQueue);
            }

            ToolTipService.SetToolTip(AddToPlaylistButton, Strings.Resources.AddToPlaylist);
        }
        else
        {
            if (IsPlayButtonVisible && PlayButton is not null)
            {
                PlayButton.ClearValue(ToolTipService.ToolTipProperty);
            }

            PlayNextButton.ClearValue(ToolTipService.ToolTipProperty);

            if (IsAddToQueueButtonVisible && AddToQueueButton is not null)
            {
                AddToQueueButton.ClearValue(ToolTipService.ToolTipProperty);
            }

            AddToPlaylistButton.ClearValue(ToolTipService.ToolTipProperty);
        }
    }
}
