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

ASS_FILE = OUT_DIR / "balcao_ad_legendas.ass"
MUSIC_FILE = OUT_DIR / "music_soft_loop.wav"
FINAL_FILE = OUT_DIR / "balcao-livre-ads-instagram-editado.mp4"
THUMB_FILE = OUT_DIR / "balcao-livre-ads-thumb.jpg"


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
    subtitles = [
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

    info_cards = [
        (0.00, 2.70, "PDV Online para restaurante"),
        (6.00, 11.80, "Caixa • mesas • delivery"),
        (13.50, 21.20, "Estoque + financeiro em tempo real"),
        (22.00, 33.20, "Cozinha, iFood e pagamentos integrados"),
        (39.00, 49.40, "Plano por R$139/mês"),
    ]

    cta_cards = [
        (0.00, 49.40, "Balcão Livre PDV Online"),
        (44.00, 49.40, "Chame no WhatsApp e teste grátis"),
    ]

    header = """[Script Info]
ScriptType: v4.00+
PlayResX: 1080
PlayResY: 1920
ScaledBorderAndShadow: yes

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Caption,Segoe UI Semibold,58,&H00FFFFFF,&H00FFFFFF,&HAA2B1F00,&HAA2B1F00,-1,0,0,0,100,100,0,0,3,20,0,2,70,70,190,1
Style: Info,Segoe UI Semibold,43,&H00FFFFFF,&H00FFFFFF,&H002B1F00,&HAA2B1F00,-1,0,0,0,100,100,0,0,3,22,0,8,55,55,160,1
Style: Brand,Segoe UI Black,36,&H00FFFFFF,&H00FFFFFF,&H002B1F00,&HAA2B1F00,-1,0,0,0,100,100,0,0,3,18,0,7,50,50,55,1
Style: Cta,Segoe UI Black,45,&H002B1F00,&H002B1F00,&H00C7D22C,&H44C7D22C,-1,0,0,0,100,100,0,0,3,18,0,2,70,70,80,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
"""

    lines = [header]
    for start, end, text in cta_cards[:1]:
        lines.append(
            f"Dialogue: 0,{ass_time(start)},{ass_time(end)},Brand,,0,0,0,,{ass_escape(text)}"
        )
    for start, end, text in info_cards:
        lines.append(
            f"Dialogue: 1,{ass_time(start)},{ass_time(end)},Info,,0,0,0,,{ass_escape(text)}"
        )
    for start, end, text in subtitles:
        lines.append(
            f"Dialogue: 2,{ass_time(start)},{ass_time(end)},Caption,,0,0,0,,{ass_escape(text)}"
        )
    for start, end, text in cta_cards[1:]:
        lines.append(
            f"Dialogue: 3,{ass_time(start)},{ass_time(end)},Cta,,0,0,0,,{ass_escape(text)}"
        )

    ASS_FILE.write_text("\n".join(lines), encoding="utf-8-sig")


def write_music(duration_s: float = 55.0, sample_rate: int = 44100) -> None:
    """Generate a subtle royalty-free background bed."""
    bpm = 94
    beat = 60.0 / bpm
    total = int(duration_s * sample_rate)

    chords = [
        (130.81, 164.81, 196.00),  # C minor-ish bed
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
            for f in chord:
                pad += math.sin(2 * math.pi * f * t) * 0.045
                pad += math.sin(2 * math.pi * (f * 2.0) * t) * 0.018
            pad *= env(local, 0.08, 0.45, beat * 4)

            kick_phase = t % beat
            kick = 0.0
            if kick_phase < 0.11 and int(t / beat) % 4 in (0, 2):
                kick = math.sin(2 * math.pi * (58 - kick_phase * 230) * t) * (1 - kick_phase / 0.11) * 0.12

            hat_phase = (t + beat / 2) % beat
            hat = 0.0
            if hat_phase < 0.035:
                noise = ((i * 1103515245 + 12345) & 0xFFFF) / 0xFFFF - 0.5
                hat = noise * (1 - hat_phase / 0.035) * 0.045

            sample = max(-1.0, min(1.0, pad + kick + hat))
            left = int(sample * 32767)
            right = int(sample * 32767)
            wf.writeframes(struct.pack("<hh", left, right))


def run_ffmpeg() -> None:
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    ass_path = ASS_FILE.as_posix().replace(":", "\\:")
    video_filter = (
        "scale=1080:1920:force_original_aspect_ratio=increase,"
        "crop=1080:1920,"
        "eq=contrast=1.08:saturation=1.08:brightness=0.015,"
        f"ass='{ass_path}'"
    )

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
            (
                f"[0:v]{video_filter}[v];"
                "[0:a]volume=1.18,acompressor=threshold=-20dB:ratio=2.2:attack=18:release=180[voice];"
                "[1:a]volume=0.095,atrim=duration=49.76,afade=t=out:st=47.5:d=2.0[music];"
                "[voice][music]amix=inputs=2:duration=first:dropout_transition=2[a]"
            ),
            "-map",
            "[v]",
            "-map",
            "[a]",
            "-c:v",
            "libx264",
            "-preset",
            "medium",
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

    subprocess.run(
        [
            ffmpeg,
            "-y",
            "-ss",
            "8",
            "-i",
            str(FINAL_FILE),
            "-frames:v",
            "1",
            "-update",
            "1",
            str(THUMB_FILE),
        ],
        check=True,
    )


def main() -> None:
    write_ass()
    write_music()
    run_ffmpeg()
    print(FINAL_FILE)
    print(THUMB_FILE)


if __name__ == "__main__":
    main()
