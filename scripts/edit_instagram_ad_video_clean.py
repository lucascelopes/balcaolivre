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

FINAL_FILE = OUT_DIR / "balcao-livre-ads-instagram-depoimento-limpo.mp4"
THUMB_FILE = OUT_DIR / "balcao-livre-ads-instagram-depoimento-limpo-thumb.jpg"
CTA_THUMB_FILE = OUT_DIR / "balcao-livre-ads-instagram-depoimento-limpo-cta.jpg"
ASS_FILE = OUT_DIR / "balcao_ad_depoimento_legendas.ass"
MUSIC_FILE = OUT_DIR / "music_depoimento_soft.wav"

ICON = ROOT / "BalcaoLivreLadingPage" / "public" / "brand" / "bl-modern-icon.png"

W, H = 1080, 1920
NAVY = (0, 31, 43)
NAVY_2 = (5, 76, 101)
TEAL = (44, 210, 199)
MINT = (210, 252, 244)
WHITE = (255, 255, 255)

OVERLAYS = {
    "intro": OUT_DIR / "clean_intro.png",
    "delivery": OUT_DIR / "clean_delivery.png",
    "stock": OUT_DIR / "clean_stock.png",
    "print": OUT_DIR / "clean_print.png",
    "integrations": OUT_DIR / "clean_integrations.png",
    "final": OUT_DIR / "clean_final.png",
}


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


def rounded_shadow(base: Image.Image, box: tuple[int, int, int, int], radius: int, alpha: int = 80) -> None:
    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(shadow)
    x1, y1, x2, y2 = box
    draw.rounded_rectangle((x1, y1 + 10, x2, y2 + 12), radius=radius, fill=(0, 12, 20, alpha))
    base.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(16)))


def logo_bug(base: Image.Image, draw: ImageDraw.ImageDraw) -> None:
    box = (34, 34, 424, 118)
    rounded_shadow(base, box, 24, 50)
    draw.rounded_rectangle(box, radius=24, fill=(*NAVY, 232), outline=(*TEAL, 120), width=1)
    icon = Image.open(ICON).convert("RGBA").resize((58, 58), Image.LANCZOS)
    base.alpha_composite(icon, (50, 47))
    draw.text((120, 55), "Balcão Livre", font=f(30, "black"), fill=WHITE)
    draw.text((120, 88), "PDV Online", font=f(18, "semi"), fill=TEAL)


def pill(
    base: Image.Image,
    draw: ImageDraw.ImageDraw,
    text: str,
    xy: tuple[int, int],
    tone: str = "dark",
    icon: str | None = None,
) -> None:
    font = f(30, "black")
    small = f(23, "semi")
    label = f"{icon}  {text}" if icon else text
    tw, th = text_size(draw, label, font)
    x, y = xy
    w = min(940, tw + 62)
    h = 72
    fill = (*NAVY, 228) if tone == "dark" else (255, 255, 255, 232)
    fg = WHITE if tone == "dark" else NAVY
    rounded_shadow(base, (x, y, x + w, y + h), 24, 45)
    draw.rounded_rectangle((x, y, x + w, y + h), radius=24, fill=fill, outline=(*TEAL, 160), width=1)
    draw.text((x + 31, y + 18), label, font=font if len(label) < 30 else small, fill=fg)


def bottom_gradient(base: Image.Image, strength: int = 145) -> None:
    grad = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    pix = grad.load()
    for y in range(H):
        if y < 1120:
            continue
        a = int(((y - 1120) / (H - 1120)) ** 1.2 * strength)
        for x in range(W):
            pix[x, y] = (0, 20, 28, a)
    base.alpha_composite(grad)


def save_overlay(name: str, painter) -> None:
    base = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(base)
    painter(base, draw)
    base.save(OVERLAYS[name])


def build_intro(base: Image.Image, draw: ImageDraw.ImageDraw) -> None:
    logo_bug(base, draw)
    bottom_gradient(base, 175)
    draw.rounded_rectangle((52, 1322, 1028, 1580), radius=34, fill=(*NAVY, 222), outline=(*TEAL, 150), width=2)
    draw.text((86, 1374), "PDV online para restaurante", font=f(38, "black"), fill=TEAL)
    draw.text((86, 1440), "Caixa, mesas, delivery e estoque", font=f(47, "black"), fill=WHITE)
    draw.text((86, 1506), "em um sistema só.", font=f(47, "black"), fill=WHITE)
    draw.rounded_rectangle((52, 1608, 396, 1680), radius=24, fill=(*TEAL, 245))
    draw.text((224, 1644), "R$139/mês", font=f(34, "black"), fill=NAVY, anchor="mm")


def build_delivery(base: Image.Image, draw: ImageDraw.ImageDraw) -> None:
    logo_bug(base, draw)
    pill(base, draw, "feito para lanchonete, restaurante e delivery", (48, 138), "dark")


def build_stock(base: Image.Image, draw: ImageDraw.ImageDraw) -> None:
    logo_bug(base, draw)
    pill(base, draw, "estoque e financeiro em tempo real", (48, 138), "dark")


def build_print(base: Image.Image, draw: ImageDraw.ImageDraw) -> None:
    logo_bug(base, draw)
    pill(base, draw, "pedido imprime direto na cozinha", (48, 138), "dark")


def build_integrations(base: Image.Image, draw: ImageDraw.ImageDraw) -> None:
    logo_bug(base, draw)
    pill(base, draw, "iFood, WhatsApp e pagamentos integrados", (48, 138), "dark")


def build_final(base: Image.Image, draw: ImageDraw.ImageDraw) -> None:
    draw.rectangle((0, 0, W, H), fill=(*NAVY, 108))
    logo_bug(base, draw)
    bottom_gradient(base, 210)
    draw.rounded_rectangle((58, 1338, 1022, 1734), radius=38, fill=(*NAVY, 235), outline=(*TEAL, 160), width=2)
    draw.text((92, 1396), "Balcão Livre PDV Online", font=f(43, "black"), fill=WHITE)
    draw.text((92, 1460), "para restaurante, bar e delivery", font=f(31, "semi"), fill=MINT)
    draw.line((92, 1518, 988, 1518), fill=(*TEAL, 150), width=2)
    draw.text((92, 1586), "R$139/mês", font=f(59, "black"), fill=WHITE)
    draw.rounded_rectangle((490, 1544, 982, 1642), radius=30, fill=(*TEAL, 255))
    draw.text((736, 1594), "Teste grátis", font=f(39, "black"), fill=NAVY, anchor="mm")
    draw.text((92, 1690), "Chame no WhatsApp e veja funcionando.", font=f(30, "semi"), fill=WHITE)


def build_overlays() -> None:
    save_overlay("intro", build_intro)
    save_overlay("delivery", build_delivery)
    save_overlay("stock", build_stock)
    save_overlay("print", build_print)
    save_overlay("integrations", build_integrations)
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
        (10.99, 12.89, "Esse é o Balcão Livre."),
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
Style: Caption,Segoe UI Semibold,48,&H00FFFFFF,&H00FFFFFF,&HBA1F1400,&HBA1F1400,-1,0,0,0,100,100,0,0,3,15,0,2,76,76,112,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
"""
    lines = [header]
    for start, end, text in captions:
        lines.append(f"Dialogue: 5,{ass_time(start)},{ass_time(end)},Caption,,0,0,0,,{ass_escape(text)}")
    ASS_FILE.write_text("\n".join(lines), encoding="utf-8-sig")


def write_music(duration_s: float = 52.0, sample_rate: int = 44100) -> None:
    # Simple low-volume bed, intentionally subtle so it does not sound like AI narration.
    bpm = 88
    beat = 60.0 / bpm
    total = int(duration_s * sample_rate)
    chords = [
        (110.00, 164.81, 220.00),
        (98.00, 146.83, 196.00),
        (130.81, 164.81, 196.00),
        (116.54, 174.61, 220.00),
    ]

    def env(t: float, length: float) -> float:
        return min(1.0, t / 0.25, max(0.0, (length - t) / 0.45))

    with wave.open(str(MUSIC_FILE), "wb") as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(sample_rate)
        for i in range(total):
            t = i / sample_rate
            bar_len = beat * 4
            chord = chords[int(t / bar_len) % len(chords)]
            local = t % bar_len
            pad = 0.0
            for freq in chord:
                pad += math.sin(2 * math.pi * freq * t) * 0.035
                pad += math.sin(2 * math.pi * freq * 2.0 * t) * 0.012
            pad *= env(local, bar_len)
            pulse = math.sin(2 * math.pi * 55 * (t % beat)) * max(0, 1 - (t % beat) / 0.10) * 0.035
            value = max(-0.75, min(0.75, pad + pulse))
            wf.writeframes(struct.pack("<hh", int(value * 32767), int(value * 0.92 * 32767)))


def fade_overlay(label: str, start: float, end: float, input_idx: int, base: str, out: str) -> str:
    safe = label.replace("-", "_")
    return (
        f"[{input_idx}:v]format=rgba,"
        f"fade=t=in:st={start}:d=0.18:alpha=1,"
        f"fade=t=out:st={max(start, end - 0.18)}:d=0.18:alpha=1[{safe}];"
        f"[{base}][{safe}]overlay=0:0:enable='between(t,{start},{end})'[{out}]"
    )


def run_ffmpeg() -> None:
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    ass_path = ASS_FILE.as_posix().replace(":", "\\:")

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
    overlay_paths = [
        OVERLAYS["intro"],
        OVERLAYS["delivery"],
        OVERLAYS["stock"],
        OVERLAYS["print"],
        OVERLAYS["integrations"],
        OVERLAYS["final"],
    ]
    for overlay in overlay_paths:
        cmd += ["-loop", "1", "-t", "49.76", "-i", str(overlay)]

    filters = [
        "[0:v]scale=1080:1920:force_original_aspect_ratio=increase,crop=1080:1920,"
        "eq=contrast=1.045:saturation=1.04:brightness=0.006,"
        "unsharp=5:5:0.45:3:3:0.18,format=rgba[base0]",
        fade_overlay("intro", 0.00, 2.65, 2, "base0", "base1"),
        fade_overlay("delivery", 4.95, 10.20, 3, "base1", "base2"),
        fade_overlay("stock", 13.55, 20.90, 4, "base2", "base3"),
        fade_overlay("print", 20.90, 25.95, 5, "base3", "base4"),
        fade_overlay("integrations", 26.10, 33.30, 6, "base4", "base5"),
        fade_overlay("final", 43.90, 49.76, 7, "base5", "base6"),
        f"[base6]ass='{ass_path}',format=yuv420p[v]",
        "[0:a]volume=1.10,acompressor=threshold=-21dB:ratio=2.0:attack=16:release=160[voice]",
        "[1:a]volume=0.055,atrim=duration=49.76,afade=t=out:st=47.8:d=1.7[music]",
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
        "fast",
        "-crf",
        "20",
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

    for ss, path in [("6", THUMB_FILE), ("46", CTA_THUMB_FILE)]:
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
