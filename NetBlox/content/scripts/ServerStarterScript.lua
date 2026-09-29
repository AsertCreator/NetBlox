local Timeout = 3000;

function main()
    local NetworkServer = game:GetService("NetworkServer");
    local ConsoleArguments = game:ReadAllConsoleArguments();

    local CA_Verbose = ConsoleArguments.unpaired["Verbose"] == true;
    local CA_Port = ConsoleArguments.paired["Port"];

    if CA_Port == nil then
        warn("No port specified, ending...")
        game:Shutdown();
        return;
    end

    NetworkServer.FirstMessageTimeout = Timeout;

    NetworkServer:StartServer("0.0.0.0:" .. CA_Port);
end

main()