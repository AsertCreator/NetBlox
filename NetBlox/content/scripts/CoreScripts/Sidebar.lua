--[[
	NetBlox's CoreScripts

	Sidebar.lua - the main GUI of the game (version A)
	features a sidebar, a top bar and chat
]]

local Chat = game:GetService("Chat");
local PlatformService = game:GetService("PlatformService");
local UserInputService = game:GetService("UserInputService");
local CoreGui = game:GetService("CoreGui");
local RobloxGui = CoreGui:FindFirstChild("RobloxGui");
local Sidebar = Instance.new("Frame");
local BlackOut = Instance.new("Frame");
local TopbarFrame = Instance.new("Frame");
local TextField = Instance.new("TextBox");

local ChatModule = require(script.Parent.Modules.Chat);
local ServerControlModule = require(script.Parent.Modules.ServerControl);

local function createSidebar()
	-- here we init Sidebar
	local function createSidebarHeading()
		local TitleLabel = Instance.new("TextLabel");
		TitleLabel.Parent = Sidebar;
		TitleLabel.Position = UDim2.new(0, 30, 0, 0);
		TitleLabel.Size = UDim2.new(1, -60, 0, 70);
		TitleLabel.FontSize = TitleLabel.FontSize * 1.25;
		TitleLabel.BackgroundColor3 = Color3.new(1, 1, 1);
		TitleLabel.TextColor3 = Color3.new(1, 1, 1);
		TitleLabel.LeftAligned = true;
		TitleLabel.Text = game.Name;

		local AuthorLabel = Instance.new("TextLabel");
		AuthorLabel.Parent = Sidebar;
		AuthorLabel.Position = UDim2.new(0, 30, 0, 10);
		AuthorLabel.Size = UDim2.new(1, -60, 0, 100);
		AuthorLabel.FontSize = TitleLabel.FontSize * 0.75;
		AuthorLabel.BackgroundColor3 = Color3.new(1, 1, 1);
		AuthorLabel.TextColor3 = Color3.new(1, 1, 1);
		AuthorLabel.LeftAligned = true;
		AuthorLabel.Text = "by " .. game.AuthorName;
	end
	local function createSidebarButton(index, text, handler)
		local SidebarButton = Instance.new("TextButton");
		SidebarButton.Parent = Sidebar;
		SidebarButton.Position = UDim2.new(0, 30, 0, 60 + 40 * index);
		SidebarButton.Size = UDim2.new(1, -60, 0, 35);
		SidebarButton.BackgroundColor3 = Color3.new(1, 1, 1);
		SidebarButton.TextColor3 = Color3.new(1, 1, 1);
		SidebarButton.BackgroundTransparency = 0.7;
		SidebarButton.Text = text;
		SidebarButton.MouseButton1Click:Connect(handler);
		return SidebarButton;
	end
	local function createSidebarCloseButton()
		local function handlerSidebarCloseButton()
			Sidebar.Visible = false;
			BlackOut.Visible = false;
		end

		local SidebarCloseButton = Instance.new("TextButton");
		SidebarCloseButton.Parent = Sidebar;
		SidebarCloseButton.Position = UDim2.new(1, -30, 0, 20);
		SidebarCloseButton.Size = UDim2.new(0, 25, 0, 25);
		SidebarCloseButton.BackgroundColor3 = Color3.new(1, 1, 1);
		SidebarCloseButton.TextColor3 = Color3.new(1, 1, 1);
		SidebarCloseButton.BackgroundTransparency = 0.7;
		SidebarCloseButton.Text = "X";
		SidebarCloseButton.MouseButton1Click:Connect(handlerSidebarCloseButton);
		return SidebarCloseButton;
	end

	-- button handlers

	local MuteButton;

	local function handlerSidebarExitButton()
		Sidebar.Visible = false;
		BlackOut.Visible = false;

		ChatModule:SendSystemMessage("Goodbye!");

		game:Shutdown();
	end
	local function handlerSidebarResetButton() -- how do i debug this 😭
		Sidebar.Visible = false;
		BlackOut.Visible = false;

		local pls = game.Players;
		local lpr = pls.LocalPlayer;
		local chr = lpr.Character;

		if chr then
			chr.Humanoid:ResetCharacter();
		end
	end
	local function handlerSidebarMuteButton()
		if workspace.BirdAmbient then
			MuteButton.Text = "Unmute ambient sounds"
		else
			MuteButton.Text = "Mute ambient sounds"
		end;
		workspace.BirdAmbient = not workspace.BirdAmbient;
	end
	
	Sidebar.Name = "NBGSidebar";
	Sidebar.Parent = RobloxGui;
	Sidebar.Position = UDim2.new(0, 0, 0, 30);
	Sidebar.Size = UDim2.new(0, 400, 1, -30);
	Sidebar.BackgroundColor3 = Color3.new(0.2, 0.2, 0.2);
	Sidebar.BackgroundTransparency = 0.005;
	Sidebar.ZIndex = 3;
	Sidebar.Visible = false;

	createSidebarHeading();
	createSidebarCloseButton();

	local ExitButton = createSidebarButton(1, "Leave the game", handlerSidebarExitButton);
	local ResetButton = createSidebarButton(2, "Reset character", handlerSidebarResetButton);
	MuteButton = createSidebarButton(3, "Mute ambient sounds", handlerSidebarMuteButton);

	-- here are the buttons which are invisible, unless the game is started with a debugger

	if PlatformService.IsDebugged then
		local function handlerSidebarKillServerButton()
			ServerControlModule.killServer();
		end
		local KillServerButton = createSidebarButton(4, "Kill The Server", handlerSidebarKillServerButton);
	end

	local VersionText = Instance.new("TextLabel");
	VersionText.Parent = Sidebar;
	VersionText.Position = UDim2.new(0, 0, 1, -35);
	VersionText.Size = UDim2.new(1, 0, 0, 35);
	VersionText.TextColor3 = Color3.new(1, 1, 1);
	VersionText.Text = PlatformService:FormatVersion();
end
local function createBlackOut()
	BlackOut.Name = "NBGBlackOut";
	BlackOut.Parent = RobloxGui;
	BlackOut.Position = UDim2.new(0, 0, 0, 0);
	BlackOut.Size = UDim2.new(1, 0, 1, 0);
	BlackOut.BackgroundColor3 = Color3.new(0, 0, 0);
	BlackOut.BackgroundTransparency = 0.6;
	BlackOut.ZIndex = 0;
	BlackOut.Visible = false;
end
local function createTopbar()
	TopbarFrame.Name = "NBGTopbar";
	TopbarFrame.Parent = RobloxGui;
	TopbarFrame.Position = UDim2.new(0, 0, 0, 0);
	TopbarFrame.Size = UDim2.new(1, 0, 0, 30);
	TopbarFrame.BackgroundColor3 = Color3.new(0.1, 0.1, 0.1);
	TopbarFrame.BackgroundTransparency = 0.5;

	local MenuButton = Instance.new("ImageButton");
	MenuButton.Parent = TopbarFrame;
	MenuButton.Position = UDim2.new(0, 0, 0, 0);
	MenuButton.Size = UDim2.new(0, 30, 0, 30);
	MenuButton.Image = "rbxasset://textures/menu.png";
	MenuButton.ZIndex = 4;
	MenuButton.MouseButton1Click:Connect(function()
		Sidebar.Visible = not Sidebar.Visible;
		BlackOut.Visible = Sidebar.Visible;
	end)

	local PlayerName = Instance.new("TextLabel");
	PlayerName.Parent = TopbarFrame;
	PlayerName.Position = UDim2.new(0, 33, 0, 0);
	PlayerName.Size = UDim2.new(0, 150, 1, 0);
	PlayerName.LeftAligned = true;
	PlayerName.TextColor3 = Color3.new(1, 1, 1);
	PlayerName.ZIndex = 4;

	local PlayerType = Instance.new("TextLabel");
	PlayerType.Parent = TopbarFrame;
	PlayerType.Position = UDim2.new(0, 153, 0, 0);
	PlayerType.Size = UDim2.new(0, 150, 1, 0);
	PlayerType.LeftAligned = true;
	PlayerType.TextColor3 = Color3.new(0.75, 0.75, 0.75);
	PlayerType.ZIndex = 4;
	
	PlayerName.Text = game.Players.LocalPlayer.Name;
	if game.Players.LocalPlayer.Guest then
		PlayerType.Text = "a guest account";
	else -- not really lol
		PlayerType.Text = ">13 account";
	end
end
local function createUserInputHandler()
	UserInputService.KeyboardPress:Connect(function(x) 
		if x == 256 then
			Sidebar.Visible = not Sidebar.Visible;
			BlackOut.Visible = Sidebar.Visible;
		elseif x == 72 then
			nextnum = nextnum + 1;
			game:GetService("Chat"):SendMessage("test" .. tostring(nextnum));
		elseif x == 299 then
			CoreGui:TakeScreenshot();
		end
	end)
end

local nextnum = 0;

local function awaitForPlayers()
	while true do
		if game:FindFirstChild("Players") then
			if game.Players.LocalPlayer then
				createBlackOut();
				createTopbar();
				createSidebar();
				createUserInputHandler();

				ChatModule.initChat();
				ChatModule.showChat();

				local defaultTitle = PlatformService:GetDefaultWindowTitle();
				PlatformService:SetWindowTitle(defaultTitle .. " - " .. game.Name);

				Chat:SendSystemMessage("Welcome to NetBlox!");
				Chat:SendSystemMessage("You are playing " .. game.Name);

				-- backpack.setBackpackInstance(game.Players.LocalPlayer.Backpack)
				-- backpack.mount(script.Parent)

				break;
			end
		end

		wait(0.5);
	end
end

awaitForPlayers();