namespace NetBlox.API;

public static class API
{
    public static string CurrentAPIBase { get; set; } = "http://127.0.0.1:3151";
    public static string? CurrentAccessToken { get; private set; }
    private static readonly HttpClient HttpClient = new HttpClient();

    public static void SetAccessToken(string accessToken)
    {
        CurrentAccessToken = accessToken;
    }
    public static async Task<APIResult> LoginAs(string username, string password)
    {
        APIResult result = await GetAsync("/api/v1/users/login", 
            ("username", username),
            ("password", password)
        );

        if (!result.IsSuccess)
            return result;
        
        CurrentAccessToken = result.Get<string>("accessToken")!;

        return result;
    }

    public static async Task<APIResult> DownloadAssetAt(long assetId)
    {
        APIResult result = await GetStreamAsync("/api/v1/assets/" + assetId);
        return result;
    }

    public static async Task<APIResult> GetAsync(string path, params ValueTuple<string, object>[] args)
    {
        string fullpath = CurrentAPIBase + path;

        if (args.Length > 0)
        {
            fullpath += "?";
            for (int i = 0; i < args.Length; i++)
            {
                fullpath += args[i].Item1 + "=" + Uri.EscapeDataString(args[i].Item2.ToString() ?? "");
                if (i != args.Length - 1)
                    fullpath += "&";
            }
        }

        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, fullpath);
        if (CurrentAccessToken != null)
            requestMessage.Headers.Add("Authentication", CurrentAccessToken);

        HttpResponseMessage responseMessage = await HttpClient.SendAsync(requestMessage);
        string responseString = await responseMessage.Content.ReadAsStringAsync();

        return new APIResult(responseString);
    }
    public static async Task<APIResult> GetStreamAsync(string path, params ValueTuple<string, object>[] args)
    {
        string fullpath = CurrentAPIBase + path;

        if (args.Length > 0)
        {
            fullpath += "?";
            for (int i = 0; i < args.Length; i++)
            {
                fullpath += args[i].Item1 + "=" + Uri.EscapeDataString(args[i].Item2.ToString() ?? "");
                if (i != args.Length - 1)
                    fullpath += "&";
            }
        }

        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, fullpath);
        if (CurrentAccessToken != null)
            requestMessage.Headers.Add("Authentication", CurrentAccessToken);

        HttpResponseMessage responseMessage = await HttpClient.SendAsync(requestMessage);
        Stream stream = await responseMessage.Content.ReadAsStreamAsync();
        
        return new APIResult((int)responseMessage.StatusCode >= 200 && (int)responseMessage.StatusCode < 300, 
            (int)responseMessage.StatusCode, [])
        {
            OptionalStream = stream  
        };
    }

    public static async Task<APIResult> PostAsync(string path, Stream content)
    {
        string fullpath = CurrentAPIBase + path;

        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, fullpath);
        if (CurrentAccessToken != null)
            requestMessage.Headers.Add("Authentication", CurrentAccessToken);
        requestMessage.Content = new StreamContent(content);

        HttpResponseMessage responseMessage = await HttpClient.SendAsync(requestMessage);
        string responseString = await responseMessage.Content.ReadAsStringAsync();

        return new APIResult(responseString);
    }
}