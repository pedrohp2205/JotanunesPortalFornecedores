namespace Jotanunes.External.BddTests.Support.Models;

public class TestSettings
{
    public DataBaseSettings DataBase { get; set; } = new DataBaseSettings();
}

public class DataBaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}
