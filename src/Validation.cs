using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace Catcon {
    static class Validation {
        public static int Run(string root,string report){
            var checks=new List<string>();var failures=new List<string>();
            Action<bool,string> check=(ok,name)=>{if(ok)checks.Add(name);else failures.Add(name);};
            try{using(var content=Content.Load(root,"mikan")){
                var s=content.Skin;var config=content.Behavior;var a=new Animator(s);
                check(s.canvas==32,"Native canvas is 32 x 32");check(s.clips.Count>=25,"Complete first animation pack");
                foreach(var pair in s.clips){
                    var hashes=new HashSet<string>();
                    for(int f=0;f<pair.Value.frames.Count;f++)using(var image=a.RenderAt(pair.Key,Animator.FrameStartSeconds(pair.Value,f)+.001,false)){
                        int pixels=0;bool transparent=false;for(int y=0;y<32;y++)for(int x=0;x<32;x++){if(image.GetPixel(x,y).A>0)pixels++;else transparent=true;}
                        check(pixels>80&&transparent,"Visible transparent frame "+pair.Key+"/"+f);
                        using(var stream=new MemoryStream()){image.Save(stream,System.Drawing.Imaging.ImageFormat.Png);using(var sha=SHA256.Create())hashes.Add(Convert.ToBase64String(sha.ComputeHash(stream.ToArray())));}
                    }
                    check(hashes.Count>1,"Animation actually changes pixels: "+pair.Key);
                }
                var timing=new Clip{frameMs=100,frames=new List<Dictionary<string,LayerPatch>>{new Dictionary<string,LayerPatch>(),new Dictionary<string,LayerPatch>(),new Dictionary<string,LayerPatch>()}};
                check(Animator.FrameIndex(timing,.101)==1&&Animator.FrameIndex(timing,.301)==0,"Legacy uniform frame timing remains compatible");
                timing.frameDurations=new List<int>{100,200,100};
                check(Animator.FrameIndex(timing,.099)==0&&Animator.FrameIndex(timing,.101)==1&&Animator.FrameIndex(timing,.299)==1&&Animator.FrameIndex(timing,.301)==2,"Authored timing respects short and held frames");
                timing.playback="once";check(Animator.FrameIndex(timing,10)==2,"One-shot animations hold their final drawing");
                timing.playback="loop";check(Animator.FrameIndex(timing,.401)==0,"Loop timeline wraps at its authored duration");
                check(s.clips["pounce"].playback=="once"&&s.clips["pounce"].frames.Select(f=>f["body"].sprite).Distinct().Count()>=8,"Pounce uses eight independently drawn body poses");
                check(s.clips["run"].frames.Select(f=>f["body"].sprite).Distinct().Count()>=6&&s.clips["run"].frames.All(f=>f["head"].sprite.StartsWith("face_side_")),"Run uses a six-phase gait with actual profile heads");
                check(s.clips["chase_tail"].frames.Any(f=>f["head"].sprite=="face_back")&&!s.clips["chase_tail"].deformers.Any(d=>d.type=="spin"),"Tail chase turns through a drawn rear view without spin deformation");
                check(s.clips.Values.SelectMany(c2=>c2.frames).SelectMany(f=>f.Values).All(p=>!p.rotation.HasValue&&!p.sx.HasValue&&!p.sy.HasValue),"Redrawn pack does not rotate or stretch bitmap poses");
                check(s.poses["watch"].First(l=>l.id=="head").sprite.EndsWith("_right"),"Video companion faces its screen");
                foreach(var pair in s.clips.ToArray()){
                    var sample=new Clip{pose=pair.Value.pose,seconds=6,frameMs=6000,playback="loop",frames=new List<Dictionary<string,LayerPatch>>{pair.Value.frames[0]}};
                    s.clips["cycle_validation"]=sample;
                    try{
                        var hashes=new HashSet<string>();
                        foreach(double time in new[]{.001,.161,.301,.601,.701,1.201,1.801})using(var image=a.RenderAt("cycle_validation",time,false))using(var stream=new MemoryStream()){
                            image.Save(stream,System.Drawing.Imaging.ImageFormat.Png);using(var sha=SHA256.Create())hashes.Add(Convert.ToBase64String(sha.ComputeHash(stream.ToArray())));
                        }
                        check(hashes.Count>1,"Tail moves independently while the body pose is held: "+pair.Key);
                        var timeline=Animator.ExportTimeline(s,pair.Value);
                        check(timeline[0]==0&&timeline.Last()==(int)Animator.TimelineMilliseconds(pair.Value)&&timeline.Zip(timeline.Skip(1),(x,y)=>y>x).All(ok=>ok),"Export preserves body timing and independent component events: "+pair.Key);
                    }finally{s.clips.Remove("cycle_validation");}
                }
                for(int f=0;f<s.clips["yarn"].frames.Count;f++)foreach(bool mirror in new[]{false,true}){
                    var paw=s.clips["yarn"].frames[f]["left"];bool? visible=paw.visible;double t=Animator.FrameStartSeconds(s.clips["yarn"],f)+.001;
                    using(var full=a.RenderAt("yarn",t,mirror)){
                        Bitmap hidden;try{paw.visible=false;hidden=a.RenderAt("yarn",t,mirror);}finally{paw.visible=visible;}
                        using(hidden){int visibleTip=0;for(int y=0;y<32;y++)for(int x=0;x<32;x++)if(full.GetPixel(x,y).ToArgb()==ColorTranslator.FromHtml(s.palette["c"]).ToArgb()&&full.GetPixel(x,y)!=hidden.GetPixel(x,y))visibleTip++;
                            check(visibleTip>=3,"Yarn paw remains visible after head and ball compositing: "+f+" / "+mirror);
                        }
                    }
                }
                var c=content.Classifiers;
                check(c.Classify("Code","project",false)=="coding","VS Code");check(c.Classify("cursor","",false)=="coding","Cursor IDE");
                check(c.Classify("chrome","a video - YouTube",false)=="video","Browser video priority");
                check(c.Classify("msedge","GitHub - project",false)=="coding","Browser coding priority");
                check(c.Classify("firefox","Google Docs",false)=="office","Browser documents priority");
                check(c.Classify("chrome","Discord",false)=="chat","Browser chat priority");
                check(c.Classify("chrome","Random page",true)=="browsing","Fullscreen browsers stay browsing");
                check(c.Classify("Weixin","",false)=="chat","WeChat");check(c.Classify("excel","",false)=="office","Excel");
                check(c.Classify("mpv","",true)=="video","Fullscreen video");check(c.Classify("cs2","",true)=="gaming","Known game");
                check(c.Classify("somegame","",true)=="idle","Fullscreen alone has insufficient confidence");check(c.Classify("explorer","",true)=="idle","Desktop exclusion");
                check(c.Classify("unknown","",false)=="idle","Unknown window fallback");
                foreach(string app in new[]{"Codex","OpenAI.Codex","Codex-desktop","codex-code-mode-host","codex-command-runner-0.163.0-alpha.10.2","WorkBuddy","work-buddy"})check(c.Classify(app,"",true)=="coding","Coding assistant process: "+app);
                foreach(string app in new[]{"WXWork","WXWorkWeb","WeCom","wecom-agent","WeWork","WeChatWork","WeChatAppEx"})check(c.Classify(app,"",true)=="chat","Enterprise WeChat process: "+app);
                check(c.Classify("msedgewebview2","Codex · Workspace",false)=="coding","Codex native shared-host title");
                check(c.Classify("applicationframehost","WorkBuddy",false)=="coding","WorkBuddy packaged-host title");
                check(c.Classify("msedgewebview2","企业微信",false)=="chat","Enterprise WeChat shared-host title");
                check(c.ClassifyDetailed("renamed-shell","项目",false,"Codex Desktop",true).Category=="coding","Product identity recognizes renamed Codex executable");
                check(c.ClassifyDetailed("renamed-shell","会话",false,"企业微信",true).Category=="chat","Product identity recognizes enterprise chat");
                var inferred=c.ClassifyDetailed("customeditor","main.rs — cargo compile",false,"",true);
                check(inferred.Category=="coding"&&inferred.Source=="keywords"&&inferred.Confidence>=c.fallback.minConfidence,"Independent coding clues infer a confident result");
                check(c.Classify("unknown-player","clip.mkv - now playing",false)=="video","Unknown video player keyword fallback");
                check(c.Classify("unknown-doc","周报.docx - 文档编辑",false)=="office","Unknown document editor keyword fallback");
                check(c.Classify("unknown-chat","Group chat - unread messages",false)=="chat","Unknown messaging client keyword fallback");
                check(c.Classify("unknown-game","游戏大厅",true)=="gaming","Fullscreen supports meaningful game clues");
                check(c.Classify("unknown","project code work",false)=="idle","Weak generic words abstain");
                var conflict=c.ClassifyDetailed("unknown","Codex Discord",false,"",true);
                check(conflict.Category=="idle"&&!conflict.Accepted&&conflict.Margin<c.fallback.minMargin,"Conflicting strong categories abstain");
                check(c.Classify("unknown",String.Join(" ",Enumerable.Repeat("compile",80)),false)=="idle","Repeated weak keyword cannot inflate confidence");
                check(c.ClassifyDetailed("customeditor","main.rs — cargo compile",false,"",false).Category=="idle","Smart fallback can be disabled independently");
                check(c.Classify("chrome","main.rs — cargo compile",false)=="coding","Unknown browser content can infer before generic browsing");
                check(c.Classify("chrome","Codex 教程 - YouTube",false)=="video","Explicit browser video rule outranks coding keywords");
                check(c.Classify("WorkBuddy","视频播放",true)=="coding"&&c.Classify("WXWorkWeb","GitHub",true)=="chat","Known requested app categories outrank incidental title keywords");
                check(c.Classify(null,null,false)=="idle","Missing process and title remain idle");
                check(c.ClassifyDetailed("Codex","",false,"",false).Category=="coding"&&c.ClassifyDetailed("WorkBuddy","",false,"",false).Category=="coding"&&c.ClassifyDetailed("WXWorkWeb","",false,"",false).Category=="chat","Requested applications stay exact matches with smart mode disabled");
                check(c.Classify("chrome","main.rs compile group chat unread messages",false)=="idle","Conflicting browser content abstains instead of forcing a category");
                var monitor=new ContextMonitor(c);var exact=c.ClassifyDetailed("WorkBuddy","",false,"",true);
                monitor.Observe(exact,"workbuddy");check(monitor.Category=="idle","A single new context sample does not flicker");
                monitor.Observe(exact,"workbuddy");check(monitor.Category=="coding","Stable coding application samples are adopted");
                monitor.Observe(conflict,"unknown");check(monitor.Category=="coding","A single uncertain sample does not erase a stable context");
                monitor.Observe(conflict,"unknown");check(monitor.Category=="idle","Stable low-confidence samples fall back to idle");
                var input=new InputFrame{WorkArea=new Rectangle(0,0,1600,900),Cursor=new Point(1400,800)};
                var engine=new BehaviorEngine(s,config,1){X=400,Y=400,PixelScale=4};
                engine.Advance(.04,input);input.Cursor=new Point(470,470);engine.Advance(.04,input);
                check(engine.Animation.Current==config.events["startle"],"Fast approach startles");
                for(int count=0;count<2;count++){
                    input.Cursor=new Point(1400,800);for(int j=0;j<130;j++)engine.Advance(.04,input);
                    input.Cursor=new Point((int)engine.X+64,(int)engine.Y+70);engine.Advance(.04,input);
                }
                check(engine.Animation.Current==config.events["repeatedStartle"],"Three startles settle into loaf");
                engine=new BehaviorEngine(s,config,2){X=400,Y=400,PixelScale=4};input.Cursor=new Point(465,470);input.Reactions=true;
                for(int j=0;j<135;j++)engine.Advance(.04,input);
                check(engine.Animation.Current==config.events["dwell"]&&engine.Moving,"Cursor dwell causes retreat");
                engine=new BehaviorEngine(s,config,3){X=400,Y=400,PixelScale=4};engine.Emit("chase");input.Cursor=new Point(1100,600);
                bool arrived=false;for(int j=0;j<300;j++){engine.Advance(.02,input);if(engine.Animation.Current=="pounce"){arrived=true;break;}}
                check(arrived&&engine.Moving,"Chase stops short and starts a moving pounce");
                double contactX=(s.clips["pounce"].contactAnchor[0]+.5)/s.canvas,contactY=(s.clips["pounce"].contactAnchor[1]+.5)/s.canvas;
                double launchX=engine.X,launchY=engine.Y;
                check(Math.Abs(BehaviorEngine.Distance(launchX,launchY,input.Cursor.X-engine.Width*(1-contactX),input.Cursor.Y-engine.Width*contactY)-s.clips["pounce"].leap.distance*engine.PixelScale)<2,"Run reserves an actual body-length leap before the cursor");
                engine.Advance(.1,input);check(Math.Abs(engine.X-launchX)<.01&&Math.Abs(engine.Y-launchY)<.01,"Pounce anticipation holds its launch position");
                input.Cursor=new Point(1300,700);engine.Advance(.295,input);
                double landingX=1100-engine.Width*(1-contactX),landingY=600-engine.Width*contactY;
                check(engine.X>launchX+20&&engine.X<landingX-20&&engine.Y<(launchY+landingY)/2-20,"Flight advances through space and rises above the approach line");
                engine.Advance(.165,input);input.Cursor=new Point(1100,600);
                check(!engine.Moving,"Pounce ends displacement on the impact frame");
                check(engine.Animation.FaceRight&&Math.Abs(engine.X+engine.Width*(1-contactX)-input.Cursor.X)<2&&Math.Abs(engine.Y+engine.Width*contactY-input.Cursor.Y)<2,"Pounce aligns its drawn contact pixel with the cursor");
                using(var impact=a.RenderAt("pounce",Animator.FrameStartSeconds(s.clips["pounce"],5)+.001,true))check(impact.GetPixel(28,27).A>0,"Contact anchor reaches a visible leading paw in the impact drawing");
                check(engine.X>=0&&engine.X+128<=1600&&engine.Y>=0&&engine.Y+128<=900,"Pounce stays in working area");
                Action<double,double,int,Point,Rectangle,string> leapCase=(startX,startY,scale,cursor,work,name)=>{
                    var pet=new BehaviorEngine(s,config,31){X=startX,Y=startY,PixelScale=scale};
                    var state=new InputFrame{WorkArea=work,Cursor=cursor,Reactions=false,Movement=false,Context="coding"};pet.Emit("chase");
                    for(int j=0;j<500&&pet.Animation.Current!="pounce";j++)pet.Advance(.017,state);
                    double sx=pet.X,sy=pet.Y;bool facing=pet.Animation.FaceRight;
                    PointF end=BehaviorEngine.Clamp((float)(cursor.X-pet.Width*(facing?1-contactX:contactX)),(float)(cursor.Y-pet.Width*contactY),work,pet.Width);
                    check(pet.Animation.Current=="pounce"&&BehaviorEngine.Distance(sx,sy,end.X,end.Y)>1,name+": actual takeoff distance");
                    bool inside=true;for(int j=0;j<19;j++){pet.Advance(.033,state);inside&=pet.X>=work.Left&&pet.Y>=work.Top&&pet.X+pet.Width<=work.Right+.01&&pet.Y+pet.Width<=work.Bottom+.01;}
                    check(inside&&Math.Abs(pet.X-end.X)<.01&&Math.Abs(pet.Y-end.Y)<.01&&!pet.Moving,name+": bounded impact at locked target");
                    check(pet.Animation.Current=="pounce",name+": landing animation completes despite context change and disabled wandering");
                };
                leapCase(800,400,4,new Point(300,500),new Rectangle(0,0,1600,900),"Leftward leap");
                leapCase(400,400,4,new Point(464,180),new Rectangle(0,0,1600,900),"Upward leap");
                leapCase(400,200,4,new Point(464,650),new Rectangle(0,0,1600,900),"Downward leap");
                leapCase(400,400,2,new Point(1000,550),new Rectangle(0,0,1600,900),"Small scale leap");
                leapCase(400,400,6,new Point(1000,550),new Rectangle(0,0,1600,900),"Large scale leap");
                leapCase(400,400,4,new Point(550,520),new Rectangle(0,0,1600,900),"Nearby target leap");
                leapCase(400,400,4,new Point(0,0),new Rectangle(0,0,1600,900),"Top left screen edge");
                leapCase(400,400,4,new Point(1599,899),new Rectangle(0,0,1600,900),"Bottom right screen edge");
                leapCase(-1200,300,4,new Point(-500,200),new Rectangle(-1920,-100,1920,1080),"Negative monitor leap");
                engine=new BehaviorEngine(s,config,32){X=400,Y=400,PixelScale=4};engine.Animation.FaceRight=true;engine.Play("pounce",true);
                input.Reactions=false;engine.Advance(.4,input);double flyingX=engine.X;engine.Paused=true;engine.Advance(.1,input);
                check(engine.X==flyingX&&engine.X>400,"Pause freezes an airborne manual pounce");
                engine.Paused=false;engine.BeginDrag();engine.Advance(.3,input);check(!engine.Moving&&engine.X==flyingX,"Dragging cancels the leap trajectory");
                engine=new BehaviorEngine(s,config,4){X=-500,Y=-300,PixelScale=4};engine.KeepInside(new Rectangle(-1920,-100,1920,1080));
                check(engine.Y==-100&&engine.X==-500,"Negative-coordinate monitor clamp");
                engine.X=5000;engine.Y=5000;engine.KeepInside(new Rectangle(0,0,800,600));check(engine.X==672&&engine.Y==472,"Offscreen position recovery");
                engine=new BehaviorEngine(s,config,5){X=400,Y=400,PixelScale=4};engine.Paused=true;double elapsed=engine.Animation.Elapsed;engine.Advance(1,input);check(engine.Animation.Elapsed==elapsed,"Pause freezes animation");
                engine.Paused=false;engine.BeginDrag();engine.Advance(1,input);check(engine.Dragging&&engine.Animation.Current=="drag"&&!engine.Moving,"Drag suspends autonomous movement");engine.EndDrag();check(!engine.Dragging&&engine.Animation.Current=="sit","Drop returns to sitting");
                engine=new BehaviorEngine(s,config,6){X=400,Y=400,PixelScale=4};engine.Play("run",false);input.Movement=false;engine.Advance(.04,input);check(!engine.Moving&&engine.Animation.Current=="sit","Disabling movement cancels wander");
                input.Movement=true;input.Context="gaming";input.QuietGames=true;engine.Play("run",false);engine.Advance(.04,input);check(!engine.Moving,"Game quiet mode cancels movement");
                input.Context="coding";input.Reactions=false;input.Movement=false;
                engine=new BehaviorEngine(s,config,7){X=400,Y=400,PixelScale=4};for(int j=0;j<220;j++)engine.Advance(.05,input);
                check(config.contexts["coding"].Concat(config.contexts["idle"]).Any(x=>x.clip==engine.Animation.Current),"Context selects from preferred or everyday actions");
                engine=new BehaviorEngine(s,config,9){X=400,Y=400,PixelScale=4};engine.Play("video_watch",false);
                for(int j=0;j<50;j++)engine.Advance(.04,input);
                check(engine.Animation.Current!="video_watch"&&config.contexts["coding"].Concat(config.contexts["idle"]).Any(x=>x.clip==engine.Animation.Current),"New context reselects a preferred or everyday action promptly");
                engine=new BehaviorEngine(s,config,10){X=400,Y=400,PixelScale=4};engine.Emit("pet");
                for(int j=0;j<50;j++)engine.Advance(.04,input);
                check(engine.Animation.Current=="pet","Context changes preserve protected user interactions");
                var mix=Json.Read<BehaviorConfig>(Path.Combine(root,"config","behaviors.json"));mix.ambientChance=0;
                mix.contexts["idle"]=new List<Choice>{new Choice{clip="sit",weight=1}};mix.contexts["coding"]=new List<Choice>{new Choice{clip="code_watch",weight=1}};
                engine=new BehaviorEngine(s,mix,71);int preferredCount=0;for(int i=0;i<4000;i++)if(engine.SelectAutonomousAction("coding",false)=="code_watch")preferredCount++;
                check(Math.Abs(preferredCount/4000.0-mix.contextChance)<.035&&preferredCount>0&&preferredCount<4000,"Context actions follow configured probability and retain everyday alternatives");
                mix.contextChance=0;check(Enumerable.Range(0,100).All(i=>engine.SelectAutonomousAction("coding",false)=="sit"),"Zero context preference uses everyday actions");
                mix.contextChance=1;check(Enumerable.Range(0,100).All(i=>engine.SelectAutonomousAction("coding",false)=="code_watch"),"Full context preference uses its weighted pool");
                check(Enumerable.Range(0,100).All(i=>engine.SelectAutonomousAction("unrecognized",false)=="sit"),"Unknown context uses everyday actions");
                mix.contexts["coding"][0].clip="run";check(engine.SelectAutonomousAction("coding",false)=="sit","Movement filtering falls back when the preferred pool is unavailable");
                foreach(string context in ContextNames.All.Keys.Where(k=>k!="idle")){
                    engine=new BehaviorEngine(s,config,81);var samples=new HashSet<string>();for(int i=0;i<800;i++)samples.Add(engine.SelectAutonomousAction(context,false));
                    check(samples.Any(id=>config.contexts[context].Any(x=>x.clip==id))&&samples.Any(id=>config.contexts["idle"].Any(x=>x.clip==id)&&!config.contexts[context].Any(x=>x.clip==id))&&samples.All(id=>s.clips[id].motion==null),"Every context mixes everyday actions while movement is disabled: "+context);
                }
                string first=s.poses["sit"].First(l=>l.id=="head").sprite;using(var img=a.RenderAt("sit",.6,false)){}check(s.poses["sit"].First(l=>l.id=="head").sprite==first,"Rendering does not mutate base poses");
                bool rejected=false;try{var bad=Json.Read<Skin>(Path.Combine(root,"assets","skins","mikan","skin.json"));bad.clips["sit"].pose="missing";bad.Validate();}catch(InvalidDataException){rejected=true;}check(rejected,"Broken skin references rejected");
                rejected=false;try{var bad=Json.Read<Skin>(Path.Combine(root,"assets","skins","mikan","skin.json"));bad.clips["../escape"]=bad.clips["sit"];bad.Validate();}catch(InvalidDataException){rejected=true;}check(rejected,"Animation export identifiers cannot escape their directory");
                rejected=false;try{Content.Load(root,"../other");}catch(InvalidDataException){rejected=true;}check(rejected,"Skin paths cannot escape skin directory");
                rejected=false;try{var bad=Json.Read<Skin>(Path.Combine(root,"assets","skins","mikan","skin.json"));bad.poses["sit"][0].spriteCycle=new[]{"missing","missing"};bad.Validate();}catch(InvalidDataException){rejected=true;}check(rejected,"Invalid component cycle references are rejected");
            }}catch(Exception ex){failures.Add(ex.ToString());}
            Json.Write(report,new{passed=failures.Count==0,checks=checks.Count,failures=failures,verified=checks});return failures.Count==0?0:1;
        }
        public static int Smoke(string root,string report){
            Storage.TestDirectory=Path.Combine(Path.GetDirectoryName(report),"smoke-data");
            Directory.CreateDirectory(Path.GetDirectoryName(report));
            int exit=1;var preferences=new Preferences();
            using(var pet=new PetWindow(root,preferences)){
                IntPtr before=Native.GetForegroundWindow();uint startGdi=0;var timer=new Timer{Interval=1800};
                pet.Shown+=delegate{startGdi=Native.GetGuiResources(Process.GetCurrentProcess().Handle,0);timer.Start();};
                timer.Tick+=delegate{
                    timer.Stop();try{
                        for(int i=0;i<240;i++){pet.Engine.Animation.Advance(.03);pet.DrawPet();}
                        uint endGdi=Native.GetGuiResources(Process.GetCurrentProcess().Handle,0);int style=Native.GetWindowLong(pet.Handle,-20);
                        bool layered=(style&0x80000)!=0,noActivate=(style&0x08000000)!=0,tool=(style&0x80)!=0;
                        bool foreground=before==Native.GetForegroundWindow();
                        using(var panel=new CompanionPanel(pet,root)){
                            panel.Show();Application.DoEvents();
                            using(var bmp=new Bitmap(panel.Width,panel.Height)){panel.DrawToBitmap(bmp,new Rectangle(0,0,bmp.Width,bmp.Height));bmp.Save(Path.Combine(Path.GetDirectoryName(report),"control-panel.png"),System.Drawing.Imaging.ImageFormat.Png);}
                            panel.Close();
                        }
                        bool passed=layered&&noActivate&&tool&&foreground&&endGdi<=startGdi+8;
                        Json.Write(report,new{passed=passed,layered=layered,noActivate=noActivate,toolWindow=tool,keptForeground=foreground,gdiBefore=startGdi,gdiAfter=endGdi,renderedExtraFrames=240});exit=passed?0:1;
                    }catch(Exception ex){Json.Write(report,new{passed=false,error=ex.ToString()});}finally{pet.Close();}
                };
                Application.Run(pet);timer.Dispose();
            }
            return exit;
        }
    }
}
