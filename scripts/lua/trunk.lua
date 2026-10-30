--[[
    this script does something don't ask
]]

for x = 0,3200 do
    local part = Instance.new("Part");
    part.Position = Vector3.new(8 * math.sin(x / 24 * 2 * 3.14159), 10 + x / 2, 8 * math.cos(x / 24 * 2 * 3.14159));
    part.Size = Vector3.new(2, 2, 2);
    part.BrickColor = BrickColor.random();
    part.Parent = game.Workspace;
    part.Shape = Enum.PartType.Sphere;
    part.Velocity = Vector3.new(0, 0, 0);
end
for x = 0,3200 do
    local part = Instance.new("Part");
    part.Position = Vector3.new(14 * math.cos(x / 24 * 2 * 3.14159), 10 + x / 1.8, 14 * math.sin(x / 24 * 2 * 3.14159));
    part.Size = Vector3.new(2, 2, 2);
    part.BrickColor = BrickColor.random();
    part.Parent = game.Workspace;
    part.Shape = Enum.PartType.Sphere;
    part.Velocity = Vector3.new(0, 0, 0);
end
for x = 0,3200 do
    local part = Instance.new("Part");
    part.Position = Vector3.new(20 * math.sin(x / 24 * 2 * 3.14159), 10 + x / 1.6, 20 * math.cos(x / 24 * 2 * 3.14159));
    part.Size = Vector3.new(2, 2, 2);
    part.BrickColor = BrickColor.random();
    part.Parent = game.Workspace;
    part.Shape = Enum.PartType.Sphere;
    part.Velocity = Vector3.new(0, 0, 0);
end
for x = 0,3200 do
    local part = Instance.new("Part");
    part.Position = Vector3.new(24 * math.cos(x / 24 * 2 * 3.14159), 10 + x / 1.4, 24 * math.sin(x / 24 * 2 * 3.14159));
    part.Size = Vector3.new(2, 2, 2);
    part.BrickColor = BrickColor.random();
    part.Parent = game.Workspace;
    part.Shape = Enum.PartType.Sphere;
    part.Velocity = Vector3.new(0, 0, 0);
end