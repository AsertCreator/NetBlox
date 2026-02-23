--[[
	NetBlox's CoreScripts

	ServerControl.lua - API for controlling the debugged server from the client
]]

local module = {};

local RunService = game:GetService("RunService");
local PlatformService = game:GetService("PlatformService");

local function checkIfClient()
	-- we shouldn't invoke ServerControl on servers because we can't.
	-- this is because this is supposed to be invoked from the client lol

	if not RunService:IsClient() then
		error("Cannot call ServerControl.lua functions on servers!")
	end
end

-- these send an rpc to the server to do things. but if the server is not
-- currently being debugged, then it will do nothing.

function module.killServer()
	checkIfClient();
	PlatformService:SendServerControlPacket(1, "", "");
end
function module.runCode(code)
	checkIfClient();
	PlatformService:SendServerControlPacket(2, code, "");
end

return module;