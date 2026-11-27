# アイコン生成スクリプト: python3 tools/make_icon.py src/Copipet/app.ico icon256.png preview.png
import sys
from PIL import Image, ImageDraw
BLUE=(0,174,239,255); WHITE=(255,255,255,255)
def render(size):
    S=1024; im=Image.new("RGBA",(S,S),(0,0,0,0)); d=ImageDraw.Draw(im)
    small = size<=24
    ow = 70 if small else 44     # white outline width
    # bubble body
    box=(64,96,960,800); r=200
    tail=[(230,760),(170,980),(470,770)]
    # outline layer (white, grown)
    d.rounded_rectangle((box[0]-ow,box[1]-ow,box[2]+ow,box[3]+ow),r+ow,fill=WHITE)
    d.polygon([(230-ow,740),(150,1010),(500+ow,760)],fill=WHITE)
    d.rounded_rectangle(box,r,fill=BLUE)
    d.polygon(tail,fill=BLUE)
    # katakana コ : top bar, right bar, bottom bar
    t = 120 if small else 96
    x0,x1,y0,y1 = 300,720,250,650
    d.rectangle((x0,y0,x1,y0+t),fill=WHITE)
    d.rectangle((x1-t,y0,x1,y1),fill=WHITE)
    d.rectangle((x0,y1-t,x1,y1),fill=WHITE)
    return im.resize((size,size),Image.LANCZOS)
sizes=[16,24,32,48,64,128,256]
imgs=[render(s) for s in sizes]
imgs[-1].save(sys.argv[1],format="ICO",sizes=[(s,s) for s in sizes],append_images=imgs[:-1])
render(256).save(sys.argv[2])
# preview sheet on light and dark
sheet=Image.new("RGBA",(2*(256+48+32+16)+60,300),(0,0,0,0))
for i,bg in enumerate([(245,245,245,255),(30,30,30,255)]):
    ox=i*(sheet.width//2); ImageDraw.Draw(sheet).rectangle((ox,0,ox+sheet.width//2,300),fill=bg)
    x=ox+10
    for s in [256,48,32,16]:
        sheet.alpha_composite(render(s),(x,20)); x+=s+10
sheet.save(sys.argv[3])
