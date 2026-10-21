using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Catcon {
    /// <summary>第二个悬浮窗口：一块像素花圃。可拖拽、点击浇水、右键播种。</summary>
    public sealed class GardenWindow : Form {
        readonly PetWindow pet;readonly Timer timer;readonly Stopwatch watch=new Stopwatch();
        readonly ContextMenuStrip menu;readonly Random random=new Random();
        bool down,dragged,disposed;Point pressCursor,pressLocation;
        double lastTime,saveAt,visitAt,notifyAt,sowAt;
        string sowText;
        public GardenEngine Engine{get;private set;}
        public GardenRenderer Renderer{get;private set;}
        public event Action Changed;
        public GardenWindow(PetWindow pet,string root){
            this.pet=pet;
            GardenConfig config=GardenConfig.Load(Path.Combine(root,"config","garden.json"));
            GardenState state=GardenEngine.LoadState();
            bool resumed=state!=null;
            if(state==null)state=GardenEngine.NewState(config,new Random(Environment.TickCount));
            Engine=new GardenEngine(config,state,Environment.TickCount);
            if(resumed)Engine.CatchUp(GardenEngine.OfflineSeconds(state));
            Renderer=new GardenRenderer(config);
            Engine.Dirty=true;
            FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;
            Text="蜜柑的花园";TopMost=pet.Settings.Topmost;
            ClientSize=new Size(PixelWidth,PixelHeight);
            Rectangle work=Screen.PrimaryScreen.WorkingArea;
            int width=PixelWidth,height=PixelHeight;
            int x=pet.Settings.GardenX??(work.Left+60),y=pet.Settings.GardenY??(work.Bottom-height-24);
            Location=Clamp(new Point(x,y));
            menu=BuildMenu();
            timer=new Timer{Interval=40};timer.Tick+=Tick;
            HandleCreated+=delegate{watch.Start();lastTime=0;timer.Start();Draw();};
            IntPtr forced=Handle;
            visitAt=70+random.NextDouble()*90;
        }
        double Zoom{get{return Preferences.SnapGardenScale(pet.Settings.GardenScale);}}
        int PixelWidth{get{return (int)Math.Round(Renderer.Width*Zoom);}}
        int PixelHeight{get{return (int)Math.Round(Renderer.Height*Zoom);}}
        protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x80000|0x80|0x08000000;return p;}}
        protected override bool ShowWithoutActivation{get{return true;}}
        protected override void OnPaint(PaintEventArgs e){}
        protected override void OnPaintBackground(PaintEventArgs e){}
        protected override void WndProc(ref Message m){
            if(m.Msg==0x21){m.Result=new IntPtr(3);return;}
            if(m.Msg==0x84&&!down){
                Native.POINT p;Native.GetCursorPos(out p);
                double scale=Zoom;
                int x=(int)Math.Floor((p.X-Left)/scale),y=(int)Math.Floor((p.Y-Top)/scale);
                if(!Renderer.Opaque(x,y)){m.Result=new IntPtr(-1);return;}
            }
            base.WndProc(ref m);
        }
        protected override void OnVisibleChanged(EventArgs e){
            base.OnVisibleChanged(e);
            if(Visible){if(!timer.Enabled)timer.Start();lastTime=0;Draw();}
        }
        void Tick(object sender,EventArgs e){
            try{
                double now=watch.Elapsed.TotalSeconds,elapsed=now-lastTime,dt=Math.Min(.2,elapsed);lastTime=now;
                Engine.Advance(dt);
                if(Visible)Draw();
                if(now-saveAt>15&&Engine.Dirty){saveAt=now;GardenEngine.Save(Engine);}
                Notify(now);Visit(dt);
            }catch(Exception ex){Storage.Log(ex);timer.Stop();MessageBox.Show("花园遇到了问题，已暂停。可以从托盘重新打开。\n"+ex.Message,"Catcon",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        }
        public void Draw(){
            if(disposed||!IsHandleCreated)return;
            Renderer.Render(Engine);
            Native.SetLayer(Handle,Renderer.Frame(Zoom),new Point(Left,Top));
        }
        void Notify(double now){
            if(!Engine.NeedsWater||now<notifyAt)return;
            notifyAt=now+2400;
            pet.NotifyGarden(Engine.Critical?"花圃的土已经干透了，蜜柑蹲在旁边等你浇水。":"花园有点渴了，点一下花圃就能浇水。");
        }
        void Visit(double dt){
            if(!pet.Settings.GardenVisits||!Visible)return;
            var visits=Engine.Config.visits;
            visitAt-=dt;if(visitAt>0)return;
            visitAt=visits.minSeconds+random.NextDouble()*(visits.maxSeconds-visits.minSeconds);
            // 摸头、拖拽、扑击等受保护交互进行中时不来，等一会儿再抽。
            if(pet.Engine.Dragging||pet.Engine.Protected){visitAt=18+random.NextDouble()*30;return;}
            if(random.NextDouble()<visits.chance)pet.VisitGarden(RestPoint,PickInteraction());
        }
        /// <summary>
        /// 到花园之后做点什么：凑近开花的植株闻一闻、接飘落的花瓣、在湿土上踩两下、
        /// 盯着蝴蝶看，或者干脆趴下。全部复用已有动作，不额外画帧。
        /// </summary>
        public string PickInteraction(){
            if(pet.Content==null||pet.Content.Skin==null)return null;
            var candidates=new List<string>();
            if(Engine.Blooming>0){
                for(int i=0;i<2;i++)candidates.Add("sit_tilt");
                candidates.Add("sneeze");
            }
            if(Engine.Wind>.2&&Engine.Petals.Count>=2)for(int i=0;i<3;i++)candidates.Add("leaf_catch");
            if(Engine.Moisture>.5)for(int i=0;i<2;i++)candidates.Add("knead");
            if(Engine.Config.decorations.Any(d=>d.type=="butterfly"))candidates.Add("bug_watch");
            for(int i=0;i<2;i++)candidates.Add("loaf");
            var usable=candidates.Where(c=>pet.Content.Skin.clips.ContainsKey(c)).ToList();
            return usable.Count==0?null:usable[random.Next(usable.Count)];
        }
        public Point RestPoint{
            get{
                double scale=Zoom;var bed=Engine.Config.bed;
                int petWidth=pet.Engine.Width;
                // 有开花的植株就蹲在它旁边，看起来像在照看那一株。
                int targetX=(bed.left+bed.right)/2;
                var blooming=Engine.Plants.Where(p=>GardenEngine.IsBlooming(p)&&p.slot>=0&&p.slot<Engine.Config.slots.Count)
                    .OrderByDescending(p=>p.growth).FirstOrDefault();
                if(blooming!=null)targetX=Engine.Config.slots[blooming.slot].x+(random.Next(2)==0?13:-13);
                double soilX=Left+targetX*scale-petWidth/2.0+(random.Next(-9,10));
                double soilY=Top+(bed.bottom-7)*scale;
                return new Point((int)Math.Round(soilX),(int)Math.Round(soilY-petWidth*.96));
            }
        }
        void ReleasePointer(){
            if(!down)return;
            down=false;Capture=false;
            if(dragged){Location=Clamp(Location);pet.Settings.GardenX=Left;pet.Settings.GardenY=Top;pet.Save();Draw();}
        }
        protected override void OnMouseDown(MouseEventArgs e){
            base.OnMouseDown(e);
            if(e.Button==MouseButtons.Left){down=true;dragged=false;pressCursor=Cursor.Position;pressLocation=Location;Capture=true;}
            if(e.Button==MouseButtons.Right){down=false;Capture=false;menu.Show(Cursor.Position);}
        }
        protected override void OnMouseMove(MouseEventArgs e){
            base.OnMouseMove(e);
            if(!down)return;
            Point cursor=Cursor.Position;
            if(!dragged&&BehaviorEngine.Distance(cursor.X,cursor.Y,pressCursor.X,pressCursor.Y)>4)dragged=true;
            if(dragged)Location=Clamp(new Point(pressLocation.X+cursor.X-pressCursor.X,pressLocation.Y+cursor.Y-pressCursor.Y));
        }
        protected override void OnMouseUp(MouseEventArgs e){
            base.OnMouseUp(e);
            if(e.Button!=MouseButtons.Left)return;
            bool wasDrag=dragged;
            ReleasePointer();
            if(!wasDrag)Water();
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e){
            base.OnMouseDoubleClick(e);
            if(e.Button==MouseButtons.Left)pet.OpenPanel(CompanionPanel.Section.Garden);
        }
        Point Clamp(Point point){
            int width=PixelWidth,height=PixelHeight;
            Rectangle work=Screen.FromPoint(new Point(point.X+width/2,point.Y+height/2)).WorkingArea;
            return new Point(Math.Max(work.Left,Math.Min(work.Right-width,point.X)),Math.Max(work.Top,Math.Min(work.Bottom-height,point.Y)));
        }
        public void Water(){Engine.Water();Draw();GardenEngine.Save(Engine);if(Changed!=null)Changed();}
        public bool PlantSeed(string speciesId){
            GardenPlant plant=Engine.Plant(speciesId);
            if(plant==null)return false;
            GardenEngine.Save(Engine);Draw();
            Announce(plant);
            if(Changed!=null)Changed();return true;
        }
        /// <summary>播种后立刻给出反馈：花圃里扬土冒光，托盘弹一句，面板也显示几秒。</summary>
        void Announce(GardenPlant plant){
            GardenSpecies species=Engine.Config.SpeciesOf(plant.species);
            if(species==null)return;
            double now=watch.Elapsed.TotalSeconds;
            sowAt=now;sowText="已经撒下 "+species.name+"，现在还是"+Engine.StageName(plant)+"。";
            // 连着撒好几包时不刷屏，4 秒内只弹一次气泡。
            if(now-lastSowTip>4){
                lastSowTip=now;
                pet.NotifyGarden(sowText+"土干了记得浇水。");
            }
        }
        double lastSowTip=-10;
        /// <summary>刚撒完种的提示语，6 秒内有效；面板用它顶替常规提示。</summary>
        public string SownMessage{
            get{return sowText!=null&&watch.Elapsed.TotalSeconds-sowAt<6?sowText:null;}
        }
        public void ApplyScale(){
            ClientSize=new Size(PixelWidth,PixelHeight);
            Location=Clamp(Location);Draw();
        }
        public void ApplyTopmost(){
            TopMost=pet.Settings.Topmost;
            Native.SetWindowPos(Handle,new IntPtr(pet.Settings.Topmost?-1:-2),0,0,0,0,0x13);
            Draw();
        }
        public void SetVisible(bool visible){
            pet.Settings.GardenVisible=visible;
            if(visible){Show();Draw();}else Hide();
            pet.Save();if(Changed!=null)Changed();
        }
        ContextMenuStrip BuildMenu(){
            var strip=new ContextMenuStrip{Font=new Font("Microsoft YaHei UI",9)};
            var water=new ToolStripMenuItem("浇水",null,delegate{Water();});strip.Items.Add(water);
            var seeds=new ToolStripMenuItem("播种");foreach(var species in Engine.Config.species){
                GardenSpecies target=species;
                seeds.DropDownItems.Add(species.name,null,delegate{
                    if(!PlantSeed(target.id))MessageBox.Show(this,"花圃已经长满了。","蜜柑的花园",MessageBoxButtons.OK,MessageBoxIcon.Asterisk);
                });
            }
            strip.Items.Add(seeds);
            strip.Items.Add(new ToolStripSeparator());
            strip.Items.Add("让蜜柑过来看看花",null,delegate{pet.VisitGarden(RestPoint,PickInteraction());});
            strip.Items.Add("打开花园面板…",null,delegate{pet.OpenPanel(CompanionPanel.Section.Garden);});
            strip.Items.Add(new ToolStripSeparator());
            var top=new ToolStripMenuItem("始终置顶",null,delegate{pet.Settings.Topmost=!pet.Settings.Topmost;pet.ApplySettings();ApplyTopmost();});
            strip.Items.Add(top);
            strip.Items.Add("收起花园",null,delegate{SetVisible(false);});
            strip.Opening+=delegate{
                water.Text="浇水  ·  土壤 "+Engine.WateredPercent+"%";
                water.Enabled=Engine.Moisture<.995;
                seeds.Enabled=!Engine.Full;top.Checked=pet.Settings.Topmost;
            };
            return strip;
        }
        protected override void Dispose(bool disposing){
            if(disposing){
                if(!disposed){disposed=true;timer.Stop();timer.Dispose();GardenEngine.Save(Engine);if(menu!=null)menu.Dispose();if(Renderer!=null)Renderer.Dispose();}
            }
            base.Dispose(disposing);
        }
    }
}
