using System;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Catcon {
    public sealed class ClassifierRule {
        public string category {get;set;} public string process {get;set;} public string title {get;set;} public string product {get;set;}
        [System.Web.Script.Serialization.ScriptIgnore]public Regex ProcessRegex,TitleRegex,ProductRegex;
    }
    public sealed class KeywordSignal {
        public string category {get;set;}public string group {get;set;}public string pattern {get;set;}public double weight {get;set;}
        [System.Web.Script.Serialization.ScriptIgnore]public Regex PatternRegex;
    }
    public sealed class FallbackConfig {
        public bool enabled {get;set;}public double minConfidence {get;set;}public double minMargin {get;set;}public double minScore {get;set;}
        public double evidenceScale {get;set;}public double fullscreenWeight {get;set;}public List<KeywordSignal> signals {get;set;}
        public void Prepare(){
            Skin.Require(minConfidence>0&&minConfidence<=1&&minMargin>=0&&minMargin<=1&&minScore>0&&evidenceScale>0&&fullscreenWeight>=0,"Invalid fallback thresholds");
            Skin.Require(signals!=null&&signals.Count>0,"Missing keyword signals");
            foreach(var s in signals){Skin.Require(ContextNames.All.ContainsKey(s.category)&&s.category!="idle"&&!String.IsNullOrEmpty(s.group)&&!String.IsNullOrEmpty(s.pattern)&&s.weight>0,"Invalid keyword signal");s.PatternRegex=ClassifierConfig.Compile(s.pattern);}
        }
    }
    public sealed class Classification {
        public string Category,Source;public double Confidence,Margin,EvidenceScore;public bool Accepted;
        public Classification(string category,string source,double confidence,bool accepted){Category=category;Source=source;Confidence=confidence;Accepted=accepted;}
    }
    public sealed class ClassifierConfig {
        public int schemaVersion {get;set;}public double pollSeconds {get;set;}public int stableSamples {get;set;}
        public List<ClassifierRule> rules {get;set;} public bool fullscreenGameFallback {get;set;}public string fullscreenExclude {get;set;}
        public FallbackConfig fallback {get;set;}
        [System.Web.Script.Serialization.ScriptIgnore]public Regex ExcludeRegex;
        public static Regex Compile(string pattern){return new Regex(pattern,RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(60));}
        public void Prepare(){
            Skin.Require(schemaVersion==1&&pollSeconds>=.5&&stableSamples>=1&&rules!=null,"Invalid classifier configuration");
            foreach(var r in rules){
                Skin.Require(ContextNames.All.ContainsKey(r.category)&&(!String.IsNullOrEmpty(r.process)||!String.IsNullOrEmpty(r.product)),"Invalid classifier rule");
                if(r.process!=null)r.ProcessRegex=Compile(r.process);if(r.title!=null)r.TitleRegex=Compile(r.title);if(r.product!=null)r.ProductRegex=Compile(r.product);
            }
            ExcludeRegex=Compile(fullscreenExclude??"^$");
            if(fallback!=null)fallback.Prepare();
        }
        public string Classify(string process,string title,bool fullscreen) {
            return ClassifyDetailed(process,title,fullscreen,"",true).Category;
        }
        public Classification ClassifyDetailed(string process,string title,bool fullscreen,string product,bool smart){
            process=Bound(process,160);title=Bound(title,1024);product=Bound(product,256);
            Classification browser=null;
            try {
                foreach(var r in rules)if((r.ProcessRegex==null||r.ProcessRegex.IsMatch(process))&&(r.TitleRegex==null||r.TitleRegex.IsMatch(title))&&(r.ProductRegex==null||r.ProductRegex.IsMatch(product))){
                    var result=new Classification(r.category,r.ProductRegex!=null?"product":r.TitleRegex!=null?"title":"process",r.ProductRegex!=null?.97:r.TitleRegex!=null?.93:.99,true);
                    // Keep generic browsing as a known-app baseline, so an
                    // unlisted developer/video/document site can still infer.
                    if(r.category=="browsing"&&r.TitleRegex==null){browser=result;continue;}
                    return result;
                }
                if(smart&&fallback!=null&&fallback.enabled){
                    var inferred=Infer(process,title,fullscreen,product);
                    if(inferred.Accepted||browser==null||inferred.EvidenceScore>=fallback.minScore)return inferred;
                }
            }catch(RegexMatchTimeoutException){}
            return browser??new Classification("idle","unknown",0,false);
        }
        Classification Infer(string process,string title,bool fullscreen,string product){
            var groups=new Dictionary<string,double>();var budget=Stopwatch.StartNew();
            foreach(var signal in fallback.signals){
                if(budget.ElapsedMilliseconds>100)return new Classification("idle","uncertain",0,false);
                double score=0;
                try{
                    if(signal.PatternRegex.IsMatch(title))score=signal.weight;
                    if(signal.PatternRegex.IsMatch(product))score=Math.Max(score,signal.weight*1.15);
                    if(signal.PatternRegex.IsMatch(process))score=Math.Max(score,signal.weight*1.25);
                }catch(RegexMatchTimeoutException){continue;}
                string key=signal.category+":"+signal.group;double previous;
                if(score>0&&(!groups.TryGetValue(key,out previous)||score>previous))groups[key]=score;
            }
            if(budget.ElapsedMilliseconds>100)return new Classification("idle","uncertain",0,false);
            // Fullscreen alone is weak evidence: unknown office/media windows
            // must not become games just because they occupy the screen.
            if(fullscreenGameFallback&&fullscreen&&!ExcludeRegex.IsMatch(process))groups["gaming:fullscreen"]=fallback.fullscreenWeight;
            var scores=ContextNames.All.Keys.Where(k=>k!="idle").Select(k=>new{Category=k,Score=groups.Where(g=>g.Key.StartsWith(k+":",StringComparison.Ordinal)).Sum(g=>g.Value)}).OrderByDescending(s=>s.Score).ToArray();
            double top=scores[0].Score,runner=scores[1].Score;
            double margin=top>0?(top-runner)/(top+runner):0;
            double confidence=top>0?(1-Math.Exp(-top/fallback.evidenceScale))*(1-.5*runner/top):0;
            bool accepted=top>=fallback.minScore&&confidence>=fallback.minConfidence&&margin>=fallback.minMargin;
            return new Classification(accepted?scores[0].Category:"idle",accepted?"keywords":"uncertain",confidence,accepted){Margin=margin,EvidenceScore=top};
        }
        static string Bound(string value,int length){return String.IsNullOrEmpty(value)?"":value.Length<=length?value:value.Substring(0,length);}
    }
    public sealed class ContextMonitor {
        readonly ClassifierConfig config;readonly int ownPid;
        string candidate="idle";int samples;double untilPoll;
        sealed class Identity{public long Started;public string Product;}
        readonly Dictionary<int,Identity> identities=new Dictionary<int,Identity>();
        public string Category {get;private set;}public string ProcessName {get;private set;}public string Reason {get;private set;}public double Confidence {get;private set;}
        public ContextMonitor(ClassifierConfig config){this.config=config;ownPid=Process.GetCurrentProcess().Id;Category="idle";ProcessName="";Reason="等待前台应用";}
        public void Update(double dt,bool enabled,bool smart){
            untilPoll-=dt;if(!enabled){Category="idle";Confidence=0;Reason="自动识别已关闭";candidate="idle";samples=0;return;}if(untilPoll>0)return;untilPoll=config.pollSeconds;
            IntPtr hwnd=Native.GetForegroundWindow();if(hwnd==IntPtr.Zero){Observe(new Classification("idle","unavailable",0,false),"");return;}
            uint pid;Native.GetWindowThreadProcessId(hwnd,out pid);if(pid==ownPid)return;
            try {
                string name,product;using(var p=Process.GetProcessById((int)pid)){name=p.ProcessName.ToLowerInvariant();product=ProductIdentity(p);}
                var title=new StringBuilder(1024);Native.GetWindowText(hwnd,title,title.Capacity);
                Native.RECT rect;bool full=false;
                if(Native.GetWindowRect(hwnd,out rect)){var bounds=Screen.FromHandle(hwnd).Bounds;full=Math.Abs(rect.Left-bounds.Left)<=2&&Math.Abs(rect.Top-bounds.Top)<=2&&Math.Abs(rect.Right-bounds.Right)<=2&&Math.Abs(rect.Bottom-bounds.Bottom)<=2;}
                Observe(config.ClassifyDetailed(name,title.ToString(),full,product,smart),name);
            }catch(ArgumentException){Unavailable();}catch(System.ComponentModel.Win32Exception){Unavailable();}catch(InvalidOperationException){Unavailable();}
        }
        void Unavailable(){Observe(new Classification("idle","unavailable",0,false),"");}
        internal void Observe(Classification classification,string name){
            string result=classification.Category;if(result==candidate)samples++;else{candidate=result;samples=1;}
            if(samples<config.stableSamples)return;
            Category=result;ProcessName=name;Confidence=classification.Confidence;
            Reason=result=="idle"?(classification.Source=="unavailable"?"未取得前台信息 · 自由活动":classification.Source=="uncertain"?"线索不足或冲突 · 自由活动":"未匹配到规则 · 自由活动"):
                (classification.Source=="keywords"?"智能推断 "+Confidence.ToString("P0")+" · ":"应用匹配 · ")+name;
        }
        string ProductIdentity(Process process){
            try{
                long started=process.StartTime.Ticks;Identity saved;
                if(identities.TryGetValue(process.Id,out saved)&&saved.Started==started)return saved.Product;
                var info=process.MainModule.FileVersionInfo;string product=(info.ProductName??"")+" "+(info.FileDescription??"");
                if(identities.Count>=64)identities.Clear();identities[process.Id]=new Identity{Started=started,Product=product};return product;
            }catch(System.ComponentModel.Win32Exception){return "";}catch(InvalidOperationException){return "";}catch(System.NotSupportedException){return "";}catch(UnauthorizedAccessException){return "";}catch(System.IO.IOException){return "";}
        }
    }
}
