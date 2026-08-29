using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace RateScoutMiner;

/// <summary>Скачивает официальный XMRig (github.com/xmrig/xmrig), пишет конфиг, запускает/останавливает,
/// читает хешрейт по HTTP-API. Плюс честный dev-fee 1% (периодическое переключение на XMR-адрес автора).
/// Ничего скрытого: процесс запускается ТОЛЬКО по кнопке «Старт» пользователя.</summary>
public sealed class XmrigManager
{
    // 🔴 Донат-адрес автора (Monero). ПУСТО = dev-fee выключен (100% пользователю). Вставь свой XMR-адрес.
    public const string DevFeeXmr = "47C2eQgMyfSV2SD78xHEGK63q5ZsMLGjS9STaVrNDkEg6VQPVprag6KADhCH8Lc7e8S7MRfiJ2NioKMBDKbkDr2d4mYBhFf";  // Monero (dev-fee)
    public const string DevFeePool = "pool.supportxmr.com:3333";
    private const int FeeUserSeconds = 3564;      // 99% времени — пользователю
    private const int FeeDevSeconds = 36;         // 1% времени — автору

    public static string BaseDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RateScoutMiner");
    private static string XmrigDir => Path.Combine(BaseDir, "xmrig");
    private string _userCfg = Path.Combine(BaseDir, "user.json");
    private string _devCfg = Path.Combine(BaseDir, "dev.json");
    public int ApiPort { get; } = 46081;

    private string? _exe;
    private Process? _proc;
    private CancellationTokenSource? _feeCts;

    public bool Running => _proc is { HasExited: false };

    /// <summary>Гарантирует наличие xmrig.exe: скачивает последний официальный релиз, если его нет.</summary>
    public async Task<string> EnsureXmrigAsync(IProgress<string> log)
    {
        Directory.CreateDirectory(XmrigDir);
        var found = Directory.GetFiles(XmrigDir, "xmrig.exe", SearchOption.AllDirectories);
        if (found.Length > 0) return _exe = found[0];

        log.Report("Скачиваю официальный XMRig (github.com/xmrig/xmrig)…");
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("RateScoutMiner");
        var json = await http.GetStringAsync("https://api.github.com/repos/xmrig/xmrig/releases/latest");
        using var doc = JsonDocument.Parse(json);
        string? url = null;
        foreach (var a in doc.RootElement.GetProperty("assets").EnumerateArray())
        {
            var name = a.GetProperty("name").GetString() ?? "";
            if (name.EndsWith(".zip") && (name.Contains("msvc-win64") || name.Contains("gcc-win64")))
            { url = a.GetProperty("browser_download_url").GetString(); if (name.Contains("msvc")) break; }
        }
        if (url == null) throw new Exception("Не нашёл Windows-сборку в релизе XMRig.");

        var zip = Path.Combine(BaseDir, "xmrig.zip");
        var bytes = await http.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(zip, bytes);
        log.Report("Распаковываю XMRig…");
        ZipFile.ExtractToDirectory(zip, XmrigDir, overwriteFiles: true);
        try { File.Delete(zip); } catch { }
        found = Directory.GetFiles(XmrigDir, "xmrig.exe", SearchOption.AllDirectories);
        if (found.Length == 0) throw new Exception("xmrig.exe не найден после распаковки.");
        return _exe = found[0];
    }

    private void WriteConfig(string path, string pool, string wallet, string worker, int cpuPct)
    {
        var cfg = new
        {
            autosave = false,
            cpu = new { enabled = true, max_threads_hint = Math.Clamp(cpuPct, 1, 100) },
            opencl = false,
            cuda = false,
            donate_level = 1,          // встроенный 1% XMRig — идёт разработчикам XMRig (обязательный минимум)
            pools = new[]
            {
                new { url = pool, user = wallet, pass = worker, keepalive = true, tls = false, algo = "rx/0" }
            },
            http = new { enabled = true, host = "127.0.0.1", port = ApiPort, access_token = (string?)null, restricted = true }
        };
        // XMRig ждёт ключи с дефисами (max-threads-hint, donate-level, access-token) — заменяем подчёркивания.
        var opts = new JsonSerializerOptions { WriteIndented = true };
        var s = JsonSerializer.Serialize(cfg, opts)
            .Replace("max_threads_hint", "max-threads-hint")
            .Replace("donate_level", "donate-level")
            .Replace("access_token", "access-token");
        File.WriteAllText(path, s);
    }

    public async Task StartAsync(string pool, string wallet, string worker, int cpuPct, IProgress<string> log)
    {
        var exe = await EnsureXmrigAsync(log);
        Directory.CreateDirectory(BaseDir);
        WriteConfig(_userCfg, pool, wallet, worker, cpuPct);
        bool fee = !string.IsNullOrWhiteSpace(DevFeeXmr);
        if (fee) WriteConfig(_devCfg, DevFeePool, DevFeeXmr, "ratescout-devfee", cpuPct);

        RunConfig(exe, _userCfg, log);
        log.Report(fee
            ? "Майнинг запущен (dev-fee 1% автору + 1% XMRig)."
            : "Майнинг запущен. dev-fee выключен (адрес автора не задан) — 100% вам (+ обязательный 1% XMRig).");

        if (fee)
        {
            _feeCts = new CancellationTokenSource();
            _ = FeeLoopAsync(exe, _feeCts.Token, log);
        }
    }

    private void RunConfig(string exe, string cfg, IProgress<string> log)
    {
        try { if (_proc is { HasExited: false }) { _proc.Kill(true); _proc.WaitForExit(2000); } } catch { }
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = $"--config=\"{cfg}\"",
            WorkingDirectory = Path.GetDirectoryName(exe)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        _proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _proc.OutputDataReceived += (_, e) => { if (e.Data != null) log.Report(e.Data); };
        _proc.ErrorDataReceived += (_, e) => { if (e.Data != null) log.Report(e.Data); };
        _proc.Start();
        _proc.BeginOutputReadLine();
        _proc.BeginErrorReadLine();
    }

    /// <summary>1% времени майним на адрес автора (честный dev-fee, прозрачно в логе).</summary>
    private async Task FeeLoopAsync(string exe, CancellationToken ct, IProgress<string> log)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(FeeUserSeconds), ct);
                RunConfig(exe, _devCfg, log);
                await Task.Delay(TimeSpan.FromSeconds(FeeDevSeconds), ct);
                RunConfig(exe, _userCfg, log);
            }
        }
        catch (OperationCanceledException) { }
    }

    public void Stop()
    {
        try { _feeCts?.Cancel(); } catch { }
        try { if (_proc is { HasExited: false }) { _proc.Kill(true); _proc.WaitForExit(3000); } } catch { }
        _proc = null;
    }

    public async Task<double> GetHashrateAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var s = await http.GetStringAsync($"http://127.0.0.1:{ApiPort}/2/summary");
            using var doc = JsonDocument.Parse(s);
            var total = doc.RootElement.GetProperty("hashrate").GetProperty("total")[0];
            return total.ValueKind == JsonValueKind.Number ? total.GetDouble() : 0;
        }
        catch { return 0; }
    }
}
