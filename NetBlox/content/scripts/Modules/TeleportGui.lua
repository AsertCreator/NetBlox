-- teleport gui

local PlatformService = game:GetService("PlatformService");
local CoreGui = game:GetService("CoreGui");
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
TeleportFrame.ZIndex = 10000;
TeleportFrame.BackgroundColor3 = Color3.new(0.222, 0.222, 0.222);

GameTitle.Parent = TeleportFrame;
GameTitle.Position = UDim2.new(0, 0, 0, 0);
GameTitle.Size = UDim2.new(1, 0, 1, 0);
GameTitle.TextColor3 = Color3.new(1, 1, 1);
GameTitle.FontSize = 36;
GameTitle.ZIndex = 10001;
GameTitle.Text = "";

AuthorTitle.Parent = TeleportFrame;
AuthorTitle.Position = UDim2.new(0, 0, 0, 46);
AuthorTitle.Size = UDim2.new(1, 0, 1, 00);
AuthorTitle.TextColor3 = Color3.new(1, 1, 1);
AuthorTitle.FontSize = 24;
AuthorTitle.ZIndex = 10001;
AuthorTitle.Text = "";

return {
	show = function(placename, authorname, pid, uid)
		TeleportGui.Enabled = true;
		GameTitle.Text = placename;
		AuthorTitle.Text = "by " .. authorname;
	end,
	hide = function()
		TeleportGui.Enabled = false;
	end
};