from __future__ import annotations

from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "outputs" / "instagram-mercado"
OUT.mkdir(parents=True, exist_ok=True)

for old_file in OUT.glob("*"):
    if old_file.is_file():
        old_file.unlink()

W, H = 1080, 1920

NAVY = "#071A2C"
INK = "#0C1B2A"
BLUE = "#155C91"
TEAL = "#0F8377"
TEAL_DARK = "#09665E"
MINT = "#DFF5F1"
CYAN = "#8CE1D8"
ICE = "#F3F8FB"
LINE = "#C9D9E6"
WHITE = "#FFFFFF"
MUTED = "#5D7187"
GREEN = "#2FA66A"
RED = "#D74848"

FONT_DIR = Path("C:/Windows/Fonts")
FONT_REG = FONT_DIR / "segoeui.ttf"
FONT_BOLD = FONT_DIR / "segoeuib.ttf"
FONT_BLACK = FONT_DIR / "arialbd.ttf"
FONT_NUM = FONT_DIR / "bahnschrift.ttf"

ICON = ROOT / "BalcaoLivreLadingPage" / "public" / "balcao-livre-icon.png"


def font(path: Path, size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(path), size=size)


def text_size(draw: ImageDraw.ImageDraw, text: str, fnt: ImageFont.FreeTypeFont) -> tuple[int, int]:
    box = draw.textbbox((0, 0), text, font=fnt)
    return box[2] - box[0], box[3] - box[1]


def wrap(draw: ImageDraw.ImageDraw, text: str, fnt: ImageFont.FreeTypeFont, max_w: int) -> list[str]:
    lines: list[str] = []
    for paragraph in text.split("\n"):
        words = paragraph.split()
        line = ""
        for word in words:
            test = f"{line} {word}".strip()
            if text_size(draw, test, fnt)[0] <= max_w or not line:
                line = test
            else:
                lines.append(line)
                line = word
        if line:
            lines.append(line)
    return lines


def text_block(
    draw: ImageDraw.ImageDraw,
    x: int,
    y: int,
    text: str,
    fnt: ImageFont.FreeTypeFont,
    fill: str,
    max_w: int,
    gap: int = 10,
) -> int:
    for line in wrap(draw, text, fnt, max_w):
        draw.text((x, y), line, font=fnt, fill=fill)
        y += text_size(draw, line, fnt)[1] + gap
    return y


def gradient(top: str = "#F4F8FB", bottom: str = "#DCEEF2") -> Image.Image:
    img = Image.new("RGBA", (W, H), top)
    draw = ImageDraw.Draw(img)
    a = tuple(int(top[i : i + 2], 16) for i in (1, 3, 5))
    b = tuple(int(bottom[i : i + 2], 16) for i in (1, 3, 5))
    for y in range(H):
        t = y / (H - 1)
        mix = min(1, t * 0.92)
        color = tuple(int(a[i] * (1 - mix) + b[i] * mix) for i in range(3))
        draw.line((0, y, W, y), fill=color + (255,))
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse((600, -220, 1280, 520), fill=(255, 255, 255, 26))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(28)))
    return img


def dark_gradient() -> Image.Image:
    return gradient("#071A2C", "#0C625E")


def shadow_card(
    base: Image.Image,
    xy: tuple[int, int, int, int],
    radius: int = 26,
    fill: str = WHITE,
    outline: str = LINE,
    shadow_alpha: int = 55,
) -> None:
    x1, y1, x2, y2 = xy
    shadow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    sd.rounded_rectangle((x1 + 10, y1 + 14, x2 + 10, y2 + 14), radius, fill=(5, 32, 56, shadow_alpha))
    base.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(18)))
    draw = ImageDraw.Draw(base)
    draw.rounded_rectangle(xy, radius, fill=fill, outline=outline, width=2)


def brand(base: Image.Image, y: int = 58, dark: bool = True) -> None:
    draw = ImageDraw.Draw(base)
    icon = Image.open(ICON).convert("RGBA").resize((82, 82), Image.Resampling.LANCZOS)
    base.alpha_composite(icon, (66, y - 12))
    primary = WHITE if dark else INK
    secondary = CYAN if dark else TEAL
    draw.text((164, y), "Balcao Livre PDV", font=font(FONT_BOLD, 38), fill=primary)
    draw.text((164, y + 42), "caixa simples para restaurante", font=font(FONT_REG, 23), fill=secondary)


def pill(draw: ImageDraw.ImageDraw, x: int, y: int, text: str, fill: str, color: str) -> None:
    fnt = font(FONT_BOLD, 23)
    tw, th = text_size(draw, text, fnt)
    draw.rounded_rectangle((x, y, x + tw + 44, y + 46), 23, fill=fill)
    draw.text((x + 22, y + 10), text, font=fnt, fill=color)


def ui_panel(base: Image.Image, x: int, y: int, w: int, h: int) -> None:
    draw = ImageDraw.Draw(base)
    shadow_card(base, (x, y, x + w, y + h), 28, "#F9FCFE", "#BFD0DF", 48)
    draw.rounded_rectangle((x, y, x + w, y + 78), 28, fill=NAVY)
    draw.text((x + 32, y + 21), "PDV em operacao", font=font(FONT_BOLD, 28), fill=WHITE)
    draw.rounded_rectangle((x + w - 242, y + 18, x + w - 32, y + 58), 14, fill=MINT)
    draw.text((x + w - 216, y + 24), "Caixa aberto", font=font(FONT_BOLD, 20), fill=TEAL)

    tab_y = y + 112
    for i, name in enumerate(["Mesas", "Balcao", "Delivery"]):
        bx = x + 34 + i * 180
        active = i == 0
        draw.rounded_rectangle((bx, tab_y, bx + 150, tab_y + 62), 12, fill=TEAL if active else "#EAF1F6")
        draw.text((bx + 34, tab_y + 16), name, font=font(FONT_BOLD, 20), fill=WHITE if active else BLUE)

    grid_y = tab_y + 95
    products = ["X-Burger", "Batata", "Suco", "Coxinha"]
    for i, name in enumerate(products):
        px = x + 34 + (i % 2) * 255
        py = grid_y + (i // 2) * 112
        draw.rounded_rectangle((px, py, px + 225, py + 84), 12, fill=WHITE, outline="#CAD8E6", width=2)
        draw.text((px + 18, py + 16), name, font=font(FONT_BOLD, 22), fill=INK)
        draw.text((px + 18, py + 50), "produto ativo", font=font(FONT_BOLD, 18), fill=TEAL)

    total_x = x + w - 285
    draw.rounded_rectangle((total_x, grid_y, x + w - 34, grid_y + 196), 16, fill="#EEF8F6", outline="#B9D6D2", width=2)
    draw.text((total_x + 24, grid_y + 24), "Resumo", font=font(FONT_BOLD, 24), fill=BLUE)
    draw.text((total_x + 24, grid_y + 70), "Mesa ativa", font=font(FONT_BLACK, 36), fill=INK)
    draw.rounded_rectangle((total_x + 24, grid_y + 134, x + w - 58, grid_y + 176), 12, fill=TEAL)
    draw.text((total_x + 58, grid_y + 140), "Finalizar", font=font(FONT_BOLD, 22), fill=WHITE)


def phone_panel(base: Image.Image, x: int, y: int, w: int, h: int) -> None:
    draw = ImageDraw.Draw(base)
    shadow_card(base, (x, y, x + w, y + h), 38, "#F8FBFD", "#AEC7DA", 58)
    draw.rounded_rectangle((x + 34, y + 34, x + w - 34, y + h - 34), 28, fill=WHITE, outline="#D3E0EA", width=2)
    draw.rounded_rectangle((x + 72, y + 70, x + w - 72, y + 210), 24, fill="#EAF5F4")
    draw.text((x + 100, y + 98), "Garcom mobile", font=font(FONT_BLACK, 28), fill=INK)
    draw.text((x + 100, y + 142), "mesa, produtos e conta", font=font(FONT_BOLD, 21), fill=TEAL)
    for i, label in enumerate(["Mesa 03", "Produtos", "Observacao", "Enviar pedido"]):
        yy = y + 260 + i * 82
        fill = TEAL if i == 3 else "#F3F7FA"
        color = WHITE if i == 3 else INK
        draw.rounded_rectangle((x + 72, yy, x + w - 72, yy + 58), 16, fill=fill, outline="#D2DFEA")
        draw.text((x + 104, yy + 14), label, font=font(FONT_BOLD, 22), fill=color)


def benefit_grid(draw: ImageDraw.ImageDraw, x: int, y: int, items: list[str], dark: bool = True) -> None:
    text_color = WHITE if dark else INK
    marker = CYAN if dark else TEAL
    for i, item in enumerate(items):
        col = i % 2
        row = i // 2
        cx = x + col * 452
        cy = y + row * 64
        draw.rounded_rectangle((cx, cy + 4, cx + 34, cy + 38), 10, fill=marker)
        draw.text((cx + 10, cy + 4), "OK", font=font(FONT_BOLD, 12), fill=NAVY if dark else WHITE)
        draw.text((cx + 50, cy), item, font=font(FONT_BOLD, 26), fill=text_color)


def whatsapp_box(base: Image.Image, x: int, y: int, title: str = "Consulte no WhatsApp") -> None:
    draw = ImageDraw.Draw(base)
    shadow_card(base, (x, y, x + 874, y + 316), 28, "#F7FBFF", "#C2D4E2", 52)
    draw.text((x + 38, y + 34), "ATENDIMENTO COMERCIAL", font=font(FONT_BOLD, 25), fill=TEAL)
    draw.text((x + 38, y + 86), title, font=font(FONT_BLACK, 58), fill=INK)
    draw.text((x + 38, y + 166), "Plano indicado conforme sua operacao.", font=font(FONT_BOLD, 30), fill=BLUE)
    draw.rounded_rectangle((x + 38, y + 232, x + 410, y + 280), 14, fill=MINT)
    draw.text((x + 62, y + 241), "Wender (27) 98126-7551", font=font(FONT_BOLD, 23), fill=TEAL_DARK)
    draw.rounded_rectangle((x + 430, y + 232, x + 836, y + 280), 14, fill="#EAF1F6")
    draw.text((x + 454, y + 241), "Lucas (33) 99960-9457", font=font(FONT_BOLD, 23), fill=BLUE)


def seller_strip(base: Image.Image, x: int, y: int) -> None:
    draw = ImageDraw.Draw(base)
    shadow_card(base, (x, y, x + 940, y + 112), 24, "#F7FBFF", "#C2D4E2", 42)
    draw.text((x + 34, y + 24), "Atendimento pelo WhatsApp", font=font(FONT_BLACK, 28), fill=INK)
    draw.text((x + 34, y + 66), "Wender (27) 98126-7551  |  Lucas (33) 99960-9457", font=font(FONT_BOLD, 22), fill=TEAL)


def cta(draw: ImageDraw.ImageDraw, text: str = "CHAMAR NO WHATSAPP") -> None:
    draw.rounded_rectangle((70, 1722, 1010, 1812), 23, fill=TEAL)
    tw, _ = text_size(draw, text, font(FONT_BLACK, 36))
    draw.text(((W - tw) // 2, 1748), text, font=font(FONT_BLACK, 36), fill=WHITE)
    draw.text((70, 1842), "balcaolivrepdv.com.br", font=font(FONT_BOLD, 28), fill=CYAN)
    draw.text((678, 1842), "PDV online e offline", font=font(FONT_REG, 25), fill="#D8E8EF")


def save(img: Image.Image, name: str) -> None:
    img.convert("RGB").save(OUT / name, quality=96)


def post_01() -> None:
    img = dark_gradient()
    draw = ImageDraw.Draw(img)
    brand(img)
    pill(draw, 70, 190, "PDV PARA RESTAURANTE", "#0D2B48", CYAN)
    text_block(draw, 70, 298, "CAIXA, MESAS, DELIVERY E CARDAPIO EM UM SO SISTEMA", font(FONT_BLACK, 68), WHITE, 940, 7)
    draw.text((74, 568), "Balcao Livre organiza o atendimento do comeco ao fechamento.", font=font(FONT_BOLD, 31), fill=CYAN)
    ui_panel(img, 70, 650, 940, 510)
    whatsapp_box(img, 103, 1210)
    benefit_grid(draw, 95, 1560, ["PDV offline", "PDV web", "Garcom mobile", "Cardapio digital"], True)
    cta(draw, "CONSULTAR NO WHATSAPP")
    save(img, "01_pdv_restaurante_whatsapp_story.png")


def post_02() -> None:
    img = gradient("#F5FAFD", "#DFEEF3")
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle((0, 0, W, 270), 0, fill=NAVY)
    brand(img)
    text_block(draw, 70, 330, "CANSOU DE USAR VARIOS SISTEMAS?", font(FONT_BLACK, 72), INK, 920, 6)
    draw.text((72, 565), "Atendimento, caixa, pedidos e controle no mesmo fluxo.", font=font(FONT_BOLD, 31), fill=TEAL)
    shadow_card(img, (70, 665, 1010, 990), 28, WHITE, LINE, 45)
    benefit_grid(draw, 105, 720, ["PDV", "Estoque", "Mesas", "iFood", "WhatsApp", "Mercado Pago"], False)
    draw.rounded_rectangle((150, 1038, 930, 1118), 24, fill=NAVY)
    draw.text((221, 1055), "MENOS IMPROVISO. MAIS CONTROLE.", font=font(FONT_BLACK, 32), fill=WHITE)
    ui_panel(img, 110, 1190, 860, 360)
    seller_strip(img, 70, 1568)
    cta(draw, "PEDIR DEMONSTRACAO")
    save(img, "02_tudo_em_um_whatsapp_story.png")


def post_03() -> None:
    img = dark_gradient()
    draw = ImageDraw.Draw(img)
    brand(img)
    pill(draw, 70, 190, "INTEGRACOES", "#0D2B48", CYAN)
    text_block(draw, 70, 300, "IFOOD E WHATSAPP ENTRANDO NA ROTINA DO PDV", font(FONT_BLACK, 68), WHITE, 930, 7)
    draw.text((74, 555), "Pedidos conectados ao atendimento, producao e fechamento.", font=font(FONT_BOLD, 31), fill=CYAN)
    shadow_card(img, (70, 650, 1010, 1080), 30, "#F7FBFF", "#C2D4E2", 55)
    cards = [
        ("iFood", "Pedido online no fluxo"),
        ("WhatsApp", "Atendimento comercial"),
        ("Cardapio", "Pedido direto pelo celular"),
        ("Cozinha", "Setor e impressao"),
    ]
    for i, (title, desc) in enumerate(cards):
        x = 110 + (i % 2) * 455
        y = 705 + (i // 2) * 162
        draw.rounded_rectangle((x, y, x + 390, y + 120), 20, fill="#ECF5F6", outline="#D0DFE8", width=2)
        draw.text((x + 28, y + 26), title, font=font(FONT_BLACK, 33), fill=INK)
        draw.text((x + 28, y + 72), desc, font=font(FONT_BOLD, 22), fill=TEAL)
    whatsapp_box(img, 103, 1165, "Consultar integracoes")
    benefit_grid(draw, 95, 1510, ["iFood no plano pago", "WhatsApp no plano pago", "Mercado Pago integrado", "Automacoes sob consulta"], True)
    cta(draw, "FALAR COM VENDEDOR")
    save(img, "03_ifood_whatsapp_integrado_story.png")


def post_04() -> None:
    img = gradient("#F5FAFD", "#DDEFF0")
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle((0, 0, W, 270), 0, fill=NAVY)
    brand(img)
    text_block(draw, 70, 330, "MERCADO PAGO INTEGRADO AO PDV", font(FONT_BLACK, 72), INK, 930, 6)
    draw.text((72, 565), "Pix, cartao e controle de pagamento com menos retrabalho.", font=font(FONT_BOLD, 31), fill=TEAL)
    shadow_card(img, (70, 670, 1010, 1165), 30, WHITE, LINE, 45)
    for i, (title, desc) in enumerate(
        [
            ("Pix", "Recebimento organizado"),
            ("Cartao", "Fluxo conectado ao caixa"),
            ("Comprovante", "Venda conferida na tela"),
            ("Relatorio", "Resumo simples do dia"),
        ]
    ):
        y = 730 + i * 96
        draw.rounded_rectangle((115, y, 205, y + 64), 18, fill=MINT)
        draw.text((237, y + 2), title, font=font(FONT_BLACK, 31), fill=INK)
        draw.text((237, y + 42), desc, font=font(FONT_BOLD, 22), fill=MUTED)
        draw.rounded_rectangle((850, y + 13, 930, y + 51), 18, fill=TEAL)
        draw.text((872, y + 20), "OK", font=font(FONT_BOLD, 18), fill=WHITE)
    whatsapp_box(img, 103, 1235, "Ative com suporte")
    cta(draw, "CHAMAR NO WHATSAPP")
    save(img, "04_mercado_pago_whatsapp_story.png")


def post_05() -> None:
    img = dark_gradient()
    draw = ImageDraw.Draw(img)
    brand(img)
    pill(draw, 70, 190, "GARCOM MOBILE", "#0D2B48", CYAN)
    text_block(draw, 70, 300, "PEDIDO DA MESA EM TEMPO REAL NO CAIXA", font(FONT_BLACK, 68), WHITE, 930, 7)
    draw.text((74, 548), "Garcom abre mesa, lanca produto e envia observacao pelo celular.", font=font(FONT_BOLD, 31), fill=CYAN)
    phone_panel(img, 120, 650, 410, 690)
    shadow_card(img, (560, 710, 1010, 1220), 28, "#F7FBFF", "#C2D4E2", 50)
    draw.text((600, 765), "Ideal para", font=font(FONT_BOLD, 28), fill=TEAL)
    text_block(draw, 600, 820, "lanchonete, pizzaria, acai, bar, restaurante e delivery.", font(FONT_BLACK, 42), INK, 350, 8)
    seller_strip(img, 70, 1470)
    cta(draw, "PEDIR VIDEO DEMO")
    save(img, "05_garcom_mobile_whatsapp_story.png")


def post_06() -> None:
    img = gradient("#F5FAFD", "#DCEFF2")
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle((0, 0, W, 270), 0, fill=NAVY)
    brand(img)
    text_block(draw, 70, 330, "QUER UM FLUXO DIFERENTE PARA SUA LOJA?", font(FONT_BLACK, 70), INK, 930, 6)
    draw.text((72, 610), "Configuravel e personalizavel conforme a operacao.", font=font(FONT_BOLD, 31), fill=TEAL)
    shadow_card(img, (70, 710, 1010, 1205), 30, WHITE, LINE, 45)
    rows = [
        ("Setores", "balcao, cozinha, bar, sobremesa"),
        ("Impressoras", "por setor e por tipo de pedido"),
        ("Comandas", "mesa, delivery, balcao e garcom"),
        ("Automacoes", "sob consulta para plano pago"),
    ]
    for i, (title, desc) in enumerate(rows):
        y = 765 + i * 94
        draw.text((115, y), title, font=font(FONT_BLACK, 31), fill=INK)
        draw.text((330, y + 5), desc, font=font(FONT_BOLD, 24), fill=MUTED)
        draw.line((115, y + 68, 940, y + 68), fill="#E0E9F0", width=2)
    whatsapp_box(img, 103, 1290, "Consultar no WhatsApp")
    benefit_grid(draw, 95, 1600, ["Offline", "Online", "iFood", "Mercado Pago"], False)
    cta(draw, "MONTAR MEU PLANO")
    save(img, "06_personalizavel_whatsapp_story.png")


for builder in [post_01, post_02, post_03, post_04, post_05, post_06]:
    builder()

sheet = Image.new("RGB", (1080, 1180), "#EDF4F8")
thumb_w, thumb_h = 330, 586
for idx, file in enumerate(sorted(OUT.glob("*_story.png"))):
    thumb = Image.open(file).convert("RGB")
    thumb.thumbnail((thumb_w, thumb_h), Image.Resampling.LANCZOS)
    x = 30 + (idx % 3) * 350
    y = 30 + (idx // 3) * 570
    sheet.paste(thumb, (x, y))
sheet.save(OUT / "00_preview_grid.jpg", quality=94)

print(f"Generated {len(list(OUT.glob('*_story.png')))} posts at {OUT}")
