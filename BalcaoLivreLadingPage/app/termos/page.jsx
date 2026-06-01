import SiteHeader from "../SiteHeader";

export const metadata = {
  title: "Termos, condicoes e privacidade | Balcao Livre PDV",
  description:
    "Termos de uso, politica de privacidade, regras de app stores, WhatsApp Cloud, dados e suporte do Balcao Livre PDV.",
  alternates: {
    canonical: "/termos/"
  }
};

export default function TermsPage() {
  return (
    <main className="lpPage legalPage">
      <SiteHeader />

      <section className="infoPage legalTermsPage">
        <div className="infoHero">
          <p className="eyebrow">Documento publico de conformidade</p>
          <h1>Termos, condicoes de uso e politica de privacidade.</h1>
          <p>
            Ultima atualizacao: <strong>27 de maio de 2026</strong>. Este documento cobre o uso do Balcao Livre PDV por clientes, app stores, Meta/WhatsApp Cloud API, provedores de nuvem, integracoes e autoridades publicas.
          </p>
          <div className="legalNotice">
            Documento operacional para uso comercial e revisao de plataformas. Em contratos, licitacoes, setores regulados ou exigencias especificas, a proposta assinada, a nota fiscal, o contrato principal e a legislacao aplicavel prevalecem.
          </div>
        </div>

        <div className="infoLayout">
          <aside className="infoAside" aria-label="Indice dos termos">
            <a href="#identificacao">Identificacao</a>
            <a href="#aceite">Aceite e escopo</a>
            <a href="#planos">Planos e pagamento</a>
            <a href="#fiscal">Uso fiscal</a>
            <a href="#integracoes">Integracoes</a>
            <a href="#privacidade">Privacidade</a>
            <a href="#lgpd">LGPD</a>
            <a href="#appstores">Google Play e iOS</a>
            <a href="#whatsapp">Meta/WhatsApp</a>
            <a href="#seguranca">Seguranca</a>
            <a href="#suporte">Suporte</a>
            <a href="#cancelamento">Cancelamento</a>
            <a href="#governo">Governo e auditoria</a>
            <a href="#referencias">Referencias</a>
          </aside>

          <div className="infoContent">
            <section className="infoBlock" id="identificacao">
              <h2>1. Identificacao, contatos e finalidade</h2>
              <p>
                O Balcao Livre PDV e um software comercial de ponto de venda para restaurantes, bares, lanchonetes, delivery, eventos e operacoes semelhantes, oferecido pela marca Balcao Livre PDV / Nagazaki Software. Os dados fiscais completos do fornecedor, quando exigidos, devem constar na proposta, contrato, recibo, nota fiscal ou instrumento comercial aplicavel.
              </p>
              <p>
                Contatos comerciais, suporte inicial e solicitacoes sobre privacidade podem ser enviados pelos canais oficiais:
              </p>
              <ul className="legalContactList">
                <li>Vendedor Wender: <a href="https://wa.me/5527981267551?text=Ola%2C%20quero%20conhecer%20o%20Balcao%20Livre%20PDV.%20Pode%20me%20ajudar%20com%20planos%2C%20valores%20e%20personalizacao%3F">(27) 98126-7551</a></li>
                <li>Vendedor Lucas: <a href="https://wa.me/5533999609457?text=Ola%2C%20quero%20conhecer%20o%20Balcao%20Livre%20PDV.%20Pode%20me%20ajudar%20com%20planos%2C%20valores%20e%20personalizacao%3F">(33) 99960-9457</a></li>
              </ul>
              <p>
                Caso seja publicado um e-mail, portal de suporte, DPO/encarregado ou canal contratual especifico, esse canal tambem podera ser utilizado para demandas formais.
              </p>
            </section>

            <section className="infoBlock" id="aceite">
              <h2>2. Aceite, escopo e capacidade de contratacao</h2>
              <p>
                Ao instalar, acessar, testar, ativar, renovar, pagar ou continuar usando o Balcao Livre PDV, o cliente declara que leu e aceitou estes Termos. Quem contrata em nome de uma empresa, orgao, franquia, filial ou terceiro declara possuir autorizacao para assumir obrigacoes em nome dessa organizacao.
              </p>
              <p>
                Estes Termos cobrem o PDV Windows, PDV web, painel administrativo, recursos online, aplicativo mobile, paginas publicas, suporte, treinamento, atualizacoes, sincronizacao, impressao, delivery, comandas, mesas, estoque, relatorios e integracoes relacionadas ao Balcao Livre PDV.
              </p>
              <ul>
                <li>O software e licenciado, nao vendido. A propriedade intelectual continua pertencendo ao fornecedor ou a seus licenciantes.</li>
                <li>O cliente nao pode copiar, revender, sublicenciar, desmontar, burlar ativacao, compartilhar chave indevidamente ou usar o sistema para fins ilegais.</li>
                <li>Recursos demonstrativos, prints, textos de marketing e telas podem evoluir sem que isso gere obrigacao de manter layout ou funcao identica em todas as versoes.</li>
              </ul>
            </section>

            <section className="infoBlock" id="planos">
              <h2>3. Planos, licenca, pagamento e renovacao</h2>
              <p>
                A liberacao do uso depende do plano contratado, da confirmacao de pagamento e da ativacao tecnica quando aplicavel. Precos, promocoes e condicoes exibidos no site podem mudar; o valor valido e o que estiver confirmado na proposta, checkout, contrato, recibo ou conversa comercial documentada.
              </p>
              <ul>
                <li><strong>Balcao Livre PDV Offline:</strong> foco em operacao local no Windows, com ativacao por chave, instalador Windows, PDV offline e atualizacoes inclusas no periodo contratado. Referencia comercial atual: R$ 17,00 mensal ou R$ 200,00 anual.</li>
                <li><strong>Balcao Livre PDV Online:</strong> recursos conectados, uso web, WhatsApp, iFood e/ou integracoes de delivery, zonas de entrega, garcom no celular em tempo real, sincronizacao e rotinas em nuvem conforme contratacao. Mensalidade e consultiva pelo WhatsApp; anual abaixo de R$ 999,00, conforme proposta vigente.</li>
                <li>Taxas de app stores, gateway de pagamento, maquininha, iFood, Meta, provedor cloud, hospedagem especial, dominio, certificado, integracao personalizada ou implantacao fora do padrao podem ser cobradas separadamente.</li>
              </ul>
              <p>
                Atraso, chargeback, fraude, abuso, uso indevido ou violacao destes Termos pode suspender acesso, atualizacoes, suporte, renovacao automatica ou recursos online.
              </p>
            </section>

            <section className="infoBlock" id="fiscal">
              <h2>4. Uso operacional, responsabilidade fiscal e documentos</h2>
              <p>
                O Balcao Livre PDV organiza vendas, comandas, mesas, pagamentos, estoque, impressao e comprovantes operacionais. Quando a tela, recibo ou comprovante indicar "nao e documento fiscal", esse comprovante nao substitui NFC-e, NF-e, SAT, MFE, cupom fiscal, nota de servico, recibo fiscal ou qualquer documento legalmente exigido.
              </p>
              <ul>
                <li>O cliente e responsavel por cadastro correto de produtos, precos, taxas, descontos, operadores, estoque, formas de pagamento e fechamento de caixa.</li>
                <li>O cliente e responsavel pela emissao fiscal, guarda de documentos, apuracao de impostos, defesa do consumidor, regras trabalhistas, alvaras e exigencias locais.</li>
                <li>Relatorios do sistema sao apoio operacional. Eles nao substituem contabilidade, auditoria independente, certificacao fiscal ou conciliacao bancaria oficial.</li>
                <li>Integracoes fiscais, se contratadas, dependem de certificado, credenciais, configuracao, disponibilidade de terceiros e regras do estado/municipio.</li>
              </ul>
            </section>

            <section className="infoBlock" id="integracoes">
              <h2>5. Recursos online, nuvem, iFood, delivery, web e terceiros</h2>
              <p>
                O plano online pode depender de servicos de terceiros, como hospedagem, banco de dados, armazenamento, CDN, app stores, gateways de pagamento, Meta/WhatsApp Cloud API, iFood, APIs de delivery, mapas, impressao, sistemas operacionais, navegadores e provedores de internet.
              </p>
              <ul>
                <li>O cliente deve manter credenciais, tokens, contas de app store, contas Meta, contas iFood, dominios, certificados e acessos sob sua responsabilidade.</li>
                <li>Mudancas em APIs, politicas, limites, precos, bloqueios, indisponibilidades ou revisoes de terceiros podem afetar recursos conectados.</li>
                <li>Quando uma integracao exigir aprovacao de plataforma, verificacao empresarial, template, webhook, permissao, conta comercial ou compliance adicional, o cliente deve fornecer as informacoes solicitadas.</li>
                <li>Dados enviados a terceiros seguem tambem os termos, politicas e configuracoes desses terceiros.</li>
              </ul>
            </section>

            <section className="infoBlock" id="privacidade">
              <h2>6. Politica de privacidade e dados tratados</h2>
              <p>
                O Balcao Livre PDV e um produto B2B. Ele nao e destinado a criancas e nao deve ser usado para cadastrar dados de menores, dados sensiveis ou dados desnecessarios sem base legal. O cliente decide quais dados inserir no sistema e deve orientar seus funcionarios, operadores, entregadores, garcons e clientes finais quando necessario.
              </p>
              <p>Conforme o uso contratado, podem ser tratados:</p>
              <ul>
                <li>Dados de conta e contato: nome, telefone, e-mail, empresa, CNPJ/CPF quando informado, endereco comercial, cidade, estado, login, perfil de acesso e historico de suporte.</li>
                <li>Dados operacionais: produtos, categorias, precos, estoque, comandas, mesas, vendas, formas de pagamento, troco, descontos, caixa, operadores, relatorios e registros de impressao.</li>
                <li>Dados de clientes finais e delivery: nome, telefone, endereco, complemento, ponto de referencia, pedido, observacoes, status de entrega e comunicacoes associadas.</li>
                <li>Dados tecnicos: dispositivo, sistema operacional, versao do app, identificador de instalacao/licenca, logs, IP, horarios de acesso, falhas, eventos de seguranca e metricas de uso.</li>
                <li>Dados de pagamentos: metodo, valor, status, comprovante, identificador de transacao e conciliacao. O sistema nao deve armazenar numero completo de cartao, CVV ou credenciais bancarias sensiveis; quando pagamento online for usado, esses dados sao processados pelo provedor de pagamento.</li>
                <li>Dados de mensagens e integracoes: telefone, conteudo operacional necessario, status de envio, templates, opt-in/opt-out, identificadores de conversa, pedido ou canal.</li>
              </ul>
              <p>
                As finalidades incluem executar o contrato, ativar licencas, operar o PDV, sincronizar dados, prestar suporte, melhorar estabilidade, prevenir fraude, cumprir obrigacoes legais, proteger direitos, atender solicitacoes do cliente e manter registros de seguranca.
              </p>
            </section>

            <section className="infoBlock" id="lgpd">
              <h2>7. LGPD: papeis, bases legais, direitos e exclusao</h2>
              <p>
                Para dados de consumidores finais, funcionarios, entregadores, garcons e operadores cadastrados pelo cliente, o cliente normalmente atua como controlador dos dados. O Balcao Livre PDV pode atuar como operador quando hospeda, sincroniza, processa, presta suporte ou executa instrucoes do cliente. Para dados de cobranca, contrato, suporte, marketing proprio, seguranca e defesa de direitos, o fornecedor pode atuar como controlador.
              </p>
              <ul>
                <li>Bases legais possiveis: execucao de contrato, cumprimento de obrigacao legal/regulatoria, legitimo interesse, exercicio regular de direitos, protecao do credito, prevencao a fraude e consentimento quando exigido.</li>
                <li>Compartilhamento: pode ocorrer com provedores de nuvem, suporte, pagamentos, app stores, Meta/WhatsApp, iFood, delivery, contabilidade, autoridades publicas, auditores, parceiros autorizados e fornecedores essenciais.</li>
                <li>Retencao: dados sao mantidos enquanto necessarios para contrato, operacao, suporte, seguranca, auditoria, obrigacao legal, backup, prevencao de fraude ou defesa de direitos. Depois disso, podem ser excluidos, anonimizados ou bloqueados conforme a lei.</li>
                <li>Direitos do titular: confirmacao de tratamento, acesso, correcao, anonimizacao, bloqueio, eliminacao, portabilidade, informacao sobre compartilhamento, revisao de decisoes automatizadas quando houver, oposicao e revogacao de consentimento nos limites legais.</li>
              </ul>
              <p>
                Para solicitar acesso, correcao, exportacao ou exclusao de conta/dados, envie mensagem aos contatos oficiais informando nome, empresa, CNPJ/licenca quando houver, telefone, e-mail de login e pedido desejado. A exclusao pode nao apagar imediatamente dados que precisem ser mantidos por obrigacao legal, fiscal, contabil, seguranca, backup, antifraude ou defesa de direitos.
              </p>
            </section>

            <section className="infoBlock" id="appstores">
              <h2>8. Google Play, Android, iOS/App Store e permissoes mobile</h2>
              <p>
                Esta pagina tambem serve como politica publica para listagens em Google Play, App Store/iOS, revisao de apps, revisao corporativa e analise de privacidade. As declaracoes de seguranca de dados no Google Play e de App Privacy na Apple devem refletir a versao real do aplicativo publicado, os SDKs usados, as permissoes ativas e os recursos contratados.
              </p>
              <ul>
                <li>Permissoes de internet e rede sao usadas para login, sincronizacao, suporte, pedidos, mensagens e recursos online.</li>
                <li>Camera pode ser usada para QR Code, codigo de barras, comprovante ou leitura operacional, somente quando o recurso existir e for acionado.</li>
                <li>Arquivos, armazenamento, fotos ou documentos podem ser usados para importar/exportar dados, salvar comprovantes, anexos, backups ou configuracoes, conforme permissao do sistema.</li>
                <li>Notificacoes podem avisar pedidos, caixa, status de venda, suporte ou alertas operacionais.</li>
                <li>Localizacao, quando existir no app, deve se limitar a recursos como entrega, zonas, endereco, distancia, rota ou operacao logistica, conforme configuracao e permissao do usuario.</li>
                <li>O app nao deve vender dados pessoais. Rastreamento para publicidade de terceiros, se algum dia existir, deve ser declarado separadamente e depender das permissoes exigidas pela plataforma.</li>
              </ul>
              <p>
                Se uma versao mobile passar a coletar novas categorias de dados, usar novo SDK, mudar finalidade, ativar analytics, pagamentos, publicidade, localizacao ou mensageria, esta pagina e os formularios das lojas devem ser atualizados antes ou junto da publicacao.
              </p>
            </section>

            <section className="infoBlock" id="whatsapp">
              <h2>9. Meta, WhatsApp Cloud API e comunicacoes</h2>
              <p>
                Recursos de WhatsApp, quando usados, dependem de conta comercial, politicas da Meta/WhatsApp, numeros autorizados, templates aprovados, consentimento do destinatario quando exigido e uso responsavel do canal.
              </p>
              <ul>
                <li>O cliente e responsavel por obter base legal e opt-in valido antes de enviar mensagens a consumidores, inclusive confirmacoes de pedido, entrega, cobranca, promocao ou atendimento.</li>
                <li>O cliente deve respeitar opt-out, bloqueio, solicitacao de parada, limites de envio, templates, janelas de atendimento, categorias de mensagem e regras comerciais da plataforma.</li>
                <li>Contatos nao devem ser importados, comprados, raspados ou usados sem autorizacao. Mensagens abusivas, spam, conteudo ilegal, discriminatorio, enganoso ou proibido podem causar suspensao.</li>
                <li>WhatsApp, Meta, iFood e outros terceiros podem revisar, limitar, reprovar, bloquear ou encerrar contas, templates, numeros e integracoes conforme suas proprias politicas.</li>
              </ul>
            </section>

            <section className="infoBlock" id="seguranca">
              <h2>10. Seguranca, backups, disponibilidade e incidentes</h2>
              <p>
                O Balcao Livre PDV adota medidas proporcionais ao porte e ao recurso contratado, como controles de acesso, separacao de perfis, registros tecnicos, atualizacoes, protecao de credenciais, conexoes seguras quando aplicavel e backups em rotinas online. Nenhum sistema e imune a falhas, ataques, erros de configuracao, indisponibilidade de internet ou problemas de terceiros.
              </p>
              <ul>
                <li>No plano Offline, o cliente deve proteger o computador, Windows, senha, antivirus, energia, impressora, rede local e copias de seguranca.</li>
                <li>No plano Online, disponibilidade depende tambem de internet, nuvem, banco de dados, app stores, provedores externos e manutencoes programadas ou emergenciais.</li>
                <li>Credenciais, tokens, senhas, chaves de ativacao e acessos administrativos devem ser mantidos em sigilo pelo cliente.</li>
                <li>Incidentes relevantes serao tratados conforme a lei aplicavel, impacto, evidencias disponiveis e canais de contato informados pelo cliente.</li>
              </ul>
            </section>

            <section className="infoBlock" id="suporte">
              <h2>11. Suporte, implantacao, treinamento e atualizacoes</h2>
              <p>
                O suporte cobre orientacao de instalacao, ativacao, uso das telas principais, configuracao basica, verificacao de falhas, orientacao de impressao e duvidas operacionais dentro do plano contratado. Suporte remoto pode exigir acesso temporario ao computador, navegador, app ou painel, sempre com autorizacao do cliente.
              </p>
              <ul>
                <li>Implantacao, migracao de dados, integracao personalizada, automacao fiscal, treinamento presencial, customizacao e atendimento fora do escopo podem ter custo separado.</li>
                <li>Atualizacoes podem corrigir falhas, melhorar seguranca, alterar telas, remover recursos obsoletos, adaptar politicas de terceiros ou preparar novas funcoes.</li>
                <li>O cliente deve manter versoes minimamente atuais. Versoes antigas podem perder suporte por seguranca, compatibilidade ou mudancas de plataforma.</li>
              </ul>
            </section>

            <section className="infoBlock" id="cancelamento">
              <h2>12. Cancelamento, reembolso, encerramento e portabilidade</h2>
              <p>
                O cancelamento encerra renovacoes futuras e pode bloquear recursos pagos ao fim do periodo contratado ou apos prazo de cortesia, se houver. Valores ja pagos, instalacao, ativacao, integracao, taxas de terceiros e periodos promocionais podem nao ser reembolsaveis, salvo previsao legal, contratual ou decisao do meio de pagamento.
              </p>
              <ul>
                <li>Antes do encerramento, o cliente deve exportar relatorios e dados que desejar manter, quando o recurso estiver disponivel.</li>
                <li>Apos encerramento, dados online podem ser mantidos por prazo operacional, legal, backup, antifraude, fiscal, contabil ou defesa de direitos, e depois excluidos ou anonimizados.</li>
                <li>Se a contratacao ocorrer por app store, marketplace ou gateway, cancelamentos e reembolsos tambem podem seguir as regras dessa plataforma.</li>
              </ul>
            </section>

            <section className="infoBlock" id="governo">
              <h2>13. Uso governamental, auditoria, requisicoes legais e ordem publica</h2>
              <p>
                Orgaos publicos, entidades governamentais, contratacoes administrativas, licitacoes, convenios ou operacoes reguladas podem exigir contrato especifico, documentacao adicional, DPA, nivel de servico, residencia de dados, matriz de responsabilidade, analise de seguranca ou requisitos proprios. Esses requisitos somente vinculam o fornecedor quando aceitos por escrito.
              </p>
              <ul>
                <li>O fornecedor pode cooperar com requisicoes legais validas, ordens judiciais, autoridades competentes, fiscalizacoes e medidas de seguranca, respeitando a legislacao aplicavel.</li>
                <li>O cliente e responsavel por responder a auditorias fiscais, sanitarias, trabalhistas, consumidor, licitatorias e contabeis relacionadas ao seu negocio.</li>
                <li>Registros do sistema podem apoiar verificacoes, mas nao garantem certificacao oficial, trilha imutavel, assinatura digital, guarda fiscal ou cadeia de custodia sem modulo/contrato especifico.</li>
                <li>Uso politico, governamental ou de interesse publico em canais de mensageria tambem deve observar as regras da Meta/WhatsApp e da legislacao eleitoral, administrativa e de protecao de dados.</li>
              </ul>
            </section>

            <section className="infoBlock" id="propriedade">
              <h2>14. Propriedade intelectual, conteudo e limites de responsabilidade</h2>
              <p>
                Marcas, codigo, layout, textos, banco de dados estrutural, instaladores, scripts, documentacao, telas, imagens, modelos de relatorio e materiais do Balcao Livre PDV pertencem ao fornecedor ou a seus licenciantes. O cliente mantem seus proprios dados comerciais e operacionais.
              </p>
              <ul>
                <li>O cliente nao deve inserir conteudo ilegal, ofensivo, discriminatorio, fraudulento, sensivel sem base legal ou que viole direitos de terceiros.</li>
                <li>O fornecedor nao responde por perda causada por uso incorreto, falta de backup local, queda de energia, falha de internet, impressora, equipamento, sistema operacional, terceiro, credencial vazada ou dado cadastrado incorretamente pelo cliente.</li>
                <li>A responsabilidade total, quando houver, sera limitada ao valor efetivamente pago pelo cliente pelo periodo afetado, exceto quando a lei exigir regra diferente.</li>
              </ul>
            </section>

            <section className="infoBlock" id="alteracoes">
              <h2>15. Alteracoes dos termos, legislacao e foro</h2>
              <p>
                Estes Termos podem ser atualizados para refletir novas leis, recursos, precos, politicas de app stores, Meta/WhatsApp, iFood, nuvem, pagamentos, seguranca ou operacao. A data de atualizacao sera indicada nesta pagina. O uso continuado apos a publicacao caracteriza aceitacao da versao vigente, sem prejuizo de contratos assinados.
              </p>
              <p>
                Aplicam-se as leis brasileiras, incluindo normas de protecao de dados, internet, consumidor, contratos, propriedade intelectual e regras fiscais conforme o caso. Quando houver contrato principal, o foro nele definido prevalece; na falta dele, sera aplicado o foro competente conforme a legislacao brasileira.
              </p>
            </section>

            <section className="infoBlock" id="referencias">
              <h2>16. Referencias oficiais consideradas</h2>
              <p>
                Este texto foi estruturado para alinhar a pagina publica do produto com referencias oficiais consultadas em 27 de maio de 2026. As politicas abaixo podem mudar e devem ser revisitadas antes de publicacoes em loja, novas integracoes ou contratos regulados.
              </p>
              <ul className="referenceLinks">
                <li><a href="https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm" target="_blank" rel="noopener noreferrer">Lei Geral de Protecao de Dados Pessoais - LGPD</a></li>
                <li><a href="https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2014/lei/l12965.htm" target="_blank" rel="noopener noreferrer">Marco Civil da Internet</a></li>
                <li><a href="https://www.planalto.gov.br/ccivil_03/leis/l8078compilado.htm" target="_blank" rel="noopener noreferrer">Codigo de Defesa do Consumidor</a></li>
                <li><a href="https://www.gov.br/anpd/pt-br" target="_blank" rel="noopener noreferrer">Autoridade Nacional de Protecao de Dados - ANPD</a></li>
                <li><a href="https://support.google.com/googleplay/android-developer/answer/10144311" target="_blank" rel="noopener noreferrer">Google Play - User Data policy</a></li>
                <li><a href="https://support.google.com/googleplay/android-developer/answer/10787469" target="_blank" rel="noopener noreferrer">Google Play - Data safety section</a></li>
                <li><a href="https://developer.apple.com/app-store/review/guidelines/" target="_blank" rel="noopener noreferrer">Apple App Store Review Guidelines</a></li>
                <li><a href="https://developer.apple.com/app-store/user-privacy-and-data-use/" target="_blank" rel="noopener noreferrer">Apple - User Privacy and Data Use</a></li>
                <li><a href="https://www.whatsapp.com/legal/business-terms/" target="_blank" rel="noopener noreferrer">WhatsApp Business Terms</a></li>
                <li><a href="https://www.whatsapp.com/legal/business-policy/" target="_blank" rel="noopener noreferrer">WhatsApp Business Messaging Policy</a></li>
                <li><a href="https://developers.facebook.com/terms/" target="_blank" rel="noopener noreferrer">Meta Platform Terms</a></li>
              </ul>
            </section>
          </div>
        </div>
      </section>
    </main>
  );
}
