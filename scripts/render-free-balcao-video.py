from __future__ import annotations

import math
import subprocess
import wave
from pathlib import Path

import imageio_ffmpeg
import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "outputs" / "free-video-balcao"
FRAMES = OUT / "frames"
VIDEO_OUT = OUT / "balcao-livre-pdv-reel-com-narracao.mp4"
AUDIO_OUT = OUT / "narracao-balcao-livre.wav"
NARRATION_TXT = OUT / "narracao-balcao-livre.txt"

WIDTH, HEIGHT = 1080, 1920
FPS = 30
TRANSITION_SECONDS = 0.35

NARRATION = """
Seu restaurante ainda vende no improviso? Com o Balcao Livre PDV, o caixa vende rapido no Windows, fecha a conta e emite comprovante sem depender de planilha.

Chegou pedido no iFood? Ele aparece no painel do delivery, toca alerta e a equipe acompanha tudo: novo, preparo, saiu para entrega e entregue.

O garcom usa o celular na rede local, abre a mesa, escolhe os produtos e manda o pedido direto para o caixa.

Mesa, comanda, balcao e delivery ficam no mesmo sistema. Da para controlar consumo, estoque e operacao em tempo real.

E o cliente tambem pode pedir pelo cardapio online com QR Code. Os produtos saem do cadastro do PDV, com preco e disponibilidade atualizados.

Balcao Livre PDV Online: caixa, iFood, garcom web, cardapio digital e controle da loja em um so lugar. Peca uma demonstracao no WhatsApp.
""".strip()


def run_powershell_tts(text: str, output: Path) -> None:
    safe_text = text.replace("'", "''")
    safe_path = str(output).replace("'", "''")
    command = f"""
Add-Type -AssemblyName System.Speech
$s = New-Object System.Speech.Synthesis.SpeechSynthesizer
$s.SelectVoice('Microsoft Maria Desktop')
$s.Rate = 1
$s.Volume = 100
$s.SetOutputToWaveFile('{safe_path}')
$s.Speak('{safe_text}')
$s.Dispose()
"""
    subprocess.run(
        ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command],
        check=True,
        cwd=ROOT,
    )


def wav_duration(path: Path) -> float:
    with wave.open(str(path), "rb") as audio:
        return audio.getnframes() / float(audio.getframerate())


def load_frame(path: Path) -> Image.Image:
    return Image.open(path).convert("RGB").resize((WIDTH, HEIGHT), Image.LANCZOS)


def zoom_frame(img: Image.Image, progress: float) -> Image.Image:
    scale = 1.0 + 0.035 * progress
    w = int(WIDTH * scale)
    h = int(HEIGHT * scale)
    resized = img.resize((w, h), Image.LANCZOS)
    left = (w - WIDTH) // 2
    top = (h - HEIGHT) // 2
    return resized.crop((left, top, left + WIDTH, top + HEIGHT))


def blend(a: Image.Image, b: Image.Image, alpha: float) -> Image.Image:
    return Image.blend(a, b, alpha)


def render_video() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    NARRATION_TXT.write_text(NARRATION + "\n", encoding="utf-8")
    run_powershell_tts(NARRATION, AUDIO_OUT)

    scene_paths = sorted(FRAMES.glob("scene-*.png"))
    if not scene_paths:
        raise RuntimeError(f"Nenhum frame encontrado em {FRAMES}")

    scenes = [load_frame(path) for path in scene_paths]
    audio_seconds = wav_duration(AUDIO_OUT)
    total_seconds = max(36.0, audio_seconds + 0.8)
    scene_seconds = total_seconds / len(scenes)
    scene_frame_count = max(1, int(scene_seconds * FPS))
    transition_frames = int(TRANSITION_SECONDS * FPS)

    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    cmd = [
        ffmpeg,
        "-y",
        "-f",
        "rawvideo",
        "-vcodec",
        "rawvideo",
        "-pix_fmt",
        "rgb24",
        "-s",
        f"{WIDTH}x{HEIGHT}",
        "-r",
        str(FPS),
        "-i",
        "-",
        "-i",
        str(AUDIO_OUT),
        "-c:v",
        "libx264",
        "-preset",
        "medium",
        "-crf",
        "21",
        "-pix_fmt",
        "yuv420p",
        "-c:a",
        "aac",
        "-b:a",
        "128k",
        "-movflags",
        "+faststart",
        str(VIDEO_OUT),
    ]

    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE, cwd=ROOT)
    assert proc.stdin is not None
    try:
        for i, scene_img in enumerate(scenes):
            next_img = scenes[i + 1] if i + 1 < len(scenes) else None
            for frame_index in range(scene_frame_count):
                progress = frame_index / max(1, scene_frame_count - 1)
                frame = zoom_frame(scene_img, progress)
                if next_img is not None and frame_index >= scene_frame_count - transition_frames:
                    alpha = (frame_index - (scene_frame_count - transition_frames)) / max(1, transition_frames)
                    frame = blend(frame, zoom_frame(next_img, 0), min(1.0, max(0.0, alpha)))
                proc.stdin.write(np.asarray(frame, dtype=np.uint8).tobytes())
    finally:
        proc.stdin.close()

    code = proc.wait()
    if code != 0:
        raise RuntimeError(f"ffmpeg falhou com codigo {code}")

    print(f"Video criado: {VIDEO_OUT}")
    print(f"Audio criado: {AUDIO_OUT}")
    print(f"Duracao audio: {audio_seconds:.1f}s")
    print(f"Duracao video alvo: {total_seconds:.1f}s")


if __name__ == "__main__":
    render_video()
