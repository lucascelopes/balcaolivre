import Script from "next/script";

export const metadata = {
  title: "Admin | Balcao Livre PDV",
  description: "Painel Next.js do Balcao Livre PDV para licencas, clientes e suporte."
};

const adminMarkup = `
  <main id="loginView" class="login-shell">
    <section class="login-card">
      <div class="brand-mark">BL</div>
      <h1>Balcao Livre PDV</h1>
      <p>Painel interno de licencas, clientes e uso do programa.</p>
      <label>Login</label>
      <input id="loginUser" autocomplete="username" placeholder="Login admin">
      <label>Senha</label>
      <input id="loginPassword" autocomplete="current-password" type="password" placeholder="Senha admin">
      <button id="loginButton">Entrar</button>
      <div id="loginMessage" class="message"></div>
    </section>
  </main>

  <main id="appView" class="app-shell hidden">
    <aside class="sidebar">
      <div class="logo-row">
        <div class="brand-mark small">BL</div>
        <div>
          <strong>Balcao Livre</strong>
          <span>Admin Next.js</span>
        </div>
      </div>
      <a class="nav link-nav" href="/">Landing page</a>
      <a class="nav link-nav" href="/pdv">PDV Web</a>
      <button class="nav active" data-view="dashboard">Dashboard</button>
      <button class="nav" data-view="licenses">Licencas</button>
      <button class="nav" data-view="support">Suporte</button>
      <button class="nav" data-view="devices">Clientes</button>
      <button class="nav" data-view="keys">Criar chave</button>
      <button id="logoutButton" class="nav danger">Sair</button>
    </aside>

    <section class="content">
      <header class="topbar">
        <div>
          <h1 id="viewTitle">Dashboard</h1>
          <p id="viewSubtitle">Uso do programa, chaves e clientes ativos.</p>
        </div>
        <div class="top-actions">
          <span id="storageMode" class="storage-badge error">Supabase nao configurado</span>
          <button id="refreshButton">Atualizar</button>
        </div>
      </header>

      <section id="dashboardView" class="view">
        <div class="metric-grid">
          <article class="metric"><span>Licencas ativas</span><strong id="mActive">0</strong></article>
          <article class="metric"><span>Disponiveis</span><strong id="mAvailable">0</strong></article>
          <article class="metric"><span>Clientes 24h</span><strong id="mOnline">0</strong></article>
          <article class="metric"><span>Usuarios do app</span><strong id="mUsers">0</strong></article>
          <article class="metric"><span>Apps instalados</span><strong id="mDevices">0</strong></article>
          <article class="metric support-metric"><span>Suporte aberto</span><strong id="mSupport">0</strong></article>
        </div>
        <div class="grid-two">
          <article class="panel">
            <h2>Vencendo em ate 15 dias</h2>
            <div id="expiringList" class="list"></div>
          </article>
          <article class="panel">
            <h2>Versoes instaladas</h2>
            <div id="versionsList" class="list"></div>
          </article>
        </div>
        <article class="panel">
          <h2>Eventos recentes</h2>
          <div id="eventsList" class="event-list"></div>
        </article>
      </section>

      <section id="supportView" class="view hidden">
        <article class="panel">
          <div class="panel-head">
            <div>
              <h2>Suporte por conversa</h2>
              <span id="supportLiveBadge" class="live-badge">consulta economica</span>
            </div>
            <input id="supportSearch" placeholder="Buscar por cliente, telefone, chave ou mensagem">
          </div>
          <div id="supportList" class="support-list"></div>
        </article>
      </section>

      <section id="licensesView" class="view hidden">
        <article class="panel">
          <div class="panel-head">
            <h2>Licencas</h2>
            <input id="licenseSearch" placeholder="Buscar por cliente, chave, CNPJ ou PC">
          </div>
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Status</th>
                  <th>Cliente</th>
                  <th>Chave</th>
                  <th>PC</th>
                  <th>Expira</th>
                  <th>Ultimo uso</th>
                  <th></th>
                </tr>
              </thead>
              <tbody id="licensesTable"></tbody>
            </table>
          </div>
        </article>
      </section>

      <section id="devicesView" class="view hidden">
        <article class="panel">
          <h2>Clientes</h2>
          <div id="devicesList" class="device-grid"></div>
        </article>
      </section>

      <section id="keysView" class="view hidden">
        <article class="panel key-panel">
          <h2>Criar chave de ativacao</h2>
          <div class="form-grid">
            <label>Cliente</label>
            <input id="keyCustomer" placeholder="Nome do cliente/restaurante">
            <label>Plano</label>
            <input id="keyPlan" placeholder="Ex: Mensal, Anual, Teste">
            <label>Periodo</label>
            <div class="inline">
              <input id="keyAmount" type="number" min="1" value="30">
              <select id="keyUnit">
                <option value="minutes">minutos</option>
                <option value="days" selected>dias</option>
                <option value="months">meses</option>
                <option value="years">anos</option>
              </select>
            </div>
            <label>Observacao</label>
            <textarea id="keyNotes" rows="3" placeholder="Opcional"></textarea>
          </div>
          <button id="createKeyButton">Gerar chave</button>
          <div id="createdKey" class="created-key hidden"></div>
        </article>
      </section>
    </section>
  </main>
`;

export default function AdminPage() {
  return (
    <>
      <link rel="stylesheet" href="/admin-assets/styles.css" />
      <div dangerouslySetInnerHTML={{ __html: adminMarkup }} />
      <Script src="/admin-assets/app.js" strategy="afterInteractive" />
    </>
  );
}
