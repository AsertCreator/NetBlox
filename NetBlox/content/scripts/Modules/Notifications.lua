--[[
	NetBlox's CoreScripts

	Notifications.lua - implements the notifications GUI
]]

local module = {};

local StarterGui = game:GetService("StarterGui");
local CoreGui = game:GetService("CoreGui");
local Debris = game:GetService("Debris");

function module.initNotify()
	StarterGui:RegisterSetCore("SendNotification", function(tabl)
		module.sendNotification(tabl.Title, tabl.Text);
	end)
end

function module.sendNotification(title, msg)
	local NotifyGui = Instance.new("ScreenGui");
	local NotifyFrame = Instance.new("Frame");
	local Title = Instance.new("TextLabel");
	local Text = Instance.new("TextLabel");

	Title.Position = UDim2.new(0, 0, 0.5, -9);
	Title.Size = UDim2.new(1, 0, 0, 18);
	Title.TextColor3 = Color3.new(1, 1, 1);
	Title.Text = title;
	Title.ZIndex = 101;
	Title.Parent = NotifyFrame;
	Title.FontSize = 18;

	Text.Position = UDim2.new(0, 0, 0.5, 11);
	Text.Size = UDim2.new(1, 0, 0, 18);
	Text.TextColor3 = Color3.new(1, 1, 1);
	Text.Text = msg;
	Text.ZIndex = 101;
	Text.Parent = NotifyFrame;

	NotifyFrame.Position = UDim2.new(0, 0, 0, 0);
	NotifyFrame.Size = UDim2.new(1, 0, 1, 0);
	NotifyFrame.BackgroundColor3 = Color3.new(0.8, 0.8, 0.8);
	NotifyFrame.BackgroundTransparency = 0.5;
	NotifyFrame.ZIndex = 100;
	NotifyFrame.Parent = NotifyGui;

	print("Notification sent, title: " .. title);
	print("Notification sent, message: " .. msg);

	NotifyGui.Parent = CoreGui;
	Debris:AddItem(NotifyGui, 4);
end

return module;