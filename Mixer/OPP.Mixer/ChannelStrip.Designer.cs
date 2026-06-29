namespace OPP.Mixer
{
    partial class ChannelStrip
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.trackBar = new System.Windows.Forms.TrackBar();
            this.muteButton = new System.Windows.Forms.CheckBox();
            this.label = new System.Windows.Forms.Label();
            this.numericBox = new KLib.Controls.KNumericBox();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // trackBar
            // 
            this.trackBar.Location = new System.Drawing.Point(19, 35);
            this.trackBar.Maximum = 0;
            this.trackBar.Minimum = -50;
            this.trackBar.Name = "trackBar";
            this.trackBar.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBar.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.trackBar.Size = new System.Drawing.Size(45, 147);
            this.trackBar.TabIndex = 0;
            this.trackBar.TickFrequency = 5;
            this.trackBar.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.trackBar.Scroll += new System.EventHandler(this.trackBar_Scroll);
            this.trackBar.ValueChanged += new System.EventHandler(this.trackBar_ValueChanged);
            // 
            // muteButton
            // 
            this.muteButton.Appearance = System.Windows.Forms.Appearance.Button;
            this.muteButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.muteButton.Location = new System.Drawing.Point(6, 204);
            this.muteButton.Name = "muteButton";
            this.muteButton.Size = new System.Drawing.Size(74, 24);
            this.muteButton.TabIndex = 2;
            this.muteButton.Text = "Mute";
            this.muteButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.muteButton.UseVisualStyleBackColor = false;
            this.muteButton.CheckedChanged += new System.EventHandler(this.muteButton_CheckedChanged);
            // 
            // label
            // 
            this.label.Location = new System.Drawing.Point(3, 9);
            this.label.Name = "label";
            this.label.Size = new System.Drawing.Size(77, 23);
            this.label.TabIndex = 3;
            this.label.Text = "Signal";
            this.label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // numericBox
            // 
            this.numericBox.AllowInf = false;
            this.numericBox.AutoSize = true;
            this.numericBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.numericBox.ClearOnDisable = false;
            this.numericBox.FloatValue = -3F;
            this.numericBox.IntValue = -3;
            this.numericBox.IsInteger = false;
            this.numericBox.Location = new System.Drawing.Point(6, 181);
            this.numericBox.MaxCoerce = true;
            this.numericBox.MaximumSize = new System.Drawing.Size(20000, 20);
            this.numericBox.MaxValue = 0D;
            this.numericBox.MinCoerce = true;
            this.numericBox.MinimumSize = new System.Drawing.Size(10, 20);
            this.numericBox.MinValue = -50D;
            this.numericBox.Name = "numericBox";
            this.numericBox.Size = new System.Drawing.Size(74, 20);
            this.numericBox.TabIndex = 4;
            this.numericBox.TextFormat = "K1";
            this.numericBox.ToolTip = "";
            this.numericBox.Units = "dB";
            this.numericBox.Value = -3D;
            this.numericBox.ValueChanged += new System.EventHandler(this.numericBox_ValueChanged);
            // 
            // ChannelStrip
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.numericBox);
            this.Controls.Add(this.label);
            this.Controls.Add(this.muteButton);
            this.Controls.Add(this.trackBar);
            this.Name = "ChannelStrip";
            this.Size = new System.Drawing.Size(83, 232);
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TrackBar trackBar;
        private System.Windows.Forms.CheckBox muteButton;
        private System.Windows.Forms.Label label;
        private KLib.Controls.KNumericBox numericBox;
    }
}
