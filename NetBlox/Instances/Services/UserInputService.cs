using MoonSharp.Interpreter;
using NetBlox.Instances.UI;
using NetBlox.Runtime;
using NetBlox.Runtime.Bridges;
using NetBlox.Structs;
using Raylib_cs;

namespace NetBlox.Instances.Services;

[ReplicateChildren]
public class UserInputService : Instance
{
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent TextBoxFocusReleased { get; } = new LuaEvent();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent TextBoxFocused { get; } = new LuaEvent();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent InputBegin { get; } = new LuaEvent();
    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public LuaEvent InputEnded { get; } = new LuaEvent();

    public override string ClassName => nameof(UserInputService);

    private Dictionary<KeyboardKey, bool> Keyboard = [];
    private Dictionary<KeyCode, InputObject> CachedInputObjects = [];

    public const ulong NETWORK_CONSTANT_ID = 20;

    private TextBox? focusedTextBox;

    public UserInputService(ulong id, GameManager gameManager) : base(NETWORK_CONSTANT_ID, gameManager)
    {
    }

    private InputObject GetInputObjectForKeyCode(KeyCode code)
    {
        if (!CachedInputObjects.TryGetValue(code, out InputObject? inputObject))
        {
            inputObject = GameManager.GameRegistry.Construct<InputObject>();
            inputObject.ParentLocked = true;
            
            GameManager.GameRegistry.SpeedThroughInitialization(inputObject);

            CachedInputObjects[code] = inputObject;
        }
        return inputObject;
    }

    private void HandleRaylibKeyDown(KeyboardKey keyboardKey)
    {
        KeyCode code = FromRaylibKey(keyboardKey);
        if (code == KeyCode.None)
            return;

        if (focusedTextBox != null && focusedTextBox.IsPartOfDataModel)
        {
            InputObject inputObject = GetInputObjectForKeyCode(code);

            inputObject.KeyCode = code;
            inputObject.UserInputType = UserInputType.Keyboard;
            inputObject.UserInputState = UserInputState.Begin;
            
            if (InputBegin.HasConnections || InputBegin.HasWaiters)
                InputBegin.Fire([DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, inputObject))]);
        }
    }
    private void HandleRaylibKeyUp(KeyboardKey keyboardKey)
    {
        // i think locking this if focusedTextBox isn't null will cause sticky keys effect when a player
        // focuses on a text box while holding a key so no

        KeyCode code = FromRaylibKey(keyboardKey);
        if (code == KeyCode.None)
            return;

        InputObject inputObject = GetInputObjectForKeyCode(code);

        inputObject.KeyCode = code;
        inputObject.UserInputType = UserInputType.Keyboard;
        inputObject.UserInputState = UserInputState.End;
        
        if (InputEnded.HasConnections || InputEnded.HasWaiters)
            InputEnded.Fire([DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, inputObject))]);
    }
    public void HandleInput()
    {
        for (int i = 0; i < (int)KeyboardKey.End; i++)
        {
            KeyboardKey keyboardKey = (KeyboardKey)i;

            if (Raylib.IsKeyDown(keyboardKey))
            {
                if (Keyboard.TryGetValue(keyboardKey, out bool down) && !down)
                    HandleRaylibKeyDown(keyboardKey);
                Keyboard[keyboardKey] = true;
            }
            else if (Raylib.IsKeyUp(keyboardKey))
            {
                if (Keyboard.TryGetValue(keyboardKey, out bool down) && down)
                    HandleRaylibKeyUp(keyboardKey);
                Keyboard[keyboardKey] = false;
            }
        }
    }

    [ScriptCallable(RequiredLevel = SimpleSecurityCapabilityLevel.LocalUser)]
    public TextBox? GetFocusedTextBox()
    {
        return focusedTextBox;
    }
    public void SetTextBoxFocus(TextBox? textBox)
    {
        if (focusedTextBox != textBox)
        {
            TextBox? oldFocusedTextbox = focusedTextBox;
            focusedTextBox = textBox;

            if (oldFocusedTextbox != null)
                TextBoxFocusReleased.Fire([DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, oldFocusedTextbox))]);
            if (textBox != null)
                TextBoxFocused.Fire([DynValue.NewUserData(InstanceBridge.PushUserData(GameManager, textBox))]);
        }
    }

    public override void Destroy()
    {
        if (!GameManager.GameScheduler.GetCurrentSecurityIdentity()!.RequireSimpleCapability(SimpleSecurityCapabilityLevel.DestroyServices))
            return;
        base.Destroy();
    }

    public static KeyCode FromRaylibKey(Raylib_cs.KeyboardKey keyboardKey)
    {
        return keyboardKey switch
        {
            Raylib_cs.KeyboardKey.Null => KeyCode.None,
            Raylib_cs.KeyboardKey.Apostrophe => KeyCode.Quote,
            Raylib_cs.KeyboardKey.Comma => KeyCode.Comma,
            Raylib_cs.KeyboardKey.Minus => KeyCode.Minus,
            Raylib_cs.KeyboardKey.Period => KeyCode.Period,
            Raylib_cs.KeyboardKey.Slash => KeyCode.Slash,
            Raylib_cs.KeyboardKey.Zero => KeyCode.Zero,
            Raylib_cs.KeyboardKey.One => KeyCode.One,
            Raylib_cs.KeyboardKey.Two => KeyCode.Two,
            Raylib_cs.KeyboardKey.Three => KeyCode.Three,
            Raylib_cs.KeyboardKey.Four => KeyCode.Four,
            Raylib_cs.KeyboardKey.Five => KeyCode.Five,
            Raylib_cs.KeyboardKey.Six => KeyCode.Six,
            Raylib_cs.KeyboardKey.Seven => KeyCode.Seven,
            Raylib_cs.KeyboardKey.Eight => KeyCode.Eight,
            Raylib_cs.KeyboardKey.Nine => KeyCode.Nine,
            Raylib_cs.KeyboardKey.Semicolon => KeyCode.Semicolon,
            Raylib_cs.KeyboardKey.Equal => KeyCode.Equals,
            Raylib_cs.KeyboardKey.A => KeyCode.A,
            Raylib_cs.KeyboardKey.B => KeyCode.B,
            Raylib_cs.KeyboardKey.C => KeyCode.C,
            Raylib_cs.KeyboardKey.D => KeyCode.D,
            Raylib_cs.KeyboardKey.E => KeyCode.E,
            Raylib_cs.KeyboardKey.F => KeyCode.F,
            Raylib_cs.KeyboardKey.G => KeyCode.G,
            Raylib_cs.KeyboardKey.H => KeyCode.H,
            Raylib_cs.KeyboardKey.I => KeyCode.I,
            Raylib_cs.KeyboardKey.J => KeyCode.J,
            Raylib_cs.KeyboardKey.K => KeyCode.K,
            Raylib_cs.KeyboardKey.L => KeyCode.L,
            Raylib_cs.KeyboardKey.M => KeyCode.M,
            Raylib_cs.KeyboardKey.N => KeyCode.N,
            Raylib_cs.KeyboardKey.O => KeyCode.O,
            Raylib_cs.KeyboardKey.P => KeyCode.P,
            Raylib_cs.KeyboardKey.Q => KeyCode.Q,
            Raylib_cs.KeyboardKey.R => KeyCode.R,
            Raylib_cs.KeyboardKey.S => KeyCode.S,
            Raylib_cs.KeyboardKey.T => KeyCode.T,
            Raylib_cs.KeyboardKey.U => KeyCode.U,
            Raylib_cs.KeyboardKey.V => KeyCode.V,
            Raylib_cs.KeyboardKey.W => KeyCode.W,
            Raylib_cs.KeyboardKey.X => KeyCode.X,
            Raylib_cs.KeyboardKey.Y => KeyCode.Y,
            Raylib_cs.KeyboardKey.Z => KeyCode.Z,

            // Function keys
            Raylib_cs.KeyboardKey.Space => KeyCode.Space,
            Raylib_cs.KeyboardKey.Escape => KeyCode.Escape,
            Raylib_cs.KeyboardKey.Enter => KeyCode.Return,
            Raylib_cs.KeyboardKey.Tab => KeyCode.Tab,
            Raylib_cs.KeyboardKey.Backspace => KeyCode.Backspace,
            Raylib_cs.KeyboardKey.Insert => KeyCode.Insert,
            Raylib_cs.KeyboardKey.Delete => KeyCode.Delete,
            Raylib_cs.KeyboardKey.Right => KeyCode.Right,
            Raylib_cs.KeyboardKey.Left => KeyCode.Left,
            Raylib_cs.KeyboardKey.Down => KeyCode.Right,
            Raylib_cs.KeyboardKey.Up => KeyCode.Up,
            Raylib_cs.KeyboardKey.PageUp => KeyCode.PageUp,
            Raylib_cs.KeyboardKey.PageDown => KeyCode.PageDown,
            Raylib_cs.KeyboardKey.Home => KeyCode.Home,
            Raylib_cs.KeyboardKey.End => KeyCode.End,
            Raylib_cs.KeyboardKey.CapsLock => KeyCode.CapsLock,
            Raylib_cs.KeyboardKey.ScrollLock => KeyCode.ScrollLock,
            Raylib_cs.KeyboardKey.NumLock => KeyCode.NumLock,
            Raylib_cs.KeyboardKey.PrintScreen => KeyCode.Print,
            Raylib_cs.KeyboardKey.Pause => KeyCode.Pause,
            Raylib_cs.KeyboardKey.F1 => KeyCode.F1,
            Raylib_cs.KeyboardKey.F2 => KeyCode.F2,
            Raylib_cs.KeyboardKey.F3 => KeyCode.F3,
            Raylib_cs.KeyboardKey.F4 => KeyCode.F4,
            Raylib_cs.KeyboardKey.F5 => KeyCode.F5,
            Raylib_cs.KeyboardKey.F6 => KeyCode.F6,
            Raylib_cs.KeyboardKey.F7 => KeyCode.F7,
            Raylib_cs.KeyboardKey.F8 => KeyCode.F8,
            Raylib_cs.KeyboardKey.F9 => KeyCode.F9,
            Raylib_cs.KeyboardKey.F10 => KeyCode.F10,
            Raylib_cs.KeyboardKey.F11 => KeyCode.F11,
            Raylib_cs.KeyboardKey.F12 => KeyCode.F12,
            Raylib_cs.KeyboardKey.LeftShift => KeyCode.LeftShift,
            Raylib_cs.KeyboardKey.LeftControl => KeyCode.LeftControl,
            Raylib_cs.KeyboardKey.LeftAlt => KeyCode.LeftAlt,
            Raylib_cs.KeyboardKey.LeftSuper => KeyCode.LeftSuper,
            Raylib_cs.KeyboardKey.RightShift => KeyCode.RightShift,
            Raylib_cs.KeyboardKey.RightControl => KeyCode.RightControl,
            Raylib_cs.KeyboardKey.RightAlt => KeyCode.RightAlt,
            Raylib_cs.KeyboardKey.RightSuper => KeyCode.RightSuper,
            Raylib_cs.KeyboardKey.KeyboardMenu => KeyCode.Menu,
            Raylib_cs.KeyboardKey.LeftBracket => KeyCode.LeftBracket,
            Raylib_cs.KeyboardKey.Backslash => KeyCode.BackSlash,
            Raylib_cs.KeyboardKey.RightBracket => KeyCode.RightBracket,
            Raylib_cs.KeyboardKey.Grave => KeyCode.Tilde,

            // Keypad keys
            Raylib_cs.KeyboardKey.Kp0 => KeyCode.KeypadZero,
            Raylib_cs.KeyboardKey.Kp1 => KeyCode.KeypadOne,
            Raylib_cs.KeyboardKey.Kp2 => KeyCode.KeypadTwo,
            Raylib_cs.KeyboardKey.Kp3 => KeyCode.KeypadThree,
            Raylib_cs.KeyboardKey.Kp4 => KeyCode.KeypadFour,
            Raylib_cs.KeyboardKey.Kp5 => KeyCode.KeypadFive,
            Raylib_cs.KeyboardKey.Kp6 => KeyCode.KeypadSix,
            Raylib_cs.KeyboardKey.Kp7 => KeyCode.KeypadSeven,
            Raylib_cs.KeyboardKey.Kp8 => KeyCode.KeypadEight,
            Raylib_cs.KeyboardKey.Kp9 => KeyCode.KeypadNine,
            Raylib_cs.KeyboardKey.KpDecimal => KeyCode.KeypadPeriod,
            Raylib_cs.KeyboardKey.KpDivide => KeyCode.KeypadDivide,
            Raylib_cs.KeyboardKey.KpMultiply => KeyCode.KeypadMultiply,
            Raylib_cs.KeyboardKey.KpSubtract => KeyCode.KeypadMinus,
            Raylib_cs.KeyboardKey.KpAdd => KeyCode.KeypadPlus,
            Raylib_cs.KeyboardKey.KpEnter => KeyCode.KeypadEnter,
            Raylib_cs.KeyboardKey.KpEqual => KeyCode.KeypadEquals,

            // Android key buttons
            Raylib_cs.KeyboardKey.Back => KeyCode.None,
            Raylib_cs.KeyboardKey.Menu => KeyCode.None,
            Raylib_cs.KeyboardKey.VolumeUp => KeyCode.None,
            Raylib_cs.KeyboardKey.VolumeDown => KeyCode.None,

            _ => KeyCode.None
        };
    }

    public override bool IsA(string className)
    {
        if (className != nameof(UserInputService))
            return base.IsA(className);
        return true;
    }
}