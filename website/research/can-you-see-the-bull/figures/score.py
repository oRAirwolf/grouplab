from reportlab.lib.pagesizes import letter
from reportlab.pdfgen import canvas
from reportlab.lib.units import inch
W,H=letter
c=canvas.Canvas("/mnt/user-data/outputs/GroupLab-aim-point-score-sheet-v2.pdf",pagesize=letter)
c.setTitle("GroupLab aim point score sheet, 2026-09-23")
c.setFont("Helvetica-Bold",13);c.drawString(0.5*inch,H-0.55*inch,"Aim point score sheet: 0 cannot see centre, 1 see but not centre, 2 centre confidently")
c.setFont("Helvetica",8.5)
c.drawString(0.5*inch,H-0.78*inch,"Card at 100 yd unless the Dist column says otherwise. 25x on all three high power scopes is the like-for-like glass comparison.")
cols=["Scope","Mag","Dist","Light / mirage"]+list("ABCDEFGHI")
xs=[0.5,2.3,2.8,3.3,4.4]+[4.4+0.4*(k+1) for k in range(8)]
y0=H-1.0*inch;rh=0.33*inch
rows=[("Razor HD Gen III 6-36x56","10x","100"),("","18x","100"),("","25x","100"),("","36x","100"),
("DNT TheOne 7-35x56","10x","100"),("","18x","100"),("","25x","100"),("","35x","100"),
("Strike Eagle 5-25x56","10x","100"),("","18x","100"),("","25x","100"),
("PLxC 1-8x24 (FFP)","4x","100"),("","8x","100"),("","8x","50"),
("Friend's scope:","","100"),("","","100"),("","","100"),("","","")]
c.setFont("Helvetica-Bold",9)
for k,cn in enumerate(cols): c.drawString(xs[k]*inch+3,y0-rh+9,cn)
c.setFont("Helvetica",8.5)
n=len(rows)+1
for r in range(n+1):
    yy=y0-rh*r
    thick = r in (1,5,9,12,15)
    c.setLineWidth(1.2 if thick else 0.5); c.line(0.5*inch,yy,8.0*inch,yy)
for r,(s,m,d) in enumerate(rows):
    yy=y0-rh*(r+2)
    c.drawString(0.5*inch+3,yy+9,s);c.drawString(2.3*inch+3,yy+9,m);c.drawString(2.8*inch+3,yy+9,d)
c.setLineWidth(0.5)
for x in xs+[8.0]: c.line(x*inch,y0,x*inch,y0-rh*n)
yy=y0-rh*n-0.35*inch
c.setFont("Helvetica-Bold",10);c.drawString(0.5*inch,yy,"Also note: time of day, sun direction, and which letter you would choose to shoot load development on.")
c.setFont("Helvetica",9);c.drawString(0.5*inch,yy-0.2*inch,"If there is time: 3 shots at A and 3 at your favourite, same rifle and load. A first look, not a proof.")
c.drawString(0.5*inch,yy-0.45*inch,"Friend's scope make, model, magnification range, focal plane:")
c.drawString(0.5*inch,yy-0.75*inch,"Notes:")
c.save()
