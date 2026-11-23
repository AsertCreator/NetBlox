local CoreGui = game:GetService("CoreGui");
local Players = game:GetService("Players");
local ScriptContext = game:GetService("ScriptContext");
local PlatformService = game:GetService("PlatformService");
local CoreReplicatedStorage = game:GetService("CoreReplicatedStorage");
local RobloxGui = CoreGui.RobloxGui;

-- Character management

while Players.LocalPlayer == nil do
    wait()
end

local LocalPlayer = Players.LocalPlayer;
LocalPlayer.CharacterAdded:Connect(function (character)
    local Camera = workspace:FindFirstChildOfClass("Camera");
    local Head = character:FindFirstChild("Head");

    if Head == nil then
        return;
    end

    Camera.CameraSubject = character;
    Camera.CFrame = CFrame.new(head.Position + Vector3.new(20, 0, 20));
end)

_G.LocalPlayerCached = LocalPlayer;

local CharacterRequestEvent = CoreReplicatedStorage:WaitForChild("CharacterRequestEvent");
CharacterRequestEvent:FireServer();

--[[

function SpawnWindow(id, name, position, size, anchor)
    if (RobloxGui:FindFirstChild(id) ~= nil) then
        return;
    end

    local RobloxGuiFrame = Instance.new("Frame");

    RobloxGuiFrame.Name = id;
    RobloxGuiFrame.Parent = RobloxGui;
    RobloxGuiFrame.AnchorPoint = anchor;
    RobloxGuiFrame.Position = position;
    RobloxGuiFrame.Size = size;
    RobloxGuiFrame.BorderSizePixel = 1;
    RobloxGuiFrame.BackgroundColor3 = Color3.new(0.1, 0.1, 0.13);
    RobloxGuiFrame.BackgroundTransparency = 0.2;

    return RobloxGuiFrame;
end

]]