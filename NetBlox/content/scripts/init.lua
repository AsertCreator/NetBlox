local PlatformService = game:GetService("PlatformService")
local CoreGui = game:GetService("CoreGui")

local function stgc()
	print("we cum")
end
local function htgc()
	print("we came")
end

PlatformService.Parent = game

if PlatformService:IsServer() then
	PlatformService:EnableStatusPipe();
end

printidentity();

CoreGui:SetShowTeleportGuiCallback(stgc);
CoreGui:SetHideTeleportGuiCallback(htgc);

print("Platform initialized")