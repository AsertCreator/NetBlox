using System.Diagnostics;
using NetBlox.Instances.Services.Internal;

namespace NetBlox.Structs;

public class AssetDownloadTask
{
    public ContentId ContentId => contentId;
    public bool HadFinished => hadFinished;
    public bool HadErrored => hadErrored;
    public string? LocalDownloadPath => localDownloadPath;
    public CancellationTokenSource CancellationTokenSource = new();
    public GameAssetManager GameAssetManager;

    private ContentId contentId;
    private bool hadFinished;
    private bool hadErrored;
    private bool isLocal;
    private string? localDownloadPath = null;
    private byte[]? buffer;
    private List<Action<AssetDownloadTask>> callbacksFailure = [];
    private List<Action<AssetDownloadTask>> callbacksSuccess = [];

    public AssetDownloadTask(GameAssetManager gameAssetManager, ContentId contentId)
    {
        this.contentId = contentId;
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
                    if (string.IsNullOrWhiteSpace(ContentId.AssetPath))
                    {
                        Trace.TraceError("AssetDownloadTask: local path is null; cannot resolve");
                        hadFinished = true;
                        hadErrored = true;
                        CallAllFailureCallbacks();
                        return;
                    }
                    if (File.Exists(Path.GetFullPath(ContentId.AssetPath)))
                    {
                        hadFinished = true;
                        hadErrored = false;
                        localDownloadPath = Path.GetFullPath(ContentId.AssetPath);
                        CallAllSuccessCallbacks();
                        return;
                    }
                    break;
                }
            case ContentIdProtocol.RbxAsset:
                {
                    if (ContentId.AssetId.HasValue)
                    {
                        StartDownloadingAssetId(ContentId.AssetId.Value);
                    }
                    else
                    {
                        isLocal = true;

                        if (string.IsNullOrWhiteSpace(ContentId.AssetPath))
                        {
                            Trace.TraceError("AssetDownloadTask: local path is null; cannot resolve");
                            hadFinished = true;
                            hadErrored = true;
                            CallAllFailureCallbacks();
                            return;
                        }
                        
                        hadFinished = true;
                        hadErrored = false;
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
        PlatformService platformService = GameAssetManager.Root.GetService<PlatformService>();

        string cacheKey = assetId + GameAssetManager.BaseUrl;
        string cacheFile = ResolveCacheFilePath(cacheKey);

        if (File.Exists(cacheFile) && File.GetLastWriteTimeUtc(cacheFile) - DateTime.UtcNow > GameAssetManager.CacheExpirySpan)
            File.Delete(cacheFile);
        
        if (File.Exists(cacheFile))
        {
            hadFinished = true;
            hadErrored = false;
            localDownloadPath = cacheFile;
            CallAllSuccessCallbacks();
        }

        var task = GameAssetManager.HttpClient.GetAsync(platformService.ResolveAPI_AssetDelivery_Get(assetId), CancellationTokenSource.Token);
        task.ContinueWith(response =>
        {
            try
            {
                using FileStream cacheFileStream = File.OpenWrite(cacheFile);
                response.Result.Content.CopyTo(cacheFileStream, null, CancellationTokenSource.Token);

                if (CancellationTokenSource.IsCancellationRequested)
                    return;

                hadFinished = true;
                hadErrored = false;
                localDownloadPath = cacheFile;
                CallAllSuccessCallbacks();
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
        lock (GameAssetManager)
            GameAssetManager.AllCurrentDownloadTasks.Remove(this);
    }
    private void CallAllSuccessCallbacks()
    {
        // Trace.TraceInformation("AssetDownloadTask: CallAllSuccessCallbacks + " + ContentId.ToString());
        callbacksSuccess.ForEach(x => x(this));
        lock (GameAssetManager)
            GameAssetManager.AllCurrentDownloadTasks.Remove(this);
    }
}