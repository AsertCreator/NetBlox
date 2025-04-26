using System.ComponentModel;

namespace NetBloxDebug
{
	partial class GameDebugForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			ToolStripSeparator toolStripSeparator;
			GroupBox groupBox1;
			GroupBox groupBox2;
			GroupBox groupBox3;
			gameCharsLabel = new Label();
			gameUptimeLabel = new Label();
			gameNameLabel = new Label();
			luaExecutorLevelSelector = new NumericUpDown();
			luaExecutorRun = new Button();
			luaExecutorBox = new TextBox();
			clearLog = new Button();
			gameLog = new TextBox();
			menuStrip1 = new MenuStrip();
			gameToolStripMenuItem = new ToolStripMenuItem();
			pauseToolStripMenuItem = new ToolStripMenuItem();
			pausePhysicsToolStripMenuItem = new ToolStripMenuItem();
			pauseRenderingToolStripMenuItem = new ToolStripMenuItem();
			shutdownThisInstanceToolStripMenuItem = new ToolStripMenuItem();
			shutdownAllInstancesToolStripMenuItem = new ToolStripMenuItem();
			detachToolStripMenuItem = new ToolStripMenuItem();
			testsToolStripMenuItem = new ToolStripMenuItem();
			runToolStripMenuItem = new ToolStripMenuItem();
			toolStripSeparator = new ToolStripSeparator();
			groupBox1 = new GroupBox();
			groupBox2 = new GroupBox();
			groupBox3 = new GroupBox();
			groupBox1.SuspendLayout();
			groupBox2.SuspendLayout();
			((ISupportInitialize)luaExecutorLevelSelector).BeginInit();
			groupBox3.SuspendLayout();
			menuStrip1.SuspendLayout();
			SuspendLayout();
			// 
			// toolStripSeparator
			// 
			toolStripSeparator.Name = "toolStripSeparator";
			toolStripSeparator.Size = new Size(194, 6);
			// 
			// groupBox1
			// 
			groupBox1.Controls.Add(gameCharsLabel);
			groupBox1.Controls.Add(gameUptimeLabel);
			groupBox1.Controls.Add(gameNameLabel);
			groupBox1.Location = new Point(12, 27);
			groupBox1.Name = "groupBox1";
			groupBox1.Size = new Size(251, 161);
			groupBox1.TabIndex = 1;
			groupBox1.TabStop = false;
			groupBox1.Text = "General information";
			// 
			// gameCharsLabel
			// 
			gameCharsLabel.Location = new Point(6, 49);
			gameCharsLabel.Name = "gameCharsLabel";
			gameCharsLabel.Size = new Size(239, 109);
			gameCharsLabel.TabIndex = 0;
			gameCharsLabel.Text = "Placeholder";
			// 
			// gameUptimeLabel
			// 
			gameUptimeLabel.AutoSize = true;
			gameUptimeLabel.Location = new Point(6, 34);
			gameUptimeLabel.Name = "gameUptimeLabel";
			gameUptimeLabel.Size = new Size(69, 15);
			gameUptimeLabel.TabIndex = 0;
			gameUptimeLabel.Text = "Placeholder";
			// 
			// gameNameLabel
			// 
			gameNameLabel.AutoSize = true;
			gameNameLabel.Location = new Point(6, 19);
			gameNameLabel.Name = "gameNameLabel";
			gameNameLabel.Size = new Size(69, 15);
			gameNameLabel.TabIndex = 0;
			gameNameLabel.Text = "Placeholder";
			// 
			// groupBox2
			// 
			groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
			groupBox2.Controls.Add(luaExecutorLevelSelector);
			groupBox2.Controls.Add(luaExecutorRun);
			groupBox2.Controls.Add(luaExecutorBox);
			groupBox2.Location = new Point(12, 194);
			groupBox2.Name = "groupBox2";
			groupBox2.Size = new Size(251, 246);
			groupBox2.TabIndex = 1;
			groupBox2.TabStop = false;
			groupBox2.Text = "Lua interpreter";
			// 
			// luaExecutorLevelSelector
			// 
			luaExecutorLevelSelector.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			luaExecutorLevelSelector.Location = new Point(209, 212);
			luaExecutorLevelSelector.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
			luaExecutorLevelSelector.Name = "luaExecutorLevelSelector";
			luaExecutorLevelSelector.Size = new Size(36, 23);
			luaExecutorLevelSelector.TabIndex = 2;
			luaExecutorLevelSelector.Value = new decimal(new int[] { 2, 0, 0, 0 });
			// 
			// luaExecutorRun
			// 
			luaExecutorRun.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
			luaExecutorRun.Location = new Point(6, 212);
			luaExecutorRun.Name = "luaExecutorRun";
			luaExecutorRun.Size = new Size(75, 23);
			luaExecutorRun.TabIndex = 1;
			luaExecutorRun.Text = "Run code";
			luaExecutorRun.UseVisualStyleBackColor = true;
			luaExecutorRun.Click += luaExecutorRun_Click;
			// 
			// luaExecutorBox
			// 
			luaExecutorBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			luaExecutorBox.Location = new Point(6, 22);
			luaExecutorBox.Multiline = true;
			luaExecutorBox.Name = "luaExecutorBox";
			luaExecutorBox.Size = new Size(239, 184);
			luaExecutorBox.TabIndex = 0;
			// 
			// groupBox3
			// 
			groupBox3.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			groupBox3.Controls.Add(clearLog);
			groupBox3.Controls.Add(gameLog);
			groupBox3.Location = new Point(269, 27);
			groupBox3.Name = "groupBox3";
			groupBox3.Size = new Size(641, 413);
			groupBox3.TabIndex = 2;
			groupBox3.TabStop = false;
			groupBox3.Text = "Game log";
			// 
			// clearLog
			// 
			clearLog.Location = new Point(6, 384);
			clearLog.Name = "clearLog";
			clearLog.Size = new Size(60, 23);
			clearLog.TabIndex = 1;
			clearLog.Text = "Clear";
			clearLog.UseVisualStyleBackColor = true;
			clearLog.Click += clearLog_Click;
			// 
			// gameLog
			// 
			gameLog.AcceptsReturn = true;
			gameLog.AcceptsTab = true;
			gameLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			gameLog.Location = new Point(6, 22);
			gameLog.Multiline = true;
			gameLog.Name = "gameLog";
			gameLog.ReadOnly = true;
			gameLog.Size = new Size(629, 359);
			gameLog.TabIndex = 0;
			// 
			// menuStrip1
			// 
			menuStrip1.Items.AddRange(new ToolStripItem[] { gameToolStripMenuItem, testsToolStripMenuItem });
			menuStrip1.Location = new Point(0, 0);
			menuStrip1.Name = "menuStrip1";
			menuStrip1.Size = new Size(922, 24);
			menuStrip1.TabIndex = 0;
			menuStrip1.Text = "menuStrip1";
			// 
			// gameToolStripMenuItem
			// 
			gameToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { pauseToolStripMenuItem, pausePhysicsToolStripMenuItem, pauseRenderingToolStripMenuItem, toolStripSeparator, shutdownThisInstanceToolStripMenuItem, shutdownAllInstancesToolStripMenuItem, detachToolStripMenuItem });
			gameToolStripMenuItem.Name = "gameToolStripMenuItem";
			gameToolStripMenuItem.Size = new Size(50, 20);
			gameToolStripMenuItem.Text = "Game";
			// 
			// pauseToolStripMenuItem
			// 
			pauseToolStripMenuItem.Name = "pauseToolStripMenuItem";
			pauseToolStripMenuItem.Size = new Size(197, 22);
			pauseToolStripMenuItem.Text = "Pause heartbeat";
			pauseToolStripMenuItem.Click += pauseToolStripMenuItem_Click;
			// 
			// pausePhysicsToolStripMenuItem
			// 
			pausePhysicsToolStripMenuItem.Name = "pausePhysicsToolStripMenuItem";
			pausePhysicsToolStripMenuItem.Size = new Size(197, 22);
			pausePhysicsToolStripMenuItem.Text = "Pause physics";
			pausePhysicsToolStripMenuItem.Click += pausePhysicsToolStripMenuItem_Click;
			// 
			// pauseRenderingToolStripMenuItem
			// 
			pauseRenderingToolStripMenuItem.Name = "pauseRenderingToolStripMenuItem";
			pauseRenderingToolStripMenuItem.Size = new Size(197, 22);
			pauseRenderingToolStripMenuItem.Text = "Pause rendering";
			pauseRenderingToolStripMenuItem.Click += pauseRenderingToolStripMenuItem_Click;
			// 
			// shutdownThisInstanceToolStripMenuItem
			// 
			shutdownThisInstanceToolStripMenuItem.Name = "shutdownThisInstanceToolStripMenuItem";
			shutdownThisInstanceToolStripMenuItem.Size = new Size(197, 22);
			shutdownThisInstanceToolStripMenuItem.Text = "Shutdown this instance";
			shutdownThisInstanceToolStripMenuItem.Click += shutdownThisInstanceToolStripMenuItem_Click;
			// 
			// shutdownAllInstancesToolStripMenuItem
			// 
			shutdownAllInstancesToolStripMenuItem.Name = "shutdownAllInstancesToolStripMenuItem";
			shutdownAllInstancesToolStripMenuItem.Size = new Size(197, 22);
			shutdownAllInstancesToolStripMenuItem.Text = "Shutdown all instances";
			shutdownAllInstancesToolStripMenuItem.Click += shutdownAllInstancesToolStripMenuItem_Click;
			// 
			// detachToolStripMenuItem
			// 
			detachToolStripMenuItem.Name = "detachToolStripMenuItem";
			detachToolStripMenuItem.Size = new Size(197, 22);
			detachToolStripMenuItem.Text = "Detach";
			detachToolStripMenuItem.Click += detachToolStripMenuItem_Click;
			// 
			// testsToolStripMenuItem
			// 
			testsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { runToolStripMenuItem });
			testsToolStripMenuItem.Name = "testsToolStripMenuItem";
			testsToolStripMenuItem.Size = new Size(45, 20);
			testsToolStripMenuItem.Text = "Tests";
			// 
			// runToolStripMenuItem
			// 
			runToolStripMenuItem.Name = "runToolStripMenuItem";
			runToolStripMenuItem.Size = new Size(180, 22);
			runToolStripMenuItem.Text = "Run";
			// 
			// GameDebugForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(922, 452);
			Controls.Add(groupBox3);
			Controls.Add(groupBox2);
			Controls.Add(groupBox1);
			Controls.Add(menuStrip1);
			MainMenuStrip = menuStrip1;
			Name = "GameDebugForm";
			Text = "GameDebugForm";
			groupBox1.ResumeLayout(false);
			groupBox1.PerformLayout();
			groupBox2.ResumeLayout(false);
			groupBox2.PerformLayout();
			((ISupportInitialize)luaExecutorLevelSelector).EndInit();
			groupBox3.ResumeLayout(false);
			groupBox3.PerformLayout();
			menuStrip1.ResumeLayout(false);
			menuStrip1.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		private MenuStrip menuStrip1;
		private ToolStripMenuItem gameToolStripMenuItem;
		private ToolStripMenuItem pauseToolStripMenuItem;
		private ToolStripMenuItem shutdownThisInstanceToolStripMenuItem;
		private ToolStripMenuItem shutdownAllInstancesToolStripMenuItem;
		private ToolStripMenuItem detachToolStripMenuItem;
		private ToolStripMenuItem pausePhysicsToolStripMenuItem;
		private ToolStripMenuItem pauseRenderingToolStripMenuItem;

		#endregion

		private Label gameCharsLabel;
		private Label gameUptimeLabel;
		private Label gameNameLabel;
		private NumericUpDown luaExecutorLevelSelector;
		private Button luaExecutorRun;
		private TextBox luaExecutorBox;
		private TextBox gameLog;
		private Button clearLog;
		private ToolStripMenuItem testsToolStripMenuItem;
		private ToolStripMenuItem runToolStripMenuItem;
	}
}
