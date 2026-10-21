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
                // 自己找乐子：鼠标快速划过时偶尔会扑一下。把概率调到 1 验证机制本身，
                // 再断言出厂配置里这仍然是个"小概率、长冷却"的念头。
                var eager=Json.Read<BehaviorConfig>(Path.Combine(root,"config","behaviors.json"));
                eager.pounce.chance=1;eager.pounce.cooldownSeconds=5;
                var tempted=new BehaviorEngine(s,eager,17){X=400,Y=400,PixelScale=4};
                var sweep=new InputFrame{WorkArea=new Rectangle(0,0,1600,900),Cursor=new Point(900,400),Context="coding"};
                string previous="";int springs=0;
                for(int j=0;j<900;j++){
                    sweep.Cursor=new Point(900+(j%2)*180,400+(j%3)*30);
                    tempted.Advance(.05,sweep);
                    if(tempted.LastEvent=="chase"&&previous!="chase")springs++;
                    previous=tempted.LastEvent;
                }
                check(springs>=1,"A passing cursor can tempt the cat into a spontaneous pounce");
                check(config.pounce!=null&&config.pounce.chance>0&&config.pounce.chance<=.2&&config.pounce.cooldownSeconds>=30,"The shipped pounce stays a rare urge with a long cooldown");
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
                BehaviorRuleValidation.Run(s,root,check);
                engine=new BehaviorEngine(s,config,41){X=200,Y=200,PixelScale=4};
                var walkFrame=new InputFrame{WorkArea=new Rectangle(0,0,1600,900),Cursor=new Point(1500,800),Reactions=false,Movement=true};
                engine.Play("run",true);engine.Destination=new PointF(600,320);
                bool walked=false;
                for(int j=0;j<600&&!walked;j++){engine.Advance(.05,walkFrame);walked=Math.Abs(engine.X-600)<1&&Math.Abs(engine.Y-320)<1;}
                check(walked&&engine.Destination==null&&engine.Animation.Current==config.events["idle"],"An external destination walks the cat there, then clears itself on arrival");
                engine=new BehaviorEngine(s,config,44){X=200,Y=200,PixelScale=4};
                bool arrivalFired=false;int arrivalCount=0;
                engine.Play("run",true);engine.Destination=new PointF(600,320);
                engine.Arrived=delegate{arrivalFired=true;arrivalCount++;};
                for(int j=0;j<600&&!arrivalFired;j++)engine.Advance(.05,walkFrame);
                for(int j=0;j<40;j++)engine.Advance(.05,walkFrame);
                check(arrivalFired&&arrivalCount==1&&engine.Arrived==null,"Arriving at a garden spot fires the interaction hook exactly once");
                engine= new BehaviorEngine(s,config,42){X=200,Y=200,PixelScale=4};
                engine.Destination=new PointF(100,100);engine.Play("sit",true);
                check(engine.Destination==null,"Playing another action drops a stale walk destination");
                engine=new BehaviorEngine(s,config,43);
                engine.Play("pet",true);
                bool protectedOnPlay=engine.Protected;
                engine.Advance(s.clips["pet"].seconds+.5,walkFrame);
                check(protectedOnPlay&&!engine.Protected,"A protected interaction reports itself busy until its time is over");
                string first=s.poses["sit"].First(l=>l.id=="head").sprite;using(var img=a.RenderAt("sit",.6,false)){}check(s.poses["sit"].First(l=>l.id=="head").sprite==first,"Rendering does not mutate base poses");
                bool rejected=false;try{var bad=Json.Read<Skin>(Path.Combine(root,"assets","skins","mikan","skin.json"));bad.clips["sit"].pose="missing";bad.Validate();}catch(InvalidDataException){rejected=true;}check(rejected,"Broken skin references rejected");
                rejected=false;try{var bad=Json.Read<Skin>(Path.Combine(root,"assets","skins","mikan","skin.json"));bad.clips["../escape"]=bad.clips["sit"];bad.Validate();}catch(InvalidDataException){rejected=true;}check(rejected,"Animation export identifiers cannot escape their directory");
                rejected=false;try{Content.Load(root,"../other");}catch(InvalidDataException){rejected=true;}check(rejected,"Skin paths cannot escape skin directory");
                rejected=false;try{var bad=Json.Read<Skin>(Path.Combine(root,"assets","skins","mikan","skin.json"));bad.poses["sit"][0].spriteCycle=new[]{"missing","missing"};bad.Validate();}catch(InvalidDataException){rejected=true;}check(rejected,"Invalid component cycle references are rejected");
                var gardenConfig=GardenConfig.Load(Path.Combine(root,"config","garden.json"));
                check(gardenConfig.slots.Count>=gardenConfig.maxPlants&&gardenConfig.species.Count>=6,"Garden config offers enough slots and species");
                check(gardenConfig.decorations.Select(d2=>d2.type).Distinct().Count()>=10,"The garden ships a varied set of small decorations");
                check(gardenConfig.decorations.Count(d2=>d2.type=="bird")>=5&&gardenConfig.decorations.Where(d2=>d2.type=="bird").Select(d2=>d2.variantIndex).Distinct().Count()>=5,"The garden ships a flock of five birds in five different plumages");
                var birdTune=gardenConfig.birds;
                var perched=BirdSchedule.At(1234,12,26,false,0,3,birdTune,0);
                check(perched.visible&&perched.pose==0&&perched.x==12&&perched.y==26,"Every bird starts the day sitting on the rail");
                bool sawFlight=false,sawHop=false,sawGone=false;
                for(int seed=1;seed<=24;seed++){
                    for(double t=0;t<90;t+=.1){
                        var frame=BirdSchedule.At(seed,12,26,false,seed%4,seed%4==0?1:(seed%4==1?2:0),birdTune,t);
                        if(frame.pose==2)sawFlight=true;
                        if(frame.pose==1)sawHop=true;
                        if(!frame.visible)sawGone=true;
                        if(sawFlight&&sawHop&&sawGone)break;
                    }
                    if(sawFlight&&sawHop&&sawGone)break;
                }
                check(sawFlight&&sawGone,"Birds now fly off the rail and come back later");
                check(sawHop,"Birds also hop along the rail instead of always flying");
                var once=BirdSchedule.At(7,12,26,false,1,1,birdTune,31.7);var twice=BirdSchedule.At(7,12,26,false,1,1,birdTune,31.7);
                check(once.visible==twice.visible&&once.x==twice.x&&once.y==twice.y&&once.pose==twice.pose,"Bird behaviour is a pure function of time, not stored state");
                bool ownSchedule=false;
                for(int t=0;t<200&&!ownSchedule;t++){
                    var firstBird=BirdSchedule.At(3,12,26,false,0,1,birdTune,t*.7);
                    var secondBird=BirdSchedule.At(9,12,26,false,0,2,birdTune,t*.7);
                    ownSchedule=firstBird.pose!=secondBird.pose||firstBird.x!=secondBird.x||firstBird.visible!=secondBird.visible;
                }
                check(ownSchedule,"Each bird keeps its own schedule instead of moving in lockstep");
                // 一群鸟全都朝同一边会很像复制粘贴，所以栏杆上必须是真的有左有右。
                var birdLine=gardenConfig.decorations.Where(d2=>d2.type=="bird").ToList();
                check(birdLine.Count(b2=>b2.flip)>=2&&birdLine.Count(b2=>!b2.flip)>=2,"The flock faces both ways instead of all looking the same direction");
                bool flyFollowsFacing=true;
                foreach(var bird in birdLine){
                    for(double t=0;t<160;t+=.2){
                        var shot=BirdSchedule.At(bird.seed,bird.x,bird.y,bird.flip,bird.variantIndex,bird.habitCode,birdTune,t);
                        if(!shot.visible||shot.pose!=2)continue;
                        int sideways=shot.x-bird.x;
                        if(sideways==0)continue;
                        if((sideways>0)==bird.flip)flyFollowsFacing=false;   // 面朝左就该往左飞
                        break;
                    }
                }
                check(flyFollowsFacing,"A bird flies out the way it is facing instead of backwards");
                check(gardenConfig.species.Count(s2=>s2.Flowering)>=4&&gardenConfig.species.Any(s2=>!s2.Flowering),"Garden mixes flowering and foliage species");
                check(GardenEngine.StageName(.05,true)=="刚撒下"&&GardenEngine.StageName(.5,true)=="长叶"&&GardenEngine.StageName(.6,true)=="现蕾"&&GardenEngine.StageName(1,true)=="盛开"&&GardenEngine.StageName(1,false)=="茂盛","Garden growth stages read from the same thresholds");
                var garden=new GardenEngine(gardenConfig,null,21);
                check(garden.Plants.Count==gardenConfig.initialPlants&&garden.Plants.All(p2=>p2.growth<.2),"A new garden starts with one or two seedlings");
                double seeding=garden.Plants[0].growth;
                for(int i=0;i<300;i++)garden.Advance(.2);
                check(garden.Plants[0].growth>seeding+.02&&garden.Plants[0].growth<1,"Watered seedlings grow slowly without overshooting full size");
                check(garden.Moisture<1&&garden.Plants.All(p2=>p2.wilt<.01),"Growing garden spends water without wilting");
                for(int i=0;i<6000&&!garden.Critical;i++)garden.Advance(.5);
                check(garden.Critical&&garden.NeedsWater,"Soil eventually runs dry and asks for water");
                double dryGrowth=garden.Plants[0].growth;
                for(int i=0;i<600;i++)garden.Advance(.5);
                check(garden.Plants[0].growth-dryGrowth<.08&&garden.Plants[0].wilt>.1,"Dry soil nearly pauses growth and wilts the plants");
                garden.Water();
                check(garden.Moisture==1&&garden.Watering>0&&garden.Plants[0].wilt<.2,"Watering refills the soil and revives the plants");
                check(garden.Petals.Count>0,"Watering drops fall inside the bed");
                var sowEngine=new GardenEngine(gardenConfig,null,55);
                using(var sowRenderer=new GardenRenderer(gardenConfig)){
                    sowRenderer.Render(sowEngine);
                    int cw=gardenConfig.canvas.width,ch=gardenConfig.canvas.height;
                    var calm=new int[cw*ch];
                    for(int sy2=0;sy2<ch;sy2++)for(int sx2=0;sx2<cw;sx2++)calm[sy2*cw+sx2]=sowRenderer.PixelAt(sx2,sy2);
                    var sown=sowEngine.Plant(gardenConfig.species.First(sp=>sp.Flowering).id);
                    check(sown!=null&&sowEngine.Sparks.Count>=10&&sowEngine.SowGlow>.9&&sowEngine.SowSlot==sown.slot,"Sowing seeds kicks up soil and lights the spot");
                    sowRenderer.Render(sowEngine);
                    int changed=0;
                    for(int sy2=0;sy2<ch;sy2++)for(int sx2=0;sx2<cw;sx2++)if(sowRenderer.PixelAt(sx2,sy2)!=calm[sy2*cw+sx2])changed++;
                    check(changed>=8,"A freshly sown spot paints visible sparks");
                    int flashX=gardenConfig.slots[sown.slot].x,flashY=gardenConfig.slots[sown.slot].y;
                    int brightest=0,bx=0,by=0;
                    for(int dy=-12;dy<=-3;dy++)for(int dx=-4;dx<=4;dx++){
                        int p=sowRenderer.PixelAt(flashX+dx,flashY+dy);
                        int lum=((p>>16)&255)+((p>>8)&255)+(p&255);
                        if(lum>brightest){brightest=lum;bx=flashX+dx;by=flashY+dy;}
                    }
                    check(brightest>600,"Sowing flashes a bright marker above the seed (best "+brightest+" at "+bx+","+by+" center "+flashX+","+(flashY-9)+"="+sowRenderer.PixelAt(flashX,flashY-9)+")");
                }
                for(int i=0;i<60;i++)sowEngine.Advance(.05);
                check(sowEngine.Sparks.Count==0&&sowEngine.SowGlow==0,"Sowing sparks settle back into the soil on their own");
                var offlineSow=new GardenEngine(gardenConfig,null,56);
                offlineSow.Plant(gardenConfig.species[0].id);
                offlineSow.CatchUp(900);
                check(offlineSow.Sparks.Count==0&&offlineSow.SowGlow==0,"Offline catch-up discards leftover sowing sparks");
                var spreadEngine=Spread(gardenConfig,21);
                for(int i=0;i<7200&&spreadEngine.Plants.Count<3;i++)spreadEngine.Advance(.5);
                check(spreadEngine.Plants.Count>2,"Blooming plants slowly add new plants");
                for(int i=0;i<60000&&!spreadEngine.Full;i++){spreadEngine.Advance(.5);if(spreadEngine.Critical)spreadEngine.Water();}
                check(spreadEngine.Full&&spreadEngine.Plants.Count==gardenConfig.maxPlants,"The garden stops spreading at its capacity");
                check(spreadEngine.Plant("catnip")==null,"A full garden rejects new seeds");
                var windEngine=Spread(gardenConfig,5);
                bool petals=false;
                for(int i=0;i<9000&&!petals;i++){windEngine.Advance(.5);petals=windEngine.Petals.Count>0;}
                check(petals,"Blooming flowers release petals when wind passes");
                check(windEngine.Petals.All(p2=>p2.life>0),"Petals keep a finite lifetime");
                check(gardenConfig.wind.petalsPerSecond<=4&&gardenConfig.wind.petalCap<=48,"Wind sheds a thinner veil of petals than before");
                bool large=false;
                for(int i=0;i<1400&&!large;i++){windEngine.Advance(.1);large=windEngine.Petals.Any(p2=>p2.size==2);}
                check(large,"Some petals are drawn larger so the fall has depth");
                check(windEngine.Petals.Where(p2=>p2.kind==0).All(p2=>p2.spin>0),"Flower petals tumble instead of falling as flat dots");
                int peak=0;var gust=Spread(gardenConfig,5);
                for(int i=0;i<12000;i++){gust.Advance(.25);peak=Math.Max(peak,gust.Petals.Count);}
                check(peak>0&&peak<=gardenConfig.wind.petalCap,"Petal count stays under the configured ceiling (peak "+peak+")");
                var foliageEngine=Foliage(gardenConfig,5);
                for(int i=0;i<12000;i++)foliageEngine.Advance(.5);
                check(foliageEngine.Petals.Count==0,"Foliage-only species never shed petals");
                check(Math.Abs(GardenEngine.OfflineSeconds(new GardenState{savedAt=DateTime.UtcNow.AddHours(-100).ToString("o")})-6*3600)<2,"Offline catch-up is capped at six hours");
                check(GardenEngine.OfflineSeconds(new GardenState{savedAt=DateTime.UtcNow.AddHours(-1).ToString("o")})>3500,"Offline catch-up replays real elapsed time");
                var resumedState=new GardenState{moisture=1};
                resumedState.plants.Add(new GardenPlant(gardenConfig.species[0].id,0,.4,resumedState.nextId++));
                var resumed=new GardenEngine(gardenConfig,resumedState,21);double resumedGrowth=resumed.Plants[0].growth;resumed.CatchUp(1800);
                check(resumed.Plants[0].growth>resumedGrowth,"Returning after a break advances the garden");
                using(var renderer=new GardenRenderer(gardenConfig)){
                    renderer.Render(garden);
                    check(renderer.Opaque(gardenConfig.bed.left+30,gardenConfig.bed.bottom-6),"The planted bed renders visible pixels");
                    check(!renderer.Opaque(1,1)&&!renderer.Opaque(gardenConfig.canvas.width-2,1),"Garden corners stay transparent");
                    int sky=0;for(int sx=2;sx<gardenConfig.canvas.width-2;sx++)for(int sy=1;sy<gardenConfig.fence.top-2;sy++)if(renderer.Opaque(sx,sy))sky++;
                    check(!renderer.Opaque(1,1)&&sky<60,"The sky above the fence stays open apart from a few birds");
                    var resting=new GardenEngine(gardenConfig,null,21);
                    renderer.Render(resting);
                    check(renderer.Opaque(12,24)&&renderer.Opaque(14,21),"A perched bird reads above the fence rail");
                    // 五只鸟要穿五件不同的衣服：直接读它们各自身体那一格的颜色，
                    // 光看配置里写了五个 variant 不够——配色回落成默认值时画面照样全是克隆。
                    var plumage=new System.Collections.Generic.HashSet<int>();
                    foreach(var bird in gardenConfig.decorations.Where(d2=>d2.type=="bird"))plumage.Add(renderer.PixelAt(bird.x,bird.y-1));
                    check(plumage.Count>=5,"Each bird wears its own plumage instead of five recolours of one bird ("+plumage.Count+" colours)");
                    // 让一只鸟飞出栏杆那一帧单独渲染一次：飞行姿态必须真的画在画布上，
                    // 不能只是"从杆上消失"。取样点取 0.25 秒的整数倍，和引擎步长对齐。
                    double flightAt=-1;var flightFrame=new BirdFrame();
                    foreach(var bird in gardenConfig.decorations.Where(d2=>d2.type=="bird")){
                        for(double t=0;t<180&&flightAt<0;t+=.25){
                            var birdNow=BirdSchedule.At(bird.seed,bird.x,bird.y,bird.flip,bird.variantIndex,bird.habitCode,birdTune,t);
                            if(!birdNow.visible||birdNow.pose!=2)continue;
                            if(birdNow.x<6||birdNow.x>gardenConfig.canvas.width-6||birdNow.y<4||birdNow.y>gardenConfig.fence.top)continue;
                            flightAt=t;flightFrame=birdNow;
                        }
                        if(flightAt>0)break;
                    }
                    check(flightAt>0,"Birds actually leave the rail and cross the sky on the way out (first seen at "+flightAt+"s)");
                    if(flightAt>0){
                        var flying=new GardenEngine(gardenConfig,null,21);
                        for(int i=0;i<(int)Math.Round(flightAt/.25);i++)flying.Advance(.25);
                        renderer.Render(flying);
                        int around=0;
                        for(int dy=-3;dy<=3;dy++)for(int dx=-4;dx<=4;dx++)if(renderer.Opaque(flightFrame.x+dx,flightFrame.y+dy))around++;
                        check(around>=3,"A flying bird is drawn in the air on its way out ("+around+" pixels)");
                    }
                    renderer.Render(garden);
                    var frame=renderer.Frame(2);
                    check(frame.Width==gardenConfig.canvas.width*2&&frame.Height==gardenConfig.canvas.height*2,"Garden frames scale by whole pixels");
                    var half=renderer.Frame(1.5);
                    check(half.Width==(int)Math.Round(gardenConfig.canvas.width*1.5)&&half.Height==(int)Math.Round(gardenConfig.canvas.height*1.5),"Garden frames also scale by fractional steps");
                    check(Preferences.GardenScales.Length==8&&Math.Abs(Preferences.GardenScales[0]-2.5)<.001&&Math.Abs(Preferences.GardenScales[7]-6)<.001,"Garden zoom offers 2.5x through 6x");
                    check(Math.Abs(Preferences.SnapGardenScale(1)-2.5)<.001&&Math.Abs(Preferences.SnapGardenScale(9)-6)<.001&&Math.Abs(Preferences.SnapGardenScale(3.2)-3)<.001&&Math.Abs(Preferences.SnapGardenScale(0)-2.5)<.001,"Garden zoom snaps onto the offered steps");
                    check(Math.Abs(new Preferences().GardenScale-3)<.001,"The garden opens at 3x by default");
                    check(renderer.Opaque(gardenConfig.bed.left+30,gardenConfig.bed.bottom-6),"Soil keeps rendering visible pixels after the texture pass");
                    var dry=new GardenEngine(gardenConfig,new GardenState{moisture=0},21);
                    renderer.Render(dry);
                    check(renderer.Opaque(8,9),"A dry garden shows the water reminder droplet");
                    renderer.Render(garden);
                    check(!renderer.Opaque(8,9),"The water reminder disappears once watered");
                    using(var stream=new MemoryStream()){frame.Save(stream,System.Drawing.Imaging.ImageFormat.Png);check(stream.Length>2000,"Garden frame encodes as a real image");}
                }
                rejected=false;try{var bad=Json.Read<GardenConfig>(Path.Combine(root,"config","garden.json"));bad.slots[0].x=1;bad.Prepare();}catch(InvalidDataException){rejected=true;}check(rejected,"Garden slots outside the bed are rejected");
                rejected=false;try{var bad=Json.Read<GardenConfig>(Path.Combine(root,"config","garden.json"));bad.species[0].growSeconds=0;bad.Prepare();}catch(InvalidDataException){rejected=true;}check(rejected,"Impossible garden growth timing is rejected");
            }}catch(Exception ex){failures.Add(ex.ToString());}
            Json.Write(report,new{passed=failures.Count==0,checks=checks.Count,failures=failures,verified=checks});return failures.Count==0?0:1;
        }
        static GardenEngine Spread(GardenConfig config,int seed){
            var state=new GardenState();string flowering=config.species.First(s=>s.Flowering).id;
            state.plants.Add(new GardenPlant(flowering,0,1,state.nextId++));
            state.plants.Add(new GardenPlant(flowering,4,1,state.nextId++));
            return new GardenEngine(config,state,seed);
        }
        static GardenEngine Foliage(GardenConfig config,int seed){
            var state=new GardenState();string leafy=config.species.First(s=>!s.Flowering).id;
            state.plants.Add(new GardenPlant(leafy,0,1,state.nextId++));
            state.plants.Add(new GardenPlant(leafy,4,1,state.nextId++));
            return new GardenEngine(config,state,seed);
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
                        bool gardenLayered=pet.Garden!=null&&(Native.GetWindowLong(pet.Garden.Handle,-20)&0x80000)!=0;
                        bool minZoom=false,interactionOk=true;
                        if(pet.Garden!=null){
                            double originalScale=pet.Settings.GardenScale;
                            pet.Settings.GardenScale=2.5;pet.Garden.ApplyScale();Application.DoEvents();
                            minZoom=pet.Garden.Width==(int)Math.Round(pet.Garden.Renderer.Width*2.5)&&pet.Garden.Height==(int)Math.Round(pet.Garden.Renderer.Height*2.5);
                            pet.Settings.GardenScale=6;pet.Garden.ApplyScale();Application.DoEvents();
                            bool maxZoom=pet.Garden.Width==(int)Math.Round(pet.Garden.Renderer.Width*6.0)&&pet.Garden.Height==(int)Math.Round(pet.Garden.Renderer.Height*6.0);
                            minZoom=minZoom&&maxZoom;
                            pet.Settings.GardenScale=originalScale;pet.Garden.ApplyScale();Application.DoEvents();
                            string clip=pet.Garden.PickInteraction();
                            interactionOk=clip==null||pet.Content.Skin.clips.ContainsKey(clip);
                            pet.Garden.Renderer.Render(pet.Garden.Engine);
                            pet.Garden.Renderer.Frame(4).Save(Path.Combine(Path.GetDirectoryName(report),"garden-live.png"),System.Drawing.Imaging.ImageFormat.Png);
                        }
                        using(var panel=new CompanionPanel(pet,root)){
                            panel.Show();Application.DoEvents();
                            using(var bmp=new Bitmap(panel.Width,panel.Height)){panel.DrawToBitmap(bmp,new Rectangle(0,0,bmp.Width,bmp.Height));bmp.Save(Path.Combine(Path.GetDirectoryName(report),"control-panel.png"),System.Drawing.Imaging.ImageFormat.Png);}
                            panel.Select(CompanionPanel.Section.Garden);Application.DoEvents();
                            using(var bmp=new Bitmap(panel.Width,panel.Height)){panel.DrawToBitmap(bmp,new Rectangle(0,0,bmp.Width,bmp.Height));bmp.Save(Path.Combine(Path.GetDirectoryName(report),"control-panel-garden.png"),System.Drawing.Imaging.ImageFormat.Png);}
                            panel.Close();
                        }
                        bool passed=layered&&noActivate&&tool&&foreground&&gardenLayered&&minZoom&&interactionOk&&endGdi<=startGdi+8;
                        Json.Write(report,new{passed=passed,layered=layered,noActivate=noActivate,toolWindow=tool,keptForeground=foreground,gardenLayered=gardenLayered,gardenZoomRange=minZoom,gardenInteraction=interactionOk,gdiBefore=startGdi,gdiAfter=endGdi,renderedExtraFrames=240});exit=passed?0:1;
                    }catch(Exception ex){Json.Write(report,new{passed=false,error=ex.ToString()});}finally{pet.Close();}
                };
                Application.Run(pet);timer.Dispose();
            }
            return exit;
        }
    }
}
