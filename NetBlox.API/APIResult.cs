using Newtonsoft.Json;

namespace NetBlox.API;

public sealed record class APIResult
{
    public readonly bool IsSuccess;
    public readonly int ResultCode;
    public readonly Dictionary<string, object> ResponseBody;
    public Stream? OptionalStream;

    public APIResult(bool isSuccess, int result, Dictionary<string, object> response)
    {
        IsSuccess = isSuccess;
        ResultCode = result;
        ResponseBody = response;
    }
    public APIResult(string serialized)
    {
        Dictionary<string, object>? result = JsonConvert.DeserializeObject<Dictionary<string, object>>(serialized);

        if (result == null)
        {
            IsSuccess = false;
            ResultCode = -1;
            ResponseBody = new()
            {
                ["raw"] = serialized  
            };
        }
        else
        {
            IsSuccess = (bool)result["ok"];
            ResultCode = (int)result["code"];
            ResponseBody = result;
        }
    }

    public T? Get<T>(string key)
    {
        if (ResponseBody.TryGetValue(key, out object? value) && value is T val)
            return val;
        return default;
    }
}