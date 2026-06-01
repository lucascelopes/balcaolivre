import asyncio
import os
import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "outputs" / "free-video-balcao"
STOCK = OUT / "stock"
REAL = OUT / "realista"
REAL.mkdir(parents=True, exist_ok=True)

W, H = 720, 1280
FONT_DIR = Path("C:/Windows/Fonts")
FONT_BOLD = str(FONT_DIR / "segoeuib.ttf")
FONT_REG = str(FONT_DIR / "segoeui.ttf")
FONT_SEMI = str(FONT_DIR / "seguisb.ttf")


def font(path, size):
    try:
        return ImageFont.truetype(path, size)
    except Exception:
        return ImageFont.load_default()

F_TITLE = font(FONT_BOLD, 52)
F_SUB = font(FONT_SEMI, 31)
F_SMALL = font(FONT_REG, 24)
F_BADGE = font(FONT_BOLD, 22)
F_LOGO = font(FONT_BOLD, 30)
F_MICRO = font(FONT_SEMI, 19)
F_CTA = font(FONT_BOLD, 36)

NAVY = (0, 31, 43, 238)
NAVY2 = (8, 78, 111, 238)
TEAL = (44, 210, 199, 245)
WHITE = (255, 255, 255, 255)
MUTED = (224, 240, 246, 235)
YELLOW = (255, 204, 82, 255)
RED = (226, 72, 66, 255)
GREEN = (95, 207, 122, 255)


def rounded(draw, xy, radius, fill, outline=None, width=1):
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=outline, width=width)


def wrap_text(draw, text, fnt, max_width):
    words = text.split()
    lines = []
    line = ""
    for word in words:
        test = f"{line} {word}".strip()
        if draw.textlength(test, font=fnt) <= max_width:
            line = test
        else:
            if line:
                lines.append(line)
            line = word
    if line:
        lines.append(line)
    return lines


def draw_text_block(draw, text, xy, fnt, fill, max_width, line_gap=8):
    x, y = xy
    for line in wrap_text(draw, text, fnt, max_width):
        draw.text((x, y), line, font=fnt, fill=fill)
        y += fnt.size + line_gap
    return y


def make_overlay(idx, title, subtitle, chip=None, mini=None, cta=False):
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # bottom gradient / readability panel
    grad = Image.new("RGBA", (W, 520), (0, 0, 0, 0))
    gd = ImageDraw.Draw(grad)
    for y in range(520):
        alpha = int(10 + 218 * (y / 520) ** 1.8)
        gd.line([(0, y), (W, y)], fill=(0, 20, 28, alpha))
    img.alpha_composite(grad, (0, H - 520))

    # small top brand
    rounded(d, (28, 30, 92, 94), 15, (0, 50, 70, 230), outline=(88, 220, 215, 210), width=2)
    d.text((43, 45), "BL", font=F_LOGO, fill=WHITE)
    d.text((105, 43), "Balcão Livre PDV Online", font=F_MICRO, fill=WHITE)
    d.text((105, 70), "caixa, iFood e garçom conectados", font=font(FONT_REG, 17), fill=MUTED)

    if chip:
        tw = int(d.textlength(chip, font=F_BADGE)) + 44
        rounded(d, (W - tw - 28, 42, W - 28, 84), 21, (0, 31, 43, 220), outline=(44, 210, 199, 220), width=2)
        d.text((W - tw - 6, 51), chip, font=F_BADGE, fill=TEAL)

    # mini product overlay, kept small
    if mini:
        x, y = mini.get("xy", (58, 170))
        mw, mh = mini.get("size", (604, 190))
        rounded(d, (x, y, x + mw, y + mh), 24, (245, 252, 255, 238), outline=(170, 218, 235, 230), width=2)
        d.text((x + 24, y + 22), mini["head"], font=font(FONT_BOLD, 26), fill=(0, 31, 43, 255))
        d.text((x + 24, y + 62), mini["body"], font=font(FONT_REG, 22), fill=(70, 91, 106, 255))
        if mini.get("badge"):
            bw = int(d.textlength(mini["badge"], font=font(FONT_BOLD, 18))) + 30
            rounded(d, (x + 24, y + mh - 52, x + 24 + bw, y + mh - 18), 17, mini.get("badge_color", TEAL), None)
            d.text((x + 39, y + mh - 47), mini["badge"], font=font(FONT_BOLD, 18), fill=(0, 31, 43, 255))

    # title/subtitle
    y0 = H - 392 if not cta else H - 440
    d.text((48, y0), title, font=F_TITLE if not cta else font(FONT_BOLD, 58), fill=WHITE)
    draw_text_block(d, subtitle, (50, y0 + 70), F_SUB, (226, 242, 248, 255), W - 100, 7)

    if cta:
        rounded(d, (50, H - 168, W - 50, H - 92), 22, TEAL, None)
        d.text((88, H - 148), "Chame no WhatsApp e teste", font=F_CTA, fill=(0, 31, 43, 255))
        d.text((50, H - 64), "balcaolivrepdv.com.br", font=font(FONT_BOLD, 25), fill=(226, 242, 248, 255))

    p = REAL / f"overlay-{idx:02}.png"
    img.save(p)
    return p


def ffmpeg_exe():
    try:
        import imageio_ffmpeg
        return imageio_ffmpeg.get_ffmpeg_exe()
    except Exception:
        return "ffmpeg"


scenes = [
    {
        "stock": STOCK / "stock-01.mp4", "ss": 0.8, "dur": 5.2,
        "title": "Caixa mais rápido.",
        "subtitle": "Venda no balcão, receba no Pix ou cartão e imprima sem travar a operação.",
        "chip": "CAIXA",
        "mini": {"head": "Venda lançada", "body": "Pagamento confirmado e comprovante pronto.", "badge": "F9 receber", "badge_color": TEAL},
    },
    {
        "stock": STOCK / "stock-03.mp4", "ss": 0.4, "dur": 5.2,
        "title": "iFood no mesmo lugar.",
        "subtitle": "Quando o pedido entra, o caixa vê na hora e a cozinha recebe a produção.",
        "chip": "IFOOD",
        "mini": {"head": "Novo pedido iFood", "body": "Mesa delivery - preparo iniciado", "badge": "NOVO", "badge_color": (88, 184, 255, 255)},
    },
    {
        "stock": STOCK / "stock-02.mp4", "ss": 1.2, "dur": 5.5,
        "title": "Garçom lança na mesa.",
        "subtitle": "O pedido feito pelo celular cai direto no computador do caixa, em tempo real.",
        "chip": "GARÇOM WEB",
        "mini": {"head": "Mesa 03", "body": "2 hambúrgueres, 1 refrigerante", "badge": "Enviado ao caixa", "badge_color": GREEN},
    },
    {
        "stock": STOCK / "stock-04.mp4", "ss": 0.3, "dur": 5.0,
        "title": "Cardápio online pronto.",
        "subtitle": "Cliente vê produtos e preços atualizados sem você ficar refazendo link toda hora.",
        "chip": "QR CODE",
        "mini": {"head": "Cardápio digital", "body": "Produtos sincronizados com estoque", "badge": "online", "badge_color": TEAL},
    },
    {
        "stock": STOCK / "stock-01.mp4", "ss": 5.5, "dur": 5.0,
        "title": "Tudo conectado.",
        "subtitle": "Comandas, estoque, delivery, impressão e fechamento de caixa em uma tela só.",
        "chip": "PDV ONLINE",
        "mini": {"head": "Painel do caixa", "body": "Balcão + mesas + delivery + estoque", "badge": "ao vivo", "badge_color": YELLOW},
    },
    {
        "stock": STOCK / "stock-02.mp4", "ss": 7.0, "dur": 6.0,
        "title": "Balcão Livre PDV",
        "subtitle": "Para restaurante, bar, lanchonete e delivery que precisam vender sem complicação.",
        "chip": "TESTE AGORA",
        "mini": None,
        "cta": True,
    },
]

narration = (
    "Restaurante cheio, pedido chegando toda hora. "
    "Com o Balcão Livre PDV Online, o caixa vende mais rápido, recebe no Pix ou cartão e imprime sem travar a operação. "
    "O pedido do iFood entra no mesmo lugar e a cozinha já recebe a produção. "
    "O garçom lança a mesa pelo celular, e tudo aparece na hora no computador do caixa. "
    "Cardápio online, estoque, comandas e fechamento ficam conectados. "
    "Balcão Livre PDV Online. Chame no WhatsApp e teste no seu restaurante."
)

async def make_voice():
    mp3 = REAL / "narracao-neural.mp3"
    if mp3.exists() and mp3.stat().st_size > 1000:
        return mp3
    import edge_tts
    communicate = edge_tts.Communicate(narration, "pt-BR-AntonioNeural", rate="+8%", volume="+0%")
    await communicate.save(str(mp3))
    return mp3


def run(cmd):
    print("RUN", " ".join(str(c) for c in cmd[:6]), "...")
    subprocess.run(cmd, check=True)


def render():
    ff = ffmpeg_exe()
    overlays = []
    rendered = []
    for i, sc in enumerate(scenes, 1):
        ov = make_overlay(i, sc["title"], sc["subtitle"], sc.get("chip"), sc.get("mini"), sc.get("cta", False))
        overlays.append(ov)
        out = REAL / f"scene-{i:02}.mp4"
        rendered.append(out)
        # Re-render each time; fast enough and keeps updates consistent.
        vf = "[0:v]scale=720:1280:force_original_aspect_ratio=increase,crop=720:1280,setsar=1,eq=contrast=1.06:saturation=1.08:brightness=-0.015,format=rgba[base];[base][1:v]overlay=0:0:shortest=1,format=yuv420p[v]"
        run([
            ff, "-y", "-ss", str(sc["ss"]), "-t", str(sc["dur"]), "-i", str(sc["stock"]),
            "-loop", "1", "-i", str(ov),
            "-filter_complex", vf,
            "-map", "[v]", "-an", "-r", "30",
            "-c:v", "libx264", "-preset", "veryfast", "-crf", "21", str(out)
        ])

    concat = REAL / "concat.txt"
    concat.write_text("\n".join(f"file '{p.as_posix()}'" for p in rendered), encoding="utf-8")
    video_no_audio = REAL / "video-sem-audio.mp4"
    run([ff, "-y", "-f", "concat", "-safe", "0", "-i", str(concat), "-c", "copy", str(video_no_audio)])

    audio = asyncio.run(make_voice())
    final = OUT / "balcao-livre-pdv-reel-realista-com-pessoas.mp4"
    run([
        ff, "-y", "-i", str(video_no_audio), "-i", str(audio),
        "-map", "0:v:0", "-map", "1:a:0",
        "-c:v", "copy", "-c:a", "aac", "-b:a", "160k", "-shortest", str(final)
    ])

    (OUT / "roteiro-reel-realista.md").write_text(
        "# Reel realista - Balcao Livre PDV Online\n\n"
        "## Narração\n" + narration + "\n\n"
        "## Cenas\n" + "\n".join(f"- {s['title']} {s['subtitle']}" for s in scenes) + "\n",
        encoding="utf-8"
    )
    print(final)


if __name__ == "__main__":
    render()
