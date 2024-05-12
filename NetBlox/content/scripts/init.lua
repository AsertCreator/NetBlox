local PlatformService = game:GetService("PlatformService");
local CoreGui = game:GetService("CoreGui");

function initStatus()
	if PlatformService:IsServer() then
		PlatformService:EnableStatusPipe();
	end
end

function initIGG()
	local TopbarGui = Instance.new("ScreenGui");
	local TopbarFrame = Instance.new("Frame");
	
	TopbarGui.Name = "TopbarGui";
	TopbarGui.Parent = CoreGui;
	TopbarGui.Enabled = true;

	TopbarFrame.Parent = TopbarGui;
	TopbarFrame.Position = UDim2.new(0, 0, 0, 0);
	TopbarFrame.Size = UDim2.new(1, 0, 0, 30);
	TopbarFrame.BackgroundColor = Color3.new(0.1, 0.1, 0.1);
	TopbarFrame.BackgroundTransparency = 0.5
end
function initTUI()
	local TeleportGui = Instance.new("ScreenGui");
	local TeleportFrame = Instance.new("Frame");
	local GameTitle = Instance.new("TextLabel");
	local AuthorTitle = Instance.new("TextLabel");

	TeleportGui.Name = "TeleportGui";
	TeleportGui.Parent = CoreGui;
	TeleportGui.Enabled = true;

	TeleportFrame.Parent = TeleportGui;
	TeleportFrame.Position = UDim2.new(0, 0, 0, 0);
	TeleportFrame.Size = UDim2.new(1, 0, 1, 0);
	TeleportFrame.BackgroundColor = Color3.new(0.333, 0.333, 0.333);

	GameTitle.Parent = TeleportFrame;
	GameTitle.Position = UDim2.new(0, 0, 0, 0);
	GameTitle.Size = UDim2.new(1, 0, 1, 00);
	GameTitle.FontSize = 36;
	GameTitle.Text = "";
	
	AuthorTitle.Parent = TeleportFrame;
	AuthorTitle.Position = UDim2.new(0, 0, 0, 46);
	AuthorTitle.Size = UDim2.new(1, 0, 1, 00);
	AuthorTitle.FontSize = 24;
	AuthorTitle.Text = "";

	CoreGui:SetShowTeleportGuiCallback(function(placename, authorname, pid, uid)
		TeleportGui.Enabled = true;
		GameTitle.Text = placename;
		AuthorTitle.Text = "by " .. authorname;
	end);
	CoreGui:SetHideTeleportGuiCallback(function()
		TeleportGui.Enabled = false;
	end);
end

initStatus();
initIGG();
initTUI();

print("Platform initialized");

PlatformService:BeginQueuedTeleport();