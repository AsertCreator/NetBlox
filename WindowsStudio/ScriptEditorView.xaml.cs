using ICSharpCode.AvalonEdit.Highlighting;
using NetBlox.Instances.Scripts;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace NetBlox.Studio
{
	/// <summary>
	/// Interaction logic for ScriptEditorView.xaml
	/// </summary>
	public partial class ScriptEditorView : System.Windows.Controls.UserControl
	{
		public BaseScript? CurrentScript;

		public ScriptEditorView()
		{
			InitializeComponent();
		}
		public ScriptEditorView(BaseScript bs)
		{
			InitializeComponent();
			CurrentScript = bs;
			text.Text = CurrentScript.Source;
			text.TextChanged += (x, y) =>
			{
				CurrentScript.Source = text.Text;
			};
			text.SyntaxHighlighting = new LuaHighlighter();
		}
	}
	public class LuaHighlighter : IHighlightingDefinition
	{
		public string Name => "Lua";
		public HighlightingRuleSet MainRuleSet => new HighlightingRuleSet() 
		{ 
			Rules =
			{
				new HighlightingRule()
				{
					Regex = new Regex("\"\"(\\\"\"|.)*?\"\""),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkOrange)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("UDim2"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.Blue)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("Color3"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.Blue)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("new"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.Blue)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("Instance"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.Blue)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("(-)?[0-9]+"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkOrange)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("local"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkViolet)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("function"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkViolet)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("end"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkViolet)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("if"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkViolet)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("else"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkViolet)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("elif"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkViolet)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("return"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkViolet)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("for"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkViolet)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("do"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkViolet)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("then"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkViolet)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("--.+"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkGreen)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("print"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkBlue)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("warn"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkBlue)
					}
				},
				new HighlightingRule()
				{
					Regex = new Regex("error"),
					Color = new HighlightingColor()
					{
						FontWeight = FontWeights.Bold,
						Foreground = new SimpleHighlightingBrush(Colors.DarkBlue)
					}
				},
			}
		};
		public IEnumerable<HighlightingColor> NamedHighlightingColors => throw new NotImplementedException();
		public IDictionary<string, string> Properties => throw new NotImplementedException();

		public HighlightingColor GetNamedColor(string name)
		{
			throw new NotImplementedException();
		}
		public HighlightingRuleSet GetNamedRuleSet(string name)
		{
			throw new NotImplementedException();
		}
	}
}
