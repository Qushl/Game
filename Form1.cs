using System.Windows.Forms;
using System.Reflection;
using System.Drawing;

namespace TopDownHighwayDrifter;

public partial class Form1 : Form
{
    private GameManager _gameManager;
    private System.Windows.Forms.Timer _gameTimer;
    private bool _keyLeft = false, _keyRight = false, _keyUp = false, _keyDown = false;

    public Form1()
    {
        InitializeComponent();
        this.DoubleBuffered = true;
        this.ClientSize = new System.Drawing.Size(800, 800);
        this.Text = "Top-Down Highway Drifter";
        this.BackColor = System.Drawing.Color.Black;
        this.KeyPreview = true;

        _gameManager = new GameManager(this.ClientSize.Width, this.ClientSize.Height);
        ApplySettingsFromUI();

        try
        {
            if (_menuPanel != null)
            {
                _menuPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                _menuPanel.Location = new Point(this.ClientSize.Width - _menuPanel.Width - 8, 8);
                var prop = typeof(Panel).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                prop?.SetValue(_menuPanel, true, null);
            }
        }
        catch { }

        _gameTimer = new System.Windows.Forms.Timer();
        _gameTimer.Interval = 16;
        _gameTimer.Tick += GameTimer_Tick;
        _gameTimer.Start();

        this.Paint += Form1_Paint;
        this.KeyDown += Form1_KeyDown;
        this.KeyUp += Form1_KeyUp;
    }

    private void ApplySettingsFromUI()
    {
        try
        {
            if (_nudEnemies != null) _gameManager.MaxEnemies = (int)_nudEnemies.Value;
            if (_cbCarModel != null && _cbCarModel.SelectedItem != null)
            {
                var sel = _cbCarModel.SelectedItem.ToString();
                switch (sel)
                {
                    case "Default": _gameManager.SetPlayerModel(PlayerCar.CarModelType.Default); break;
                    case "Straight": _gameManager.SetPlayerModel(PlayerCar.CarModelType.Straight); break;
                    case "Sideways": _gameManager.SetPlayerModel(PlayerCar.CarModelType.Sideways); break;
                }
            }
        }
        catch { }
    }

    private void GameTimer_Tick(object sender, EventArgs e)
    {
        _gameManager.Player.SetInput(_keyLeft, _keyRight, _keyUp, _keyDown);
        _gameManager.Update();
        this.Invalidate();
    }

    private void Form1_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        _gameManager.DrawGame(e.Graphics, this.ClientSize.Width, this.ClientSize.Height);
    }

    private void Form1_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.A: _keyLeft = true; break;
            case Keys.D: _keyRight = true; break;
            case Keys.W: _keyUp = true; break;
            case Keys.S: _keyDown = true; break;
            case Keys.R: if (_gameManager.IsGameOver) _gameManager.Reset(); break;
            case Keys.Escape: this.Close(); break;
        }
    }

    private void Form1_KeyUp(object sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.A: _keyLeft = false; break;
            case Keys.D: _keyRight = false; break;
            case Keys.W: _keyUp = false; break;
            case Keys.S: _keyDown = false; break;
        }
    }

    private void BtnApplySettings_Click(object? sender, EventArgs e)
    {
        ApplySettingsFromUI();
    }
}
