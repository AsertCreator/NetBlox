--[[
	NetBlox's CoreScripts

	Cursor.lua - API for controlling the cursor
]]

local module = {};

local RunService = game:GetService("RunService");
local CoreGui = game:GetService("CoreGui");

local function checkIfClient()
	-- we probably shouldn't invoke rendering related functions on servers to avoid
	-- exceesive memory footprint (raylib takes 300 mbs for no reason :sob:)
	-- engine already prevents rendering a lot of things on servers
	-- using things like Color is ok btw, these don't invoke raylib's functions and
	-- don't make it actually load into memory and initialize

	if not RunService:IsClient() then
		error("Cannot call Cursor.lua functions on servers!")
	end
end

function module.setNeutral()
	checkIfClient();
	CoreGui:SetCursorTo("rbxasset://textures/cursorNeutral.png")
end
function module.setError()
	checkIfClient();
	CoreGui:SetCursorTo("rbxasset://textures/cursorError.png")
end
function module.setSuccess()
	checkIfClient();
	CoreGui:SetCursorTo("rbxasset://textures/cursorSuccess.png")
end
function module.reset()
	checkIfClient();
	CoreGui:SetCursorTo("rbxasset://textures/cursorNeutral.png")
end

function module.hide()
	checkIfClient();
	CoreGui:SetCursorVisible(false);
end
function module.show()
	checkIfClient();
	CoreGui:SetCursorVisible(true);
end

return module;