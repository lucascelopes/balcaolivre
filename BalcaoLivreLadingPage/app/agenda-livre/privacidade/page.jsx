import Link from "next/link";
import { ArrowLeft, Database, LockKeyhole, Mail, ShieldCheck } from "lucide-react";
import styles from "./privacidade.module.css";

export const metadata = {
  title: "Política de Privacidade | Agenda Livre",
  description: "Saiba como o Agenda Livre coleta, utiliza, protege e compartilha dados pessoais.",
  alternates: { canonical: "/agenda-livre/privacidade" }
};

const sections = [
  ["resumo", "Resumo"], ["dados", "Dados coletados"], ["finalidades", "Como usamos"],
  ["compartilhamento", "Compartilhamento"], ["seguranca", "Segurança"],
  ["retencao", "Retenção"], ["direitos", "Seus direitos"], ["cookies", "Cookies"], ["contato", "Contato"]
];

export default function PrivacyPage() {
  return <main className={styles.page}>
    <header className={styles.header}>
      <Link className={styles.brand} href="/agenda-livre" aria-label="Voltar para o Agenda Livre">
        <img src="/agenda-livre/agenda-livre-mark.png" alt="" />
        <span><strong>Agenda Livre</strong><small>Sistema de agendamentos</small></span>
      </Link>
      <Link className={styles.back} href="/agenda-livre"><ArrowLeft size={17} /> Voltar ao site</Link>
    </header>

    <section className={styles.hero}>
      <div className={styles.heroIcon}><ShieldCheck size={30} /></div>
      <p className={styles.eyebrow}>Privacidade e proteção de dados</p>
      <h1>Política de Privacidade</h1>
      <p className={styles.lead}>Transparência sobre os dados usados para manter sua agenda funcionando com segurança.</p>
      <p className={styles.updated}>Última atualização: <strong>18 de julho de 2026</strong></p>
    </section>

    <div className={styles.layout}>
      <aside className={styles.aside} aria-label="Índice da política"><span>Nesta página</span>{sections.map(([id,label])=><a key={id} href={`#${id}`}>{label}</a>)}</aside>
      <article className={styles.content}>
        <section id="resumo" className={styles.highlight}><ShieldCheck size={24}/><div><h2>Privacidade de forma simples</h2><p>Usamos somente os dados necessários para criar sua conta, operar agendamentos, sincronizar dispositivos, oferecer integrações e prestar suporte. Não vendemos seus dados pessoais.</p></div></section>
        <section><h2>1. Sobre esta política</h2><p>Esta Política explica como o Agenda Livre trata dados pessoais em suas versões Web, Windows e Android, no portal público de agendamento e nos canais de atendimento. Ao utilizar o serviço, você declara estar ciente das práticas descritas aqui.</p><p>Para dados da conta, cobrança, segurança e suporte, o Agenda Livre atua como controlador. Para dados de clientes, profissionais e agendamentos inseridos pelo estabelecimento, o estabelecimento normalmente é o controlador e o Agenda Livre atua como operador, seguindo suas instruções.</p></section>
        <section id="dados"><h2>2. Dados que podemos coletar</h2><ul><li><strong>Conta e estabelecimento:</strong> nome, e-mail, telefone, senha protegida, nome do negócio, segmento, endereço e preferências.</li><li><strong>Operação da agenda:</strong> serviços, profissionais, horários, clientes, observações, agendamentos, status e histórico de atendimento.</li><li><strong>Financeiro:</strong> valores, formas e status de pagamento, despesas e relatórios. Dados completos de cartão são processados pelo provedor de pagamento, não pelo Agenda Livre.</li><li><strong>Integrações:</strong> identificadores e informações necessárias para WhatsApp, Instagram, Mercado Pago e outros serviços ativados.</li><li><strong>Dados técnicos:</strong> endereço IP, dispositivo, navegador, versão do aplicativo, registros de acesso, falhas e eventos de segurança.</li></ul></section>
        <section id="finalidades"><h2>3. Como utilizamos os dados</h2><p>Tratamos os dados para executar o contrato e entregar as funções solicitadas, autenticar usuários, sincronizar a agenda, enviar confirmações e lembretes, processar assinaturas, atender solicitações, prevenir fraude, manter a segurança e cumprir obrigações legais.</p><p>Também podemos usar informações agregadas ou anonimizadas para entender o desempenho do produto e melhorar recursos, sem identificar diretamente uma pessoa.</p></section>
        <section id="compartilhamento"><h2>4. Compartilhamento de dados</h2><p>Dados podem ser compartilhados apenas quando necessário com provedores de hospedagem e banco de dados, autenticação, pagamentos, comunicação, suporte e integrações escolhidas pelo usuário. Esses fornecedores recebem somente o necessário para executar seus serviços e ficam sujeitos às próprias políticas e obrigações legais.</p><p>Também poderemos compartilhar informações para cumprir ordem judicial, requisição de autoridade competente, proteger direitos e prevenir fraude ou incidente de segurança. Não comercializamos listas ou dados pessoais.</p></section>
        <section id="seguranca"><h2>5. Segurança</h2><div className={styles.featureGrid}><div><LockKeyhole size={21}/><strong>Acesso protegido</strong><span>Controles de autenticação e permissões.</span></div><div><Database size={21}/><strong>Dados protegidos</strong><span>Conexões seguras, registros e rotinas de backup.</span></div></div><p>Adotamos medidas técnicas e administrativas proporcionais ao serviço. Nenhum sistema é totalmente imune a riscos; por isso, o usuário também deve manter sua senha em sigilo, proteger os dispositivos e limitar os acessos da equipe.</p></section>
        <section id="retencao"><h2>6. Retenção e exclusão</h2><p>Os dados são mantidos enquanto a conta estiver ativa e pelo período necessário para operação, suporte, segurança, prevenção de fraude, backups, obrigações legais ou defesa de direitos. Após esses prazos, poderão ser excluídos ou anonimizados.</p><p>O encerramento da conta não elimina imediatamente informações que precisem ser conservadas por obrigação legal ou que permaneçam temporariamente em cópias de segurança protegidas.</p></section>
        <section id="direitos"><h2>7. Seus direitos pela LGPD</h2><p>Nos termos da Lei Geral de Proteção de Dados, você pode solicitar confirmação e acesso ao tratamento, correção, informação sobre compartilhamento, portabilidade quando aplicável, anonimização, bloqueio, eliminação, oposição e revogação do consentimento.</p><p>Para proteger sua conta, poderemos solicitar informações adicionais para confirmar sua identidade. Quando a solicitação envolver dados cadastrados por um estabelecimento, ela poderá ser encaminhada ao respectivo responsável.</p></section>
        <section id="cookies"><h2>8. Cookies e armazenamento local</h2><p>O site e a versão Web podem usar cookies e armazenamento local estritamente necessários para manter a sessão, lembrar preferências, proteger o acesso e medir estabilidade. Ferramentas opcionais de análise, quando usadas, deverão respeitar as configurações e bases legais aplicáveis.</p></section>
        <section><h2>9. Crianças e adolescentes</h2><p>O Agenda Livre é destinado à gestão de negócios e não é direcionado a crianças. Estabelecimentos que atendam menores devem avaliar a base legal apropriada e coletar somente os dados realmente necessários, observando as regras de proteção aplicáveis.</p></section>
        <section><h2>10. Alterações desta política</h2><p>Esta Política poderá ser atualizada para refletir mudanças legais, de segurança ou do produto. A versão vigente e a data da última atualização estarão sempre publicadas nesta página.</p></section>
        <section id="contato" className={styles.contact}><Mail size={25}/><div><h2>Fale conosco sobre privacidade</h2><p>Para exercer seus direitos ou tirar dúvidas, entre em contato pelos canais oficiais do Agenda Livre.</p><a href="https://wa.me/5533991314125" target="_blank" rel="noreferrer">Enviar mensagem pelo WhatsApp</a></div></section>
      </article>
    </div>
    <footer className={styles.footer}>© 2026 Agenda Livre. Todos os direitos reservados. <Link href="/agenda-livre">Voltar ao site</Link></footer>
  </main>;
}
