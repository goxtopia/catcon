using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

namespace Catcon {
    /// <summary>把花园画成透明的 PNG 检查图，便于美术复核与文档配图。</summary>
    static class GardenExport {
        public static void Run(string root,string destination){
            GardenConfig config=GardenConfig.Load(Path.Combine(root,"config","garden.json"));
            Directory.CreateDirectory(destination);
            using(var renderer=new GardenRenderer(config)){
                var full=Scene(config,Full);
                renderer.Render(full);Save(renderer.Frame(4),Path.Combine(destination,"garden.png"));
                var wind=Scene(config,Full);
                AddPetals(wind,config);
                renderer.Render(wind);Save(renderer.Frame(4),Path.Combine(destination,"garden-wind.png"));
                var thirsty=Scene(config,Seedlings);
                thirsty.State.moisture=.04;
                renderer.Render(thirsty);Save(renderer.Frame(4),Path.Combine(destination,"garden-thirsty.png"));
                // 刚撒下一包种子：土粒扬起、嫩芽冒光点、槽位上还留着一圈光环。
                // 固定种在床中央那个空槽（5 号，50,63），避开洒水壶和玻璃罩，
                // 位置可预期又能看清反馈；只推进 0.1 秒，粒子还聚在落点附近。
                var sowing=Scene(config,delegate(List<PlantSeed> list){
                    list.Add(new PlantSeed("catnip",0,.3));
                });
                sowing.Plant("poppy",5);
                for(int i=0;i<2;i++)sowing.Advance(.05);
                renderer.Render(sowing);
                Save(renderer.Frame(4),Path.Combine(destination,"garden-sowing.png"));
                // 整园图里这个动作只有十来像素，再出一张 12 倍的局部图给文档和美术复核。
                using(var zoom=renderer.Region(38,42,30,32,12)){
                    Save(zoom,Path.Combine(destination,"garden-sowing-zoom.png"));
                }
                // 五只鸟排队站在杆上的样子，放大一张，方便一眼看出体形与朝向的差别。
                var lineup=Scene(config,Full);
                renderer.Render(lineup);
                using(var rail=renderer.Region(6,15,78,16,13)){
                    Save(rail,Path.Combine(destination,"garden-birds-rail.png"));
                }
                // 小鸟飞出栏杆的那一刻：整园一张，再出一张局部放大图。
                var flock=Scene(config,Full);
                double flightAt=-1;var airborneBird=new BirdFrame();
                foreach(var bird in config.decorations.Where(d=>d.type=="bird")){
                    for(double t=0;t<200&&flightAt<0;t+=.25){
                        BirdFrame shot=BirdSchedule.At(bird.seed,bird.x,bird.y,bird.flip,bird.variantIndex,bird.habitCode,config.birds,t);
                        if(!shot.visible||shot.pose!=2)continue;
                        if(shot.x<14||shot.x>config.canvas.width-14||shot.y<6||shot.y>config.fence.top)continue;
                        flightAt=t;airborneBird=shot;
                    }
                    if(flightAt>0)break;
                }
                if(flightAt>0){
                    for(int i=0;i<(int)Math.Round(flightAt/.25);i++)flock.Advance(.25);
                    renderer.Render(flock);
                    Save(renderer.Frame(4),Path.Combine(destination,"garden-birds.png"));
                    using(var zoom=renderer.Region(airborneBird.x-12,Math.Max(0,airborneBird.y-10),24,20,14)){
                        Save(zoom,Path.Combine(destination,"garden-birds-zoom.png"));
                    }
                }
                foreach(var species in config.species){
                    var stages=Scene(config,delegate(List<PlantSeed> list){
                        double[] steps=new double[]{0,0,.12,.34,.56,.78,1};
                        for(int i=0;i<steps.Length;i++)if(i<config.slots.Count)list.Add(new PlantSeed(species.id,i,steps[i]));
                    });
                    renderer.Render(stages);
                    Save(renderer.Frame(4),Path.Combine(destination,"garden-stages-"+species.id+".png"));
                }
            }
        }
        sealed class PlantSeed {
            public readonly string species;public readonly int slot;public readonly double growth;
            public PlantSeed(string species,int slot,double growth){this.species=species;this.slot=slot;this.growth=growth;}
        }
        static GardenEngine Scene(GardenConfig config,Action<List<PlantSeed>> fill){
            var state=new GardenState();
            var list=new List<PlantSeed>();fill(list);
            foreach(var seed in list)state.plants.Add(new GardenPlant(seed.species,seed.slot,seed.growth,state.nextId++));
            return new GardenEngine(config,state,7);
        }
        static void Full(List<PlantSeed> list){
            double[] growth={1,.9,1,.72,.55,1,.35,1,.2,1,.8,1};
            string[] order={"poppy","lavender","catnip","freesia","rosemary","daisy"};
            for(int i=0;i<12;i++)list.Add(new PlantSeed(order[i%order.Length],i,growth[i]));
        }
        static void Seedlings(List<PlantSeed> list){
            list.Add(new PlantSeed("catnip",0,.3));
            list.Add(new PlantSeed("daisy",4,.2));
            list.Add(new PlantSeed("poppy",9,.42));
        }
        static void AddPetals(GardenEngine engine,GardenConfig config){
            var colors=new[]{"#e2604c","#b6a4e8","#f3d478","#a68dd9","#fbf6e8"};
            var random=new Random(3);
            // 现在起风是"一层薄花瓣"，导出图也按同样的密度摆，别把文档画成比以前还满。
            for(int i=0;i<24;i++)engine.Petals.Add(new GardenPetal{
                x=12+random.NextDouble()*88,y=20+random.NextDouble()*52,
                vx=0,vy=4,life=2.5,maxLife=5,phase=random.NextDouble()*6.28,
                spin=.5+random.NextDouble()*1.6,flutter=.5+random.NextDouble()*1.1,
                size=random.NextDouble()<.26?2:1,kind=0,
                color=ColorTranslator.FromHtml(colors[random.Next(colors.Length)]).ToArgb()});
        }
        static void Save(Bitmap bitmap,string path){
            bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);
        }
    }
}
