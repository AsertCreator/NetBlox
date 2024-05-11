local CoreGui = Instance.new("CoreGui")
local PlatformService = Instance.new("PlatformService")

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

CoreGui.Parent = game
CoreGui:SetShowTeleportGuiCallback(stgc);
CoreGui:SetHideTeleportGuiCallback(htgc);

game:AddCrossDataModelInstance(CoreGui)
game:AddCrossDataModelInstance(PlatformService)

print("Platform initialized")