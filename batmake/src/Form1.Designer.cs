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
			this.SuspendLayout();
			// 
			// propertyGrid
			// 
			this.propertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.propertyGrid.Location = new System.Drawing.Point(-5, -4);
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.Size = new System.Drawing.Size(808, 391);
			this.propertyGrid.TabIndex = 0;
			// 
			// button
			// 
			this.button.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button.Location = new System.Drawing.Point(696, 393);
			this.button.Name = "button";
			this.button.Size = new System.Drawing.Size(92, 45);
			this.button.TabIndex = 1;
			this.button.Text = "作成";
			this.button.UseVisualStyleBackColor = true;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.button);
			this.Controls.Add(this.propertyGrid);
			this.Name = "Form1";
			this.Text = "バッチジェネレータ";
			this.ResumeLayout(false);

		}

		#endregion

		private PropertyGrid propertyGrid;
		private Button button;
	}
}
