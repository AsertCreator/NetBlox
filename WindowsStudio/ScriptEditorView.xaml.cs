using NetBlox.Instances.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
		}
	}
}
