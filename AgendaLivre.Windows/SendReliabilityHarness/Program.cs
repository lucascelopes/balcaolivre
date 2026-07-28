using AgendaLivre.Windows;
using System.Net;

var now = new DateTimeOffset(2026, 7, 17, 15, 0, 0, TimeSpan.Zero);
var guard = new WhatsAppManualSendGuard(TimeSpan.FromSeconds(12));
var ids = new Queue<string>(["attempt-1", "attempt-2", "attempt-3"]);
string NextId() => ids.Dequeue();

var first = guard.Begin("5533998007983", "Oi", now, NextId);
Require(first.Accepted && first.AttemptId == "attempt-1", "a primeira tentativa deve ser aceita");

var duplicate = guard.Begin("5533998007983", "Oi", now.AddSeconds(4), NextId);
Require(!duplicate.Accepted && duplicate.AttemptId == "attempt-1",
    "uma duplicata curta deve ser bloqueada e apontar para a tentativa original");

var differentText = guard.Begin("5533998007983", "Outro texto", now.AddSeconds(5), NextId);
Require(differentText.Accepted && differentText.AttemptId == "attempt-2",
    "uma mensagem diferente deve ser aceita");

var afterWindow = guard.Begin("5533998007983", "Oi", now.AddSeconds(12), NextId);
Require(afterWindow.Accepted && afterWindow.AttemptId == "attempt-3",
    "a mesma mensagem deve ser aceita depois da janela de proteção");

Require(WhatsAppManualSendPolicy.AllowsLegacyFallback(
        404,
        "{\"ok\":false,\"code\":\"ROUTE_NOT_FOUND\"}"),
    "somente o 404 explícito de rota ausente deve permitir o fallback legado");
Require(!WhatsAppManualSendPolicy.AllowsLegacyFallback(
        404,
        "{\"ok\":false,\"code\":\"INSTANCE_NOT_FOUND\"}"),
    "instância inválida nunca deve contornar a validação pelo gateway legado");
Require(!WhatsAppManualSendPolicy.AllowsLegacyFallback(502, "Bad gateway"),
    "502 nunca deve disparar fallback e um segundo envio");
Require(WhatsAppManualSendPolicy.IsAmbiguousHttpStatus(502),
    "502 deve ser tratado como resultado de entrega incerto");
Require(WhatsAppManualSendPolicy.IsExistingPending(false, "sending"),
    "uma requisição idempotente já em andamento deve continuar pendente");
Require(WhatsAppManualSendPolicy.CanTransitionDeliveryStatus("erro", "entregue"),
    "uma confirmação real de entrega deve recuperar um erro local antigo");
Require(WhatsAppManualSendPolicy.IsTextWithinLimit(new string('a', 8_000)) &&
        !WhatsAppManualSendPolicy.IsTextWithinLimit(new string('a', 8_001)),
    "o app deve rejeitar textos acima do limite sem truncar silenciosamente");
Require(WhatsAppPhoneNormalizer.Normalize("+55 (33) 9131-4120") == "553391314120",
    "um identificador legado do WhatsApp deve ser preservado sem inserir outro nono digito");
Require(WhatsAppPhoneNormalizer.Normalize("(33) 99800-7983") == "5533998007983",
    "um numero local atual deve receber apenas o codigo do Brasil");

const string instance = "bl-test";
var optimistic = Message(
    id: "request-1",
    clientRequestId: "request-1",
    providerMessageId: "",
    status: "pendente",
    instance: instance);
var official = Message(
    id: "provider-1",
    clientRequestId: "",
    providerMessageId: "provider-1",
    status: "enviado",
    instance: instance);
var optimisticSource = Message(
    id: "provider-1",
    clientRequestId: "request-1",
    providerMessageId: "provider-1",
    status: "enviado",
    instance: instance);
var optimisticRows = new List<WhatsAppMessage> { official, optimistic };
var optimisticResult = WhatsAppMessageIdentityReconciler.ConsolidateAuthoritativeExactDuplicates(
    optimisticRows,
    [optimisticSource]);
Require(
    optimisticResult.RemovedCount == 1 &&
    optimisticRows.Count == 1 &&
    ReferenceEquals(optimisticRows[0], optimistic) &&
    optimistic.ProviderMessageId == "provider-1",
    "a mensagem oficial deve reconciliar na bolha otimista sem criar uma segunda bolha");

var historicalLocal = Message(
    id: "local-random-id",
    clientRequestId: "",
    providerMessageId: "provider-historical",
    status: "erro",
    instance: instance);
var historicalOfficial = Message(
    id: "provider-historical",
    clientRequestId: "",
    providerMessageId: "provider-historical",
    status: "erro",
    instance: instance);
var historicalRows = new List<WhatsAppMessage> { historicalLocal, historicalOfficial };
var historicalResult = WhatsAppMessageIdentityReconciler.ConsolidateAuthoritativeExactDuplicates(
    historicalRows,
    [historicalOfficial]);
Require(
    historicalResult.RemovedCount == 1 &&
    historicalRows.Count == 1 &&
    ReferenceEquals(historicalRows[0], historicalOfficial),
    "a duplicata historica deve manter o registro cujo id e o id oficial do provedor");

var firstProvider = Message(
    id: "provider-a",
    clientRequestId: "",
    providerMessageId: "provider-a",
    status: "enviado",
    instance: instance);
var secondProvider = Message(
    id: "provider-b",
    clientRequestId: "",
    providerMessageId: "provider-b",
    status: "enviado",
    instance: instance);
var distinctProviderRows = new List<WhatsAppMessage> { firstProvider, secondProvider };
var distinctResult = WhatsAppMessageIdentityReconciler.ConsolidateAuthoritativeExactDuplicates(
    distinctProviderRows,
    [firstProvider, secondProvider]);
Require(
    distinctResult.RemovedCount == 0 && distinctProviderRows.Count == 2,
    "mensagens de mesmo conteudo com ids de provedor diferentes nunca devem ser consolidadas");

var absentPending = Message(
    id: "request-pending",
    clientRequestId: "request-pending",
    providerMessageId: "",
    status: "pendente",
    instance: instance);
var pendingRows = new List<WhatsAppMessage> { absentPending };
var pendingResult = WhatsAppMessageIdentityReconciler.ConsolidateAuthoritativeExactDuplicates(
    pendingRows,
    []);
Require(
    pendingResult.RemovedCount == 0 &&
    pendingRows.Count == 1 &&
    ReferenceEquals(pendingRows[0], absentPending),
    "uma bolha pendente ausente do snapshot deve ser preservada");

var useComputer = AgendaSyncConflictPolicy.CreateTransition(
    AgendaSyncConflictResolution.UseThisComputer,
    remoteRevision: 41);
Require(useComputer.BaseRevision == 41, "a resolução local deve partir da revisão remota atual");
Require(useComputer.Pending, "a resolução local deve permanecer pendente até o CAS concluir");
Require(!useComputer.ApplyRemote, "a resolução local não deve aplicar o payload remoto");
Require(useComputer.QueueLocal, "a resolução local deve reenfileirar o arquivo mais recente");

var useCloud = AgendaSyncConflictPolicy.CreateTransition(
    AgendaSyncConflictResolution.UseCloud,
    remoteRevision: 57);
Require(useCloud.BaseRevision == 57, "a resolução pela nuvem deve gravar a revisão escolhida");
Require(!useCloud.Pending, "a resolução pela nuvem deve limpar a pendência local");
Require(useCloud.ApplyRemote, "a resolução pela nuvem deve aplicar o payload remoto");
Require(!useCloud.QueueLocal, "a resolução pela nuvem não deve criar um push de retorno");

Require(AgendaSyncRetryPolicy.DelayAfterFailure(1) == TimeSpan.FromSeconds(2),
    "o primeiro retry deve aguardar dois segundos");
Require(AgendaSyncRetryPolicy.DelayAfterFailure(2) == TimeSpan.FromSeconds(4),
    "o backoff deve dobrar após a segunda falha");
Require(AgendaSyncRetryPolicy.DelayAfterFailure(5) == TimeSpan.FromSeconds(30),
    "o backoff deve chegar ao teto de trinta segundos");
Require(AgendaSyncRetryPolicy.DelayAfterFailure(50) == TimeSpan.FromSeconds(30),
    "o backoff deve continuar limitado enquanto o app estiver aberto");
Require(AgendaSyncRetryPolicy.IsRetryable(new HttpRequestException("offline")),
    "falha de rede deve entrar no retry automático");
Require(AgendaSyncRetryPolicy.IsRetryable(new TaskCanceledException("timeout")),
    "timeout do cliente deve entrar no retry automático");
Require(AgendaSyncRetryPolicy.IsRetryable(new TestRetryableException()),
    "falha temporária de autenticação deve entrar no retry automático");
Require(!AgendaSyncRetryPolicy.IsRetryable(new InvalidOperationException("programação")),
    "erro de programação não deve criar loop de retry");

Require(AgendaOfflineSessionPolicy.HasUsableCachedIdentity("user-1", "teste@agenda.local", "refresh"),
    "uma sessão protegida completa deve poder abrir o cache offline");
Require(!AgendaOfflineSessionPolicy.HasUsableCachedIdentity("", "teste@agenda.local", "refresh"),
    "um cache sem identidade de usuário não deve abrir a agenda");
Require(AgendaOfflineSessionPolicy.InvalidatesCachedSession(HttpStatusCode.BadRequest),
    "refresh token rejeitado com 400 deve invalidar o cache");
Require(AgendaOfflineSessionPolicy.InvalidatesCachedSession(HttpStatusCode.Unauthorized),
    "refresh token rejeitado com 401 deve invalidar o cache");
Require(AgendaOfflineSessionPolicy.InvalidatesCachedSession(HttpStatusCode.Forbidden),
    "refresh token rejeitado com 403 deve invalidar o cache");
Require(!AgendaOfflineSessionPolicy.InvalidatesCachedSession(HttpStatusCode.TooManyRequests),
    "limite temporário não deve apagar a sessão offline");
Require(!AgendaOfflineSessionPolicy.InvalidatesCachedSession(HttpStatusCode.InternalServerError),
    "falha temporária do servidor não deve apagar a sessão offline");

var newAccountData = new AgendaData();
AgendaAuthenticatedProfilePolicy.ApplyOnboardingDefaults(
    newAccountData,
    "  nova@agenda.com  ",
    "  Maria Silva  ",
    "  Studio Maria  ");
Require(newAccountData.Settings.AccountEmail == "nova@agenda.com",
    "o e-mail usado no cadastro deve aparecer preenchido no onboarding");
Require(newAccountData.Settings.AccountFullName == "Maria Silva",
    "o nome usado no cadastro deve aparecer preenchido no onboarding");
Require(newAccountData.Settings.BusinessName == "Studio Maria",
    "o estabelecimento usado no cadastro deve aparecer preenchido no onboarding");

var existingAccountData = new AgendaData();
existingAccountData.Settings.AccountFullName = "Nome já salvo";
existingAccountData.Settings.BusinessName = "Negócio já salvo";
AgendaAuthenticatedProfilePolicy.ApplyOnboardingDefaults(
    existingAccountData,
    "conta@agenda.com",
    "Outro nome",
    "Outro negócio");
Require(
    existingAccountData.Settings.AccountEmail == "conta@agenda.com" &&
    existingAccountData.Settings.AccountFullName == "Nome já salvo" &&
    existingAccountData.Settings.BusinessName == "Negócio já salvo",
    "entrar em uma conta existente deve preservar o cadastro já concluído");

Console.WriteLine("Agenda Livre reliability: 44 checks passed.");

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static WhatsAppMessage Message(
    string id,
    string clientRequestId,
    string providerMessageId,
    string status,
    string instance) => new()
{
    Id = id,
    ClientRequestId = clientRequestId,
    ProviderMessageId = providerMessageId,
    Instance = instance,
    Phone = "5533998007983",
    Message = "Oi",
    Direction = "saida",
    Status = status,
    CreatedAt = new DateTime(2026, 7, 17, 15, 0, 0, DateTimeKind.Utc)
};

sealed class TestRetryableException : Exception, IAgendaSyncRetryableException
{
}
