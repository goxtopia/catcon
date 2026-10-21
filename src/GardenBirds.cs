using System;

namespace Catcon {
    /// <summary>停在栅栏上的小鸟在某一时刻的状态。</summary>
    public struct BirdFrame {
        public bool visible;
        public int x,y,variant;
        public bool flip;
        /// <summary>0 = 站在杆上，1 = 蹦跳中（离杆一两像素），2 = 飞行。</summary>
        public int pose;
        public bool wingUp;
    }

    /// <summary>小鸟的作息：在杆上待多久、飞走还是跳走、离开多久。</summary>
    public sealed class GardenBirdConfig {
        public double[] perchSeconds=new double[]{10,24};
        public double[] awaySeconds=new double[]{5,12};
        public double flyChance=.42;
        public double hopChance=.38;
        public double flySeconds=1.3;
        public double hopSeconds=2.4;
        public double hopDistance=9;
        public void Validate(){
            Skin.Require(perchSeconds!=null&&perchSeconds.Length==2&&perchSeconds[0]>1&&perchSeconds[1]>=perchSeconds[0],"Invalid bird perch timing");
            Skin.Require(awaySeconds!=null&&awaySeconds.Length==2&&awaySeconds[0]>0&&awaySeconds[1]>=awaySeconds[0],"Invalid bird away timing");
            Skin.Require(flyChance>=0&&flyChance<=1&&hopChance>=0&&hopChance<=1&&flyChance+hopChance<=1,"Bird flight and hop chances must add up to at most 1");
            Skin.Require(flySeconds>.4&&flySeconds<=4&&hopSeconds>.3&&hopSeconds<=8,"Invalid bird flight or hop duration");
            Skin.Require(hopDistance>=2&&hopDistance<=24,"Invalid bird hop distance");
        }
    }

    /// <summary>
    /// 小鸟的全部作息都用「种子 + 时间」算出来，不存状态：同一只鸟在同一时刻永远得到同一帧，
    /// 离线补算、拖动窗口、改缩放都不会让画面里的鸟跳位。
    /// </summary>
    public static class BirdSchedule {
        /// <summary>习性：0 = 抛硬币决定，1 = 总是飞走，2 = 总是在杆上蹦，3 = 一直待着。</summary>
        public static BirdFrame At(int seed,int perchX,int perchY,bool flip,int variant,int habit,GardenBirdConfig config,double time){
            var frame=new BirdFrame{visible=true,x=perchX,y=perchY,flip=flip,variant=variant,pose=0};
            if(config==null)return frame;
            if(time<0)time=0;
            double perch=config.perchSeconds[0]+Roll(seed,1)*(config.perchSeconds[1]-config.perchSeconds[0]);
            double route=Roll(seed,2);
            bool flyer=habit==1||(habit==0&&route<config.flyChance);
            bool hopper=habit==2||(habit==0&&!flyer&&route<config.flyChance+config.hopChance);
            frame.wingUp=((int)(time*8+seed%5))%2==0;
            if(hopper)return Hop(frame,seed,perchX,perchY,perch,time,config);
            if(flyer)return Fly(frame,seed,perchX,perchY,perch,time,config);
            return frame;
        }
        static BirdFrame Hop(BirdFrame frame,int seed,int perchX,int perchY,double perch,double time,GardenBirdConfig config){
            // 往哪边蹦跟着朝向走：面朝左的鸟不会往右蹦。
            int dir=frame.flip?-1:1;
            int distance=(int)Math.Round(config.hopDistance*(.6+Roll(seed,4)*.8));
            double cycle=perch+config.hopSeconds;
            long index=(long)Math.Floor(time/cycle);
            double local=time-index*cycle;
            // 每个周期在「原位」和「隔壁」之间来回，所以看起来是一直在杆上蹦着换地方。
            int from=index%2==0?0:dir*distance;
            int to=index%2==0?dir*distance:0;
            if(local<perch){frame.x=perchX+from;return frame;}
            double progress=Math.Min(1,(local-perch)/config.hopSeconds);
            const int steps=3;
            double scaled=progress*steps;
            int hop=(int)Math.Floor(scaled);
            double lift=scaled-hop;
            frame.x=perchX+(int)Math.Round(from+(to-from)*(hop+lift)/steps);
            frame.y=perchY-(int)Math.Round(Math.Sin(lift*Math.PI)*2);
            frame.pose=frame.y<perchY?1:0;
            return frame;
        }
        static BirdFrame Fly(BirdFrame frame,int seed,int perchX,int perchY,double perch,double time,GardenBirdConfig config){
            // 起飞方向也跟随朝向，否则会出现「面朝右却从左边飞走」的别扭画面。
            int dir=frame.flip?-1:1;
            double away=config.flySeconds*2+config.awaySeconds[0]+Roll(seed,5)*(config.awaySeconds[1]-config.awaySeconds[0]);
            double cycle=perch+away;
            long index=(long)Math.Floor(time/cycle);
            double local=time-index*cycle;
            if(local<perch)return frame;
            double phase=local-perch;
            double span=config.flySeconds;
            if(phase<span)return Airborne(frame,perchX,perchY,dir,phase/span);
            if(phase<span+away-2*span){frame.visible=false;return frame;}
            return Airborne(frame,perchX,perchY,dir,1-Math.Min(1,(phase-(span+away-2*span))/span));
        }
        /// <summary>起飞与降落共用一条抛物线：0 表示停在杆上，1 表示已经飞出画面。
        /// 横向用三次方、纵向用减速曲线，所以是先几乎垂直窜起来，再加速斜着出去，
        /// 不会贴着栏杆横向滑过旁边那几只鸟。</summary>
        static BirdFrame Airborne(BirdFrame frame,int perchX,int perchY,int dir,double progress){
            double rise=1-(1-progress)*(1-progress);
            double drift=progress*progress*progress;
            frame.pose=2;
            frame.x=perchX+(int)Math.Round(dir*58*drift);
            frame.y=perchY-(int)Math.Round((perchY+14)*rise);
            frame.visible=true;
            return frame;
        }
        static double Roll(int seed,int salt){
            unchecked{
                int h=seed*374761393+salt*668265263;
                h=(h^(h>>13))*1274126177;
                h^=h>>16;
                return (h&0x7fffffff)/(double)0x7fffffff;
            }
        }
    }
}
