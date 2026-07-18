using AgendaLivre.Windows;

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

Console.WriteLine("WhatsApp manual send reliability: 17 checks passed.");

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
