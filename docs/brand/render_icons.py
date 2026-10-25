import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import svg_raster

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
EDITOR = os.path.join(ROOT, 'Packages', 'com.nekuzaky.horror-ai-manager', 'Editor')
GLYPHS = os.path.join(EDITOR, 'UI', 'Icons')
ICONS = os.path.join(EDITOR, 'Icons')


TILES = [
    ('HorrorAI', (226, 72, 84), (140, 26, 40), 'horror-eye'),
    ('HorrorEnemy', (214, 72, 80), None, 'crosshair'),
    ('HorrorTarget', (84, 152, 214), None, 'bullseye'),
    ('HorrorLightSource', (222, 170, 72), None, 'light'),
    ('NoiseEmitter', (226, 128, 60), None, 'broadcast'),
    ('PatrolRoute', (79, 179, 126), None, 'signpost-split-fill'),
    ('HorrorArchetype', (150, 120, 226), None, 'layers-fill'),
    ('PerceptionProfile', (52, 160, 152), None, 'eye-fill'),
    ('BehaviorProfile', (92, 124, 214), None, 'diagram-3-fill'),
    ('ResonanceNoiseMap', (47, 169, 184), None, 'soundwave'),
    ('ResonanceNoiseBridge', (47, 169, 184), None, 'ear-fill'),
    ('HorrorHindsightProbe', (142, 132, 222), None, 'clock-history'),
]


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


def gradient(size, top, bottom, horizontal=False):
    width, height = int(size[0]), int(size[1])
    size = (width, height)
    image = Image.new('RGB', size)
    draw = ImageDraw.Draw(image)
    steps = width if horizontal else height
    for i in range(steps):
        color = mix(top, bottom, i / max(1, steps - 1))
        if horizontal:
            draw.line([(i, 0), (i, height)], fill=color)
        else:
            draw.line([(0, i), (width, i)], fill=color)
    return image


def tile(size, color, bottom, glyph):
    scale = 4
    big = size * scale
    margin = 2 * scale * size // 64
    radius = 13 * scale * size // 64
    top = mix(color, (255, 255, 255), 0.10) if bottom is None else color
    low = mix(color, (0, 0, 0), 0.06) if bottom is None else bottom
    shape = Image.new('L', (big, big), 0)
    ImageDraw.Draw(shape).rounded_rectangle([margin, margin, big - margin - 1, big - margin - 1], radius=radius, fill=255)
    image = Image.new('RGBA', (big, big), (0, 0, 0, 0))
    image.paste(gradient((big, big), top, low), (0, 0), shape)
    image = image.resize((size, size), Image.LANCZOS)
    glyph_size = int(round(size * 0.53))
    mask = svg_raster.render(os.path.join(GLYPHS, glyph + '.svg'), glyph_size)
    offset = ((size - glyph_size) // 2, (size - glyph_size) // 2)
    white = Image.new('RGBA', (glyph_size, glyph_size), (255, 255, 255, 255))
    image.alpha_composite(Image.composite(white, Image.new('RGBA', white.size, (0, 0, 0, 0)), mask), offset)
    return image


def render_tiles():
    os.makedirs(ICONS, exist_ok=True)
    for name, color, bottom, glyph in TILES:
        tile(64, color, bottom, glyph).save(os.path.join(ICONS, name + '.png'), optimize=True)


if __name__ == '__main__':
    render_tiles()
    print('icons: ' + ', '.join(t[0] for t in TILES))
