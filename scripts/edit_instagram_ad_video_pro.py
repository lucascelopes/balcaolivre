from __future__ import annotations

import math
import struct
import subprocess
import wave
from pathlib import Path

import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
INPUT = Path(r"C:\Users\lucas\Downloads\WhatsApp Video 2026-05-30 at 19.35.38.mp4")
OUT_DIR = ROOT / "outputs" / "ads-video-edit"
OUT_DIR.mkdir(parents=True, exist_ok=True)

FINAL_FILE = OUT_DIR / "balcao-livre-ads-instagram-pro.mp4"
THUMB_FILE = OUT_DIR / "balcao-livre-ads-instagram-pro-thumb.jpg"
CTA_THUMB_FILE = OUT_DIR / "balcao-livre-ads-instagram-pro-cta.jpg"
ASS_FILE = OUT_DIR / "balcao_ad_pro_legendas.ass"
MUSIC_FILE = OUT_DIR / "music_soft_loop.wav"

ICON = ROOT / "BalcaoLivreLadingPage" / "public" / "brand" / "bl-modern-icon.png"
SCREEN_PDV = ROOT / "tmp" / "app-window-compact-readable.png"
SCREEN_COMMAND = ROOT / "BalcaoLivreLadingPage" / "public" / "guide" / "windows-pdv" / "01-comandas-mesas.png"
SCREEN_DELIVERY = ROOT / "BalcaoLivreLadingPage" / "public" / "guide" / "windows-pdv" / "03-delivery-pedidos.png"
SCREEN_MENU = ROOT / "BalcaoLivreLadingPage" / "public" / "guide" / "windows-pdv" / "02-balcao-fichas.png"

OVERLAYS = {
    "intro": OUT_DIR / "pro_intro.png",
    "proof": OUT_DIR / "pro_proof_pdv.png",
    "brand": OUT_DIR / "pro_brand_hit.png",
    "pdv": OUT_DIR / "pro_pdv.png",
    "delivery": OUT_DIR / "pro_delivery.png",
    "menu": OUT_DIR / "pro_menu.png",
    "final": OUT_DIR / "pro_final_cta.png",
}

W, H = 1080, 1920
NAVY = (0, 31, 43)
NAVY_2 = (4, 67, 89)
TEAL = (44, 210, 199)
MINT = (210, 252, 244)
WHITE = (255, 255, 255)
TEXT = (9, 28, 43)
MUTED = (86, 109, 126)


def font_file(*names: str) -> str:
    fonts_dir = Path(r"C:\Windows\Fonts")
    for name in names:
        path = fonts_dir / name
        if path.exists():
            return str(path)
    return str(fonts_dir / "arial.ttf")


FONT_BLACK = font_file("seguibl.ttf", "segoeuib.ttf", "arialbd.ttf")
FONT_BOLD = font_file("segoeuib.ttf", "arialbd.ttf")
FONT_SEMI = font_file("seguisb.ttf", "segoeuib.ttf", "arialbd.ttf")
FONT_REG = font_file("segoeui.ttf", "arial.ttf")


def f(size: int, weight: str = "regular") -> ImageFont.FreeTypeFont:
    if weight == "black":
        return ImageFont.truetype(FONT_BLACK, size)
    if weight == "bold":
        return ImageFont.truetype(FONT_BOLD, size)
    if weight == "semi":
        return ImageFont.truetype(FONT_SEMI, size)
    return ImageFont.truetype(FONT_REG, size)


def text_size(draw: ImageDraw.ImageDraw, text: str, font: ImageFont.FreeTypeFont) -> tuple[int, int]:
    box = draw.textbbox((0, 0), text, font=font)
    return box[2] - box[0], box[3] - box[1]


def wrap_text(draw: ImageDraw.ImageDraw, text: str, font: ImageFont.FreeTypeFont, max_width: int) -> list[str]:
    lines: list[str] = []
    current = ""
    for word in text.split():
        trial = word if not current else f"{current} {word}"
        if text_size(draw, trial, font)[0] <= max_width:
            current = trial
        else:
            if current:
                lines.append(current)
            current = word
    if current:
        lines.append(current)
    return lines


def draw_multiline(
    draw: ImageDraw.ImageDraw,
    xy: tuple[int, int],
    text: str,
    font: ImageFont.FreeTypeFont,
    fill: tuple[int, int, int] | tuple[int, int, int, int],
    max_width: int,
    line_gap: int = 8,
    anchor: str = "la",
) -> int:
    x, y = xy
    lines = wrap_text(draw, text, font, max_width)
    for line in lines:
        w, h = text_size(draw, line, font)
        tx = x - w // 2 if anchor == "ma" else x
        draw.text((tx, y), line, font=font, fill=fill)
        y += h + line_gap
    return y


def add_shadow(base: Image.Image, box: tuple[int, int, int, int], radius: int = 34, alpha: int = 80) -> None:
    x1, y1, x2, y2 = box
    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    sdraw = ImageDraw.Draw(shadow)
    sdraw.rounded_rectangle((x1, y1 + 14, x2, y2 + 18), radius=radius, fill=(0, 15, 26, alpha))
    shadow = shadow.filter(ImageFilter.GaussianBlur(16))
    base.alpha_composite(shadow)


def paste_rounded(
    base: Image.Image,
    image_path: Path,
    box: tuple[int, int, int, int],
    radius: int = 24,
    border: tuple[int, int, int, int] = (44, 210, 199, 255),
) -> None:
    x1, y1, x2, y2 = box
    add_shadow(base, box, radius=radius, alpha=70)
    w, h = x2 - x1, y2 - y1
    img = Image.open(image_path).convert("RGB")
    scale = min(w / img.width, h / img.height)
    resized = img.resize((int(img.width * scale), int(img.height * scale)), Image.LANCZOS)
    canvas = Image.new("RGBA", (w, h), (247, 251, 253, 255))
    ox = (w - resized.width) // 2
    oy = (h - resized.height) // 2
    canvas.alpha_composite(resized.convert("RGBA"), (ox, oy))

    mask = Image.new("L", (w, h), 0)
    mdraw = ImageDraw.Draw(mask)
    mdraw.rounded_rectangle((0, 0, w, h), radius=radius, fill=255)
    clipped = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    clipped.alpha_composite(canvas)
    base.paste(clipped, (x1, y1), mask)

    draw = ImageDraw.Draw(base)
    draw.rounded_rectangle(box, radius=radius, outline=border, width=3)


def pill(
    draw: ImageDraw.ImageDraw,
    xy: tuple[int, int],
    text: str,
    fill: tuple[int, int, int, int],
    fg: tuple[int, int, int] | tuple[int, int, int, int],
    font: ImageFont.FreeTypeFont,
    px: int = 24,
    py: int = 14,
) -> tuple[int, int]:
    x, y = xy
    tw, th = text_size(draw, text, font)
    w, h = tw + px * 2, th + py * 2
    draw.rounded_rectangle((x, y, x + w, y + h), radius=h // 2, fill=fill)
    draw.text((x + px, y + py - 2), text, font=font, fill=fg)
    return x + w, y + h


def logo_chip(draw: ImageDraw.ImageDraw, base: Image.Image, x: int = 46, y: int = 52) -> None:
    icon = Image.open(ICON).convert("RGBA").resize((72, 72), Image.LANCZOS)
    draw.rounded_rectangle((x, y, x + 410, y + 88), radius=24, fill=(*NAVY, 224), outline=(*TEAL, 110), width=1)
    base.alpha_composite(icon, (x + 12, y + 8))
    draw.text((x + 96, y + 22), "Balcão Livre", font=f(31, "black"), fill=WHITE)
    draw.text((x + 96, y + 56), "PDV Online", font=f(19, "semi"), fill=TEAL)


def save_overlay(name: str, painter) -> None:
    base = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    painter(base, ImageDraw.Draw(base))
    base.save(OVERLAYS[name])


def build_intro(base: Image.Image, draw: ImageDraw.ImageDraw) -> None:
    draw.rectangle((0, 0, W, H), fill=(*NAVY, 255))
    draw.polygon([(760, 0), (1080, 0), (1080, 230), (900, 285)], fill=(44, 210, 199, 52))
    draw.polygon([(0, 1655), (1080, 1540), (1080, 1920), (0, 1920)], fill=(4, 67, 89, 155))
    draw.line((90, 112, 990, 112), fill=(*TEAL, 160), width=3)
    icon = Image.open(ICON).convert("RGBA").resize((168, 168), Image.LANCZOS)
    base.alpha_composite(icon, (456, 188))
    draw.text((W // 2, 410), "Balcão Livre PDV Online", font=f(48, "black"), fill=WHITE, anchor="mm")
    draw_multiline(
        draw,
        (W // 2, 520),
        "Sistema completo para restaurante, bar e delivery",
        f(64, "black"),
        WHITE,
        860,
        line_gap=12,
        anchor="ma",
    )
    draw.text((W // 2, 760), "Caixa • mesas • estoque • iFood • WhatsApp", font=f(34, "semi"), fill=TEAL, anchor="mm")
    draw.rounded_rectangle((200, 930, 880, 1096), radius=34, fill=(*NAVY_2, 255), outline=(*TEAL, 210), width=3)
    draw.text((W // 2, 984), "Plano completo por", font=f(29, "semi"), fill=MINT, anchor="mm")
    draw.text((W // 2, 1052), "R$139/mês", font=f(60, "black"), fill=WHITE, anchor="mm")
    pill(draw, (312, 1250), "Teste grátis pelo WhatsApp", (*TEAL, 255), NAVY, f(30, "black"), 30, 16)
    draw.text((W // 2, 1495), "feito para operar no dia a dia", font=f(31, "semi"), fill=(230, 244, 248), anchor="mm")


def build_proof(base: Image.Image, draw: ImageDraw.ImageDraw) -> None:
    logo_chip(draw, base)
    draw.rounded_rectangle((50, 180, 1030, 430), radius=34, fill=(*NAVY, 230), outline=(*TEAL, 180), width=2)
    draw.text((88, 220), "Não é só cadastro.", font=f(38, "black"), fill=TEAL)
    draw_multiline(draw, (88, 282), "É venda, comanda, delivery, pagamento e estoque em uma tela.", f(42, "black"), WHITE, 860, 8)
    paste_rounded(base, SCREEN_PDV, (66, 1000, 1014, 1518), radius=28)
    pill(draw, (78, 1538), "Tela real do PDV", (*TEAL, 245), NAVY, f(24, "black"), 22, 12)
    pill(draw, (370, 1538), "Tudo no Windows", (255, 255, 255, 230), NAVY, f(24, "black"), 22, 12)


def build_brand_hit(base: Image.Image, draw: ImageDraw.ImageDraw) -> None:
    draw.rectangle((0, 0, W, H), fill=(*NAVY, 196))
    icon = Image.open(ICON).convert("RGBA").resize((210, 210), Image.LANCZOS)
    base.alpha_composite(icon, (435, 440))
    draw.text((W // 2, 730), "Esse é o", font=f(44, "semi"), fill=MINT, anchor="mm")
    draw.text((W // 2, 820), "Balcão Livre", font=f(86, "black"), fill=WHITE, anchor="mm")
    draw.text((W // 2, 914), "PDV Online", font=f(48, "black"), fill=TEAL, anchor="mm")
    draw.rounded_rectangle((190, 1120, 890, 1224), radius=28, fill=(255, 255, 255, 20), outline=(*TEAL, 170), width=2)
    draw.text((W // 2, 1174), "para restaurante vender sem bagunça", font=f(33, "semi"), fill=WHITE, anchor="mm")


def build_pdv(base: Image.Image, draw: ImageDraw.ImageDraw) -> None:
    logo_chip(draw, base)
    paste_rounded(base, SCREEN_COMMAND, (62, 900, 1018, 1412), radius=28)
    draw.rounded_rectangle((60, 170, 1020, 462), radius=34, fill=(255, 255, 255, 238), outline=(*TEAL, 255), width=3)
    draw.text((92, 214), "Comandas, estoque e financeiro", font=f(43, "black"), fill=NAVY)
    draw_multiline(draw, (92, 292), "Acompanhe vendas em tempo real e organize mesa, balcão e delivery.", f(35, "semi"), MUTED, 840, 8)
    x = 92
    for label in ["Mesas", "Estoque", "Caixa", "Relatórios"]:
        x, _ = pill(draw, (x, 378), label, (*TEAL, 235), NAVY, f(23, "black"), 18, 10)
        x += 12


def build_delivery(base: Image.Image, draw: ImageDraw.ImageDraw) -> None:
    logo_chip(draw, base)
    paste_rounded(base, SCREEN_DELIVERY, (62, 890, 1018, 1402), radius=28)
    draw.rounded_rectangle((60, 170, 1020, 500), radius=34, fill=(*NAVY, 238), outline=(*TEAL, 245), width=3)
    draw.text((92, 214), "Delivery sem retrabalho", font=f(48, "black"), fill=WHITE)
    draw_multiline(draw, (92, 296), "Pedido chega, imprime na cozinha e segue para preparo.", f(37, "semi"), MINT, 830, 8)
    x = 92
    for label in ["iFood", "WhatsApp", "Cozinha", "Pagamentos"]:
        x, _ = pill(draw, (x, 405), label, (255, 255, 255, 235), NAVY, f(23, "black"), 18, 10)
        x += 12


def build_menu(base: Image.Image, draw: ImageDraw.ImageDraw) -> None:
    logo_chip(draw, base)
    paste_rounded(base, SCREEN_MENU, (62, 870, 1018, 1382), radius=28)
    draw.rounded_rectangle((60, 168, 1020, 490), radius=34, fill=(255, 255, 255, 238), outline=(*TEAL, 250), width=3)
    draw.text((92, 214), "Cardápio online + Garçom Web", font=f(42, "black"), fill=NAVY)
    draw_multiline(draw, (92, 292), "Cliente vê o cardápio pelo QR Code e o pedido cai no caixa.", f(36, "semi"), MUTED, 850, 8)
    x = 92
    for label in ["QR Code", "Mesa", "Celular", "Tempo real"]:
        x, _ = pill(draw, (x, 398), label, (*TEAL, 235), NAVY, f(23, "black"), 18, 10)
        x += 12


def build_final(base: Image.Image, draw: ImageDraw.ImageDraw) -> None:
    draw.rectangle((0, 0, W, H), fill=(0, 31, 43, 155))
    logo_chip(draw, base, 70, 74)
    draw.rounded_rectangle((72, 250, 1008, 822), radius=42, fill=(*NAVY, 246), outline=(*TEAL, 220), width=3)
    draw.text((W // 2, 322), "PDV Online para restaurante", font=f(42, "black"), fill=TEAL, anchor="mm")
    draw_multiline(draw, (W // 2, 405), "Caixa, mesas, delivery, estoque e pagamentos em um só app.", f(55, "black"), WHITE, 820, 10, anchor="ma")
    draw.text((W // 2, 682), "R$139/mês", font=f(78, "black"), fill=WHITE, anchor="mm")
    draw.text((W // 2, 750), "teste grátis disponível", font=f(32, "semi"), fill=MINT, anchor="mm")
    draw.rounded_rectangle((132, 1450, 948, 1605), radius=42, fill=(*TEAL, 255))
    draw.text((W // 2, 1528), "Chame no WhatsApp", font=f(52, "black"), fill=NAVY, anchor="mm")
    draw.rounded_rectangle((56, 1660, 1024, 1810), radius=34, fill=(*NAVY, 246), outline=(*TEAL, 150), width=2)
    draw.text((W // 2, 1716), "Balcão Livre PDV Online", font=f(37, "black"), fill=WHITE, anchor="mm")
    draw.text((W // 2, 1764), "teste grátis para conhecer o sistema", font=f(27, "semi"), fill=MINT, anchor="mm")


def build_overlays() -> None:
    save_overlay("intro", build_intro)
    save_overlay("proof", build_proof)
    save_overlay("brand", build_brand_hit)
    save_overlay("pdv", build_pdv)
    save_overlay("delivery", build_delivery)
    save_overlay("menu", build_menu)
    save_overlay("final", build_final)


def ass_time(seconds: float) -> str:
    centis = int(round(seconds * 100))
    h = centis // 360000
    centis %= 360000
    m = centis // 6000
    centis %= 6000
    s = centis // 100
    cs = centis % 100
    return f"{h}:{m:02d}:{s:02d}.{cs:02d}"


def ass_escape(text: str) -> str:
    return text.replace("\\", "\\\\").replace("{", "\\{").replace("}", "\\}")


def write_ass() -> None:
    captions = [
        (2.81, 5.70, "Vem comigo! Vou mostrar pra vocês"),
        (5.70, 10.11, "um programa excelente para lanchonete, restaurante e delivery."),
        (10.99, 12.89, "Esse é o Balcão Livre!"),
        (13.73, 17.80, "Gerenciamento de estoque e financeiro"),
        (17.80, 20.93, "com vendas em tempo real."),
        (20.93, 25.95, "Ele imprime o pedido direto na cozinha."),
        (26.20, 29.10, "Tem integração com iFood."),
        (29.10, 33.17, "E pode integrar com maquininha e Mercado Pago."),
        (34.05, 38.85, "Novas integrações em desenvolvimento."),
        (39.47, 43.90, "Gestão mais simples para o seu negócio."),
        (44.10, 48.55, "Peça um teste grátis e uma avaliação do seu aplicativo."),
    ]

    header = """[Script Info]
ScriptType: v4.00+
PlayResX: 1080
PlayResY: 1920
ScaledBorderAndShadow: yes

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Caption,Segoe UI Semibold,50,&H00FFFFFF,&H00FFFFFF,&HAA2B1F00,&HAA2B1F00,-1,0,0,0,100,100,0,0,3,18,0,2,70,70,176,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
"""
    lines = [header]
    for start, end, text in captions:
        lines.append(f"Dialogue: 10,{ass_time(start)},{ass_time(end)},Caption,,0,0,0,,{ass_escape(text)}")
    ASS_FILE.write_text("\n".join(lines), encoding="utf-8-sig")


def write_music(duration_s: float = 55.0, sample_rate: int = 44100) -> None:
    bpm = 94
    beat = 60.0 / bpm
    total = int(duration_s * sample_rate)
    chords = [
        (130.81, 164.81, 196.00),
        (103.83, 155.56, 196.00),
        (116.54, 174.61, 220.00),
        (98.00, 146.83, 196.00),
    ]

    def env(t: float, attack: float, release: float, length: float) -> float:
        if t < attack:
            return t / attack
        if t > length - release:
            return max(0.0, (length - t) / release)
        return 1.0

    with wave.open(str(MUSIC_FILE), "wb") as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(sample_rate)
        for i in range(total):
            t = i / sample_rate
            bar = int(t / (beat * 4))
            chord = chords[bar % len(chords)]
            local = t % (beat * 4)
            pad = 0.0
            for freq in chord:
                pad += math.sin(2 * math.pi * freq * t) * 0.045
                pad += math.sin(2 * math.pi * freq * 2.0 * t) * 0.018
            pad *= env(local, 0.08, 0.45, beat * 4)
            kick = math.sin(2 * math.pi * 58 * (t % beat)) * max(0, 1 - (t % beat) / 0.13) * 0.09
            hat = (math.sin(2 * math.pi * 9000 * t) + math.sin(2 * math.pi * 12000 * t)) * 0.006
            hat *= 1.0 if (t % (beat / 2)) < 0.035 else 0.0
            value = max(-0.9, min(0.9, pad + kick + hat))
            left = int(value * 32767)
            right = int((pad * 0.94 + kick + hat * 0.8) * 32767)
            wf.writeframes(struct.pack("<hh", left, right))


def fade_overlay(label: str, start: float, end: float, in_idx: int, base: str, out: str) -> str:
    safe_label = label.replace("-", "_")
    return (
        f"[{in_idx}:v]format=rgba,"
        f"fade=t=in:st={start}:d=0.25:alpha=1,"
        f"fade=t=out:st={max(start, end - 0.25)}:d=0.25:alpha=1[{safe_label}];"
        f"[{base}][{safe_label}]overlay=0:0:enable='between(t,{start},{end})'[{out}]"
    )


def run_ffmpeg() -> None:
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    ass_path = ASS_FILE.as_posix().replace(":", "\\:")
    overlay_paths = [
        OVERLAYS["intro"],
        OVERLAYS["proof"],
        OVERLAYS["brand"],
        OVERLAYS["pdv"],
        OVERLAYS["delivery"],
        OVERLAYS["menu"],
        OVERLAYS["final"],
    ]

    cmd = [
        ffmpeg,
        "-y",
        "-i",
        str(INPUT),
        "-stream_loop",
        "-1",
        "-i",
        str(MUSIC_FILE),
    ]
    for overlay in overlay_paths:
        cmd += ["-loop", "1", "-t", "49.76", "-i", str(overlay)]

    filters = [
        "[0:v]scale=1080:1920:force_original_aspect_ratio=increase,crop=1080:1920,"
        "eq=contrast=1.08:saturation=1.08:brightness=0.012,format=rgba[base0]",
        fade_overlay("intro", 0.00, 2.90, 2, "base0", "base1"),
        fade_overlay("proof", 3.00, 10.55, 3, "base1", "base2"),
        fade_overlay("brand", 10.65, 13.35, 4, "base2", "base3"),
        fade_overlay("pdv", 13.45, 21.30, 5, "base3", "base4"),
        fade_overlay("delivery", 21.50, 33.25, 6, "base4", "base5"),
        fade_overlay("menu", 33.60, 39.45, 7, "base5", "base6"),
        fade_overlay("final", 43.70, 49.76, 8, "base6", "base7"),
        f"[base7]ass='{ass_path}',format=yuv420p[v]",
        "[0:a]volume=1.20,acompressor=threshold=-20dB:ratio=2.2:attack=18:release=180[voice]",
        "[1:a]volume=0.085,atrim=duration=49.76,afade=t=out:st=47.5:d=2.0[music]",
        "[voice][music]amix=inputs=2:duration=first:dropout_transition=2[a]",
    ]

    cmd += [
        "-filter_complex",
        ";".join(filters),
        "-map",
        "[v]",
        "-map",
        "[a]",
        "-c:v",
        "libx264",
        "-preset",
        "medium",
        "-crf",
        "19",
        "-pix_fmt",
        "yuv420p",
        "-c:a",
        "aac",
        "-b:a",
        "160k",
        "-movflags",
        "+faststart",
        str(FINAL_FILE),
    ]
    subprocess.run(cmd, check=True)

    for ss, path in [("8", THUMB_FILE), ("46", CTA_THUMB_FILE)]:
        subprocess.run(
            [
                ffmpeg,
                "-y",
                "-ss",
                ss,
                "-i",
                str(FINAL_FILE),
                "-frames:v",
                "1",
                "-update",
                "1",
                str(path),
            ],
            check=True,
        )


def main() -> None:
    build_overlays()
    write_ass()
    write_music()
    run_ffmpeg()
    print(FINAL_FILE)
    print(THUMB_FILE)
    print(CTA_THUMB_FILE)


if __name__ == "__main__":
    main()
