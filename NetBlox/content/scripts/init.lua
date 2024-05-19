local PlatformService = game:GetService("PlatformService");
local CoreGui = game:GetService("CoreGui");

if not PlatformService.IsStudio then
	function initStatus()
		if PlatformService:IsServer() then
			PlatformService:EnableStatusPipe();
		end
	end

	function initIGG()
		local TopbarGui = Instance.new("ScreenGui");
		local Sidebar = Instance.new("Frame");
		local TopbarFrame = Instance.new("Frame");
		local MenuButton = Instance.new("TextButton");
		local ExitButton = Instance.new("TextButton");
	
		TopbarGui.Name = "TopbarGui";
		TopbarGui.Parent = CoreGui;
		TopbarGui.Enabled = true;

		TopbarFrame.Parent = TopbarGui;
		TopbarFrame.Position = UDim2.new(0, 0, 0, 0);
		TopbarFrame.Size = UDim2.new(1, 0, 0, 30);
		TopbarFrame.BackgroundColor = Color3.new(0.1, 0.1, 0.1);
		TopbarFrame.BackgroundTransparency = 0.5;
		
		Sidebar.Parent = TopbarGui;
		Sidebar.Position = UDim2.new(0, 0, 0, 0);
		Sidebar.Size = UDim2.new(0, 400, 1, 0);
		Sidebar.BackgroundColor = Color3.new(0.1, 0.1, 0.1);
		Sidebar.Visible = false;

		-- here we init Sidebar
		
		ExitButton.Parent = Sidebar;
		ExitButton.Position = UDim2.new(0, 50, 0, 50);
		ExitButton.Size = UDim2.new(0, 250, 0, 30);
		ExitButton.BackgroundColor = Color3.new(1, 1, 1);
		ExitButton.ForegroundColor = Color3.new(1, 1, 1);
		ExitButton.Text = "Leave the game";
		ExitButton.MouseButton1Click:Connect(function()
			game:Shutdown();
		end)

		-- here we stop init Sidebar
		
		MenuButton.Parent = TopbarFrame;
		MenuButton.Position = UDim2.new(0, 0, 0, 0);
		MenuButton.Size = UDim2.new(0, 30, 0, 30);
		MenuButton.BackgroundColor = Color3.new(1, 1, 1);
		MenuButton.BackgroundTransparency = 0.7;
		MenuButton.MouseButton1Click:Connect(function()
			Sidebar.Visible = not Sidebar.Visible;
		end)
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
		GameTitle.ForegroundColor = Color3.new(1, 1, 1);
		GameTitle.FontSize = 36;
		GameTitle.Text = "";
	
		AuthorTitle.Parent = TeleportFrame;
		AuthorTitle.Position = UDim2.new(0, 0, 0, 46);
		AuthorTitle.Size = UDim2.new(1, 0, 1, 00);
		AuthorTitle.ForegroundColor = Color3.new(1, 1, 1);
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
else
	print("init.lua does not run on NetBlox Studio!");
end