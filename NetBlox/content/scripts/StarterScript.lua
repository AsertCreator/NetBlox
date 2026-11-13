--[[
    This script is first Lua code that runs on the client. It bootstraps the gameplay management and CoreGui
]]

local CoreGui = game:GetService("CoreGui");
local ScriptContext = game:GetService("ScriptContext");
local PlatformService = game:GetService("PlatformService");
local ConsoleArguments = game:ReadAllConsoleArguments();
local RobloxGui = Instance.new("ScreenGui");

RobloxGui.Parent = CoreGui;
RobloxGui.Name = "RobloxGui";

CoreGui:LoadModules();

local ConnectionTimeout = 3000;

function ConnectToServer()
    local NetworkClient = game:GetService("NetworkClient");

    if ConsoleArguments.paired["Ticket"] then
        local CA_Ticket = ConsoleArguments.paired["Ticket"];
        CA_Ticket = PlatformService:DecodeBase64(CA_Ticket);

        NetworkClient:ConnectWithTicket(CA_Ticket);
    else
        local CA_DisableDefaultClientBehavior = ConsoleArguments.unpaired["DisableDefaultClientBehavior"] == true;
        local CA_Verbose = ConsoleArguments.unpaired["Verbose"] == true;
        local CA_Server = ConsoleArguments.paired["Server"];
        local CA_Port = ConsoleArguments.paired["Port"];

        if CA_Server == nil or CA_Port == nil then
            warn("No port or address specified, skipping...")
            return;
        end

        NetworkClient:ConnectToServer(CA_Server .. ":" .. CA_Port, ConnectionTimeout);
    end

    if not CA_DisableDefaultClientBehavior then
        ScriptContext:AddCoreScriptLocal("CoreScripts/ClientGameplayInit", RobloxGui);
    end
end
function StartApplication()
    ScriptContext:AddCoreScriptLocal("CoreScripts/Application", RobloxGui);
end

local CA_Application = ConsoleArguments.unpaired["App"] == true;

if CA_Application then
    StartApplication()
else
    ConnectToServer()
end