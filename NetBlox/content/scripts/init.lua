local CoreGui = Instance.new("CoreGui")
local function stgc()
	print("we cum")
end
local function htgc()
	print("we came")
end

CoreGui.Parent = game
CoreGui:SetShowTeleportGuiCallback(stgc);
CoreGui:SetHideTeleportGuiCallback(htgc);

game:AddCrossDataModelInstance(CoreGui)
print("CoreGui initialized")