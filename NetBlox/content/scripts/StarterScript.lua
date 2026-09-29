local CoreGui = game:GetService("CoreGui");
local RobloxGui = Instance.new("ScreenGui");

RobloxGui.Parent = CoreGui;

local Timeout = 3000;
local ConsoleArguments = game:ReadAllConsoleArguments();

function ConnectToServer()
    local NetworkClient = game:GetService("NetworkClient");

    local CA_Verbose = ConsoleArguments.unpaired["Verbose"] == true;
    local CA_Server = ConsoleArguments.paired["Server"];
    local CA_Port = ConsoleArguments.paired["Port"];

    if CA_Server == nil or CA_Port == nil then
        warn("No port or address specified, skipping...")
        return;
    end

    NetworkClient:ConnectToServer(CA_Server .. ":" .. CA_Port, Timeout);

    SpawnWindow("main", "Main Window", UDim2.new(0.5, 0, 0.5, 0), UDim2.new(0, 50, 0, 50), Vector2.new(0.5, 0.5));
end
function StartApplication()
    CoreGui:AddCoreScriptLocal("CoreScripts/Application", CoreGui);
end
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
function Mux() 
    local CA_Application = ConsoleArguments.unpaired["App"] == true;

    if CA_Application then
        StartApplication()
    else
        ConnectToServer()
    end
end

Mux();