--[[
	NetBlox's CoreScripts

	Chat.lua - implements the chat GUI
]]

local module = {};

local CoreGui = game:GetService("CoreGui");
local RobloxGui = CoreGui:FindFirstChild("RobloxGui");

local ChatContainerFrame;
local ChatTextField;
local ChatSendButton;
local ChatFrame;

function module.initChat()
	ChatContainerFrame = Instance.new("Frame");
	ChatFrame = Instance.new("ChatFrame");

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
	
	-- ChatTextField.Name = "NBGChatTextField";
	-- ChatTextField.Parent = ChatContainerFrame;
	-- ChatTextField.Position = UDim2.new(0.25, 0, 0.5, 0);
	-- ChatTextField.Size = UDim2.new(0, 400, 0, 30);
	-- ChatTextField.BackgroundColor3 = Color3.new(0.2, 0.2, 0.2);
	-- ChatTextField.BackgroundTransparency = 0.005;
	-- ChatTextField.ZIndex = 1;
	-- ChatTextField.Visible = true;
end
function module.showChat()
	ChatContainerFrame.Visible = true;
end
function module.hideChat()
	ChatContainerFrame.Visible = false;
end

return module;