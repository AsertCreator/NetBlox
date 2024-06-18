-- init script

local PlatformService = game:GetService("PlatformService");
local StarterGui = game:GetService("StarterGui");
local CoreGui = game:GetService("CoreGui");
local Debris = game:GetService("Debris");
local RobloxGui = CoreGui:FindFirstChild("RobloxGui");
local guivar = game:GetFastInt("FIntDefaultUIVariant");

if not game:GetFastFlag("FFlagShowCoreGui") then
	return;
end

if not PlatformService.IsStudio then
	function sendNotification(title, msg)
		local NotifyGui = Instance.new("ScreenGui");
		local NotifyFrame = Instance.new("Frame");
		local Title = Instance.new("TextLabel");
		local Text = Instance.new("TextLabel");
	
		Title.Position = UDim2.new(0, 0, 0.5, -9);
		Title.Size = UDim2.new(1, 0, 0, 18);
		Title.TextColor3 = Color3.new(1, 1, 1);
		Title.Text = title;
		Title.ZIndex = 101;
		Title.Parent = NotifyFrame;
		Title.FontSize = 18;

		Text.Position = UDim2.new(0, 0, 0.5, 11);
		Text.Size = UDim2.new(1, 0, 0, 18);
		Text.TextColor3 = Color3.new(1, 1, 1);
		Text.Text = msg;
		Text.ZIndex = 101;
		Text.Parent = NotifyFrame;

		print(title)
		print(msg)

		NotifyFrame.Position = UDim2.new(0, 0, 0, 0);
		NotifyFrame.Size = UDim2.new(1, 0, 1, 0);
		NotifyFrame.BackgroundColor3 = Color3.new(0.8, 0.8, 0.8);
		NotifyFrame.BackgroundTransparency = 0.5;
		NotifyFrame.ZIndex = 100;
		NotifyFrame.Parent = NotifyGui;

		NotifyGui.Parent = CoreGui;
		Debris:AddItem(NotifyGui, 4);
	end

	-- initializes server communication with public service
	function initStatus()
		if PlatformService:IsServer() then
			PlatformService:EnableStatusPipe();
			PlatformService:EnableRctlPipe();
		end
	end
	-- initializes notifications
	function initNotify()
		StarterGui:RegisterSetCore("SendNotification", function(tabl)
			sendNotification(tabl.Title, tabl.Text);
		end)
	end
	-- initializes in-game GUI
	function initIGG()
		if guivar == 1 then
			require(script.Parent.Modules.SidebarGui);
		end
	end
	-- initializes teleport GUI
	function initTUI()
		local telgui = require(script.Parent.Modules.TeleportGui);
		print(telgui);
		CoreGui.OnTeleportStarts:Connect(telgui.show);
		CoreGui.OnTeleportEnds:Connect(telgui.hide);
	end
	
	initStatus();
	initNotify();
	initIGG();
	initTUI();

	print("Platform initialized");

	PlatformService:BeginQueuedTeleport();
else
	print("init.lua does not run on NetBlox Studio!");
end