local PlatformService = game:GetService("PlatformService");

-- initializes server communication with public service
function initStatus()
	if PlatformService:IsServer() then
		PlatformService:EnableRctlPipe();
	end
end

initStatus();

print("Server platform initialized");