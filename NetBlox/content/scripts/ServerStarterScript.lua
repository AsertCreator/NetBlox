local CoreGui = game:GetService("CoreGui");
local ScriptContext = game:GetService("ScriptContext");
local PlatformService = game:GetService("PlatformService");
local RobloxGui = Instance.new("ScreenGui");

RobloxGui.Parent = CoreGui;
RobloxGui.Name = "RobloxGui";

CoreGui:LoadModules();

local ConnectionTimeout = 3000;

function main()
    local NetworkServer = game:GetService("NetworkServer");
    local ConsoleArguments = game:ReadAllConsoleArguments();

    local CA_DisableDefaultServerBehavior = ConsoleArguments.unpaired["DisableDefaultServerBehavior"] == true;
    local CA_Verbose = ConsoleArguments.unpaired["Verbose"] == true;
    local CA_MaxPlayers = tonumber(ConsoleArguments.paired["MaxPlayers"]) or 10;
    local CA_Port = ConsoleArguments.paired["Port"];

    if CA_Port == nil then
        warn("No port specified, ending...")
        game:Shutdown();
        return;
    end

    NetworkServer.FirstMessageTimeout = ConnectionTimeout;
    NetworkServer.MaxPlayerCount = CA_MaxPlayers;

    NetworkServer:StartServer("0.0.0.0:" .. CA_Port);

    if not CA_DisableDefaultServerBehavior then
        ScriptContext:AddCoreScriptLocal("CoreScripts/ServerGameplayInit", RobloxGui);
    end
end

main()