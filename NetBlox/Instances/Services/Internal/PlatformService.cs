using System.Diagnostics;
using NetBlox.Runtime;

namespace NetBlox.Instances.Services.Internal;

[Service]
[NotReplicated]
public class PlatformService : Instance
{
    public override string ClassName => nameof(PlatformService);

    public const ulong NETWORK_CONSTANT_ID = 100;

    public const string API_AssetDelivery = "/api/assetdelivery";

    public PlatformService(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public string ResolveAPI_AssetDelivery_Get(long assetId) =>
        GameManager.GameAssetManager.BaseUrl + API_AssetDelivery + "/get?id=" + assetId;

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public void Elevate()
    {
        if (GameManager.GameScheduler.CurrentSchedulerTask != null)
            GameManager.GameScheduler.CurrentSchedulerTask.Identity = SecurityIdentity.SI_ElevatedStudioPlugin;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public int GetCurrentSecurityIdentityLevel()
    {
        if (GameManager.GameScheduler.CurrentSchedulerTask == null)
            return 0;
        if (GameManager.GameScheduler.CurrentSchedulerTask.Identity == null)
            return 0;
        return GameManager.GameScheduler.CurrentSchedulerTask.Identity.SecurityIdentityNumber;
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public void OpenShellLink(string link)
    {
        ProcessStartInfo processStartInfo = new ProcessStartInfo();
        processStartInfo.UseShellExecute = true;
        processStartInfo.FileName = link;
        Process.Start(processStartInfo);
    }
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.RobloxScript)]
    public string GetLicenseText()
    {
        return @"Copyright 2026 AsertCreator

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.";
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        base.Destroy();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(PlatformService))
            return base.IsA(className);
        return true;
    }
}