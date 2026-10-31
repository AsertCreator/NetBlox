--[[
    this script does something don't ask
]]

local remoteFunction = Instance.new("RemoteFunction");
remoteFunction.Parent = game.Workspace;
remoteFunction.OnServerInvoke = function(player, a)
    print("fcka");
    wait(3);
    return a + 5;
end

local remoteEvent = Instance.new("RemoteEvent");
remoteEvent.Parent = game.Workspace;
remoteEvent.OnServerEvent:Connect(function(player, a)
    game:Shutdown();
end);

local localScriptContent = "";
localScriptContent = localScriptContent .. "local func = game.Workspace:WaitForChild(\"RemoteFunction\");\n";
localScriptContent = localScriptContent .. "local event = game.Workspace:WaitForChild(\"RemoteEvent\");\n";
localScriptContent = localScriptContent .. "local response = tostring(func:InvokeServer(5));\n";
localScriptContent = localScriptContent .. "local message = Instance.new(\"Message\");\n";
localScriptContent = localScriptContent .. "message.Text = response;\n";
localScriptContent = localScriptContent .. "message.Parent = workspace;\n";
localScriptContent = localScriptContent .. "wait(3);\n";
localScriptContent = localScriptContent .. "message:Destroy();\n";
localScriptContent = localScriptContent .. "event:FireServer();\n";

local localScript = Instance.new("LocalScript");
localScript.Parent = game.Workspace;
localScript.Source = localScriptContent;