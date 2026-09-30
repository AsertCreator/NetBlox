local CoreGui = game:GetService("CoreGui")
local CoreHttpService = game:GetService("CoreHttpService");
local CoreFileService = game:GetService("CoreFileService");

local MainGuiScreenGui = Instance.new("ScreenGui");
local MainGuiFrame = Instance.new("Frame");

MainGuiFrame.Name = "MainGuiFrame";
MainGuiFrame.Position = UDim2.new(0, 0, 0, 0);
MainGuiFrame.Size = UDim2.new(1, 0, 1, 0);
MainGuiFrame.BackgroundTransparency = 0;
MainGuiFrame.BorderSizePixel = 0;
MainGuiFrame.BackgroundColor3 = Color3.new(0.2, 0.2, 0.26);
MainGuiFrame.Parent = MainGuiScreenGui;

MainGuiScreenGui.Name = "MainGuiScreenGui";
MainGuiScreenGui.Parent = CoreGui;

function CreateBasicView(name)
    local View = Instance.new("Frame");
    View.Name = name;
    View.Position = UDim2.new(0, 20, 0, 0);
    View.Size = UDim2.new(1, -40, 1, 0);
    View.BackgroundTransparency = 0.9;
    View.BorderSizePixel = 0;
    return View;
end
function ParentAndPosition(child, parent, position)
    child.Position = position;
    child.Parent = parent;
end
function CreateText(text, fontsizeem, size)
    local TextLabel = Instance.new("TextLabel");
    TextLabel.Text = text;
    TextLabel.Size = size;
    TextLabel.TextSize = TextLabel.TextSize * fontsizeem;
    return TextLabel;
end
function CreateTestingView()
    local View = CreateBasicView("testing");
    local ScrollingFrame = Instance.new("ScrollingFrame");
    ScrollingFrame.Parent = View;
    ScrollingFrame.BackgroundTransparency = 1;
    ScrollingFrame.BorderSizePixel = 0;
    ScrollingFrame.Position = UDim2.new(0, 0, 0, 0);
    ScrollingFrame.Size = UDim2.new(1, 0, 1, 0);
    ScrollingFrame.ContentSize = UDim2.new(1, 0, 2, 0);

    local y = 0;

    local function PushHeaderText(text)
        local _ = CreateText(text, 2, UDim2.new(1, 0, 0, 70));
        _.TextXAlignment = Enum.TextXAlignment.Left;
        _.BackgroundTransparency = 1;
        _.BorderSizePixel = 0;
        _.TextColor3 = Color3.new(1, 1, 1)
        ParentAndPosition(_, ScrollingFrame, UDim2.new(0, 0, 0, y));
        y = y + 70;
    end
    local function PushRegularText(text)
        local _ = CreateText(text, 1, UDim2.new(1, 0, 0, 70));
        _.TextXAlignment = Enum.TextXAlignment.Left;
        _.TextYAlignment = Enum.TextYAlignment.Top;
        _.BackgroundTransparency = 1;
        _.BorderSizePixel = 0;
        _.TextColor3 = Color3.new(1, 1, 1);
        _.TextWrapped = true;
        ParentAndPosition(_, ScrollingFrame, UDim2.new(0, 0, 0, y));
        y = y + 70;
    end

    PushHeaderText("Testing")
    PushRegularText("Sed ut perspiciatis, unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam eaque ipsa, quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt, explicabo. Nemo enim ipsam voluptatem, quia voluptas sit, aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos, qui ratione voluptatem sequi nesciunt, neque porro quisquam est, qui dolorem ipsum, quia dolor sit, amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt, ut labore et dolore magnam aliquam quaerat voluptatem. Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem vel eum iure reprehenderit, qui in ea voluptate velit esse, quam nihil molestiae consequatur, vel illum, qui dolorem eum fugiat, quo voluptas nulla pariatur? At vero eos et accusamus et iusto odio dignissimos ducimus, qui blanditiis praesentium voluptatum deleniti atque corrupti, quos dolores et quas molestias excepturi sint, obcaecati cupiditate non provident, similique sunt in culpa, qui officia deserunt mollitia animi, id est laborum et dolorum fuga. Et harum quidem rerum facilis est et expedita distinctio. Nam libero tempore, cum soluta nobis est eligendi optio, cumque nihil impedit, quo minus id, quod maxime placeat, facere possimus, omnis voluptas assumenda est, omnis dolor repellendus. Temporibus autem quibusdam et aut officiis debitis aut rerum necessitatibus saepe eveniet, ut et voluptates repudiandae sint et molestiae non recusandae. Itaque earum rerum hic tenetur a sapiente delectus, ut aut reiciendis voluptatibus maiores alias consequatur aut perferendis doloribus asperiores repellat.")
    
    return View;
end

CreateTestingView().Parent = MainGuiFrame;