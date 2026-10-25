import math
import re
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw

COMMANDS = 'MmLlHhVvCcSsQqTtAaZz'
NUMBER = re.compile(r'[-+]?(?:\d*\.\d+|\d+\.?)(?:[eE][-+]?\d+)?')


class PathReader:
    def __init__(self, text):
        self.text = text
        self.pos = 0

    def skip(self):
        while self.pos < len(self.text) and self.text[self.pos] in ' ,\t\r\n':
            self.pos += 1

    def done(self):
        self.skip()
        return self.pos >= len(self.text)

    def command(self):
        self.skip()
        if self.pos < len(self.text) and self.text[self.pos] in COMMANDS:
            self.pos += 1
            return self.text[self.pos - 1]
        return None

    def number(self):
        self.skip()
        match = NUMBER.match(self.text, self.pos)
        if not match:
            raise ValueError('number expected at %d in %r' % (self.pos, self.text[:60]))
        self.pos = match.end()
        return float(match.group(0))

    def flag(self):
        self.skip()
        value = self.text[self.pos]
        self.pos += 1
        return value == '1'


def cubic(p0, p1, p2, p3, steps=20):
    points = []
    for i in range(1, steps + 1):
        t = i / steps
        u = 1 - t
        x = u * u * u * p0[0] + 3 * u * u * t * p1[0] + 3 * u * t * t * p2[0] + t * t * t * p3[0]
        y = u * u * u * p0[1] + 3 * u * u * t * p1[1] + 3 * u * t * t * p2[1] + t * t * t * p3[1]
        points.append((x, y))
    return points


def quad(p0, p1, p2, steps=16):
    c1 = (p0[0] + 2 / 3 * (p1[0] - p0[0]), p0[1] + 2 / 3 * (p1[1] - p0[1]))
    c2 = (p2[0] + 2 / 3 * (p1[0] - p2[0]), p2[1] + 2 / 3 * (p1[1] - p2[1]))
    return cubic(p0, c1, c2, p2, steps)


def arc(p0, rx, ry, angle, large, sweep, p1):
    if rx == 0 or ry == 0 or p0 == p1:
        return [p1]
    rx, ry = abs(rx), abs(ry)
    phi = math.radians(angle)
    cos, sin = math.cos(phi), math.sin(phi)
    dx, dy = (p0[0] - p1[0]) / 2, (p0[1] - p1[1]) / 2
    x1 = cos * dx + sin * dy
    y1 = -sin * dx + cos * dy
    scale = x1 * x1 / (rx * rx) + y1 * y1 / (ry * ry)
    if scale > 1:
        rx *= math.sqrt(scale)
        ry *= math.sqrt(scale)
    num = rx * rx * ry * ry - rx * rx * y1 * y1 - ry * ry * x1 * x1
    den = rx * rx * y1 * y1 + ry * ry * x1 * x1
    factor = math.sqrt(max(0.0, num / den)) if den else 0.0
    if large == sweep:
        factor = -factor
    cx1 = factor * rx * y1 / ry
    cy1 = -factor * ry * x1 / rx
    cx = cos * cx1 - sin * cy1 + (p0[0] + p1[0]) / 2
    cy = sin * cx1 + cos * cy1 + (p0[1] + p1[1]) / 2
    start = math.atan2((y1 - cy1) / ry, (x1 - cx1) / rx)
    end = math.atan2((-y1 - cy1) / ry, (-x1 - cx1) / rx)
    delta = end - start
    if sweep and delta < 0:
        delta += 2 * math.pi
    if not sweep and delta > 0:
        delta -= 2 * math.pi
    steps = max(4, int(abs(delta) / (math.pi / 24)))
    points = []
    for i in range(1, steps + 1):
        t = start + delta * i / steps
        x = rx * math.cos(t)
        y = ry * math.sin(t)
        points.append((cos * x - sin * y + cx, sin * x + cos * y + cy))
    return points


def parse_path(d):
    reader = PathReader(d)
    contours = []
    current = []
    pos = (0.0, 0.0)
    start = (0.0, 0.0)
    last_control = None
    command = None
    while not reader.done():
        found = reader.command()
        if found:
            command = found
        elif command in 'Mm':
            command = 'L' if command == 'M' else 'l'
        relative = command.islower()
        base = pos if relative else (0.0, 0.0)
        upper = command.upper()
        control = None
        if upper == 'Z':
            if current:
                contours.append(current)
            current = []
            pos = start
            last_control = None
            continue
        if upper == 'M':
            if current:
                contours.append(current)
            pos = (base[0] + reader.number(), base[1] + reader.number())
            start = pos
            current = [pos]
        elif upper == 'L':
            pos = (base[0] + reader.number(), base[1] + reader.number())
            current.append(pos)
        elif upper == 'H':
            pos = ((pos[0] if relative else 0.0) + reader.number(), pos[1])
            current.append(pos)
        elif upper == 'V':
            pos = (pos[0], (pos[1] if relative else 0.0) + reader.number())
            current.append(pos)
        elif upper in 'CS':
            if upper == 'C':
                c1 = (base[0] + reader.number(), base[1] + reader.number())
            else:
                c1 = (2 * pos[0] - last_control[0], 2 * pos[1] - last_control[1]) if last_control and last_control[2] == 'c' else pos
            c2 = (base[0] + reader.number(), base[1] + reader.number())
            end = (base[0] + reader.number(), base[1] + reader.number())
            current.extend(cubic(pos, c1, c2, end))
            control = (c2[0], c2[1], 'c')
            pos = end
        elif upper in 'QT':
            if upper == 'Q':
                c1 = (base[0] + reader.number(), base[1] + reader.number())
            else:
                c1 = (2 * pos[0] - last_control[0], 2 * pos[1] - last_control[1]) if last_control and last_control[2] == 'q' else pos
            end = (base[0] + reader.number(), base[1] + reader.number())
            current.extend(quad(pos, c1, end))
            control = (c1[0], c1[1], 'q')
            pos = end
        elif upper == 'A':
            rx, ry, angle = reader.number(), reader.number(), reader.number()
            large, sweep = reader.flag(), reader.flag()
            end = (base[0] + reader.number(), base[1] + reader.number())
            current.extend(arc(pos, rx, ry, angle, large, sweep, end))
            pos = end
        last_control = control
    if current:
        contours.append(current)
    return contours


def circle_path(cx, cy, r):
    return 'M%f %fA%f %f 0 1 1 %f %fA%f %f 0 1 1 %f %fZ' % (cx - r, cy, r, r, cx + r, cy, r, r, cx - r, cy)


def shapes(svg_path):
    root = ET.parse(svg_path).getroot()
    view = [float(v) for v in root.get('viewBox', '0 0 16 16').replace(',', ' ').split()]
    result = []
    for element in root.iter():
        tag = element.tag.split('}')[-1]
        rule = element.get('fill-rule', 'nonzero')
        if tag == 'path':
            result.append((parse_path(element.get('d')), rule))
        elif tag == 'circle':
            result.append((parse_path(circle_path(float(element.get('cx', 0)), float(element.get('cy', 0)), float(element.get('r')))), rule))
        elif tag == 'rect':
            x, y = float(element.get('x', 0)), float(element.get('y', 0))
            w, h = float(element.get('width')), float(element.get('height'))
            result.append(([[(x, y), (x + w, y), (x + w, y + h), (x, y + h)]], rule))
        elif tag in ('svg', 'g', 'title', 'defs'):
            continue
        else:
            raise ValueError('unsupported element %s in %s' % (tag, svg_path))
    return view, result


def fill(draw, contours, rule, size, scale, offset):
    edges = []
    for contour in contours:
        points = [((x - offset[0]) * scale, (y - offset[1]) * scale) for x, y in contour]
        for i in range(len(points)):
            a, b = points[i], points[(i + 1) % len(points)]
            if a[1] != b[1]:
                edges.append((a, b))
    for row in range(size):
        y = row + 0.5
        crossings = []
        for a, b in edges:
            if (a[1] <= y < b[1]) or (b[1] <= y < a[1]):
                x = a[0] + (y - a[1]) * (b[0] - a[0]) / (b[1] - a[1])
                crossings.append((x, 1 if b[1] > a[1] else -1))
        crossings.sort()
        winding = 0
        for i in range(len(crossings) - 1):
            winding += crossings[i][1]
            inside = (winding % 2 == 1) if rule == 'evenodd' else winding != 0
            if inside:
                x0 = int(round(crossings[i][0]))
                x1 = int(round(crossings[i + 1][0])) - 1
                if x1 >= x0:
                    draw.line([(x0, row), (x1, row)], fill=255)


def render(svg_path, size, supersample=4):
    view, items = shapes(svg_path)
    big = size * supersample
    mask = Image.new('L', (big, big), 0)
    scale = big / max(view[2], view[3])
    for contours, rule in items:
        layer = Image.new('L', (big, big), 0)
        fill(ImageDraw.Draw(layer), contours, rule, big, scale, (view[0], view[1]))
        mask.paste(255, mask=layer)
    return mask.resize((size, size), Image.LANCZOS)
