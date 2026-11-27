# アイコン生成スクリプト（案H：昭和の特売シール）
#   python3 tools/make_icon.py src/Copipet/app.ico icon256.png preview.png [フォント.ttc]
# フォントは太い日本語フォント（既定は Noto Sans CJK JP Black）。
# 小さいサイズでは文字がつぶれるため、サイズごとに描き分ける：
#   48px以上 … 「コピペ」＋「ペタッ!」
#   32px     … 「コピペ」のみ
#   16〜24px … 大きな「コ」1文字
import math
import sys
from PIL import Image, ImageDraw, ImageFont

FONT = sys.argv[4] if len(sys.argv) > 4 else "/usr/share/fonts/opentype/noto/NotoSansCJK-Black.ttc"
YELLOW = (255, 214, 0, 255)
RED = (213, 0, 0, 255)
NAVY = (26, 35, 126, 255)
S = 1024  # 描画用の大きさ（縮小して各サイズを作る）


def font(px):
    return ImageFont.truetype(FONT, px, index=0)  # index 0 = JP


def starburst(points=16, outer=0.48, inner=0.40):
    c = S / 2
    pts = []
    for i in range(points * 2):
        r = (outer if i % 2 == 0 else inner) * S
        a = math.pi * i / points - math.pi / 2
        pts.append((c + r * math.cos(a), c + r * math.sin(a)))
    return pts


def text_center(d, cx, cy, s, f, fill):
    box = d.textbbox((0, 0), s, font=f)
    w, h = box[2] - box[0], box[3] - box[1]
    d.text((cx - w / 2 - box[0], cy - h / 2 - box[1]), s, font=f, fill=fill)


def render(size):
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    small = size <= 24
    # 赤い縁 → 黄色の本体（縁は小さいサイズほど太く）
    border = 0.07 if small else 0.035
    d.polygon(starburst(outer=0.49, inner=0.41), fill=RED)
    d.polygon(starburst(outer=0.49 - border, inner=0.41 - border), fill=YELLOW)

    if small:
        text_center(d, S / 2, S / 2, "コ", font(560), RED)
    elif size <= 32:
        text_center(d, S / 2, S / 2, "コピペ", font(250), RED)
    else:
        text_center(d, S / 2, S * 0.45, "コピペ", font(250), RED)
        text_center(d, S / 2, S * 0.67, "ペタッ!", font(118), NAVY)
    return im.resize((size, size), Image.LANCZOS)


sizes = [16, 24, 32, 48, 64, 128, 256]
images = [render(s) for s in sizes]
images[-1].save(sys.argv[1], format="ICO", sizes=[(s, s) for s in sizes], append_images=images[:-1])
render(256).save(sys.argv[2])

# 確認用：ライト／ダーク背景に 256・48・32・24・16 を並べた画像
show = [256, 48, 32, 24, 16]
w = sum(show) + 10 * (len(show) + 1)
sheet = Image.new("RGBA", (w * 2, 276), (0, 0, 0, 0))
for i, bg in enumerate([(245, 245, 245, 255), (30, 30, 30, 255)]):
    ox = i * w
    ImageDraw.Draw(sheet).rectangle((ox, 0, ox + w, 276), fill=bg)
    x = ox + 10
    for s in show:
        sheet.alpha_composite(render(s), (x, 10))
        x += s + 10
sheet.save(sys.argv[3])
