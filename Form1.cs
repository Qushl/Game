using System.Windows.Forms;

namespace TopDownHighwayDrifter;

public partial class Form1 : Form
{
    private GameManager _gameManager;
    private System.Windows.Forms.Timer _gameTimer;
    private bool _keyLeft = false;
    private bool _keyRight = false;
    private bool _keyUp = false;
    private bool _keyDown = false;

    public Form1()
    {
        InitializeComponent();
        this.DoubleBuffered = true;
        this.ClientSize = new System.Drawing.Size(800, 800);
        this.Text = "Top-Down Highway Drifter";
        this.BackColor = System.Drawing.Color.Black;
        this.KeyPreview = true;

        _gameManager = new GameManager(this.ClientSize.Width, this.ClientSize.Height);

        // Инициализируем таймер для игрового цикла (50-60 FPS = 16-20 ms)
        _gameTimer = new System.Windows.Forms.Timer();
        _gameTimer.Interval = 16; // ~60 FPS
        _gameTimer.Tick += GameTimer_Tick;
        _gameTimer.Start();

        this.Paint += Form1_Paint;
        this.KeyDown += Form1_KeyDown;
        this.KeyUp += Form1_KeyUp;
    }

    private void GameTimer_Tick(object sender, EventArgs e)
    {
        // Обновляем управление игрока
        _gameManager.Player.SetInput(_keyLeft, _keyRight, _keyUp, _keyDown);

        // Обновляем игровую логику
        _gameManager.Update();

        // Перерисовываем форму (вызывает Paint событие)
        this.Invalidate();
    }

    private void Form1_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        // Рисуем мир игры
        _gameManager.DrawGame(e.Graphics, this.ClientSize.Width, this.ClientSize.Height);
    }

    private void Form1_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.A:
                _keyLeft = true;
                break;
            case Keys.D:
                _keyRight = true;
                break;
            case Keys.W:
                _keyUp = true;
                break;
            case Keys.S:
                _keyDown = true;
                break;
            case Keys.R:
                if (_gameManager.IsGameOver)
                    _gameManager.Reset();
                break;
            case Keys.Escape:
                this.Close();
                break;
        }
    }

    private void Form1_KeyUp(object sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.A:
                _keyLeft = false;
                break;
            case Keys.D:
                _keyRight = false;
                break;
            case Keys.W:
                _keyUp = false;
                break;
            case Keys.S:
                _keyDown = false;
                break;
        }
    }
}
