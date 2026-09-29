using System.Diagnostics;
using NetBlox.Instances;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox;

public sealed class GameAssetManager
{
    public readonly GameManager GameManager;
    public bool AllowNetworkAssets = false;
    public List<AssetDownloadTask> AllCurrentDownloadTasks;
    public string BaseUrl = "http://localhost";
    public TimeSpan CacheExpirySpan = TimeSpan.FromDays(4);
    public HttpClient HttpClient = new();

    public string ContentDirectory
    {
        get => field;
        private set
        {
            field = value;
            Trace.TraceInformation("Setting ContentDirectory to " + value);
        }
    }
    public DataModel Root => GameManager.RootModel;

    private bool disallowAnything = false;
    private Dictionary<string, Texture2D> cachedImages = [];
    private Dictionary<string, Shader> cachedShaders = [];

    public GameAssetManager(GameManager gameManager, bool assemblyAsSource)
    {
        AllCurrentDownloadTasks = [];
        GameManager = gameManager;
        
        if (Environment.ProcessPath == null)
        {
            throw new NotImplementedException("Environment.ProcessPath == null; not supported");
        }
        else
        {
            string homeDirectory = Environment.CurrentDirectory;
            string? executableDirectory = Path.GetDirectoryName(Environment.ProcessPath);
            string contentDirectoryAtHome = Path.Combine(homeDirectory, "content/");

            if (!Directory.Exists(contentDirectoryAtHome))
                Trace.TraceError(contentDirectoryAtHome + " cannot be used as a content directory; skipping...");
            else
            {
                ContentDirectory = contentDirectoryAtHome;
                return;
            }

            if (executableDirectory == null)
                throw new InvalidOperationException("How come this program is running in the root directory; change that");

            string contentDirectoryAtProcessPath = Path.Combine(executableDirectory, "content/");

            if (!Directory.Exists(contentDirectoryAtProcessPath))
                throw new InvalidOperationException(contentDirectoryAtProcessPath + " cannot be used as a content directory; no more candidates");
            else
            {
                ContentDirectory = contentDirectoryAtProcessPath;
                return;
            }
        }
    }
    public AssetDownloadTask? BeginDownloadingFileFrom(ContentId contentId)
    {
        if (disallowAnything)
            return null;
        AssetDownloadTask assetDownloadTask = new AssetDownloadTask(this, contentId);
        lock (AllCurrentDownloadTasks)
            AllCurrentDownloadTasks.Add(assetDownloadTask);
        assetDownloadTask.StartTask();
        return assetDownloadTask;
    }
    public AssetDownloadTask? QuickLoad(string contentId)
    {
        if (!TryParseContentId(contentId, out ContentId contentId1))
            return null;
        return BeginDownloadingFileFrom(contentId1);
    }
    public Texture2D LoadTextureFromPath(string filePath)
    {
        if (cachedImages.TryGetValue(filePath, out Texture2D texture2D))
            return texture2D;
        if (GameManager.WindowReady)
        {
            cachedImages[filePath] = Raylib.LoadTexture(filePath);
            Raylib.SetTextureFilter(cachedImages[filePath], TextureFilter.Anisotropic4X);
            return cachedImages[filePath];
        }
        return default;
    }
    public Shader LoadShaderFromPath(string filePath)
    {
        if (cachedShaders.TryGetValue(filePath, out Shader shader))
            return shader;
        if (GameManager.WindowReady)
        {
            cachedShaders[filePath] = Raylib.LoadShader(filePath + ".vs", filePath + ".fs");
            return cachedShaders[filePath];
        }
        return default;
    }
    public void Shutdown()
    {
        disallowAnything = true;
        AllowNetworkAssets = false;
        AllCurrentDownloadTasks.ForEach(x => x.CancellationTokenSource.Cancel());
        AllCurrentDownloadTasks.Clear();
    }
    public bool TryParseContentId(string contentIdString, out ContentId contentId)
    {
        try
        {
            contentIdString = contentIdString.TrimStart().TrimEnd();

            if (string.IsNullOrWhiteSpace(contentIdString))
            {
                contentId = default;
                return false;
            }

            if (long.TryParse(contentIdString, out long shortAssetId))
            {
                contentId = new ContentId()
                {
                    Protocol = ContentIdProtocol.RbxAsset,
                    AssetId = shortAssetId
                };
                return true;
            }
            
            if (contentIdString.StartsWith("rbxasset://"))
            {
                if (long.TryParse(contentIdString.Substring(11), out long assetId))
                {
                    contentId = new ContentId()
                    {
                        Protocol = ContentIdProtocol.RbxAsset,
                        AssetId = assetId
                    };
                    return true;
                }
                else
                {
                    contentId = new ContentId()
                    {
                        Protocol = ContentIdProtocol.RbxAsset,
                        AssetPath = contentIdString.Substring(11)
                    };
                    return true;
                }
            }
            else if (contentIdString.StartsWith("net-internal://"))
            {
                contentId = new ContentId()
                {
                    Protocol = ContentIdProtocol.NetBloxInternalAsset,
                    AssetPath = contentIdString.Substring(15)
                };
                return true;
            }
            else if (contentIdString.StartsWith("net-local://"))
            {
                contentId = new ContentId()
                {
                    Protocol = ContentIdProtocol.NetBloxLocalFile,
                    AssetPath = contentIdString.Substring(12)
                };
                return true;
            }
            else if (contentIdString.StartsWith("net-network://"))
            {
                contentId = new ContentId()
                {
                    Protocol = ContentIdProtocol.NetBloxNetworkAsset,
                    AssetPath = contentIdString.Substring(14)
                };
                return true;
            }

            contentId = default;
            return false;
        }
        catch
        {
            contentId = default;
            return false;
        }
    }
}