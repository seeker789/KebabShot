using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Kebabshot {
internal enum Tool { Move, Pen, Line, Arrow, Box, Highlight, Text }
internal sealed class Mark {
    public Tool Kind;
    public Color Color;
    public readonly List<Point> Points=new List<Point>();
    public string Caption;
    public void Draw(Graphics g) {
        Point a=Points[0], b=Points[Points.Count-1];
        using(var p=new Pen(Color,3)) {
            p.StartCap=LineCap.Round;p.EndCap=LineCap.Round;p.LineJoin=LineJoin.Round;
            if(Kind==Tool.Text){using(var f=Style.Font(17,true))using(var brush=new SolidBrush(Color))g.DrawString(Caption,f,brush,a);}
            else if(Kind==Tool.Box){var r=Editor.Between(a,b);g.DrawRectangle(p,r);}
            else if(Kind==Tool.Highlight || Kind==Tool.Pen){
                if(Kind==Tool.Highlight){p.Color=System.Drawing.Color.FromArgb(90,Color);p.Width=18;}
                if(Points.Count>1)g.DrawLines(p,Points.ToArray());
                else using(var brush=new SolidBrush(p.Color))g.FillEllipse(brush,a.X-p.Width/2,a.Y-p.Width/2,p.Width,p.Width);
            } else if(Kind==Tool.Arrow){using(var cap=new AdjustableArrowCap(4,5,true)){p.CustomEndCap=cap;g.DrawLine(p,a,b);}}
            else g.DrawLine(p,a,b);
        }
    }
}
internal sealed class Editor : Form {
    readonly Bitmap shot; readonly Rectangle desktop;
    readonly FlowLayoutPanel bar=new FlowLayoutPanel();
    readonly Dictionary<Tool,Button> buttons=new Dictionary<Tool,Button>();
    readonly List<Mark> marks=new List<Mark>(), redo=new List<Mark>();
    Rectangle region,original; Point anchor; int corner=-1;
    enum Drag {None,Select,Move,Resize,Draw}
    Drag drag; Tool tool=Tool.Move; Color ink=Color.FromArgb(234,78,59);
    Mark current; TextBox entry; Point entryPoint;
    readonly Button color;
    public Editor(Bitmap image,Rectangle bounds) {
        shot=image;desktop=bounds;AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=bounds;
        DoubleBuffered=true;KeyPreview=true;TopMost=true;ShowInTaskbar=false;Cursor=Cursors.Cross;
        bar.BackColor=Style.Ink;bar.Padding=new Padding(3);bar.Visible=false;bar.Cursor=Cursors.Default;Controls.Add(bar);
        foreach(Tool t in Enum.GetValues(typeof(Tool))){Tool choice=t;buttons[t]=Add(t.ToString(),()=>Choose(choice));}
        color=Add("Color",()=>{using(var dlg=new ColorDialog {Color=ink,FullOpen=true})if(dlg.ShowDialog(this)==DialogResult.OK){ink=dlg.Color;color.ForeColor=ink;}});
        color.ForeColor=ink;
        Add("Undo",Undo);Add("Redo",Redo);Add("Copy",()=>Export(false));Add("Save",()=>Export(true));Add("Close",Close);
        Choose(Tool.Move);
    }
    protected override void OnShown(EventArgs e){base.OnShown(e);Bounds=desktop;Activate();}
    Button Add(string name,Action action) {
        var b=Style.Button(name,Style.Ink,Style.Paper);b.Font=Style.Font(9,true);b.Width=TextRenderer.MeasureText(name,b.Font).Width+15;b.Height=31;b.Margin=new Padding(1);b.TabStop=false;
        b.Click+=(s,e)=>{CommitText();action();};bar.Controls.Add(b);return b;
    }
    void Choose(Tool next){tool=next;foreach(var item in buttons){item.Value.BackColor=item.Key==tool?Style.Moss:Style.Ink;item.Value.ForeColor=item.Key==tool?Style.Ink:Style.Paper;}Invalidate();}
    internal static Rectangle Between(Point a,Point b){return Rectangle.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));}
    Point[] Corners(Rectangle r){return new[]{r.Location,new Point(r.Right,r.Top),new Point(r.Left,r.Bottom),new Point(r.Right,r.Bottom)};}
    int HitCorner(Point pt){var pts=Corners(region);for(int i=0;i<4;i++)if(Math.Abs(pt.X-pts[i].X)<=6 && Math.Abs(pt.Y-pts[i].Y)<=6)return i;return -1;}
    Point Local(Point p){return new Point(p.X-region.X,p.Y-region.Y);}
    protected override void OnPaint(PaintEventArgs e){
        var g=e.Graphics;g.DrawImageUnscaled(shot,0,0);
        using(var shade=new SolidBrush(Color.FromArgb(115,12,16,14)))g.FillRectangle(shade,ClientRectangle);
        if(region.Width>0 && region.Height>0){
            g.DrawImage(shot,region,region,GraphicsUnit.Pixel);var state=g.Save();g.SetClip(region);g.TranslateTransform(region.X,region.Y);DrawMarks(g);g.Restore(state);
            using(var p=new Pen(Style.Moss,1)){g.DrawRectangle(p,region);}
            foreach(var pt in Corners(region))g.FillRectangle(Brushes.White,pt.X-3,pt.Y-3,6,6);
            Badge(g,region.Width+" × "+region.Height,new Point(region.Left,Math.Max(0,region.Top-26)));
        }else{var screen=Screen.FromPoint(Cursor.Position).Bounds;Badge(g,"Drag to capture a region   ·   Esc to cancel",new Point(screen.Left-desktop.Left+24,screen.Top-desktop.Top+24));}
    }
    void Badge(Graphics g,string text,Point pt){var size=TextRenderer.MeasureText(text,Font);var rect=new Rectangle(pt,new Size(size.Width+14,size.Height+8));using(var b=new SolidBrush(Style.Ink))g.FillRectangle(b,rect);TextRenderer.DrawText(g,text,Font,rect,Style.Paper,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
    void DrawMarks(Graphics g){g.SmoothingMode=SmoothingMode.AntiAlias;foreach(var m in marks)m.Draw(g);if(current!=null)current.Draw(g);}
    void PlaceBar(){
        if(region.IsEmpty)return;
        Point center=PointToScreen(new Point(region.Left+region.Width/2,region.Top+region.Height/2));
        Rectangle monitor=RectangleToClient(Screen.FromPoint(center).Bounds);int width=bar.Padding.Horizontal;
        foreach(Control c in bar.Controls)width+=c.Width+c.Margin.Horizontal;
        bar.Width=Math.Min(monitor.Width,width);bar.Height=bar.GetPreferredSize(new Size(bar.Width,0)).Height;
        int x=Math.Max(monitor.Left,Math.Min(region.Right-bar.Width,monitor.Right-bar.Width));
        int y=region.Bottom+7;if(y+bar.Height>monitor.Bottom)y=region.Top-bar.Height-7;
        if(y<monitor.Top)y=monitor.Bottom-bar.Height;bar.Location=new Point(x,y);bar.Visible=true;
    }
    protected override void OnMouseDown(MouseEventArgs e){
        base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;CommitText();Focus();anchor=e.Location;original=region;
        corner=region.IsEmpty?-1:HitCorner(anchor);
        if(corner>=0 && tool==Tool.Move)drag=Drag.Resize;
        else if(region.IsEmpty || !region.Contains(anchor)){drag=Drag.Select;marks.Clear();redo.Clear();region=Rectangle.Empty;Choose(Tool.Move);}
        else if(tool==Tool.Move)drag=Drag.Move;
        else if(tool==Tool.Text){BeginText(anchor);return;}
        else {drag=Drag.Draw;current=new Mark {Kind=tool,Color=ink};current.Points.Add(Local(anchor));}
        Capture=true;bar.Hide();Invalidate();
    }
    protected override void OnMouseMove(MouseEventArgs e){
        base.OnMouseMove(e);if(drag==Drag.None){Cursor=tool==Tool.Move && !region.IsEmpty && HitCorner(e.Location)>=0?(HitCorner(e.Location)==0 || HitCorner(e.Location)==3?Cursors.SizeNWSE:Cursors.SizeNESW):tool==Tool.Move && region.Contains(e.Location)?Cursors.SizeAll:Cursors.Cross;return;}
        var pt=new Point(Math.Max(0,Math.Min(ClientSize.Width,e.X)),Math.Max(0,Math.Min(ClientSize.Height,e.Y)));
        if(drag==Drag.Select)region=Between(anchor,pt);
        else if(drag==Drag.Resize){Point opposite=Corners(original)[3-corner];region=Between(opposite,pt);}
        else if(drag==Drag.Move){region.Location=new Point(Math.Max(0,Math.Min(ClientSize.Width-region.Width,original.X+pt.X-anchor.X)),Math.Max(0,Math.Min(ClientSize.Height-region.Height,original.Y+pt.Y-anchor.Y)));}
        else if(drag==Drag.Draw){Point end=Local(pt);if(current.Kind!=Tool.Pen && current.Kind!=Tool.Highlight && current.Points.Count>1)current.Points.RemoveAt(1);current.Points.Add(end);}
        Invalidate();
    }
    protected override void OnMouseUp(MouseEventArgs e){
        base.OnMouseUp(e);if(e.Button!=MouseButtons.Left || drag==Drag.None)return;
        if(drag==Drag.Draw){marks.Add(current);current=null;redo.Clear();}
        if(region.Width<1 || region.Height<1)region=drag==Drag.Resize?original:Rectangle.Empty;
        if(drag==Drag.Resize){int dx=original.Left-region.Left,dy=original.Top-region.Top;foreach(var stack in new[]{marks,redo})foreach(var mark in stack)for(int i=0;i<mark.Points.Count;i++){var p=mark.Points[i];p.Offset(dx,dy);mark.Points[i]=p;}}
        drag=Drag.None;Capture=false;PlaceBar();Invalidate();
    }
    void BeginText(Point p){entryPoint=Local(p);entry=new TextBox{Location=p,Width=Math.Max(60,Math.Min(300,region.Right-p.X)),Font=Style.Font(17,true),ForeColor=ink};Controls.Add(entry);entry.BringToFront();entry.Focus();}
    void CommitText(){if(entry==null)return;string label=entry.Text;entry.Dispose();entry=null;if(label.Length>0){var m=new Mark{Kind=Tool.Text,Color=ink,Caption=label};m.Points.Add(entryPoint);marks.Add(m);redo.Clear();}Invalidate();}
    void Undo(){if(marks.Count>0){redo.Add(marks[marks.Count-1]);marks.RemoveAt(marks.Count-1);Invalidate();}}
    void Redo(){if(redo.Count>0){marks.Add(redo[redo.Count-1]);redo.RemoveAt(redo.Count-1);Invalidate();}}
    void Export(bool save){
        if(region.IsEmpty)return;CommitText();
        try{using(var output=new Bitmap(region.Width,region.Height)){
            using(var g=Graphics.FromImage(output)){g.DrawImage(shot,new Rectangle(Point.Empty,region.Size),region,GraphicsUnit.Pixel);DrawMarks(g);}
            if(save){using(var dlg=new SaveFileDialog{Filter="PNG image (*.png)|*.png",DefaultExt="png",FileName="Kebabshot-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".png",InitialDirectory=Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)}){if(dlg.ShowDialog(this)!=DialogResult.OK)return;output.Save(dlg.FileName,ImageFormat.Png);}}
            else Clipboard.SetDataObject(output,true,5,100);
        }Close();}catch(Exception ex){MessageBox.Show(this,ex.Message,save?"Save failed":"Copy failed");}
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys key){
        if(entry!=null){if(key==Keys.Escape){entry.Clear();CommitText();return true;}if(key==Keys.Enter){CommitText();return true;}return base.ProcessCmdKey(ref msg,key);}
        if(key==Keys.Escape){Close();return true;}
        if(drag==Drag.None && !region.IsEmpty){
            if(key==(Keys.Control|Keys.C)||key==Keys.Enter){Export(false);return true;}
            if(key==(Keys.Control|Keys.S)){Export(true);return true;}
            if(key==(Keys.Control|Keys.Z)){Undo();return true;}
            if(key==(Keys.Control|Keys.Y)){Redo();return true;}
        }
        return base.ProcessCmdKey(ref msg,key);
    }
}
}
