--[[
	NetBlox's CoreScripts

	Chat.lua - implements the chat GUI
]]

local CoreGui = game:GetService("CoreGui");
local RobloxGui = CoreGui:FindFirstChild("RobloxGui");
local ChatContainerFrame = Instance.new("Frame");
local ChatFrame = Instance.new("ChatFrame");

ChatContainerFrame.Name = "NBGChatContainerFrame";
ChatContainerFrame.Parent = RobloxGui;
ChatContainerFrame.Position = UDim2.new(0, 0, 0, 30);
ChatContainerFrame.Size = UDim2.new(0.25, 0, 0.2, 0);
ChatContainerFrame.BackgroundColor3 = Color3.new(0.1, 0.1, 0.1);
ChatContainerFrame.BackgroundTransparency = 0.75;
ChatContainerFrame.ZIndex = 1;
ChatContainerFrame.Visible = true;

ChatFrame.Name = "NBGChatFrame";
ChatFrame.Parent = ChatContainerFrame;
ChatFrame.Position = UDim2.new(0, 5, 0, 5);
ChatFrame.Size = UDim2.new(1, -10, 1, -40);
ChatFrame.BackgroundTransparency = 1;
ChatFrame.ZIndex = 1;
ChatFrame.Visible = true;

return {
	showChat = function()
		ChatContainerFrame.Visible = true;
	end,
	hideChat = function()
		ChatContainerFrame.Visible = false;
	end,
}