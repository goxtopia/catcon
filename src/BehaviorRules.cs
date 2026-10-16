using System;
using System.Linq;
using System.Collections.Generic;

namespace Catcon {
    // Rule state belongs to one running pet; it never writes user activity.
    sealed class BehaviorRules {
        sealed class Opportunity {
            public IndependentAction Rule;public double Next;public bool Pending;
        }
        readonly Skin skin;readonly BehaviorConfig config;readonly Random random;
        readonly List<Opportunity> opportunities=new List<Opportunity>();
        public BehaviorRules(Skin skin,BehaviorConfig config,Random random){
            this.skin=skin;this.config=config;this.random=random;
            foreach(var rule in config.independentActions)opportunities.Add(new Opportunity{Rule=rule,Next=Interval(rule)});
        }
        double Interval(IndependentAction rule){return rule.intervalSeconds[0]+random.NextDouble()*(rule.intervalSeconds[1]-rule.intervalSeconds[0]);}
        bool Eligible(IndependentAction rule,string context,bool movement,bool quiet){
            return !(quiet&&rule.quietGames)&&(movement||skin.clips[rule.clip].motion==null)&&(rule.contexts==null||rule.contexts.Contains(context));
        }
        public bool HasFollowUp(string clip){return config.followUps.ContainsKey(clip);}
        public string FollowUp(string clip,bool movement){
            FollowUpRule rule;if(!config.followUps.TryGetValue(clip,out rule))return null;
            var choices=rule.choices.Where(c=>movement||skin.clips[c.clip].motion==null).ToList();
            if(choices.Count==0||rule.chance<=0||random.NextDouble()>=rule.chance)return null;
            int roll=random.Next(choices.Sum(c=>c.weight));
            foreach(var choice in choices){roll-=choice.weight;if(roll<0)return choice.clip;}
            return null;
        }
        public void Advance(double now,string context,bool movement,bool quiet){
            foreach(var item in opportunities){
                bool eligible=Eligible(item.Rule,context,movement,quiet);
                if(!eligible)item.Pending=false;
                if(now<item.Next)continue;
                // One draw per elapsed window, even after suspend or a long drag.
                // A miss consumes the window; frame rate cannot amplify chance.
                item.Next=now+Interval(item.Rule);item.Pending=false;
                if(eligible&&item.Rule.chance>0&&random.NextDouble()<item.Rule.chance)item.Pending=true;
            }
        }
        public string TakeIndependent(string context,bool movement,bool quiet){
            var ready=opportunities.Where(x=>x.Pending&&Eligible(x.Rule,context,movement,quiet)).ToList();
            if(ready.Count==0)return null;
            var chosen=ready[random.Next(ready.Count)];chosen.Pending=false;return chosen.Rule.clip;
        }
        public void Played(string clip,double now){
            foreach(var item in opportunities.Where(x=>x.Rule.clip==clip)){item.Pending=false;item.Next=now+Interval(item.Rule);}
        }
    }
}
