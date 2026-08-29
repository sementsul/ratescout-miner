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

    private readonly TextBox _pool = New("pool.supportxmr.com:3333");
    private readonly TextBox _wallet = New("");
    private readonly TextBox _worker = New("rig1");
    private readonly NumericUpDown _cpu = new() { Minimum = 1, Maximum = 100, Value = 50 };
    private readonly Button _start = new() { Text = "СТАРТ" };
    private readonly Button _stop = new() { Text = "СТОП", Enabled = false };
    private readonly Label _hash = new() { Text = "Хешрейт: —", AutoSize = true };
    private readonly TextBox _log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly LinkLabel _footer = new() { Text = "RateScout Miner · miner.ratescout.ru" };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 3000 };

    private readonly XmrigManager _mgr = new();

    public MainForm()
    {
        Text = "RateScout Miner ⛏";
        Font = new Font("Consolas", 9.5f);
        BackColor = Bg;
        ForeColor = Fg;
        ClientSize = new Size(620, 520);
        MinimumSize = new Size(560, 480);
        StartPosition = FormStartPosition.CenterScreen;

        int y = 16;
        AddRow("Пул:", _pool, ref y);
        AddRow("XMR-кошелёк (куда майнить):", _wallet, ref y);
        AddRow("Воркер:", _worker, ref y);

        var lblCpu = Lbl("Нагрузка CPU, %:", 16, y + 4);
        _cpu.SetBounds(230, y, 80, 26); Style(_cpu);
        Controls.Add(lblCpu); Controls.Add(_cpu);
        y += 40;

        _start.SetBounds(16, y, 140, 40); StyleBtn(_start, Cyan, Bg);
        _stop.SetBounds(168, y, 140, 40); StyleBtn(_stop, Panel, Cyan);
        _hash.Location = new Point(330, y + 10); _hash.ForeColor = Cyan;
        Controls.AddRange([_start, _stop, _hash]);
        y += 52;

        var note = new Label
        {
            Text = "Комиссия: 1% времени — автору (dev-fee) + обязательный 1% XMRig. " +
                   "Майнит на ЭТОЙ машине только по кнопке «Старт». Антивирус может пометить XMRig как riskware.",
            Location = new Point(16, y), Size = new Size(588, 40), ForeColor = Color.FromArgb(0xFF, 0xFF, 0x55),
        };
        Controls.Add(note);
        y += 46;

        Style(_log); _log.SetBounds(16, y, 588, 380 - y + 120);
        _log.Height = ClientSize.Height - y - 34;
        _log.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        Controls.Add(_log);

        _footer.SetBounds(16, ClientSize.Height - 24, 588, 20);
        _footer.LinkColor = Cyan; _footer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _footer.LinkClicked += (_, _) => Open(ProjectUrl);
        Controls.Add(_footer);

        _start.Click += OnStart;
        _stop.Click += (_, _) => StopMining();
        _timer.Tick += async (_, _) => await Tick();
        FormClosing += (_, _) => _mgr.Stop();
    }

    private async void OnStart(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_wallet.Text))
        {
            MessageBox.Show(this, "Укажите XMR-кошелёк для майнинга.", "RateScout Miner",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
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

    // --- UI helpers (DOS-стиль как на сайте) ---
    private void AddRow(string label, TextBox box, ref int y)
    {
        Controls.Add(Lbl(label, 16, y + 4));
        box.SetBounds(230, y, 374, 26); Style(box); box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        Controls.Add(box);
        y += 36;
    }
    private static Label Lbl(string t, int x, int y) => new() { Text = t, AutoSize = true, Location = new Point(x, y), ForeColor = Color.FromArgb(0xA8, 0xA8, 0xA8) };
    private static TextBox New(string t) => new() { Text = t };
    private static void Style(Control c) { c.BackColor = Color.FromArgb(0x1A, 0x1A, 0x1A); c.ForeColor = Color.FromArgb(0x55, 0xFF, 0xFF); }
    private static void StyleBtn(Button b, Color bg, Color fg)
    { b.BackColor = bg; b.ForeColor = fg; b.FlatStyle = FlatStyle.Flat; b.Font = new Font("Consolas", 11f, FontStyle.Bold); }

    private static void Open(string url)
    { try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { } }
}
