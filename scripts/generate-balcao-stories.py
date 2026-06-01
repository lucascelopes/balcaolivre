from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "outputs" / "stories"
OUT.mkdir(parents=True, exist_ok=True)

W, H = 1080, 1920

FONT_REGULAR = Path("C:/Windows/Fonts/segoeui.ttf")
FONT_SEMIBOLD = Path("C:/Windows/Fonts/seguisb.ttf")
FONT_BOLD = Path("C:/Windows/Fonts/segoeuib.ttf")


def font(size: int, weight: str = "regular") -> ImageFont.FreeTypeFont:
    path = {
        "bold": FONT_BOLD if FONT_BOLD.exists() else FONT_SEMIBOLD,
        "semibold": FONT_SEMIBOLD,
        "regular": FONT_REGULAR,
    }.get(weight, FONT_REGULAR)
    return ImageFont.truetype(str(path), size)


F = {
    "xs": font(26),
    "sm": font(32),
    "md": font(42),
    "lg": font(58, "semibold"),
    "xl": font(76, "bold"),
    "hero": font(92, "bold"),
    "hero2": font(82, "bold"),
}


COLORS = {
    "ink": "#0B1F33",
    "muted": "#617186",
    "line": "#D7E3EE",
    "blue": "#2F6FAE",
    "teal": "#0F766E",
    "teal2": "#12897F",
    "green": "#0F8A5F",
    "amber": "#B37209",
    "red": "#A11D1D",
    "soft": "#EEF5FA",
    "white": "#FFFFFF",
}


def hex_to_rgb(value: str) -> tuple[int, int, int]:
    value = value.lstrip("#")
    return tuple(int(value[i : i + 2], 16) for i in (0, 2, 4))


def gradient(top: str, bottom: str) -> Image.Image:
    img = Image.new("RGB", (W, H), top)
    draw = ImageDraw.Draw(img)
    t = hex_to_rgb(top)
    b = hex_to_rgb(bottom)
    for y in range(H):
        ratio = y / max(1, H - 1)
        c = tuple(round(t[i] * (1 - ratio) + b[i] * ratio) for i in range(3))
        draw.line((0, y, W, y), fill=c)
    return img


def shadow(base: Image.Image, box: tuple[int, int, int, int], radius: int = 28, alpha: int = 45) -> None:
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    x1, y1, x2, y2 = box
    d.rounded_rectangle((x1, y1, x2, y2), radius=radius, fill=(7, 31, 51, alpha))
    layer = layer.filter(ImageFilter.GaussianBlur(18))
    base.alpha_composite(layer)


def rounded_card(
    base: Image.Image,
    box: tuple[int, int, int, int],
    fill: str = "#FFFFFF",
    outline: str = "#D7E3EE",
    radius: int = 30,
    width: int = 2,
    shadowed: bool = True,
) -> None:
    if shadowed:
        shadow(base, box, radius)
    d = ImageDraw.Draw(base)
    d.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def text_size(draw: ImageDraw.ImageDraw, text: str, fnt: ImageFont.FreeTypeFont) -> tuple[int, int]:
    box = draw.textbbox((0, 0), text, font=fnt)
    return box[2] - box[0], box[3] - box[1]


def draw_text(
    draw: ImageDraw.ImageDraw,
    xy: tuple[int, int],
    text: str,
    fnt: ImageFont.FreeTypeFont,
    fill: str,
    max_width: int | None = None,
    line_gap: int = 10,
) -> int:
    x, y = xy
    if not max_width:
        draw.text((x, y), text, font=fnt, fill=fill)
        return y + text_size(draw, text, fnt)[1]

    words = text.split()
    lines: list[str] = []
    current = ""
    for word in words:
        candidate = f"{current} {word}".strip()
        if text_size(draw, candidate, fnt)[0] <= max_width or not current:
            current = candidate
        else:
            lines.append(current)
            current = word
    if current:
        lines.append(current)

    for line in lines:
        draw.text((x, y), line, font=fnt, fill=fill)
        y += text_size(draw, line, fnt)[1] + line_gap
    return y


def logo(draw: ImageDraw.ImageDraw, x: int, y: int, dark: bool = True) -> None:
    fill = COLORS["ink"] if dark else "#FFFFFF"
    accent = COLORS["teal"] if dark else "#CFE7E5"
    draw.text((x, y), "Balcão Livre PDV", font=font(38, "bold"), fill=fill)
    draw.rounded_rectangle((x, y + 58, x + 230, y + 64), radius=3, fill=accent)


def pill(draw: ImageDraw.ImageDraw, x: int, y: int, text: str, fill: str, color: str = "#FFFFFF") -> int:
    w, h = text_size(draw, text, F["xs"])
    draw.rounded_rectangle((x, y, x + w + 34, y + 46), radius=23, fill=fill)
    draw.text((x + 17, y + 8), text, font=F["xs"], fill=color)
    return x + w + 48


def paste_screenshot(base: Image.Image, path: Path, box: tuple[int, int, int, int], angle: float = -2.0) -> None:
    if not path.exists():
        return
    img = Image.open(path).convert("RGBA")
    x1, y1, x2, y2 = box
    target_w = x2 - x1
    target_h = y2 - y1
    img.thumbnail((target_w, target_h), Image.Resampling.LANCZOS)
    frame = Image.new("RGBA", (target_w, target_h), (255, 255, 255, 0))
    px = (target_w - img.width) // 2
    py = (target_h - img.height) // 2
    rounded_card(frame, (0, 0, target_w - 1, target_h - 1), radius=30, shadowed=False)
    frame.alpha_composite(img, (px, py))
    frame = frame.rotate(angle, resample=Image.Resampling.BICUBIC, expand=True)
    base.alpha_composite(frame, (x1 - (frame.width - target_w) // 2, y1 - (frame.height - target_h) // 2))


def device_frame(base: Image.Image, x: int, y: int, w: int, h: int, title: str, rows: list[tuple[str, str, str]]) -> None:
    rounded_card(base, (x, y, x + w, y + h), fill="#FFFFFF", outline="#C9D8E5", radius=36)
    d = ImageDraw.Draw(base)
    d.rounded_rectangle((x + 34, y + 34, x + 124, y + 72), radius=18, fill="#EAF3FB")
    d.text((x + 52, y + 40), "PDV", font=font(22, "bold"), fill=COLORS["blue"])
    d.text((x + 150, y + 38), title, font=font(34, "bold"), fill=COLORS["ink"])
    d.line((x + 34, y + 98, x + w - 34, y + 98), fill="#D8E2EC", width=2)
    top = y + 140
    for i, (left, middle, tag) in enumerate(rows):
        ry = top + i * 82
        fill = "#F7FAFD" if i % 2 == 0 else "#FFFFFF"
        d.rounded_rectangle((x + 34, ry, x + w - 34, ry + 62), radius=14, fill=fill)
        d.text((x + 54, ry + 13), left, font=font(28, "semibold"), fill=COLORS["ink"])
        d.text((x + 250, ry + 16), middle, font=font(24), fill=COLORS["muted"])
        tw, _ = text_size(d, tag, font(20, "bold"))
        d.rounded_rectangle((x + w - 64 - tw, ry + 15, x + w - 42, ry + 47), radius=16, fill="#E8F7F4")
        d.text((x + w - 52 - tw, ry + 19), tag, font=font(20, "bold"), fill=COLORS["teal"])


def qr_like(draw: ImageDraw.ImageDraw, x: int, y: int, size: int) -> None:
    cell = size // 17
    draw.rounded_rectangle((x - 18, y - 18, x + size + 18, y + size + 18), radius=24, fill="#FFFFFF")
    pattern = [
        "11111110010111111",
        "10000010100100001",
        "10111010111101101",
        "10111010001101101",
        "10111011110101101",
        "10000010101000001",
        "11111110101011111",
        "00000000100000000",
        "11010111101101011",
        "00101100110011100",
        "11100111101010111",
        "10010001011100100",
        "10111110101111101",
        "10100100011000101",
        "10101111101110101",
        "10000010110000101",
        "11111110101111111",
    ]
    for row, bits in enumerate(pattern):
        for col, bit in enumerate(bits):
            if bit == "1":
                draw.rectangle((x + col * cell, y + row * cell, x + (col + 1) * cell - 2, y + (row + 1) * cell - 2), fill="#0B1F33")


def story_01() -> Path:
    img = gradient("#F3F8FC", "#DDEBF4").convert("RGBA")
    d = ImageDraw.Draw(img)
    logo(d, 74, 74)
    d.rounded_rectangle((74, 172, 368, 225), radius=26, fill="#FFFFFF", outline="#CFE0EE", width=2)
    d.text((104, 184), "PDV ONLINE", font=font(26, "bold"), fill=COLORS["blue"])
    y = draw_text(d, (74, 272), "Um PDV completo para restaurante operar sem bagunça.", F["hero2"], COLORS["ink"], max_width=900, line_gap=12)
    draw_text(d, (78, y + 22), "Balcão, mesas, delivery, estoque, caixa e comprovante em uma tela de trabalho.", F["md"], COLORS["muted"], max_width=860, line_gap=10)
    device_frame(
        img,
        92,
        705,
        896,
        630,
        "Pedidos Delivery",
        [
            ("I3950", "IFOOD ACEITO", "NOVO"),
            ("MESA 07", "R$ 105,00", "ABERTA"),
            ("BALCAO", "PIX MERCADO PAGO", "PAGO"),
            ("I7015", "CANCELADO", "OK"),
            ("ESTOQUE", "26 produtos", "ATIVO"),
        ],
    )
    x = 74
    for item, color in [("Windows", COLORS["blue"]), ("iFood", COLORS["red"]), ("WhatsApp", COLORS["teal"]), ("Mercado Pago", COLORS["amber"])]:
        x = pill(d, x, 1440, item, color)
    draw_text(d, (74, 1535), "BalcaoLivrePDV.com.br", F["lg"], COLORS["ink"])
    draw_text(d, (76, 1601), "Licença anual para PDV Online", F["sm"], COLORS["muted"])
    path = OUT / "story-01-pdv-completo.png"
    img.convert("RGB").save(path, quality=95)
    return path


def story_02() -> Path:
    img = gradient("#092A3A", "#0F766E").convert("RGBA")
    d = ImageDraw.Draw(img)
    logo(d, 74, 74, dark=False)
    d.rounded_rectangle((74, 172, 465, 226), radius=27, fill="#FFFFFF", outline=(255, 255, 255, 70), width=2)
    d.text((105, 184), "PEDIDO DIGITAL", font=font(26, "bold"), fill=COLORS["teal"])
    y = draw_text(d, (74, 272), "O pedido chega e o PDV organiza tudo.", F["hero2"], "#FFFFFF", max_width=900, line_gap=10)
    draw_text(d, (78, y + 20), "iFood, cardápio digital, WhatsApp e garçom web entram no fluxo do caixa.", F["md"], "#CFE7E5", max_width=860, line_gap=8)

    rounded_card(img, (92, 705, 988, 1273), fill="#FFFFFF", outline="#D8E2EC", radius=36)
    for i, (channel, desc, color) in enumerate(
        [
            ("iFood", "pedido aceito em tempo real", COLORS["red"]),
            ("WhatsApp", "cardápio por código e confirmação", COLORS["teal"]),
            ("Garcom web", "mesa direto no celular", COLORS["blue"]),
            ("Cardapio digital", "pedido sem instalar app", COLORS["amber"]),
        ]
    ):
        top = 755 + i * 118
        d.rounded_rectangle((140, top, 224, top + 84), radius=22, fill=color)
        d.text((165, top + 22), channel[:2].upper(), font=font(28, "bold"), fill="#FFFFFF")
        d.text((252, top + 8), channel, font=font(36, "bold"), fill=COLORS["ink"])
        d.text((252, top + 53), desc, font=font(28), fill=COLORS["muted"])

    d.line((540, 1294, 540, 1408), fill="#CFE7E5", width=4)
    d.polygon([(540, 1424), (516, 1384), (564, 1384)], fill="#CFE7E5")
    draw_text(d, (174, 1460), "Cliente confirma. Pedido entra no PDV.", F["lg"], "#FFFFFF", max_width=760, line_gap=8)
    path = OUT / "story-02-pedidos-digitais.png"
    img.convert("RGB").save(path, quality=95)
    return path


def story_03() -> Path:
    img = gradient("#F8FBFD", "#E7F0F7").convert("RGBA")
    d = ImageDraw.Draw(img)
    logo(d, 74, 74)
    d.rounded_rectangle((74, 172, 490, 226), radius=27, fill="#FFFFFF", outline="#CFE0EE", width=2)
    d.text((105, 184), "PAGAMENTO", font=font(26, "bold"), fill=COLORS["teal"])
    y = draw_text(d, (74, 272), "Pix e Mercado Pago com comprovante no ponto certo.", F["hero2"], COLORS["ink"], max_width=900, line_gap=10)
    draw_text(d, (78, y + 20), "QR na tela, QR gigante na impressora e fechamento só depois da confirmação.", F["md"], COLORS["muted"], max_width=860, line_gap=8)

    rounded_card(img, (118, 690, 962, 1325), fill="#FFFFFF", outline="#D8E2EC", radius=46)
    d.text((180, 755), "MERCADO PAGO PIX", font=font(38, "bold"), fill=COLORS["ink"])
    d.text((180, 805), "Aguardando pagamento de R$ 58,00", font=font(29), fill=COLORS["muted"])
    qr_like(d, 330, 917, 340)
    d.rounded_rectangle((690, 939, 868, 1009), radius=28, fill="#E8F7F4")
    d.text((727, 957), "PIX", font=font(32, "bold"), fill=COLORS["teal"])
    d.rounded_rectangle((690, 1035, 890, 1105), radius=28, fill="#EAF3FB")
    d.text((720, 1053), "POINT", font=font(32, "bold"), fill=COLORS["blue"])
    d.rounded_rectangle((690, 1131, 904, 1201), radius=28, fill="#FFF4D7")
    d.text((722, 1149), "RECIBO", font=font(32, "bold"), fill=COLORS["amber"])

    draw_text(d, (74, 1448), "Menos copia e cola. Mais venda fechada.", F["lg"], COLORS["ink"], max_width=880)
    path = OUT / "story-03-mercado-pago-pix.png"
    img.convert("RGB").save(path, quality=95)
    return path


def story_04() -> Path:
    img = gradient("#0B1F33", "#245B91").convert("RGBA")
    d = ImageDraw.Draw(img)
    logo(d, 74, 74, dark=False)
    d.rounded_rectangle((74, 172, 430, 226), radius=27, fill="#FFFFFF", outline=(255, 255, 255, 70), width=2)
    d.text((105, 184), "GESTÃO", font=font(26, "bold"), fill=COLORS["teal"])
    y = draw_text(d, (74, 272), "Controle que aparece no dia a dia.", F["hero2"], "#FFFFFF", max_width=900, line_gap=10)
    draw_text(d, (78, y + 20), "Estoque com busca, alertas, backup versionado e permissões por ação.", F["md"], "#D5E5F2", max_width=860, line_gap=8)

    metrics = [
        ("Produtos", "26", "itens cadastrados", COLORS["blue"]),
        ("Criticos", "2", "abaixo do minimo", COLORS["red"]),
        ("Unidades", "783", "saldo fisico", COLORS["amber"]),
        ("Backup", "ON", "nuvem versionada", COLORS["teal"]),
    ]
    top = 690
    for i, (title, value, detail, color) in enumerate(metrics):
        x = 92 + (i % 2) * 456
        y = top + (i // 2) * 270
        rounded_card(img, (x, y, x + 400, y + 220), fill="#FFFFFF", outline="#D8E2EC", radius=32)
        d.rounded_rectangle((x, y, x + 10, y + 220), radius=5, fill=color)
        d.text((x + 38, y + 38), title, font=font(30, "semibold"), fill=COLORS["muted"])
        d.text((x + 38, y + 82), value, font=font(66, "bold"), fill=COLORS["ink"])
        d.text((x + 38, y + 160), detail, font=font(25), fill=COLORS["muted"])

    rounded_card(img, (92, 1310, 988, 1510), fill=(255, 255, 255, 240), outline="#D8E2EC", radius=34)
    d.text((142, 1363), "Gerente controla. Operador trabalha.", font=font(43, "bold"), fill=COLORS["ink"])
    d.text((142, 1423), "PIN, permissões e histórico para loja real.", font=font(30), fill=COLORS["muted"])
    path = OUT / "story-04-estoque-backup.png"
    img.convert("RGB").save(path, quality=95)
    return path


def story_05() -> Path:
    img = gradient("#F7FAFD", "#DDEBF4").convert("RGBA")
    d = ImageDraw.Draw(img)
    logo(d, 74, 74)
    d.rounded_rectangle((74, 172, 395, 226), radius=27, fill="#FFFFFF", outline="#CFE0EE", width=2)
    d.text((105, 184), "CONTRATE", font=font(26, "bold"), fill=COLORS["teal"])
    draw_text(d, (74, 285), "Seu restaurante com PDV Online.", F["hero2"], COLORS["ink"], max_width=900, line_gap=12)
    draw_text(d, (78, 500), "Windows + nuvem + integrações para vender sem depender de planilha.", F["md"], COLORS["muted"], max_width=860, line_gap=8)

    rounded_card(img, (92, 710, 988, 1195), fill="#FFFFFF", outline="#D8E2EC", radius=42)
    rows = [
        ("Offline 100%", "R$ 260 / ano"),
        ("Online + garcom web", "R$ 450 / ano"),
        ("Mercado Pago integrado", "R$ 650 / ano"),
        ("iFood + WhatsApp + cardapio", "R$ 1.200 / ano"),
    ]
    for i, (name, price) in enumerate(rows):
        y = 768 + i * 96
        d.text((150, y), name, font=font(32, "semibold"), fill=COLORS["ink"])
        d.text((640, y), price, font=font(30, "bold"), fill=COLORS["teal"])
        if i < len(rows) - 1:
            d.line((150, y + 62, 930, y + 62), fill="#E3EBF2", width=2)

    d.rounded_rectangle((118, 1340, 962, 1445), radius=42, fill=COLORS["teal"])
    d.text((248, 1365), "balcaolivrepdv.com.br", font=font(46, "bold"), fill="#FFFFFF")
    draw_text(d, (118, 1512), "Stories prontos para divulgar no Instagram e WhatsApp.", F["sm"], COLORS["muted"], max_width=844)
    path = OUT / "story-05-planos-cta.png"
    img.convert("RGB").save(path, quality=95)
    return path


def main() -> None:
    paths = [story_01(), story_02(), story_03(), story_04(), story_05()]
    print("\n".join(str(path) for path in paths))


if __name__ == "__main__":
    main()
