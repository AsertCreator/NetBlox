-- init script

local PlatformService = game:GetService("PlatformService");
local StarterGui = game:GetService("StarterGui");
local CoreGui = game:GetService("CoreGui");

if not PlatformService.IsStudio then
	function sendNotification(title, msg)
		local NotifyGui = Instance.new("ScreenGui");
		local NotifyFrame = Instance.new("Frame");
		local Title = Instance.new("TextLabel");
		local Text = Instance.new("TextLabel");
	
		Title.Parent = NotifyFrame;
		Text.Parent = NotifyFrame;
		NotifyFrame.Parent = NotifyGui;
		NotifyGui.Parent = CoreGui;


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
		local TopbarGui = Instance.new("ScreenGui");
		local Sidebar = Instance.new("Frame");
		local TopbarFrame = Instance.new("Frame");
		local MenuButton = Instance.new("ImageButton");
	
		TopbarGui.Name = "TopbarGui";
		TopbarGui.Parent = CoreGui;
		TopbarGui.Enabled = true;
		
		Sidebar.Parent = TopbarGui;
		Sidebar.Position = UDim2.new(0, 0, 0, 30);
		Sidebar.Size = UDim2.new(0, 400, 1, -30);
		Sidebar.BackgroundColor3 = Color3.new(0.1, 0.1, 0.1);
		Sidebar.BackgroundTransparency = 0.2;
		Sidebar.ZIndex = 1;
		Sidebar.Visible = false;

		TopbarFrame.Parent = TopbarGui;
		TopbarFrame.Position = UDim2.new(0, 0, 0, 0);
		TopbarFrame.Size = UDim2.new(1, 0, 0, 30);
		TopbarFrame.BackgroundColor3 = Color3.new(0.1, 0.1, 0.1);
		TopbarFrame.BackgroundTransparency = 0.5;

		-- here we init Sidebar
		
		local TitleLabel = Instance.new("TextLabel");
		TitleLabel.Parent = Sidebar;
		TitleLabel.Position = UDim2.new(0, 30, 0, 30 + 40 * 0);
		TitleLabel.Size = UDim2.new(1, -60, 0, 35);
		TitleLabel.BackgroundColor3 = Color3.new(1, 1, 1);
		TitleLabel.TextColor3 = Color3.new(1, 1, 1);
		TitleLabel.Text = game.Name;

		local ExitButton = Instance.new("TextButton");
		ExitButton.Parent = Sidebar;
		ExitButton.Position = UDim2.new(0, 30, 0, 30 + 40 * 1);
		ExitButton.Size = UDim2.new(1, -60, 0, 35);
		ExitButton.BackgroundColor3 = Color3.new(1, 1, 1);
		ExitButton.TextColor3 = Color3.new(1, 1, 1);
		ExitButton.BackgroundTransparency = 0.7;
		ExitButton.Text = "Leave the game";
		ExitButton.MouseButton1Click:Connect(function()
			game:Shutdown();
		end)
		
		local ResetButton = Instance.new("TextButton");
		ResetButton.Parent = Sidebar;
		ResetButton.Position = UDim2.new(0, 30, 0, 30 + 40 * 2);
		ResetButton.Size = UDim2.new(1, -60, 0, 35);
		ResetButton.BackgroundColor3 = Color3.new(1, 1, 1);
		ResetButton.TextColor3 = Color3.new(1, 1, 1);
		ResetButton.BackgroundTransparency = 0.7;
		ResetButton.Text = "Reset character";
		ResetButton.MouseButton1Click:Connect(function()
			game.Players.LocalPlayer.Character:Destroy();
		end)
		
		local CloseButton = Instance.new("TextButton");
		CloseButton.Parent = Sidebar;
		CloseButton.Position = UDim2.new(0, 30, 0, 30 + 40 * 3);
		CloseButton.Size = UDim2.new(1, -60, 0, 35);
		CloseButton.BackgroundColor3 = Color3.new(1, 1, 1);
		CloseButton.TextColor3 = Color3.new(1, 1, 1);
		CloseButton.BackgroundTransparency = 0.7;
		CloseButton.Text = "Close sidebar";
		CloseButton.MouseButton1Click:Connect(function()
			Sidebar.Visible = false;
		end)
		
		local VersionText = Instance.new("TextLabel");
		VersionText.Parent = Sidebar;
		VersionText.Position = UDim2.new(0, 0, 1, -60);
		VersionText.Size = UDim2.new(1, 0, 0, 30);
		VersionText.TextColor3 = Color3.new(1, 1, 1);
		VersionText.Text = PlatformService:FormatVersion();

		-- here we stop init Sidebar
		
		MenuButton.Parent = TopbarFrame;
		MenuButton.Position = UDim2.new(0, 0, 0, 0);
		MenuButton.Size = UDim2.new(0, 30, 0, 30);
		MenuButton.FilePath = "rbxasset://textures/menu.png";
		MenuButton.MouseButton1Click:Connect(function()
			TitleLabel.Text = game.Name; -- lol
			Sidebar.Visible = not Sidebar.Visible;
		end)
	end
	-- initializes teleport GUI
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
		TeleportFrame.BackgroundColor3 = Color3.new(0.222, 0.222, 0.222);

		GameTitle.Parent = TeleportFrame;
		GameTitle.Position = UDim2.new(0, 0, 0, 0);
		GameTitle.Size = UDim2.new(1, 0, 1, 0);
		GameTitle.TextColor3 = Color3.new(1, 1, 1);
		GameTitle.FontSize = 36;
		GameTitle.Text = "";
	
		AuthorTitle.Parent = TeleportFrame;
		AuthorTitle.Position = UDim2.new(0, 0, 0, 46);
		AuthorTitle.Size = UDim2.new(1, 0, 1, 00);
		AuthorTitle.TextColor3 = Color3.new(1, 1, 1);
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

	-- initialized rendering(???)
	function initRender()
		PlatformService:SetRenderFlag("Msaa4xHint");
	end
	
	initStatus();
	initRender();
	initNotify();
	initIGG();
	initTUI();

	print("Platform initialized");

	PlatformService:BeginQueuedTeleport();
else
	print("init.lua does not run on NetBlox Studio!");
end