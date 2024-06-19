using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RibbonLib;

namespace NetBlox.Studio
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
			Ribbon r = new();
			r.Parent = this;
			r.Minimized = false;
			r.ResourceName = "NetBlox.Studio.RibbonMarkup.ribbon";
			r.ShortcutTableResourceName = null;
			r.Size = new Size(Width, 100);
			r.Dock = DockStyle.Top;
			r.TabIndex = 0;
		}
	}
}
