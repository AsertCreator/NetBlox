local HumanoidMaker = {}

HumanoidMaker.CreateHumanoidForPlayer = function (player)
    local newCharacter = Instance.new("Model");
    local newHumanoid = Instance.new("Humanoid");

    newHumanoid.Parent = newCharacter;

    local humanoidRootPart = Instance.new("Part");
    local torso = Instance.new("Part");
    local leftLeg = Instance.new("Part");
    local rightLeg = Instance.new("Part");
    local leftArm = Instance.new("Part");
    local rightArm = Instance.new("Part");
    local head = Instance.new("Part");

    humanoidRootPart.Name = "HumanoidRootPart";
    humanoidRootPart.Transparency = 1;
    humanoidRootPart.CanCollide = true;
    humanoidRootPart.Parent = newCharacter;
    humanoidRootPart.Size = Vector3.new(1, 5, 2);
    humanoidRootPart.Position = Vector3.new(0, -0.5, 0);

    torso.Name = "Torso";
    torso.Parent = newCharacter;
    torso.Size = Vector3.new(1, 2, 2);
    torso.BrickColor = player:GetPlayerColor();
    torso.CanCollide = false;

    leftArm.Name = "Left Arm";
    leftArm.Parent = newCharacter;
    leftArm.Size = Vector3.new(1, 2, 1);
    leftArm.Position = Vector3.new(0, 0, 1.5);
    leftArm.BrickColor = BrickColor.random();
    leftArm.CanCollide = false;

    rightArm.Name = "Right Arm";
    rightArm.Parent = newCharacter;
    rightArm.Size = Vector3.new(1, 2, 1);
    rightArm.Position = Vector3.new(0, 0, -1.5);
    rightArm.BrickColor = leftArm.BrickColor;
    rightArm.CanCollide = false;

    leftLeg.Name = "Left Leg";
    leftLeg.Parent = newCharacter;
    leftLeg.Size = Vector3.new(1, 2, 1);
    leftLeg.Position = Vector3.new(0, -2, 0.5);
    leftLeg.BrickColor = BrickColor.random();
    leftLeg.CanCollide = false;

    rightLeg.Name = "Right Leg";
    rightLeg.Parent = newCharacter;
    rightLeg.Size = Vector3.new(1, 2, 1);
    rightLeg.Position = Vector3.new(0, -2, -0.5);
    rightLeg.BrickColor = leftLeg.BrickColor;
    rightLeg.CanCollide = false;

    head.Name = "Head";
    head.Parent = newCharacter;
    head.Shape = Enum.PartType.Ball;
    head.Size = Vector3.new(1.5, 1.5, 1.5);
    head.Position = Vector3.new(0, 1.7, 0);
    head.BrickColor = BrickColor.random();
    head.CanCollide = false;

    local weld;

    weld = Instance.new("Weld");
    weld.Part0 = torso;
    weld.Part1 = humanoidRootPart;
    weld.Parent = humanoidRootPart;
    weld = Instance.new("Weld");
    weld.Part0 = leftArm;
    weld.Part1 = humanoidRootPart;
    weld.Parent = humanoidRootPart;
    weld = Instance.new("Weld");
    weld.Part0 = rightArm;
    weld.Part1 = humanoidRootPart;
    weld.Parent = humanoidRootPart;
    weld = Instance.new("Weld");
    weld.Part0 = leftLeg;
    weld.Part1 = humanoidRootPart;
    weld.Parent = humanoidRootPart;
    weld = Instance.new("Weld");
    weld.Part0 = rightLeg;
    weld.Part1 = humanoidRootPart;
    weld.Parent = humanoidRootPart;
    weld = Instance.new("Weld");
    weld.Part0 = head;
    weld.Part1 = humanoidRootPart;
    weld.Parent = humanoidRootPart;

    return newCharacter;
end

return HumanoidMaker;