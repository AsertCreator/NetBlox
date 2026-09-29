local module = {}
local usedbackpack;

function module.setBackpackInstance(backpack)
	usedbackpack = backpack;
end
function module.mount(parent)
	local backpackFrame = Instance.new("Frame")

	backpackFrame.AnchorPoint = Vector2.new(0, 1)
	backpackFrame.Size = UDim2.new(0, 100, 0, 100)
	backpackFrame.BackgroundColor3 = Color3.new(0.1, 0.1, 0.1);
	backpackFrame.BackgroundTransparency = 0.5;
	backpackFrame.Name = "BackpackUI";
	backpackFrame.Parent = parent;
end

return module;