from __future__ import annotations

import textwrap
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "outputs" / "free-video-balcao"
FRAMES = OUT / "frames"

W, H = 1080, 1920
BG = "#061B29"
NAVY = "#08283B"
BLUE = "#0B3A52"
CYAN = "#24D6C8"
WHITE = "#F7FBFF"
MUTED = "#B8CAD8"
GREEN = "#75D96B"
RED = "#F26D66"
AMBER = "#F1B44C"


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = [
        "C:/Windows/Fonts/segoeuib.ttf" if bold else "C:/Windows/Fonts/segoeui.ttf",
        "C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf",
    ]
    for candidate in candidates:
        path = Path(candidate)
        if path.exists():
            return ImageFont.truetype(str(path), size=size)
    return ImageFont.load_default()


F_TITLE = font(76, True)
F_H2 = font(46, True)
F_BODY = font(34, False)
F_BODY_BOLD = font(34, True)
F_SMALL = font(25, False)
F_SMALL_BOLD = font(25, True)


def rounded(draw: ImageDraw.ImageDraw, xy, radius, fill, outline=None, width=1):
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=outline, width=width)


def wrap(draw: ImageDraw.ImageDraw, text: str, fnt, max_width: int) -> list[str]:
    lines: list[str] = []
    for paragraph in text.split("\n"):
        words = paragraph.split()
        line = ""
        for word in words:
            trial = f"{line} {word}".strip()
            if draw.textbbox((0, 0), trial, font=fnt)[2] <= max_width:
                line = trial
            else:
                if line:
                    lines.append(line)
                line = word
        if line:
            lines.append(line)
    return lines


def text(draw: ImageDraw.ImageDraw, xy, value: str, fnt, fill=WHITE, max_width=None, line_gap=8):
    x, y = xy
    lines = wrap(draw, value, fnt, max_width) if max_width else value.split("\n")
    for line in lines:
        draw.text((x, y), line, font=fnt, fill=fill)
        y += draw.textbbox((0, 0), line, font=fnt)[3] + line_gap
    return y


def fit_image(path: Path, box: tuple[int, int, int, int], crop=False) -> Image.Image:
    source = Image.open(path).convert("RGB")
    bw, bh = box[2] - box[0], box[3] - box[1]
    if crop:
        ratio = max(bw / source.width, bh / source.height)
    else:
        ratio = min(bw / source.width, bh / source.height)
    resized = source.resize((max(1, int(source.width * ratio)), max(1, int(source.height * ratio))), Image.LANCZOS)
    canvas = Image.new("RGB", (bw, bh), "#EAF3F8")
    x = (bw - resized.width) // 2
    y = (bh - resized.height) // 2
    if crop:
        left = max(0, -x)
        top = max(0, -y)
        right = min(resized.width, left + bw)
        bottom = min(resized.height, top + bh)
        resized = resized.crop((left, top, right, bottom))
        x, y = 0, 0
    canvas.paste(resized, (x, y))
    return canvas


def paste_card(base: Image.Image, asset: Path, box, crop=False):
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    rounded(sd, (box[0] + 12, box[1] + 18, box[2] + 12, box[3] + 18), 34, (0, 0, 0, 92))
    shadow = shadow.filter(ImageFilter.GaussianBlur(16))
    base.alpha_composite(shadow)

    d = ImageDraw.Draw(layer)
    rounded(d, box, 34, "#F8FBFE", "#2F5D75", 3)
    img = fit_image(asset, (box[0] + 18, box[1] + 18, box[2] - 18, box[3] - 18), crop=crop)
    mask = Image.new("L", img.size, 0)
    md = ImageDraw.Draw(mask)
    md.rounded_rectangle((0, 0, img.width, img.height), radius=24, fill=255)
    layer.paste(img.convert("RGBA"), (box[0] + 18, box[1] + 18), mask)
    base.alpha_composite(layer)


def logo_header(base: Image.Image):
    d = ImageDraw.Draw(base)
    logo = ROOT / "BalcaoLivreLadingPage" / "public" / "brand" / "bl-modern-icon.png"
    if logo.exists():
        img = Image.open(logo).convert("RGBA").resize((74, 74), Image.LANCZOS)
        base.alpha_composite(img, (58, 56))
    d.text((150, 62), "Balcao Livre PDV", font=F_BODY_BOLD, fill=WHITE)
    d.text((150, 103), "caixa, delivery, mesas e cardapio", font=F_SMALL, fill=MUTED)


def pill(draw: ImageDraw.ImageDraw, xy, label: str, color: str):
    x, y = xy
    bbox = draw.textbbox((0, 0), label, font=F_SMALL_BOLD)
    w = bbox[2] - bbox[0] + 34
    rounded(draw, (x, y, x + w, y + 42), 20, color)
    draw.text((x + 17, y + 7), label, font=F_SMALL_BOLD, fill="#061B29" if color != BLUE else WHITE)


def scene(number: int, eyebrow: str, title: str, body: str, asset: Path, chips: list[tuple[str, str]], crop=False) -> Image.Image:
    img = Image.new("RGBA", (W, H), BG)
    d = ImageDraw.Draw(img)

    # subtle background grid
    for x in range(-300, W + 300, 100):
        d.line((x, 0, x + 540, H), fill="#092537", width=1)
    d.rectangle((0, 0, W, 190), fill="#04141F")
    logo_header(img)

    d.text((64, 230), eyebrow.upper(), font=F_SMALL_BOLD, fill=CYAN)
    y = text(d, (64, 280), title, F_TITLE, WHITE, max_width=930, line_gap=10)
    y = text(d, (64, y + 22), body, F_BODY, MUTED, max_width=920, line_gap=12)

    chip_y = max(y + 30, 620)
    chip_x = 64
    for label, color in chips:
        pill(d, (chip_x, chip_y), label, color)
        chip_x += d.textbbox((0, 0), label, font=F_SMALL_BOLD)[2] + 58

    paste_card(img, asset, (72, 790, 1008, 1668), crop=crop)

    rounded(d, (64, 1720, 1016, 1836), 32, CYAN)
    d.text((110, 1752), "Peca uma demonstracao no WhatsApp", font=F_H2, fill="#061B29")
    d.text((110, 1802), "balcaolivrepdv.com.br", font=F_SMALL_BOLD, fill="#061B29")
    d.text((64, 1870), f"CENA {number:02d}/06", font=F_SMALL_BOLD, fill="#5D8195")
    return img


def build():
    OUT.mkdir(parents=True, exist_ok=True)
    FRAMES.mkdir(parents=True, exist_ok=True)

    assets = {
        "caixa": ROOT / "BalcaoLivreLadingPage" / "public" / "brand" / "pdv-online-screen.png",
        "comandas": ROOT / "BalcaoLivreLadingPage" / "public" / "brand" / "pdv-command-screen.png",
        "mesas": ROOT / "BalcaoLivreLadingPage" / "public" / "guide" / "windows-pdv" / "01-comandas-mesas.png",
        "balcao": ROOT / "BalcaoLivreLadingPage" / "public" / "guide" / "windows-pdv" / "02-balcao-fichas.png",
        "delivery": ROOT / "BalcaoLivreLadingPage" / "public" / "guide" / "windows-pdv" / "03-delivery-pedidos.png",
        "digital": ROOT / "outputs" / "stories" / "story-02-pedidos-digitais.png",
    }

    scenes = [
        (
            "Caixa rapido",
            "Venda no caixa sem travar a operacao.",
            "O operador lanca produtos, recebe em dinheiro, Pix ou cartao e fecha a venda no Windows.",
            assets["caixa"],
            [("CAIXA", CYAN), ("PIX", GREEN), ("COMPROVANTE", AMBER)],
            False,
        ),
        (
            "iFood integrado",
            "Pedido chegou? O caixa ve na hora.",
            "O pedido entra no painel do delivery, toca alerta e a equipe acompanha preparo, rota e entrega.",
            assets["delivery"],
            [("IFOOD", CYAN), ("ALERTA", AMBER), ("DELIVERY", GREEN)],
            False,
        ),
        (
            "Garcom web",
            "O garcom abre a mesa pelo celular.",
            "Ele escolhe mesa, produtos e quantidade. O pedido aparece no PC do caixa em tempo real.",
            assets["mesas"],
            [("MESA", GREEN), ("CELULAR", CYAN), ("TEMPO REAL", AMBER)],
            False,
        ),
        (
            "Comandas e balcao",
            "Mesa, comanda e balcao no mesmo lugar.",
            "Controle consumo, transfira pedidos e mantenha a operacao organizada sem planilha.",
            assets["balcao"],
            [("COMANDA", CYAN), ("BALCAO", GREEN), ("ESTOQUE", AMBER)],
            False,
        ),
        (
            "Cardapio online",
            "Cliente pede pelo cardapio digital.",
            "Produtos, precos e disponibilidade saem do cadastro do PDV. A loja fica pronta para vender online.",
            assets["digital"],
            [("QR CODE", CYAN), ("ONLINE", GREEN), ("SEM RETRABALHO", AMBER)],
            True,
        ),
        (
            "Tudo conectado",
            "Menos bagunca. Mais controle.",
            "Caixa, iFood, garcom, mesas, estoque, Pix e comprovantes trabalhando juntos no Balcao Livre PDV.",
            assets["balcao"],
            [("PDV WINDOWS", CYAN), ("DELIVERY", GREEN), ("RESTAURANTE", AMBER)],
            False,
        ),
    ]

    frame_paths = []
    for idx, item in enumerate(scenes, start=1):
        img = scene(idx, *item)
        path = FRAMES / f"scene-{idx:02d}.png"
        img.convert("RGB").save(path, quality=95)
        frame_paths.append(path)

    frames = [Image.open(path).convert("P", palette=Image.Palette.ADAPTIVE) for path in frame_paths]
    gif_path = OUT / "balcao-livre-reel-preview.gif"
    frames[0].save(
        gif_path,
        save_all=True,
        append_images=frames[1:],
        duration=2800,
        loop=0,
        optimize=True,
    )

    storyboard = Image.new("RGB", (1080, 3240), "#EAF3F8")
    thumb_w, thumb_h = 500, 888
    d = ImageDraw.Draw(storyboard)
    d.text((48, 32), "Storyboard - Balcao Livre PDV", font=F_H2, fill="#071A2C")
    for idx, path in enumerate(frame_paths):
        x = 40 + (idx % 2) * 520
        y = 120 + (idx // 2) * 1010
        thumb = Image.open(path).convert("RGB").resize((thumb_w, thumb_h), Image.LANCZOS)
        storyboard.paste(thumb, (x, y))
        d.text((x, y + thumb_h + 14), f"Cena {idx + 1}", font=F_BODY_BOLD, fill="#0B3A52")
    storyboard.save(OUT / "storyboard-balcao-livre-reel.png", quality=95)

    script = """# Roteiro pronto - Reel Balcao Livre PDV

Formato: vertical 9:16, 35 a 45 segundos.
Estilo: demonstracao rapida com telas reais do app, cortes curtos e legenda grande.

## Falas para narracao

Cena 1 - Caixa rapido
\"Seu restaurante ainda vende no improviso? Com o Balcao Livre PDV, o caixa vende rapido no Windows, fecha a conta e emite comprovante sem depender de planilha.\"

Cena 2 - Pedido iFood
\"Chegou pedido no iFood? Ele aparece no painel do delivery, toca alerta e a equipe acompanha tudo: novo, preparo, saiu para entrega e entregue.\"

Cena 3 - Garcom na mesa
\"O garcom usa o celular na rede local, abre a mesa, escolhe os produtos e manda o pedido direto para o caixa.\"

Cena 4 - Comandas e balcao
\"Mesa, comanda, balcao e delivery ficam no mesmo sistema. Da para controlar consumo, estoque e operacao em tempo real.\"

Cena 5 - Cardapio online
\"E o cliente tambem pode pedir pelo cardapio online com QR Code. Os produtos saem do cadastro do PDV, com preco e disponibilidade atualizados.\"

Cena 6 - Chamada final
\"Balcao Livre PDV Online: caixa, iFood, garcom web, cardapio digital e controle da loja em um so lugar. Peca uma demonstracao no WhatsApp.\"

## Texto curto para legenda do Instagram

PDV Windows para restaurante, bar e delivery.
Caixa, iFood, garcom web, mesas, comandas, estoque, Pix e cardapio online no mesmo sistema.

Chame no WhatsApp e peca uma demonstracao.

## Texto na tela por cena

1. Venda no caixa sem travar
2. Pedido iFood aparece na hora
3. Garcom envia mesa pelo celular
4. Comandas, balcao e delivery juntos
5. Cardapio online com QR Code
6. Menos bagunca. Mais controle.
"""
    (OUT / "roteiro-e-falas-balcao-livre-reel.md").write_text(script, encoding="utf-8")

    prompts = """# Prompts gratis para gerar video IA

Use estes prompts no Runway Free, Pika, Kling, CapCut IA ou qualquer gerador gratis. Gere clipes de 4 a 5 segundos e monte no CapCut com as falas do roteiro.

## Prompt base de estilo

video comercial vertical 9:16 para software de restaurante, visual moderno e profissional, restaurante brasileiro movimentado, caixa usando computador Windows com PDV, garcom usando celular na mesa, pedido de delivery chegando, cortes rapidos, iluminacao realista, cores azul escuro e turquesa, estilo anuncio de tecnologia para pequenos restaurantes, sem texto falso na tela, sem logos inventados

## Cena 1 - Caixa

Operador de caixa em lanchonete usando um sistema PDV em computador Windows, tela com lista de produtos e total de venda, cliente pagando no balcao, camera aproximando lentamente, ambiente realista, profissional, vertical 9:16.

## Cena 2 - iFood

Computador no caixa recebe novo pedido de delivery, alerta visual na tela, equipe de restaurante preparando embalagem ao fundo, sensacao de pedido chegando em tempo real, camera com movimento leve, vertical 9:16.

## Cena 3 - Garcom web

Garcom em restaurante usando celular para abrir pedido de uma mesa, cliente sentado aguardando, interface moderna no celular, pedido sendo enviado para o caixa, visual realista, vertical 9:16.

## Cena 4 - Pedido chegando no PC

Tela do computador no caixa atualiza automaticamente com itens enviados pelo garcom, funcionario olha e confirma pedido, fluxo rapido e organizado, restaurante moderno, vertical 9:16.

## Cena 5 - Cardapio online

Cliente escaneia QR Code na mesa e ve cardapio digital no celular, escolhe lanche e bebida, ambiente de restaurante limpo e moderno, vertical 9:16.

## Cena 6 - Final

Montagem dinamica com caixa, garcom, delivery, cardapio online e estoque conectados, sensacao de sistema completo para restaurante, camera cinematografica, final com espaco para texto e chamada para WhatsApp, vertical 9:16.
"""
    (OUT / "prompts-video-ia-gratis.md").write_text(prompts, encoding="utf-8")

    print(f"Pacote criado em: {OUT}")
    print(f"GIF preview: {gif_path}")
    print(f"Storyboard: {OUT / 'storyboard-balcao-livre-reel.png'}")


if __name__ == "__main__":
    build()
