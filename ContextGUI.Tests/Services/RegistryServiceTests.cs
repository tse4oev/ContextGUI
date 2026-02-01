using System.Threading.Tasks;
using ContextGUI.Services;
using ContextGUI.Services.Interfaces;
using FluentAssertions;
using Microsoft.Win32;
using NSubstitute;
using Xunit;

namespace ContextGUI.Tests.Services;

public class RegistryServiceTests
{
    [Fact]
    public async Task GetAllContextMenuItemsAsync_WhenNotAdmin_ReturnsEmpty()
    {
        var registry = Substitute.For<IRegistryWrapper>();
        var backup = Substitute.For<IBackupService>();
        var admin = Substitute.For<IAdminService>();
        var logger = Substitute.For<ILoggingService>();
        admin.IsAdministrator().Returns(false);

        var sut = new RegistryService(registry, backup, admin, logger);

        var result = await sut.GetAllContextMenuItemsAsync();

        result.Should().BeEmpty();
        registry.DidNotReceiveWithAnyArgs().OpenClassesRootSubKey(default!);
    }

    [Fact]
    public async Task GetAllContextMenuItemsAsync_WhenAdmin_ReadsItems()
    {
        var registry = Substitute.For<IRegistryWrapper>();
        var backup = Substitute.For<IBackupService>();
        var admin = Substitute.For<IAdminService>();
        var logger = Substitute.For<ILoggingService>();
        admin.IsAdministrator().Returns(true);

        var baseKey = Substitute.For<IRegistryKey>();
        var itemKey = Substitute.For<IRegistryKey>();
        var commandKey = Substitute.For<IRegistryKey>();

        registry.OpenClassesRootSubKey("*\\shell").Returns(baseKey);
        baseKey.GetSubKeyNames().Returns(new[] { "MyItem" });
        baseKey.OpenSubKey("MyItem").Returns(itemKey);

        itemKey.GetValue(string.Empty).Returns("My Item");
        itemKey.GetValue("Icon").Returns("icon.ico");
        itemKey.GetValue("LegacyDisable").Returns(null);
        itemKey.OpenSubKey("command").Returns(commandKey);
        commandKey.GetValue(string.Empty).Returns("do-something");

        var sut = new RegistryService(registry, backup, admin, logger);

        var result = await sut.GetAllContextMenuItemsAsync();

        result.Should().ContainSingle();
        var item = result[0];
        item.DisplayName.Should().Be("My Item");
        item.IconPath.Should().Be("icon.ico");
        item.IsEnabled.Should().BeTrue();
        item.Command.Should().Be("do-something");
        item.Category.Should().Be("All Files");
        item.RegistryPath.Should().Be("HKEY_CLASSES_ROOT\\*\\shell\\MyItem");
    }

    [Fact]
    public async Task DisableContextMenuItemAsync_WhenNotAdmin_ReturnsUnauthorized()
    {
        var registry = Substitute.For<IRegistryWrapper>();
        var backup = Substitute.For<IBackupService>();
        var admin = Substitute.For<IAdminService>();
        var logger = Substitute.For<ILoggingService>();
        admin.IsAdministrator().Returns(false);

        var sut = new RegistryService(registry, backup, admin, logger);

        var result = await sut.DisableContextMenuItemAsync("HKEY_CLASSES_ROOT\\*\\shell\\Item");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Administrator");
        await backup.DidNotReceiveWithAnyArgs().CreateBackupAsync(default!);
    }

    [Fact]
    public async Task DisableContextMenuItemAsync_WhenNotClassesRoot_ReturnsError()
    {
        var registry = Substitute.For<IRegistryWrapper>();
        var backup = Substitute.For<IBackupService>();
        var admin = Substitute.For<IAdminService>();
        var logger = Substitute.For<ILoggingService>();
        admin.IsAdministrator().Returns(true);

        var sut = new RegistryService(registry, backup, admin, logger);

        var result = await sut.DisableContextMenuItemAsync("HKEY_LOCAL_MACHINE\\Software");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("HKEY_CLASSES_ROOT");
    }

    [Fact]
    public async Task DisableContextMenuItemAsync_WhenKeyExists_SetsLegacyDisable()
    {
        var registry = Substitute.For<IRegistryWrapper>();
        var backup = Substitute.For<IBackupService>();
        var admin = Substitute.For<IAdminService>();
        var logger = Substitute.For<ILoggingService>();
        admin.IsAdministrator().Returns(true);

        backup.CreateBackupAsync("HKEY_CLASSES_ROOT\\*\\shell\\Item", Arg.Any<System.Threading.CancellationToken>())
            .Returns(Task.FromResult("backup.reg"));

        var itemKey = Substitute.For<IRegistryKey>();
        registry.OpenClassesRootSubKey("*\\shell\\Item", true).Returns(itemKey);

        var sut = new RegistryService(registry, backup, admin, logger);

        var result = await sut.DisableContextMenuItemAsync("HKEY_CLASSES_ROOT\\*\\shell\\Item");

        result.Success.Should().BeTrue();
        result.BackupPath.Should().Be("backup.reg");
        itemKey.Received(1).SetValue("LegacyDisable", string.Empty, RegistryValueKind.String);
    }
}
