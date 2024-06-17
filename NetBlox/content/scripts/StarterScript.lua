-- init script

local PlatformService = game:GetService("PlatformService");
local StarterGui = game:GetService("StarterGui");
local CoreGui = game:GetService("CoreGui");
local RobloxGui = CoreGui:FindFirstChild("RobloxGui");

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
		local Sidebar = Instance.new("Frame");
		local BlackOut = Instance.new("Frame");
		local TopbarFrame = Instance.new("Frame");
		local MenuButton = Instance.new("ImageButton");
		
		BlackOut.Name = "NBGBlackOut";
		BlackOut.Parent = RobloxGui;
		BlackOut.Position = UDim2.new(0, 0, 0, 0);
		BlackOut.Size = UDim2.new(1, 0, 1, 0);
		BlackOut.BackgroundColor3 = Color3.new(0, 0, 0);
		BlackOut.BackgroundTransparency = 0.6;
		BlackOut.ZIndex = 2;
		BlackOut.Visible = false;

		Sidebar.Name = "NBGSidebar";
		Sidebar.Parent = RobloxGui;
		Sidebar.Position = UDim2.new(0, 0, 0, 30);
		Sidebar.Size = UDim2.new(0, 400, 1, -30);
		Sidebar.BackgroundColor3 = Color3.new(0.2, 0.2, 0.2);
		Sidebar.ZIndex = 1;
		Sidebar.Visible = false;
		
		TopbarFrame.Name = "NBGTopbar";
		TopbarFrame.Parent = RobloxGui;
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
			Sidebar.Visible = false;
			BlackOut.Visible = false;

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
		ResetButton.MouseButton1Click:Connect(function() -- how do i debug this 😭
			Sidebar.Visible = false;
			BlackOut.Visible = false;

			local pls = game.Players;
			local lpr = pls.LocalPlayer;
			local chr = lpr.Character;
			chr.Health = 0;
		end)
		
		local ResetButton = Instance.new("TextButton");
		ResetButton.Parent = Sidebar;
		ResetButton.Position = UDim2.new(0, 30, 0, 30 + 40 * 3);
		ResetButton.Size = UDim2.new(1, -60, 0, 35);
		ResetButton.BackgroundColor3 = Color3.new(1, 1, 1);
		ResetButton.TextColor3 = Color3.new(1, 1, 1);
		ResetButton.BackgroundTransparency = 0.7;
		ResetButton.Text = "Damage yourself";
		ResetButton.MouseButton1Click:Connect(function()
			local pls = game.Players;
			local lpr = pls.LocalPlayer;
			local chr = lpr.Character;
			chr.Health = chr.Health - 1;
		end)
		
		local CloseButton = Instance.new("TextButton");
		CloseButton.Parent = Sidebar;
		CloseButton.Position = UDim2.new(0, 30, 0, 30 + 40 * 4);
		CloseButton.Size = UDim2.new(1, -60, 0, 35);
		CloseButton.BackgroundColor3 = Color3.new(1, 1, 1);
		CloseButton.TextColor3 = Color3.new(1, 1, 1);
		CloseButton.BackgroundTransparency = 0.7;
		CloseButton.Text = "Close sidebar";
		CloseButton.MouseButton1Click:Connect(function()
			Sidebar.Visible = false;
			BlackOut.Visible = false;
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
			BlackOut.Visible = Sidebar.Visible;
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

		CoreGui.OnTeleportStarts:Connect(function(placename, authorname, pid, uid)
			TeleportGui.Enabled = true;
			GameTitle.Text = placename;
			AuthorTitle.Text = "by " .. authorname;
		end);
		CoreGui.OnTeleportEnds:Connect(function()
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