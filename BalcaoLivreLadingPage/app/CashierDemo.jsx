"use client";

import { useMemo, useRef, useState } from "react";

const products = [
  { code: "000001", name: "COCA COLA", group: "BEBIDAS", price: 6, cost: 3.2, stock: 18, min: 5 },
  { code: "000002", name: "COXINHA DE CATUPIRY", group: "SALGADOS", price: 4, cost: 1.55, stock: 10, min: 4 },
  { code: "000003", name: "X-BURGER", group: "LANCHES", price: 18, cost: 9.4, stock: 8, min: 3 },
  { code: "000004", name: "SUCO NATURAL", group: "BEBIDAS", price: 9, cost: 3.8, stock: 12, min: 4 },
  { code: "000005", name: "BATATA FRITA", group: "PORCOES", price: 14, cost: 5.7, stock: 7, min: 3 },
  { code: "000006", name: "PIZZA BROTINHO", group: "PIZZAS", price: 36, cost: 17.2, stock: 5, min: 2 },
  { code: "000007", name: "HAMBURGUER ARTESANAL", group: "LANCHES", price: 28, cost: 13.6, stock: 11, min: 3 },
  { code: "000008", name: "CHEESE SALADA", group: "LANCHES", price: 22, cost: 10.4, stock: 14, min: 4 },
  { code: "000009", name: "PASTEL DE CARNE", group: "SALGADOS", price: 8, cost: 3.1, stock: 24, min: 6 },
  { code: "000010", name: "PASTEL DE QUEIJO", group: "SALGADOS", price: 8, cost: 2.9, stock: 22, min: 6 },
  { code: "000011", name: "FRANGO A PASSARINHO", group: "PORCOES", price: 38, cost: 18.8, stock: 6, min: 2 },
  { code: "000012", name: "CALABRESA ACEBOLADA", group: "PORCOES", price: 32, cost: 14.7, stock: 9, min: 3 },
  { code: "000013", name: "MARMITA EXECUTIVA", group: "PRATOS", price: 24.9, cost: 11.2, stock: 16, min: 5 },
  { code: "000014", name: "FILE COM FRITAS", group: "PRATOS", price: 39.9, cost: 19.8, stock: 8, min: 3 },
  { code: "000015", name: "PIZZA MUSSARELA", group: "PIZZAS", price: 58, cost: 27.5, stock: 5, min: 2 },
  { code: "000016", name: "PIZZA CALABRESA", group: "PIZZAS", price: 62, cost: 29.6, stock: 5, min: 2 },
  { code: "000017", name: "REFRIGERANTE LATA", group: "BEBIDAS", price: 7, cost: 3.05, stock: 36, min: 10 },
  { code: "000018", name: "AGUA MINERAL", group: "BEBIDAS", price: 4, cost: 1.25, stock: 42, min: 12 },
  { code: "000019", name: "BROWNIE COM SORVETE", group: "SOBREMESAS", price: 18, cost: 7.6, stock: 10, min: 3 },
  { code: "000020", name: "TAXA DE ENTREGA", group: "DELIVERY", price: 8, cost: 0, stock: 999, min: 0 }
];

const paymentMethods = ["Dinheiro", "Pix", "Credito", "Debito"];

const qrCells = [
  1, 1, 1, 0, 1, 0, 1, 1, 0,
  1, 0, 1, 1, 0, 1, 0, 1, 1,
  1, 1, 1, 0, 1, 1, 1, 0, 1,
  0, 1, 0, 1, 1, 0, 1, 1, 0,
  1, 0, 1, 0, 1, 1, 0, 1, 1,
  0, 1, 1, 1, 0, 1, 1, 0, 1,
  1, 0, 1, 1, 1, 0, 1, 1, 1,
  1, 1, 0, 0, 1, 1, 0, 1, 0,
  0, 1, 1, 1, 0, 1, 1, 1, 1
];

function money(value) {
  return value.toLocaleString("pt-BR", {
    style: "currency",
    currency: "BRL"
  });
}

function inputMoney(value) {
  return value.toLocaleString("pt-BR", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
}

function parseMoney(value) {
  const normalized = String(value).replace(/\./g, "").replace(",", ".");
  const parsed = Number.parseFloat(normalized);
  return Number.isFinite(parsed) ? parsed : 0;
}

export default function CashierDemo() {
  const receiptRef = useRef(null);
  const [items, setItems] = useState([]);
  const [code, setCode] = useState("");
  const [selectedCode, setSelectedCode] = useState("000001");
  const [received, setReceived] = useState("0,00");
  const [method, setMethod] = useState("Dinheiro");
  const [modal, setModal] = useState(null);
  const [receiptReady, setReceiptReady] = useState(false);
  const [message, setMessage] = useState("Teste o caixa: clique em um produto ou digite 000003 e pressione Enter.");

  const total = useMemo(
    () => items.reduce((sum, item) => sum + item.price * item.qty, 0),
    [items]
  );
  const stockRows = useMemo(
    () =>
      products.map((product) => {
        const sold = items.find((item) => item.code === product.code)?.qty ?? 0;
        const available = product.stock - sold;
        const profit = product.price - product.cost;
        const margin = Math.round((profit / product.price) * 100);

        return { ...product, sold, available, profit, margin };
      }),
    [items]
  );
  const lowStockCount = stockRows.filter((product) => product.available <= product.min).length;
  const receivedValue = parseMoney(received);
  const change = Math.max(0, receivedValue - total);

  function availableStock(productCode) {
    return stockRows.find((product) => product.code === productCode)?.available ?? 0;
  }

  function addProduct(product) {
    if (availableStock(product.code) <= 0) {
      setSelectedCode(product.code);
      setMessage(`${product.name} sem estoque na demo.`);
      return;
    }

    setItems((current) => {
      const existing = current.find((item) => item.code === product.code);
      if (existing) {
        return current.map((item) =>
          item.code === product.code ? { ...item, qty: item.qty + 1 } : item
        );
      }

      return [...current, { ...product, qty: 1 }];
    });
    setSelectedCode(product.code);
    setCode("");
    setReceiptReady(false);
    setMessage(`${product.name} incluido. Estoque baixou automaticamente na demo.`);
  }

  function addByCode() {
    const typedCode = code.trim().padStart(6, "0");
    const product = products.find((item) => item.code === typedCode);

    if (!product) {
      setMessage("Codigo nao encontrado. Use 000001 a 000020 nesta demo.");
      return;
    }

    addProduct(product);
  }

  function removeItem(codeToRemove) {
    setItems((current) => current.filter((item) => item.code !== codeToRemove));
    setReceiptReady(false);
    setMessage("Item removido da comanda.");
  }

  function clearSale() {
    setItems([]);
    setCode("");
    setReceived("0,00");
    setMethod("Dinheiro");
    setModal(null);
    setReceiptReady(false);
    setMessage("Caixa limpo. Comece outra venda com os produtos fake.");
  }

  function finishSale() {
    if (items.length === 0) {
      setMessage("Inclua pelo menos um produto antes de finalizar.");
      return;
    }

    setModal("payment");
    setReceiptReady(false);
    setMessage("Escolha a forma de pagamento para gerar o comprovante.");
  }

  function chooseMethod(option) {
    setMethod(option);
    if (option !== "Dinheiro") {
      setReceived(inputMoney(total));
    }
  }

  function confirmPayment() {
    if (parseMoney(received) < total) {
      setMessage("Valor recebido menor que o total da comanda.");
      return;
    }

    setModal(null);
    setReceiptReady(true);
    setMessage(`Venda finalizada em ${method}. Comprovante gerado na tela.`);
    window.setTimeout(() => {
      receiptRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
    }, 80);
  }

  return (
    <section className="appDemo cashierDemoSection" aria-label="Teste do caixa do Balcao Livre PDV">
      <div className="sectionIntro">
        <p className="eyebrow">Teste o caixa</p>
        <h2>Clique nos produtos ou digite o codigo para simular uma venda.</h2>
        <p>
          Esta demo mostra o fluxo que o restaurante usa no dia a dia: lancar
          item, agrupar quantidade, ver total, informar dinheiro recebido e
          calcular troco.
        </p>
      </div>

      <div className="cashierDemo">
        <div className="cashierTop">
          <div>
            <span>Balcao Livre PDV</span>
            <strong>Caixa demonstrativo</strong>
          </div>
          <b>Caixa aberto {money(184 + total)}</b>
        </div>

        <div className="cashierTabs" aria-label="Modos de venda">
          <button className="active" type="button">Comandas</button>
          <button type="button">Balcao</button>
          <button type="button">Delivery</button>
        </div>

        <div className="cashierGrid">
          <aside className="ticketConsole">
            <div className="consoleTitle">
              <span>Comanda</span>
              <b>000012</b>
            </div>
            <div className="miniFields">
              <span>Mesa 12</span>
              <span>Garcom 2</span>
              <span>{items.reduce((sum, item) => sum + item.qty, 0)} itens</span>
            </div>
            <div className="ticketRows">
              {items.length === 0 ? (
                <p className="emptyTicket">Nenhum produto lancado.</p>
              ) : (
                items.map((item) => (
                  <div className="ticketRow" key={item.code}>
                    <span>{item.code}</span>
                    <strong>{item.name}</strong>
                    <b>{item.qty}x</b>
                    <em>{money(item.price * item.qty)}</em>
                    <button type="button" onClick={() => removeItem(item.code)}>Excluir</button>
                  </div>
                ))
              )}
            </div>
            <div className="ticketTotalLive">
              <span>Total da comanda</span>
              <strong>{money(total)}</strong>
            </div>
          </aside>

          <section className="productConsole">
            <div className="codeEntry">
              <label htmlFor="demo-product-code">Codigo do produto</label>
              <div>
                <input
                  id="demo-product-code"
                  inputMode="numeric"
                  placeholder="Ex: 000003"
                  value={code}
                  onChange={(event) => setCode(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === "Enter") {
                      addByCode();
                    }
                  }}
                />
                <button type="button" onClick={addByCode}>Enter incluir</button>
              </div>
            </div>

            <div className="productPicker">
              {products.map((product) => (
                <button
                  className={product.code === selectedCode ? "active" : undefined}
                  key={product.code}
                  type="button"
                  onClick={() => addProduct(product)}
                  disabled={availableStock(product.code) <= 0}
                >
                  <span>{product.code}</span>
                  <strong>{product.name}</strong>
                  <small>{product.group} | Est. {availableStock(product.code)}</small>
                  <b>{money(product.price)}</b>
                </button>
              ))}
            </div>
          </section>

          <aside className="paymentConsole">
            <div className="paymentTotal">
              <span>Total</span>
              <strong>{money(total)}</strong>
            </div>
            <label htmlFor="demo-received">Valor recebido</label>
            <input
              id="demo-received"
              inputMode="decimal"
              value={received}
              onChange={(event) => setReceived(event.target.value)}
            />
            <div className="changeBox">
              <span>Troco</span>
              <b>{money(change)}</b>
            </div>
            <div className="payButtons">
              {["Dinheiro", "Pix", "Credito"].map((option) => (
                <button
                  className={method === option ? "active" : undefined}
                  key={option}
                  type="button"
                  onClick={() => chooseMethod(option)}
                >
                  {option}
                </button>
              ))}
            </div>
            <button className="finishButton" type="button" onClick={finishSale}>
              Finalizar venda
            </button>
            <button className="clearButton" type="button" onClick={clearSale}>
              Limpar venda
            </button>
          </aside>

          <section className="stockConsole" aria-label="Estoque integrado ao caixa demo">
            <div className="stockHeader">
              <div>
                <span>Estoque conectado ao PDV</span>
                <strong>Venda baixa estoque e mostra lucro previsto.</strong>
              </div>
              <b>{lowStockCount} alerta{lowStockCount === 1 ? "" : "s"} de minimo</b>
            </div>
            <div className="stockRows">
              {stockRows.map((product) => (
                <div
                  className={product.available <= product.min ? "stockRow low" : "stockRow"}
                  key={product.code}
                >
                  <span>{product.code}</span>
                  <strong>{product.name}</strong>
                  <small>Est. {product.available} / min. {product.min}</small>
                  <em>Compra {money(product.cost)}</em>
                  <b>Lucro {money(product.profit)} | {product.margin}%</b>
                </div>
              ))}
            </div>
          </section>
        </div>

        <div className="demoStatus">
          <span>{message}</span>
          <b>Comprovante: NAO E DOCUMENTO FISCAL</b>
        </div>

        <section className="inlineReceiptDemo" ref={receiptRef} id="comprovante-demo">
          <div className="inlineReceiptCopy">
            <span>Comprovante demo</span>
            <strong>{receiptReady ? "Venda finalizada na demo." : "A previa acompanha os produtos adicionados."}</strong>
            <p>
              Adicione produtos no PDV, escolha a forma de pagamento e confirme
              para ver o comprovante preenchido com a venda testada.
            </p>
          </div>
          <div className="receiptPaper liveReceiptPaper">
            <h3>BALCAO LIVRE PDV</h3>
            <p>{receiptReady ? "COMPROVANTE GERADO" : "PREVIA DA CONTA"}</p>
            <p>COMANDA 000012 | GARCOM 2</p>
            <p>NAO E DOCUMENTO FISCAL</p>
            <div className="receiptLine" />
            {items.length === 0 ? (
              <p className="emptyReceipt">Adicione produtos no caixa demo para montar o comprovante.</p>
            ) : (
              items.map((item) => (
                <div className="receiptProduct" key={item.code}>
                  <span>{item.name}</span>
                  <small>{item.qty},000 x {money(item.price)}</small>
                  <b>{money(item.price * item.qty)}</b>
                </div>
              ))
            )}
            <div className="receiptLine" />
            <div className="receiptTotals">
              <span>TOTAL</span><b>{money(total)}</b>
              <span>{method.toUpperCase()}</span><b>{money(receivedValue)}</b>
              <span>TROCO</span><b>{money(change)}</b>
            </div>
            {method === "Pix" && items.length > 0 && (
              <div className="receiptQr">
                <div className="miniQr large" aria-label="QR Pix demonstrativo">
                  {qrCells.map((cell, index) => (
                    <i className={cell ? "on" : undefined} key={index} />
                  ))}
                </div>
                <span>PIX {money(total)}</span>
              </div>
            )}
            <strong className="receiptThanks">OBRIGADO PELA PREFERENCIA</strong>
          </div>
        </section>
      </div>

      {modal === "payment" && (
        <div className="demoModalBackdrop" role="dialog" aria-modal="true" aria-label="Receber venda demo">
          <div className="demoModal paymentModal">
            <div className="demoModalHeader">
              <div>
                <span>PDV</span>
                <strong>Receber venda</strong>
              </div>
              <button type="button" onClick={() => setModal(null)}>X</button>
            </div>
            <div className="paymentDialogGrid">
              <section className="paymentDialogTotal">
                <span>Total da comanda</span>
                <strong>{money(total)}</strong>
                <small>Comanda 000012 | Garcom 2</small>
              </section>
              <section className="paymentDialogForm">
                <label>Forma de pagamento</label>
                <div className="paymentMethodGrid">
                  {paymentMethods.map((option) => (
                    <button
                      className={method === option ? "active" : undefined}
                      key={option}
                      type="button"
                      onClick={() => chooseMethod(option)}
                    >
                      {option}
                    </button>
                  ))}
                </div>
                <label htmlFor="payment-modal-received">Valor recebido</label>
                <input
                  id="payment-modal-received"
                  inputMode="decimal"
                  value={received}
                  onChange={(event) => setReceived(event.target.value)}
                />
                <div className="paymentDialogChange">
                  <span>Troco</span>
                  <b>{money(change)}</b>
                </div>
                {method === "Pix" && (
                  <div className="pixPreview">
                    <div className="miniQr" aria-label="QR Pix demonstrativo">
                      {qrCells.map((cell, index) => (
                        <i className={cell ? "on" : undefined} key={index} />
                      ))}
                    </div>
                    <div>
                      <strong>Pix pronto com valor</strong>
                      <span>{money(total)} para o cliente pagar.</span>
                    </div>
                  </div>
                )}
                <button className="finishButton" type="button" onClick={confirmPayment}>
                  Confirmar e ver comprovante
                </button>
              </section>
            </div>
          </div>
        </div>
      )}

      {modal === "receipt" && (
        <div className="demoModalBackdrop" role="dialog" aria-modal="true" aria-label="Comprovante demo">
          <div className="demoModal receiptDialog">
            <div className="demoModalHeader">
              <div>
                <span>PDV</span>
                <strong>Comprovante gerado</strong>
              </div>
              <button type="button" onClick={() => setModal(null)}>X</button>
            </div>
            <div className="receiptPaper">
              <h3>BALCAO LIVRE PDV</h3>
              <p>COMANDA 000012 | GARCOM 2</p>
              <p>NAO E DOCUMENTO FISCAL</p>
              <div className="receiptLine" />
              {items.map((item) => (
                <div className="receiptProduct" key={item.code}>
                  <span>{item.name}</span>
                  <small>{item.qty},000 x {money(item.price)}</small>
                  <b>{money(item.price * item.qty)}</b>
                </div>
              ))}
              <div className="receiptLine" />
              <div className="receiptTotals">
                <span>TOTAL</span><b>{money(total)}</b>
                <span>{method.toUpperCase()}</span><b>{money(receivedValue)}</b>
                <span>TROCO</span><b>{money(change)}</b>
              </div>
              {method === "Pix" && (
                <div className="receiptQr">
                  <div className="miniQr large" aria-label="QR Pix demonstrativo">
                    {qrCells.map((cell, index) => (
                      <i className={cell ? "on" : undefined} key={index} />
                    ))}
                  </div>
                  <span>PIX {money(total)}</span>
                </div>
              )}
              <strong className="receiptThanks">OBRIGADO PELA PREFERENCIA</strong>
            </div>
            <div className="receiptActions">
              <button type="button" onClick={() => setModal("payment")}>Voltar pagamento</button>
              <button className="finishButton" type="button" onClick={clearSale}>Nova venda</button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}
