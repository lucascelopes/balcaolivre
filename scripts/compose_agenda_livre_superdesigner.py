from __future__ import annotations

import json
import math
import shutil
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "outputs" / "logos" / "agenda-livre-superdesigner-cobalto"
GENERATED = OUT / "generated"
SYMBOLS = GENERATED / "symbols"
LOCKUPS = GENERATED / "lockups"
ICONS = GENERATED / "icons"
TESTS = GENERATED / "tests"
THUMBS = GENERATED / "mcp-thumbs"

for folder in (SYMBOLS, LOCKUPS, ICONS, TESTS, THUMBS):
    folder.mkdir(parents=True, exist_ok=True)

BLUE = "#0057C8"
BLUE_DARK = "#0049A8"
INK = "#172033"
SOFT = "#EAF1FF"
MUTED = "#68758A"
LINE = "#D9E2F0"
WHITE = "#FFFFFF"
TRANSPARENT = (0, 0, 0, 0)

FONT_DIR = Path(r"C:\Windows\Fonts")
FONTS = {
    "regular": FONT_DIR / "segoeui.ttf",
    "semibold": FONT_DIR / "seguisb.ttf",
    "bold": FONT_DIR / "segoeuib.ttf",
    "variable": FONT_DIR / "SegUIVar.ttf",
    "bahnschrift": FONT_DIR / "bahnschrift.ttf",
    "candara": FONT_DIR / "Candara.ttf",
    "candara_bold": FONT_DIR / "Candarab.ttf",
    "corbel": FONT_DIR / "corbel.ttf",
    "corbel_bold": FONT_DIR / "corbelb.ttf",
}


@dataclass(frozen=True)
class Concept:
    id: str
    number: str
    label: str
    family: str
    best: str


CONCEPTS = [
    Concept("01-faixa-livre", "01", "Faixa Livre", "wordmark proprietário", "organização que se abre em capacidade"),
    Concept("02-g-de-janela", "02", "G de Janela", "glifo tipográfico", "ícone ligado diretamente ao nome"),
    Concept("03-controle-respiro", "03", "Controle → Respiro", "wordmark adaptativo", "densidade que vira espaço livre"),
    Concept("04-coordenada-aberta", "04", "Coordenada Aberta", "sistema geométrico", "tempo e profissional deixam um vão útil"),
    Concept("05-dois-tempos", "05", "Dois Tempos", "humanista relacional", "cliente e profissional compartilham o momento"),
    Concept("06-modulo-30", "06", "Módulo 30", "utilitário modular", "a janela de 30 minutos vira unidade de marca"),
    Concept("07-desvio-livre", "07", "Desvio Livre", "tecnológico calmo", "o fluxo contorna o horário aberto sem quebrar"),
    Concept("08-monolito-do-vao", "08", "Monólito do Vão", "minimalismo icônico", "máxima força com uma única contraforma"),
    Concept("09-assinatura-precisa", "09", "Assinatura Precisa", "tipografia humanista", "proximidade humana com precisão operacional"),
    Concept("10-quatro-para-um", "10", "Quatro para Um", "sistema coordenado", "quatro elementos convergem em uma janela"),
]


def font(name: str, size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONTS[name]), size=size)


def rgba(hex_color: str, alpha: int = 255) -> tuple[int, int, int, int]:
    value = hex_color.lstrip("#")
    return (int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16), alpha)


def cubic(p0, p1, p2, p3, steps=48):
    points = []
    for i in range(steps + 1):
        t = i / steps
        u = 1 - t
        points.append((
            u**3 * p0[0] + 3 * u**2 * t * p1[0] + 3 * u * t**2 * p2[0] + t**3 * p3[0],
            u**3 * p0[1] + 3 * u**2 * t * p1[1] + 3 * u * t**2 * p2[1] + t**3 * p3[1],
        ))
    return points


def scale_points(points, s):
    return [(round(x * s), round(y * s)) for x, y in points]


def symbol_canvas(concept_id: str, size: int = 512) -> Image.Image:
    s = 4
    image = Image.new("RGBA", (512 * s, 512 * s), TRANSPARENT)
    d = ImageDraw.Draw(image)

    def box(coords):
        return tuple(round(v * s) for v in coords)

    def rr(coords, radius, fill):
        d.rounded_rectangle(box(coords), radius=round(radius * s), fill=rgba(fill))

    def poly(points, fill):
        d.polygon(scale_points(points, s), fill=rgba(fill))

    def line(points, fill, width, joint="curve"):
        d.line(scale_points(points, s), fill=rgba(fill), width=round(width * s), joint=joint)

    if concept_id == "01-faixa-livre":
        top = cubic((78, 118), (78, 78), (111, 56), (151, 56))
        top += [(388, 56)]
        top += cubic((388, 56), (424, 56), (442, 77), (442, 108))[1:]
        top += [(442, 183), (390, 171), (341, 168), (292, 176), (178, 196)]
        top += cubic((178, 196), (133, 204), (101, 227), (78, 251))[1:]
        poly(top, BLUE)
        bottom = cubic((78, 394), (78, 434), (111, 456), (151, 456))
        bottom += [(388, 456)]
        bottom += cubic((388, 456), (424, 456), (442, 435), (442, 404))[1:]
        bottom += [(442, 329), (390, 341), (341, 344), (292, 336), (178, 316)]
        bottom += cubic((178, 316), (133, 308), (101, 285), (78, 261))[1:]
        poly(bottom, INK)
        rr((167, 226, 345, 286), 30, BLUE)

    elif concept_id == "02-g-de-janela":
        rr((58, 54, 454, 458), 116, BLUE)
        rr((166, 160, 350, 352), 54, "#000000")
        # Punch the counters directly back to transparency.
        d.rounded_rectangle(box((166, 160, 350, 352)), radius=54 * s, fill=TRANSPARENT)
        d.rounded_rectangle(box((314, 214, 480, 308)), radius=24 * s, fill=TRANSPARENT)
        poly([(275, 250), (410, 250), (410, 346), (352, 404), (277, 404), (277, 329), (334, 329), (334, 307), (275, 307)], INK)

    elif concept_id == "03-controle-respiro":
        rr((50, 54, 478, 458), 76, BLUE)
        d.rectangle(box((158, 54, 196, 315)), fill=TRANSPARENT)
        d.rectangle(box((246, 54, 316, 292)), fill=TRANSPARENT)
        d.rectangle(box((382, 54, 478, 254)), fill=TRANSPARENT)
        d.rectangle(box((50, 354, 478, 458)), fill=rgba(INK))

    elif concept_id == "04-coordenada-aberta":
        poly([(58, 66), (326, 66), (326, 148), (148, 148), (148, 292), (58, 292)], BLUE)
        poly([(454, 446), (186, 446), (186, 364), (364, 364), (364, 220), (454, 220)], INK)
        rr((186, 220, 326, 292), 22, BLUE_DARK)
        d.rectangle(box((326, 292, 364, 364)), fill=rgba(SOFT))

    elif concept_id == "05-dois-tempos":
        top = cubic((66, 130), (170, 130), (172, 238), (238, 256))
        top += [(294, 256)]
        top += cubic((294, 256), (358, 238), (362, 130), (446, 130))[1:]
        line(top, INK, 78)
        bottom = cubic((66, 382), (170, 382), (172, 274), (238, 256))
        bottom += [(294, 256)]
        bottom += cubic((294, 256), (358, 274), (362, 382), (446, 382))[1:]
        line(bottom, BLUE, 78)
        rr((226, 216, 306, 296), 24, BLUE_DARK)

    elif concept_id == "06-modulo-30":
        rr((126, 58, 426, 166), 34, BLUE)
        rr((58, 202, 358, 310), 34, INK)
        rr((126, 346, 426, 454), 34, BLUE)
        rr((358, 202, 426, 310), 30, SOFT)

    elif concept_id == "07-desvio-livre":
        line([(58, 330), (166, 330), (166, 154), (346, 154), (346, 330), (454, 330)], BLUE, 82)
        rr((58, 289, 140, 371), 41, INK)
        rr((372, 289, 454, 371), 41, BLUE_DARK)

    elif concept_id == "08-monolito-do-vao":
        rr((74, 52, 438, 460), 38, BLUE)
        # Distinct asymmetric shoulder and a single internal slot.
        poly([(74, 324), (210, 460), (112, 460), (74, 424)], INK)
        d.rounded_rectangle(box((184, 188, 394, 276)), radius=36 * s, fill=TRANSPARENT)

    elif concept_id == "09-assinatura-precisa":
        outer = cubic((354, 170), (326, 96), (223, 72), (143, 122))
        outer += cubic((143, 122), (67, 169), (60, 286), (126, 352))[1:]
        outer += cubic((126, 352), (190, 416), (302, 391), (348, 314))[1:]
        outer += cubic((348, 314), (381, 258), (348, 199), (290, 194))[1:]
        outer += cubic((290, 194), (238, 189), (201, 225), (202, 270))[1:]
        outer += cubic((202, 270), (203, 313), (243, 340), (284, 320))[1:]
        line(outer, BLUE, 72)
        tail = cubic((348, 313), (396, 354), (373, 428), (294, 448))
        line(tail, BLUE, 72)
        rr((245, 411, 357, 475), 26, INK)

    elif concept_id == "10-quatro-para-um":
        line([(74, 102), (204, 220)], INK, 74)
        line([(438, 126), (308, 220)], BLUE, 74)
        line([(88, 420), (204, 292)], BLUE, 74)
        line([(426, 394), (308, 292)], INK, 74)
        rr((204, 220, 308, 292), 24, SOFT)

    return image.resize((size, size), Image.Resampling.LANCZOS)


def draw_wordmark(canvas: Image.Image, concept: Concept) -> None:
    d = ImageDraw.Draw(canvas)
    x = 418
    if concept.id == "01-faixa-livre":
        d.text((x, 180), "AGENDA", font=font("bold", 84), fill=rgba(INK))
        d.text((x + 2, 278), "LIVRE", font=font("semibold", 72), fill=rgba(BLUE), stroke_width=0)
        d.rounded_rectangle((x + 4, 373, x + 178, 383), radius=5, fill=rgba(BLUE))
    elif concept.id == "02-g-de-janela":
        f1 = font("semibold", 94)
        d.text((x, 230), "Agenda", font=f1, fill=rgba(INK))
        w = d.textlength("Agenda", font=f1)
        d.text((x + w + 15, 230), "Livre", font=font("regular", 94), fill=rgba(BLUE))
    elif concept.id == "03-controle-respiro":
        f1 = font("bahnschrift", 96)
        d.text((x, 230), "agenda", font=f1, fill=rgba(INK))
        w = d.textlength("agenda", font=f1)
        d.text((x + w + 35, 239), "l i v r e", font=font("regular", 78), fill=rgba(BLUE))
    elif concept.id == "04-coordenada-aberta":
        f = font("semibold", 90)
        d.text((x, 230), "Agenda Livre", font=f, fill=rgba(INK))
        d.rounded_rectangle((x + 2, 347, x + 262, 355), radius=4, fill=rgba(BLUE))
        d.rounded_rectangle((x + 286, 347, x + 370, 355), radius=4, fill=rgba(SOFT))
    elif concept.id == "05-dois-tempos":
        f = font("candara_bold", 100)
        d.text((x, 225), "agenda", font=f, fill=rgba(INK))
        w = d.textlength("agenda", font=f)
        d.text((x + w + 12, 225), "livre", font=f, fill=rgba(BLUE))
    elif concept.id == "06-modulo-30":
        f = font("bahnschrift", 86)
        d.text((x, 170), "AGENDA", font=f, fill=rgba(INK))
        d.text((x, 280), "L I V R E", font=f, fill=rgba(BLUE))
    elif concept.id == "07-desvio-livre":
        f1 = font("corbel_bold", 100)
        d.text((x, 225), "Agenda", font=f1, fill=rgba(INK))
        w = d.textlength("Agenda", font=f1)
        d.text((x + w + 15, 225), "Livre", font=font("corbel", 100), fill=rgba(BLUE))
    elif concept.id == "08-monolito-do-vao":
        f = font("bold", 94)
        d.text((x, 225), "Agenda", font=f, fill=rgba(INK))
        w = d.textlength("Agenda", font=f)
        d.text((x + w + 12, 225), "Livre", font=f, fill=rgba(BLUE))
    elif concept.id == "09-assinatura-precisa":
        f1 = font("variable", 100)
        d.text((x, 226), "agenda", font=f1, fill=rgba(INK))
        w = d.textlength("agenda", font=f1)
        d.text((x + w + 15, 226), "livre", font=font("regular", 100), fill=rgba(BLUE))
    else:
        f = font("semibold", 92)
        d.text((x, 230), "Agenda Livre", font=f, fill=rgba(INK))
        d.rounded_rectangle((x + 3, 352, x + 87, 362), radius=5, fill=rgba(INK))
        d.rounded_rectangle((x + 101, 352, x + 225, 362), radius=5, fill=rgba(BLUE))
        d.rounded_rectangle((x + 239, 352, x + 297, 362), radius=5, fill=rgba(INK))


def make_lockup(concept: Concept) -> Image.Image:
    scale = 2
    canvas = Image.new("RGBA", (1200 * scale, 560 * scale), rgba(WHITE))
    symbol = symbol_canvas(concept.id, 334 * scale)
    canvas.alpha_composite(symbol, (70 * scale, 113 * scale))
    temp = Image.new("RGBA", (1200, 560), TRANSPARENT)
    draw_wordmark(temp, concept)
    temp = temp.resize(canvas.size, Image.Resampling.LANCZOS)
    canvas.alpha_composite(temp)
    return canvas.resize((1200, 560), Image.Resampling.LANCZOS).convert("RGB")


def make_icon(symbol: Image.Image) -> Image.Image:
    icon = Image.new("RGBA", (512, 512), rgba(SOFT))
    d = ImageDraw.Draw(icon)
    d.rounded_rectangle((1, 1, 510, 510), radius=112, outline=rgba("#CFE0F8"), width=3)
    resized = symbol.resize((326, 326), Image.Resampling.LANCZOS)
    icon.alpha_composite(resized, (93, 93))
    return icon.convert("RGB")


def make_card(concept: Concept) -> Image.Image:
    card = Image.new("RGB", (850, 450), WHITE)
    d = ImageDraw.Draw(card)
    d.rounded_rectangle((1, 1, 848, 448), radius=18, fill=WHITE, outline=LINE, width=2)
    d.line((30, 305, 820, 305), fill="#EDF2F8", width=1)

    lockup = Image.open(LOCKUPS / f"{concept.id}-lockup.png").convert("RGB")
    lockup.thumbnail((790, 280), Image.Resampling.LANCZOS)
    card.paste(lockup, (30 + (790 - lockup.width) // 2, 14 + (280 - lockup.height) // 2))

    icon = Image.open(ICONS / f"{concept.id}-icon.png").resize((72, 72), Image.Resampling.LANCZOS)
    card.paste(icon, (34, 335))
    d.text((124, 337), "TESTE REAL", font=font("bold", 13), fill=MUTED)

    for i, size in enumerate((16, 24, 32, 48)):
        micro = Image.open(TESTS / f"{concept.id}-{size}.png").convert("RGB")
        cx = (175, 235, 300, 375)[i]
        card.paste(micro, (round(cx - size / 2), round(382 - size / 2)))
        label = f"{size}px"
        label_w = d.textlength(label, font=font("regular", 11))
        d.text((cx - label_w / 2, 422), label, font=font("regular", 11), fill=MUTED)

    d.text((470, 326), concept.number, font=font("bold", 18), fill=BLUE)
    d.text((510, 322), concept.label, font=font("bold", 24), fill=INK)
    d.text((470, 359), concept.family, font=font("semibold", 15), fill=MUTED)
    d.text((470, 390), concept.best, font=font("regular", 14), fill=MUTED)
    return card


def main() -> None:
    manifest_items = []
    for concept in CONCEPTS:
        symbol = symbol_canvas(concept.id)
        symbol_path = SYMBOLS / f"{concept.id}-symbol.png"
        symbol.save(symbol_path)

        lockup = make_lockup(concept)
        lockup_path = LOCKUPS / f"{concept.id}-lockup.png"
        lockup.save(lockup_path, optimize=True)
        shutil.copyfile(lockup_path, GENERATED / f"{concept.id}.png")

        icon = make_icon(symbol)
        icon_path = ICONS / f"{concept.id}-icon.png"
        icon.save(icon_path, optimize=True)

        for size in (16, 24, 32, 48):
            micro = symbol.resize((size, size), Image.Resampling.LANCZOS)
            bg = Image.new("RGBA", (size, size), rgba(WHITE))
            bg.alpha_composite(micro)
            bg.convert("RGB").save(TESTS / f"{concept.id}-{size}.png", optimize=True)

        thumb = Image.new("RGB", (720, 360), WHITE)
        fitted = lockup.copy()
        fitted.thumbnail((700, 336), Image.Resampling.LANCZOS)
        thumb.paste(fitted, ((720 - fitted.width) // 2, (360 - fitted.height) // 2))
        thumb.save(THUMBS / f"{concept.id}.jpg", quality=90, subsampling=0)

        manifest_items.append({
            "id": concept.id,
            "title": f"{concept.number}. {concept.label}",
            "family": concept.family,
            "description": concept.best,
            "image": f"generated/{concept.id}.png",
            "imageUrl": f"generated/{concept.id}.png",
            "previewImageUrl": f"generated/mcp-thumbs/{concept.id}.jpg",
            "sourceImageUrl": f"generated/lockups/{concept.id}-lockup.png",
            "remixSuggestions": [
                "Refinar esta direção com menos detalhes",
                "Explorar uma versão mais tipográfica",
                "Testar uma versão monocromática para favicon",
            ],
        })

    sheet = Image.new("RGB", (1800, 2450), "#F7F9FC")
    d = ImageDraw.Draw(sheet)
    d.text((50, 40), "Agenda Livre — nova direção autoral", font=font("bold", 34), fill=INK)
    d.text((50, 84), "10 conceitos baseados na janela livre de 30 minutos · azul-cobalto original do aplicativo", font=font("regular", 17), fill=MUTED)
    for index, concept in enumerate(CONCEPTS):
        card = make_card(concept)
        x = 50 + (index % 2) * 875
        y = 130 + (index // 2) * 460
        sheet.paste(card, (x, y))
    sheet.save(OUT / "offer-contact-sheet.png", optimize=True)

    data = OUT / "data"
    data.mkdir(parents=True, exist_ok=True)
    manifest = {
        "meta": {
            "title": "Agenda Livre — nova direção autoral",
            "subtitle": "10 conceitos na paleta azul-cobalto original",
            "itemCount": len(manifest_items),
            "preset": "image-wall",
        },
        "items": manifest_items,
    }
    (data / "review-manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"concepts": len(CONCEPTS), "contactSheet": str(OUT / "offer-contact-sheet.png")}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
