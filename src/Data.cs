using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Drawing;
using System.Web.Script.Serialization;

namespace Catcon {
    public static class Json {
        static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024, RecursionLimit = 80 };
        public static T Read<T>(string path) { return Serializer.Deserialize<T>(File.ReadAllText(path, Encoding.UTF8)); }
        public static void Write(string path, object value) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            File.WriteAllText(temp, Serializer.Serialize(value), new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }
    }
    public sealed class SpriteData { public int width {get;set;} public int height {get;set;} public string[] rows {get;set;} }
    public sealed class Layer {
        public string id {get;set;} public string sprite {get;set;}
        public string[] spriteCycle {get;set;} public int cycleMs {get;set;}
        public double x {get;set;} public double y {get;set;} public int z {get;set;}
        public double sx {get;set;} public double sy {get;set;} public double rotation {get;set;} public bool visible {get;set;}
        public Layer() { sx=1;sy=1;visible=true;cycleMs=240; }
        public Layer Copy() { return (Layer)MemberwiseClone(); }
        public void Apply(LayerPatch p) {
            if(p.sprite!=null){sprite=p.sprite;spriteCycle=null;}if(p.spriteCycle!=null)spriteCycle=p.spriteCycle;if(p.cycleMs.HasValue)cycleMs=p.cycleMs.Value;
            if(p.x.HasValue)x=p.x.Value;if(p.y.HasValue)y=p.y.Value;
            if(p.sx.HasValue)sx=p.sx.Value;if(p.sy.HasValue)sy=p.sy.Value;if(p.rotation.HasValue)rotation=p.rotation.Value;
            if(p.visible.HasValue)visible=p.visible.Value;if(p.z.HasValue)z=p.z.Value;
        }
    }
    public sealed class LayerPatch {
        public string sprite {get;set;} public double? x {get;set;} public double? y {get;set;}
        public string[] spriteCycle {get;set;} public int? cycleMs {get;set;}
        public double? sx {get;set;} public double? sy {get;set;} public double? rotation {get;set;}
        public bool? visible {get;set;} public int? z {get;set;}
    }
    public sealed class Deformer {
        public string type {get;set;} public string[] parts {get;set;}
        public double amplitude {get;set;} public double period {get;set;}
    }
    public sealed class LeapMotion {
        public double distance {get;set;} public double height {get;set;}
        public int takeoffFrame {get;set;} public int landingFrame {get;set;}
    }
    public sealed class Clip {
        public double? previewSeconds {get;set;}
        public string label {get;set;} public string pose {get;set;} public double seconds {get;set;}
        public int frameMs {get;set;} public List<Dictionary<string,LayerPatch>> frames {get;set;}
        public List<int> frameDurations {get;set;} public string playback {get;set;}
        public double[] contactAnchor {get;set;}
        public List<Deformer> deformers {get;set;} public string motion {get;set;} public double speed {get;set;}
        public string onArrival {get;set;}
        public LeapMotion leap {get;set;}
    }
    public sealed class Skin : IDisposable {
        public int schemaVersion {get;set;} public string id {get;set;} public string name {get;set;} public string author {get;set;}
        public int canvas {get;set;} public Dictionary<string,string> palette {get;set;}
        public Dictionary<string,SpriteData> sprites {get;set;} public Dictionary<string,List<Layer>> poses {get;set;}
        public Dictionary<string,Clip> clips {get;set;}
        [ScriptIgnore] public Dictionary<string,Bitmap> Images = new Dictionary<string,Bitmap>();
        public void Prepare() {
            Validate();
            var colors=palette.ToDictionary(p=>p.Key[0],p=>p.Value.Length==9?Color.FromArgb((int)Convert.ToInt64(p.Value.Substring(1),16)):ColorTranslator.FromHtml(p.Value));
            foreach(var s in sprites) {
                Require(IsIdentifier(s.Key),"Invalid sprite identifier: "+s.Key);
                Bitmap b=new Bitmap(s.Value.width,s.Value.height,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)b.SetPixel(x,y,colors[s.Value.rows[y][x]]);
                Images.Add(s.Key,b);
            }
        }
        public void Validate() {
            Require(schemaVersion==1,"Unsupported skin schema");Require(canvas>=32&&canvas<=128,"Canvas must be 32..128");
            Require(!String.IsNullOrWhiteSpace(id)&&!String.IsNullOrWhiteSpace(name),"Skin requires id/name");
            Require(palette!=null&&sprites!=null&&poses!=null&&clips!=null,"Missing skin collections");
            foreach(var p in palette) { Require(p.Key.Length==1,"Palette keys must be one character"); ColorTranslator.FromHtml(p.Value.Length==9?"#"+p.Value.Substring(3):p.Value); }
            foreach(var s in sprites) {
                Require(s.Value.width>0&&s.Value.width<=128&&s.Value.height>0&&s.Value.height<=128,"Invalid sprite size: "+s.Key);
                Require(s.Value.rows!=null&&s.Value.rows.Length==s.Value.height,"Sprite height: "+s.Key);
                foreach(string row in s.Value.rows)Require(row.Length==s.Value.width&&row.All(c=>palette.ContainsKey(c.ToString())),"Invalid sprite pixels: "+s.Key);
            }
            foreach(var pose in poses) {
                Require(IsIdentifier(pose.Key),"Invalid pose identifier: "+pose.Key);
                Require(pose.Value.Select(l=>l.id).Distinct().Count()==pose.Value.Count,"Duplicate layer: "+pose.Key);
                foreach(var l in pose.Value) {Require(sprites.ContainsKey(l.sprite),"Missing sprite: "+l.sprite);Require(l.sx>0&&l.sy>0,"Layer scale must be positive");ValidateCycle(l.spriteCycle,l.cycleMs);}
            }
            foreach(var c in clips) {
                Require(IsIdentifier(c.Key),"Invalid clip identifier: "+c.Key);
                Require(!c.Value.previewSeconds.HasValue||(c.Value.previewSeconds>=0&&c.Value.previewSeconds<=c.Value.seconds),"Invalid preview time: "+c.Key);
                Require(poses.ContainsKey(c.Value.pose),"Missing pose: "+c.Key);Require(c.Value.seconds>0&&c.Value.frameMs>=30&&c.Value.frames!=null&&c.Value.frames.Count>0,"Invalid clip timing: "+c.Key);
                Require(c.Value.playback==null||c.Value.playback=="loop"||c.Value.playback=="once","Invalid playback mode: "+c.Key);
                if(c.Value.frameDurations!=null)Require(c.Value.frameDurations.Count==c.Value.frames.Count&&c.Value.frameDurations.All(ms=>ms>=30&&ms<=60000),"Invalid frame durations: "+c.Key);
                if(c.Value.playback=="once")Require(Animator.TimelineMilliseconds(c.Value)<=c.Value.seconds*1000+1,"One-shot frames exceed action duration: "+c.Key);
                if(c.Value.contactAnchor!=null)Require(c.Value.contactAnchor.Length==2&&c.Value.contactAnchor.All(v=>v>=0&&v<canvas),"Invalid contact anchor: "+c.Key);
                Require(c.Value.onArrival==null||clips.ContainsKey(c.Value.onArrival),"Missing arrival clip");
                Require(c.Value.motion==null||new[]{"wander","cursor","retreat","leap"}.Contains(c.Value.motion),"Invalid motion: "+c.Key);
                if(c.Value.motion!=null&&c.Value.motion!="leap")Require(c.Value.speed>0,"Motion needs speed");
                if(c.Value.motion=="leap")Require(c.Value.leap!=null&&c.Value.playback=="once"&&c.Value.leap.distance>0&&c.Value.leap.distance<=canvas*4&&c.Value.leap.height>=0&&c.Value.leap.height<=canvas*2&&c.Value.leap.takeoffFrame>=0&&c.Value.leap.landingFrame>c.Value.leap.takeoffFrame&&c.Value.leap.landingFrame<c.Value.frames.Count,"Invalid leap trajectory: "+c.Key);
                foreach(var f in c.Value.frames)foreach(var p in f) {
                    Require(poses[c.Value.pose].Any(l=>l.id==p.Key),"Unknown layer in "+c.Key+": "+p.Key);
                    Require(p.Value.sprite==null||sprites.ContainsKey(p.Value.sprite),"Unknown patched sprite");
                    Require((!p.Value.sx.HasValue||p.Value.sx>0)&&(!p.Value.sy.HasValue||p.Value.sy>0),"Invalid patch scale");
                    ValidateCycle(p.Value.spriteCycle,p.Value.cycleMs??240);
                }
                if(c.Value.deformers!=null)foreach(var d in c.Value.deformers) {
                    Require(new[]{"breathe","float","shake","hop","spin"}.Contains(d.type)&&d.period>0,"Invalid deformer");
                    Require(d.parts!=null&&d.parts.All(p=>p=="*"||poses[c.Value.pose].Any(l=>l.id==p)),"Unknown deformer layer");
                }
            }
        }
        void ValidateCycle(string[] cycle,int ms){
            Require(ms>=30&&ms<=60000,"Invalid component cycle timing");
            if(cycle!=null)Require(cycle.Length>=2&&cycle.Length<=64&&cycle.All(id=>sprites.ContainsKey(id)),"Invalid component cycle sprites");
        }
        public static void Require(bool condition,string reason) {if(!condition)throw new InvalidDataException(reason);}
        static bool IsIdentifier(string value){return !String.IsNullOrEmpty(value)&&value.Length<=64&&value.All(c=>Char.IsLetterOrDigit(c)||c=='_'||c=='-');}
        public void Dispose() { foreach(var b in Images.Values)b.Dispose();Images.Clear(); }
    }
    public sealed class Choice {public string clip {get;set;} public int weight {get;set;}}
    public sealed class FollowUpRule {
        public double chance {get;set;}public List<Choice> choices {get;set;}
    }
    public sealed class IndependentAction {
        public IndependentAction(){quietGames=true;}
        public string clip {get;set;}public double chance {get;set;}
        public double[] intervalSeconds {get;set;}public string[] contexts {get;set;}
        public bool quietGames {get;set;}
    }
    /// <summary>
    /// 自己忍不住扑一下鼠标：小概率、长冷却。和受惊不同，这是主动的玩心，
    /// 所以要求鼠标动得够快、离得不远也不近，扑完还得歇一会儿。
    /// </summary>
    public sealed class PounceRule {
        public double chance {get;set;} public double cooldownSeconds {get;set;}
        public double cursorSpeed {get;set;} public double maxDistance {get;set;}
        public PounceRule(){chance=.12;cooldownSeconds=45;cursorSpeed=260;maxDistance=560;}
    }
    public sealed class BehaviorConfig {
        public BehaviorConfig(){contextChance=.7;followUps=new Dictionary<string,FollowUpRule>();independentActions=new List<IndependentAction>();pounce=new PounceRule();}
        public int schemaVersion {get;set;} public double[] decisionSeconds {get;set;}
        public double contextChangeSeconds {get;set;}
        public double contextChance {get;set;}
        public double ambientChance {get;set;}public List<Choice> ambientChoices {get;set;}
        public double cursorNearPixels {get;set;} public double cursorDwellSeconds {get;set;}
        public double startleSpeed {get;set;} public double startleCooldown {get;set;}
        public PounceRule pounce {get;set;}
        public int repeatedStartleCount {get;set;} public double repeatedStartleWindow {get;set;}
        public Dictionary<string,string> events {get;set;} public Dictionary<string,List<Choice>> contexts {get;set;}
        public Dictionary<string,FollowUpRule> followUps {get;set;}
        public List<IndependentAction> independentActions {get;set;}
        public void Validate(Skin s) {
            Skin.Require(schemaVersion==1&&decisionSeconds!=null&&decisionSeconds.Length==2&&decisionSeconds[0]>0&&decisionSeconds[1]>=decisionSeconds[0],"Invalid behavior timing");
            Skin.Require(contextChangeSeconds>=0&&contextChangeSeconds<=10,"Invalid context change delay");
            Skin.Require(contextChance>=0&&contextChance<=1,"Invalid context choice probability");
            Skin.Require(cursorNearPixels>0&&cursorDwellSeconds>0&&startleCooldown>0&&startleSpeed>0&&repeatedStartleCount>1&&repeatedStartleWindow>0,"Invalid reaction thresholds");
            if(pounce!=null)Skin.Require(pounce.chance>=0&&pounce.chance<=1&&pounce.cooldownSeconds>=5&&pounce.cursorSpeed>0&&pounce.maxDistance>120,"Invalid spontaneous pounce rule");
            foreach(string ev in new[]{"startle","repeatedStartle","dwell","drag","release","pet","chase","idle"})Skin.Require(events.ContainsKey(ev)&&s.clips.ContainsKey(events[ev]),"Missing behavior event: "+ev);
            foreach(string category in ContextNames.All.Keys)Skin.Require(contexts.ContainsKey(category),"Missing context: "+category);
            foreach(var c in contexts.Values)Skin.Require(c.Count>0&&c.All(x=>x.weight>0&&s.clips.ContainsKey(x.clip)),"Invalid behavior choice");
            Skin.Require(ambientChance>=0&&ambientChance<=1,"Invalid ambient probability");
            if(ambientChance>0)Skin.Require(ambientChoices!=null&&ambientChoices.Count>0&&ambientChoices.All(x=>x.weight>0&&s.clips.ContainsKey(x.clip)),"Invalid ambient choices");
            Skin.Require(followUps!=null&&independentActions!=null,"Behavior rules must be collections");
            foreach(var entry in followUps){
                var rule=entry.Value;
                Skin.Require(s.clips.ContainsKey(entry.Key)&&rule!=null&&Probability(rule.chance),"Invalid follow-up probability or source");
                Skin.Require(rule.choices!=null&&rule.choices.Count>0&&rule.choices.All(x=>x!=null&&x.clip!=null&&x.weight>0&&s.clips.ContainsKey(x.clip))&&rule.choices.Sum(x=>(long)x.weight)<=Int32.MaxValue,"Invalid follow-up choices");
            }
            Skin.Require(independentActions.All(x=>x!=null&&x.clip!=null)&&independentActions.Select(x=>x.clip).Distinct().Count()==independentActions.Count,"Duplicate or missing independent action");
            foreach(var rule in independentActions){
                Skin.Require(s.clips.ContainsKey(rule.clip)&&Probability(rule.chance),"Invalid independent action or probability");
                Skin.Require(rule.intervalSeconds!=null&&rule.intervalSeconds.Length==2&&Finite(rule.intervalSeconds[0])&&Finite(rule.intervalSeconds[1])&&rule.intervalSeconds[0]>=30&&rule.intervalSeconds[1]>=rule.intervalSeconds[0]&&rule.intervalSeconds[1]<=604800,"Invalid independent action interval");
                Skin.Require(rule.contexts==null||(rule.contexts.Length>0&&rule.contexts.All(ContextNames.All.ContainsKey)),"Invalid independent action contexts");
            }
        }
        static bool Finite(double value){return !Double.IsNaN(value)&&!Double.IsInfinity(value);}
        static bool Probability(double value){return Finite(value)&&value>=0&&value<=1;}
    }
    public static class ContextNames {
        public static readonly Dictionary<string,string> All=new Dictionary<string,string>{{"idle","自由活动"},{"coding","写代码"},{"video","看视频"},{"office","办公"},{"browsing","浏览网页"},{"chat","聊天"},{"gaming","玩游戏"}};
    }
    public sealed class Preferences {
        public int Scale {get;set;} public bool Topmost {get;set;} public bool Wander {get;set;} public bool Classify {get;set;}
        public bool Reactions {get;set;} public bool QuietGames {get;set;} public string SkinId {get;set;}
        public bool SmartClassify {get;set;}
        public int? X {get;set;} public int? Y {get;set;}
        public bool GardenVisible {get;set;} public double GardenScale {get;set;} public bool GardenVisits {get;set;}
        public int? GardenX {get;set;} public int? GardenY {get;set;}
        public Preferences() {Scale=4;Topmost=true;Wander=true;Classify=true;SmartClassify=true;Reactions=true;QuietGames=true;SkinId="mikan";GardenVisible=true;GardenScale=3;GardenVisits=true;}
        /// <summary>花园可选的显示倍数。默认 3 倍，范围 2.5～6 倍，半档递增。</summary>
        public static readonly double[] GardenScales=new double[]{2.5,3,3.5,4,4.5,5,5.5,6};
        public static double SnapGardenScale(double scale) {
            double best=3,delta=double.MaxValue;
            foreach(double option in GardenScales) {double d=Math.Abs(option-scale);if(d<delta-.0001){delta=d;best=option;}}
            return best;
        }
    }
    public sealed class Content : IDisposable {
        public Skin Skin;public BehaviorConfig Behavior;public ClassifierConfig Classifiers;
        public static Content Load(string root,string skinId) {
            if(String.IsNullOrEmpty(skinId)||skinId.IndexOfAny(Path.GetInvalidFileNameChars())>=0||skinId.Contains("..")||skinId.Contains("/")||skinId.Contains("\\"))throw new InvalidDataException("Invalid skin directory");
            var c=new Content();
            try {
                c.Skin=Json.Read<Skin>(Path.Combine(root,"assets","skins",skinId,"skin.json"));c.Skin.Prepare();
                c.Behavior=Json.Read<BehaviorConfig>(Path.Combine(root,"config","behaviors.json"));c.Behavior.Validate(c.Skin);
                c.Classifiers=Json.Read<ClassifierConfig>(Path.Combine(root,"config","classifiers.json"));c.Classifiers.Prepare();return c;
            } catch {c.Dispose();throw;}
        }
        public void Dispose(){if(Skin!=null)Skin.Dispose();}
    }
    public static class Storage {
        public static string TestDirectory;
        public static string DirectoryPath {get{return TestDirectory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Catcon");}}
        public static string SettingsPath {get{return Path.Combine(DirectoryPath,"settings.json");}}
        public static string GardenPath {get{return Path.Combine(DirectoryPath,"garden.json");}}
        public static Preferences Load() {try {var p=Json.Read<Preferences>(SettingsPath);p.Scale=Math.Max(2,Math.Min(6,p.Scale));p.GardenScale=Preferences.SnapGardenScale(p.GardenScale);return p;}catch{return new Preferences();}}
        public static void Save(Preferences p) {try{Json.Write(SettingsPath,p);}catch(Exception ex){Log(ex);}}
        public static void Log(Exception ex) {try{Directory.CreateDirectory(DirectoryPath);File.AppendAllText(Path.Combine(DirectoryPath,"errors.log"),DateTime.Now.ToString("s")+" "+ex+Environment.NewLine);}catch{}}
    }
}
