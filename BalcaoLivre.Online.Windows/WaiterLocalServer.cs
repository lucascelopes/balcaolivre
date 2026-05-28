using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BalcaoLivre.Online.Windows;

public sealed class WaiterLocalServer : IAsyncDisposable
{
    private readonly Func<Task<WaiterStateDto>> _getState;
    private readonly Func<WaiterOpenBoardRequest, Task<WaiterActionResult>> _openBoard;
    private readonly Func<WaiterAddProductRequest, Task<WaiterActionResult>> _addProduct;
    private readonly Func<WaiterBoardNoteRequest, Task<WaiterActionResult>> _saveBoardNote;
    private readonly Func<WaiterRemoveLineRequest, Task<WaiterActionResult>> _removeLine;
    private readonly Func<WaiterBoardRequest, Task<WaiterActionResult>> _requestBill;
    private WebApplication? _app;

    public WaiterLocalServer(
        int port,
        Func<Task<WaiterStateDto>> getState,
        Func<WaiterOpenBoardRequest, Task<WaiterActionResult>> openBoard,
        Func<WaiterAddProductRequest, Task<WaiterActionResult>> addProduct,
        Func<WaiterBoardNoteRequest, Task<WaiterActionResult>> saveBoardNote,
        Func<WaiterRemoveLineRequest, Task<WaiterActionResult>> removeLine,
        Func<WaiterBoardRequest, Task<WaiterActionResult>> requestBill)
    {
        Port = port;
        _getState = getState;
        _openBoard = openBoard;
        _addProduct = addProduct;
        _saveBoardNote = saveBoardNote;
        _removeLine = removeLine;
        _requestBill = requestBill;
    }

    public int Port { get; }
    public string LocalUrl => $"http://localhost:{Port}/garcom";
    public string NetworkUrl => $"http://{GetLanIpAddress()}:{Port}/garcom";

    public async Task StartAsync()
    {
        if (_app is not null)
        {
            return;
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = [],
            ApplicationName = typeof(WaiterLocalServer).Assembly.FullName
        });
        builder.WebHost.UseUrls($"http://0.0.0.0:{Port}");
        builder.Services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = false;
        });

        var app = builder.Build();
        app.MapGet("/", () => Results.Redirect("/garcom"));
        app.MapGet("/garcom", () => Results.Content(WaiterWebAssets.Html, "text/html; charset=utf-8"));
        app.MapGet("/garcom/styles.css", () => Results.Content(WaiterWebAssets.Css, "text/css; charset=utf-8"));
        app.MapGet("/garcom/app.js", () => Results.Content(WaiterWebAssets.Js, "application/javascript; charset=utf-8"));
        app.MapGet("/api/waiter/state", async () => Results.Json(await _getState()));
        app.MapPost("/api/waiter/open", async (WaiterOpenBoardRequest request) => Results.Json(await _openBoard(request)));
        app.MapPost("/api/waiter/add", async (WaiterAddProductRequest request) => Results.Json(await _addProduct(request)));
        app.MapPost("/api/waiter/note", async (WaiterBoardNoteRequest request) => Results.Json(await _saveBoardNote(request)));
        app.MapPost("/api/waiter/remove", async (WaiterRemoveLineRequest request) => Results.Json(await _removeLine(request)));
        app.MapPost("/api/waiter/bill", async (WaiterBoardRequest request) => Results.Json(await _requestBill(request)));
        await app.StartAsync();
        _app = app;
    }

    public async Task StopAsync()
    {
        if (_app is null)
        {
            return;
        }

        await _app.StopAsync(TimeSpan.FromSeconds(2));
        await _app.DisposeAsync();
        _app = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private static string GetLanIpAddress()
    {
        try
        {
            var networkAddress = NetworkInterface.GetAllNetworkInterfaces()
                .Where(IsUsableWaiterNetworkInterface)
                .SelectMany(adapter =>
                {
                    var properties = adapter.GetIPProperties();
                    var hasGateway = properties.GatewayAddresses.Any(gateway =>
                        gateway.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !gateway.Address.Equals(IPAddress.Any) &&
                        !gateway.Address.Equals(IPAddress.None));

                    return properties.UnicastAddresses
                        .Where(address =>
                            address.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(address.Address) &&
                            !address.Address.ToString().StartsWith("169.254.", StringComparison.Ordinal))
                        .Select(address => new
                        {
                            Adapter = adapter,
                            Address = address.Address,
                            HasGateway = hasGateway
                        });
                })
                .OrderByDescending(item => item.HasGateway)
                .ThenByDescending(item => item.Adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                .ThenByDescending(item => item.Adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                .Select(item => item.Address.ToString())
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(networkAddress))
            {
                return networkAddress;
            }

            return Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .FirstOrDefault(address =>
                    address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address) &&
                    !address.ToString().StartsWith("169.254.", StringComparison.Ordinal))?
                .ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    private static bool IsUsableWaiterNetworkInterface(NetworkInterface adapter)
    {
        if (adapter.OperationalStatus != OperationalStatus.Up ||
            adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
            adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
        {
            return false;
        }

        var name = $"{adapter.Name} {adapter.Description}".ToLowerInvariant();
        return !name.Contains("virtual", StringComparison.Ordinal) &&
               !name.Contains("docker", StringComparison.Ordinal) &&
               !name.Contains("hyper-v", StringComparison.Ordinal) &&
               !name.Contains("vmware", StringComparison.Ordinal) &&
               !name.Contains("virtualbox", StringComparison.Ordinal) &&
               !name.Contains("wsl", StringComparison.Ordinal) &&
               !name.Contains("tailscale", StringComparison.Ordinal) &&
               !name.Contains("zerotier", StringComparison.Ordinal);
    }
}

internal static class WaiterWebAssets
{
    public const string Html = """
<!doctype html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover" />
  <title>Balcao Livre - Garcom</title>
  <link rel="stylesheet" href="/garcom/styles.css" />
</head>
<body>
  <main class="shell">
    <header class="topbar">
      <div class="brand">
        <span class="brand-mark">BL</span>
        <div>
          <strong id="restaurantName">Carregando loja...</strong>
          <span id="serverTime">conectando...</span>
        </div>
      </div>
      <div id="livePill" class="live-pill" aria-live="polite">
        <span></span>
        <strong id="liveState">Local</strong>
      </div>
    </header>

    <section class="operator">
      <label>Garcom/operador
        <select id="staffSelect"></select>
      </label>
      <label>Mesa
        <input id="tableInput" inputmode="numeric" placeholder="000001" />
      </label>
      <label>Cliente opcional
        <input id="customerInput" placeholder="Nome da mesa" />
      </label>
      <button id="openBtn">Abrir mesa</button>
    </section>

    <nav class="mobile-tabs" aria-label="Areas do garcom">
      <button class="active" data-view="tables" type="button">Mesas</button>
      <button data-view="order" type="button">Conta</button>
      <button data-view="products" type="button">Produtos</button>
    </nav>

    <section class="layout">
      <aside class="panel tables-panel active-view" data-panel="tables">
        <div class="panel-head">
          <h1>Mesas</h1>
          <span id="tableCount">0</span>
        </div>
        <div id="tablesGrid" class="tables-grid"></div>
      </aside>

      <section class="panel order-panel" data-panel="order">
        <div class="panel-head">
          <div>
            <h1 id="ticketTitle">Selecione uma mesa</h1>
            <span id="ticketStatus">Livre/Ocupada aparece aqui</span>
          </div>
          <strong id="ticketTotal">R$ 0,00</strong>
        </div>
        <div class="board-note-row">
          <label>Observacao da mesa
            <textarea id="boardNoteInput" rows="2" placeholder="Ex.: cliente com pressa, sem taxa, aniversario..."></textarea>
          </label>
          <button id="saveNoteBtn" class="light" type="button">Salvar obs</button>
        </div>
        <div id="ticketLines" class="ticket-lines"></div>
        <button id="billBtn" class="bill">Pedir conta</button>
      </section>

      <section class="panel products-panel" data-panel="products">
        <div class="panel-head">
          <h1>Produtos</h1>
          <span id="productCount">0</span>
        </div>
        <div class="quick-add">
          <input id="productSearch" placeholder="Buscar ou digitar codigo" inputmode="numeric" />
          <input id="qtyInput" value="1" inputmode="numeric" />
        </div>
        <input id="noteInput" class="note" placeholder="Observacao do item: sem cebola, ponto da carne..." />
        <div id="categoryChips" class="chips"></div>
        <div id="productsGrid" class="products-grid"></div>
      </section>
    </section>

    <section id="paymentSheet" class="payment-sheet" hidden>
      <div class="payment-card" role="dialog" aria-modal="true" aria-labelledby="paymentTitle">
        <header>
          <div>
            <h1 id="paymentTitle">Pedir conta</h1>
            <span id="paymentSubtitle">Escolha como a mesa vai sair.</span>
          </div>
          <strong id="paymentTotal">R$ 0,00</strong>
        </header>

        <button id="paidToggle" class="paid-toggle" type="button" aria-pressed="false">
          <span></span>
          Pago agora
        </button>

        <div id="paymentMethods" class="payment-methods"></div>

        <label>Valor recebido
          <input id="paymentAmount" inputmode="decimal" value="0,00" />
        </label>
        <div id="changePreview" class="change-preview">Troco: R$ 0,00</div>

        <div class="payment-actions">
          <button id="cancelPaymentBtn" class="light" type="button">Cancelar</button>
          <button id="confirmBillBtn" type="button">Imprimir conta</button>
        </div>
      </div>
    </section>

    <div id="toast" class="toast"></div>
  </main>
  <script src="/garcom/app.js"></script>
</body>
</html>
""";

    public const string Css = """
:root {
  --bg:#e8eef3;
  --panel:#ffffff;
  --panel-soft:#f7fafc;
  --ink:#0e1a24;
  --muted:#637588;
  --line:#cbd9e5;
  --blue:#104f7e;
  --blue2:#1d6fa7;
  --green:#0a8a79;
  --green-strong:#148a37;
  --green-soft:#dff6e3;
  --red:#b01c25;
  --red-soft:#fde8e8;
  --amber:#a56707;
  --shadow:0 12px 32px rgba(11,33,55,.10);
  font-family: Inter, Segoe UI, system-ui, Arial, sans-serif;
}
* { box-sizing:border-box; letter-spacing:0; }
html,body { height:100%; overflow:hidden; }
body { margin:0; background:var(--bg); color:var(--ink); }
button,input,select,textarea { font:inherit; min-width:0; max-width:100%; }
button { border:0; border-radius:8px; min-height:44px; padding:0 16px; font-weight:850; background:var(--green); color:white; cursor:pointer; }
button:active { transform:translateY(1px); }
.shell { height:100vh; height:100dvh; padding:12px; display:grid; grid-template-rows:auto auto auto minmax(0,1fr); gap:10px; overflow:hidden; }
.topbar { display:flex; justify-content:space-between; align-items:center; gap:12px; background:var(--blue); color:white; margin:-12px -12px 0; padding:calc(12px + env(safe-area-inset-top)) 16px 12px; box-shadow:0 10px 28px rgba(10,38,66,.20); }
.brand { min-width:0; flex:1 1 auto; display:flex; align-items:center; gap:10px; }
.brand > div { min-width:0; }
.brand-mark { width:38px; height:38px; flex:0 0 38px; display:grid; place-items:center; border-radius:8px; background:white; color:var(--blue); font-weight:950; }
.topbar strong { display:block; min-width:0; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; font-size:20px; }
.topbar span { display:block; opacity:.84; font-size:12px; margin-top:2px; }
.live-pill { flex:0 0 auto; display:flex; align-items:center; gap:7px; min-height:34px; padding:0 11px; border-radius:999px; background:rgba(255,255,255,.14); border:1px solid rgba(255,255,255,.22); font-size:12px; }
.live-pill span { width:9px; height:9px; border-radius:999px; background:#72e58a; box-shadow:0 0 0 4px rgba(114,229,138,.16); }
.live-pill.offline span { background:#ff8b82; box-shadow:0 0 0 4px rgba(255,139,130,.18); }
.operator { display:grid; grid-template-columns:minmax(180px,1.15fr) 130px minmax(160px,1fr) 150px; gap:10px; align-items:end; min-height:0; }
.operator > *, .mobile-tabs > *, .layout, .panel { min-width:0; }
label { display:grid; gap:5px; color:var(--muted); font-weight:850; font-size:12px; }
input,select,textarea { width:100%; min-height:46px; border:1px solid var(--line); border-radius:8px; padding:0 12px; background:white; color:var(--ink); font-weight:780; outline:none; text-overflow:ellipsis; }
select { padding-right:34px; white-space:nowrap; overflow:hidden; }
textarea { min-height:58px; padding:10px 12px; resize:none; line-height:1.25; text-overflow:clip; }
input:focus,select:focus,textarea:focus { border-color:var(--green); box-shadow:0 0 0 3px rgba(15,130,118,.13); }
.mobile-tabs { display:none; }
.layout { min-height:0; display:grid; grid-template-columns:minmax(280px,330px) minmax(320px,400px) minmax(420px,1fr); gap:12px; }
.panel { min-height:0; background:var(--panel); border:1px solid var(--line); border-radius:8px; padding:12px; box-shadow:var(--shadow); display:grid; }
.tables-panel { grid-template-rows:auto minmax(0,1fr); }
.order-panel { grid-template-rows:auto auto minmax(0,1fr) auto; }
.products-panel { grid-template-rows:auto auto auto auto minmax(0,1fr); }
.panel-head { display:flex; justify-content:space-between; align-items:flex-start; gap:10px; border-bottom:1px solid var(--line); padding-bottom:10px; margin-bottom:10px; }
h1 { margin:0; font-size:21px; line-height:1.1; }
.panel-head span { display:block; color:var(--muted); margin-top:4px; font-weight:700; }
.tables-grid { min-height:0; display:grid; grid-template-columns:repeat(3,minmax(0,1fr)); align-content:start; gap:8px; overflow:auto; padding-right:2px; }
.table-card { background:white; border:1px solid var(--line); border-top:6px solid #75d86d; min-height:78px; border-radius:8px; padding:10px 6px; display:grid; place-items:center; text-align:center; color:var(--ink); }
.table-card strong { display:block; font-size:18px; }
.table-card span { display:inline-block; margin-top:7px; padding:3px 8px; border-radius:999px; background:var(--green-soft); color:var(--green-strong); font-size:11px; font-weight:950; }
.table-card.busy { border-top-color:#f49c92; }
.table-card.busy span { background:#ffdcd8; color:#8f1b16; }
.table-card.selected { outline:3px solid var(--blue2); outline-offset:-3px; background:#f3f9ff; }
.ticket-lines { min-height:0; overflow:auto; border:1px solid var(--line); border-radius:8px; background:var(--panel-soft); }
.empty { padding:24px; color:var(--muted); text-align:center; font-weight:700; }
.line { display:grid; grid-template-columns:1fr auto auto; gap:8px; align-items:center; padding:12px; border-bottom:1px solid var(--line); }
.line:last-child { border-bottom:0; }
.line small { display:block; color:var(--muted); margin-top:3px; font-weight:700; }
.line strong { font-size:15px; }
.line .remove { min-height:34px; padding:0 10px; border-radius:8px; background:var(--red); }
#ticketTotal { color:var(--red); font-size:28px; }
.board-note-row { display:grid; grid-template-columns:minmax(0,1fr) 126px; gap:8px; align-items:end; margin-bottom:10px; }
.board-note-row button { min-height:58px; padding:0 10px; }
.bill { width:100%; margin-top:12px; background:var(--blue2); }
.quick-add { display:grid; grid-template-columns:1fr 74px; gap:10px; }
.note { margin-top:10px; }
.chips { display:flex; flex-wrap:wrap; gap:8px; margin:12px 0; }
.chip { background:white; color:var(--blue); border:1px solid var(--line); min-height:34px; padding:0 12px; border-radius:999px; }
.chip.active { background:#e7f2fb; border-color:var(--blue2); }
.products-grid { min-height:0; display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); align-content:start; gap:8px; overflow:auto; padding-right:2px; }
.product { min-height:96px; background:white; border:1px solid var(--line); border-radius:8px; padding:12px; text-align:left; color:var(--ink); display:grid; align-content:space-between; }
.product strong { display:block; font-size:16px; line-height:1.15; }
.product small { color:var(--muted); font-weight:800; }
.product b { color:var(--green); font-size:18px; }
.toast { position:fixed; left:14px; right:14px; bottom:14px; transform:translateY(calc(100% + 28px)); opacity:0; visibility:hidden; pointer-events:none; transition:transform .18s, opacity .18s, visibility 0s linear .18s; background:var(--green); color:white; border-radius:8px; padding:14px 16px; font-weight:850; box-shadow:0 16px 40px rgba(0,0,0,.22); z-index:9; }
.toast.show { transform:translateY(0); opacity:1; visibility:visible; pointer-events:auto; transition:transform .18s, opacity .18s; }
.toast.error { background:var(--red); }
.payment-sheet { position:fixed; inset:0; z-index:20; display:grid; place-items:end center; padding:14px; background:rgba(8,24,38,.34); }
.payment-sheet[hidden] { display:none; }
.payment-card { width:min(520px,100%); display:grid; gap:12px; border:1px solid var(--line); border-radius:10px; background:white; padding:14px; box-shadow:0 24px 70px rgba(6,24,42,.28); }
.payment-card header { display:flex; justify-content:space-between; gap:12px; align-items:flex-start; border-bottom:1px solid var(--line); padding-bottom:10px; }
.payment-card header span { display:block; margin-top:4px; color:var(--muted); font-size:12px; font-weight:750; }
.payment-card header strong { color:var(--red); font-size:26px; white-space:nowrap; }
.paid-toggle { display:flex; align-items:center; justify-content:flex-start; gap:10px; border:1px solid var(--line); background:white; color:var(--ink); }
.paid-toggle span { width:24px; height:24px; border-radius:999px; border:2px solid var(--line); background:white; }
.paid-toggle.active { border-color:var(--green); background:var(--green-soft); color:var(--green); }
.paid-toggle.active span { border-color:var(--green); background:var(--green); box-shadow:inset 0 0 0 5px var(--green-soft); }
.payment-methods { display:grid; grid-template-columns:repeat(3,minmax(0,1fr)); gap:8px; }
.pay-method { min-height:40px; padding:0 8px; border:1px solid var(--line); background:white; color:var(--blue); }
.pay-method.active { border-color:var(--blue); background:#e7f2fb; color:var(--blue); }
.change-preview { min-height:42px; display:flex; align-items:center; padding:0 12px; border-radius:8px; background:var(--panel-soft); border:1px solid var(--line); color:var(--muted); font-weight:850; }
.change-preview.warn { color:var(--amber); background:#fff7e8; border-color:#f0c979; }
.change-preview.error { color:var(--red); background:var(--red-soft); border-color:#f3aaa7; }
.payment-actions { display:grid; grid-template-columns:1fr 1.4fr; gap:8px; }
.light { background:white; color:var(--blue); border:1px solid var(--line); }
@media (max-width: 1120px) {
  .layout { grid-template-columns:300px 1fr; }
  .products-panel { grid-column:1 / -1; }
  .products-grid { grid-template-columns:repeat(3,minmax(0,1fr)); }
}
@media (max-width: 760px) {
  .shell { padding:10px 10px max(10px, env(safe-area-inset-bottom)); grid-template-rows:auto auto 42px minmax(0,1fr); gap:8px; }
  .topbar { margin:-10px -10px 0; padding:calc(10px + env(safe-area-inset-top)) 12px 10px; overflow:hidden; }
  .brand-mark { width:34px; height:34px; flex-basis:34px; }
  .topbar strong { font-size:17px; }
  .topbar span { font-size:11px; }
  .live-pill { min-height:30px; max-width:92px; padding:0 9px; }
  .live-pill strong { overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
  .operator { grid-template-columns:minmax(0,1fr) 112px; gap:8px; }
  .operator label:first-child { grid-column:1 / 2; }
  .operator label:nth-child(2) { grid-column:2 / 3; }
  .operator label:nth-child(3) { grid-column:1 / 2; }
  .operator button { grid-column:2 / 3; min-height:42px; padding:0 10px; align-self:end; }
  label { font-size:11px; gap:4px; }
  input,select,textarea { min-height:42px; padding:0 10px; font-size:15px; }
  textarea { min-height:54px; padding-top:9px; padding-bottom:9px; }
  #staffSelect { padding-right:28px; font-size:14px; font-weight:850; }
  .mobile-tabs { display:grid; grid-template-columns:repeat(3,1fr); gap:6px; min-height:42px; }
  .mobile-tabs button { min-height:42px; background:white; color:var(--blue); border:1px solid var(--line); }
  .mobile-tabs button.active { background:var(--blue); border-color:var(--blue); color:white; }
  .layout { position:relative; display:block; min-height:0; overflow:hidden; }
  .layout .panel { position:absolute; inset:0; display:none; padding:10px; box-shadow:none; }
  .layout .panel.active-view { display:grid; }
  .panel-head { padding-bottom:8px; margin-bottom:8px; }
  h1 { font-size:19px; }
  .panel-head span { font-size:12px; }
  .board-note-row { grid-template-columns:minmax(0,1fr) 112px; gap:8px; margin-bottom:8px; }
  .board-note-row button { min-height:54px; font-size:13px; padding:0 8px; }
  .tables-grid { grid-template-columns:repeat(3,minmax(0,1fr)); gap:8px; }
  .table-card { min-height:84px; padding:8px 4px; }
  .table-card strong { font-size:18px; }
  #ticketTotal { font-size:25px; }
  .line { grid-template-columns:1fr auto; }
  .line .remove { grid-column:1 / -1; }
  .chips { flex-wrap:nowrap; overflow-x:auto; margin:8px 0; padding-bottom:2px; }
  .chip { flex:0 0 auto; }
  .products-grid { grid-template-columns:1fr; }
  .product { min-height:82px; grid-template-columns:1fr auto; align-items:center; align-content:center; gap:10px; }
  .toast { left:10px; right:10px; bottom:max(10px, env(safe-area-inset-bottom)); }
  .payment-sheet { align-items:end; padding:10px; }
  .payment-card { max-height:calc(100dvh - 20px); overflow:auto; }
}
@media (max-width: 520px) {
  .brand { gap:8px; }
  .brand-mark { width:32px; height:32px; flex-basis:32px; }
  .topbar strong { font-size:16px; }
  .live-pill { width:34px; max-width:34px; justify-content:center; padding:0; }
  .live-pill strong { display:none; }
  .operator { grid-template-columns:minmax(0,1fr) 92px; }
  .operator button { font-size:14px; padding:0 8px; }
  .mobile-tabs button { padding:0 8px; font-size:15px; }
  .board-note-row { grid-template-columns:1fr; }
  .board-note-row button { min-height:42px; }
  .tables-grid { grid-template-columns:repeat(2,minmax(0,1fr)); }
}
""";

    public const string Js = """
let state = null;
let selectedBoard = localStorage.getItem('bl_waiter_board') || '';
let selectedCategory = 'TODOS';
let activeView = 'tables';
let loadingState = false;
let paidNow = false;
let selectedPaymentMethod = 'DINHEIRO';
const paymentMethods = ['DINHEIRO', 'PIX', 'CREDITO', 'DEBITO', 'VALE', 'FIADO'];
const $ = (id) => document.getElementById(id);
const qsa = (selector) => Array.from(document.querySelectorAll(selector));
const escapeMap = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };

function escapeHtml(value) {
  return String(value ?? '').replace(/[&<>"']/g, (char) => escapeMap[char]);
}

async function api(path, body) {
  const res = await fetch(path, {
    method: body ? 'POST' : 'GET',
    cache: 'no-store',
    headers: body ? { 'Content-Type': 'application/json' } : {},
    body: body ? JSON.stringify(body) : undefined
  });
  if (!res.ok) throw new Error('Falha de rede');
  return await res.json();
}

async function load(options = {}) {
  if (loadingState) return;
  loadingState = true;
  try {
    state = await api('/api/waiter/state');
    setLiveState(true);
    normalizeSelection();
    render();
  } catch (err) {
    setLiveState(false);
    if (!options.silent) toast('Nao conectou no PDV. Confira se o Windows esta aberto.', true);
  } finally {
    loadingState = false;
  }
}

function money(value) {
  return Number(value || 0).toLocaleString('pt-BR', { style:'currency', currency:'BRL' });
}

function inputMoney(value) {
  return Number(value || 0).toLocaleString('pt-BR', { minimumFractionDigits:2, maximumFractionDigits:2 });
}

function parseMoney(value) {
  const normalized = String(value || '0').replace(/R\$/gi, '').replace(/\./g, '').replace(',', '.').trim();
  const parsed = Number.parseFloat(normalized);
  return Number.isFinite(parsed) ? parsed : 0;
}

function setLiveState(online) {
  $('livePill').classList.toggle('offline', !online);
  $('liveState').textContent = online ? 'Ao vivo' : 'Offline';
  if (!online) $('serverTime').textContent = 'aguardando PDV Windows';
}

function normalizeSelection() {
  if (!state?.boards?.length) return;
  if (!selectedBoard || !state.boards.some((board) => board.number === selectedBoard)) {
    selectedBoard = state.boards[0].number;
    localStorage.setItem('bl_waiter_board', selectedBoard);
  }
}

function currentBoard() {
  return state?.boards?.find(b => b.number === selectedBoard) || null;
}

function compactStaffLabel(staff) {
  const number = String(staff?.number || '').trim();
  const name = String(staff?.name || '').trim().split(/\s+/).filter(Boolean).slice(0, 2).join(' ');
  return [number, name].filter(Boolean).join(' - ') || number || name || 'Garcom';
}

function fullStaffLabel(staff) {
  const number = String(staff?.number || '').trim();
  const name = String(staff?.name || '').trim();
  const role = String(staff?.role || '').trim();
  return `${number}${name ? ' - ' + name : ''}${role ? ' (' + role + ')' : ''}`;
}

function isMobileView() {
  return window.matchMedia('(max-width: 760px)').matches;
}

function preferredBoardView(board) {
  if (!board) return 'tables';
  const status = String(board.status || '').toUpperCase();
  const hasLines = Array.isArray(board.lines) && board.lines.length > 0;
  if (hasLines || status.includes('CONTA') || status.includes('FECH')) return 'order';
  if (status && status !== 'LIVRE') return 'products';
  return 'tables';
}

function setSmartBoardView(board = currentBoard()) {
  if (isMobileView()) setView(preferredBoardView(board));
}

function render() {
  if (!state) return;
  const restaurantName = state.restaurantName || 'Balcao Livre PDV';
  $('restaurantName').textContent = restaurantName;
  document.title = `${restaurantName} - Garcom`;
  $('serverTime').textContent = 'online agora ' + new Date(state.serverTime).toLocaleTimeString('pt-BR');
  renderStaff();
  renderTables();
  renderTicket();
  renderProducts();
  setView(activeView);
}

function renderStaff() {
  const select = $('staffSelect');
  const previous = select.value || localStorage.getItem('bl_waiter_staff') || '';
  const signature = state.staff.map(s => `${s.number}:${s.name}:${s.role}`).join('|');
  if (select.dataset.signature !== signature) {
    select.innerHTML = state.staff.map(s => `<option value="${escapeHtml(s.number)}" title="${escapeHtml(fullStaffLabel(s))}">${escapeHtml(compactStaffLabel(s))}</option>`).join('');
    select.dataset.signature = signature;
  }
  if (previous && [...select.options].some(o => o.value === previous)) select.value = previous;
}

function renderTables() {
  $('tableCount').textContent = state.boards.length;
  const grid = $('tablesGrid');
  const scrollTop = grid.scrollTop;
  grid.innerHTML = state.boards.length ? state.boards.map(b => {
    const busy = b.status !== 'LIVRE';
    const selected = b.number === selectedBoard;
    return `<button class="table-card ${busy ? 'busy' : ''} ${selected ? 'selected' : ''}" data-board="${escapeHtml(b.number)}" type="button">
      <strong>${escapeHtml(b.number)}</strong>
      <span>${escapeHtml(b.status)}</span>
    </button>`;
  }).join('') : '<div class="empty">Crie as mesas no caixa Windows primeiro.</div>';
  grid.scrollTop = scrollTop;

  qsa('[data-board]').forEach((button) => {
    button.addEventListener('click', () => selectBoard(button.dataset.board));
  });
}

function renderTicket() {
  const board = currentBoard();
  if (!board) {
    $('ticketTitle').textContent = 'Selecione uma mesa';
    $('ticketStatus').textContent = 'Toque em uma mesa ou digite o numero.';
    $('ticketTotal').textContent = money(0);
    if (document.activeElement !== $('boardNoteInput')) $('boardNoteInput').value = '';
    $('ticketLines').innerHTML = '<div class="empty">Sem mesa selecionada.</div>';
    return;
  }

  if (document.activeElement !== $('tableInput')) $('tableInput').value = board.number;
  if (document.activeElement !== $('boardNoteInput')) $('boardNoteInput').value = board.notes || '';
  $('ticketTitle').textContent = `${board.kind} ${board.number}`;
  $('ticketStatus').textContent = `${board.status}${board.waiter ? ' | garcom ' + board.waiter : ''}`;
  $('ticketTotal').textContent = board.totalText || money(board.total);
  const lines = $('ticketLines');
  const scrollTop = lines.scrollTop;
  lines.innerHTML = board.lines.length ? board.lines.map(line => `
    <div class="line">
      <div>
        <strong>${escapeHtml(line.quantity)}x ${escapeHtml(line.name)}</strong>
        <small>${escapeHtml(line.code)}${line.note ? ' | ' + escapeHtml(line.note) : ''}</small>
      </div>
      <strong>${escapeHtml(line.totalText)}</strong>
      <button class="remove" data-remove-line="${escapeHtml(line.index)}" type="button">Excluir</button>
    </div>`).join('') : '<div class="empty">Mesa sem itens.</div>';
  lines.scrollTop = scrollTop;

  qsa('[data-remove-line]').forEach((button) => {
    button.addEventListener('click', () => removeLine(Number(button.dataset.removeLine)));
  });
}

function renderProducts() {
  if (!state) return;
  const query = $('productSearch').value.trim().toLowerCase();
  const categories = ['TODOS', ...state.categories];
  $('categoryChips').innerHTML = categories.map(cat => `<button class="chip ${cat === selectedCategory ? 'active' : ''}" data-category="${escapeHtml(cat)}" type="button">${escapeHtml(cat)}</button>`).join('');
  const products = filteredProducts(query);
  $('productCount').textContent = products.length;
  const grid = $('productsGrid');
  const scrollTop = grid.scrollTop;
  grid.innerHTML = products.length ? products.map(p => `
    <button class="product" data-product="${escapeHtml(p.code)}" type="button">
      <span><small>${escapeHtml(p.code)} | ${escapeHtml(p.category)}</small><strong>${escapeHtml(p.name)}</strong></span>
      <b>${escapeHtml(p.priceText)}</b>
    </button>`).join('') : '<div class="empty">Nenhum produto encontrado.</div>';
  grid.scrollTop = scrollTop;

  qsa('[data-category]').forEach((button) => {
    button.addEventListener('click', () => setCategory(button.dataset.category));
  });
  qsa('[data-product]').forEach((button) => {
    button.addEventListener('click', () => addProduct(button.dataset.product));
  });
}

function filteredProducts(query = $('productSearch').value.trim().toLowerCase()) {
  return state.products.filter(p => {
    const byCategory = selectedCategory === 'TODOS' || p.category === selectedCategory;
    const byQuery = !query || p.code.toLowerCase().includes(query) || p.name.toLowerCase().includes(query) || p.category.toLowerCase().includes(query);
    return byCategory && byQuery;
  });
}

function setCategory(category) {
  selectedCategory = category;
  renderProducts();
}

function selectBoard(number) {
  selectedBoard = number;
  localStorage.setItem('bl_waiter_board', selectedBoard);
  render();
  setSmartBoardView();
}

function setView(view) {
  activeView = view;
  qsa('[data-panel]').forEach((panel) => panel.classList.toggle('active-view', panel.dataset.panel === view));
  qsa('[data-view]').forEach((button) => button.classList.toggle('active', button.dataset.view === view));
}

async function openBoard() {
  const staff = $('staffSelect').value;
  localStorage.setItem('bl_waiter_staff', staff);
  const number = $('tableInput').value.trim();
  const customerName = $('customerInput').value.trim();
  const result = await runAction('/api/waiter/open', { kind:'MESA', boardNumber:number, waiterNumber:staff, customerName });
  if (result?.ok && number) selectBoard(number.padStart(6, '0'));
}

async function addProduct(code) {
  const board = currentBoard();
  if (!board) return toast('Selecione ou abra uma mesa primeiro.', true);
  const staff = $('staffSelect').value;
  localStorage.setItem('bl_waiter_staff', staff);
  const quantity = Math.max(1, parseInt($('qtyInput').value || '1', 10));
  const note = $('noteInput').value.trim();
  const result = await runAction('/api/waiter/add', { kind:'MESA', boardNumber:board.number, waiterNumber:staff, productCode:code, quantity, note });
  if (!result) return;
  $('noteInput').value = '';
  $('qtyInput').value = '1';
  if (result.ok) setSmartBoardView();
}

async function saveBoardNote() {
  const board = currentBoard();
  if (!board) return toast('Selecione ou abra uma mesa primeiro.', true);
  const staff = $('staffSelect').value;
  localStorage.setItem('bl_waiter_staff', staff);
  const note = $('boardNoteInput').value.trim();
  const result = await runAction('/api/waiter/note', { kind:'MESA', boardNumber:board.number, waiterNumber:staff, note });
  if (result?.ok) setView('order');
}

async function removeLine(index) {
  const board = currentBoard();
  if (!board) return;
  const result = await runAction('/api/waiter/remove', { kind:'MESA', boardNumber:board.number, lineIndex:index });
  if (result?.ok) setSmartBoardView();
}

async function requestBill() {
  if (!$('paymentSheet').hidden) {
    await confirmBill();
    return;
  }
  openPaymentSheet();
}

function openPaymentSheet() {
  const board = currentBoard();
  if (!board) return toast('Selecione uma mesa.', true);
  if (!board.lines.length) return toast('Mesa sem itens.', true);
  paidNow = false;
  selectedPaymentMethod = selectedPaymentMethod || 'DINHEIRO';
  $('paymentTotal').textContent = board.totalText || money(board.total);
  $('paymentAmount').value = inputMoney(board.total);
  $('paymentSheet').hidden = false;
  renderPaymentMethods();
  refreshPaymentPreview();
}

function closePaymentSheet() {
  $('paymentSheet').hidden = true;
}

function renderPaymentMethods() {
  $('paidToggle').classList.toggle('active', paidNow);
  $('paidToggle').setAttribute('aria-pressed', String(paidNow));
  $('paymentMethods').innerHTML = paymentMethods.map(method => `
    <button class="pay-method ${method === selectedPaymentMethod ? 'active' : ''}" data-pay-method="${method}" type="button">${method}</button>
  `).join('');
  qsa('[data-pay-method]').forEach((button) => {
    button.addEventListener('click', () => {
      selectedPaymentMethod = button.dataset.payMethod;
      renderPaymentMethods();
      refreshPaymentPreview();
    });
  });
}

function refreshPaymentPreview() {
  const board = currentBoard();
  if (!board) return;
  const tendered = parseMoney($('paymentAmount').value);
  const change = Math.max(0, tendered - Number(board.total || 0));
  const remaining = Math.max(0, Number(board.total || 0) - tendered);
  const preview = $('changePreview');
  preview.className = 'change-preview';

  $('confirmBillBtn').textContent = paidNow ? 'Imprimir pago' : 'Imprimir conta';
  $('paymentSubtitle').textContent = paidNow
    ? `Pagamento em ${selectedPaymentMethod}`
    : 'Sem marcar pago, imprime so a conferencia.';

  if (!paidNow) {
    preview.textContent = 'Troco: R$ 0,00';
    return;
  }

  if (tendered <= 0) {
    preview.textContent = 'Informe o valor recebido.';
    preview.classList.add('error');
    return;
  }

  if (change > 0 && selectedPaymentMethod !== 'DINHEIRO') {
    preview.textContent = 'Troco acima do total somente em DINHEIRO.';
    preview.classList.add('error');
    return;
  }

  if (remaining > 0) {
    preview.textContent = `Falta ${money(remaining)} para marcar como pago.`;
    preview.classList.add('warn');
    return;
  }

  preview.textContent = change > 0 ? `Troco: ${money(change)}` : 'Troco: R$ 0,00';
}

async function confirmBill() {
  const board = currentBoard();
  if (!board) return toast('Selecione uma mesa.', true);
  const tendered = parseMoney($('paymentAmount').value);
  if (paidNow) {
    if (tendered < Number(board.total || 0)) {
      toast('Valor recebido menor que o total.', true);
      refreshPaymentPreview();
      return;
    }
    if (tendered > Number(board.total || 0) && selectedPaymentMethod !== 'DINHEIRO') {
      toast('Troco acima do total somente em DINHEIRO.', true);
      refreshPaymentPreview();
      return;
    }
  }

  const result = await runAction('/api/waiter/bill', {
    kind:'MESA',
    boardNumber:board.number,
    paid:paidNow,
    paymentMethod:selectedPaymentMethod,
    tenderedAmount:paidNow ? tendered : 0,
    payer:board.customerName || 'Cliente'
  });
  if (result?.ok) closePaymentSheet();
}

async function runAction(path, body) {
  try {
    const result = await api(path, body);
    afterAction(result);
    return result;
  } catch (err) {
    setLiveState(false);
    toast('Acao nao enviada. Confira se o PDV Windows esta aberto.', true);
    return null;
  }
}

function afterAction(result) {
  if (result.state) state = result.state;
  setLiveState(true);
  normalizeSelection();
  toast(result.message, !result.ok);
  render();
}

function toast(message, error = false) {
  const el = $('toast');
  el.textContent = message;
  el.className = 'toast show' + (error ? ' error' : '');
  clearTimeout(window.__toastTimer);
  window.__toastTimer = setTimeout(() => el.className = 'toast', 2600);
}

$('openBtn').onclick = openBoard;
$('billBtn').onclick = requestBill;
$('saveNoteBtn').onclick = saveBoardNote;
$('cancelPaymentBtn').onclick = closePaymentSheet;
$('confirmBillBtn').onclick = confirmBill;
$('paidToggle').onclick = () => {
  paidNow = !paidNow;
  renderPaymentMethods();
  refreshPaymentPreview();
};
$('paymentAmount').addEventListener('input', refreshPaymentPreview);
$('productSearch').addEventListener('input', renderProducts);
$('staffSelect').addEventListener('change', () => localStorage.setItem('bl_waiter_staff', $('staffSelect').value));
$('tableInput').addEventListener('keydown', (ev) => { if (ev.key === 'Enter') openBoard(); });
$('boardNoteInput').addEventListener('keydown', (ev) => { if (ev.key === 'Enter' && (ev.ctrlKey || ev.metaKey)) saveBoardNote(); });
$('productSearch').addEventListener('keydown', (ev) => {
  if (ev.key !== 'Enter') return;
  if (!state) return;
  const query = $('productSearch').value.trim();
  const visibleProducts = filteredProducts();
  const found = state.products.find(p => p.code === query.padStart(6, '0') || p.code === query) || (visibleProducts.length === 1 ? visibleProducts[0] : null);
  if (found) addProduct(found.code);
});
qsa('[data-view]').forEach((button) => {
  button.addEventListener('click', () => setView(button.dataset.view));
});
$('paymentSheet').addEventListener('click', (ev) => {
  if (ev.target === $('paymentSheet')) closePaymentSheet();
});
document.addEventListener('visibilitychange', () => {
  if (!document.hidden) load({ silent:true });
});
window.addEventListener('focus', () => load({ silent:true }));

load();
setView(activeView);
setInterval(() => {
  if (!document.hidden) load({ silent:true });
}, 1500);
""";
}
