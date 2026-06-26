namespace Screenbox.Templates;

public sealed partial class GroupedViewTemplates
{
    public GroupedViewTemplates()
    {
        InitializeComponent();
    }

    /// <summary>Determines if a value is normal.</summary>
    /// <param name="value">The value to be checked.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> is normal; otherwise, <see langword="false"/>.</returns>
    /// <seealso href="https://learn.microsoft.com/dotnet/api/system.int32.system-numerics-inumberbase-system-int32--isnormal"/>
    /// <seealso href="https://github.com/dotnet/dotnet/blob/v11.0.0/src/runtime/src/libraries/System.Private.CoreLib/src/System/Int32.cs#L804"/>
    public static bool IsIntegerNormal(int value) => value != 0;
}
