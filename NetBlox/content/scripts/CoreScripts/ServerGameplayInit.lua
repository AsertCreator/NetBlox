local CoreGui = game:GetService("CoreGui");
local ScriptContext = game:GetService("ScriptContext");
local PlatformService = game:GetService("PlatformService");
local CoreReplicatedStorage = game:GetService("CoreReplicatedStorage");
local RobloxGui = CoreGui.RobloxGui;

local Modules = CoreGui.Modules;
local HumanoidMaker = require(Modules.HumanoidMaker);

-- Character management

local function HandleCharacterRequest(player)
    if player.Character ~= nil then
        return;
    end

    local humanoid = HumanoidMaker.CreateHumanoidForPlayer(player);
    humanoid.Name = player.Name;
    humanoid.Parent = workspace;

    humanoid:PivotTo(CFrame.new(0, 60, 0))

    player.Character = humanoid;
end

local CharacterRequestEvent = Instance.new("RemoteEvent");
CharacterRequestEvent.Name = "CharacterRequestEvent";
CharacterRequestEvent.Parent = CoreReplicatedStorage;
CharacterRequestEvent.OnServerEvent:Connect(HandleCharacterRequest);