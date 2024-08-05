-- gui version A (sidebar one)

local PlatformService = game:GetService("PlatformService");
local UserInputService = game:GetService("UserInputService");
local CoreGui = game:GetService("CoreGui");
local RobloxGui = CoreGui:FindFirstChild("RobloxGui");
local Sidebar = Instance.new("Frame");
local BlackOut = Instance.new("Frame");
local TopbarFrame = Instance.new("Frame");
local ChatFrame = Instance.new("ChatFrame");

BlackOut.Name = "NBGBlackOut";
BlackOut.Parent = RobloxGui;
BlackOut.Position = UDim2.new(0, 0, 0, 0);
BlackOut.Size = UDim2.new(1, 0, 1, 0);
BlackOut.BackgroundColor3 = Color3.new(0, 0, 0);
BlackOut.BackgroundTransparency = 0.6;
BlackOut.ZIndex = 0;
BlackOut.Visible = false;

ChatFrame.Name = "NBGChatFrame";
ChatFrame.Parent = RobloxGui;
ChatFrame.Position = UDim2.new(0, 0, 0, 30);
ChatFrame.Size = UDim2.new(1, 0, 0, 30);
ChatFrame.BackgroundColor3 = Color3.new(0, 0, 0);
ChatFrame.BackgroundTransparency = 0.6;
ChatFrame.ZIndex = 1;
ChatFrame.Visible = false;

Sidebar.Name = "NBGSidebar";
Sidebar.Parent = RobloxGui;
Sidebar.Position = UDim2.new(0, 0, 0, 30);
Sidebar.Size = UDim2.new(0, 400, 1, -30);
Sidebar.BackgroundColor3 = Color3.new(0.2, 0.2, 0.2);
Sidebar.BackgroundTransparency = 0.005;
Sidebar.ZIndex = 3;
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
TitleLabel.Position = UDim2.new(0, 30, 0, 0);
TitleLabel.Size = UDim2.new(1, -60, 0, 70);
TitleLabel.FontSize = TitleLabel.FontSize * 1.25;
TitleLabel.BackgroundColor3 = Color3.new(1, 1, 1);
TitleLabel.TextColor3 = Color3.new(1, 1, 1);
TitleLabel.LeftAligned = true;
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

local MuteButton = Instance.new("TextButton");
MuteButton.Parent = Sidebar;
MuteButton.Position = UDim2.new(0, 30, 0, 30 + 40 * 5);
MuteButton.Size = UDim2.new(1, -60, 0, 35);
MuteButton.BackgroundColor3 = Color3.new(1, 1, 1);
MuteButton.TextColor3 = Color3.new(1, 1, 1);
MuteButton.BackgroundTransparency = 0.7;
MuteButton.Text = "Mute ambient sounds";
MuteButton.MouseButton1Click:Connect(function()
	if workspace.BirdAmbient then
		MuteButton.Text = "Unmute ambient sounds"
	else
		MuteButton.Text = "Mute ambient sounds"
	end;
	workspace.BirdAmbient = not workspace.BirdAmbient;
end)

local PressButton = Instance.new("TextButton");
PressButton.Parent = Sidebar;
PressButton.Position = UDim2.new(0, 30, 0, 30 + 40 * 6);
PressButton.Size = UDim2.new(1, -60, 0, 35);
PressButton.BackgroundColor3 = Color3.new(1, 1, 1);
PressButton.TextColor3 = Color3.new(1, 1, 1);
PressButton.BackgroundTransparency = 0.7;
PressButton.Text = "Create new dummy TS job";
PressButton.MouseButton1Click:Connect(function()
	for i = 1,10 do
		spawn(function()
			print("Hi, from a dummy")
			while true do
				printidentity()
				wait(1)
			end
		end)
	end
end)

local VersionText = Instance.new("TextLabel");
VersionText.Parent = Sidebar;
VersionText.Position = UDim2.new(0, 10, 1, -35);
VersionText.Size = UDim2.new(1, 0, 0, 30);
VersionText.TextColor3 = Color3.new(1, 1, 1);
VersionText.LeftAligned = true;
VersionText.Text = PlatformService:FormatVersion();

-- here we stop init Sidebar

local MenuButton = Instance.new("ImageButton");
MenuButton.Parent = TopbarFrame;
MenuButton.Position = UDim2.new(0, 0, 0, 0);
MenuButton.Size = UDim2.new(0, 30, 0, 30);
MenuButton.Image = "rbxasset://textures/menu.png";
MenuButton.ZIndex = 4;
MenuButton.MouseButton1Click:Connect(function()
	TitleLabel.Text = game.Name; -- lol
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

UserInputService.KeyboardPress:Connect(function(x) 
	if x == 256 then
		TitleLabel.Text = game.Name; -- lol
		Sidebar.Visible = not Sidebar.Visible;
		BlackOut.Visible = Sidebar.Visible;
	elseif x == 72 then
		game:GetService("Chat"):SendMessage("test");
	elseif x == 299 then
		CoreGui:TakeScreenshot();
	end
end)

while true do
	if game:FindFirstChild("Players") then
		if game.Players.LocalPlayer then
			PlayerName.Text = game.Players.LocalPlayer.Name;
			if game.Players.LocalPlayer.Guest then
				PlayerType.Text = "a guest account";
			else -- not really lol
				PlayerType.Text = ">13 account";
			end
		end
	end
	wait(1);
end