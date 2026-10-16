using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Catcon {
    public sealed class PetWindow : Form {
        public Content Content {get;private set;}public BehaviorEngine Engine {get;private set;}
        public readonly Preferences Settings;
        public ContextMonitor Monitor {get;private set;}
        public string OverrideContext;
        readonly string root;readonly Timer timer;readonly Stopwatch watch=new Stopwatch();
        readonly NotifyIcon tray;readonly Icon appIcon;
        Bitmap lastPixels;CompanionPanel panel;ContextMenuStrip menu;
        bool down,dragged,disposed;Point pressCursor,pressLocation;double lastTime,saveAt;
        public event Action StatusChanged;
        public PetWindow(string root,Preferences preferences) {
            this.root=root;Settings=preferences;Content=Content.Load(root,Settings.SkinId);
            Engine=new BehaviorEngine(Content.Skin,Content.Behavior,Environment.TickCount){PixelScale=Settings.Scale};Monitor=new ContextMonitor(Content.Classifiers);
            FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;Text="Catcon · 蜜柑";TopMost=Settings.Topmost;
            ClientSize=new Size(Engine.Width,Engine.Width);
            Rectangle work=Screen.PrimaryScreen.WorkingArea;
            Engine.X=Settings.X??(work.Right-Engine.Width-60);Engine.Y=Settings.Y??(work.Bottom-Engine.Width-20);KeepVisible();
            Location=new Point((int)Engine.X,(int)Engine.Y);
            using(var pixels=Engine.Animation.Render())using(var iconBitmap=Animator.Scale(pixels,2))appIcon=Native.CreateIcon(iconBitmap);
            Icon=appIcon;menu=BuildMenu();
            tray=new NotifyIcon{Icon=appIcon,Text="蜜柑 · 右键猫咪打开菜单",ContextMenuStrip=menu,Visible=true};
            tray.DoubleClick+=delegate{OpenPanel();};
            timer=new Timer{Interval=33};timer.Tick+=Tick;
            SystemEvents.DisplaySettingsChanged+=DisplayChanged;
            Shown+=delegate{watch.Start();timer.Start();DrawPet();};
            FormClosed+=delegate{DisposeResources();};
        }
        protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x80000|0x80|0x08000000;return p;}}
        protected override bool ShowWithoutActivation {get{return true;}}
        protected override void OnPaint(PaintEventArgs e){}
        protected override void OnPaintBackground(PaintEventArgs e){}
        protected override void WndProc(ref Message m){
            if(m.Msg==0x21){m.Result=new IntPtr(3);return;}
            if(m.Msg==0x84&&lastPixels!=null&&!down){
                Native.POINT p;Native.GetCursorPos(out p);int x=(p.X-(int)Engine.X)/Settings.Scale,y=(p.Y-(int)Engine.Y)/Settings.Scale;
                if(x<0||y<0||x>=lastPixels.Width||y>=lastPixels.Height||lastPixels.GetPixel(x,y).A<20){m.Result=new IntPtr(-1);return;}
            }
            base.WndProc(ref m);
        }
        void Tick(object sender,EventArgs e){
            try{
                double now=watch.Elapsed.TotalSeconds,dt=Math.Min(.15,now-lastTime);lastTime=now;
                Native.POINT cp;Native.GetCursorPos(out cp);Point cursor=new Point(cp.X,cp.Y);
                if(down){
                    if(!dragged&&BehaviorEngine.Distance(cursor.X,cursor.Y,pressCursor.X,pressCursor.Y)>4){dragged=true;Engine.BeginDrag();}
                    if(dragged){Engine.X=pressLocation.X+cursor.X-pressCursor.X;Engine.Y=pressLocation.Y+cursor.Y-pressCursor.Y;Engine.KeepInside(Screen.FromPoint(cursor).WorkingArea);}
                    if((Native.GetAsyncKeyState(1)&0x8000)==0)ReleasePointer();
                }
                Monitor.Update(dt,Settings.Classify,Settings.SmartClassify);
                var area=Screen.FromPoint(new Point((int)Engine.X+Engine.Width/2,(int)Engine.Y+Engine.Width/2)).WorkingArea;
                Engine.Advance(dt,new InputFrame{Cursor=cursor,WorkArea=area,Context=OverrideContext??Monitor.Category,Movement=Settings.Wander,Reactions=Settings.Reactions,QuietGames=Settings.QuietGames});
                DrawPet();
                if(now-saveAt>8){saveAt=now;Save();if(StatusChanged!=null)StatusChanged();}
            }catch(Exception ex){Storage.Log(ex);timer.Stop();MessageBox.Show("桌宠遇到了问题，已暂停。可以从托盘退出后重新打开。\n"+ex.Message,"Catcon",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        }
        public void DrawPet(){
            if(IsDisposed||!IsHandleCreated)return;
            Bitmap next=Engine.Animation.Render();if(lastPixels!=null)lastPixels.Dispose();lastPixels=next;
            using(var scaled=Animator.Scale(lastPixels,Settings.Scale))Native.SetLayer(Handle,scaled,new Point((int)Math.Round(Engine.X),(int)Math.Round(Engine.Y)));
        }
        protected override void OnMouseDown(MouseEventArgs e){
            base.OnMouseDown(e);
            if(e.Button==MouseButtons.Left){down=true;dragged=false;pressCursor=Cursor.Position;pressLocation=new Point((int)Engine.X,(int)Engine.Y);Capture=true;}
            if(e.Button==MouseButtons.Right)menu.Show(Cursor.Position);
        }
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button==MouseButtons.Left)ReleasePointer();}
        protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);if(e.Button==MouseButtons.Left)OpenPanel();}
        void ReleasePointer(){if(!down)return;down=false;Capture=false;if(dragged)Engine.EndDrag();else Engine.Emit("pet");Save();}
        void DisplayChanged(object sender,EventArgs e){if(IsHandleCreated&&!IsDisposed)BeginInvoke((Action)delegate{KeepVisible();DrawPet();});}
        void KeepVisible(){
            var center=new Point((int)Engine.X+Engine.Width/2,(int)Engine.Y+Engine.Width/2);Engine.KeepInside(Screen.FromPoint(center).WorkingArea);
        }
        ContextMenuStrip BuildMenu(){
            var m=new ContextMenuStrip{Font=new Font("Microsoft YaHei UI",9)};
            m.Items.Add("蜜柑的小窝…",null,delegate{OpenPanel();});m.Items.Add(new ToolStripSeparator());
            m.Items.Add("摸摸头",null,delegate{PlayEvent("pet");});m.Items.Add("喂小鱼干",null,delegate{Play("eat");});m.Items.Add("喝点水",null,delegate{Play("drink");});m.Items.Add("玩毛线球",null,delegate{Play("yarn");});
            m.Items.Add("扑向鼠标（3 秒后）",null,delegate{ScheduleChase();});
            var animations=new ToolStripMenuItem("动作图鉴");foreach(var c in Content.Skin.clips){string id=c.Key;animations.DropDownItems.Add(c.Value.label,null,delegate{Play(id);});}m.Items.Add(animations);
            m.Items.Add(new ToolStripSeparator());
            var pause=new ToolStripMenuItem("暂停",null,delegate{Engine.Paused=!Engine.Paused;Changed();});m.Items.Add(pause);
            var top=new ToolStripMenuItem("始终置顶",null,delegate{Settings.Topmost=!Settings.Topmost;ApplySettings();});m.Items.Add(top);
            m.Items.Add("把猫找回来",null,delegate{Recall();});m.Items.Add(new ToolStripSeparator());m.Items.Add("退出 Catcon",null,delegate{Close();});
            m.Opening+=delegate{pause.Text=Engine.Paused?"继续活动":"暂停";top.Checked=Settings.Topmost;};return m;
        }
        public void Play(string id){Engine.Paused=false;Engine.Play(id,true);DrawPet();Changed();}
        public void PlayEvent(string ev){Engine.Paused=false;Engine.Emit(ev);DrawPet();Changed();}
        public void ScheduleChase(){
            var delay=new Timer{Interval=3000};delay.Tick+=delegate{delay.Stop();delay.Dispose();if(!IsDisposed)PlayEvent("chase");};delay.Start();
        }
        public void Recall(){var area=Screen.FromPoint(Cursor.Position).WorkingArea;Engine.X=area.Right-Engine.Width-30;Engine.Y=area.Bottom-Engine.Width-10;KeepVisible();Show();Engine.Play(Content.Behavior.events["idle"],false);DrawPet();Save();}
        public void OpenPanel(){if(panel==null||panel.IsDisposed)panel=new CompanionPanel(this,root);panel.Show();panel.Activate();}
        public void ApplySettings(){Engine.PixelScale=Settings.Scale;TopMost=Settings.Topmost;Native.SetWindowPos(Handle,new IntPtr(Settings.Topmost?-1:-2),0,0,0,0,0x13);KeepVisible();DrawPet();Save();Changed();}
        public bool Reload(string skinId,out string error){
            Content next=null;
            try {
                next=Content.Load(root,skinId);var old=Content;double x=Engine.X,y=Engine.Y;
                Content=next;Settings.SkinId=skinId;Engine=new BehaviorEngine(next.Skin,next.Behavior,Environment.TickCount){X=x,Y=y,PixelScale=Settings.Scale};Monitor=new ContextMonitor(next.Classifiers);
                tray.ContextMenuStrip=null;menu.Dispose();menu=BuildMenu();tray.ContextMenuStrip=menu;old.Dispose();ApplySettings();error=null;return true;
            } catch(Exception ex){if(next!=null&&next!=Content)next.Dispose();error=ex.Message;Storage.Log(ex);return false;}
        }
        void Changed(){if(StatusChanged!=null)StatusChanged();}
        public void Save(){Settings.X=(int)Engine.X;Settings.Y=(int)Engine.Y;Storage.Save(Settings);}
        void DisposeResources(){if(disposed)return;disposed=true;SystemEvents.DisplaySettingsChanged-=DisplayChanged;timer.Stop();timer.Dispose();if(panel!=null)panel.Dispose();Save();tray.Visible=false;tray.Dispose();menu.Dispose();appIcon.Dispose();if(lastPixels!=null)lastPixels.Dispose();Content.Dispose();}
    }
}
