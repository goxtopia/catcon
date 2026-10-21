using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace Catcon {
    public sealed class CompanionPanel : Form {
        public enum Section{Home=0,Collection=1,Garden=2,Settings=3}
        readonly PetWindow pet;readonly string root;readonly Timer previewTimer;
        readonly Label status,detail;readonly Preview preview;readonly FlowLayoutPanel actions;
        readonly TabControl tabs;Label gardenStatus,gardenHint;GardenPreview gardenPreview;
        readonly Color paper=Color.FromArgb(246,244,236),ink=Color.FromArgb(53,67,59),muted=Color.FromArgb(111,122,110),sage=Color.FromArgb(225,233,218);
        public CompanionPanel(PetWindow pet,string root):this(pet,root,Section.Home){}
        public CompanionPanel(PetWindow pet,string root,Section section){
            this.pet=pet;this.root=root;Text="蜜柑的小窝 · CATCON";Icon=pet.Icon;ClientSize=new Size(610,780);MinimumSize=new Size(626,700);MaximumSize=new Size(626,1100);
            StartPosition=FormStartPosition.CenterScreen;BackColor=paper;ForeColor=ink;Font=new Font("Microsoft YaHei UI",9F);AutoScaleMode=AutoScaleMode.Dpi;
            var header=new Panel{Dock=DockStyle.Top,Height=80,Padding=new Padding(24,18,24,8)};
            header.Controls.Add(new Label{Text="一只猫，一点陪伴。",Font=new Font("Microsoft YaHei UI",19,FontStyle.Bold),ForeColor=ink,AutoSize=true,Location=new Point(24,13)});
            header.Controls.Add(new Label{Text="CATCON  /  蜜柑的小窝",Font=new Font("Segoe UI",8,FontStyle.Bold),ForeColor=muted,AutoSize=true,Location=new Point(26,52)});
            var top=new Panel{Dock=DockStyle.Top,Height=188,Padding=new Padding(24,0,24,12)};
            preview=new Preview(pet){Bounds=new Rectangle(24,0,184,166)};top.Controls.Add(preview);
            top.Controls.Add(new Label{Text="蜜柑",Font=new Font("Microsoft YaHei UI",23,FontStyle.Bold),AutoSize=true,Location=new Point(228,14)});
            top.Controls.Add(new Label{Text="橘白猫  ·  原生 32 × 32 像素",AutoSize=true,ForeColor=muted,Location=new Point(232,58)});
            status=new Label{Bounds=new Rectangle(232,85,335,24),Font=new Font("Microsoft YaHei UI",10,FontStyle.Bold)};top.Controls.Add(status);
            detail=new Label{Bounds=new Rectangle(232,113,335,42),ForeColor=muted};top.Controls.Add(detail);
            var tabsLocal=new TabControl{Dock=DockStyle.Fill,Padding=new Point(20,10),Font=Font};tabs=tabsLocal;
            var home=Page(tabsLocal,"相处");var collection=Page(tabsLocal,"动作图鉴");var garden=Page(tabsLocal,"花园");var settings=Page(tabsLocal,"设置");
            var homeFlow=Flow(home);homeFlow.Controls.Add(Heading("今天也想和你待在一起。"));homeFlow.Controls.Add(Note("单击摸头 · 双击打开小窝 · 拖拽移动 · 右键打开菜单"));
            var quick=new FlowLayoutPanel{Width=538,Height=106,Margin=new Padding(0,12,0,8)};
            quick.Controls.Add(Button("摸摸脑袋",delegate{pet.PlayEvent("pet");},166));quick.Controls.Add(Button("吃小鱼干",delegate{pet.Play("eat");},166));quick.Controls.Add(Button("喝点水",delegate{pet.Play("drink");},166));
            quick.Controls.Add(Button("玩毛线球",delegate{pet.Play("yarn");},166));quick.Controls.Add(Button("睡一会儿",delegate{pet.Play("sleep");},166));quick.Controls.Add(Button("追鼠标",delegate{pet.ScheduleChase();detail.Text="3 秒后出发，把鼠标移到目标位置。";},166));homeFlow.Controls.Add(quick);
            homeFlow.Controls.Add(Heading("陪伴情境"));homeFlow.Controls.Add(Note("自动跟随前台应用；也可以手动切换，看看猫咪的反应。"));
            var contexts=new ComboBox{Width=516,DropDownStyle=ComboBoxStyle.DropDownList,Margin=new Padding(0,8,0,12)};
            contexts.Items.Add("自动识别前台应用");foreach(var pair in ContextNames.All)contexts.Items.Add(pair.Value);contexts.SelectedIndex=0;
            contexts.SelectedIndexChanged+=delegate{pet.OverrideContext=contexts.SelectedIndex==0?null:ContextNames.All.Keys.ElementAt(contexts.SelectedIndex-1);if(pet.OverrideContext!=null){var choices=pet.Content.Behavior.contexts[pet.OverrideContext];pet.Play(choices[0].clip);}UpdateStatus();};homeFlow.Controls.Add(contexts);
            var controls=new FlowLayoutPanel{Width=538,Height=48};var pause=Button("暂停 / 继续",delegate{pet.Engine.Paused=!pet.Engine.Paused;UpdateStatus();},253);controls.Controls.Add(pause);controls.Controls.Add(Button("把猫找回来",delegate{pet.Recall();},253));homeFlow.Controls.Add(controls);
            homeFlow.Controls.Add(Note("关掉这个小窝后，蜜柑仍会陪着你。完全退出请用托盘或猫咪右键菜单。"));
            var library=Flow(collection);library.Controls.Add(Heading("每一种小动作，都有点心情。"));library.Controls.Add(Note("点击预览。动作播放结束后恢复自主活动；暂停可定格当前画面。"));
            actions=new FlowLayoutPanel{Width=538,Height=600,MaximumSize=new Size(538,0),AutoSize=true,Margin=new Padding(0,12,0,0)};library.Controls.Add(actions);BuildActions();
            var gardenFlow=Flow(garden);gardenFlow.Controls.Add(Heading("一块土，慢慢长成一小片花圃。"));
            gardenFlow.Controls.Add(Note("单击花圃浇水 · 拖拽移动 · 右键播种。撒下种子时土会扬起一小把、嫩芽上冒几个光点；花开之后偶尔有风经过，会落下像素花瓣，蜜柑会跑过来接。"));
            gardenPreview=new GardenPreview(pet){Width=516,Height=296,Margin=new Padding(0,6,0,6)};gardenFlow.Controls.Add(gardenPreview);
            gardenStatus=new Label{Width=516,Height=24,Font=new Font("Microsoft YaHei UI",9,FontStyle.Bold),ForeColor=ink,Margin=new Padding(0,4,0,2)};gardenFlow.Controls.Add(gardenStatus);
            gardenHint=new Label{Width=516,Height=22,ForeColor=muted,Margin=new Padding(0,0,0,6)};gardenFlow.Controls.Add(gardenHint);
            var gardenButtons=new FlowLayoutPanel{Width=538,Height=52};
            gardenButtons.Controls.Add(Button("浇水",delegate{if(pet.Garden!=null)pet.Garden.Water();UpdateStatus();},166));
            gardenButtons.Controls.Add(Button("让蜜柑过来",delegate{if(pet.Garden!=null){pet.Garden.Show();pet.Garden.Draw();pet.VisitGarden(pet.Garden.RestPoint,pet.Garden.PickInteraction());}},166));
            gardenButtons.Controls.Add(Button("显示 / 收起花园",delegate{if(pet.Garden!=null)pet.Garden.SetVisible(!pet.Settings.GardenVisible);UpdateStatus();},166));
            gardenFlow.Controls.Add(gardenButtons);
            gardenFlow.Controls.Add(Heading("播种"));
            var seeds=new ComboBox{Width=516,DropDownStyle=ComboBoxStyle.DropDownList,Margin=new Padding(0,6,0,6)};
            if(pet.Garden!=null)foreach(var species in pet.Garden.Engine.Config.species)seeds.Items.Add(new SeedItem(species));
            if(seeds.Items.Count>0)seeds.SelectedIndex=0;gardenFlow.Controls.Add(seeds);
            gardenFlow.Controls.Add(Button("撒下这一包种子",delegate{
                if(pet.Garden==null||!(seeds.SelectedItem is SeedItem))return;
                if(!pet.Garden.PlantSeed(((SeedItem)seeds.SelectedItem).Species.id))MessageBox.Show(this,"花圃已经长满了，先让它们开一阵子吧。","蜜柑的花园",MessageBoxButtons.OK,MessageBoxIcon.Asterisk);
                UpdateStatus();
            },253));
            gardenFlow.Controls.Add(Heading("花园设置"));
            AddCheck(gardenFlow,"在桌面显示花园窗口",pet.Settings.GardenVisible,v=>{pet.Settings.GardenVisible=v;if(pet.Garden!=null)pet.Garden.SetVisible(v);UpdateStatus();});
            AddCheck(gardenFlow,"蜜柑偶尔自己走去花园看看",pet.Settings.GardenVisits,v=>pet.Settings.GardenVisits=v);
            var gardenScale=new ComboBox{Width=516,DropDownStyle=ComboBoxStyle.DropDownList,Margin=new Padding(0,6,0,10)};
            double currentScale=Preferences.SnapGardenScale(pet.Settings.GardenScale);
            int canvasW=pet.Garden!=null?pet.Garden.Engine.Config.canvas.width:112;
            int canvasH=pet.Garden!=null?pet.Garden.Engine.Config.canvas.height:80;
            foreach(double option in Preferences.GardenScales){
                gardenScale.Items.Add(option.ToString("0.#",CultureInfo.InvariantCulture)+" 倍  ·  "+
                    (int)Math.Round(canvasW*option)+" × "+(int)Math.Round(canvasH*option)+" 屏幕像素"+
                    (Math.Abs(option-3)<.001?"　（默认）":""));
                if(Math.Abs(option-currentScale)<.001)gardenScale.SelectedIndex=gardenScale.Items.Count-1;
            }
            gardenScale.SelectedIndexChanged+=delegate{
                pet.Settings.GardenScale=Preferences.GardenScales[gardenScale.SelectedIndex];
                if(pet.Garden!=null)pet.Garden.ApplyScale();
                pet.Save();
            };gardenFlow.Controls.Add(gardenScale);
            gardenFlow.Controls.Add(Note("植物从幼苗开始生长，土壤会慢慢变干；缺水时花圃左上角会出现水滴提醒，点一下花圃即可浇水。长成之后会自然分株，最多 12 株。"));
            var settingsFlow=Flow(settings);settingsFlow.Controls.Add(Heading("按你的节奏相处"));
            AddCheck(settingsFlow,"始终置顶",pet.Settings.Topmost,v=>pet.Settings.Topmost=v);
            AddCheck(settingsFlow,"允许自己散步和避让鼠标",pet.Settings.Wander,v=>pet.Settings.Wander=v);
            AddCheck(settingsFlow,"允许鼠标靠近互动（炸毛 / 停留避让）",pet.Settings.Reactions,v=>pet.Settings.Reactions=v);
            AddCheck(settingsFlow,"根据前台应用选择动作",pet.Settings.Classify,v=>pet.Settings.Classify=v);
            AddCheck(settingsFlow,"智能补充识别（线索不足时自由活动）",pet.Settings.SmartClassify,v=>pet.Settings.SmartClassify=v);
            AddCheck(settingsFlow,"游戏时安静陪伴（停用自主移动和鼠标反应）",pet.Settings.QuietGames,v=>pet.Settings.QuietGames=v);
            settingsFlow.Controls.Add(Heading("显示大小"));
            var scale=new ComboBox{Width=516,DropDownStyle=ComboBoxStyle.DropDownList,Margin=new Padding(0,6,0,10)};
            for(int i=2;i<=6;i++)scale.Items.Add(i+" 倍  ·  "+(32*i)+" × "+(32*i)+" 屏幕像素");scale.SelectedIndex=pet.Settings.Scale-2;
            scale.SelectedIndexChanged+=delegate{pet.Settings.Scale=scale.SelectedIndex+2;pet.ApplySettings();};settingsFlow.Controls.Add(scale);
            settingsFlow.Controls.Add(Heading("皮肤与动作包"));
            var skins=new ComboBox{Width=516,DropDownStyle=ComboBoxStyle.DropDownList,Margin=new Padding(0,6,0,10)};
            string[] folders=Directory.GetDirectories(Path.Combine(root,"assets","skins")).Where(d=>File.Exists(Path.Combine(d,"skin.json"))).OrderBy(d=>d).ToArray();
            foreach(string folder in folders)skins.Items.Add(Path.GetFileName(folder));skins.SelectedItem=pet.Settings.SkinId;settingsFlow.Controls.Add(skins);
            var skinButtons=new FlowLayoutPanel{Width=538,Height=48};skinButtons.Controls.Add(Button("应用 / 重新加载",delegate{
                string error;if(pet.Reload((string)skins.SelectedItem,out error)){BuildActions();UpdateStatus();detail.Text="素材与配置已重新加载。";}else MessageBox.Show(this,"配置校验未通过，当前猫咪继续使用原素材。\n\n"+error,"素材包有点问题",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            },253));skinButtons.Controls.Add(Button("打开素材目录",delegate{System.Diagnostics.Process.Start("explorer.exe","\""+Path.Combine(root,"assets","skins")+"\"");},253));settingsFlow.Controls.Add(skinButtons);
            settingsFlow.Controls.Add(Note("本地识别：匹配进程名、应用产品信息和窗口标题；未命中时按关键词推断，线索不足或冲突时自由活动。窗口标题仅用于即时匹配，不保存、不上传。全屏仅作为弱线索。"));
            settingsFlow.Controls.Add(Note("原生透明窗口不支持覆盖 Windows 安全桌面；独占全屏游戏可能遮挡猫咪，可使用无边框窗口模式。"));
            Controls.Add(tabs);Controls.Add(top);Controls.Add(header);
            previewTimer=new Timer{Interval=100};previewTimer.Tick+=delegate{preview.Invalidate();if(gardenPreview!=null)gardenPreview.Invalidate();UpdateStatus();};previewTimer.Start();pet.StatusChanged+=UpdateStatus;UpdateStatus();Select(section);
        }
        public void Select(Section section){if(tabs==null||tabs.IsDisposed)return;int index=(int)section;if(index>=0&&index<tabs.TabPages.Count)tabs.SelectedIndex=index;}
        protected override void Dispose(bool disposing){if(disposing&&previewTimer!=null){previewTimer.Stop();previewTimer.Dispose();pet.StatusChanged-=UpdateStatus;}base.Dispose(disposing);}
        TabPage Page(TabControl tabs,string title){var p=new TabPage(title){BackColor=paper,Padding=new Padding(18)};tabs.TabPages.Add(p);return p;}
        FlowLayoutPanel Flow(Control parent){var f=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false};parent.Controls.Add(f);return f;}
        Label Heading(string text){return new Label{Text=text,Width=516,Height=30,Font=new Font("Microsoft YaHei UI",11,FontStyle.Bold),Margin=new Padding(0,12,0,0)};}
        Label Note(string text){return new Label{Text=text,Width=516,Height=TextRenderer.MeasureText(text,Font,new Size(516,300),TextFormatFlags.WordBreak).Height+14,ForeColor=muted,Margin=new Padding(0,3,0,5)};}
        Button Button(string text,Action action,int width){var b=new Button{Text=text,Width=width,Height=40,BackColor=sage,ForeColor=ink,FlatStyle=FlatStyle.Flat,Cursor=Cursors.Hand,Margin=new Padding(0,0,8,8)};b.FlatAppearance.BorderSize=0;b.Click+=delegate{action();};return b;}
        void AddCheck(FlowLayoutPanel flow,string text,bool value,Action<bool> update){var c=new CheckBox{Text=text,Checked=value,Width=516,Height=30,Margin=new Padding(0,3,0,3)};c.CheckedChanged+=delegate{update(c.Checked);pet.ApplySettings();};flow.Controls.Add(c);}
        void BuildActions(){foreach(Control c in actions.Controls.Cast<Control>().ToArray())c.Dispose();actions.Controls.Clear();foreach(var pair in pet.Content.Skin.clips){string id=pair.Key;actions.Controls.Add(Button(pair.Value.label,delegate{pet.Play(id);},166));}}
        void UpdateStatus(){
            if(IsDisposed)return;string category=pet.OverrideContext??pet.Monitor.Category;status.Text=(pet.Engine.Paused?"暂停中  ·  ":"")+pet.Engine.Animation.Clip.label;detail.Text="陪伴情境："+ContextNames.All[category]+"\n"+(pet.OverrideContext==null?pet.Monitor.Reason:"手动选择的陪伴情境");
            if(gardenStatus!=null){
                if(pet.Garden==null){gardenStatus.Text="花园没有加载起来";if(gardenHint!=null)gardenHint.Text="请检查 config/garden.json 是否存在且通过校验。";}
                else{
                    var garden=pet.Garden.Engine;
                    gardenStatus.Text="植物 "+garden.Plants.Count+" / "+garden.Config.maxPlants+"　·　土壤湿度 "+garden.WateredPercent+"%　·　盛开 "+garden.Blooming+" 株";
                    if(gardenHint!=null){
                        string sown=pet.Garden.SownMessage;
                        if(sown!=null)gardenHint.Text=sown;
                        else if(garden.Plants.Count==0)gardenHint.Text="花圃还空着，撒一包种子吧。";
                        else if(garden.Critical)gardenHint.Text="土壤已经干透，植物暂停生长了，点一下花圃浇水。";
                        else if(garden.NeedsWater)gardenHint.Text="土壤有点干了，趁现在浇水。";
                        else if(garden.Full)gardenHint.Text="花圃已经长满，风一过就有花瓣飘下来。";
                        else gardenHint.Text="长成开花的植株会慢慢分出新的一株。";
                    }
                }
            }
        }
        sealed class SeedItem {
            public readonly GardenSpecies Species;public SeedItem(GardenSpecies species){Species=species;}
            public override string ToString(){return Species.name+"　"+Species.note;}
        }
        sealed class GardenPreview : Control {
            readonly PetWindow pet;public GardenPreview(PetWindow pet){this.pet=pet;DoubleBuffered=true;}
            protected override void OnPaint(PaintEventArgs e){
                e.Graphics.Clear(Color.FromArgb(233,235,222));using(var pen=new Pen(Color.FromArgb(221,225,210))){for(int x=0;x<Width;x+=12)e.Graphics.DrawLine(pen,x,0,x,Height);for(int y=0;y<Height;y+=12)e.Graphics.DrawLine(pen,0,y,Width,y);}
                if(pet.Garden==null)return;
                pet.Garden.Renderer.Render(pet.Garden.Engine);
                Bitmap frame=pet.Garden.Renderer.Frame(3);
                int left=(Width-frame.Width)/2,top=(Height-frame.Height)/2;
                e.Graphics.DrawImageUnscaled(frame,left,top);
                using(var pen=new Pen(Color.FromArgb(203,209,193)))e.Graphics.DrawRectangle(pen,left-1,top-1,frame.Width+1,frame.Height+1);
            }
        }
        sealed class Preview : Control {
            readonly PetWindow pet;public Preview(PetWindow pet){this.pet=pet;DoubleBuffered=true;}
            protected override void OnPaint(PaintEventArgs e){
                e.Graphics.Clear(Color.FromArgb(233,235,222));using(var pen=new Pen(Color.FromArgb(221,225,210))){for(int x=0;x<Width;x+=12)e.Graphics.DrawLine(pen,x,0,x,Height);for(int y=0;y<Height;y+=12)e.Graphics.DrawLine(pen,0,y,Width,y);}
                using(var frame=pet.Engine.Animation.Render())using(var scaled=Animator.Scale(frame,4))e.Graphics.DrawImageUnscaled(scaled,(Width-scaled.Width)/2,8);
                using(var font=new Font("Segoe UI",8,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,"MIKAN / II",font,new Rectangle(0,143,Width,20),Color.FromArgb(109,124,103),TextFormatFlags.HorizontalCenter);
            }
        }
    }
}
