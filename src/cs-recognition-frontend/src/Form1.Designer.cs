using System;
using System.Drawing;
using System.Windows.Forms;

namespace Haru.Kei {
	partial class Form1 {
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if(disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.propertyGrid = new System.Windows.Forms.PropertyGrid();
			this.button = new System.Windows.Forms.Button();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.batToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			this.testmicToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.testambientToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.plisetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.whisperToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.googleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.yukarinetteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.yukaconeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this.micToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// propertyGrid
			// 
			this.propertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.propertyGrid.Location = new System.Drawing.Point(0, 27);
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.Size = new System.Drawing.Size(800, 350);
			this.propertyGrid.TabIndex = 0;
			// 
			// button
			// 
			this.button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button.Location = new System.Drawing.Point(696, 393);
			this.button.Name = "button";
			this.button.Size = new System.Drawing.Size(92, 45);
			this.button.TabIndex = 1;
			this.button.Text = "起動";
			this.button.UseVisualStyleBackColor = true;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.plisetToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(800, 24);
			this.menuStrip1.TabIndex = 2;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.batToolStripMenuItem,
            this.toolStripMenuItem3,
            this.testmicToolStripMenuItem,
            this.testambientToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(67, 20);
			this.fileToolStripMenuItem.Text = "ファイル(&F)";
			// 
			// batToolStripMenuItem
			// 
			this.batToolStripMenuItem.Name = "batToolStripMenuItem";
			this.batToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.batToolStripMenuItem.Text = "バッチファイルを作成";
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(177, 6);
			// 
			// testmicToolStripMenuItem
			// 
			this.testmicToolStripMenuItem.Enabled = false;
			this.testmicToolStripMenuItem.Name = "testmicToolStripMenuItem";
			this.testmicToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.testmicToolStripMenuItem.Text = "マイクテスト";
			// 
			// testambientToolStripMenuItem
			// 
			this.testambientToolStripMenuItem.Enabled = false;
			this.testambientToolStripMenuItem.Name = "testambientToolStripMenuItem";
			this.testambientToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.testambientToolStripMenuItem.Text = "環境音測定";
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(177, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.exitToolStripMenuItem.Text = "終了(&E)";
			// 
			// plisetToolStripMenuItem
			// 
			this.plisetToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.whisperToolStripMenuItem,
            this.googleToolStripMenuItem,
            this.toolStripMenuItem2,
            this.yukarinetteToolStripMenuItem,
            this.yukaconeToolStripMenuItem,
            this.toolStripMenuItem4,
            this.micToolStripMenuItem});
			this.plisetToolStripMenuItem.Name = "plisetToolStripMenuItem";
			this.plisetToolStripMenuItem.Size = new System.Drawing.Size(77, 20);
			this.plisetToolStripMenuItem.Text = "プリセット(&P)";
			// 
			// whisperToolStripMenuItem
			// 
			this.whisperToolStripMenuItem.Name = "whisperToolStripMenuItem";
			this.whisperToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
			this.whisperToolStripMenuItem.Text = "音声認識にwhisperを使用";
			// 
			// googleToolStripMenuItem
			// 
			this.googleToolStripMenuItem.Name = "googleToolStripMenuItem";
			this.googleToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
			this.googleToolStripMenuItem.Text = "音声認識にgoogleを使用";
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(202, 6);
			// 
			// yukarinetteToolStripMenuItem
			// 
			this.yukarinetteToolStripMenuItem.Name = "yukarinetteToolStripMenuItem";
			this.yukarinetteToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
			this.yukarinetteToolStripMenuItem.Text = "ゆかりねっとと連携";
			// 
			// yukaconeToolStripMenuItem
			// 
			this.yukaconeToolStripMenuItem.Name = "yukaconeToolStripMenuItem";
			this.yukaconeToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
			this.yukaconeToolStripMenuItem.Text = "ゆかコネNEOと連携";
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size(202, 6);
			// 
			// micToolStripMenuItem
			// 
			this.micToolStripMenuItem.Enabled = false;
			this.micToolStripMenuItem.Name = "micToolStripMenuItem";
			this.micToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
			this.micToolStripMenuItem.Text = "マイクの初期値";
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.button);
			this.Controls.Add(this.propertyGrid);
			this.Controls.Add(this.menuStrip1);
			this.Name = "Form1";
			this.Text = "ゆーかねすぴれこランチャー";
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private PropertyGrid propertyGrid;
		private Button button;
		private MenuStrip menuStrip1;
		private ToolStripMenuItem fileToolStripMenuItem;
		private ToolStripMenuItem batToolStripMenuItem;
		private ToolStripSeparator toolStripMenuItem1;
		private ToolStripMenuItem exitToolStripMenuItem;
		private ToolStripMenuItem plisetToolStripMenuItem;
		private ToolStripMenuItem whisperToolStripMenuItem;
		private ToolStripMenuItem googleToolStripMenuItem;
		private ToolStripSeparator toolStripMenuItem2;
		private ToolStripMenuItem yukarinetteToolStripMenuItem;
		private ToolStripMenuItem yukaconeToolStripMenuItem;
		private ToolStripMenuItem testmicToolStripMenuItem;
		private ToolStripMenuItem testambientToolStripMenuItem;
		private ToolStripSeparator toolStripMenuItem3;
		private ToolStripSeparator toolStripMenuItem4;
		private ToolStripMenuItem micToolStripMenuItem;
	}
}
