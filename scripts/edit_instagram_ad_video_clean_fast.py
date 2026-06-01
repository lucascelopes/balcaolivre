from __future__ import annotations

import math
import struct
import subprocess
import wave
from pathlib import Path

import imageio_ffmpeg


ROOT = Path(__file__).resolve().parents[1]
INPUT = Path(r"C:\Users\lucas\Downloads\WhatsApp Video 2026-05-30 at 19.35.38.mp4")
OUT_DIR = ROOT / "outputs" / "ads-video-edit"
OUT_DIR.mkdir(parents=True, exist_ok=True)

FINAL_FILE = OUT_DIR / "balcao-livre-ads-instagram-depoimento-natural.mp4"
THUMB_FILE = OUT_DIR / "balcao-livre-ads-instagram-depoimento-natural-thumb.jpg"
CTA_THUMB_FILE = OUT_DIR / "balcao-livre-ads-instagram-depoimento-natural-cta.jpg"
ASS_FILE = OUT_DIR / "balcao_ad_depoimento_natural.ass"
MUSIC_FILE = OUT_DIR / "music_soft_loop.wav"


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


def line(layer: int, start: float, end: float, style: str, text: str, raw: bool = False) -> str:
    payload = text if raw else ass_escape(text)
    return f"Dialogue: {layer},{ass_time(start)},{ass_time(end)},{style},,0,0,0,,{payload}"


def write_ass() -> None:
    # ASS colors are AABBGGRR. Back boxes use dark navy with partial transparency.
    header = r"""[Script Info]
ScriptType: v4.00+
PlayResX: 1080
PlayResY: 1920
ScaledBorderAndShadow: yes

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Caption,Segoe UI Semibold,47,&H00FFFFFF,&H00FFFFFF,&H9A2B1F00,&H9A2B1F00,-1,0,0,0,100,100,0,0,3,14,0,2,76,76,112,1
Style: Bug,Segoe UI Semibold,29,&H00FFFFFF,&H00FFFFFF,&H8A2B1F00,&H8A2B1F00,-1,0,0,0,100,100,0,0,3,16,0,7,42,42,42,1
Style: Pill,Segoe UI Semibold,31,&H00FFFFFF,&H00FFFFFF,&H952B1F00,&H952B1F00,-1,0,0,0,100,100,0,0,3,16,0,7,54,54,126,1
Style: Intro,Segoe UI Black,49,&H00FFFFFF,&H00FFFFFF,&H982B1F00,&H982B1F00,-1,0,0,0,100,100,0,0,3,24,0,7,62,62,320,1
Style: IntroGreen,Segoe UI Black,39,&H00C7D22C,&H00C7D22C,&H982B1F00,&H982B1F00,-1,0,0,0,100,100,0,0,3,18,0,7,62,62,320,1
Style: Final,Segoe UI Black,48,&H00FFFFFF,&H00FFFFFF,&HA02B1F00,&HA02B1F00,-1,0,0,0,100,100,0,0,3,24,0,2,78,78,250,1
Style: FinalGreen,Segoe UI Black,58,&H002BD22C,&H002BD22C,&HA02B1F00,&H00000000,-1,0,0,0,100,100,0,0,1,5,0,2,78,78,170,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
"""

    events = [
        line(0, 0.00, 49.76, "Bug", r"{\an7\pos(58,48)}BL  Balcão Livre PDV", raw=True),
        line(3, 0.10, 2.70, "IntroGreen", r"{\an7\pos(70,1295)}PDV Online para restaurante", raw=True),
        line(3, 0.10, 2.70, "Intro", r"{\an7\pos(70,1368)}Caixa, mesas, delivery\Ne estoque em um sistema só.", raw=True),
        line(3, 0.10, 2.70, "IntroGreen", r"{\an7\pos(70,1532)}R$139/mês", raw=True),
        line(2, 5.00, 10.20, "Pill", r"{\an7\pos(58,142)}Lanchonete, restaurante e delivery", raw=True),
        line(2, 13.55, 20.90, "Pill", r"{\an7\pos(58,142)}Estoque e financeiro em tempo real", raw=True),
        line(2, 20.90, 25.95, "Pill", r"{\an7\pos(58,142)}Pedido imprime direto na cozinha", raw=True),
        line(2, 26.10, 33.30, "Pill", r"{\an7\pos(58,142)}iFood, WhatsApp e pagamentos", raw=True),
        line(2, 39.30, 43.70, "Pill", r"{\an7\pos(58,142)}Gestão simples para o dia a dia", raw=True),
        line(3, 43.90, 49.76, "Final", r"{\an2\pos(540,1374)}Balcão Livre PDV Online\Nrestaurante, bar e delivery", raw=True),
        line(3, 43.90, 49.76, "FinalGreen", r"{\an2\pos(540,1590)}R$139/mês", raw=True),
        line(3, 43.90, 49.76, "Final", r"{\an2\pos(540,1720)}Teste grátis pelo WhatsApp", raw=True),
    ]

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
    ]
    for start, end, text in captions:
        events.append(line(5, start, end, "Caption", text))

    ASS_FILE.write_text(header + "\n".join(events) + "\n", encoding="utf-8-sig")


def write_music(duration_s: float = 52.0, sample_rate: int = 44100) -> None:
    bpm = 88
    beat = 60.0 / bpm
    total = int(duration_s * sample_rate)
    chords = [
        (110.00, 164.81, 220.00),
        (98.00, 146.83, 196.00),
        (130.81, 164.81, 196.00),
        (116.54, 174.61, 220.00),
    ]

    with wave.open(str(MUSIC_FILE), "wb") as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(sample_rate)
        for i in range(total):
            t = i / sample_rate
            bar_len = beat * 4
            chord = chords[int(t / bar_len) % len(chords)]
            local = t % bar_len
            fade = min(1.0, local / 0.25, max(0.0, (bar_len - local) / 0.42))
            pad = sum(math.sin(2 * math.pi * freq * t) * 0.032 for freq in chord) * fade
            pulse = math.sin(2 * math.pi * 55 * (t % beat)) * max(0, 1 - (t % beat) / 0.10) * 0.026
            value = max(-0.7, min(0.7, pad + pulse))
            wf.writeframes(struct.pack("<hh", int(value * 32767), int(value * 0.92 * 32767)))


def run_ffmpeg() -> None:
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    ass_path = ASS_FILE.as_posix().replace(":", "\\:")
    filters = [
        "[0:v]scale=1080:1920:force_original_aspect_ratio=increase,crop=1080:1920,"
        "eq=contrast=1.045:saturation=1.04:brightness=0.006,"
        f"ass='{ass_path}',format=yuv420p[v]",
        "[0:a]volume=1.12,acompressor=threshold=-21dB:ratio=2.0:attack=16:release=160[voice]",
        "[1:a]volume=0.050,atrim=duration=49.76,afade=t=out:st=47.8:d=1.7[music]",
        "[voice][music]amix=inputs=2:duration=first:dropout_transition=2[a]",
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
    write_ass()
    if not MUSIC_FILE.exists():
        write_music()
    run_ffmpeg()
    print(FINAL_FILE)
    print(THUMB_FILE)
    print(CTA_THUMB_FILE)


if __name__ == "__main__":
    main()
