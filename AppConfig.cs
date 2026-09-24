public class AppConfig
{
    public string ConnectionString { get; internal set; } = string.Empty;
    public int TimeoutSeconds { get; internal set; }
    public bool EnableLogging { get; internal set; }
}

public class AppConfigBuilder
{
    private readonly AppConfig _config = new AppConfig();

    public AppConfigBuilder UseConnectionString(string connectionString)
    {
        _config.ConnectionString = connectionString;
        return this; // Returns the builder instance to allow chaining
    }

    public AppConfigBuilder WithTimeout(int seconds)
    {
        _config.TimeoutSeconds = seconds;
        return this;
    }

    public AppConfigBuilder EnableDebugLogging()
    {
        _config.EnableLogging = true;
        return this;
    }

    // The terminal method that outputs the final object
    public AppConfig Build()
    {
        // Add validation here if needed
        return _config;
    }
}
