using Jotanunes.Internal.BddTests.Support.Models;

namespace Jotanunes.Internal.BddTests.Support.Fixtures;

public class TestSettingsFixture
{
    public TestSettings TestSettings { get; private set; }

    public TestSettingsFixture(ConfigurationFixture configurationFixture)
    {
        var configuration = configurationFixture.Configuration;
        TestSettings = TestConfigurationHelper.GetTestSettings(configuration);
    }
}
