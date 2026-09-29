using NetBlox.API;

namespace NetBlox;

public class CloudConfiguration
{
    public string? Username { get; set; }
    public long UserId { get; set; }

    private string? currentLoginToken;
    private Dictionary<string, object> config = [];

    public CloudConfiguration()
    {
        SyncWithPublicService();
    }

    public void AuthenticateAsGuest()
    {
        
    }
    public void AuthenticateAsUser(string username, string password)
    {
        if (currentLoginToken != null)
            throw new InvalidOperationException("Cannot authenticate a second time");

        Task<APIResult> apiResult = API.API.LoginAs(username, password);
        apiResult.Wait();
        if (apiResult.Result.IsSuccess)
        {
            string? loginToken = apiResult.Result.ResponseBody["accessToken"] as string;
            if (loginToken == null)
            {
                AuthenticateAsGuest();
                return;
            }
            currentLoginToken = loginToken;
        }
    }
    public void SyncWithPublicService()
    {
        // todo: stub
    }

    public bool GetFeatureFlagStatusBy(string name, bool defaultValue)
    {
        if (config.TryGetValue(name, out object? configValue) && configValue is bool b)
            return b;
        return defaultValue;
    }
    public string GetFeatureFlagStringStatusBy(string name, string defaultValue)
    {
        if (config.TryGetValue(name, out object? configValue) && configValue is string b)
            return b;
        return defaultValue;
    }
    public long GetFeatureFlagIntegerStatusBy(string name, long defaultValue)
    {
        if (config.TryGetValue(name, out object? configValue) && configValue is long b)
            return b;
        return defaultValue;
    }
    public bool HasDefined(string name)
    {
        return config.ContainsKey(name);
    }

    public void AddOverride(string name, object value)
    {
        config[name] = value;
    }
}