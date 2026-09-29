using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Fixtures;

using Microsoft.Extensions.Configuration;

using AssemblyFixtureAttribute = Reqnroll.xUnit3.ReqnrollPlugin.AssemblyFixtureAttribute;

[assembly: AssemblyFixture(typeof(ConfigurationFixture))]

namespace Jotanunes.Internal.BddTests.Support.Fixtures;

public class ConfigurationFixture
{
    public IConfiguration Configuration { get; private set; }

    public ConfigurationFixture()
    {
        Configuration = TestConfigurationHelper.GetConfiguration();
    }
}
