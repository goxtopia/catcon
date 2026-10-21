using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Globalization;
using System.Collections.Generic;

namespace Catcon {
    public sealed class GardenSize { public int width {get;set;} public int height {get;set;} }
    public sealed class GardenMoisture {
        public double fullSeconds {get;set;} public double thirstyBelow {get;set;}
        public double dryBelow {get;set;} public double wiltSeconds {get;set;}
    }
    public sealed class GardenSpread { public double chancePerMinute {get;set;} public double minMoisture {get;set;} }
    public sealed class GardenWind {
        public double minSeconds {get;set;} public double maxSeconds {get;set;} public double chance {get;set;}
        public double[] durationSeconds {get;set;} public double petalsPerSecond {get;set;}
        /// <summary>同屏最多几片，防止长时间刮风堆满画面。</summary>
        public int petalCap {get;set;}
        public GardenWind(){petalCap=48;}
    }
    public sealed class GardenGrass { public int top {get;set;} public int left {get;set;} public int right {get;set;} }
    public sealed class GardenFence { public int top {get;set;} public int spacing {get;set;} }
    public sealed class GardenBed { public int left {get;set;} public int top {get;set;} public int right {get;set;} public int bottom {get;set;} public int inset {get;set;} }
    public sealed class GardenFlower {
        public string form {get;set;} public string color {get;set;} public string color2 {get;set;} public string core {get;set;}
        public int size {get;set;} public int count {get;set;}
    }
    public sealed class GardenSpecies {
        public string id {get;set;} public string name {get;set;} public string note {get;set;}
        public double growSeconds {get;set;} public int height {get;set;} public int stemWidth {get;set;}
        public int stems {get;set;} public int leafPairs {get;set;} public int leafWidth {get;set;} public double leafRise {get;set;}
        public string stemColor {get;set;} public string leafColor {get;set;} public string leafDark {get;set;}
        public GardenFlower flower {get;set;}
        public GardenSpecies(){stems=1;leafRise=.6;}
        public bool Flowering {get{return flower!=null&&flower.form!="none"&&flower.count>0;}}
    }
    public sealed class GardenDecoration {
        public string type {get;set;} public int x {get;set;} public int y {get;set;}
        public bool flip {get;set;} public double scale {get;set;}
        /// <summary>小鸟的毛色，见 BirdVariants；其他装饰忽略这个字段。</summary>
        public string variant {get;set;}
        /// <summary>小鸟的习性：fly / hop / stay / either，默认 either 抛硬币。</summary>
        public string habit {get;set;}
        [System.Web.Script.Serialization.ScriptIgnore] public int variantIndex;
        [System.Web.Script.Serialization.ScriptIgnore] public int habitCode;
        /// <summary>由坐标算出的稳定种子，让每只鸟的作息不一样又不会每帧乱跳。</summary>
        [System.Web.Script.Serialization.ScriptIgnore] public int seed;
        public GardenDecoration(){scale=1;}
    }
    /// <summary>蜜柑自己溜达到花园来的节奏。</summary>
    public sealed class GardenVisits {
        public double minSeconds {get;set;} public double maxSeconds {get;set;} public double chance {get;set;}
        public GardenVisits(){minSeconds=90;maxSeconds=260;chance=.6;}
    }
    public sealed class GardenSlot { public int x {get;set;} public int y {get;set;} public double scale {get;set;} }

    public sealed class GardenConfig {
        public int schemaVersion {get;set;} public GardenSize canvas {get;set;}
        public int maxPlants {get;set;} public int initialPlants {get;set;}
        public GardenMoisture moisture {get;set;} public GardenSpread spread {get;set;} public GardenWind wind {get;set;}
        public GardenGrass grass {get;set;} public GardenFence fence {get;set;} public GardenBed bed {get;set;}
        public GardenBirdConfig birds {get;set;} public GardenVisits visits {get;set;}
        public Dictionary<string,string> colors {get;set;}
        public List<GardenDecoration> decorations {get;set;}
        public List<GardenSlot> slots {get;set;}
        public List<GardenSpecies> species {get;set;}
        [System.Web.Script.Serialization.ScriptIgnore] public Dictionary<string,GardenSpecies> ById;
        static readonly string[] Effects=new[]{"cluster","cup","poppy","spike","daisy","none"};
        static readonly string[] Decorations=new[]{"sign","lantern","mushrooms","stones","can","butterfly",
            "fairyLights","gnome","duck","pinwheel","cloche","gazing","baubles","bird"};
        static readonly string[] BirdVariants=new[]{"sparrow","rust","blue","gold","coal"};
        static readonly string[] BirdHabits=new[]{"either","fly","hop","stay"};
        static readonly string[] RequiredColors=new[]{"outline","soil","soilWet","soilDry","soilDark","soilLight","grass","grassLight","grassDark","wood","woodLight","woodDark","stone","stoneDark","metal","metalLight","metalDark","glow","water","waterLight","mushroomCap","mushroomStem","butterfly","butterflyWing","glass","glassLight","ceramic","paintA","paintB","gold","bird","birdLight","birdWing","beak"};
        public static GardenConfig Load(string path){
            var config=Json.Read<GardenConfig>(path);
            config.Prepare();
            return config;
        }
        public void Prepare(){
            Validate();
            if(birds==null)birds=new GardenBirdConfig();
            if(visits==null)visits=new GardenVisits();
            ById=species.ToDictionary(s=>s.id,s=>s);
            if(decorations!=null)foreach(var d in decorations){
                d.seed=(d.x*7919+d.y*104729+d.type.Length*131)%1000003;
                if(d.type=="bird"){
                    d.variantIndex=Array.IndexOf(BirdVariants,d.variant??"sparrow");
                    d.habitCode=Math.Max(0,Array.IndexOf(BirdHabits,d.habit??"either"));
                }
            }
        }
        public void Validate(){
            Skin.Require(schemaVersion==1,"Unsupported garden schema");
            Skin.Require(canvas!=null&&canvas.width>=64&&canvas.width<=220&&canvas.height>=48&&canvas.height<=220,"Garden canvas must be 64..220 by 48..220");
            Skin.Require(moisture!=null&&spread!=null&&wind!=null&&grass!=null&&fence!=null&&bed!=null,"Missing garden sections");
            Skin.Require(moisture.fullSeconds>0&&moisture.wiltSeconds>0,"Garden moisture timing must be positive");
            Skin.Require(moisture.dryBelow>0&&moisture.thirstyBelow>moisture.dryBelow&&moisture.thirstyBelow<=1,"Garden moisture thresholds must be ordered inside 0..1");
            Skin.Require(spread.chancePerMinute>=0&&spread.chancePerMinute<=1&&spread.minMoisture>=0&&spread.minMoisture<=1,"Invalid garden spread probability");
            Skin.Require(wind.minSeconds>0&&wind.maxSeconds>=wind.minSeconds&&wind.chance>=0&&wind.chance<=1,"Invalid garden wind timing");
            Skin.Require(wind.durationSeconds!=null&&wind.durationSeconds.Length==2&&wind.durationSeconds[0]>0&&wind.durationSeconds[1]>=wind.durationSeconds[0],"Invalid garden wind duration");
            Skin.Require(wind.petalsPerSecond>=0&&wind.petalsPerSecond<=120,"Invalid petal rate");
            Skin.Require(wind.petalCap>=8&&wind.petalCap<=160,"Invalid petal cap");
            Skin.Require(grass.right>grass.left&&grass.top>0&&fence.top>=0&&fence.spacing>=6,"Invalid garden grass or fence geometry");
            Skin.Require(bed.right>bed.left&&bed.bottom>bed.top&&bed.inset>0&&bed.top>fence.top&&bed.bottom<canvas.height,"Garden bed must sit inside the canvas below the fence");
            Skin.Require(colors!=null&&RequiredColors.All(colors.ContainsKey),"Missing garden colors: "+String.Join(",",RequiredColors.Where(c=>colors==null||!colors.ContainsKey(c))));
            foreach(var pair in colors)Skin.Require(pair.Key.Length>=1&&IsHex(pair.Value),"Invalid garden color: "+pair.Key);
            Skin.Require(slots!=null&&slots.Count>=4&&slots.Count<=24,"Garden needs 4..24 planting slots");
            foreach(var slot in slots)
                Skin.Require(slot.x>bed.left&&slot.x<bed.right&&slot.y>bed.top&&slot.y<bed.bottom&&slot.scale>=.5&&slot.scale<=1.6,"Planting slot outside the bed: "+slot.x+"/"+slot.y);
            Skin.Require(slots.Select(s=>s.x+"x"+s.y).Distinct().Count()==slots.Count,"Duplicate planting slot");
            Skin.Require(maxPlants>0&&maxPlants<=slots.Count,"Garden capacity must fit inside its slots");
            Skin.Require(initialPlants>=0&&initialPlants<=maxPlants,"Initial plants must fit inside capacity");
            if(decorations!=null)foreach(var d in decorations){
                Skin.Require(d!=null&&Decorations.Contains(d.type),"Unknown garden decoration: "+(d==null?"null":d.type));
                Skin.Require(d.x>=0&&d.x<canvas.width&&d.y>0&&d.y<=canvas.height&&d.scale>=.5&&d.scale<=2,"Garden decoration outside the canvas: "+d.type);
                if(d.type=="bird"){
                    Skin.Require(String.IsNullOrEmpty(d.variant)||BirdVariants.Contains(d.variant),"Unknown bird variant: "+d.variant);
                    Skin.Require(String.IsNullOrEmpty(d.habit)||BirdHabits.Contains(d.habit),"Unknown bird habit: "+d.habit);
                }
            }
            if(birds!=null)birds.Validate();
            if(visits!=null)Skin.Require(visits.minSeconds>0&&visits.maxSeconds>=visits.minSeconds&&visits.chance>=0&&visits.chance<=1,"Invalid garden visit timing");
            Skin.Require(species!=null&&species.Count>0&&species.Count<=24,"Garden needs 1..24 species");
            Skin.Require(species.Select(s=>s.id).Distinct().Count()==species.Count,"Duplicate garden species id");
            foreach(var s in species){
                Skin.Require(!String.IsNullOrEmpty(s.id)&&s.id.Length<=32&&s.id.All(c=>Char.IsLetterOrDigit(c)||c=='_'),"Invalid species id: "+s.id);
                Skin.Require(!String.IsNullOrWhiteSpace(s.name)&&!String.IsNullOrWhiteSpace(s.note),"Species needs name and note: "+s.id);
                Skin.Require(s.growSeconds>=30&&s.height>=6&&s.height<=canvas.height-24,"Invalid species growth: "+s.id);
                Skin.Require(s.stemWidth>=1&&s.stemWidth<=2&&s.stems>=1&&s.stems<=4&&s.leafPairs>=1&&s.leafPairs<=6&&s.leafWidth>=1&&s.leafWidth<=4,"Invalid species foliage: "+s.id);
                Skin.Require(s.leafRise>=0&&s.leafRise<=2,"Invalid leaf angle: "+s.id);
                foreach(string color in new[]{s.stemColor,s.leafColor,s.leafDark})Skin.Require(IsHex(color),"Invalid species color: "+s.id);
                Skin.Require(s.flower!=null,"Missing flower definition: "+s.id);
                Skin.Require(Effects.Contains(s.flower.form),"Unknown flower form: "+s.id);
                if(s.flower.form!="none"){
                    foreach(string color in new[]{s.flower.color,s.flower.color2,s.flower.core})Skin.Require(IsHex(color),"Invalid flower color: "+s.id);
                    Skin.Require(s.flower.size>=1&&s.flower.size<=5&&s.flower.count>=1&&s.flower.count<=6,"Invalid flower size or count: "+s.id);
                }
            }
        }
        static bool IsHex(string value){
            if(String.IsNullOrEmpty(value)||(value.Length!=7&&value.Length!=9)||value[0]!='#')return false;
            for(int i=1;i<value.Length;i++)if(!Uri.IsHexDigit(value[i]))return false;
            return true;
        }
        public GardenSpecies SpeciesOf(string id){
            GardenSpecies found;return id!=null&&ById.TryGetValue(id,out found)?found:null;
        }
    }

    public sealed class GardenPlant {
        public string species {get;set;} public int slot {get;set;}
        public double growth {get;set;} public double wilt {get;set;} public long id {get;set;}
        public GardenPlant(){}
        public GardenPlant(string species,int slot,double growth,long id){this.species=species;this.slot=slot;this.growth=growth;this.id=id;}
    }
    public sealed class GardenPetal {
        public double x,y,vx,vy,life,maxLife,phase;
        public int color;public bool fading;
        /// <summary>翻面速度：花瓣在空中会翻过来，单像素的朝向跟着变。</summary>
        public double spin;
        /// <summary>飘摆节奏。每片不一样，凑在一起才不像一起下落的一排点。</summary>
        public double flutter;
        /// <summary>1 = 小花瓣，2 = 大一点的花瓣（远景/近景的层次）。</summary>
        public int size;
        /// <summary>0 = 花瓣，1 = 浇水的水珠。</summary>
        public int kind;
    }
    /// <summary>撒种时的一次性粒子：种子落下、土粒抛起、嫩芽上的光点。</summary>
    public sealed class GardenSpark {
        public double x,y,vx,vy,life,maxLife,floorY;public int color;
        /// <summary>0 = 土粒，1 = 光点，2 = 下落的种子。</summary>
        public int kind;
        public bool glow{get{return kind==1;}}
    }
    public sealed class GardenState {
        public int schemaVersion {get;set;} public string savedAt {get;set;}
        public double moisture {get;set;} public long nextId {get;set;}
        public List<GardenPlant> plants {get;set;}
        public GardenState(){
            schemaVersion=1;moisture=1;nextId=1;plants=new List<GardenPlant>();
            savedAt=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture);
        }
    }

    public sealed class GardenEngine {
        public const double BloomStart=.62,BloomFull=.95;
        public readonly GardenConfig Config;
        public GardenState State {get;private set;}
        public readonly List<GardenPetal> Petals=new List<GardenPetal>();
        public readonly List<GardenSpark> Sparks=new List<GardenSpark>();
        /// <summary>最近一次撒种的位置与余晖，用来在槽位处画一圈扩散的光环。</summary>
        public int SowSlot=-1;
        public double SowGlow;
        public double Time {get;private set;}
        public double Wind {get;private set;}
        public int WindDirection {get;private set;}
        public double Watering {get;private set;}
        public bool Dirty;
        readonly Random random;
        double windLeft,windTarget,nextWind,spreadAccum,windDirSign;
        public GardenEngine(GardenConfig config,GardenState state,int seed){
            Config=config;State=state??NewState(config,new Random(seed));random=new Random(seed);
            if(State.plants==null)State.plants=new List<GardenPlant>();
            nextWind=config.wind.minSeconds+random.NextDouble()*(config.wind.maxSeconds-config.wind.minSeconds);
            windDirSign=1;
        }
        public IList<GardenPlant> Plants {get{return State.plants;}}
        public double Moisture {get{return State.moisture;}}
        public bool NeedsWater {get{return State.moisture<Config.moisture.thirstyBelow;}}
        public bool Critical {get{return State.moisture<Config.moisture.dryBelow;}}
        public bool Full {get{return State.plants.Count>=Config.maxPlants;}}
        public int Blooming {get{return State.plants.Count(IsBlooming);}}
        public static bool IsBlooming(GardenPlant plant){return plant.growth>=BloomFull;}
        public bool Flowering(GardenPlant plant){
            GardenSpecies s=Config.SpeciesOf(plant.species);
            return s!=null&&s.Flowering;
        }
        public static string StageName(double growth,bool flowering){
            if(growth<.10)return "刚撒下";
            if(growth<.30)return "幼苗";
            if(growth<.55)return flowering?"长叶":"抽枝";
            if(growth<BloomStart)return flowering?"现蕾":"长叶";
            if(growth<BloomFull)return flowering?"花苞":"茂盛";
            return flowering?"盛开":"茂盛";
        }
        public string StageName(GardenPlant plant){
            GardenSpecies s=Config.SpeciesOf(plant.species);return StageName(plant.growth,s!=null&&s.Flowering);
        }
        public static GardenState NewState(GardenConfig config,Random random){
            var state=new GardenState{moisture=1};
            for(int i=0;i<config.initialPlants;i++){
                int slot=FreeSlot(config,state,random);
                if(slot<0)break;
                var species=config.species[random.Next(config.species.Count)];
                state.plants.Add(new GardenPlant(species.id,slot,.12+random.NextDouble()*.08,state.nextId++));
            }
            return state;
        }
        static int FreeSlot(GardenConfig config,GardenState state,Random random){
            var used=new HashSet<int>(state.plants.Select(p=>p.slot));
            var free=Enumerable.Range(0,config.slots.Count).Where(i=>!used.Contains(i)).ToList();
            if(free.Count==0)return -1;
            return free[random.Next(free.Count)];
        }
        public static string SavePath{get{return Storage.GardenPath;}}
        public static GardenState LoadState(){
            GardenState state=null;
            try{if(File.Exists(SavePath)){state=Json.Read<GardenState>(SavePath);if(state!=null&&state.plants==null)state.plants=new List<GardenPlant>();}}catch(Exception ex){Storage.Log(ex);}
            return state;
        }
        public static string Save(GardenEngine engine){
            try{
                engine.State.savedAt=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture);
                Json.Write(SavePath,engine.State);engine.Dirty=false;return null;
            }catch(Exception ex){Storage.Log(ex);return ex.Message;}
        }
        public static double OfflineSeconds(GardenState state){
            if(state==null||String.IsNullOrEmpty(state.savedAt))return 0;
            DateTime saved;
            if(!DateTime.TryParse(state.savedAt,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out saved))return 0;
            double seconds=(DateTime.UtcNow-saved.ToUniversalTime()).TotalSeconds;
            return Math.Max(0,Math.Min(6*3600,seconds));
        }
        public void CatchUp(double seconds){
            if(seconds<=0)return;
            double left=seconds;
            while(left>0){double step=Math.Min(20,left);AdvanceCore(step,true);left-=step;}
        }
        public GardenPlant Plant(string speciesId){return Plant(speciesId,-1);}
        /// <summary>撒一包种子；slot 为 -1 时随机挑一个空位，指定槽位已被占用或越界则失败。</summary>
        public GardenPlant Plant(string speciesId,int slot){
            GardenSpecies species=Config.SpeciesOf(speciesId);
            if(species==null||Full)return null;
            if(slot>=0){
                if(slot>=Config.slots.Count||State.plants.Any(p=>p.slot==slot))return null;
            }else slot=FreeSlot(Config,State,random);
            if(slot<0)return null;
            var plant=new GardenPlant(species.id,slot,.03,State.nextId++);
            State.plants.Add(plant);Dirty=true;Sow(slot,species);return plant;
        }
        /// <summary>撒种的即刻反馈：几粒种子落下来，扬一小把土，嫩芽位置冒几个光点。</summary>
        void Sow(int slot,GardenSpecies species){
            var spot=Config.slots[slot];
            SowSlot=slot;SowGlow=1;
            int dirtLight=Colors["soilLight"].ToArgb(),dirtDark=Colors["soilDark"].ToArgb();
            int seedColor=Colors["outline"].ToArgb();
            for(int i=0;i<3;i++){
                Sparks.Add(new GardenSpark{
                    x=spot.x+(i-1)*4+(random.NextDouble()-.5)*2,y=spot.y-12-random.NextDouble()*3,
                    vx=(random.NextDouble()-.5)*4,vy=24+random.NextDouble()*8,
                    life=.45,maxLife=.45,floorY=spot.y,kind=2,color=seedColor});
            }
            for(int i=0;i<8;i++){
                double spread=(random.NextDouble()-.5);
                Sparks.Add(new GardenSpark{
                    x=spot.x+spread*7,y=spot.y-1,
                    vx=spread*18,vy=-15-random.NextDouble()*14,
                    life=.55+random.NextDouble()*.3,maxLife=.85,floorY=spot.y+2,kind=0,
                    color=(i%3==0)?dirtLight:dirtDark});
            }
            int leafColor=ColorTranslator.FromHtml(species.leafColor).ToArgb();
            for(int i=0;i<4;i++){
                Sparks.Add(new GardenSpark{
                    x=spot.x+(random.NextDouble()-.5)*3,y=spot.y-2-random.NextDouble()*2,
                    vx=(random.NextDouble()-.5)*3,vy=-7-random.NextDouble()*5,
                    life=.75+random.NextDouble()*.45,maxLife=1.2,floorY=spot.y+2,kind=1,
                    color=leafColor});
            }
        }
        public bool Remove(GardenPlant plant){
            if(plant==null||!State.plants.Remove(plant))return false;
            Dirty=true;return true;
        }
        public void Water(){
            State.moisture=1;Watering=1;Dirty=true;
            foreach(var plant in State.plants)plant.wilt=Math.Max(0,plant.wilt-.45);
            for(int i=0;i<10;i++)Petals.Add(new GardenPetal{
                x=16+random.NextDouble()*(Config.bed.right-Config.bed.left-12),
                y=Config.bed.top+6+random.NextDouble()*10,
                vx=(random.NextDouble()-.5)*6,vy=26+random.NextDouble()*16,
                life=.5+random.NextDouble()*.35,maxLife=.85,phase=random.NextDouble()*6.28,
                kind=1,color=Colors["waterLight"].ToArgb()});
        }
        Dictionary<string,Color> parsed;
        Dictionary<string,Color> Colors {
            get{
                if(parsed==null){parsed=new Dictionary<string,Color>();foreach(var pair in Config.colors)parsed[pair.Key]=ColorTranslator.FromHtml(pair.Value);}
                return parsed;
            }
        }
        public int FlowerColor(GardenPlant plant){
            GardenSpecies s=Config.SpeciesOf(plant.species);
            if(s==null||s.flower==null||s.flower.form=="none")return Colors["grassLight"].ToArgb();
            return ColorTranslator.FromHtml(s.flower.color).ToArgb();
        }
        double GrowthFactor(double moisture){
            var m=Config.moisture;
            if(moisture<=m.dryBelow)return .12;
            if(moisture<m.thirstyBelow)return .35+.65*(moisture-m.dryBelow)/(m.thirstyBelow-m.dryBelow);
            return 1;
        }
        public void Advance(double dt){
            if(dt<=0)return;
            if(dt>.5)dt=.5;
            AdvanceCore(dt,false);
        }
        void AdvanceCore(double dt,bool offline){
            Time+=dt;
            State.moisture=Math.Max(0,State.moisture-dt/Config.moisture.fullSeconds);
            double factor=GrowthFactor(State.moisture),dry=Config.moisture.dryBelow;
            foreach(var plant in State.plants){
                GardenSpecies species=Config.SpeciesOf(plant.species);
                if(species==null)continue;
                if(plant.growth<1)plant.growth=Math.Min(1,plant.growth+dt/species.growSeconds*factor*(1-.55*plant.wilt));
                plant.wilt=State.moisture<dry
                    ?Math.Min(1,plant.wilt+dt/Config.moisture.wiltSeconds)
                    :Math.Max(0,plant.wilt-dt/(Config.moisture.wiltSeconds*.6));
            }
            if(!Full&&State.moisture>Config.spread.minMoisture){
                int flowering=State.plants.Count(p=>IsBlooming(p)&&Flowering(p));
                if(flowering>0){
                    spreadAccum+=dt/60.0*Config.spread.chancePerMinute*flowering;
                    while(spreadAccum>=1&&!Full){spreadAccum-=1;Plant(Config.species[random.Next(Config.species.Count)].id);}
                    if(spreadAccum>1)spreadAccum=1;
                }else spreadAccum=0;
            }
            // 离线补算只推进生长，不保留任何一次性粒子（否则回来会看到一堆陈年土屑）。
            if(offline){Sparks.Clear();SowGlow=0;return;}
            nextWind-=dt;
            if(nextWind<=0){
                nextWind=Config.wind.minSeconds+random.NextDouble()*(Config.wind.maxSeconds-Config.wind.minSeconds);
                if(windLeft<=0&&Blooming>0&&random.NextDouble()<Config.wind.chance)StartWind();
            }
            if(windLeft>0){
                windLeft-=dt;
                windDirSign=Math.Sign(WindDirection);
                int rate=(int)(Config.wind.petalsPerSecond*dt+random.NextDouble());
                for(int i=0;i<rate;i++)SpawnPetal();
                if(windLeft<=0)windTarget=0;
            }
            Wind+=(windTarget-Wind)*Math.Min(1,dt*2.2);
            Watering=Math.Max(0,Watering-dt/2.2);
            SowGlow=Math.Max(0,SowGlow-dt/1.6);
            UpdateSparks(dt);
            UpdatePetals(dt);
        }
        void UpdateSparks(double dt){
            for(int i=Sparks.Count-1;i>=0;i--){
                var s=Sparks[i];
                s.life-=dt;
                if(s.life<=0){Sparks.RemoveAt(i);continue;}
                s.vy+=(s.kind==1?-9:64)*dt;   // 土粒和种子落得快，光点只是慢慢上浮
                s.x+=s.vx*dt;s.y+=s.vy*dt;
                if(s.kind!=1&&s.vy>0&&s.y>s.floorY){Sparks.RemoveAt(i);continue;}
            }
            if(Sparks.Count>80)Sparks.RemoveRange(0,Sparks.Count-80);
        }
        void StartWind(){
            double min=Config.wind.durationSeconds[0],max=Config.wind.durationSeconds[1];
            windLeft=min+random.NextDouble()*(max-min);
            windTarget=.55+random.NextDouble()*.45;
            WindDirection=random.NextDouble()<.5?-1:1;
            if(windLeft>0)Dirty=true;
        }
        void SpawnPetal(){
            var blooming=State.plants.Where(p=>IsBlooming(p)&&Flowering(p)).ToList();
            if(blooming.Count==0)return;
            var plant=blooming[random.Next(blooming.Count)];
            GardenSpecies species=Config.SpeciesOf(plant.species);
            GardenSlot slot=Config.slots[plant.slot];
            double tipY=slot.y-species.height*slot.scale*(0.6+random.NextDouble()*.4);
            double tipX=slot.x+(random.NextDouble()-.5)*6;
            // 花瓣在花色和副色之间取，偶尔提亮一点：同一株落下来的花瓣也有深有浅。
            int color=ColorTranslator.FromHtml(random.NextDouble()<.72?species.flower.color:species.flower.color2).ToArgb();
            if(random.NextDouble()<.24)color=Lighten(color,38);
            Petals.Add(new GardenPetal{
                x=tipX,y=tipY,
                vx=WindDirection*(4+random.NextDouble()*8),
                vy=2+random.NextDouble()*3.5,
                life=3.4+random.NextDouble()*3.4,maxLife=6.8,phase=random.NextDouble()*6.28,
                spin=.5+random.NextDouble()*1.6,
                flutter=.5+random.NextDouble()*1.1,
                size=random.NextDouble()<.26?2:1,kind=0,
                color=color});
        }
        void UpdatePetals(double dt){
            for(int i=Petals.Count-1;i>=0;i--){
                var p=Petals[i];
                p.life-=dt;
                if(p.life<=0||p.x<-8||p.x>Config.canvas.width+8||p.y>Config.canvas.height+4){Petals.RemoveAt(i);continue;}
                if(p.kind==1){
                    // 水珠直着落，砸到土上就没了。
                    p.vy=Math.Min(p.vy+110*dt,150);
                    p.x+=p.vx*dt;p.y+=p.vy*dt;continue;
                }
                p.vx+=WindDirection*4.5*dt;
                // 每片都有自己的节奏：一阵快落、一阵几乎悬停，横向再摆一个 S 形，
                // 所以少几片也不显得空，反而比"一把一起往下掉"更像被风吹散的花瓣。
                double terminal=10.5+5.5*Math.Sin(p.phase+p.life*p.flutter*2.4);
                p.vy+=(terminal-p.vy)*Math.Min(1,dt*(p.vy<terminal?3.2:5.0));
                p.x+=(p.vx+Math.Sin(p.phase*1.3+p.life*3.1)*11)*dt;
                p.y+=p.vy*dt;
            }
            int cap=Config.wind.petalCap>=8?Config.wind.petalCap:48;
            if(Petals.Count>cap)Petals.RemoveRange(0,Petals.Count-cap);
        }
        static int Lighten(int argb,int amount){
            int r=Math.Min(255,((argb>>16)&255)+amount),g=Math.Min(255,((argb>>8)&255)+amount),b=Math.Min(255,(argb&255)+amount);
            return ((argb>>24)&255)<<24|r<<16|g<<8|b;
        }
        public double AverageGrowth{get{return State.plants.Count==0?0:State.plants.Average(p=>p.growth);}}
        public GardenPlant Soonest{get{return State.plants.OrderByDescending(p=>p.growth).FirstOrDefault();}}
        public int WateredPercent{get{return (int)Math.Round(State.moisture*100);}}
    }
}
