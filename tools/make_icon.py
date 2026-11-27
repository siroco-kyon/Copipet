# アイコン生成スクリプト
#   python3 tools/make_icon.py showa   … 昭和の特売シール（既定のアイコン）
#   python3 tools/make_icon.py bubble  … 青い吹き出しに「コ」（初代のアイコン）
#   python3 tools/make_icon.py all     … 両方
# 出力：assets/icons/<名前>.ico（16〜256px）と、確認用の assets/icons/<名前>_preview.png
# 必要なもの：Pillow。showa は太い日本語フォントを使う（--font で指定、既定は Noto Sans CJK JP Black）。
import argparse
import math
import os
from PIL import Image, ImageDraw, ImageFont

S = 1024  # 描画用の大きさ（ここから縮小して各サイズを作る）
SIZES = [16, 24, 32, 48, 64, 128, 256]
HERE = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = os.path.normpath(os.path.join(HERE, "..", "assets", "icons"))


# ---------------------------------------------------------------- showa
# 黄色のギザギザに赤文字。小さいサイズは描き分ける：
#   48px以上 … 「コピペ」＋「ペタッ!」／32px … 「コピペ」のみ／16〜24px … 「コ」1文字
def render_showa(size, font_path):
    yellow, red, navy = (255, 214, 0, 255), (213, 0, 0, 255), (26, 35, 126, 255)

    def font(px):
        return ImageFont.truetype(font_path, px, index=0)  # index 0 = JP

    def starburst(outer, inner, points=16):
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

    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    small = size <= 24
    border = 0.07 if small else 0.035
    d.polygon(starburst(0.49, 0.41), fill=red)
    d.polygon(starburst(0.49 - border, 0.41 - border), fill=yellow)
    if small:
        text_center(d, S / 2, S / 2, "コ", font(560), red)
    elif size <= 32:
        text_center(d, S / 2, S / 2, "コピペ", font(250), red)
    else:
        text_center(d, S / 2, S * 0.45, "コピペ", font(250), red)
        text_center(d, S / 2, S * 0.67, "ペタッ!", font(118), navy)
    return im.resize((size, size), Image.LANCZOS)


# ---------------------------------------------------------------- bubble
# 白い縁取りの青い吹き出しに、白い「コ」の字。
def render_bubble(size, font_path=None):
    blue, white = (0, 174, 239, 255), (255, 255, 255, 255)
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    small = size <= 24
    ow = 70 if small else 44  # 白い縁の太さ
    box = (64, 96, 960, 800)
    r = 200
    d.rounded_rectangle((box[0] - ow, box[1] - ow, box[2] + ow, box[3] + ow), r + ow, fill=white)
    d.polygon([(230 - ow, 740), (150, 1010), (500 + ow, 760)], fill=white)
    d.rounded_rectangle(box, r, fill=blue)
    d.polygon([(230, 760), (170, 980), (470, 770)], fill=blue)
    t = 120 if small else 96
    x0, x1, y0, y1 = 300, 720, 250, 650
    d.rectangle((x0, y0, x1, y0 + t), fill=white)
    d.rectangle((x1 - t, y0, x1, y1), fill=white)
    d.rectangle((x0, y1 - t, x1, y1), fill=white)
    return im.resize((size, size), Image.LANCZOS)


STYLES = {"showa": render_showa, "bubble": render_bubble}


def build(name, font_path):
    render = STYLES[name]
    images = [render(s, font_path) for s in SIZES]
    ico_path = os.path.join(OUT_DIR, name + ".ico")
    images[-1].save(ico_path, format="ICO", sizes=[(s, s) for s in SIZES], append_images=images[:-1])

    # 確認用：ライト／ダーク背景に 256・48・32・24・16 を並べる
    show = [256, 48, 32, 24, 16]
    w = sum(show) + 10 * (len(show) + 1)
    sheet = Image.new("RGBA", (w * 2, 276), (0, 0, 0, 0))
    for i, bg in enumerate([(245, 245, 245, 255), (30, 30, 30, 255)]):
        ox = i * w
        ImageDraw.Draw(sheet).rectangle((ox, 0, ox + w, 276), fill=bg)
        x = ox + 10
        for s in show:
            sheet.alpha_composite(render(s, font_path), (x, 10))
            x += s + 10
    sheet.save(os.path.join(OUT_DIR, name + "_preview.png"))
    print("wrote", ico_path)


def main():
    parser = argparse.ArgumentParser(description="コピペットのアイコンを作る")
    parser.add_argument("style", choices=list(STYLES) + ["all"])
    parser.add_argument("--font", default="/usr/share/fonts/opentype/noto/NotoSansCJK-Black.ttc",
                        help="showa で使う太い日本語フォント（Windows なら例: C:/Windows/Fonts/YuGothB.ttc）")
    args = parser.parse_args()
    os.makedirs(OUT_DIR, exist_ok=True)
    for name in (STYLES if args.style == "all" else [args.style]):
        build(name, args.font)


if __name__ == "__main__":
    main()
