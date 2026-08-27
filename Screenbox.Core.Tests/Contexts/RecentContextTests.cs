using Screenbox.Core.Contexts;

namespace Screenbox.Core.Tests.Contexts;

public class RecentContextTests
{
    [Test]
    public async Task RecentContext_InitialState_ShouldBeEmptyAndNotLoaded()
    {
        var context = new RecentContext();

        await Assert.That(context.Recent).IsEmpty();
        await Assert.That(context.PathToMruMappings).IsEmpty();
        await Assert.That(context.TokenToMediaMappings).IsEmpty();
        await Assert.That(context.IsLoaded).IsFalse();
    }

    [Test]
    public async Task RecentContext_IsLoaded_ShouldRaisePropertyChanged()
    {
        var context = new RecentContext();
        string? changedProperty = null;
        context.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

        context.IsLoaded = true;

        await Assert.That(context.IsLoaded).IsTrue();
        await Assert.That(changedProperty).IsEqualTo(nameof(RecentContext.IsLoaded));
    }
}
