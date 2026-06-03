from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "outputs" / "ads-planos-balcao"
BRAND = ROOT / "BalcaoLivreLadingPage" / "public" / "brand"

NAVY = (7, 31, 51)
NAVY_2 = (9, 47, 70)
TEAL = (16, 132, 122)
TEAL_2 = (37, 192, 181)
BLUE = (45, 115, 185)
MUTED = (91, 110, 130)
PAPER = (247, 251, 253)
WHITE = (255, 255, 255)
LINE = (214, 226, 237)
GOLD = (180, 111, 8)
RED = (177, 27, 34)


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = [
        Path("C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf"),
        Path("C:/Windows/Fonts/calibrib.ttf" if bold else "C:/Windows/Fonts/calibri.ttf"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size=size)
    return ImageFont.load_default(size=size)


def text_size(draw: ImageDraw.ImageDraw, text: str, fnt: ImageFont.FreeTypeFont) -> tuple[int, int]:
    box = draw.textbbox((0, 0), text, font=fnt)
    return box[2] - box[0], box[3] - box[1]


def wrap_text(draw: ImageDraw.ImageDraw, text: str, fnt: ImageFont.FreeTypeFont, max_width: int) -> list[str]:
    lines: list[str] = []
    for raw_line in text.split("\n"):
        words = raw_line.split()
        if not words:
            lines.append("")
            continue
        line = words[0]
        for word in words[1:]:
            test = f"{line} {word}"
            if text_size(draw, test, fnt)[0] <= max_width:
                line = test
            else:
                lines.append(line)
                line = word
        lines.append(line)
    return lines


def draw_wrapped(
    draw: ImageDraw.ImageDraw,
    xy: tuple[int, int],
    text: str,
    fnt: ImageFont.FreeTypeFont,
    fill: tuple[int, int, int],
    max_width: int,
    line_gap: int = 8,
) -> int:
    x, y = xy
    for line in wrap_text(draw, text, fnt, max_width):
        draw.text((x, y), line, font=fnt, fill=fill)
        y += text_size(draw, line or "Ag", fnt)[1] + line_gap
    return y


def gradient(size: tuple[int, int], c1: tuple[int, int, int], c2: tuple[int, int, int]) -> Image.Image:
    w, h = size
    img = Image.new("RGB", size, c1)
    px = img.load()
    for y in range(h):
        for x in range(w):
            t = (x * 0.65 + y * 0.35) / (w * 0.65 + h * 0.35)
            px[x, y] = tuple(int(c1[i] * (1 - t) + c2[i] * t) for i in range(3))
    return img


def rounded_shadow(base: Image.Image, box: tuple[int, int, int, int], radius: int, alpha: int = 80) -> None:
    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    x1, y1, x2, y2 = box
    sd.rounded_rectangle((x1 + 8, y1 + 12, x2 + 8, y2 + 12), radius=radius, fill=(5, 18, 31, alpha))
    shadow = shadow.filter(ImageFilter.GaussianBlur(18))
    base.alpha_composite(shadow)


def add_brand_header(draw: ImageDraw.ImageDraw, w: int, y: int, dark: bool = True) -> None:
    color = WHITE if dark else NAVY
    draw.text((64, y), "Balcao Livre PDV", font=font(32, True), fill=color)
    draw.text((64, y + 44), "www.balcaolivrepdv.com.br", font=font(22, False), fill=TEAL_2 if dark else TEAL)


def load_screenshot(name: str, target_width: int) -> Image.Image:
    path = BRAND / name
    if not path.exists():
        path = ROOT / "outputs" / "balcao-landing-desktop-final6.png"
    img = Image.open(path).convert("RGB")
    ratio = target_width / img.width
    size = (target_width, max(1, int(img.height * ratio)))
    return img.resize(size, Image.Resampling.LANCZOS)


def paste_device(base: Image.Image, screenshot: Image.Image, box: tuple[int, int, int, int]) -> None:
    x1, y1, x2, y2 = box
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    rounded_shadow(layer, box, radius=28, alpha=90)
    draw.rounded_rectangle(box, radius=28, fill=(255, 255, 255, 255), outline=(210, 225, 238, 255), width=2)
    inset = 18
    crop_w = x2 - x1 - inset * 2
    crop_h = x2 - x1 - inset * 2 if x2 - x1 > y2 - y1 else y2 - y1 - inset * 2
    shot = screenshot.copy()
    if shot.height < y2 - y1:
        shot = shot.resize((crop_w, max(y2 - y1 - inset * 2, shot.height)), Image.Resampling.LANCZOS)
    crop = shot.crop((0, 0, min(crop_w, shot.width), min(y2 - y1 - inset * 2, shot.height)))
    mask = Image.new("L", crop.size, 0)
    md = ImageDraw.Draw(mask)
    md.rounded_rectangle((0, 0, crop.width, crop.height), radius=18, fill=255)
    layer.paste(crop.convert("RGBA"), (x1 + inset, y1 + inset), mask)
    base.alpha_composite(layer)


def draw_chip(draw: ImageDraw.ImageDraw, x: int, y: int, text: str, fill: tuple[int, int, int], fg: tuple[int, int, int] = WHITE) -> int:
    f = font(22, True)
    tw, th = text_size(draw, text, f)
    pad_x, pad_y = 18, 9
    draw.rounded_rectangle((x, y, x + tw + pad_x * 2, y + th + pad_y * 2), radius=18, fill=fill)
    draw.text((x + pad_x, y + pad_y - 1), text, font=f, fill=fg)
    return x + tw + pad_x * 2 + 12


def draw_feature_list(draw: ImageDraw.ImageDraw, x: int, y: int, features: list[str], max_width: int, color: tuple[int, int, int]) -> int:
    bullet_font = font(26, True)
    body_font = font(27)
    for item in features:
        draw.rounded_rectangle((x, y + 8, x + 18, y + 26), radius=9, fill=TEAL)
        y = draw_wrapped(draw, (x + 34, y), item, body_font, color, max_width - 34, line_gap=5) + 13
    return y


def add_price_block(draw: ImageDraw.ImageDraw, x: int, y: int, price: str, label: str, accent: tuple[int, int, int]) -> int:
    draw.text((x, y), label.upper(), font=font(22, True), fill=accent)
    y += 32
    price_font_size = 76 if len(price) <= 8 else 52
    draw.text((x, y), price, font=font(price_font_size, True), fill=NAVY)
    y += price_font_size + 14
    draw.text((x, y), "licenca de 1 ano", font=font(24, True), fill=MUTED)
    return y + 42


PLANS = [
    {
        "slug": "01-hibrido-offline-online",
        "title": "PDV hibrido para vender mesmo sem internet",
        "subtitle": "Caixa local + recursos online quando conectado.",
        "price": "R$ 260",
        "price_label": "Plano anual",
        "features": [
            "Balcao, mesas, comandas e delivery local",
            "Estoque, comprovante e fechamento de caixa",
            "Funciona offline e sincroniza quando a internet voltar",
        ],
        "tag": "HIBRIDO ONLINE/OFFLINE",
        "screenshot": "pdv-command-screen.png",
        "accent": TEAL,
    },
    {
        "slug": "02-online-garcom-web",
        "title": "Online com Garcom Web no celular",
        "subtitle": "O garcom lanca pedidos direto na mesa, sem instalar app.",
        "price": "R$ 450",
        "price_label": "Plano anual",
        "features": [
            "Garcom Web por link no celular",
            "Pedidos entram no PDV do caixa",
            "Ideal para bar, lanchonete e restaurante pequeno",
        ],
        "tag": "PDV ONLINE + GARCOM",
        "screenshot": "pdv-online-screen.png",
        "accent": BLUE,
    },
    {
        "slug": "03-mercado-pago-integrado",
        "title": "PDV Online com Mercado Pago integrado",
        "subtitle": "Receba com Pix QR, link de pagamento e Point quando disponivel.",
        "price": "R$ 650",
        "price_label": "Plano anual",
        "features": [
            "Garcom Web + PDV online",
            "Pix QR na tela e impressao do QR",
            "Venda fecha apos confirmacao do pagamento",
        ],
        "tag": "ONLINE + PAGAMENTO",
        "screenshot": "pdv-online-screen.png",
        "accent": TEAL,
    },
    {
        "slug": "04-completo-ifood-whatsapp",
        "title": "Completo: iFood, WhatsApp, cardapio e Mercado Pago",
        "subtitle": "Tudo em um sistema para atendimento, entrega e caixa.",
        "price": "R$ 1.200",
        "price_label": "Plano anual",
        "features": [
            "iFood integrado no Delivery do PDV",
            "WhatsApp com cardapio e pedidos por codigo",
            "Cardapio digital, Garcom Web e Mercado Pago",
        ],
        "tag": "PLANO COMPLETO",
        "screenshot": "pdv-online-screen.png",
        "accent": GOLD,
    },
    {
        "slug": "05-modulos-avulsos",
        "title": "Escolha so o modulo que sua loja precisa",
        "subtitle": "Licencas anuais para ativar recursos separados.",
        "price": "a partir de R$ 450",
        "price_label": "Modulos anuais",
        "features": [
            "Somente iFood: R$ 500/ano",
            "Somente WhatsApp: R$ 500/ano",
            "iFood + WhatsApp: R$ 700/ano",
            "Mercado Livre: R$ 450/ano",
        ],
        "tag": "MODULOS AVULSOS",
        "screenshot": "pdv-command-screen.png",
        "accent": RED,
    },
]


def make_story(plan: dict[str, object]) -> Image.Image:
    w, h = 1080, 1920
    bg = gradient((w, h), NAVY, (3, 72, 83)).convert("RGBA")
    draw = ImageDraw.Draw(bg)
    add_brand_header(draw, w, 72, dark=True)

    x = 64
    y = 190
    draw_chip(draw, x, y, str(plan["tag"]), plan["accent"])
    y += 95
    y = draw_wrapped(draw, (x, y), str(plan["title"]), font(68, True), WHITE, 900, line_gap=8)
    y += 20
    y = draw_wrapped(draw, (x, y), str(plan["subtitle"]), font(34), (203, 224, 235), 850, line_gap=8)

    shot = load_screenshot(str(plan["screenshot"]), 760)
    paste_device(bg, shot, (120, 760, 960, 1245))

    card = Image.new("RGBA", bg.size, (0, 0, 0, 0))
    cd = ImageDraw.Draw(card)
    rounded_shadow(card, (64, 1320, 1016, 1788), radius=36, alpha=110)
    cd.rounded_rectangle((64, 1320, 1016, 1788), radius=36, fill=WHITE, outline=LINE, width=2)
    bg.alpha_composite(card)
    draw = ImageDraw.Draw(bg)
    py = add_price_block(draw, 110, 1360, str(plan["price"]), str(plan["price_label"]), plan["accent"])
    draw_feature_list(draw, 110, py, list(plan["features"]), 850, NAVY)

    draw.rounded_rectangle((64, 1810, 1016, 1880), radius=24, fill=TEAL)
    draw.text((210, 1827), "Chame no WhatsApp e peca uma demonstracao", font=font(30, True), fill=WHITE)
    return bg.convert("RGB")


def make_feed(plan: dict[str, object]) -> Image.Image:
    w, h = 1080, 1080
    bg = gradient((w, h), PAPER, (229, 241, 248)).convert("RGBA")
    draw = ImageDraw.Draw(bg)
    draw.rounded_rectangle((40, 40, 1040, 1040), radius=38, fill=WHITE, outline=LINE, width=2)
    add_brand_header(draw, w, 76, dark=False)
    draw_chip(draw, 64, 170, str(plan["tag"]), plan["accent"])

    y = 235
    y = draw_wrapped(draw, (64, y), str(plan["title"]), font(48, True), NAVY, 560, line_gap=7)
    y += 15
    y = draw_wrapped(draw, (64, y), str(plan["subtitle"]), font(28), MUTED, 520, line_gap=7)
    y += 22
    y = add_price_block(draw, 64, y, str(plan["price"]), str(plan["price_label"]), plan["accent"])
    draw_feature_list(draw, 64, y + 5, list(plan["features"]), 510, NAVY)

    shot = load_screenshot(str(plan["screenshot"]), 520)
    paste_device(bg, shot, (590, 250, 1010, 600))
    draw = ImageDraw.Draw(bg)
    draw.rounded_rectangle((590, 660, 1010, 820), radius=24, fill=(232, 247, 244), outline=(151, 214, 205), width=2)
    draw.text((620, 690), "Vende no caixa", font=font(28, True), fill=NAVY)
    draw.text((620, 730), "Atende online", font=font(28, True), fill=NAVY)
    draw.text((620, 770), "Imprime comprovante", font=font(28, True), fill=NAVY)

    draw.rounded_rectangle((590, 880, 1010, 950), radius=22, fill=TEAL)
    draw.text((635, 898), "www.balcaolivrepdv.com.br", font=font(26, True), fill=WHITE)
    return bg.convert("RGB")


def make_contact_sheet(paths: list[Path], output: Path) -> None:
    thumbs = []
    for path in paths:
        img = Image.open(path).convert("RGB")
        img.thumbnail((280, 420), Image.Resampling.LANCZOS)
        thumbs.append((path.name, img.copy()))

    cols = 5
    rows = math.ceil(len(thumbs) / cols)
    sheet = Image.new("RGB", (cols * 320 + 40, rows * 470 + 70), (238, 246, 250))
    draw = ImageDraw.Draw(sheet)
    draw.text((24, 22), "Balcao Livre PDV - artes de planos", font=font(28, True), fill=NAVY)
    for index, (name, img) in enumerate(thumbs):
        col = index % cols
        row = index // cols
        x = 24 + col * 320
        y = 70 + row * 470
        sheet.paste(img, (x, y))
        draw_wrapped(draw, (x, y + img.height + 10), name, font(15, True), NAVY, 280, line_gap=3)
    sheet.save(output, quality=95)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    generated: list[Path] = []
    for plan in PLANS:
        story = make_story(plan)
        feed = make_feed(plan)
        story_path = OUT / f"{plan['slug']}-story.png"
        feed_path = OUT / f"{plan['slug']}-feed.png"
        story.save(story_path, quality=96)
        feed.save(feed_path, quality=96)
        generated.extend([story_path, feed_path])

    make_contact_sheet(generated, OUT / "00-preview-planos.png")
    print(f"Generated {len(generated)} images in {OUT}")
    print(OUT / "00-preview-planos.png")


if __name__ == "__main__":
    main()
