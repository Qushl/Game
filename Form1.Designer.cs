namespace TopDownHighwayDrifter;

partial class Form1
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
        if (disposing && (_gameTimer != null))
        {
            _gameTimer.Stop();
            _gameTimer.Dispose();
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
        this.components = new System.ComponentModel.Container();
        this._menuPanel = new System.Windows.Forms.Panel();
        this._lblEnemies = new System.Windows.Forms.Label();
        this._nudEnemies = new System.Windows.Forms.NumericUpDown();
        this._lblCarModel = new System.Windows.Forms.Label();
        this._cbCarModel = new System.Windows.Forms.ComboBox();
        this._btnApplySettings = new System.Windows.Forms.Button();
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 800);
        this.Text = "Top-Down Highway Drifter";
        this.DoubleBuffered = true;

        // menuPanel
        this._menuPanel.BackColor = System.Drawing.Color.FromArgb(255, 40, 40, 40);
        this._menuPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this._menuPanel.Location = new System.Drawing.Point(560, 8);
        this._menuPanel.Size = new System.Drawing.Size(220, 120);

        // lblEnemies
        this._lblEnemies.AutoSize = true;
        this._lblEnemies.ForeColor = System.Drawing.Color.White;
        this._lblEnemies.Location = new System.Drawing.Point(8, 10);
        this._lblEnemies.Text = "Enemies:";

        // nudEnemies
        this._nudEnemies.Location = new System.Drawing.Point(90, 8);
        this._nudEnemies.Minimum = 0;
        this._nudEnemies.Maximum = 64;
        this._nudEnemies.Value = 12;
        this._nudEnemies.Size = new System.Drawing.Size(80, 22);

        // lblCarModel
        this._lblCarModel.AutoSize = true;
        this._lblCarModel.ForeColor = System.Drawing.Color.White;
        this._lblCarModel.Location = new System.Drawing.Point(8, 44);
        this._lblCarModel.Text = "Car Model:";

        // cbCarModel
        this._cbCarModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this._cbCarModel.Location = new System.Drawing.Point(90, 40);
        this._cbCarModel.Size = new System.Drawing.Size(120, 24);
        this._cbCarModel.Items.AddRange(new object[] { "Default", "Straight", "Sideways" });
        this._cbCarModel.SelectedIndex = 0; // Default

        // btnApplySettings
        this._btnApplySettings.Location = new System.Drawing.Point(90, 76);
        this._btnApplySettings.Size = new System.Drawing.Size(100, 28);
        this._btnApplySettings.Text = "Apply";
        this._btnApplySettings.BackColor = System.Drawing.Color.White;
        this._btnApplySettings.ForeColor = System.Drawing.Color.Black;
        this._btnApplySettings.Click += new System.EventHandler(this.BtnApplySettings_Click);

        // Add controls to panel
        this._menuPanel.Controls.Add(this._lblEnemies);
        this._menuPanel.Controls.Add(this._nudEnemies);
        this._menuPanel.Controls.Add(this._lblCarModel);
        this._menuPanel.Controls.Add(this._cbCarModel);
        this._menuPanel.Controls.Add(this._btnApplySettings);

        // Add panel to form
        this.Controls.Add(this._menuPanel);
    }

    #endregion
}

// Designer-added controls
partial class Form1
{
    private System.Windows.Forms.Panel _menuPanel;
    private System.Windows.Forms.Label _lblEnemies;
    private System.Windows.Forms.NumericUpDown _nudEnemies;
    private System.Windows.Forms.Label _lblCarModel;
    private System.Windows.Forms.ComboBox _cbCarModel;
    private System.Windows.Forms.Button _btnApplySettings;
}
