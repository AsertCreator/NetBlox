using NetBlox.Instances.UI;
using NetBlox.Runtime;

namespace NetBlox.Instances.Services;

[ReplicateChildren]
public class UserInputService : Instance
{
    public override string ClassName => nameof(UserInputService);

    public const ulong NETWORK_CONSTANT_ID = 20;

    private TextBox? focusedTextBox;

    public UserInputService(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public TextBox? GetFocusedTextBox()
    {
        return focusedTextBox;
    }
    public void SetTextBoxFocus(TextBox? textBox)
    {
        focusedTextBox = textBox;
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        base.Destroy();
    }

    public override bool IsA(string className)
    {
        if (className != nameof(UserInputService))
            return base.IsA(className);
        return true;
    }
}