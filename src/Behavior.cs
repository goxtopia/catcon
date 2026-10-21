using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

namespace Catcon {
    public sealed class InputFrame {
        public Point Cursor;public Rectangle WorkArea;public string Context="idle";
        public bool Movement=true,Reactions=true,QuietGames=true;
    }
    public sealed class BehaviorEngine {
        readonly BehaviorConfig config;readonly Random random;readonly BehaviorRules rules;
        readonly Queue<double> startles=new Queue<double>();
        public readonly Animator Animation;
        public double X,Y;public int PixelScale=3;
        public bool Paused,Dragging;
        public PointF? Destination;
        /// <summary>走到外部目的地（例如花圃）后触发一次，用来接着做一段互动。</summary>
        public Action Arrived;
        public string LastEvent {get;private set;}public string Context {get;private set;}
        double clock,activityClock,nextDecision,lockedUntil,lastStartle=-100,dwell;
        Point previousCursor;bool hadCursor;
        PointF? target,arrivalTarget;PointF leapStart;string motionClip;
        public bool Moving {get{return target.HasValue;}}
        public bool Protected {get{return clock<lockedUntil;}}
        public int Width {get{return Animation.Skin.canvas*PixelScale;}}
        public BehaviorEngine(Skin skin,BehaviorConfig config,int seed){this.config=config;random=new Random(seed);rules=new BehaviorRules(skin,config,random);Animation=new Animator(skin);Animation.Play(config.events["idle"]);Context="idle";LastEvent="刚刚醒来";nextDecision=8;}
        public void Emit(string ev){Play(config.events[ev],true);LastEvent=ev;}
        public void Play(string clip,bool protect){Animation.Play(clip);rules.Played(clip,activityClock);target=null;arrivalTarget=null;motionClip=null;dwell=0;lockedUntil=protect?clock+Animation.Clip.seconds:clock;nextDecision=clock+Animation.Clip.seconds;Destination=null;}
        public void BeginDrag(){Dragging=true;Emit("drag");}
        public void EndDrag(){Dragging=false;Emit("release");hadCursor=false;}
        public void Advance(double dt,InputFrame input,double? elapsedSeconds=null){
            if(dt<=0)return;
            if(Paused){previousCursor=input.Cursor;hadCursor=true;return;}
            clock+=dt;activityClock+=Math.Max(0,elapsedSeconds??dt);Animation.Advance(dt);
            if(Context!=input.Context)nextDecision=Math.Min(nextDecision,Math.Max(clock,lockedUntil)+config.contextChangeSeconds);
            Context=input.Context;
            bool quiet=input.QuietGames&&Context=="gaming",allowMovement=input.Movement&&!quiet;
            rules.Advance(activityClock,Context,allowMovement,quiet);
            if(Dragging){previousCursor=input.Cursor;hadCursor=true;return;}
            if(!input.Movement&&Animation.Clip.motion!=null&&Animation.Clip.motion!="cursor"&&Animation.Clip.motion!="leap") {target=null;Play(config.events["idle"],false);}
            if(!quiet&&input.Reactions&&hadCursor&&clock>=lockedUntil){
                double cx=X+Width*.5,cy=Y+Width*.58;
                double distance=Distance(input.Cursor.X,input.Cursor.Y,cx,cy),before=Distance(previousCursor.X,previousCursor.Y,cx,cy);
                double speed=Distance(input.Cursor.X,input.Cursor.Y,previousCursor.X,previousCursor.Y)/dt;
                if(distance<config.cursorNearPixels&&before>distance+12&&speed>config.startleSpeed&&clock-lastStartle>config.startleCooldown){
                    lastStartle=clock;startles.Enqueue(clock);while(startles.Count>0&&clock-startles.Peek()>config.repeatedStartleWindow)startles.Dequeue();
                    if(startles.Count>=config.repeatedStartleCount){Emit("repeatedStartle");startles.Clear();}else Emit("startle");
                }else if(distance<config.cursorNearPixels){
                    dwell+=dt;if(dwell>=config.cursorDwellSeconds&&input.Movement){Emit("dwell");dwell=0;}
                }else dwell=0;
            }
            previousCursor=input.Cursor;hadCursor=true;
            UpdateMotion(dt,input,quiet);
            if(clock>=nextDecision&&clock>=lockedUntil&&!Moving&&(!rules.HasFollowUp(Animation.Current)||Animation.Elapsed>=Animation.Clip.seconds)){
                string followUp=Animation.Elapsed>=Animation.Clip.seconds?rules.FollowUp(Animation.Current,allowMovement):null;
                string independent=followUp==null?rules.TakeIndependent(Context,allowMovement,quiet):null;
                Play(followUp??independent??SelectAutonomousAction(Context,allowMovement),followUp!=null||independent!=null);
                nextDecision=clock+Math.Max(Animation.Clip.seconds,config.decisionSeconds[0]+random.NextDouble()*(config.decisionSeconds[1]-config.decisionSeconds[0]));
            }
        }
        internal string SelectAutonomousAction(string context,bool allowMovement){
            string key=config.contexts.ContainsKey(context)?context:"idle";
            Func<List<Choice>,List<Choice>> eligible=pool=>pool.Where(c=>allowMovement||Animation.Skin.clips[c.clip].motion==null).ToList();
            var choices=eligible(config.contexts["idle"]);
            if(key!="idle"){
                var preferred=eligible(config.contexts[key]);
                if(preferred.Count>0&&(choices.Count==0||random.NextDouble()<config.contextChance))choices=preferred;
            }
            if(allowMovement&&config.ambientChance>0&&random.NextDouble()<config.ambientChance){
                var ambient=eligible(config.ambientChoices);if(ambient.Count>0)choices=ambient;
            }
            int total=choices.Sum(c=>c.weight);if(total<=0)return config.events["idle"];
            int roll=random.Next(total);
            foreach(var choice in choices){roll-=choice.weight;if(roll<0)return choice.clip;}
            return config.events["idle"];
        }
        void UpdateMotion(double dt,InputFrame input,bool quiet){
            Clip clip=Animation.Clip;if(clip.motion==null){target=null;return;}
            if(clip.motion=="leap"){UpdateLeap(input);return;}
            if(quiet&&clip.motion!="cursor"){Play(config.events["idle"],false);return;}
            if(motionClip!=Animation.Current){
                motionClip=Animation.Current;
                double tx=X,ty=Y;
                if(clip.motion=="cursor"){
                    Animation.FaceRight=input.Cursor.X>X+Width*.5;
                    double ax=.20,ay=.73;
                    Clip arrival;if(clip.onArrival!=null&&Animation.Skin.clips.TryGetValue(clip.onArrival,out arrival)&&arrival.contactAnchor!=null){ax=(arrival.contactAnchor[0]+.5)/Animation.Skin.canvas;ay=(arrival.contactAnchor[1]+.5)/Animation.Skin.canvas;}
                    tx=input.Cursor.X-Width*(Animation.FaceRight?1-ax:ax);ty=input.Cursor.Y-Width*ay;
                }
                if(clip.motion=="retreat"){double dx=X+Width*.5-input.Cursor.X,dy=Y+Width*.55-input.Cursor.Y;double len=Math.Max(1,Math.Sqrt(dx*dx+dy*dy));tx+=dx/len*180;ty+=dy/len*100;}
                if(clip.motion=="wander"){if(Destination.HasValue){tx=Destination.Value.X;ty=Destination.Value.Y;}else{tx+=random.Next(-260,261);ty+=random.Next(-90,91);}}
                target=Clamp((float)tx,(float)ty,input.WorkArea,Width);
                Clip landingClip;
                if(clip.motion=="cursor"&&clip.onArrival!=null&&Animation.Skin.clips.TryGetValue(clip.onArrival,out landingClip)&&landingClip.motion=="leap"){
                    arrivalTarget=target;
                    double dx=target.Value.X-X,dy=target.Value.Y-Y,length=Math.Sqrt(dx*dx+dy*dy);
                    double reserve=Math.Min(length,landingClip.leap.distance*PixelScale);
                    if(length>0)target=new PointF((float)(target.Value.X-dx/length*reserve),(float)(target.Value.Y-dy/length*reserve));
                }
            }
            if(!target.HasValue)return;
            PointF end=Clamp(target.Value.X,target.Value.Y,input.WorkArea,Width);target=end;
            double deltaX=end.X-X,deltaY=end.Y-Y,len2=Math.Sqrt(deltaX*deltaX+deltaY*deltaY),step=clip.speed*dt;
            if(len2<=Math.Max(2,step)){
                X=end.X;Y=end.Y;target=null;string arrival=clip.onArrival;
                PointF? landing=arrivalTarget;
                bool external=Destination.HasValue;
                Destination=null;
                Play(arrival??config.events["idle"],arrival!=null);
                if(landing.HasValue&&Animation.Clip.motion=="leap")target=landing;
                if(external&&Arrived!=null){var callback=Arrived;Arrived=null;callback();}
                return;
            }
            if(clip.motion!="cursor")Animation.FaceRight=deltaX>0;
            X+=deltaX/len2*step;Y+=deltaY/len2*step;
        }
        void UpdateLeap(InputFrame input){
            Clip clip=Animation.Clip;
            if(motionClip!=Animation.Current){
                motionClip=Animation.Current;leapStart=new PointF((float)X,(float)Y);
                if(!target.HasValue)target=new PointF((float)(X+(Animation.FaceRight?1:-1)*clip.leap.distance*PixelScale),(float)Y);
                target=Clamp(target.Value.X,target.Value.Y,input.WorkArea,Width);
            }
            if(!target.HasValue)return;
            PointF end=Clamp(target.Value.X,target.Value.Y,input.WorkArea,Width);target=end;
            double takeoff=Animator.FrameStartSeconds(clip,clip.leap.takeoffFrame),landing=Animator.FrameStartSeconds(clip,clip.leap.landingFrame);
            double t=Math.Max(0,Math.Min(1,(Animation.Elapsed-takeoff)/(landing-takeoff)));
            double x=leapStart.X+(end.X-leapStart.X)*t,y=leapStart.Y+(end.Y-leapStart.Y)*t-4*t*(1-t)*clip.leap.height*PixelScale;
            var position=Clamp((float)x,(float)y,input.WorkArea,Width);X=position.X;Y=position.Y;
            if(t>=1)target=null;
        }
        public void KeepInside(Rectangle work){var p=Clamp((float)X,(float)Y,work,Width);X=p.X;Y=p.Y;}
        public static PointF Clamp(float x,float y,Rectangle work,int width){return new PointF(Math.Max(work.Left,Math.Min(work.Right-width,x)),Math.Max(work.Top,Math.Min(work.Bottom-width,y)));}
        public static double Distance(double x,double y,double xx,double yy){return Math.Sqrt((x-xx)*(x-xx)+(y-yy)*(y-yy));}
    }
}
