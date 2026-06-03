from __future__ import annotations

import subprocess
from pathlib import Path

import imageio_ffmpeg


ROOT = Path(__file__).resolve().parents[1]
INPUT = Path(r"C:\Users\lucas\Downloads\WhatsApp Video 2026-05-30 at 19.35.38.mp4")
OUT_DIR = ROOT / "outputs" / "ads-video-edit"
OUT_DIR.mkdir(parents=True, exist_ok=True)

FINAL_FILE = OUT_DIR / "balcao-livre-ads-instagram-corte-ugc-25s.mp4"
THUMB_FILE = OUT_DIR / "balcao-livre-ads-instagram-corte-ugc-25s-thumb.jpg"
CTA_THUMB_FILE = OUT_DIR / "balcao-livre-ads-instagram-corte-ugc-25s-cta.jpg"
ASS_FILE = OUT_DIR / "balcao_ad_ugc_short.ass"
MUSIC_FILE = OUT_DIR / "music_soft_loop.wav"


SEGMENTS = [
    (5.70, 10.11),
    (10.99, 12.89),
    (13.73, 17.80),
    (20.93, 24.70),
    (26.20, 30.70),
    (39.47, 43.00),
    (44.10, 47.80),
]


def ass_time(seconds: float) -> str:
    centis = int(round(seconds * 100))
    h = centis // 360000
    centis %= 360000
    m = centis // 6000
    centis %= 6000
    s = centis // 100
    cs = centis % 100
    return f"{h}:{m:02d}:{s:02d}.{cs:02d}"


def dialogue(layer: int, start: float, end: float, style: str, text: str) -> str:
    return f"Dialogue: {layer},{ass_time(start)},{ass_time(end)},{style},,0,0,0,,{text}"


def write_ass() -> None:
    header = """[Script Info]
ScriptType: v4.00+
PlayResX: 1080
PlayResY: 1920
ScaledBorderAndShadow: yes

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Logo,Segoe UI Semibold,28,&H00FFFFFF,&H00FFFFFF,&H902B1F00,&H902B1F00,-1,0,0,0,100,100,0,0,3,15,0,7,42,42,38,1
Style: Hook,Segoe UI Black,50,&H00FFFFFF,&H00FFFFFF,&H9A2B1F00,&H9A2B1F00,-1,0,0,0,100,100,0,0,3,20,0,8,54,54,126,1
Style: HookGreen,Segoe UI Black,44,&H002CD22C,&H002CD22C,&H8A2B1F00,&H8A2B1F00,-1,0,0,0,100,100,0,0,3,16,0,8,54,54,196,1
Style: Caption,Segoe UI Semibold,48,&H00FFFFFF,&H00FFFFFF,&HAA2B1F00,&HAA2B1F00,-1,0,0,0,100,100,0,0,3,15,0,2,70,70,112,1
Style: CTA,Segoe UI Black,52,&H00FFFFFF,&H00FFFFFF,&HA02B1F00,&HA02B1F00,-1,0,0,0,100,100,0,0,3,22,0,2,74,74,210,1
Style: Price,Segoe UI Black,62,&H002CD22C,&H002CD22C,&H902B1F00,&H902B1F00,-1,0,0,0,100,100,0,0,1,4,0,2,74,74,350,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
"""
    events = [
        dialogue(0, 0.00, 25.88, "Logo", "BL  Balcão Livre PDV"),
        dialogue(3, 0.00, 2.60, "Hook", "PDV para restaurante, bar e delivery"),
        dialogue(3, 0.00, 2.60, "HookGreen", "R$139/mês"),
        dialogue(4, 0.20, 4.30, "Caption", "Programa completo para lanchonete, restaurante e delivery."),
        dialogue(4, 4.45, 6.25, "Caption", "Esse é o Balcão Livre."),
        dialogue(4, 6.35, 10.40, "Caption", "Estoque, financeiro e vendas em tempo real."),
        dialogue(4, 10.55, 14.15, "Caption", "Pedido direto na cozinha, sem retrabalho."),
        dialogue(4, 14.30, 18.75, "Caption", "Integração com iFood, maquininha e Mercado Pago."),
        dialogue(4, 18.95, 22.20, "Caption", "Gestão mais simples para o dia a dia."),
        dialogue(5, 22.30, 25.88, "CTA", "Teste grátis pelo WhatsApp"),
        dialogue(5, 22.30, 25.88, "Price", "Balcão Livre PDV Online"),
    ]
    ASS_FILE.write_text(header + "\n".join(events) + "\n", encoding="utf-8-sig")


def run_ffmpeg() -> None:
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    ass_path = ASS_FILE.as_posix().replace(":", "\\:")

    pieces: list[str] = []
    concat_inputs: list[str] = []
    for i, (start, end) in enumerate(SEGMENTS):
        pieces.append(f"[0:v]trim=start={start}:end={end},setpts=PTS-STARTPTS[v{i}]")
        pieces.append(f"[0:a]atrim=start={start}:end={end},asetpts=PTS-STARTPTS[a{i}]")
        concat_inputs.append(f"[v{i}][a{i}]")

    filters = pieces + [
        f"{''.join(concat_inputs)}concat=n={len(SEGMENTS)}:v=1:a=1[vcat][acat]",
        "[vcat]scale=1080:1920:force_original_aspect_ratio=increase,crop=1080:1920,"
        "eq=contrast=1.06:saturation=1.05:brightness=0.006,"
        f"ass='{ass_path}',format=yuv420p[v]",
        "[acat]volume=1.14,acompressor=threshold=-22dB:ratio=2.2:attack=14:release=150[voice]",
        "[1:a]volume=0.040,atrim=duration=25.88,afade=t=out:st=24.2:d=1.4[music]",
        "[voice][music]amix=inputs=2:duration=first:dropout_transition=1[a]",
    ]

    subprocess.run(
        [
            ffmpeg,
            "-y",
            "-i",
            str(INPUT),
            "-stream_loop",
            "-1",
            "-i",
            str(MUSIC_FILE),
            "-filter_complex",
            ";".join(filters),
            "-map",
            "[v]",
            "-map",
            "[a]",
            "-c:v",
            "libx264",
            "-preset",
            "veryfast",
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
        ],
        check=True,
    )

    for ss, path in [("1.6", THUMB_FILE), ("23.3", CTA_THUMB_FILE)]:
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
    write_ass()
    run_ffmpeg()
    print(FINAL_FILE)


if __name__ == "__main__":
    main()
