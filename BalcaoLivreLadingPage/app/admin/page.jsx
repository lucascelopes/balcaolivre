export const metadata = {
  title: "Admin | Balcao Livre PDV",
  description: "Painel interno do Balcao Livre PDV para licencas, clientes, suporte, downloads e treinamento WhatsApp."
};

const adminMarkup = `<main id="loginView" class="login-shell">
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
          <span>Admin interno</span>
        </div>
      </div>
      <nav class="nav-list">
        <button class="nav active" data-view="dashboard">Dashboard</button>
        <button class="nav" data-view="seo">SEO e vendas</button>
        <button class="nav" data-view="licenses">Licencas</button>
        <button class="nav" data-view="support">Suporte</button>
        <button class="nav" data-view="training">Treinamento WA</button>
        <button class="nav" data-view="devices">Clientes</button>
        <button class="nav" data-view="downloads">Downloads</button>
        <button class="nav" data-view="keys">Criar chave</button>
      </nav>
      <div class="sidebar-footer">
        <span id="sidebarSync">Carregando dados...</span>
        <button id="logoutButton" class="nav danger">Sair</button>
      </div>
    </aside>

    <section class="content">
      <header class="topbar">
        <div>
          <h1 id="viewTitle">Dashboard</h1>
          <p id="viewSubtitle">Operacao, vendas, clientes e licencas em tempo real.</p>
        </div>
        <div class="top-actions">
          <span id="realtimeMode" class="status-pill pending">Tempo real iniciando</span>
          <span id="storageMode" class="status-pill pending">Checando Supabase</span>
          <span id="lastSync" class="status-pill pending">Carregando</span>
        </div>
      </header>

      <section id="dashboardView" class="view">
        <section class="executive-grid">
          <article class="hero-stat metric-card">
            <span class="metric-label">Receita Stripe</span>
            <strong id="mStripeRevenue">R$ 0,00</strong>
            <small><b id="mStripePurchases">0 / 0</b> compras 24h / total</small>
          </article>
          <article class="summary-card metric-card">
            <span class="metric-label">Licencas ativas</span>
            <strong id="mActive">0</strong>
            <small><b id="mAvailable">0</b> disponiveis</small>
          </article>
          <article class="summary-card metric-card">
            <span class="metric-label">Clientes 24h</span>
            <strong id="mOnline">0</strong>
            <small><b id="mDevices">0</b> apps instalados</small>
          </article>
          <article class="summary-card metric-card">
            <span class="metric-label">Suporte aberto</span>
            <strong id="mSupport">0</strong>
            <small><b id="mUsers">0</b> usuarios do app</small>
          </article>
        </section>

        <section class="funnel-strip">
          <div>
            <span>Entraram no site</span>
            <strong id="mSiteVisitors">0 / 0</strong>
            <small>24h / total</small>
          </div>
          <div>
            <span>Paginas vistas</span>
            <strong id="mSiteViews">0 / 0</strong>
            <small>24h / total</small>
          </div>
          <div>
            <span>Checkout iniciado</span>
            <strong id="mCheckoutStarted">0 / 0</strong>
            <small>24h / total</small>
          </div>
          <div>
            <span>Conversao site</span>
            <strong id="mConversion">0%</strong>
            <small>compra / visitante</small>
          </div>
        </section>

        <section class="dashboard-grid">
          <article class="panel">
            <div class="panel-head compact">
              <h2>Compras Stripe</h2>
              <span id="stripeBadge" class="mini-badge">ao vivo</span>
            </div>
            <div id="stripeList" class="list dense"></div>
          </article>
          <article class="panel">
            <div class="panel-head compact">
              <h2>Site e funil</h2>
              <span id="analyticsBadge" class="mini-badge">ao vivo</span>
            </div>
            <div id="analyticsList" class="list dense"></div>
          </article>
          <article class="panel">
            <div class="panel-head compact">
              <h2>Licencas vencendo</h2>
              <span class="mini-badge muted">15 dias</span>
            </div>
            <div id="expiringList" class="list dense"></div>
          </article>
          <article class="panel">
            <div class="panel-head compact">
              <h2>Versoes instaladas</h2>
              <span class="mini-badge muted">clientes</span>
            </div>
            <div id="versionsList" class="list dense"></div>
          </article>
        </section>

        <article class="panel">
          <div class="panel-head compact">
            <h2>Eventos recentes</h2>
            <span id="eventsBadge" class="mini-badge">tempo real</span>
          </div>
          <div id="eventsList" class="event-list"></div>
        </article>
      </section>

      <section id="seoView" class="view hidden">
        <section class="training-hero panel seo-hero">
          <div>
            <span class="section-kicker">SEO + conversao</span>
            <h2>Paginas que precisam trazer teste, WhatsApp e pagamento</h2>
            <p class="panel-copy">Acompanhe paginas automáticas, intenção de busca, visitas e próximos ajustes para vender mais pelo Google.</p>
          </div>
          <div class="training-hero-actions">
            <span id="seoPagesBadge" class="status-pill ok">Carregando paginas</span>
            <button id="copySeoPlanButton" class="secondary" type="button">Copiar roteiro SEO</button>
          </div>
        </section>

        <section id="seoStats" class="training-stats"></section>

        <section class="dashboard-grid seo-dashboard">
          <article class="panel">
            <div class="panel-head compact">
              <div>
                <h2>Paginas com mais movimento</h2>
                <p class="panel-copy">Use para reforçar CTA, plano e WhatsApp nas paginas que ja recebem visita.</p>
              </div>
            </div>
            <div id="seoTopPages" class="list dense"></div>
          </article>
          <article class="panel">
            <div class="panel-head compact">
              <div>
                <h2>Oportunidades automaticas</h2>
                <p class="panel-copy">Pontos que o robô de SEO deve priorizar no proximo ajuste.</p>
              </div>
            </div>
            <div id="seoOpportunityList" class="list dense"></div>
          </article>
        </section>

        <article class="panel">
          <div class="panel-head compact">
            <div>
              <h2>Paginas comerciais criadas</h2>
              <p class="panel-copy">Landing pages por intenção de compra para aparecer no Google e converter direto.</p>
            </div>
          </div>
          <div id="seoPageList" class="seo-page-grid"></div>
        </article>
      </section>

      <section id="supportView" class="view hidden">
        <article class="panel">
          <div class="panel-head">
            <div>
              <h2>Suporte por conversa</h2>
              <span id="supportLiveBadge" class="mini-badge">tempo real</span>
            </div>
            <input id="supportSearch" placeholder="Buscar por cliente, telefone, chave ou mensagem">
          </div>
          <div id="supportList" class="support-list"></div>
        </article>
      </section>

      <section id="trainingView" class="view hidden">
        <section class="training-hero panel">
          <div>
            <span class="section-kicker">Evolution API + machine learning operacional</span>
            <h2>Central de treinamento do WhatsApp</h2>
            <p class="panel-copy">Base pronta, respostas por setor e classificador de intenção para responder diferente em restaurante, barbearia, beleza, mecânica, clínica, petshop e Agenda Livre.</p>
          </div>
          <div class="training-hero-actions">
            <span id="trainingEngineBadge" class="status-pill ok">Evolution pronta</span>
            <span id="trainingCoverageBadge" class="status-pill pending">Carregando regras</span>
            <button id="seedTrainingButton" class="secondary" type="button">Instalar base pronta</button>
          </div>
        </section>

        <section id="trainingStats" class="training-stats"></section>

        <section class="training-dashboard">
          <article class="panel training-card">
            <div class="panel-head compact">
              <div>
                <h2>Mapa do aprendizado</h2>
                <p class="panel-copy">Mostra onde o bot já tem regra treinada e onde ainda falta ensinar.</p>
              </div>
            </div>
            <div id="trainingCoverage" class="training-coverage"></div>
          </article>

          <article class="panel training-card">
            <div class="panel-head compact">
              <div>
                <h2>Distribuição das regras</h2>
                <p class="panel-copy">Volume por tipo de intenção usada pelo atendimento.</p>
              </div>
            </div>
            <div id="trainingChart" class="training-chart"></div>
          </article>

          <article class="panel training-card">
            <div class="panel-head compact">
              <div>
                <h2>Setores treinados</h2>
                <p class="panel-copy">Restaurante, Agenda Livre e serviços com base separada.</p>
              </div>
            </div>
            <div id="trainingSectorChart" class="training-chart"></div>
          </article>
        </section>

        <section class="training-layout">
          <article class="panel training-editor">
            <div class="panel-head compact">
              <div>
                <h2>Novo treinamento</h2>
                <p class="panel-copy">Ensine o que o cliente escreve e o que o atendimento deve fazer.</p>
              </div>
              <span id="trainingLiveBadge" class="mini-badge">global</span>
            </div>
            <div class="training-form">
              <label>Setor</label>
              <select id="trainingSegment">
                <option value="GLOBAL">Todos os setores</option>
                <option value="RESTAURANTE">Restaurante / delivery</option>
                <option value="BARBEARIA">Barbearia</option>
                <option value="BELEZA">Salão, unha e beleza</option>
                <option value="MECANICA">Mecânica / oficina</option>
                <option value="CLINICA">Clínica / médico</option>
                <option value="PETSHOP">Petshop / veterinário</option>
                <option value="AGENDA">Agenda genérica</option>
              </select>
              <label>Mensagem que o cliente manda</label>
              <textarea id="trainingPhrase" rows="4" placeholder="Ex: quero cancelar, pix, entrega, combo 2 coca"></textarea>
              <label>Ação que o bot deve executar</label>
              <select id="trainingIntent">
                <option value="RESPOSTA">Resposta pronta</option>
                <option value="PEDIDO">Pedido</option>
                <option value="CARDAPIO">Cardápio</option>
                <option value="HORARIO">Horário</option>
                <option value="STATUS">Status</option>
                <option value="ATENDENTE">Atendente</option>
                <option value="AGENDAMENTO">Agendamento</option>
                <option value="REMARCAR">Remarcar</option>
                <option value="CANCELAR">Cancelar</option>
              </select>
              <label>Resposta opcional</label>
              <textarea id="trainingReply" rows="5" placeholder="Se preencher, o bot usa esta resposta. Pode usar {{loja}}, {{setor}}, {{saudacao}} e {{cardapio}}."></textarea>
              <button id="saveTrainingButton" type="button">Salvar e treinar agora</button>
            </div>
          </article>

          <article class="panel">
            <div class="panel-head compact">
              <div>
                <h2>Regras treinadas</h2>
                <p class="panel-copy">O robô online consulta essa lista para responder mesmo com o PDV fechado.</p>
              </div>
            </div>
            <div id="trainingList" class="training-list"></div>
          </article>
        </section>

        <article class="panel training-timeline-panel">
          <div class="panel-head compact">
            <div>
              <h2>Últimos aprendizados aplicados</h2>
              <p class="panel-copy">Histórico rápido do que foi treinado e já está disponível para a Evolution API.</p>
            </div>
          </div>
          <div id="trainingTimeline" class="training-timeline"></div>
        </article>
      </section>

      <section id="licensesView" class="view hidden">
        <article class="panel">
          <div class="panel-head">
            <div>
              <h2>Licencas</h2>
              <span id="licenseLiveBadge" class="mini-badge">tempo real</span>
            </div>
            <input id="licenseSearch" placeholder="Buscar por cliente, chave, CNPJ, cidade, IP ou PC">
          </div>
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Status</th>
                  <th>Cliente / local</th>
                  <th>Chave</th>
                  <th>Produto / PC</th>
                  <th>Rede</th>
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
          <div class="panel-head compact">
            <h2>Clientes</h2>
            <span id="devicesLiveBadge" class="mini-badge">tempo real</span>
          </div>
          <div id="devicesList" class="device-grid"></div>
        </article>
        <article class="panel">
          <div class="panel-head compact">
            <h2>IPs bloqueados</h2>
            <span id="blockedIpLiveBadge" class="mini-badge">0 bloqueado(s)</span>
          </div>
          <div id="blockedIpList" class="blocked-ip-list"></div>
        </article>
      </section>

      <section id="downloadsView" class="view hidden">
        <article class="panel release-panel">
          <div>
            <span class="release-eyebrow">Release atual</span>
            <h2 id="releaseTitle">Balcao Livre PDV Online</h2>
            <p id="releaseSubtitle">Links oficiais para teste, compra e atualizacao dos clientes instalados.</p>
          </div>
          <div class="release-version-box">
            <span>Versao</span>
            <strong id="releaseVersion">-</strong>
            <small id="releasePublishedAt">-</small>
          </div>
        </article>

        <section id="downloadCards" class="download-grid"></section>

        <article class="panel manifest-panel">
          <div>
            <h2>Atualizacao dos clientes ativos</h2>
            <p>O app instalado consulta este manifesto. Depois que o instalador e o version.json forem publicados, quem ja tem o PDV recebe a nova versao automaticamente.</p>
          </div>
          <div class="release-url-box">
            <code id="releaseManifestUrl"></code>
            <button class="secondary" type="button" onclick="copyDownloadUrl('manifest')">Copiar manifesto</button>
          </div>
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
  </main>`;

export default function AdminPage() {
  return (
    <>
      <link rel="stylesheet" href="/admin-assets/styles.css?v=20260609-seo-sales" />
      <div dangerouslySetInnerHTML={{ __html: adminMarkup }} />
      <script src="/admin-assets/app.js?v=20260609-seo-sales" />
    </>
  );
}
