using System.Diagnostics;
using System.Text;
using NetBlox.Instances.Services.Internal;
using NetBlox.Runtime;

namespace NetBlox.Structs;

public class AssetDownloadTask
{
    public ContentId ContentId => contentId;
    public bool HadFinished => hadFinished;
    public bool HadErrored => hadErrored;
    public string? LocalDownloadPath => localDownloadPath;
    public CancellationTokenSource CancellationTokenSource = new();
    public GameAssetManager GameAssetManager;
    public string Error = "";

    private static HttpClient? NetBloxNetworkAssetWorker;

    private ContentId contentId;
    private bool hadFinished;
    private bool hadErrored;
    private bool isLocal;
    private string? localDownloadPath = null;
    private byte[]? buffer;
    private EventWaitHandle finishedEvent;
    private List<Action<AssetDownloadTask>> callbacksFailure = [];
    private List<Action<AssetDownloadTask>> callbacksSuccess = [];

    public AssetDownloadTask(GameAssetManager gameAssetManager, ContentId contentId)
    {
        this.contentId = contentId;
        finishedEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        GameAssetManager = gameAssetManager;
    }

    public AssetDownloadTask AddCallbackForFailure(Action<AssetDownloadTask> action)
    {
        if (!hadFinished)
            callbacksFailure.Add(action);
        else
            action(this);
        return this;
    }
    public AssetDownloadTask AddCallbackForSuccess(Action<AssetDownloadTask> action)
    {
        if (!hadFinished)
            callbacksSuccess.Add(action);
        else
            action(this);
        return this;
    }
    public Stream? ReadAsset()
    {
        if (isLocal)
        {
            if (localDownloadPath == null)
                return null;
            return File.OpenRead(localDownloadPath);
        }
        else
        {
            if (buffer == null)
                return null;
            return new MemoryStream(buffer);
        }
    }
    public void WaitUntilFinished()
    {
        if (hadFinished)
            return;
        finishedEvent.WaitOne();
    }
    public void SetBuffer(byte[] buffer)
    {
        this.buffer = buffer;
    }
    public void StartTask()
    {
        switch (ContentId.Protocol)
        {
            case ContentIdProtocol.NetBloxLocalFile:
                {
                    isLocal = true;
                    if (GameAssetManager.GameManager.AccessFlags.HasFlag(AccessFlags.DisableFilesystemAccess))
                    {
                        Trace.TraceError("AssetDownloadTask: filesystem access restricted");
                        hadFinished = true;
                        hadErrored = true;
                        isLocal = true;
                        Error = "Blocked due to a policy";
                        CallAllFailureCallbacks();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(ContentId.AssetPath))
                    {
                        Trace.TraceError("AssetDownloadTask: local path is null; cannot resolve");
                        hadFinished = true;
                        hadErrored = true;
                        isLocal = true;
                        Error = "Local path is null; cannot resolve";
                        CallAllFailureCallbacks();
                        return;
                    }
                    if (File.Exists(Path.GetFullPath(ContentId.AssetPath)))
                    {
                        hadFinished = true;
                        hadErrored = false;
                        isLocal = true;
                        localDownloadPath = Path.GetFullPath(ContentId.AssetPath);
                        CallAllSuccessCallbacks();
                        return;
                    }
                    break;
                }
            case ContentIdProtocol.NetBloxNetworkAsset:
                {
                    if (NetBloxNetworkAssetWorker == null)
                        NetBloxNetworkAssetWorker = new HttpClient();

                    if (GameAssetManager.GameManager.AccessFlags.HasFlag(AccessFlags.DisableHttpAccess) || GameAssetManager.GameManager.AccessFlags.HasFlag(AccessFlags.DisableNetworking))
                    {
                        Trace.TraceError("AssetDownloadTask: network access restricted");
                        hadFinished = true;
                        hadErrored = true;
                        Error = "Blocked due to a policy";
                        CallAllFailureCallbacks();
                        return;
                    }

                    if (ContentId.AssetPath != null)
                    {
                        try
                        {
                            NetBloxNetworkAssetWorker.GetAsync(ContentId.AssetPath).ContinueWith(async x =>
                            {
                                try
                                {
                                    HttpResponseMessage responseMessage = x.Result;
                                    string data = await responseMessage.Content.ReadAsStringAsync();
                                    string folderPath = GetMasterCacheDirectory();
                                    string filePath = ResolveCacheFilePath(ContentId.AssetPath);
                                    string fullPath = Path.Combine(folderPath, filePath);

                                    if (GameAssetManager.GameManager.AccessFlags.HasFlag(AccessFlags.DisableCaching))
                                    {
                                        buffer = Encoding.UTF8.GetBytes(data);
                                    }
                                    else
                                    {
                                        await File.WriteAllTextAsync(fullPath, data);
                                        isLocal = true;
                                        localDownloadPath = fullPath;
                                    }
                                    
                                    hadFinished = true;

                                    if (responseMessage.IsSuccessStatusCode)
                                    {
                                        hadErrored = false;
                                        CallAllSuccessCallbacks();
                                    }
                                    else
                                    {
                                        Error = "Server returned non-2xx status code (" + responseMessage.StatusCode + ")";
                                        hadErrored = true;
                                        CallAllFailureCallbacks();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Error = ex.GetType() + ", msg: " + ex.Message;
                                    hadErrored = true;
                                    CallAllFailureCallbacks();
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Error = ex.GetType() + ", msg: " + ex.Message;
                            hadErrored = true;
                            CallAllFailureCallbacks();
                        }
                        
                        return;
                    }
                    break;
                }
            case ContentIdProtocol.RbxAsset:
                {
                    if (ContentId.AssetId.HasValue)
                    {
                        if (GameAssetManager.GameManager.AccessFlags.HasFlag(AccessFlags.DisableHttpAccess) || GameAssetManager.GameManager.AccessFlags.HasFlag(AccessFlags.DisableNetworking))
                        {
                            Trace.TraceError("AssetDownloadTask: network access restricted");
                            hadFinished = true;
                            hadErrored = true;
                            Error = "Blocked due to a policy";
                            CallAllFailureCallbacks();
                            return;
                        }

                        StartDownloadingAssetId(ContentId.AssetId.Value);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(ContentId.AssetPath))
                        {
                            Trace.TraceError("AssetDownloadTask: local path is null; cannot resolve");
                            hadFinished = true;
                            hadErrored = true;
                            isLocal = true;
                            Error = "Local path is null; cannot resolve";
                            CallAllFailureCallbacks();
                            return;
                        }
                        if (ContentId.AssetPath.Contains("./") || ContentId.AssetPath.Contains(".."))
                        {
                            Trace.TraceError("AssetDownloadTask: path is unsanitized");
                            hadFinished = true;
                            hadErrored = true;
                            isLocal = true;
                            Error = "Malicious path; cannot resolve";
                            CallAllFailureCallbacks();
                            return;
                        }
                        
                        hadFinished = true;
                        hadErrored = false;
                        isLocal = true;
                        localDownloadPath = Path.GetFullPath(GameAssetManager.ContentDirectory + "/" + ContentId.AssetPath);
                        CallAllSuccessCallbacks();
                        return;
                    }
                    break;
                }
        }

        Trace.TraceError("AssetDownloadTask: unknown ContentId protocol: " + (int)contentId.Protocol);
        hadFinished = true;
        hadErrored = true;
        Error = "Unknown ContentId protocol; cannot resolve";
        CallAllFailureCallbacks();
    }
    private string GetMasterCacheDirectory()
    {
        string masterCacheDirectoryKey = "netblox-" + Version.VersionString;
        string masterCacheDirectoryKeyHash = HashingUtils.Sha256(masterCacheDirectoryKey);

        string masterCacheDirectoryPrefix;
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32NT:
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                masterCacheDirectoryPrefix = localAppData + "/Temp/NetBlox";
                break;
            case PlatformID.Unix:
                masterCacheDirectoryPrefix = "/tmp/NetBlox";
                break;
            default:
                throw new NotImplementedException("PlatformID.Other; come on...");
        }

        string fullpath = Path.GetFullPath(masterCacheDirectoryPrefix + "/caches/" + masterCacheDirectoryKeyHash);
        Directory.CreateDirectory(fullpath);
        return fullpath;
    }
    private string ResolveCacheFilePath(string key)
    {
        return GetMasterCacheDirectory() + HashingUtils.Sha256(key)[0..10];
    }
    private void StartDownloadingAssetId(long assetId)
    {
        if (GameAssetManager.GameManager.AccessFlags.HasFlag(AccessFlags.DisableHttpAccess) || GameAssetManager.GameManager.AccessFlags.HasFlag(AccessFlags.DisableNetworking))
        {
            Trace.TraceError("AssetDownloadTask: network access restricted");
            hadFinished = true;
            hadErrored = true;
            Error = "Blocked due to a policy";
            CallAllFailureCallbacks();
            return;
        }

        PlatformService platformService = GameAssetManager.Root.GetService<PlatformService>();

        string cacheKey = assetId + GameAssetManager.BaseUrl;
        string cacheFile = ResolveCacheFilePath(cacheKey);

        if (!GameAssetManager.GameManager.AccessFlags.HasFlag(AccessFlags.DisableCaching))
        {
            if (File.Exists(cacheFile) && File.GetLastWriteTimeUtc(cacheFile) - DateTime.UtcNow > GameAssetManager.CacheExpirySpan)
                File.Delete(cacheFile);
            
            if (File.Exists(cacheFile))
            {
                hadFinished = true;
                hadErrored = false;
                localDownloadPath = cacheFile;
                CallAllSuccessCallbacks();
            }
        }

        var task = GameAssetManager.HttpClient.GetAsync(platformService.ResolveAPI_AssetDelivery_Get(assetId), CancellationTokenSource.Token);
        task.ContinueWith(async (response) =>
        {
            try
            {
                if (GameAssetManager.GameManager.AccessFlags.HasFlag(AccessFlags.DisableCaching))
                {
                    hadFinished = true;
                    hadErrored = false;
                    buffer = await response.Result.Content.ReadAsByteArrayAsync();
                    CallAllSuccessCallbacks();
                }
                else
                {
                    using FileStream cacheFileStream = File.OpenWrite(cacheFile);
                    response.Result.Content.CopyTo(cacheFileStream, null, CancellationTokenSource.Token);

                    if (CancellationTokenSource.IsCancellationRequested)
                        return;

                    hadFinished = true;
                    hadErrored = false;
                    isLocal = true;
                    localDownloadPath = cacheFile;
                    CallAllSuccessCallbacks();
                }
            }
            catch
            {
                Trace.TraceWarning("AssetDownloadTask: StartDownloadingAssetId aborted");
                return;
            }
        });
    }
    private void CallAllFailureCallbacks()
    {
        Trace.TraceInformation("AssetDownloadTask: CallAllFailureCallbacks + " + ContentId.ToString());
        callbacksFailure.ForEach(x => x(this));
        finishedEvent.Set();
        lock (GameAssetManager)
            GameAssetManager.AllCurrentDownloadTasks.Remove(this);
    }
    private void CallAllSuccessCallbacks()
    {
        // Trace.TraceInformation("AssetDownloadTask: CallAllSuccessCallbacks + " + ContentId.ToString());
        callbacksSuccess.ForEach(x => x(this));
        finishedEvent.Set();
        lock (GameAssetManager)
            GameAssetManager.AllCurrentDownloadTasks.Remove(this);
    }
}