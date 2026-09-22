from reportlab.lib.pagesizes import letter
from reportlab.pdfgen import canvas
from reportlab.lib.units import inch
import math
W,H=letter
c=canvas.Canvas("/mnt/user-data/outputs/GroupLab-aim-point-test-card.pdf",pagesize=letter)
c.setTitle("GroupLab aim point visibility test card, 100 yards")
K=(0,0,0);Wt=(1,1,1)
def disc(x,y,d,col):
    c.setFillColorRGB(*col);c.circle(x,y,d/2*inch,stroke=0,fill=1)
def poly(pts,col):
    c.setFillColorRGB(*col);p=c.beginPath();p.moveTo(*pts[0])
    for q in pts[1:]:p.lineTo(*q)
    p.close();c.drawPath(p,stroke=0,fill=1)
def sq(x,y,s,col,rot=False):
    h=s/2*inch
    if rot: poly([(x,y+h),(x+h,y),(x,y-h),(x-h,y)],col)
    else: c.setFillColorRGB(*col);c.rect(x-h,y-h,2*h,2*h,stroke=0,fill=1)
def rect(x,y,w,h,col):
    c.setFillColorRGB(*col);c.rect(x-w/2*inch,y-h/2*inch,w*inch,h*inch,stroke=0,fill=1)
MM=1/25.4
def A(x,y):  # current GroupLab bull
    for d,col in [(25.4,K),(23.8,Wt),(12.7,K),(11.5,Wt),(2.5,K)]: disc(x,y,d*MM,col)
def B(x,y):
    for d,col in [(1.0,K),(0.84,Wt),(0.55,K),(0.39,Wt),(0.22,K)]: disc(x,y,d,col)
def C(x,y): sq(x,y,1.25,K,True); sq(x,y,0.36,Wt,True)
def D(x,y): sq(x,y,1.0,K); rect(x,y,1.0,0.06,Wt); rect(x,y,0.06,1.0,Wt)
def E(x,y): sq(x,y,1.0,K); sq(x,y,0.36,Wt); disc(x,y,0.1,K)
def F(x,y): sq(x,y,1.0,K); sq(x,y,0.8,Wt); disc(x,y,0.2,K)
def G(x,y):
    for dx,dy in [(1,0),(-1,0),(0,1),(0,-1)]:
        cx,cy=x+dx*0.4*inch,y+dy*0.4*inch
        rect(cx,cy,0.4 if dx else 0.1,0.1 if dx else 0.4,K)
def Hh(x,y):
    for a in range(4):
        t=a*math.pi/2
        tip=(x+0.2*inch*math.cos(t),y+0.2*inch*math.sin(t))
        bc=(x+0.65*inch*math.cos(t),y+0.65*inch*math.sin(t))
        px,py=-math.sin(t)*0.22*inch,math.cos(t)*0.22*inch
        poly([tip,(bc[0]+px,bc[1]+py),(bc[0]-px,bc[1]-py)],K)
def I(x,y): disc(x,y,2.0,K); disc(x,y,0.6,Wt); disc(x,y,0.15,K)
designs=[("A","Current GroupLab bull (control)","1.0 in rings, 0.03 in lines, 0.10 in dot",A),
("B","Same bull, bold lines","1.0 in, 0.08 in lines, 0.22 in dot",B),
("C","Black diamond, white centre","1.25 in point to point, 0.36 in centre",C),
("D","Black square, white cross","1.0 in square, 0.06 in white lines",D),
("E","Black square, white centre","1.0 in, 0.36 in centre, 0.10 in dot",E),
("F","Square outline, centre dot","1.0 in, 0.10 in line, 0.20 in dot",F),
("G","Open cross, no centre","0.10 in arms from 0.2 to 0.6 in",G),
("H","Four pointers","tips 0.2 in from centre",Hh),
("I","Large bull (fewer per page)","2.0 in, 0.6 in centre, 0.15 in dot",I)]
c.setFont("Helvetica-Bold",14);c.drawString(0.5*inch,H-0.55*inch,"GroupLab aim point test card: 100 yards")
c.setFont("Helvetica",8.5)
for i,t in enumerate(["Print at Actual size / 100 percent (not Fit to page). Check the 2 in bar at the bottom measures 2 in.",
"At 100 yd, for each scope and magnification, score each letter on the score sheet (page 2):",
"0 cannot see the centre, 1 can see it but cannot centre on it confidently, 2 can centre confidently.",
"A to H fit the current 1.5 in bull spacing. I needs a larger spacing. This card is for looking, not for GroupLab analysis: it has no markers."]):
    c.drawString(0.5*inch,H-(0.8+i*0.15)*inch,t)
colx=[1.5,4.25,7.0];rowy=[8.35,5.35,2.35]
for n,(L,t1,t2,f) in enumerate(designs):
    x=colx[n%3]*inch;y=rowy[n//3]*inch
    f(x,y)
    c.setFillColorRGB(0,0,0);c.setFont("Helvetica-Bold",20);c.drawString(x-1.25*inch,y+1.05*inch,L)
    c.setFont("Helvetica-Bold",8.5);c.drawCentredString(x,y-1.22*inch,t1)
    c.setFont("Helvetica",8);c.drawCentredString(x,y-1.37*inch,t2)
c.setFillColorRGB(0,0,0);c.rect(0.5*inch,0.45*inch,2*inch,0.08*inch,stroke=0,fill=1)
c.setFont("Helvetica",8);c.drawString(2.6*inch,0.46*inch,"2 in check bar")
c.rect(4.0*inch,0.45*inch,50*MM*inch,0.08*inch,stroke=0,fill=1);c.drawString(4.0*inch+50*MM*inch+0.1*inch,0.46*inch,"50 mm check bar")
c.showPage()
# score sheet
c.setFont("Helvetica-Bold",14);c.drawString(0.5*inch,H-0.6*inch,"Score sheet (0 cannot see centre, 1 see but not centre, 2 centre confidently)")
cols=["Scope","Mag","Light / mirage"]+list("ABCDEFGHI")
xs=[0.5,2.4,2.95,4.15]+[4.15+0.4*(k+1) for k in range(9)]
y0=H-1.0*inch;rh=0.36*inch
c.setFont("Helvetica-Bold",9)
for k,cn in enumerate(cols): c.drawString(xs[k]*inch+3,y0-rh+10,cn)
rows=["Razor HD Gen III 6-36x56","","","DNT TheOne 7-35x56","","","Strike Eagle 5-25x56","","","Friend's scope:","","",""]
mags=["10x","18x","max"]*4+[""]
c.setFont("Helvetica",8.5)
for r in range(len(rows)+1):
    yy=y0-rh*(r+1)
    c.line(0.5*inch,yy,8.0*inch,yy)
    if r<len(rows):
        c.drawString(0.5*inch+3,yy-rh+10,rows[r]);c.drawString(2.4*inch+3,yy-rh+10,mags[r])
for k in range(len(xs)): c.line(xs[k]*inch,y0,xs[k]*inch,y0-rh*(len(rows)+1))
c.line(8.0*inch,y0,8.0*inch,y0-rh*(len(rows)+1));c.line(0.5*inch,y0,8.0*inch,y0)
yy=y0-rh*(len(rows)+1)-0.4*inch
c.setFont("Helvetica-Bold",10);c.drawString(0.5*inch,yy,"If there is time: shoot 3 shots at A and 3 at your favourite, same rifle, same load.")
c.setFont("Helvetica",9);c.drawString(0.5*inch,yy-0.2*inch,"Photograph or scan this page after. Three shots only tells you a lot if the difference is large; it is a first look, not a proof.")
c.drawString(0.5*inch,yy-0.4*inch,"Notes:")
c.save()
