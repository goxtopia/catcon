using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Catcon {
    public sealed class Animator {
        public Skin Skin {get;private set;} public string Current {get;private set;} public double Elapsed {get;private set;}
        public bool FaceRight {get;set;} public Clip Clip {get{return Skin.clips[Current];}}
        public Animator(Skin skin) {Skin=skin;Current=skin.clips.ContainsKey("sit")?"sit":skin.clips.Keys.First();}
        public void Play(string id) {if(!Skin.clips.ContainsKey(id))throw new ArgumentException("Unknown animation: "+id);Current=id;Elapsed=0;}
        public void Advance(double dt) {Elapsed+=dt;}
        public Bitmap Render() {return RenderAt(Current,Elapsed,FaceRight);}
        public static double TimelineMilliseconds(Clip clip){return clip.frameDurations==null?clip.frameMs*clip.frames.Count:clip.frameDurations.Sum();}
        public static int FrameIndex(Clip clip,double time){
            double total=TimelineMilliseconds(clip),ms=Math.Max(0,time*1000);
            if(clip.playback=="once"&&ms>=total)return clip.frames.Count-1;
            ms%=total;
            if(clip.frameDurations==null)return Math.Min(clip.frames.Count-1,(int)(ms/clip.frameMs));
            for(int i=0;i<clip.frameDurations.Count;i++){if(ms<clip.frameDurations[i])return i;ms-=clip.frameDurations[i];}
            return clip.frames.Count-1;
        }
        public static double FrameStartSeconds(Clip clip,int index){return (clip.frameDurations==null?index*clip.frameMs:clip.frameDurations.Take(index).Sum())/1000.0;}
        public static List<int> ExportTimeline(Skin skin,Clip clip){
            var boundaries=new SortedSet<int>{0};int start=0;
            for(int f=0;f<clip.frames.Count;f++){
                int end=start+(clip.frameDurations==null?clip.frameMs:clip.frameDurations[f]);boundaries.Add(end);
                foreach(var original in skin.poses[clip.pose]){
                    var layer=original.Copy();LayerPatch patch;if(clip.frames[f].TryGetValue(layer.id,out patch))layer.Apply(patch);
                    if(layer.visible&&layer.spriteCycle!=null)for(int tick=(start/layer.cycleMs+1)*layer.cycleMs;tick<end;tick+=layer.cycleMs)boundaries.Add(tick);
                }
                start=end;
            }
            return boundaries.ToList();
        }
        public Bitmap RenderAt(string id,double time,bool mirror) {
            Clip clip=Skin.clips[id];
            int frame=FrameIndex(clip,time);
            var layers=Skin.poses[clip.pose].Select(l=>l.Copy()).ToList();
            foreach(var l in layers) {LayerPatch patch;if(clip.frames[frame].TryGetValue(l.id,out patch))l.Apply(patch);}
            foreach(var l in layers)if(l.spriteCycle!=null)l.sprite=l.spriteCycle[(int)(Math.Max(0,time)*1000/l.cycleMs%l.spriteCycle.Length)];
            bool spinMirror=false;
            if(clip.deformers!=null)foreach(var d in clip.deformers) {
                double phase=time/d.period*Math.PI*2;
                if(d.type=="spin") {spinMirror=Math.Cos(phase)<0;foreach(var l in layers){l.x+=Math.Round(Math.Sin(phase)*3);l.y+=Math.Round(Math.Cos(phase)*1.5);}continue;}
                foreach(var l in layers.Where(l=>d.parts.Contains("*")||d.parts.Contains(l.id))) {
                    if(d.type=="breathe"||d.type=="float")l.y+=Math.Round(Math.Sin(phase)*d.amplitude);
                    if(d.type=="shake")l.x+=Math.Round(Math.Sin(phase)*d.amplitude);
                    if(d.type=="hop")l.y-=Math.Round(Math.Max(0,Math.Sin(Math.Min(1,time/clip.seconds)*Math.PI))*d.amplitude);
                }
            }
            var output=new Bitmap(Skin.canvas,Skin.canvas,PixelFormat.Format32bppArgb);
            using(Graphics g=Graphics.FromImage(output)) {
                g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.SmoothingMode=SmoothingMode.None;
                foreach(var l in layers.OrderBy(l=>l.z)) {
                    if(!l.visible)continue;
                    Bitmap b=Skin.Images[l.sprite];var state=g.Save();
                    if(mirror^spinMirror){
                        if(l.id=="effect")l.x=Skin.canvas-l.x-b.Width*l.sx;
                        else{g.TranslateTransform(Skin.canvas,0);g.ScaleTransform(-1,1);}
                    }
                    if(l.rotation==0&&l.sx==1&&l.sy==1)g.DrawImageUnscaled(b,(int)Math.Round(l.x),(int)Math.Round(l.y));
                    else{
                        g.TranslateTransform((float)(l.x+b.Width*l.sx/2),(float)(l.y+b.Height*l.sy/2));
                        g.RotateTransform((float)l.rotation);g.ScaleTransform((float)l.sx,(float)l.sy);
                        g.DrawImage(b,new RectangleF(-b.Width/2f,-b.Height/2f,b.Width,b.Height),new RectangleF(0,0,b.Width,b.Height),GraphicsUnit.Pixel);
                    }
                    g.Restore(state);
                }
            }
            return output;
        }
        public static Bitmap Scale(Bitmap source,int factor) {
            var output=new Bitmap(source.Width*factor,source.Height*factor,PixelFormat.Format32bppArgb);
            using(Graphics g=Graphics.FromImage(output)){g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(source,new Rectangle(0,0,output.Width,output.Height),0,0,source.Width,source.Height,GraphicsUnit.Pixel);}return output;
        }
    }
}
