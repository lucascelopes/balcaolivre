from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "outputs" / "ads-human-balcao"
GENERATED = Path("C:/Users/lucas/.codex/generated_images/019e6205-9da7-71d3-91d9-4f6f82c6799c")

NAVY = (7, 31, 51)
NAVY_DARK = (2, 17, 29)
TEAL = (28, 190, 181)
TEAL_DARK = (14, 127, 118)
WHITE = (255, 255, 255)
MUTED_LIGHT = (203, 221, 232)
GOLD = (238, 166, 57)
RED = (235, 77, 81)
YELLOW = (255, 206, 75)
GLASS = (3, 22, 36, 218)


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    path = Path("C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf")
    if path.exists():
        return ImageFont.truetype(str(path), size=size)
    return ImageFont.load_default(size=size)


def text_box(draw: ImageDraw.ImageDraw, text: str, fnt: ImageFont.FreeTypeFont) -> tuple[int, int]:
    box = draw.textbbox((0, 0), text, font=fnt)
    return box[2] - box[0], box[3] - box[1]


def wrap(draw: ImageDraw.ImageDraw, text: str, fnt: ImageFont.FreeTypeFont, width: int) -> list[str]:
    lines: list[str] = []
    for raw in text.split("\n"):
        words = raw.split()
        if not words:
            lines.append("")
            continue
        line = words[0]
        for word in words[1:]:
            candidate = f"{line} {word}"
            if text_box(draw, candidate, fnt)[0] <= width:
                line = candidate
            else:
                lines.append(line)
                line = word
        lines.append(line)
    return lines


def draw_wrapped(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str, fnt: ImageFont.FreeTypeFont, fill: tuple[int, int, int], width: int, gap: int = 8) -> int:
    x, y = xy
    for line in wrap(draw, text, fnt, width):
        draw.text((x, y), line, font=fnt, fill=fill)
        y += text_box(draw, line or "Ag", fnt)[1] + gap
    return y


def draw_shadow_text(
    draw: ImageDraw.ImageDraw,
    xy: tuple[int, int],
    text: str,
    fnt: ImageFont.FreeTypeFont,
    fill: tuple[int, int, int],
    shadow: tuple[int, int, int] = (0, 0, 0),
    offset: int = 3,
) -> None:
    x, y = xy
    draw.text((x + offset, y + offset), text, font=fnt, fill=shadow)
    draw.text((x, y), text, font=fnt, fill=fill)


def cover_resize(path: Path, size: tuple[int, int]) -> Image.Image:
    img = Image.open(path).convert("RGB")
    target_w, target_h = size
    scale = max(target_w / img.width, target_h / img.height)
    img = img.resize((int(img.width * scale), int(img.height * scale)), Image.Resampling.LANCZOS)
    left = (img.width - target_w) // 2
    top = (img.height - target_h) // 2
    return img.crop((left, top, left + target_w, top + target_h)).convert("RGBA")


def add_left_gradient(img: Image.Image, strength: int = 228) -> None:
    w, h = img.size
    overlay = Image.new("RGBA", img.size, (0, 0, 0, 0))
    px = overlay.load()
    for y in range(h):
        for x in range(w):
            t = max(0, 1 - x / (w * 0.72))
            top_bias = max(0, 1 - y / (h * 0.82))
            alpha = int(strength * (t ** 1.25) * (0.72 + top_bias * 0.28))
            px[x, y] = (2, 18, 31, alpha)
    img.alpha_composite(overlay)


def rounded_rect_with_shadow(base: Image.Image, box: tuple[int, int, int, int], radius: int, fill: tuple[int, int, int, int]) -> ImageDraw.ImageDraw:
    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    x1, y1, x2, y2 = box
    sd.rounded_rectangle((x1 + 8, y1 + 14, x2 + 8, y2 + 14), radius=radius, fill=(0, 0, 0, 95))
    shadow = shadow.filter(ImageFilter.GaussianBlur(18))
    base.alpha_composite(shadow)
    draw = ImageDraw.Draw(base)
    draw.rounded_rectangle(box, radius=radius, fill=fill)
    return draw


def chip(draw: ImageDraw.ImageDraw, x: int, y: int, text: str, fill: tuple[int, int, int]) -> int:
    f = font(24, True)
    tw, th = text_box(draw, text, f)
    draw.rounded_rectangle((x, y, x + tw + 34, y + th + 20), radius=19, fill=fill)
    draw.text((x + 17, y + 9), text, font=f, fill=WHITE)
    return y + th + 38


def feature_row(draw: ImageDraw.ImageDraw, x: int, y: int, text: str, width: int) -> int:
    draw.rounded_rectangle((x, y + 10, x + 20, y + 30), radius=10, fill=TEAL)
    return draw_wrapped(draw, (x + 38, y), text, font(27, True), WHITE, width - 38, gap=5) + 15


def brand(draw: ImageDraw.ImageDraw, x: int, y: int, dark: bool = True) -> None:
    draw.text((x, y), "Balcao Livre PDV", font=font(32, True), fill=WHITE if dark else NAVY)
    draw.text((x, y + 42), "www.balcaolivrepdv.com.br", font=font(21, True), fill=TEAL if dark else TEAL_DARK)


PLANS = [
    {
        "name": "01-caixa-delivery-completo",
        "bg": "ig_0bd243277588f0b9016a1a055934408191831213e3e4c16dbf.png",
        "tag": "PLANO COMPLETO",
        "tag_color": GOLD,
        "hook": "PARE DE PERDER PEDIDOS",
        "title": "iFood, WhatsApp, caixa e delivery em um so lugar",
        "subtitle": "Centralize atendimento, cozinha, motoboy, estoque e pagamento.",
        "price": "R$ 1.200",
        "price_note": "COMPLETO / ANO",
        "features": [
            "Pedidos online caem direto no PDV",
            "Cozinha, caixa e entrega no mesmo fluxo",
            "Feito para restaurante vender mais com controle",
        ],
    },
    {
        "name": "02-garcom-web-online",
        "bg": "ig_0bd243277588f0b9016a1a06e66a40819184a069bab9b2a3f8.png",
        "tag": "GARCOM WEB",
        "tag_color": TEAL_DARK,
        "hook": "SEM PAPEL, SEM GRITARIA",
        "title": "Garcom lanca da mesa e o pedido cai no caixa",
        "subtitle": "Abre por link no celular. Simples para o garcom e rapido para a loja.",
        "price": "R$ 450",
        "price_note": "GARCOM WEB / ANO",
        "features": [
            "Mesas e comandas direto no celular",
            "Caixa acompanha tudo em tempo real",
            "Mais agilidade no atendimento do salao",
        ],
    },
    {
        "name": "03-hibrido-offline-online",
        "bg": "ig_0bd243277588f0b9016a1a0737a6788191bcf1ef48de8d3762.png",
        "tag": "HIBRIDO",
        "tag_color": TEAL_DARK,
        "hook": "SE A INTERNET CAIR, VOCE VENDE",
        "title": "PDV hibrido para caixa, estoque e delivery local",
        "subtitle": "Comece barato, venda no Windows e sincronize quando estiver online.",
        "price": "R$ 260",
        "price_note": "HIBRIDO / ANO",
        "features": [
            "Balcao, mesas, estoque e fechamento",
            "Venda local continua mesmo sem internet",
            "Licenca anual economica para comecar",
        ],
    },
]


def make_story(plan: dict[str, object]) -> Image.Image:
    img = cover_resize(GENERATED / str(plan["bg"]), (1080, 1920))
    add_left_gradient(img, strength=245)
    draw = ImageDraw.Draw(img)

    brand(draw, 64, 70)
    y = 180
    y = chip(draw, 64, y, str(plan["hook"]), RED if "PERDER" in str(plan["hook"]) else plan["tag_color"])
    y += 20
    y = draw_wrapped(draw, (64, y), str(plan["title"]), font(70, True), WHITE, 800, gap=8)
    draw.rounded_rectangle((64, y + 8, 390, y + 18), radius=5, fill=TEAL)
    y += 42
    y = draw_wrapped(draw, (64, y), str(plan["subtitle"]), font(34, True), MUTED_LIGHT, 780, gap=8)

    rounded_rect_with_shadow(img, (54, 1290, 1026, 1810), 34, GLASS)
    draw = ImageDraw.Draw(img)
    draw.text((102, 1332), str(plan["price_note"]).upper(), font=font(25, True), fill=TEAL)
    draw_shadow_text(draw, (102, 1372), str(plan["price"]), font(98, True), YELLOW, offset=4)
    draw.text((102, 1482), "licenca de 1 ano", font=font(30, True), fill=MUTED_LIGHT)
    draw.rounded_rectangle((630, 1350, 970, 1446), radius=24, fill=(15, 132, 122, 235), outline=(84, 238, 224, 255), width=2)
    draw.text((672, 1372), "DEMO", font=font(36, True), fill=WHITE)
    draw.text((672, 1410), "NO WHATSAPP", font=font(25, True), fill=WHITE)
    fy = 1534
    for item in list(plan["features"]):
        fy = feature_row(draw, 102, fy, item, 850)

    draw.rounded_rectangle((54, 1836, 1026, 1908), radius=24, fill=TEAL_DARK)
    draw.text((145, 1855), "CHAME AGORA E VEJA FUNCIONANDO", font=font(31, True), fill=WHITE)
    return img.convert("RGB")


def make_feed(plan: dict[str, object]) -> Image.Image:
    bg = Image.open(GENERATED / str(plan["bg"])).convert("RGB")
    scale = 1080 / bg.width
    resized = bg.resize((1080, int(bg.height * scale)), Image.Resampling.LANCZOS)
    top = max(0, min(resized.height - 1080, 360))
    img = resized.crop((0, top, 1080, top + 1080)).convert("RGBA")
    add_left_gradient(img, strength=230)
    draw = ImageDraw.Draw(img)
    brand(draw, 56, 52)
    y = chip(draw, 56, 150, str(plan["hook"]), RED if "PERDER" in str(plan["hook"]) else plan["tag_color"]) + 18
    y = draw_wrapped(draw, (56, y), str(plan["title"]), font(53, True), WHITE, 660, gap=6)
    draw.rounded_rectangle((56, y + 4, 330, y + 12), radius=4, fill=TEAL)

    rounded_rect_with_shadow(img, (46, 680, 1034, 1018), 30, GLASS)
    draw = ImageDraw.Draw(img)
    draw.text((86, 720), str(plan["price_note"]).upper(), font=font(22, True), fill=TEAL)
    draw_shadow_text(draw, (86, 750), str(plan["price"]), font(76, True), YELLOW, offset=3)
    draw.text((86, 836), "licenca de 1 ano", font=font(24, True), fill=MUTED_LIGHT)
    fy = 878
    for item in list(plan["features"])[:2]:
        fy = feature_row(draw, 86, fy, item, 585)
    draw.rounded_rectangle((710, 856, 994, 940), radius=22, fill=TEAL_DARK, outline=(84, 238, 224), width=2)
    draw.text((750, 874), "VER DEMO", font=font(31, True), fill=WHITE)
    draw.text((740, 910), "NO WHATSAPP", font=font(22, True), fill=WHITE)
    return img.convert("RGB")


def contact_sheet(paths: list[Path]) -> None:
    thumbs = []
    for path in paths:
        img = Image.open(path).convert("RGB")
        img.thumbnail((260, 430), Image.Resampling.LANCZOS)
        thumbs.append((path.name, img.copy()))
    sheet = Image.new("RGB", (940, 980), (237, 246, 250))
    draw = ImageDraw.Draw(sheet)
    draw.text((28, 24), "Balcao Livre PDV - anuncios com pessoas", font=font(30, True), fill=NAVY)
    for i, (name, img) in enumerate(thumbs):
        x = 28 + (i % 3) * 300
        y = 88 + (i // 3) * 440
        sheet.paste(img, (x, y))
        draw_wrapped(draw, (x, y + img.height + 10), name, font(15, True), NAVY, 260, gap=3)
    sheet.save(OUT / "00-preview-human-ads.png", quality=95)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    paths: list[Path] = []
    for plan in PLANS:
        story = make_story(plan)
        feed = make_feed(plan)
        story_path = OUT / f"{plan['name']}-story.png"
        feed_path = OUT / f"{plan['name']}-feed.png"
        story.save(story_path, quality=96)
        feed.save(feed_path, quality=96)
        paths.extend([story_path, feed_path])
    contact_sheet(paths)
    print(OUT)


if __name__ == "__main__":
    main()
