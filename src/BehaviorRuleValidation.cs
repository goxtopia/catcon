using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Catcon {
    static class BehaviorRuleValidation {
        public static void Run(Skin skin,string root,Action<bool,string> check){
            Func<BehaviorConfig> fixture=()=>{
                var c=Json.Read<BehaviorConfig>(Path.Combine(root,"config","behaviors.json"));c.ambientChance=0;
                foreach(var name in c.contexts.Keys.ToArray())c.contexts[name]=new List<Choice>{new Choice{clip="sit",weight=1}};
                c.followUps=new Dictionary<string,FollowUpRule>{{"knead",new FollowUpRule{chance=1,choices=new List<Choice>{new Choice{clip="sleep",weight=1}}}}};
                c.independentActions=new List<IndependentAction>();return c;
            };
            Func<double,double,IndependentAction> timed=(chance,seconds)=>new IndependentAction{clip="break_sign",chance=chance,intervalSeconds=new[]{seconds,seconds}};
            var legacy=new JavaScriptSerializer().Deserialize<BehaviorConfig>("{\"schemaVersion\":1}");
            check(legacy.followUps.Count==0&&legacy.independentActions.Count==0,"Older behavior data defaults to no additional probability rules");
            var config=fixture();config.Validate(skin);var rules=new BehaviorRules(skin,config,new Random(123));
            check(rules.FollowUp("knead",false)=="sleep"&&rules.FollowUp("sit",false)==null,"Follow-up applies only to its source action");
            config.followUps["knead"].chance=0;check(rules.FollowUp("knead",true)==null,"Zero conditional probability never triggers");
            config.followUps["knead"].chance=.8;int hits=0;
            for(int i=0;i<5000;i++)if(rules.FollowUp("knead",false)!=null)hits++;
            check(Math.Abs(hits/5000.0-.8)<.025,"Conditional probability is independent of ordinary action pool weights");
            config.followUps["knead"].chance=1;config.followUps["knead"].choices.Add(new Choice{clip="sleep_stretch",weight=3});hits=0;
            for(int i=0;i<5000;i++)if(rules.FollowUp("knead",false)=="sleep_stretch")hits++;
            check(Math.Abs(hits/5000.0-.75)<.025,"Conditional destinations retain their configured relative weights");
            config.followUps["knead"].choices=new List<Choice>{new Choice{clip="run",weight=1}};
            check(rules.FollowUp("knead",false)==null&&rules.FollowUp("knead",true)=="run","Conditional rules respect disabled autonomous movement");

            config=fixture();config.independentActions.Add(timed(1,30));rules=new BehaviorRules(skin,config,new Random(124));
            rules.Advance(29.99,"coding",false,false);check(rules.TakeIndependent("coding",false,false)==null,"Independent action waits its full initial interval");
            rules.Advance(30,"coding",false,false);check(rules.TakeIndependent("coding",false,false)=="break_sign"&&rules.TakeIndependent("coding",false,false)==null,"Due opportunity is consumed exactly once");
            rules.Played("break_sign",30);rules.Advance(59.99,"coding",false,false);check(rules.TakeIndependent("coding",false,false)==null,"Playing a scheduled action starts a new cooldown");
            rules.Advance(60,"coding",false,false);check(rules.TakeIndependent("coding",false,false)=="break_sign","Next independent window becomes eligible after its interval");
            config.independentActions[0].chance=.4;rules=new BehaviorRules(skin,config,new Random(125));hits=0;bool noReroll=true;
            for(int i=1;i<=5000;i++){
                rules.Advance(i*30,"coding",false,false);if(rules.TakeIndependent("coding",false,false)!=null)hits++;
                for(int frame=1;frame<5;frame++){rules.Advance(i*30+frame*.02,"coding",false,false);noReroll&=rules.TakeIndependent("coding",false,false)==null;}
            }
            check(noReroll,"Independent windows never re-roll between deadlines across 5000 trials");
            check(Math.Abs(hits/5000.0-.4)<.025,"Independent action probability is evaluated once per window");

            config=fixture();var jitter=timed(1,1080);jitter.intervalSeconds[1]=1320;config.independentActions.Add(jitter);
            bool noEarly=true,allDue=true;int midpoints=0;
            for(int i=0;i<200;i++){
                rules=new BehaviorRules(skin,config,new Random(i+300));rules.Advance(1079.9,"idle",false,false);noEarly&=rules.TakeIndependent("idle",false,false)==null;
                rules.Advance(1200,"idle",false,false);if(rules.TakeIndependent("idle",false,false)!=null)midpoints++;
                rules=new BehaviorRules(skin,config,new Random(i+300));rules.Advance(1320.1,"idle",false,false);allDue&=rules.TakeIndependent("idle",false,false)=="break_sign";
            }
            check(noEarly&&allDue&&midpoints>60&&midpoints<140,"Independent deadlines vary within the configured 18 to 22 minute range");

            config=fixture();config.independentActions.Add(timed(1,30));rules=new BehaviorRules(skin,config,new Random(126));
            rules.Advance(30,"gaming",false,true);rules.Advance(31,"coding",false,false);
            check(rules.TakeIndependent("coding",false,false)==null,"Quiet gaming consumes a due window without a delayed reminder");
            rules.Advance(60,"coding",false,false);rules.Advance(61,"gaming",false,true);rules.Advance(62,"coding",false,false);
            check(rules.TakeIndependent("coding",false,false)==null,"Entering quiet gaming discards an unplayed opportunity");
            config.independentActions[0].contexts=new[]{"office"};rules=new BehaviorRules(skin,config,new Random(127));
            rules.Advance(30,"coding",false,false);rules.Advance(31,"office",false,false);check(rules.TakeIndependent("office",false,false)==null,"Ineligible context skips the window without immediate catch-up");
            rules.Advance(60,"office",false,false);check(rules.TakeIndependent("office",false,false)=="break_sign","Eligible independent context can trigger at its next window");
            config.independentActions[0].contexts=null;rules=new BehaviorRules(skin,config,new Random(128));
            rules.Advance(10000,"idle",false,false);check(rules.TakeIndependent("idle",false,false)=="break_sign"&&rules.TakeIndependent("idle",false,false)==null,"Large time jump creates at most one pending opportunity");
            rules.Advance(10000.01,"idle",false,false);check(rules.TakeIndependent("idle",false,false)==null,"Resume does not replay missed windows");
            rules=new BehaviorRules(skin,config,new Random(129));rules.Advance(30,"idle",false,false);rules.Played("break_sign",31);rules.Advance(60,"idle",false,false);
            check(rules.TakeIndependent("idle",false,false)==null,"Manual playback cancels pending copies and restarts interval");
            rules.Advance(61,"idle",false,false);check(rules.TakeIndependent("idle",false,false)=="break_sign","Manual playback cooldown uses actual playback time");
            rules=new BehaviorRules(skin,config,new Random(138));rules.Advance(30,"idle",false,false);config.independentActions[0].chance=0;rules.Advance(60,"idle",false,false);
            check(rules.TakeIndependent("idle",false,false)==null,"Unplayed opportunities expire instead of accumulating across windows");
            config.independentActions[0].chance=1;config.independentActions[0].clip="run";rules=new BehaviorRules(skin,config,new Random(139));rules.Advance(30,"idle",false,false);rules.Advance(31,"idle",true,false);
            check(rules.TakeIndependent("idle",true,false)==null,"Independent movement is skipped while autonomous movement is disabled");

            var input=new InputFrame{Context="coding",Movement=false,Reactions=false,Cursor=new Point(1500,800),WorkArea=new Rectangle(0,0,1600,900)};
            config=fixture();var engine=new BehaviorEngine(skin,config,130);engine.Play("knead",false);
            engine.Advance(skin.clips["knead"].seconds-.01,input);check(engine.Animation.Current=="knead","Context changes let conditional source finish before its follow-up");
            engine.Advance(.02,input);check(engine.Animation.Current=="sleep","Completed kneading follows its conditional sleep choice");
            config=fixture();config.followUps["knead"].chance=0;engine=new BehaviorEngine(skin,config,137);engine.Play("knead",true);engine.Advance(skin.clips["knead"].seconds+.01,input);
            check(engine.Animation.Current=="sit","Missed conditional chance falls through to the ordinary action pool");
            config.followUps["knead"].chance=1;engine.Advance(.1,input);check(engine.Animation.Current=="sit","A missed conditional chance is not retried after the source has ended");
            config=fixture();config.independentActions.Add(timed(1,30));engine=new BehaviorEngine(skin,config,131);
            engine.Play("knead",true);engine.Advance(1,input);engine.Emit("pet");engine.Advance(skin.clips["pet"].seconds+.01,input);
            check(engine.Animation.Current=="sit","User interruption cancels a stale conditional sequence");
            engine=new BehaviorEngine(skin,config,132);engine.Emit("pet");engine.Advance(.01,input,30);
            check(engine.Animation.Current=="pet","Independent timer uses elapsed time while preserving a protected interaction");
            engine.Advance(skin.clips["pet"].seconds+.01,input);check(engine.Animation.Current=="break_sign","Pending independent action plays at the next safe decision");
            engine=new BehaviorEngine(skin,config,133);engine.Paused=true;engine.Advance(35,input);engine.Paused=false;engine.Advance(8.1,input);
            check(engine.Animation.Current=="sit","Pause freezes independent action timers");
            engine=new BehaviorEngine(skin,config,134);engine.BeginDrag();engine.Advance(31,input);check(engine.Animation.Current=="drag","Dragging blocks a due independent action");
            engine.EndDrag();engine.Advance(skin.clips["sit"].seconds+.01,input);check(engine.Animation.Current=="break_sign","Due opportunity can wait until dragging and release finish");
            config=fixture();config.independentActions.Add(timed(1,30));config.followUps["knead"].chance=1;engine=new BehaviorEngine(skin,config,135);
            engine.Play("knead",true);engine.Advance(skin.clips["knead"].seconds+.01,input,30);
            check(engine.Animation.Current=="sleep","Conditional continuity has priority over an independent opportunity");
            engine.Advance(skin.clips["sleep"].seconds+.01,input);check(engine.Animation.Current=="break_sign","Lower priority opportunity remains available after the follow-up");
            config=fixture();config.independentActions.Add(timed(1,30));engine=new BehaviorEngine(skin,config,136);engine.Emit("chase");
            engine.Advance(.02,input,30);check(engine.Animation.Current=="run_cursor","Independent actions do not interrupt a running cursor chase");
            for(int i=0;i<1000&&engine.Animation.Current!="break_sign";i++)engine.Advance(.02,input);
            check(engine.Animation.Current=="break_sign","Independent action waits through cursor chase and landing");

            Action<Action<BehaviorConfig>,string> rejects=(mutate,label)=>{bool rejected=false;try{var bad=fixture();mutate(bad);bad.Validate(skin);}catch(InvalidDataException){rejected=true;}check(rejected,label);};
            rejects(c=>c.followUps["knead"].chance=Double.NaN,"Non-finite conditional probability is rejected");
            rejects(c=>c.followUps["missing"]=c.followUps["knead"],"Unknown conditional source is rejected");
            rejects(c=>c.followUps["knead"].choices[0].clip="missing","Unknown conditional destination is rejected");
            rejects(c=>c.followUps["knead"].choices[0].weight=0,"Non-positive conditional weights are rejected");
            rejects(c=>c.independentActions.Add(timed(1,-30)),"Invalid independent interval is rejected");
            rejects(c=>c.independentActions.Add(timed(1.1,30)),"Out-of-range independent probability is rejected");
            rejects(c=>{var r=timed(1,30);r.contexts=new[]{"missing"};c.independentActions.Add(r);},"Unknown independent context is rejected");
            rejects(c=>{c.independentActions.Add(timed(1,30));c.independentActions.Add(timed(1,60));},"Duplicate independent action timers are rejected");
        }
    }
}
