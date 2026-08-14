namespace OppStreamer.DevShell
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            signalGraph = new ScottPlot.WinForms.FormsPlot();
            startButton = new Button();
            stopButton = new Button();
            SuspendLayout();
            // 
            // signalGraph
            // 
            signalGraph.Location = new Point(22, 12);
            signalGraph.Name = "signalGraph";
            signalGraph.Size = new Size(464, 287);
            signalGraph.TabIndex = 0;
            // 
            // startButton
            // 
            startButton.Location = new Point(51, 320);
            startButton.Name = "startButton";
            startButton.Size = new Size(114, 40);
            startButton.TabIndex = 1;
            startButton.Text = "START";
            startButton.UseVisualStyleBackColor = true;
            startButton.Click += startButton_Click;
            // 
            // stopButton
            // 
            stopButton.Enabled = false;
            stopButton.Location = new Point(180, 320);
            stopButton.Name = "stopButton";
            stopButton.Size = new Size(114, 40);
            stopButton.TabIndex = 2;
            stopButton.Text = "STOP";
            stopButton.UseVisualStyleBackColor = true;
            stopButton.Click += stopButton_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(stopButton);
            Controls.Add(startButton);
            Controls.Add(signalGraph);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "OPP Streamer Dev Shell";
            Load += MainForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private ScottPlot.WinForms.FormsPlot signalGraph;
        private Button startButton;
        private Button stopButton;
    }
}
