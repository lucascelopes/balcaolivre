import React, { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Pressable,
  SafeAreaView,
  ScrollView,
  StatusBar,
  StyleSheet,
  Text,
  TextInput,
  View
} from "react-native";
import {
  addProductToOrder,
  adjustStock,
  cashMovements,
  closeOrder,
  database,
  getSettings,
  listOrders,
  listProducts,
  openOrder,
  orderItems,
  putProduct,
  saveSettings,
  setCashOpen
} from "./src/data/db";
import { bootstrapAndApply, flushSync } from "./src/services/syncService";
import { activateMobile, backupMobile } from "./src/services/licenseApi";
import { clearSession, defaultAdminApiUrl, loadSession } from "./src/services/session";
import { buildReceiptText, printJob } from "./src/services/printService";
import { pollIFoodOrders } from "./src/services/ifoodService";
import { checkWindowsBridge, pushPendingToWindows } from "./src/services/windowsBridge";
import { CashMovement, Order, OrderItem, Product, Session, Settings, StoreProfile } from "./src/types";
import { colors } from "./src/theme";
import { money, nowIso, padOrderNumber } from "./src/utils/format";
import { newId } from "./src/utils/id";

type Tab = "Caixa" | "Comandas" | "Delivery" | "Produtos" | "Estoque" | "Pedidos" | "Config";

const tabs: Tab[] = ["Caixa", "Comandas", "Delivery", "Produtos", "Estoque", "Pedidos", "Config"];

export default function App() {
  const [booting, setBooting] = useState(true);
  const [session, setSession] = useState<Session | null>(null);
  const [settings, setSettings] = useState<Settings | null>(null);
  const [tab, setTab] = useState<Tab>("Comandas");
  const [status, setStatus] = useState("");
  const [products, setProducts] = useState<Product[]>([]);
  const [orders, setOrders] = useState<Order[]>([]);
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);
  const [items, setItems] = useState<OrderItem[]>([]);
  const [cash, setCash] = useState<CashMovement[]>([]);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    void start();
  }, []);

  async function start() {
    await database();
    const saved = await loadSession();
    const localSettings = await getSettings();
    setSession(saved);
    setSettings(localSettings);
    await refresh();
    setBooting(false);
    if (saved) {
      void bootstrapAndApply(saved).then(refresh).catch(() => null);
    }
  }

  async function refresh() {
    setProducts(await listProducts());
    const nextOrders = await listOrders();
    setOrders(nextOrders);
    setCash(await cashMovements());
    if (selectedOrder) {
      setSelectedOrder(nextOrders.find((order) => order.id === selectedOrder.id) ?? selectedOrder);
      setItems(await orderItems(selectedOrder.id));
    }
    setSettings(await getSettings());
  }

  async function selectOrder(order: Order) {
    setSelectedOrder(order);
    setItems(await orderItems(order.id));
  }

  async function guarded(action: () => Promise<void>, label = "Processando...") {
    if (busy) return;
    setBusy(true);
    setStatus(label);
    try {
      await action();
    } catch (error) {
      Alert.alert("Balcao Livre", error instanceof Error ? error.message : "Falha na operacao.");
    } finally {
      setBusy(false);
      await refresh();
    }
  }

  if (booting) {
    return <Centered text="Abrindo Balcao Livre Mobile..." />;
  }

  if (!session) {
    return <LoginScreen onLogged={async (next) => {
      setSession(next);
      setStatus("Conta mobile ativada.");
      await bootstrapAndApply(next).catch((error) => setStatus(error.message));
      await refresh();
    }} />;
  }

  const cashTotal = cash.reduce((sum, row) => {
    if (row.type === "WITHDRAW" || row.type === "CLOSE") return sum - row.amount;
    return sum + row.amount;
  }, 0);

  return (
    <SafeAreaView style={styles.safe}>
      <StatusBar barStyle="light-content" backgroundColor={colors.header} />
      <View style={styles.header}>
        <View style={styles.logo}><Text style={styles.logoText}>BL</Text></View>
        <View style={{ flex: 1 }}>
          <Text style={styles.headerTitle}>{session.profile.businessName || "Balcao Livre PDV"}</Text>
          <Text style={styles.headerSub}>{settings?.cashOpen ? "Caixa aberto" : "Caixa fechado"} | Sync {settings?.lastSyncAt ? new Date(settings.lastSyncAt).toLocaleTimeString("pt-BR") : "pendente"}</Text>
        </View>
        <Text style={styles.cashText}>{money(cashTotal)}</Text>
      </View>

      <View style={styles.tabs}>
        {tabs.map((item) => (
          <Pressable key={item} onPress={() => setTab(item)} style={[styles.tab, tab === item && styles.tabActive]}>
            <Text style={[styles.tabText, tab === item && styles.tabTextActive]}>{item}</Text>
          </Pressable>
        ))}
      </View>

      <ScrollView style={styles.body} contentContainerStyle={{ paddingBottom: 32 }}>
        {tab === "Caixa" && <CashTab settings={settings} cashTotal={cashTotal} onOpenClose={(open) => guarded(() => setCashOpen(open, 0), open ? "Abrindo caixa..." : "Fechando caixa...")} />}
        {tab === "Comandas" && (
          <OrdersTab
            kind="MESA"
            orders={orders.filter((order) => order.kind === "MESA")}
            products={products}
            selectedOrder={selectedOrder}
            items={items}
            onSelect={selectOrder}
            onOpen={(number, waiter, customer) => guarded(async () => selectOrder(await openOrder("MESA", padOrderNumber(number), waiter, customer)), "Abrindo mesa...")}
            onAdd={(product, quantity, note) => selectedOrder && guarded(() => addProductToOrder(selectedOrder, product, quantity, note).then(async () => setItems(await orderItems(selectedOrder.id))), "Lancando produto...")}
            onClose={(method) => selectedOrder && guarded(async () => { await closeOrder(selectedOrder, method, selectedOrder.total); }, "Fechando conta...")}
            onPrint={() => selectedOrder && settings && guarded(async () => printJob(settings, "receipt", buildReceiptText(selectedOrder, items, "CONFERENCIA DA CONTA"), `Mesa ${selectedOrder.number}`), "Imprimindo...")}
          />
        )}
        {tab === "Delivery" && (
          <OrdersTab
            kind="DELIVERY"
            orders={orders.filter((order) => order.kind === "DELIVERY")}
            products={products}
            selectedOrder={selectedOrder}
            items={items}
            onSelect={selectOrder}
            onOpen={(number, waiter, customer) => guarded(async () => selectOrder(await openOrder("DELIVERY", padOrderNumber(number || String(Date.now()).slice(-6)), waiter, customer)), "Criando delivery...")}
            onAdd={(product, quantity, note) => selectedOrder && guarded(() => addProductToOrder(selectedOrder, product, quantity, note).then(async () => setItems(await orderItems(selectedOrder.id))), "Lancando produto...")}
            onClose={(method) => selectedOrder && guarded(async () => { await closeOrder(selectedOrder, method, selectedOrder.total); }, "Fechando delivery...")}
            onPrint={() => selectedOrder && settings && guarded(async () => printJob(settings, "delivery", buildReceiptText(selectedOrder, items, "PEDIDO DELIVERY"), `Delivery ${selectedOrder.number}`), "Imprimindo delivery...")}
          />
        )}
        {tab === "Produtos" && <ProductsTab products={products} onSave={(product) => guarded(() => putProduct(product), "Salvando produto...")} />}
        {tab === "Estoque" && <StockTab products={products} onAdjust={(product, quantity) => guarded(() => adjustStock(product, quantity, "Ajuste mobile"), "Ajustando estoque...")} />}
        {tab === "Pedidos" && <OrdersOnlineTab session={session} onPoll={() => guarded(async () => { const found = await pollIFoodOrders(session); setStatus(`${found.length} pedido(s) iFood importado(s).`); }, "Buscando pedidos...")} />}
        {tab === "Config" && settings && (
          <ConfigTab
            session={session}
            settings={settings}
            status={status}
            onSave={(next) => guarded(async () => saveSettings(next), "Salvando configuracoes...")}
            onSync={() => guarded(async () => { const result = await flushSync(session); setStatus(`${result.synced} evento(s) sincronizado(s).`); }, "Sincronizando...")}
            onBridge={() => guarded(async () => {
              await checkWindowsBridge(settings);
              const result = await pushPendingToWindows(settings);
              setStatus(`${result.imported} evento(s) enviados ao Windows.`);
            }, "Enviando para Windows...")}
            onBackup={() => guarded(async () => { await backupMobile(session); setStatus("Backup mobile enviado."); }, "Enviando backup...")}
            onLogout={() => guarded(async () => { await clearSession(); setSession(null); }, "Saindo...")}
          />
        )}
      </ScrollView>

      <View style={styles.footer}>
        <Text style={styles.footerText}>{busy ? status : status || "Enter cria mesa, produto baixa estoque e sync fica pendente ate ter internet."}</Text>
      </View>
    </SafeAreaView>
  );
}

function LoginScreen({ onLogged }: { onLogged: (session: Session) => Promise<void> }) {
  const [licenseKey, setLicenseKey] = useState("");
  const [adminUrl, setAdminUrl] = useState(defaultAdminApiUrl());
  const [profile, setProfile] = useState<StoreProfile>({
    email: "",
    businessName: "",
    ownerName: "",
    document: "",
    phone: "",
    city: "",
    state: ""
  });
  const [busy, setBusy] = useState(false);

  async function login() {
    if (busy) return;
    setBusy(true);
    try {
      const session = await activateMobile(licenseKey, profile, adminUrl);
      await onLogged(session);
    } catch (error) {
      Alert.alert("Ativacao", error instanceof Error ? error.message : "Falha ao ativar.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <SafeAreaView style={styles.safe}>
      <StatusBar barStyle="light-content" backgroundColor={colors.header} />
      <ScrollView style={styles.body}>
        <View style={styles.loginHero}>
          <View style={styles.logoBig}><Text style={styles.logoBigText}>BL</Text></View>
          <Text style={styles.loginTitle}>Balcao Livre Mobile</Text>
          <Text style={styles.loginSub}>Entre com a key da loja para liberar o PDV completo no celular.</Text>
        </View>
        <Field label="URL do servidor" value={adminUrl} onChangeText={setAdminUrl} />
        <Field label="Key da licenca" value={licenseKey} onChangeText={setLicenseKey} autoCapitalize="characters" />
        <Field label="Email da conta" value={profile.email} onChangeText={(email) => setProfile({ ...profile, email })} keyboardType="email-address" />
        <Field label="Nome da loja" value={profile.businessName} onChangeText={(businessName) => setProfile({ ...profile, businessName })} />
        <Field label="Responsavel" value={profile.ownerName} onChangeText={(ownerName) => setProfile({ ...profile, ownerName })} />
        <Field label="CPF ou CNPJ" value={profile.document} onChangeText={(document) => setProfile({ ...profile, document })} />
        <Field label="Telefone" value={profile.phone} onChangeText={(phone) => setProfile({ ...profile, phone })} keyboardType="phone-pad" />
        <PrimaryButton label={busy ? "Validando..." : "Entrar no PDV"} onPress={login} />
      </ScrollView>
    </SafeAreaView>
  );
}

function CashTab({ settings, cashTotal, onOpenClose }: { settings: Settings | null; cashTotal: number; onOpenClose: (open: boolean) => void }) {
  return (
    <View>
      <SectionTitle title="Caixa" />
      <View style={styles.metricRow}>
        <Metric title="Status" value={settings?.cashOpen ? "ABERTO" : "FECHADO"} />
        <Metric title="Movimento" value={money(cashTotal)} />
      </View>
      <PrimaryButton label={settings?.cashOpen ? "Fechar caixa" : "Abrir caixa"} onPress={() => onOpenClose(!settings?.cashOpen)} />
    </View>
  );
}

function OrdersTab(props: {
  kind: "MESA" | "DELIVERY";
  orders: Order[];
  products: Product[];
  selectedOrder: Order | null;
  items: OrderItem[];
  onSelect: (order: Order) => void;
  onOpen: (number: string, waiter: string, customer: string) => void;
  onAdd: (product: Product, quantity: number, note: string) => void;
  onClose: (method: "DINHEIRO" | "PIX" | "CREDITO" | "DEBITO") => void;
  onPrint: () => void;
}) {
  const [number, setNumber] = useState("");
  const [waiter, setWaiter] = useState("1");
  const [customer, setCustomer] = useState("");
  const [search, setSearch] = useState("");
  const [quantity, setQuantity] = useState("1");
  const [note, setNote] = useState("");
  const visibleProducts = props.products.filter((product) => `${product.code} ${product.name}`.toLowerCase().includes(search.toLowerCase())).slice(0, 8);

  return (
    <View>
      <SectionTitle title={props.kind === "MESA" ? "Comandas / Mesas" : "Delivery"} />
      <View style={styles.row}>
        <TextInput style={[styles.input, { flex: 1 }]} placeholder="Numero" value={number} onChangeText={setNumber} keyboardType="number-pad" onSubmitEditing={() => props.onOpen(number, waiter, customer)} />
        <TextInput style={[styles.input, { width: 84 }]} placeholder="Garcom" value={waiter} onChangeText={setWaiter} keyboardType="number-pad" />
      </View>
      <Field label="Cliente opcional" value={customer} onChangeText={setCustomer} />
      <PrimaryButton label={props.kind === "MESA" ? "Abrir/criar mesa" : "Criar delivery"} onPress={() => props.onOpen(number, waiter, customer)} />

      <View style={styles.grid}>
        {props.orders.map((order) => (
          <Pressable key={order.id} style={[styles.orderCard, props.selectedOrder?.id === order.id && styles.orderSelected]} onPress={() => props.onSelect(order)}>
            <View style={[styles.statusStrip, { backgroundColor: statusColor(order.status) }]} />
            <Text style={styles.orderNumber}>{order.number}</Text>
            <Text style={styles.muted}>{order.customerName || order.kind}</Text>
            <Text style={styles.badge}>{order.status}</Text>
            <Text style={styles.orderTotal}>{money(order.total)}</Text>
          </Pressable>
        ))}
      </View>

      {props.selectedOrder && (
        <View style={styles.card}>
          <Text style={styles.cardTitle}>{props.selectedOrder.kind} {props.selectedOrder.number}</Text>
          <Text style={styles.muted}>{props.selectedOrder.customerName || "Sem cliente"} | {money(props.selectedOrder.total)}</Text>
          {props.items.map((item) => (
            <Text key={item.id} style={styles.itemLine}>{item.quantity}x {item.name} - {money(item.total)}</Text>
          ))}
          <Field label="Buscar produto" value={search} onChangeText={setSearch} />
          <View style={styles.row}>
            <TextInput style={[styles.input, { width: 90 }]} value={quantity} onChangeText={setQuantity} keyboardType="number-pad" />
            <TextInput style={[styles.input, { flex: 1 }]} placeholder="Observacao" value={note} onChangeText={setNote} />
          </View>
          {visibleProducts.map((product) => (
            <Pressable key={product.id} style={styles.productRow} onPress={() => props.onAdd(product, Math.max(1, Number(quantity) || 1), note)}>
              <View>
                <Text style={styles.productName}>{product.name}</Text>
                <Text style={styles.muted}>{product.code} | Est. {product.stock}</Text>
              </View>
              <Text style={styles.price}>{money(product.price)}</Text>
            </Pressable>
          ))}
          <View style={styles.actions}>
            <SecondaryButton label="Imprimir" onPress={props.onPrint} />
            <SecondaryButton label="Dinheiro" onPress={() => props.onClose("DINHEIRO")} />
            <SecondaryButton label="Pix" onPress={() => props.onClose("PIX")} />
            <SecondaryButton label="Cartao" onPress={() => props.onClose("CREDITO")} />
          </View>
        </View>
      )}
    </View>
  );
}

function ProductsTab({ products, onSave }: { products: Product[]; onSave: (product: Product) => void }) {
  const [name, setName] = useState("");
  const [price, setPrice] = useState("");
  const [category, setCategory] = useState("GERAL");
  const nextCode = useMemo(() => String(100000 + products.length + 1), [products.length]);
  return (
    <View>
      <SectionTitle title="Produtos" />
      <Field label="Nome" value={name} onChangeText={setName} />
      <View style={styles.row}>
        <TextInput style={[styles.input, { flex: 1 }]} placeholder="Categoria" value={category} onChangeText={setCategory} />
        <TextInput style={[styles.input, { flex: 1 }]} placeholder="Preco" value={price} onChangeText={setPrice} keyboardType="decimal-pad" />
      </View>
      <PrimaryButton label="Novo produto" onPress={() => onSave({
        id: newId("prd"),
        code: nextCode,
        name: name.trim() || "Novo produto",
        category: category.trim() || "GERAL",
        price: Number(price.replace(",", ".")) || 0,
        costPrice: 0,
        stock: 0,
        minStock: 0,
        active: 1,
        destination: "COZINHA",
        imageUrl: "",
        updatedAt: nowIso()
      })} />
      {products.map((product) => (
        <View key={product.id} style={styles.productRow}>
          <View>
            <Text style={styles.productName}>{product.name}</Text>
            <Text style={styles.muted}>{product.code} | {product.category}</Text>
          </View>
          <Text style={styles.price}>{money(product.price)}</Text>
        </View>
      ))}
    </View>
  );
}

function StockTab({ products, onAdjust }: { products: Product[]; onAdjust: (product: Product, quantity: number) => void }) {
  return (
    <View>
      <SectionTitle title="Estoque" />
      {products.map((product) => (
        <View key={product.id} style={styles.productRow}>
          <View>
            <Text style={styles.productName}>{product.name}</Text>
            <Text style={product.stock <= product.minStock ? styles.danger : styles.muted}>Estoque {product.stock} | Min {product.minStock}</Text>
          </View>
          <View style={styles.actionsInline}>
            <SmallButton label="-1" onPress={() => onAdjust(product, -1)} />
            <SmallButton label="+1" onPress={() => onAdjust(product, 1)} />
          </View>
        </View>
      ))}
    </View>
  );
}

function OrdersOnlineTab({ session, onPoll }: { session: Session; onPoll: () => void }) {
  return (
    <View>
      <SectionTitle title="Pedidos online" />
      <View style={styles.card}>
        <Text style={styles.cardTitle}>iFood / Cardapio / WhatsApp</Text>
        <Text style={styles.muted}>O mobile usa as funcoes online ja ligadas na key da loja. Os pedidos importados entram na fila local e sincronizam com o caixa.</Text>
        <PrimaryButton label="Atualizar pedidos iFood" onPress={onPoll} />
        <Text style={styles.muted}>Licenca: {session.licenseKey}</Text>
      </View>
    </View>
  );
}

function ConfigTab(props: {
  session: Session;
  settings: Settings;
  status: string;
  onSave: (settings: Settings) => void;
  onSync: () => void;
  onBridge: () => void;
  onBackup: () => void;
  onLogout: () => void;
}) {
  const [bridge, setBridge] = useState(props.settings.windowsBridgeUrl);
  const [printMode, setPrintMode] = useState(props.settings.printMode);
  return (
    <View>
      <SectionTitle title="Config" />
      <Field label="Windows bridge" value={bridge} onChangeText={setBridge} />
      <View style={styles.actions}>
        {(["WINDOWS_BRIDGE", "ESC_POS_NETWORK", "ESC_POS_BLUETOOTH"] as const).map((mode) => (
          <SecondaryButton key={mode} label={mode.replace("ESC_POS_", "")} active={printMode === mode} onPress={() => setPrintMode(mode)} />
        ))}
      </View>
      <PrimaryButton label="Salvar config" onPress={() => props.onSave({ ...props.settings, windowsBridgeUrl: bridge, printMode })} />
      <SecondaryButton label="Sincronizar agora" onPress={props.onSync} />
      <SecondaryButton label="Enviar eventos ao Windows" onPress={props.onBridge} />
      <SecondaryButton label="Enviar backup" onPress={props.onBackup} />
      <SecondaryButton label="Sair da conta" onPress={props.onLogout} />
      <Text style={styles.muted}>Plano: {props.session.plan}</Text>
      <Text style={styles.muted}>Vence: {props.session.expiresAt || "sem data"}</Text>
      <Text style={styles.muted}>{props.status}</Text>
    </View>
  );
}

function Field(props: React.ComponentProps<typeof TextInput> & { label: string }) {
  const { label, ...inputProps } = props;
  return (
    <View style={{ marginBottom: 10 }}>
      <Text style={styles.label}>{label}</Text>
      <TextInput {...inputProps} style={[styles.input, inputProps.style]} placeholderTextColor="#8A9AAA" />
    </View>
  );
}

function SectionTitle({ title }: { title: string }) {
  return <Text style={styles.sectionTitle}>{title}</Text>;
}

function Metric({ title, value }: { title: string; value: string }) {
  return (
    <View style={styles.metric}>
      <Text style={styles.muted}>{title}</Text>
      <Text style={styles.metricValue}>{value}</Text>
    </View>
  );
}

function PrimaryButton({ label, onPress }: { label: string; onPress: () => void }) {
  return (
    <Pressable style={styles.primaryButton} onPress={onPress}>
      <Text style={styles.primaryText}>{label}</Text>
    </Pressable>
  );
}

function SecondaryButton({ label, onPress, active = false }: { label: string; onPress: () => void; active?: boolean }) {
  return (
    <Pressable style={[styles.secondaryButton, active && styles.secondaryActive]} onPress={onPress}>
      <Text style={[styles.secondaryText, active && styles.secondaryTextActive]}>{label}</Text>
    </Pressable>
  );
}

function SmallButton({ label, onPress }: { label: string; onPress: () => void }) {
  return (
    <Pressable style={styles.smallButton} onPress={onPress}>
      <Text style={styles.secondaryText}>{label}</Text>
    </Pressable>
  );
}

function Centered({ text }: { text: string }) {
  return (
    <SafeAreaView style={[styles.safe, styles.centered]}>
      <Text style={styles.cardTitle}>{text}</Text>
    </SafeAreaView>
  );
}

function statusColor(status: string) {
  switch (status) {
    case "NOVO": return colors.new;
    case "PREPARO": return colors.prep;
    case "PREPARANDO": return colors.preparing;
    case "DESPACHADO": return colors.dispatched;
    case "ENTREGUE":
    case "FECHADO": return colors.delivered;
    case "CANCELADO": return colors.cancelled;
    default: return colors.line;
  }
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.page },
  centered: { alignItems: "center", justifyContent: "center" },
  header: { backgroundColor: colors.header, padding: 14, flexDirection: "row", alignItems: "center", gap: 12 },
  logo: { width: 38, height: 38, borderRadius: 8, backgroundColor: colors.brand, alignItems: "center", justifyContent: "center" },
  logoText: { color: "#fff", fontWeight: "900", fontSize: 17 },
  logoBig: { width: 64, height: 64, borderRadius: 12, backgroundColor: colors.brand, alignItems: "center", justifyContent: "center", marginBottom: 12 },
  logoBigText: { color: "#fff", fontWeight: "900", fontSize: 30 },
  headerTitle: { color: "#fff", fontWeight: "800", fontSize: 18 },
  headerSub: { color: "#C9D7E3", fontSize: 12, marginTop: 2 },
  cashText: { color: "#fff", fontWeight: "900", fontSize: 16 },
  tabs: { backgroundColor: "#fff", flexDirection: "row", flexWrap: "wrap", padding: 8, gap: 6, borderBottomWidth: 1, borderBottomColor: colors.line },
  tab: { paddingVertical: 9, paddingHorizontal: 12, borderRadius: 8, backgroundColor: colors.brandSoft },
  tabActive: { backgroundColor: colors.header },
  tabText: { color: colors.brand, fontWeight: "800", fontSize: 12 },
  tabTextActive: { color: "#fff" },
  body: { flex: 1, padding: 12 },
  footer: { backgroundColor: "#fff", borderTopWidth: 1, borderTopColor: colors.line, padding: 10 },
  footerText: { color: colors.muted, fontSize: 12 },
  loginHero: { backgroundColor: colors.header, borderRadius: 12, padding: 18, marginBottom: 14 },
  loginTitle: { color: "#fff", fontSize: 24, fontWeight: "900" },
  loginSub: { color: "#C9D7E3", marginTop: 6 },
  label: { color: colors.muted, fontWeight: "800", marginBottom: 5 },
  input: { backgroundColor: "#fff", borderWidth: 1, borderColor: colors.line, borderRadius: 8, minHeight: 46, paddingHorizontal: 12, color: colors.ink, fontSize: 16 },
  row: { flexDirection: "row", gap: 8, alignItems: "center" },
  sectionTitle: { color: colors.ink, fontSize: 22, fontWeight: "900", marginVertical: 10 },
  primaryButton: { backgroundColor: colors.brand, borderRadius: 8, paddingVertical: 14, alignItems: "center", marginVertical: 8 },
  primaryText: { color: "#fff", fontSize: 16, fontWeight: "900" },
  secondaryButton: { backgroundColor: "#fff", borderRadius: 8, borderWidth: 1, borderColor: colors.line, paddingVertical: 12, paddingHorizontal: 14, alignItems: "center", marginVertical: 5 },
  secondaryActive: { backgroundColor: colors.brand },
  secondaryText: { color: colors.ink, fontWeight: "900" },
  secondaryTextActive: { color: "#fff" },
  smallButton: { backgroundColor: "#fff", borderWidth: 1, borderColor: colors.line, borderRadius: 8, paddingVertical: 8, paddingHorizontal: 12 },
  metricRow: { flexDirection: "row", gap: 10 },
  metric: { flex: 1, backgroundColor: "#fff", borderRadius: 10, borderWidth: 1, borderColor: colors.line, padding: 12 },
  metricValue: { color: colors.brand, fontSize: 22, fontWeight: "900", marginTop: 4 },
  grid: { flexDirection: "row", flexWrap: "wrap", gap: 10, marginTop: 10 },
  orderCard: { width: "30.8%", minHeight: 118, backgroundColor: "#fff", borderRadius: 10, borderWidth: 1, borderColor: colors.line, padding: 8, alignItems: "center", overflow: "hidden" },
  orderSelected: { borderColor: colors.teal, borderWidth: 2, backgroundColor: "#EAFDFC" },
  statusStrip: { position: "absolute", top: 0, left: 0, right: 0, height: 6 },
  orderNumber: { color: colors.ink, fontWeight: "900", fontSize: 16, marginTop: 10 },
  orderTotal: { color: colors.brand, fontWeight: "900", marginTop: 5 },
  badge: { backgroundColor: colors.brandSoft, color: colors.brand, borderRadius: 999, overflow: "hidden", paddingHorizontal: 8, paddingVertical: 3, fontSize: 11, fontWeight: "900", marginTop: 5 },
  card: { backgroundColor: "#fff", borderRadius: 10, borderWidth: 1, borderColor: colors.line, padding: 12, marginTop: 12 },
  cardTitle: { color: colors.ink, fontSize: 18, fontWeight: "900", marginBottom: 5 },
  muted: { color: colors.muted, fontSize: 12 },
  danger: { color: colors.cancelled, fontWeight: "800", fontSize: 12 },
  itemLine: { color: colors.ink, paddingVertical: 4 },
  productRow: { backgroundColor: "#fff", borderRadius: 10, borderWidth: 1, borderColor: colors.line, padding: 12, marginVertical: 5, flexDirection: "row", alignItems: "center", justifyContent: "space-between" },
  productName: { color: colors.ink, fontWeight: "900", fontSize: 15 },
  price: { color: colors.teal, fontWeight: "900", fontSize: 15 },
  actions: { flexDirection: "row", flexWrap: "wrap", gap: 8, marginTop: 8 },
  actionsInline: { flexDirection: "row", gap: 6 }
});
