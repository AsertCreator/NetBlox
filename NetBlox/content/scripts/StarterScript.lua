--[[
	NetBlox's CoreScripts

	StarterScript.lua - bootstraps the client-side of the game
]]

local PlatformService = game:GetService("PlatformService");
local ScriptContext = game:GetService("ScriptContext");
local StarterGui = game:GetService("StarterGui");
local CoreGui = game:GetService("CoreGui");
local Debris = game:GetService("Debris");
local RobloxGui = CoreGui:FindFirstChild("RobloxGui");

local guivar = game:GetFastInt("FIntDefaultUIVariant") or 1;

if game:GetFastFlag("FFlagHideCoreGui") then
	return;
end

-- initializes notifications
function initNotify()
	local notifications = require(script.Parent.Modules.Notifications);
	notifications.initNotify();
end
-- initializes in-game GUI
function initIGG()
	if guivar == 1 then
		ScriptContext:AddCoreScriptLocal("CoreScripts/Sidebar", RobloxGui);
	end
end
-- initializes teleport GUI
function initTUI()
	local telgui = require(script.Parent.Modules.TeleportGui);
	print(telgui);
	CoreGui.OnTeleportStarts:Connect(telgui.show);
	CoreGui.OnTeleportEnds:Connect(telgui.hide);
end

if not game.IsApplication then
	initNotify();
	initIGG();
	initTUI();
end

print("Platform initialized");

PlatformService:BeginQueuedTeleport();