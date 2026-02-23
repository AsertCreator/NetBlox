--[[
	NetBlox's CoreScripts

	StarterScript.lua - bootstraps the client-side of the game
]]

local PlatformService = game:GetService("PlatformService");
local ScriptContext = game:GetService("ScriptContext");
local CoreGui = game:GetService("CoreGui");
local RobloxGui = CoreGui:FindFirstChild("RobloxGui");

local Cursor = require(script.Parent.Modules.Cursor);
local TeleportGui = require(script.Parent.Modules.TeleportGui);
local Notifications = require(script.Parent.Modules.Notifications);

print("Essential CoreScript modules loaded, presumably");

local FIntDefaultUIVariant = game:GetFastInt("FIntDefaultUIVariant", 1);

local FFlagHideCoreGui = game:GetFastFlag("FFlagHideCoreGui", false);
local FFlagAllowServerControl = game:GetFastFlag("FFlagAllowServerControl", true);
local FFlagAllowUsingNotifications = game:GetFastFlag("FFlagAllowUsingNotifications", true);
local FFlagAllowUsingTeleportGui = game:GetFastFlag("FFlagAllowUsingTeleportGui", true);

if FFlagHideCoreGui then
	return;
end

-- initializes notifications
local function initNotifications()
	if FFlagAllowUsingNotifications then
		Notifications.initNotify();
	end
end

-- initializes in-game GUI
local function initInGameGui()
	if FIntDefaultUIVariant == 1 then
		ScriptContext:AddCoreScriptLocal("CoreScripts/Sidebar", RobloxGui);
	end
	Cursor.setNeutral();
end

-- initializes teleport GUI
local function initTeleportGui()
	CoreGui.OnTeleportStarts:Connect(TeleportGui.beginTeleportGuiShowing);
	CoreGui.OnTeleportEnds:Connect(TeleportGui.beginTeleportGuiHiding);
end

if not game.IsApplication then
	initNotifications();
	initInGameGui();
	initTeleportGui();

	print("Client platform initialized, performing queued teleport...");

	PlatformService:BeginQueuedTeleport();
else
	print("Initializing application...");
end