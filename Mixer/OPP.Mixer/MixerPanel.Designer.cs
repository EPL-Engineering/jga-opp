namespace OPP.Mixer
{
    partial class MixerPanel
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
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.label3 = new System.Windows.Forms.Label();
            this.flowLayoutPanel4 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel5 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel6 = new System.Windows.Forms.FlowLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.flowLayoutPanel7 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel8 = new System.Windows.Forms.FlowLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.flowLayoutPanel9 = new System.Windows.Forms.FlowLayoutPanel();
            this.caregiverStrip = new OPP.Mixer.ChannelStrip();
            this.waverStimStrip = new OPP.Mixer.ChannelStrip();
            this.talkbackStrip = new OPP.Mixer.ChannelStrip();
            this.ttsStrip = new OPP.Mixer.ChannelStrip();
            this.subjectStimStrip = new OPP.Mixer.ChannelStrip();
            this.videoStrip = new OPP.Mixer.ChannelStrip();
            this.testerStrip = new OPP.Mixer.ChannelStrip();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel4.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            this.flowLayoutPanel5.SuspendLayout();
            this.flowLayoutPanel6.SuspendLayout();
            this.flowLayoutPanel7.SuspendLayout();
            this.flowLayoutPanel8.SuspendLayout();
            this.flowLayoutPanel9.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flowLayoutPanel1.Controls.Add(this.label3);
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel4);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(106, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(275, 290);
            this.flowLayoutPanel1.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.label3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(0, 0);
            this.label3.Margin = new System.Windows.Forms.Padding(0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(273, 36);
            this.label3.TabIndex = 5;
            this.label3.Text = "B: Waver";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // flowLayoutPanel4
            // 
            this.flowLayoutPanel4.AutoSize = true;
            this.flowLayoutPanel4.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel4.Controls.Add(this.waverStimStrip);
            this.flowLayoutPanel4.Controls.Add(this.talkbackStrip);
            this.flowLayoutPanel4.Controls.Add(this.ttsStrip);
            this.flowLayoutPanel4.Location = new System.Drawing.Point(3, 39);
            this.flowLayoutPanel4.Name = "flowLayoutPanel4";
            this.flowLayoutPanel4.Size = new System.Drawing.Size(267, 246);
            this.flowLayoutPanel4.TabIndex = 7;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.AutoSize = true;
            this.flowLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flowLayoutPanel2.Controls.Add(this.label2);
            this.flowLayoutPanel2.Controls.Add(this.flowLayoutPanel3);
            this.flowLayoutPanel2.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(97, 290);
            this.flowLayoutPanel2.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.label2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(0, 0);
            this.label2.Margin = new System.Windows.Forms.Padding(0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(95, 36);
            this.label2.TabIndex = 5;
            this.label2.Text = "A: Caregiver";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.AutoSize = true;
            this.flowLayoutPanel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel3.Controls.Add(this.caregiverStrip);
            this.flowLayoutPanel3.Location = new System.Drawing.Point(3, 39);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(89, 246);
            this.flowLayoutPanel3.TabIndex = 7;
            // 
            // flowLayoutPanel5
            // 
            this.flowLayoutPanel5.AutoSize = true;
            this.flowLayoutPanel5.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel5.Controls.Add(this.flowLayoutPanel2);
            this.flowLayoutPanel5.Controls.Add(this.flowLayoutPanel1);
            this.flowLayoutPanel5.Controls.Add(this.flowLayoutPanel6);
            this.flowLayoutPanel5.Controls.Add(this.flowLayoutPanel8);
            this.flowLayoutPanel5.Location = new System.Drawing.Point(2, 2);
            this.flowLayoutPanel5.Name = "flowLayoutPanel5";
            this.flowLayoutPanel5.Size = new System.Drawing.Size(679, 296);
            this.flowLayoutPanel5.TabIndex = 7;
            // 
            // flowLayoutPanel6
            // 
            this.flowLayoutPanel6.AutoSize = true;
            this.flowLayoutPanel6.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flowLayoutPanel6.Controls.Add(this.label1);
            this.flowLayoutPanel6.Controls.Add(this.flowLayoutPanel7);
            this.flowLayoutPanel6.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel6.Location = new System.Drawing.Point(387, 3);
            this.flowLayoutPanel6.Name = "flowLayoutPanel6";
            this.flowLayoutPanel6.Size = new System.Drawing.Size(186, 290);
            this.flowLayoutPanel6.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.label1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(184, 36);
            this.label1.TabIndex = 5;
            this.label1.Text = "C: Participant";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // flowLayoutPanel7
            // 
            this.flowLayoutPanel7.AutoSize = true;
            this.flowLayoutPanel7.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel7.Controls.Add(this.subjectStimStrip);
            this.flowLayoutPanel7.Controls.Add(this.videoStrip);
            this.flowLayoutPanel7.Location = new System.Drawing.Point(3, 39);
            this.flowLayoutPanel7.Name = "flowLayoutPanel7";
            this.flowLayoutPanel7.Size = new System.Drawing.Size(178, 246);
            this.flowLayoutPanel7.TabIndex = 7;
            // 
            // flowLayoutPanel8
            // 
            this.flowLayoutPanel8.AutoSize = true;
            this.flowLayoutPanel8.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel8.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flowLayoutPanel8.Controls.Add(this.label4);
            this.flowLayoutPanel8.Controls.Add(this.flowLayoutPanel9);
            this.flowLayoutPanel8.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel8.Location = new System.Drawing.Point(579, 3);
            this.flowLayoutPanel8.Name = "flowLayoutPanel8";
            this.flowLayoutPanel8.Size = new System.Drawing.Size(97, 290);
            this.flowLayoutPanel8.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.label4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(0, 0);
            this.label4.Margin = new System.Windows.Forms.Padding(0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(95, 36);
            this.label4.TabIndex = 5;
            this.label4.Text = "D: Tester";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // flowLayoutPanel9
            // 
            this.flowLayoutPanel9.AutoSize = true;
            this.flowLayoutPanel9.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel9.Controls.Add(this.testerStrip);
            this.flowLayoutPanel9.Location = new System.Drawing.Point(3, 39);
            this.flowLayoutPanel9.Name = "flowLayoutPanel9";
            this.flowLayoutPanel9.Size = new System.Drawing.Size(89, 246);
            this.flowLayoutPanel9.TabIndex = 7;
            // 
            // caregiverStrip
            // 
            this.caregiverStrip.ChannelId = "CaregiverStim";
            this.caregiverStrip.Location = new System.Drawing.Point(3, 3);
            this.caregiverStrip.Name = "caregiverStrip";
            this.caregiverStrip.Size = new System.Drawing.Size(83, 240);
            this.caregiverStrip.TabIndex = 1;
            this.caregiverStrip.Title = "Stimulus";
            // 
            // waverStimStrip
            // 
            this.waverStimStrip.ChannelId = "WaverStim";
            this.waverStimStrip.Location = new System.Drawing.Point(3, 3);
            this.waverStimStrip.Name = "waverStimStrip";
            this.waverStimStrip.Size = new System.Drawing.Size(83, 240);
            this.waverStimStrip.TabIndex = 1;
            this.waverStimStrip.Title = "Stimulus";
            // 
            // talkbackStrip
            // 
            this.talkbackStrip.ChannelId = "WaverTalkback";
            this.talkbackStrip.Location = new System.Drawing.Point(92, 3);
            this.talkbackStrip.Name = "talkbackStrip";
            this.talkbackStrip.Size = new System.Drawing.Size(83, 240);
            this.talkbackStrip.TabIndex = 2;
            this.talkbackStrip.Title = "Talkback";
            // 
            // ttsStrip
            // 
            this.ttsStrip.ChannelId = "WaverTTS";
            this.ttsStrip.Location = new System.Drawing.Point(181, 3);
            this.ttsStrip.Name = "ttsStrip";
            this.ttsStrip.Size = new System.Drawing.Size(83, 240);
            this.ttsStrip.TabIndex = 0;
            this.ttsStrip.Title = "TTS";
            // 
            // subjectStimStrip
            // 
            this.subjectStimStrip.ChannelId = "ParticipantStim";
            this.subjectStimStrip.Location = new System.Drawing.Point(3, 3);
            this.subjectStimStrip.Name = "subjectStimStrip";
            this.subjectStimStrip.Size = new System.Drawing.Size(83, 240);
            this.subjectStimStrip.TabIndex = 1;
            this.subjectStimStrip.Title = "Stimulus";
            // 
            // videoStrip
            // 
            this.videoStrip.ChannelId = "ParticipantVideo";
            this.videoStrip.Location = new System.Drawing.Point(92, 3);
            this.videoStrip.Name = "videoStrip";
            this.videoStrip.Size = new System.Drawing.Size(83, 240);
            this.videoStrip.TabIndex = 2;
            this.videoStrip.Title = "Video";
            // 
            // testerStrip
            // 
            this.testerStrip.ChannelId = "TesterWaverMic";
            this.testerStrip.Location = new System.Drawing.Point(3, 3);
            this.testerStrip.Name = "testerStrip";
            this.testerStrip.Size = new System.Drawing.Size(83, 240);
            this.testerStrip.TabIndex = 1;
            this.testerStrip.Title = "Waver mic";
            // 
            // MixerPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(688, 306);
            this.ControlBox = false;
            this.Controls.Add(this.flowLayoutPanel5);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MixerPanel";
            this.Text = "Mixer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MixerPanel_FormClosing);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel4.ResumeLayout(false);
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.flowLayoutPanel3.ResumeLayout(false);
            this.flowLayoutPanel5.ResumeLayout(false);
            this.flowLayoutPanel5.PerformLayout();
            this.flowLayoutPanel6.ResumeLayout(false);
            this.flowLayoutPanel6.PerformLayout();
            this.flowLayoutPanel7.ResumeLayout(false);
            this.flowLayoutPanel8.ResumeLayout(false);
            this.flowLayoutPanel8.PerformLayout();
            this.flowLayoutPanel9.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel4;
        private ChannelStrip waverStimStrip;
        private ChannelStrip talkbackStrip;
        private ChannelStrip ttsStrip;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private ChannelStrip caregiverStrip;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel5;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel6;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel7;
        private ChannelStrip subjectStimStrip;
        private ChannelStrip videoStrip;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel8;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel9;
        private ChannelStrip testerStrip;
    }
}