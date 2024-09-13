local CoreGui = game:GetService("CoreGui")
local RobloxGui = CoreGui.RobloxGui;
local ScriptContext = game:GetService("ScriptContext")

print("The application is about to start...")

workspace = game:GetService("Workspace")
players = game:GetService("Players")
player = players:CreateApplicationPlayer();

local frame = Instance.new("Frame")
frame.Parent = RobloxGui;
frame.Size = UDim2.new(1, 0, 1, 0)
frame.Position = UDim2.new(0, 0, 0, 0)
frame.BackgroundColor3 = Color3.new(
	45 / 255, 
	45 / 255, 
	45 / 255)

local textbox = Instance.new("TextLabel")
textbox.Parent = frame;
textbox.Size = UDim2.new(1, 0, 1, 0)
textbox.Position = UDim2.new(0, 0, 0, 0)
textbox.Text = "NetBlox Application";
textbox.TextColor3 = Color3.new(1, 1, 1)

local welcoming = Instance.new("Part")
welcoming.Parent = workspace;
welcoming.Size = Vector3.new(50, 5, 50)
welcoming.Color3 = Color3.new(0, 0.5, 0)

