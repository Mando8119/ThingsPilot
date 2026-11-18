"""Rebuild original geometric ThingPilot package/gallery artwork (requires Pillow)."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
assets = root / "src/ThingPilot.Extension/Assets"
assets.mkdir(parents=True, exist_ok=True)

def icon(size):
    scale = 4
    image = Image.new("RGBA", (size * scale, size * scale), (0, 0, 0, 0))
    d = ImageDraw.Draw(image)
    def points(values):
        return [(round(x * size * scale), round(y * size * scale)) for x, y in values]
    d.rounded_rectangle((0, 0, size*scale-1, size*scale-1), radius=size*scale*.22, fill="#11283F")
    d.line(points([(.23,.49),(.5,.25),(.77,.49)]), fill="#63DCCB", width=max(2,round(size*scale*.065)), joint="curve")
    d.line(points([(.3,.47),(.3,.75),(.7,.75),(.7,.47)]), fill="#63DCCB", width=max(2,round(size*scale*.055)), joint="curve")
    d.polygon(points([(.46,.43),(.46,.66),(.64,.545)]), fill="#FFFFFF")
    return image.resize((size, size), Image.Resampling.LANCZOS)

for name, size in [("StoreLogo",50),("Square44x44Logo",44),("SmallTile",71),("Square150x150Logo",150),("LargeTile",310)]:
    icon(size).save(assets / (name + ".png"))
    icon(size * 2).save(assets / (name + ".scale-200.png"))

for name, width, height in [("Wide310x150Logo",310,150),("SplashScreen",620,300)]:
    for scale in [1, 2]:
        image = Image.new("RGBA", (width * scale, height * scale), "#11283F")
        mark = icon(round(height * scale * .65))
        image.alpha_composite(mark, ((image.width-mark.width)//2, (image.height-mark.height)//2))
        image.save(assets / (name + ("" if scale == 1 else ".scale-200") + ".png"))
gallery = root / "packaging/gallery/icon.png"
gallery.parent.mkdir(parents=True, exist_ok=True)
icon(256).save(gallery)
