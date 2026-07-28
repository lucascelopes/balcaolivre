from __future__ import annotations

import json
import math
import shutil
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "outputs" / "logos" / "agenda-livre-superdesigner-final"
GENERATED = OUT / "generated"
SYMBOLS = GENERATED / "symbols"
LOCKUPS = GENERATED / "lockups"
ICONS = GENERATED / "icons"
TESTS = GENERATED / "tests"
THUMBS = GENERATED / "mcp-thumbs"
for folder in (SYMBOLS, LOCKUPS, ICONS, TESTS, THUMBS):
    folder.mkdir(parents=True, exist_ok=True)

BLUE = "#0057C8"
INK = "#172033"
SOFT = "#EAF1FF"
WHITE = "#FFFFFF"
MUTED = "#68758A"
LINE = "#D9E2F0"
TRANSPARENT = (0, 0, 0, 0)

FONT_DIR = Path(r"C:\Windows\Fonts")
FONT_PATHS = {
    "regular": FONT_DIR / "segoeui.ttf",
    "semibold": FONT_DIR / "seguisb.ttf",
    "bold": FONT_DIR / "segoeuib.ttf",
    "variable": FONT_DIR / "SegUIVar.ttf",
    "bahnschrift": FONT_DIR / "bahnschrift.ttf",
    "candara": FONT_DIR / "Candara.ttf",
    "candara_bold": FONT_DIR / "Candarab.ttf",
    "sitka_italic": FONT_DIR / "SitkaVF-Italic.ttf",
}


@dataclass(frozen=True)
class Concept:
    id: str
    number: str
    label: str
    family: str
    best: str


CONCEPTS = [
    Concept("01-g-de-janela", "01", "G de Janela", "glifo tipográfico", "a janela livre nasce dentro do nome"),
    Concept("02-coordenada-aberta", "02", "Coordenada Aberta", "sistema geométrico", "tempo e profissional definem o mesmo vão"),
    Concept("03-encontro-marcado", "03", "Encontro Marcado", "símbolo humanista", "duas agendas viram um atendimento"),
    Concept("04-intervalo-vivo", "04", "Intervalo Vivo", "gesto orgânico", "o horário livre vira espaço em movimento"),
]

REVIEW_DETAILS = {
    "01-g-de-janela": {
        "routeName": "Assinatura",
        "best_for": "Ícone do app, favicon e assinatura principal.",
        "decision_advice": "Escolha se a marca precisa ser imediatamente nomeável e memorável.",
        "watch_out": "O G deve continuar claramente diferente de identidades de big tech.",
    },
    "02-coordenada-aberta": {
        "routeName": "Sistema",
        "best_for": "Produto digital, interface e motion.",
        "decision_advice": "Escolha se precisão e arquitetura do produto devem liderar.",
        "watch_out": "Os encaixes não podem parecer recorte, QR ou moldura de câmera.",
    },
    "03-encontro-marcado": {
        "routeName": "Humana",
        "best_for": "Onboarding, comunicação e negócios locais.",
        "decision_advice": "Escolha se o encontro humano deve ser o centro emocional.",
        "watch_out": "A linha compartilhada não pode parecer sofá ou banco.",
    },
    "04-intervalo-vivo": {
        "routeName": "Expressiva",
        "best_for": "Marca institucional, campanhas e experiências de onboarding.",
        "decision_advice": "Escolha se movimento, leveza e personalidade devem liderar.",
        "watch_out": "O intervalo negativo precisa continuar aberto nas reduções extremas.",
    },
}


def rgba(value: str, alpha: int = 255):
    value = value.lstrip("#")
    return (int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16), alpha)


def font(name: str, size: int):
    return ImageFont.truetype(str(FONT_PATHS[name]), size=size)


def cubic(p0, p1, p2, p3, steps=56):
    points = []
    for i in range(steps + 1):
        t = i / steps
        u = 1 - t
        points.append((
            u**3 * p0[0] + 3 * u**2 * t * p1[0] + 3 * u * t**2 * p2[0] + t**3 * p3[0],
            u**3 * p0[1] + 3 * u**2 * t * p1[1] + 3 * u * t**2 * p2[1] + t**3 * p3[1],
        ))
    return points


def symbol(concept_id: str, color: str = INK, size: int = 512) -> Image.Image:
    s = 4
    image = Image.new("RGBA", (512 * s, 512 * s), TRANSPARENT)
    d = ImageDraw.Draw(image)

    def b(coords):
        return tuple(round(v * s) for v in coords)

    def rr(coords, radius, fill=color):
        d.rounded_rectangle(b(coords), radius=round(radius * s), fill=rgba(fill))

    def polygon(points, fill=color):
        d.polygon([(round(x * s), round(y * s)) for x, y in points], fill=rgba(fill))

    def line(points, width, fill=color):
        d.line([(round(x * s), round(y * s)) for x, y in points], fill=rgba(fill), width=round(width * s), joint="curve")

    if concept_id == "01-g-de-janela":
        # One uninterrupted grid-built G. The bar is part of the same mass.
        rr((60, 58, 452, 454), 94)
        d.rounded_rectangle(b((158, 154, 354, 352)), radius=50 * s, fill=TRANSPARENT)
        d.rounded_rectangle(b((306, 190, 512, 270)), radius=16 * s, fill=TRANSPARENT)
        d.rectangle(b((276, 270, 428, 332)), fill=rgba(color))
        d.rectangle(b((382, 270, 452, 370)), fill=rgba(color))

    elif concept_id == "02-coordenada-aberta":
        # Equal-weight diagonal brackets. Their terminals define a 156 x 72 slot.
        polygon([(64, 70), (336, 70), (336, 150), (150, 150), (150, 220), (64, 220)])
        polygon([(448, 442), (176, 442), (176, 362), (362, 362), (362, 292), (448, 292)])

    elif concept_id == "03-encontro-marcado":
        # Client and professional become one shared service line.
        d.ellipse(b((104, 66, 190, 152)), fill=rgba(color))
        d.ellipse(b((326, 66, 412, 152)), fill=rgba(color))
        meeting = [(82, 190), (82, 252)]
        meeting += cubic((82, 252), (82, 324), (138, 350), (208, 350))[1:]
        meeting += [(304, 350)]
        meeting += cubic((304, 350), (378, 350), (430, 382), (430, 448))[1:]
        line(meeting, 78)

    elif concept_id == "04-intervalo-vivo":
        # Two schedules become one expressive system; the white corridor is the free interval.
        upper = [(62, 116)]
        upper += cubic((62, 116), (170, 72), (332, 78), (446, 116))[1:]
        upper += [(446, 192)]
        upper += cubic((446, 192), (344, 184), (305, 242), (214, 250))[1:]
        upper += cubic((214, 250), (132, 258), (82, 236), (62, 214))[1:]
        polygon(upper)

        lower = [(62, 294)]
        lower += cubic((62, 294), (142, 280), (178, 264), (250, 274))[1:]
        lower += cubic((250, 274), (330, 286), (374, 326), (446, 322))[1:]
        lower += [(446, 398)]
        lower += cubic((446, 398), (332, 438), (168, 434), (62, 394))[1:]
        polygon(lower)

    return image.resize((size, size), Image.Resampling.LANCZOS)


def adaptive_wordmark(color: str = INK, height: int = 150) -> Image.Image:
    text = "agendalivre"
    base_font = font("semibold", 150)
    glyphs = []
    for index, char in enumerate(text):
        temp = Image.new("L", (240, 220), 0)
        td = ImageDraw.Draw(temp)
        td.text((10, -4), char, font=base_font, fill=255)
        bbox = temp.getbbox()
        glyph = temp.crop(bbox)
        if index <= 5:
            factor = 0.78 + index * (0.16 / 5)
        else:
            factor = 1.00 + (index - 6) * (0.15 / 4)
        glyph = glyph.resize((max(1, round(glyph.width * factor)), glyph.height), Image.Resampling.LANCZOS)
        glyphs.append(glyph)

    tracks = [18, 18, 18, 18, 18, 42, 22, 26, 30, 34, 38]
    width = sum(g.width for g in glyphs) + sum(tracks[:-1]) + 60
    out = Image.new("RGBA", (width, 190), TRANSPARENT)
    x = 20
    for index, glyph in enumerate(glyphs):
        tint = Image.new("RGBA", glyph.size, rgba(color))
        colored = Image.composite(tint, Image.new("RGBA", glyph.size, TRANSPARENT), glyph)
        y = 24 + (130 - glyph.height)
        out.alpha_composite(colored, (x, y))
        x += glyph.width + tracks[index]
    bbox = out.getbbox()
    out = out.crop(bbox)
    scale = height / out.height
    return out.resize((round(out.width * scale), height), Image.Resampling.LANCZOS)


def standard_wordmark(text: str, font_name: str, size: int, color: str = INK) -> Image.Image:
    f = font(font_name, size)
    bbox = f.getbbox(text)
    image = Image.new("RGBA", (bbox[2] - bbox[0] + 20, bbox[3] - bbox[1] + 20), TRANSPARENT)
    d = ImageDraw.Draw(image)
    d.text((10 - bbox[0], 10 - bbox[1]), text, font=f, fill=rgba(color))
    return image


def editorial_wordmark(color: str = INK) -> Image.Image:
    agenda = standard_wordmark("agenda", "bahnschrift", 116, color)
    livre = standard_wordmark("livre", "sitka_italic", 132, color)
    out = Image.new("RGBA", (agenda.width + livre.width + 30, 180), TRANSPARENT)
    out.alpha_composite(agenda, (0, 42))
    out.alpha_composite(livre, (agenda.width + 18, 23))
    return out.crop(out.getbbox())


def lockup(concept: Concept) -> Image.Image:
    canvas = Image.new("RGBA", (1200, 560), rgba(WHITE))

    mark = symbol(concept.id, INK, 292)
    if concept.id == "01-g-de-janela":
        wm = standard_wordmark("agenda livre", "semibold", 112)
    elif concept.id == "02-coordenada-aberta":
        wm = standard_wordmark("Agenda Livre", "bahnschrift", 106)
    elif concept.id == "03-encontro-marcado":
        wm = standard_wordmark("Agenda Livre", "candara_bold", 108)
    else:
        wm = editorial_wordmark(INK)
    total = mark.width + 64 + wm.width
    x = max(62, (1200 - total) // 2)
    canvas.alpha_composite(mark, (x, 134))
    canvas.alpha_composite(wm, (x + mark.width + 64, (560 - wm.height) // 2 + 6))
    return canvas.convert("RGB")


def app_icon(concept: Concept) -> Image.Image:
    icon = Image.new("RGBA", (512, 512), rgba(SOFT))
    d = ImageDraw.Draw(icon)
    d.rounded_rectangle((1, 1, 510, 510), radius=112, outline=rgba("#CFE0F8"), width=3)
    mark = symbol(concept.id, BLUE, 320)
    icon.alpha_composite(mark, (96, 96))
    return icon.convert("RGB")


def make_card(concept: Concept) -> Image.Image:
    card = Image.new("RGB", (850, 520), WHITE)
    d = ImageDraw.Draw(card)
    d.rounded_rectangle((1, 1, 848, 518), radius=20, fill=WHITE, outline=LINE, width=2)

    main = Image.open(LOCKUPS / f"{concept.id}-lockup.png").convert("RGB")
    main.thumbnail((790, 320), Image.Resampling.LANCZOS)
    card.paste(main, (30 + (790 - main.width) // 2, 18 + (320 - main.height) // 2))
    d.line((30, 348, 820, 348), fill="#EDF2F8", width=1)

    icon = Image.open(ICONS / f"{concept.id}-icon.png").resize((92, 92), Image.Resampling.LANCZOS)
    card.paste(icon, (32, 382))
    d.text((146, 380), "MONOCROMIA / MICRO", font=font("bold", 13), fill=MUTED)

    centers = (168, 230, 298, 380)
    for index, size in enumerate((16, 24, 32, 48)):
        micro = Image.open(TESTS / f"{concept.id}-{size}.png").convert("RGB")
        cx = centers[index]
        card.paste(micro, (round(cx - size / 2), round(433 - size / 2)))
        label = f"{size}px"
        lw = d.textlength(label, font=font("regular", 11))
        d.text((cx - lw / 2, 482), label, font=font("regular", 11), fill=MUTED)

    d.text((470, 375), concept.number, font=font("bold", 18), fill=BLUE)
    d.text((512, 371), concept.label, font=font("bold", 25), fill=INK)
    d.text((470, 414), concept.family, font=font("semibold", 15), fill=MUTED)
    d.text((470, 449), concept.best, font=font("regular", 14), fill=MUTED)
    return card


def main():
    items = []
    for item_index, concept in enumerate(CONCEPTS, start=1):
        mark = symbol(concept.id, INK)
        mark_path = SYMBOLS / f"{concept.id}-symbol.png"
        mark.save(mark_path, optimize=True)

        main_lockup = lockup(concept)
        lockup_path = LOCKUPS / f"{concept.id}-lockup.png"
        main_lockup.save(lockup_path, optimize=True)
        shutil.copyfile(lockup_path, GENERATED / f"{concept.id}.png")

        icon = app_icon(concept)
        icon.save(ICONS / f"{concept.id}-icon.png", optimize=True)

        for micro_size in (16, 24, 32, 48):
            micro_mark = symbol(concept.id, INK, micro_size)
            micro = Image.new("RGBA", (micro_size, micro_size), rgba(WHITE))
            micro.alpha_composite(micro_mark)
            micro.convert("RGB").save(TESTS / f"{concept.id}-{micro_size}.png", optimize=True)

        thumb = Image.new("RGB", (720, 360), WHITE)
        fitted = main_lockup.copy()
        fitted.thumbnail((700, 336), Image.Resampling.LANCZOS)
        thumb.paste(fitted, ((720 - fitted.width) // 2, (360 - fitted.height) // 2))
        thumb_path = THUMBS / f"{concept.id}.jpg"
        thumb.save(thumb_path, quality=92, subsampling=0)

        review = REVIEW_DETAILS[concept.id]
        items.append({
            "id": concept.id,
            "index": item_index,
            "title": f"{concept.number}. {concept.label}",
            "routeName": review["routeName"],
            "familyTitle": concept.family,
            "src": f"generated/lockups/{concept.id}-lockup.png",
            "href": f"generated/lockups/{concept.id}-lockup.png",
            "caption": concept.best,
            "best_for": review["best_for"],
            "decision_advice": review["decision_advice"],
            "watch_out": review["watch_out"],
            "family": concept.family,
            "description": concept.best,
            "image": f"generated/{concept.id}.png",
            "imageUrl": f"generated/{concept.id}.png",
            "previewImageUrl": f"generated/mcp-thumbs/{concept.id}.jpg",
            "sourceImageUrl": f"generated/lockups/{concept.id}-lockup.png",
            "remixSuggestions": [
                "Refinar proporções e terminais desta direção",
                "Testar uma versão ainda mais minimalista",
                "Aplicar esta direção na abertura do aplicativo",
            ],
        })

    sheet = Image.new("RGB", (1800, 1270), "#F7F9FC")
    d = ImageDraw.Draw(sheet)
    d.text((50, 38), "Agenda Livre — quatro identidades curadas", font=font("bold", 36), fill=INK)
    d.text((50, 86), "Desenhadas a partir da janela livre de 30 minutos · primária monocromática · aplicação em azul-cobalto", font=font("regular", 17), fill=MUTED)
    for index, concept in enumerate(CONCEPTS):
        card = make_card(concept)
        x = 50 + (index % 2) * 875
        y = 135 + (index // 2) * 540
        sheet.paste(card, (x, y))
    sheet.save(OUT / "offer-contact-sheet.png", optimize=True)

    (OUT / "data" / "review-manifest.json").write_text(json.dumps(items, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"concepts": len(CONCEPTS), "contactSheet": str(OUT / "offer-contact-sheet.png")}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
