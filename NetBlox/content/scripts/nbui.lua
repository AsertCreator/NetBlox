local PlatformService = game:GetService("PlatformService")
local CoreGui = game:GetService("CoreGui")
local sg = Instance.new("ScreenGui");
local rf = Instance.new("Image");

sg.Parent = CoreGui;
rf.Parent = sg;
rf.Position = UDim2.new(0, 50, 0, 50)
rf.Size = UDim2.new(0, 50, 0, 50)