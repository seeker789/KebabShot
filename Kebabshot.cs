using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Kebabshot {
internal static class Program {
    [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [STAThread] static void Main() {
        try { if (!SetProcessDpiAwarenessContext(new IntPtr(-4))) SetProcessDPIAware(); }
        catch (EntryPointNotFoundException) { SetProcessDPIAware(); }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using (var home = new Home()) Application.Run(home);
    }
}
internal static class Style {
    public static readonly Color Ink=Color.FromArgb(32,36,32), Paper=Color.FromArgb(247,247,239), Moss=Color.FromArgb(215,238,154);
    public static Font Font(float size, bool bold=false) { return new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular); }
    public static Button Button(string label, Color background, Color foreground) {
        var b=new Button {Text=label,BackColor=background,ForeColor=foreground,FlatStyle=FlatStyle.Flat,Cursor=Cursors.Hand,Font=Font(10,true)};
        b.FlatAppearance.BorderSize=0; return b;
    }
}
internal sealed class Home : Form {
    [DllImport("user32.dll",SetLastError=true)] static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h,int id);
    [DllImport("dwmapi.dll")] static extern int DwmFlush();
    readonly NotifyIcon tray;
    readonly Label shortcut;
    bool print, fallback, busy, quitting;
    readonly Timer delay=new Timer {Interval=220};
    public Home() {
        Text="Kebabshot"; Font=Style.Font(10); BackColor=Style.Paper; ForeColor=Style.Ink;
        AutoScaleMode=AutoScaleMode.Dpi; ClientSize=new Size(530,410); FormBorderStyle=FormBorderStyle.FixedSingle; MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen;
        Controls.Add(new Landscape {Bounds=new Rectangle(24,24,482,178)});
        Controls.Add(new Label {Text="Kebabshot",Font=Style.Font(31,true),AutoSize=true,Location=new Point(21,211)});
        Controls.Add(new Label {Text="Capture a region, mark it up, and share it.",AutoSize=true,Location=new Point(26,275)});
        var start=Style.Button("Capture region",Style.Ink,Style.Paper); start.SetBounds(26,319,166,46); start.Click+=(s,e)=>CaptureRegion(); Controls.Add(start); AcceptButton=start;
        shortcut=new Label {AutoSize=true,Location=new Point(207,332)}; Controls.Add(shortcut);
        var menu=new ContextMenuStrip(); menu.Items.Add("Capture region",null,(s,e)=>CaptureRegion()); menu.Items.Add("Open Kebabshot",null,(s,e)=>Restore()); menu.Items.Add("Exit",null,(s,e)=>{quitting=true; Close();});
        Icon=System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath); tray=new NotifyIcon {Icon=Icon,Text="Kebabshot",ContextMenuStrip=menu,Visible=true}; tray.DoubleClick+=(s,e)=>Restore();
        delay.Tick+=(s,e)=>{delay.Stop(); OpenEditor();};
    }
    void Restore() { if(busy)return; Show(); WindowState=FormWindowState.Normal; Activate(); }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); print=RegisterHotKey(Handle,1,0x4000,0x2c); fallback=RegisterHotKey(Handle,2,0x4003,0x4b); if(shortcut!=null) UpdateShortcut(); }
    void UpdateShortcut() { shortcut.Text=print?"PrtSc":fallback?"Ctrl + Alt + K":"Use the tray to capture"; }
    protected override void OnShown(EventArgs e) { base.OnShown(e); UpdateShortcut(); }
    protected override void OnHandleDestroyed(EventArgs e) { if(print)UnregisterHotKey(Handle,1); if(fallback)UnregisterHotKey(Handle,2); base.OnHandleDestroyed(e); }
    protected override void WndProc(ref Message m) { if(m.Msg==0x0312){CaptureRegion();return;} base.WndProc(ref m); }
    bool wasVisible;
    void CaptureRegion() { if(busy)return; busy=true; wasVisible=Visible && WindowState!=FormWindowState.Minimized; Hide(); delay.Start(); }
    void OpenEditor() {
        try {
            DwmFlush();
            var bounds=SystemInformation.VirtualScreen;
            using(var shot=new Bitmap(bounds.Width,bounds.Height)) {
                using(var g=Graphics.FromImage(shot)) g.CopyFromScreen(bounds.Location,Point.Empty,bounds.Size);
                using(var editor=new Editor(shot,bounds)) editor.ShowDialog();
            }
        } catch(Exception ex) { MessageBox.Show(ex.Message,"Capture failed"); }
        finally {busy=false;if(wasVisible)Restore();}
    }
    protected override void OnFormClosing(FormClosingEventArgs e) { if(!quitting && e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();tray.ShowBalloonTip(1800,"Kebabshot","Still available in the system tray. Choose Exit there to quit.",ToolTipIcon.Info);} base.OnFormClosing(e); }
    protected override void Dispose(bool disposing) {if(disposing){tray.Visible=false;tray.Dispose();delay.Dispose();}base.Dispose(disposing);}
}
internal sealed class Landscape : Control {
    protected override void OnPaint(PaintEventArgs e) {
        var g=e.Graphics;g.ScaleTransform(Width/482f,Height/178f); g.SmoothingMode=SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(225,232,210));
        using(var sun=new SolidBrush(Color.FromArgb(244,172,108)))g.FillEllipse(sun,330,17,73,73);
        Ridge(g,Color.FromArgb(159,179,149),new[]{new Point(0,98),new Point(72,69),new Point(109,82),new Point(191,41),new Point(255,77),new Point(296,63),new Point(400,102),new Point(482,78)});
        Ridge(g,Color.FromArgb(97,130,111),new[]{new Point(0,133),new Point(97,94),new Point(143,110),new Point(202,81),new Point(277,115),new Point(359,80),new Point(482,120)});
        Ridge(g,Color.FromArgb(48,77,65),new[]{new Point(0,140),new Point(63,122),new Point(158,153),new Point(243,127),new Point(302,145),new Point(411,110),new Point(482,133)});
        using(var p=new Pen(Color.White,1)){p.DashStyle=DashStyle.Dash;g.DrawRectangle(p,103,22,274,133);}
        foreach(int x in new[]{103,377})foreach(int y in new[]{22,155})g.FillRectangle(Brushes.White,x-3,y-3,6,6);
    }
    static void Ridge(Graphics g,Color c,Point[] line){var pts=new List<Point>(line);pts.Add(new Point(482,178));pts.Add(new Point(0,178));using(var b=new SolidBrush(c))g.FillPolygon(b,pts.ToArray());}
}
}
