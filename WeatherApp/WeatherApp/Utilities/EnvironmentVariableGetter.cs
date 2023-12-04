namespace WeatherApp.Utilities
{
    public static class EnvironmentVariableGetter
    {
        public static string Get(string name)
        {
            return Environment.GetEnvironmentVariable(name) ?? throw new Exception($"Missing environment variable {name}");
        }
    }
}
