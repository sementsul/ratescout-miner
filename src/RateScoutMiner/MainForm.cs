using System.Diagnostics;
using System.Windows.Forms;

namespace RateScoutMiner;

public sealed class MainForm : Form
{
    private const string ProjectUrl = "https://miner.ratescout.ru";
    private static readonly Color Bg = Color.FromArgb(0x11, 0x11, 0x11);
    private static readonly Color Panel = Color.FromArgb(0x1A, 0x1A, 0x1A);
    private static readonly Color Cyan = Color.FromArgb(0x55, 0xFF, 0xFF);
    private static readonly Color Fg = Color.FromArgb(0xA8, 0xA8, 0xA8);
    private static readonly Color Yellow = Color.FromArgb(0xFF, 0xFF, 0x55);

    private readonly TextBox _pool = new();
    private readonly TextBox _wallet = new();
    private readonly TextBox _worker = new();
    private readonly NumericUpDown _cpu = new() { Minimum = 1, Maximum = 100, Value = 50 };
    private readonly Button _start = new() { Text = "СТАРТ" };
    private readonly Button _stop = new() { Text = "СТОП", Enabled = false };
    private readonly Label _hash = new() { Text = "Хешрейт: —", AutoSize = true };
    private readonly TextBox _log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly LinkLabel _footer = new() { Text = "RateScout Miner · miner.ratescout.ru" };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 3000 };

    private readonly XmrigManager _mgr = new();
    private readonly Settings _cfg = Settings.Load();

    public MainForm()
    {
        Text = "RateScout Miner ⛏";
        Font = new Font("Consolas", 9.5f);
        BackColor = Bg;
        ForeColor = Fg;
        ClientSize = new Size(640, 580);
        MinimumSize = new Size(560, 520);
        StartPosition = FormStartPosition.CenterScreen;

        int W = ClientSize.Width - 32;
        int y = 12;
        // подписи НАД полями (полноширинные) — чтобы длинный текст не налезал на поле
        StackRow("Пул:", _pool, W, ref y);
        StackRow("XMR-кошелёк (куда майнить):", _wallet, W, ref y);
        StackRow("Воркер:", _worker, W, ref y);

        Controls.Add(Lbl("Нагрузка CPU, %:", 16, y)); y += 20;
        _cpu.SetBounds(16, y, 90, 26); Style(_cpu); Controls.Add(_cpu);
        y += 40;

        _start.SetBounds(16, y, 150, 40); StyleBtn(_start, Cyan, Bg);
        _stop.SetBounds(176, y, 150, 40); StyleBtn(_stop, Panel, Cyan);
        _hash.Location = new Point(340, y + 11); _hash.ForeColor = Cyan;
        Controls.AddRange([_start, _stop, _hash]);
        y += 52;

        var note = new Label
        {
            Text = "Комиссия: 1% времени — автору (dev-fee) + обязательный 1% XMRig. " +
                   "Майнит на ЭТОЙ машине только по кнопке «Старт». Антивирус может пометить XMRig как riskware.",
            Location = new Point(16, y), Size = new Size(W, 38), ForeColor = Yellow,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        Controls.Add(note);
        y += 44;

        Style(_log);
        _log.SetBounds(16, y, W, ClientSize.Height - y - 34);
        _log.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        Controls.Add(_log);

        _footer.SetBounds(16, ClientSize.Height - 24, W, 20);
        _footer.LinkColor = Cyan;
        _footer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _footer.LinkClicked += (_, _) => Open(ProjectUrl);
        Controls.Add(_footer);

        // применяем сохранённые настройки
        _pool.Text = _cfg.Pool; _wallet.Text = _cfg.Wallet; _worker.Text = _cfg.Worker;
        _cpu.Value = Math.Clamp(_cfg.Cpu, 1, 100);

        _start.Click += OnStart;
        _stop.Click += (_, _) => StopMining();
        _timer.Tick += async (_, _) => await Tick();
        FormClosing += (_, _) => { SaveSettings(); _mgr.Stop(); };
    }

    private void SaveSettings()
    {
        _cfg.Pool = _pool.Text.Trim();
        _cfg.Wallet = _wallet.Text.Trim();
        _cfg.Worker = _worker.Text.Trim();
        _cfg.Cpu = (int)_cpu.Value;
        _cfg.Save();
    }

    private async void OnStart(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_wallet.Text))
        {
            MessageBox.Show(this, "Укажите XMR-кошелёк для майнинга.", "RateScout Miner",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        SaveSettings();                         // автосохранение при старте
        _start.Enabled = false;
        SetInputs(false);
        var log = new Progress<string>(Append);
        try
        {
            await _mgr.StartAsync(_pool.Text.Trim(), _wallet.Text.Trim(),
                string.IsNullOrWhiteSpace(_worker.Text) ? "rig1" : _worker.Text.Trim(),
                (int)_cpu.Value, log);
            _stop.Enabled = true;
            _timer.Start();
        }
        catch (Exception ex)
        {
            Append("ОШИБКА: " + ex.Message);
            MessageBox.Show(this, ex.Message, "RateScout Miner — ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _start.Enabled = true; SetInputs(true);
        }
    }

    private void StopMining()
    {
        _timer.Stop();
        _mgr.Stop();
        _hash.Text = "Хешрейт: —";
        _start.Enabled = true; _stop.Enabled = false; SetInputs(true);
        Append("Майнинг остановлен.");
    }

    private async Task Tick()
    {
        if (!_mgr.Running) { StopMining(); return; }
        var h = await _mgr.GetHashrateAsync();
        _hash.Text = h > 0 ? $"Хешрейт: {h:0.0} H/s" : "Хешрейт: (запуск…)";
    }

    private void SetInputs(bool on) { _pool.Enabled = _wallet.Enabled = _worker.Enabled = _cpu.Enabled = on; }
    private void Append(string s) => _log.AppendText($"{DateTime.Now:HH:mm:ss}  {s}{Environment.NewLine}");

    // --- UI helpers ---
    private void StackRow(string label, TextBox box, int w, ref int y)
    {
        Controls.Add(Lbl(label, 16, y));
        y += 20;
        box.SetBounds(16, y, w, 26); Style(box);
        box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        Controls.Add(box);
        y += 34;
    }
    private static Label Lbl(string t, int x, int y) => new() { Text = t, AutoSize = true, Location = new Point(x, y), ForeColor = Color.FromArgb(0xA8, 0xA8, 0xA8) };
    private static void Style(Control c) { c.BackColor = Color.FromArgb(0x1A, 0x1A, 0x1A); c.ForeColor = Color.FromArgb(0x55, 0xFF, 0xFF); }
    private static void StyleBtn(Button b, Color bg, Color fg)
    { b.BackColor = bg; b.ForeColor = fg; b.FlatStyle = FlatStyle.Flat; b.Font = new Font("Consolas", 11f, FontStyle.Bold); }
    private static void Open(string url)
    { try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { } }
}
