using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Catcon {
    sealed class PixelSurface : IDisposable {
        public readonly int Width,Height;public readonly int[] Pixels;public readonly Bitmap Bitmap;
        // 画植株时记录剪影，画完在外圈补描边。
        public readonly bool[] Mask;
        public bool Capture;
        public PixelSurface(int width,int height){
            Width=width;Height=height;Pixels=new int[width*height];Mask=new bool[width*height];
            Bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb);
        }
        public void Clear(){Array.Clear(Pixels,0,Pixels.Length);}
        public int Get(int x,int y){return x<0||y<0||x>=Width||y>=Height?0:Pixels[y*Width+x];}
        public void Set(int x,int y,int argb){
            if(x<0||y<0||x>=Width||y>=Height||(argb>>24)==0)return;
            Pixels[y*Width+x]=argb;
            if(Capture)Mask[y*Width+x]=true;
        }
        public void SetDirect(int x,int y,int argb){
            if(x<0||y<0||x>=Width||y>=Height)return;
            Pixels[y*Width+x]=argb;
        }
        public void Blend(int x,int y,int argb,double alpha){
            if(alpha<=0||x<0||y<0||x>=Width||y>=Height)return;
            if(alpha>1)alpha=1;
            int under=Pixels[y*Width+x];
            double sa=((argb>>24)&255)/255.0*alpha,ua=((under>>24)&255)/255.0*(1-sa),outA=sa+ua;
            if(outA<=0){Pixels[y*Width+x]=0;return;}
            int r=(int)((((argb>>16)&255)*sa+((under>>16)&255)*ua)/outA);
            int g=(int)((((argb>>8)&255)*sa+((under>>8)&255)*ua)/outA);
            int b=(int)(((argb&255)*sa+(under&255)*ua)/outA);
            Pixels[y*Width+x]=(int)(outA*255)<<24|r<<16|g<<8|b;
        }
        public void Fill(int x0,int y0,int x1,int y1,int argb){
            if(x1<x0){int t=x0;x0=x1;x1=t;}if(y1<y0){int t=y0;y0=y1;y1=t;}
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)Set(x,y,argb);
        }
        public void HLine(int x0,int x1,int y,int argb){if(x1<x0){int t=x0;x0=x1;x1=t;}for(int x=x0;x<=x1;x++)Set(x,y,argb);}
        public void VLine(int x,int y0,int y1,int argb){if(y1<y0){int t=y0;y0=y1;y1=t;}for(int y=y0;y<=y1;y++)Set(x,y,argb);}
        public void Disc(int cx,int cy,int radius,int argb){
            if(radius<1){Set(cx,cy,argb);return;}
            for(int dy=-radius;dy<=radius;dy++)for(int dx=-radius;dx<=radius;dx++)
                if(dx*dx+dy*dy<=radius*radius+radius)Set(cx+dx,cy+dy,argb);
        }
        public void Commit(){
            BitmapData data=Bitmap.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try{Marshal.Copy(Pixels,0,data.Scan0,Pixels.Length);}finally{Bitmap.UnlockBits(data);}
        }
        public void Dispose(){Bitmap.Dispose();}
    }

    sealed class SpeciesArt {
        public string form;public int height,stemWidth,leafPairs,leafWidth,size,count,stems;
        public double leafRise;
        public int stem,leaf,leafDark,flower,flower2,core,dried;
        public bool flowering;
        public Color flowerColor;
    }

    public sealed class GardenRenderer : IDisposable {
        readonly GardenConfig config;
        readonly PixelSurface surface;
        readonly Dictionary<string,int> colors=new Dictionary<string,int>();
        readonly Dictionary<string,SpeciesArt> arts=new Dictionary<string,SpeciesArt>();
        readonly Dictionary<int,Bitmap> frames=new Dictionary<int,Bitmap>();
        readonly int outline,soil,soilWet,soilDry,soilDark,soilLight,grass,grassLight,grassDark;
        readonly int wood,woodLight,woodDark,stone,stoneDark,metal,metalLight,metalDark,glow,water,waterLight;
        readonly int mushroomCap,mushroomStem,butterfly,butterflyWing;
        readonly int glass,glassLight,ceramic,paintA,paintB,gold;
        readonly int birdColor,birdLight,birdWing,beakColor;
        static readonly string[] BackDecorations=new[]{"lantern","fairyLights"};

        public GardenRenderer(GardenConfig config){
            this.config=config;
            surface=new PixelSurface(config.canvas.width,config.canvas.height);
            foreach(var pair in config.colors)colors[pair.Key]=ColorTranslator.FromHtml(pair.Value).ToArgb();
            outline=colors["outline"];soil=colors["soil"];soilWet=colors["soilWet"];soilDry=colors["soilDry"];
            soilDark=colors["soilDark"];soilLight=colors["soilLight"];
            grass=colors["grass"];grassLight=colors["grassLight"];grassDark=colors["grassDark"];
            wood=colors["wood"];woodLight=colors["woodLight"];woodDark=colors["woodDark"];
            stone=colors["stone"];stoneDark=colors["stoneDark"];
            metal=colors["metal"];metalLight=colors["metalLight"];metalDark=colors["metalDark"];
            glow=colors["glow"];water=colors["water"];waterLight=colors["waterLight"];
            mushroomCap=colors["mushroomCap"];mushroomStem=colors["mushroomStem"];
            butterfly=colors["butterfly"];butterflyWing=colors["butterflyWing"];
            glass=colors["glass"];glassLight=colors["glassLight"];ceramic=colors["ceramic"];
            paintA=colors["paintA"];paintB=colors["paintB"];gold=colors["gold"];
            birdColor=colors["bird"];birdLight=colors["birdLight"];birdWing=colors["birdWing"];beakColor=colors["beak"];
            foreach(var species in config.species)arts[species.id]=Prepare(species);
        }
        static SpeciesArt Prepare(GardenSpecies species){
            var art=new SpeciesArt();
            art.form=species.flower.form;art.height=species.height;art.stemWidth=species.stemWidth;
            art.leafPairs=species.leafPairs;art.leafWidth=species.leafWidth;art.leafRise=species.leafRise;
            art.stems=species.stems;
            art.size=species.flower.size;art.count=species.flower.count;
            art.stem=ColorTranslator.FromHtml(species.stemColor).ToArgb();
            art.leaf=ColorTranslator.FromHtml(species.leafColor).ToArgb();
            art.leafDark=ColorTranslator.FromHtml(species.leafDark).ToArgb();
            art.dried=Mix(art.leaf,ColorTranslator.FromHtml("#8a7448").ToArgb(),.75);
            art.flowering=species.Flowering;
            if(art.flowering){
                art.flower=ColorTranslator.FromHtml(species.flower.color).ToArgb();
                art.flower2=ColorTranslator.FromHtml(species.flower.color2).ToArgb();
                art.core=ColorTranslator.FromHtml(species.flower.core).ToArgb();
                art.flowerColor=ColorTranslator.FromHtml(species.flower.color);
            }else{art.flower=art.leaf;art.flower2=art.leafDark;art.core=art.leafDark;art.flowerColor=ColorTranslator.FromHtml(species.leafColor);}
            return art;
        }
        public int Width{get{return surface.Width;}}
        public int Height{get{return surface.Height;}}
        public bool Opaque(int x,int y){return ((surface.Get(x,y)>>24)&255)>24;}
        /// <summary>逐像素读取最近一次渲染结果，给回归测试比对前后差异用。</summary>
        public int PixelAt(int x,int y){return surface.Get(x,y);}
        static int Mix(int a,int b,double t){
            if(t<=0)return a;if(t>=1)return b;
            double aa=((a>>24)&255)/255.0,ba=((b>>24)&255)/255.0;
            double inv=1-t;
            int alpha=(int)Math.Round((aa*inv+ba*t)*255);
            int r=(int)Math.Round(((a>>16)&255)*inv+((b>>16)&255)*t);
            int g=(int)Math.Round(((a>>8)&255)*inv+((b>>8)&255)*t);
            int bl=(int)Math.Round((a&255)*inv+(b&255)*t);
            return alpha<<24|r<<16|g<<8|bl;
        }
        static int Shade(int argb,double factor){
            int r=(int)Math.Min(255,((argb>>16)&255)*factor),g=(int)Math.Min(255,((argb>>8)&255)*factor),b=(int)Math.Min(255,(argb&255)*factor);
            return ((argb>>24)&255)<<24|r<<16|g<<8|b;
        }
        static int Hash(int x,int y){
            unchecked{int h=x*374761393+y*668265263;h=(h^(h>>13))*1274126177;return h^(h>>16);}
        }

        public void Render(GardenEngine engine){
            surface.Clear();
            DrawSkyBirds(engine.Time);
            DrawFence();
            DrawGrass();
            DrawBed(engine.Moisture);
            DrawBackDecorations(engine);
            DrawPlants(engine);
            DrawSowFeedback(engine);
            DrawDecorations(engine);
            DrawPetals(engine);
            DrawWatering(engine);
            DrawBadge(engine);
            surface.Commit();
        }
        public Bitmap Frame(double scale){
            if(scale<=0)scale=1;
            int key=(int)Math.Round(scale*100);
            int dw=Math.Max(1,(int)Math.Round(surface.Width*scale)),dh=Math.Max(1,(int)Math.Round(surface.Height*scale));
            Bitmap cached;
            if(!frames.TryGetValue(key,out cached)||cached.Width!=dw||cached.Height!=dh){
                if(cached!=null)cached.Dispose();
                cached=new Bitmap(dw,dh,PixelFormat.Format32bppArgb);
                frames[key]=cached;
            }
            // 每次都重新贴图：位图只是缓冲区，内容跟着最近一次 Render 走。
            // 最近邻放大，边界按目标尺寸取整，1.5 / 2.5 倍也不会糊。
            BitmapData data=cached.LockBits(new Rectangle(0,0,dw,dh),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try{
                int[] row=new int[dw];
                for(int y=0;y<dh;y++){
                    int sy=Math.Min(surface.Height-1,(int)((y+.5)*surface.Height/dh));
                    int offset=sy*surface.Width;
                    for(int x=0;x<dw;x++)row[x]=surface.Pixels[offset+Math.Min(surface.Width-1,(int)((x+.5)*surface.Width/dw))];
                    Marshal.Copy(row,0,data.Scan0+y*data.Stride,dw);
                }
            }finally{cached.UnlockBits(data);}
            return cached;
        }
        /// <summary>把最近一次渲染结果的一块区域按整数倍放大导出，给美术复核用的局部细节图。</summary>
        public Bitmap Region(int x,int y,int w,int h,int scale){
            if(scale<=0)scale=1;
            int cw=Math.Max(1,Math.Min(w,surface.Width-x)),ch=Math.Max(1,Math.Min(h,surface.Height-y));
            var bmp=new Bitmap(cw*scale,ch*scale,PixelFormat.Format32bppArgb);
            BitmapData data=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try{
                int[] row=new int[bmp.Width];
                for(int sy=0;sy<ch;sy++){
                    int offset=(y+sy)*surface.Width+x;
                    for(int sx=0;sx<cw;sx++){
                        int pixel=surface.Pixels[offset+sx];
                        for(int k=0;k<scale;k++)row[sx*scale+k]=pixel;
                    }
                    for(int k=0;k<scale;k++)Marshal.Copy(row,0,data.Scan0+(sy*scale+k)*data.Stride,bmp.Width);
                }
            }finally{bmp.UnlockBits(data);}
            return bmp;
        }
        public void Dispose(){
            foreach(var frame in frames.Values)frame.Dispose();
            frames.Clear();surface.Dispose();
        }

        void DrawFence(){
            int top=config.fence.top,bottom=config.bed.top+1;
            int left=config.grass.left,right=config.grass.right;
            Rail(top+4,left,right);Rail(top+10,left,right);
            for(int x=left+3;x<right;x+=config.fence.spacing){
                surface.Fill(x,top+1,x+3,bottom,wood);
                surface.VLine(x,top+2,bottom,woodLight);
                surface.VLine(x+1,top+1,bottom,Mix(wood,woodLight,.4));
                surface.VLine(x+3,top+1,bottom,woodDark);
                surface.HLine(x,x+3,top+1,woodLight);
                surface.Set(x+2,top+1,Mix(wood,woodDark,.45));
                surface.Set(x+1,top,wood);surface.Set(x+2,top,woodDark);
                for(int y=top+5;y<bottom;y+=6){
                    surface.Set(x+2,y,Mix(wood,woodDark,.5));
                    surface.Set(x+1,y+2,Mix(wood,woodLight,.35));
                }
                surface.Blend(x+4,bottom,Mix(wood,outline,.55),.45);
            }
        }
        void Rail(int y,int left,int right){
            surface.Fill(left,y,right,y+1,wood);
            surface.HLine(left,right,y,woodLight);
            surface.HLine(left,right,y+1,woodDark);
            for(int x=left+3;x<right;x+=7)surface.Set(x,y+1,Mix(wood,woodDark,.4));
            surface.Set(left,y,woodDark);surface.Set(right,y,woodDark);
        }
        int Blade(int x){return 1+(Math.Abs(Hash(x,7))%3);}
        void DrawGrass(){
            int top=config.grass.top,bottom=config.bed.top,left=config.grass.left,right=config.grass.right;
            for(int x=left;x<=right;x++){
                int blade=Blade(x),start=top+blade;
                surface.VLine(x,start,bottom,grass);
                surface.Set(x,start,(x%5==0)?grassLight:(Math.Abs(Hash(x,31))%7==0?grassDark:grass));
                if(blade>1)surface.Set(x,start-1,(Math.Abs(Hash(x,11))%3==0)?grassLight:grassDark);
                surface.Set(x,bottom-1,grassDark);
                if(Math.Abs(Hash(x,53))%9==0)surface.Set(x,bottom-2,Mix(grass,grassDark,.55));
            }
            for(int x=left+2;x<right;x+=9){
                surface.VLine(x,top-1,top+2,grassLight);
                surface.Set(x-1,top+1,grass);
                surface.Set(x+1,top+2,grassDark);
                if(x+2<=right)surface.Set(x+2,top,grassLight);
            }
            // 零星小野花，让草地不那么平
            for(int x=left+5;x<right;x+=17){
                int y=top+Blade(x)+1;
                surface.Set(x,y,Mix(ceramic,stone,.2));
                surface.Set(x,y-1,(Math.Abs(Hash(x,3))%2==0)?paintA:glassLight);
                surface.Set(x+1,y,Mix(grass,grassDark,.7));
            }
        }
        void DrawBed(double moisture){
            var b=config.bed;
            int il=b.left+b.inset,it=b.top+b.inset,ir=b.right-b.inset,ib=b.bottom-b.inset;
            // 木箱外框：顶部受光、右侧收暗
            surface.Fill(b.left,b.top,b.right,b.bottom,wood);
            surface.HLine(b.left,b.right,b.top,woodLight);
            surface.HLine(b.left,b.right,b.top+1,Mix(wood,woodLight,.55));
            surface.VLine(b.left,b.top,b.bottom,Mix(wood,woodLight,.5));
            surface.VLine(b.right,b.top,b.bottom,woodDark);
            surface.HLine(b.left,b.right,b.bottom,woodDark);
            surface.HLine(b.left,b.right,b.bottom-1,Mix(wood,woodDark,.45));
            for(int x=b.left+8;x<b.right-1;x+=11){
                surface.VLine(x,b.top+1,b.top+b.inset-1,woodDark);
                surface.VLine(x,b.bottom-b.inset+1,b.bottom-1,woodDark);
                surface.Set(x+1,b.top+2,Mix(wood,woodLight,.4));
                surface.Set(x+1,b.bottom-2,Mix(wood,woodDark,.3));
            }
            for(int y=b.top+6;y<b.bottom-2;y+=7){
                surface.Set(b.left+1,y,woodDark);surface.Set(b.left+2,y,Mix(wood,woodDark,.5));
                surface.Set(b.right-1,y+3,woodDark);surface.Set(b.right-2,y+3,Mix(wood,woodDark,.5));
            }
            int nail=Mix(wood,outline,.62);
            surface.Set(b.left+1,b.top+1,nail);surface.Set(b.right-1,b.top+1,nail);
            surface.Set(b.left+1,b.bottom-1,nail);surface.Set(b.right-1,b.bottom-1,nail);
            // 土壤
            int baseColor=Mix(soilDry,soilWet,Math.Max(0,Math.Min(1,moisture)));
            surface.Fill(il,it,ir,ib,baseColor);
            for(int y=it;y<=ib;y++){
                double depth=(double)(y-it)/Math.Max(1,ib-it);
                for(int x=il;x<=ir;x++){
                    int h=Math.Abs(Hash(x,y))%1024;
                    int c=baseColor;
                    if(h<40)c=Mix(baseColor,soilDark,.9);
                    else if(h<92)c=Mix(baseColor,soilDark,.4);
                    else if(h>1016)c=Mix(baseColor,soilLight,.45);
                    else if(h>962)c=Mix(baseColor,soilLight,.2);
                    if(depth>.45&&h%5==0)c=Mix(c,soilDark,.2);
                    int edge=Math.Min(Math.Min(x-il,ir-x),Math.Min(y-it,ib-y));
                    if(edge==0)c=Mix(c,soilDark,.6);
                    else if(edge==1&&((x+y)&1)==0)c=Mix(c,soilDark,.3);
                    surface.Set(x,y,c);
                }
            }
            // 翻过的畦沟
            for(int y=it+4;y<=ib-1;y+=5){
                for(int x=il+1;x<ir;x++){
                    surface.Set(x,y,Mix(baseColor,soilDark,.34));
                    if(((x+y)&3)==0)surface.Set(x,y-1,Mix(baseColor,soilLight,.3));
                }
            }
            // 贴着木框的内沿受光，显得土是陷下去的
            for(int x=il+1;x<ir;x++)if((x&1)==0)surface.Set(x,it+1,Mix(baseColor,soilLight,.24));
            // 湿土的水痕与反光
            double wet=Math.Max(0,Math.Min(1,(moisture-config.moisture.thirstyBelow)/Math.Max(.001,1-config.moisture.thirstyBelow)));
            if(wet>0){
                for(int i=0;i<7;i++){
                    int x=il+2+(int)(Math.Abs(Hash(i,71))%Math.Max(1,ir-il-3));
                    int y=it+2+(int)(Math.Abs(Hash(i,97))%Math.Max(1,ib-it-3));
                    surface.Blend(x,y,Mix(baseColor,water,.35),.3*wet);
                    surface.Blend(x+1,y+1,Mix(baseColor,water,.3),.22*wet);
                }
                for(int i=0;i<5;i++){
                    int x=il+3+(int)(Math.Abs(Hash(i,131))%Math.Max(1,ir-il-5));
                    int y=it+3+(int)(Math.Abs(Hash(i,157))%Math.Max(1,ib-it-5));
                    surface.Blend(x,y,waterLight,.3*wet);
                }
            }
            // 干透时的裂纹
            if(moisture<config.moisture.dryBelow){
                for(int i=0;i<4;i++){
                    int x=il+4+(int)(Math.Abs(Hash(i,211))%Math.Max(1,ir-il-8));
                    int y=it+3+(int)(Math.Abs(Hash(i,223))%Math.Max(1,ib-it-6));
                    for(int k=0;k<5;k++){
                        surface.Set(x+k,y+((k&1)==0?0:1),Mix(baseColor,soilDark,.8));
                        if((k&1)==0)surface.Set(x+k,y-1,Mix(baseColor,soilLight,.3));
                    }
                }
            }
            // 土里的小石子
            surface.Set(il+18,ib-4,stoneDark);surface.Set(il+19,ib-4,Mix(stoneDark,stone,.6));
            surface.Set(ir-26,it+6,Mix(stoneDark,stone,.45));surface.Set(ir-27,it+6,stoneDark);
            surface.Set(il+41,ib-7,Mix(stone,stoneDark,.55));
        }
        void DrawBackDecorations(GardenEngine engine){
            foreach(var d in config.decorations){
                if(d.type=="lantern")DrawLantern(d.x,d.y);
                else if(d.type=="fairyLights")DrawFairyLights(d.x,d.y,engine.Time);
            }
        }
        void DrawLantern(int x,int y){
            surface.Blend(x,y-1,metalDark,.9);surface.Blend(x,y+1,metalDark,.9);
            surface.HLine(x-2,x+2,y+2,metalDark);
            surface.Fill(x-2,y+3,x+2,y+8,metal);
            surface.VLine(x-2,y+3,y+8,metalLight);
            surface.VLine(x+2,y+3,y+8,metalDark);
            surface.Fill(x-1,y+4,x+1,y+7,glow);
            surface.Set(x,y+5,waterLight);surface.Set(x,y+6,Shift(glow,18));
            surface.HLine(x-2,x+2,y+9,metalDark);
            surface.Set(x-1,y+10,metalDark);surface.Set(x+1,y+10,metalDark);
            for(int dy=-4;dy<=5;dy++)for(int dx=-5;dx<=5;dx++){
                int d=Math.Abs(dx)+Math.Abs(dy);
                if(d>2&&d<=6)surface.Blend(x+dx,y+5+dy,glow,.075);
            }
        }
        void DrawDecorations(GardenEngine engine){
            foreach(var d in config.decorations){
                if(Array.IndexOf(BackDecorations,d.type)>=0)continue;
                if(d.type=="sign")DrawSign(d.x,d.y);
                else if(d.type=="mushrooms")DrawMushrooms(d.x,d.y);
                else if(d.type=="stones")DrawStones(d.x,d.y);
                else if(d.type=="can")DrawCan(d.x,d.y,d.flip);
                else if(d.type=="pinwheel")DrawPinwheel(d.x,d.y,engine.Time);
                else if(d.type=="gnome")DrawGnome(d.x,d.y);
                else if(d.type=="duck")DrawDuck(d.x,d.y);
                else if(d.type=="cloche")DrawCloche(d.x,d.y);
                else if(d.type=="gazing")DrawGazing(d.x,d.y);
                else if(d.type=="baubles")DrawBaubles(d.x,d.y);
                else if(d.type=="bird")DrawBird(d.x,d.y,d.flip,engine.Time);
            }
        }
        void DrawSign(int x,int y){
            surface.VLine(x,y-9,y,woodDark);
            surface.Set(x-1,y-6,wood);surface.Set(x+1,y-5,wood);
            int bx=x-5,by=y-16;
            surface.Fill(bx,by,bx+10,by+6,woodLight);
            surface.HLine(bx,bx+10,by,wood);
            surface.HLine(bx,bx+10,by+6,woodDark);
            surface.VLine(bx,by,by+6,wood);
            surface.VLine(bx+10,by,by+6,woodDark);
            surface.Set(bx+2,by+2,mushroomCap);surface.Set(bx+4,by+2,mushroomCap);
            surface.Set(bx+2,by+3,mushroomCap);surface.Set(bx+3,by+3,mushroomCap);surface.Set(bx+4,by+3,mushroomCap);
            surface.Set(bx+3,by+4,mushroomCap);
            surface.Set(bx+7,by+2,wood);surface.Set(bx+8,by+2,wood);
            surface.HLine(bx+7,bx+8,by+4,wood);
        }
        void DrawMushrooms(int x,int y){
            Mushroom(x,y,2);Mushroom(x+5,y-1,3);Mushroom(x+2,y+1,1);
        }
        void Mushroom(int cx,int by,int r){
            surface.Fill(cx,by-2,cx+1,by,mushroomStem);
            surface.Set(cx-1,by,mushroomStem);surface.Set(cx+2,by,mushroomStem);
            for(int dx=-r;dx<=r;dx++){
                int yy=by-4+Math.Abs(dx)/2;
                surface.Set(cx+dx,yy,mushroomCap);
                surface.Set(cx+dx,yy+1,Shade(mushroomCap,.78));
                surface.Set(cx+dx,yy+2,Shade(mushroomCap,.55));
            }
            if(r>1){surface.Set(cx+1,by-4,waterLight);surface.Set(cx-1,by-3,waterLight);}
            surface.Set(cx,by-5,Shade(mushroomCap,1.1));
        }
        void DrawStones(int x,int y){
            Stone(x,y,3);Stone(x+6,y+1,2);Stone(x-5,y+1,2);
        }
        void Stone(int cx,int by,int r){
            surface.Fill(cx-r,by-1,cx+r,by,stone);
            surface.HLine(cx-r+1,cx+r-1,by-2,stone);
            surface.Set(cx,by-2,Shift(stone,8));
            surface.HLine(cx-r,cx+r,by,stoneDark);
        }
        static int Shift(int argb,int amount){
            int r=Math.Min(255,((argb>>16)&255)+amount),g=Math.Min(255,((argb>>8)&255)+amount),b=Math.Min(255,(argb&255)+amount);
            return ((argb>>24)&255)<<24|r<<16|g<<8|b;
        }
        void DrawCan(int x,int y,bool flip){
            int d=flip?-1:1;
            int left=flip?x-6:x;
            surface.Fill(left,y-5,left+6,y,metal);
            surface.HLine(left,y-5,left+6,metalLight);
            surface.HLine(left,y,left+6,metalDark);
            surface.VLine(left,y-5,y,metalLight);
            surface.VLine(left+6,y-5,y,metalDark);
            surface.Set(left+2,y-4,metalLight);surface.Set(left+3,y-4,metalLight);
            int hx=flip?left+6:left;
            surface.Set(hx+d*1,y-6,metalDark);surface.Set(hx+d*2,y-7,metalDark);
            surface.Set(hx+d*3,y-7,metalDark);surface.Set(hx+d*4,y-6,metalDark);
            int sx=flip?left:left+6;
            for(int i=1;i<=4;i++)surface.Set(sx+d*i,y-5+i/3,metal);
            surface.Set(sx+d*5,y-3,metalLight);
            surface.Set(sx+d*4,y-4,metalLight);
        }
        void DrawPinwheel(int x,int y,double time){
            surface.VLine(x,y-9,y,Mix(wood,ceramic,.35));
            surface.Set(x+1,y-5,Mix(wood,woodDark,.5));
            int cx=x,cy=y-11;
            double angle=time*1.9;
            for(int i=0;i<4;i++){
                double a=angle+i*Math.PI/2;
                int color=(i%2==0)?paintA:paintB;
                for(int r=1;r<=3;r++){
                    int px=cx+(int)Math.Round(Math.Cos(a)*r);
                    int py=cy+(int)Math.Round(Math.Sin(a)*r);
                    surface.Set(px,py,r==3?Shift(color,22):color);
                }
                surface.Set(cx+(int)Math.Round(Math.Cos(a+.85)*2),cy+(int)Math.Round(Math.Sin(a+.85)*2),Mix(color,ceramic,.4));
            }
            surface.Set(cx,cy,gold);
        }
        void DrawFairyLights(int x,int y,double time){
            int span=46;
            for(int i=0;i<=span;i++){
                int px=x+i,sag=(int)Math.Round(Math.Sin((double)i/span*Math.PI)*3.0);
                surface.Set(px,y+sag,Mix(metal,outline,.5));
                if(i%7==2){
                    int by=y+sag+1;
                    int tint=(i%21==2)?paintA:((i%21==9)?paintB:gold);
                    double glowAmt=.45+.3*Math.Sin(time*2.6+i*.9);
                    surface.Blend(px,by,tint,1);
                    surface.Blend(px,by,glow,glowAmt);
                    surface.Blend(px-1,by,tint,.32);surface.Blend(px+1,by,tint,.32);
                    surface.Blend(px,by+1,Mix(tint,outline,.55),.9);
                }
            }
            surface.Set(x,y-1,Mix(metal,outline,.4));surface.Set(x+span,y-1,Mix(metal,outline,.4));
        }
        void DrawGnome(int x,int y){
            int robe=Mix(ceramic,paintB,.18),beard=Mix(stone,ceramic,.55);
            // 靴子与袍子
            surface.HLine(x-2,x-1,y,woodDark);surface.HLine(x+1,x+2,y,woodDark);
            surface.Fill(x-3,y-5,x+3,y-1,robe);
            surface.VLine(x-3,y-5,y-1,Mix(robe,paintB,.3));
            surface.VLine(x+3,y-5,y-1,Mix(robe,outline,.28));
            surface.Set(x-1,y-1,Mix(robe,paintB,.35));surface.Set(x+1,y-1,Mix(robe,paintB,.35));
            // 胡子与脸
            surface.Fill(x-2,y-6,x+2,y-5,beard);
            surface.Set(x,y-6,Mix(beard,robe,.45));
            surface.HLine(x-1,x+1,y-7,ceramic);
            surface.Set(x-1,y-7,Mix(outline,ceramic,.3));surface.Set(x+1,y-7,Mix(outline,ceramic,.3));
            surface.Set(x,y-7,Mix(ceramic,paintA,.25));
            // 尖帽子
            surface.Fill(x-2,y-9,x+2,y-8,paintA);
            surface.HLine(x-2,x+2,y-8,Shift(paintA,18));
            surface.Set(x,y-10,paintA);surface.Set(x,y-11,Shift(paintA,30));
            surface.Set(x-2,y-7,Mix(paintA,outline,.35));surface.Set(x+2,y-7,Mix(paintA,outline,.35));
            surface.Blend(x,y+1,Mix(soilDark,outline,.35),.4);
        }
        void DrawDuck(int x,int y){
            surface.Fill(x-3,y-2,x+2,y-1,gold);
            surface.HLine(x-3,x+2,y,gold);
            surface.Set(x-3,y,Mix(gold,woodDark,.45));surface.Set(x+2,y,Mix(gold,woodDark,.45));
            surface.Fill(x+1,y-4,x+3,y-3,gold);
            surface.Set(x+2,y-5,gold);
            surface.Set(x+4,y-3,paintA);surface.Set(x+4,y-2,Mix(paintA,outline,.45));
            surface.Set(x+3,y-4,outline);
            surface.Set(x-2,y-2,Shift(gold,28));surface.Set(x+2,y-5,Shift(gold,20));
            surface.Set(x-1,y-1,Mix(gold,woodDark,.3));
            surface.Blend(x,y+1,Mix(soilDark,outline,.35),.4);
        }
        void DrawCloche(int x,int y){
            int top=y-9;
            surface.Fill(x-4,y,x+4,y,wood);surface.HLine(x-4,x+4,y+1,woodDark);
            surface.HLine(x-3,x+3,y,woodLight);
            for(int i=0;i<=9;i++){
                int yy=top+i;
                int half=(int)Math.Round(3.4*Math.Sqrt(Math.Min(1,(double)i/9*1.15)));
                for(int dx=-half;dx<=half;dx++){
                    if(Math.Abs(dx)==half||i==0)surface.Set(x+dx,yy,glass);
                    else surface.Blend(x+dx,yy,glassLight,.09);
                }
            }
            surface.Set(x,top-1,glass);surface.Set(x,top-2,glassLight);
            for(int i=2;i<7;i++)surface.Set(x-2,top+i,glassLight);
            // 罩子里的小植株
            surface.Set(x,y-1,Mix(grass,grassDark,.5));
            surface.Set(x-1,y-2,grass);surface.Set(x+1,y-2,grassLight);
            surface.Set(x,y-3,grassLight);
        }
        void DrawGazing(int x,int y){
            surface.Fill(x-2,y,x+2,y,metal);surface.HLine(x-2,x+2,y+1,metalDark);
            surface.Set(x,y-1,metalLight);
            surface.Disc(x,y-6,3,glass);
            surface.Disc(x+1,y-5,2,Mix(glass,paintB,.6));
            surface.Set(x-1,y-8,glassLight);surface.Set(x-2,y-7,glassLight);
            surface.Set(x-1,y-7,glassLight);surface.Set(x,y-2,Mix(glass,outline,.35));
        }
        void Marble(int cx,int cy,int r,int color){
            surface.Disc(cx,cy,r,color);
            surface.Set(cx-r+1,cy-r+1,Mix(color,glassLight,.75));
            surface.Set(cx+r-1,cy-r+1,Mix(color,ceramic,.45));
            surface.Set(cx+r-1,cy+r-1,Mix(color,outline,.4));
        }
        void DrawBaubles(int x,int y){
            Marble(x,y,2,paintA);Marble(x+4,y+1,1,paintB);
            Marble(x+7,y-1,2,gold);Marble(x+2,y-3,1,glass);
        }
        // 停在横杆上的小鸟：个头不大，但要有头有尾有喙，
        // 不然 4 个像素读起来只是块脏点。y 是它站着的那一行。
        void DrawBird(int x,int y,bool flip,double time){
            int d=flip?-1:1;
            int beat=(int)(time*1.15)%10;
            bool glance=beat<5;          // 前半段侧头看远处，后半段转头看这边
            bool flick=beat>=7;          // 末尾抖两下尾巴
            int body=birdColor,belly=birdLight,wing=birdWing;
            int dark=Shade(wing,.72);
            // 尾羽：平时斜向下，抖尾巴的时候抬起来
            int ty=flick?y-4:y-2,tip=ty+(flick?-1:1);
            surface.Set(x+d*-3,ty,wing);
            surface.Set(x+d*-4,tip,dark);
            surface.Set(x+d*-3,tip,wing);
            // 两条腿，站在杆子上
            surface.Set(x+d*0,y-1,outline);surface.Set(x+d*0,y,outline);
            surface.Set(x+d*2,y-1,outline);surface.Set(x+d*2,y,outline);
            // 身体
            surface.HLine(x+d*-1,x+d*2,y-3,body);
            surface.HLine(x+d*-2,x+d*3,y-2,body);
            surface.HLine(x+d*-1,x+d*2,y-1,body);
            surface.Set(x+d*-2,y-3,body);surface.Set(x+d*3,y-1,body);
            // 胸腹的浅色，把体积撑起来
            surface.Set(x+d*1,y-1,belly);surface.Set(x+d*2,y-2,belly);
            surface.Set(x+d*2,y-3,belly);surface.Set(x+d*3,y-3,belly);
            surface.Set(x+d*2,y-1,Mix(body,belly,.5));
            // 翅膀：两片羽层加一条暗边
            surface.Set(x+d*-1,y-2,wing);surface.Set(x+d*0,y-2,wing);surface.Set(x+d*0,y-3,wing);
            surface.Set(x+d*-1,y-1,Mix(wing,body,.45));
            surface.Set(x+d*-1,y-3,Mix(wing,body,.25));
            // 头
            surface.HLine(x+d*1,x+d*3,y-6,body);
            surface.HLine(x+d*1,x+d*3,y-5,body);
            surface.Set(x+d*1,y-4,body);surface.Set(x+d*2,y-4,body);
            surface.Set(x+d*1,y-6,Shift(body,20));surface.Set(x+d*2,y-6,Shift(body,12));
            // 眼睛与喙，转头时眼位往喙那边挪一格
            surface.Set(x+d*(glance?3:2),y-6,outline);
            surface.Set(x+d*4,y-5,beakColor);
            surface.Set(x+d*4,y-6,Shift(beakColor,18));
            // 杆子上的一点接触阴影
            surface.Blend(x+d*1,y+1,Mix(woodDark,outline,.55),.32);
        }
        // 天上的小鸟：只画剪影，两只翅膀一上一下就是扇动。
        void DrawSkyBirds(double time){
            for(int i=0;i<3;i++){
                double period=27+i*13;
                double phase=((time+i*7.3)/period)%1.0;
                if(phase>=.55)continue;                 // 飞过去之后就安静一会儿
                int x=(int)Math.Round(-6+phase/.55*(surface.Width+12));
                int y=3+i*4+(int)Math.Round(Math.Sin(time*.55+i*1.7)*1.6);
                bool up=((int)(time*(6.2+i*.9)))%2==0;
                DrawSkyBird(x,y,up,i==1?Shade(birdColor,.82):birdColor);
            }
        }
        void DrawSkyBird(int x,int y,bool up,int color){
            surface.Set(x-1,y-1,color);surface.Set(x,y-1,color);surface.Set(x+1,y-1,color);
            surface.Set(x,y,Shade(color,.8));
            if(up){
                surface.Set(x-2,y-2,color);surface.Set(x+2,y-2,color);
                surface.Set(x-3,y-1,color);surface.Set(x+3,y-1,color);
            }else{
                surface.Set(x-2,y,color);surface.Set(x+2,y,color);
                surface.Set(x-3,y+1,color);surface.Set(x+3,y+1,color);
            }
        }
        void DrawPlants(GardenEngine engine){
            var ordered=engine.Plants.Where(p=>p.slot>=0&&p.slot<config.slots.Count)
                .OrderBy(p=>config.slots[p.slot].y).ThenBy(p=>config.slots[p.slot].x).ToList();
            foreach(var plant in ordered){
                Array.Clear(surface.Mask,0,surface.Mask.Length);
                surface.Capture=true;
                DrawPlant(engine,plant,0,0);
                surface.Capture=false;
                OutlinePlant(plant);
            }
        }
        // 按剪影的外沿补一圈暗色：叶片之间、植株之间不会糊成一团，
        // 跟猫咪身上的深褐轮廓也是同一种处理。
        void OutlinePlant(GardenPlant plant){
            SpeciesArt art;
            if(!arts.TryGetValue(plant.species,out art))return;
            int halo=Mix(Shade(art.leafDark,.55),soilDark,.35);
            var slot=config.slots[plant.slot];
            int x0=Math.Max(0,slot.x-13),x1=Math.Min(surface.Width-1,slot.x+13);
            int y0=Math.Max(0,slot.y-(int)Math.Round(art.height*slot.scale)-8),y1=Math.Min(surface.Height-1,slot.y+3);
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){
                int i=y*surface.Width+x;
                if(surface.Mask[i])continue;
                bool edge=(x>0&&surface.Mask[i-1])||(x<surface.Width-1&&surface.Mask[i+1])
                    ||(y>0&&surface.Mask[i-surface.Width])||(y<surface.Height-1&&surface.Mask[i+surface.Width]);
                if(edge)surface.SetDirect(x,y,halo);
            }
        }
        void DrawPlant(GardenEngine engine,GardenPlant plant,int ox,int oy){
            SpeciesArt art;
            if(!arts.TryGetValue(plant.species,out art))return;
            var slot=config.slots[plant.slot];
            double growth=Math.Max(0,Math.Min(1,plant.growth)),wilt=Math.Max(0,Math.Min(1,plant.wilt));
            double phase=slot.x*.37+slot.y*.19;
            double gust=(.35+engine.Wind*2.7)*slot.scale;
            int x=slot.x+ox,y=slot.y+oy;
            int hMax=(int)Math.Round(art.height*slot.scale);
            int h=(int)Math.Round(hMax*Math.Pow(Math.Min(1,growth/.96),.85));
            int leafColor=wilt>.02?Mix(art.leaf,art.dried,wilt*.55):art.leaf;
            int leafDark=wilt>.02?Mix(art.leafDark,art.dried,wilt*.5):art.leafDark;
            int leafLight=Shift(leafColor,26);
            // 落在土上的影子，跟着株幅变宽
            int reach=(int)Math.Round((2.2+growth*2.6)*slot.scale);
            for(int dx=-reach;dx<=reach;dx++)surface.Blend(x+dx,y+1,Mix(soilDark,outline,.3),.3*(1-Math.Abs(dx)/(double)(reach+1)));
            // 根部的松土
            surface.Blend(x-1,y,Mix(soilLight,soil,.5),.45);
            surface.Blend(x,y,Mix(soilLight,soil,.75),.5);
            surface.Blend(x+1,y,Mix(soilLight,soil,.5),.45);
            if(growth<.05){surface.Set(x,y-1,Mix(soilDark,outline,.45));return;}
            if(growth<.17){DrawSprout(x,y,growth,leafColor,leafDark);return;}
            if(h<2)h=2;
            int stems=Math.Min(art.stems,1+(int)Math.Floor(growth*3.2));
            Point tip=new Point(x,y-h);
            for(int s=0;s<stems;s++){
                // 每根茎都往外斜一点、风里错开相位，不是同一根线克隆出来的。
                int order=s==0?0:(s%2==1?-1:1)*(s+1)/2;
                double rnd=((Hash(slot.x*31+plant.slot*7,s)>>9)&255)/255.0;
                double lean=(order*2.6+(rnd-.5)*1.2)*slot.scale;   // 侧茎真的往外岔开
                double sway=Math.Sin(engine.Time*.85+phase+s*2.1)*gust;
                double nod=-(lean*.45)+(rnd-.5)*1.1*slot.scale;
                double factor=s==0?1:(.78+.04*s);
                int baseOff=(int)Math.Round(order*2.3*slot.scale);
                Point stemTip=DrawStem(art,x+baseOff,y,(int)Math.Round(h*factor),lean,sway,nod,slot.scale,growth,wilt,leafColor,leafDark,leafLight,s==0,s%2,(rnd-.5)*.20);
                if(s==0)tip=stemTip;   // 花永远开在主茎顶端
            }
            // 基部的短叶，让植株不是一根杆子插在土里
            if(growth>.34){
                int baseLen=Math.Max(2,(int)Math.Round(art.leafWidth*slot.scale*1.25));
                DrawLeaf(x-1,y-2,baseLen,-1,leafColor,leafDark,leafLight,0,art.leafRise*.55);
                DrawLeaf(x+1,y-2,baseLen,1,leafColor,leafDark,leafLight,0,art.leafRise*.55);
                if(growth>.62){
                    DrawLeaf(x-1,y-4,baseLen-1,-1,leafDark,leafDark,leafColor,0,art.leafRise*.45);
                    DrawLeaf(x+1,y-4,baseLen-1,1,leafDark,leafDark,leafColor,0,art.leafRise*.45);
                }
            }
            if(art.flowering){
                double bloom=(growth-GardenEngine.BloomStart)/(GardenEngine.BloomFull-GardenEngine.BloomStart);
                if(bloom>0)DrawFlower(art,tip.X,tip.Y,Math.Min(1,bloom),slot.scale,wilt);
                else if(growth>.46){
                    int bud=wilt>.02?Mix(art.flower2,art.dried,wilt*.35):art.flower2;
                    surface.Set(tip.X,tip.Y,bud);
                    surface.Set(tip.X,tip.Y+1,leafDark);
                    if(growth>.55){
                        surface.Set(tip.X,tip.Y-1,Mix(bud,art.flower,.6));
                        surface.Set(tip.X-1,tip.Y+1,leafDark);surface.Set(tip.X+1,tip.Y+1,leafColor);
                    }
                }
            }else if(growth>.60){
                int crop=Math.Min(5,1+(int)Math.Round((growth-.60)*10));
                for(int i=0;i<crop;i++){
                    surface.Set(tip.X-i,tip.Y+i,leafColor);
                    surface.Set(tip.X+i,tip.Y+i+1,i%2==0?leafDark:leafColor);
                    surface.Set(tip.X,tip.Y-i,leafLight);
                }
            }
        }
        void DrawSprout(int x,int y,double growth,int leafColor,int leafDark){
            int light=Shift(leafColor,18);
            surface.Set(x,y-1,Mix(leafColor,leafDark,.45));
            if(growth<.08)return;
            // 两片子叶，先左右摊开，再长出第三片
            surface.Set(x-1,y-2,leafColor);surface.Set(x+1,y-2,leafColor);
            surface.Set(x-1,y-1,leafDark);surface.Set(x+1,y-1,leafDark);
            if(growth<.12)return;
            surface.Set(x-2,y-2,leafDark);surface.Set(x+2,y-2,leafDark);
            surface.Set(x-1,y-3,light);surface.Set(x+1,y-3,light);
            if(growth<.15)return;
            surface.Set(x,y-3,Mix(leafColor,leafDark,.3));
            surface.Set(x,y-4,leafColor);
            surface.Set(x-2,y-3,Mix(leafColor,leafDark,.55));
        }
        // 茎的横向位置：起手几乎竖直，中段往外鼓，顶梢被风带歪再回勾，
        // 所以放大看是一道弯而不是一根竖线。
        static double StemCurve(double lean,double sway,double nod,double t){
            double bow=Math.Sin(t*Math.PI*.92)*.85+t*.55;
            return lean*bow+sway*t*t*(1.2+t)+nod*t*t*t;
        }
        Point DrawStem(SpeciesArt art,int x,int baseY,int h,double lean,double sway,double nod,double scale,double growth,double wilt,int leafColor,int leafDark,int leafLight,bool primary,int phase,double stagger){
            int stemColor=wilt>.02?Mix(art.stem,art.dried,wilt*.6):art.stem;
            int stemLight=Shift(stemColor,18),stemDark=Shade(stemColor,.78);
            int thick=art.stemWidth>1?3:2;
            if(h<7)thick=1;
            for(int i=0;i<=h;i++){
                double t=h<=0?0:(double)i/h;
                int px=x+(int)Math.Round(StemCurve(lean,sway,nod,t)),py=baseY-i;
                int taper=(int)Math.Round(thick-(thick-1)*t*t);
                surface.Set(px,py,i%5==0?stemLight:stemColor);
                if(taper>1)surface.Set(px-1,py,stemLight);
                if(taper>2)surface.Set(px+1,py,stemDark);
                if(taper==1&&i%3==1)surface.Set(px,py,stemLight);
            }
            int pairs=primary?art.leafPairs:Math.Max(1,art.leafPairs-1);
            if(pairs>2)pairs=2;    // 叶子少而大：像素画里宁可两片大的，不要四片糊的
            for(int k=0;k<pairs;k++){
                double appear=k==0?.12:.30;
                if(growth<appear)continue;
                double open=Math.Min(1,(growth-appear)/.10);
                // 叶子压在植株中段，梢头留出一截光茎，才看得到茎的弯
                double t=(k==0?.36:.62)+stagger;
                if(t<.2)t=.2;
                if(t>.86)t=.86;
                int lx=x+(int)Math.Round(StemCurve(lean,sway,nod,t));
                int ly=baseY-(int)Math.Round(h*t);
                // 一对大叶：左后右前，中间露出茎
                int dir=((k+phase)%2==0)?1:-1;
                int len=Math.Max(3,(int)Math.Round(art.leafWidth*scale*1.6*open));
                int droop=(int)Math.Round(wilt*3.1*(.4+t));
                double rise=art.leafRise*.5;
                surface.Set(lx,ly,Mix(stemColor,outline,.4));
                // 先画背向的那片，再画朝向观察者的那片，前后有层次
                DrawLeaf(lx,ly-1,len-1,-dir,Shade(leafColor,.88),leafDark,leafColor,droop,rise);
                DrawLeaf(lx,ly-1,len,dir,leafColor,leafDark,leafLight,droop,rise);
            }
            return new Point(x+(int)Math.Round(StemCurve(lean,sway,nod,1)),baseY-h);
        }
        void DrawLeaf(int cx,int cy,int len,int dir,int color,int dark,int light,int droop,double rise){
            if(len<1)len=1;
            int dy=0;
            for(int j=1;j<=len;j++){
                double f=(double)j/len;
                // 从叶柄斜着往上抽，越靠叶尖越细；蔫了才往下垂
                dy=-(int)Math.Round(len*rise*.9*f)+droop;
                int px=cx+dir*j,py=cy+dy;
                surface.Set(px,py,color);
                if(j<len)surface.Set(px,py+1,dark);
                if(j==len||j==len-1)surface.Set(px,py-1,light);
            }
        }
        void DrawFlower(SpeciesArt art,int cx,int cy,double bloom,double scale,double wilt){
            int radius=Math.Max(1,(int)Math.Round(art.size*scale*bloom*1.45));
            int flower=wilt>.02?Mix(art.flower,art.dried,wilt*.35):art.flower;
            int flower2=wilt>.02?Mix(art.flower2,art.dried,wilt*.35):art.flower2;
            int light=Shift(flower,22),deep=Shade(flower2,.8);
            switch(art.form){
                case "cluster":{
                    int n=Math.Max(1,(int)Math.Round(art.count*bloom));
                    for(int i=0;i<n;i++){
                        int yy=cy+i*2,rr=Math.Min(2,Math.Max(1,radius-(i>1?1:0)));
                        surface.Set(cx,yy-1,flower2);
                        surface.Disc(cx,yy,rr,flower);
                        surface.Set(cx-rr+1,yy-1,light);
                        surface.Set(cx,yy,art.core);
                        surface.Set(cx+rr,yy+1,deep);
                    }
                    break;}
                case "cup":{
                    int n=Math.Max(1,(int)Math.Round(art.count*bloom));
                    for(int i=0;i<n;i++){
                        int yy=cy+i*3,xx=cx+(i%2==0?0:1);
                        // 三像素宽的钟形花冠，串在花茎上
                        surface.HLine(xx-1,xx+1,yy-2,flower2);
                        surface.Set(xx-1,yy-2,light);
                        surface.HLine(xx-1,xx+1,yy-1,flower);
                        surface.Set(xx,yy-1,art.core);
                        surface.Set(xx,yy,deep);
                    }
                    break;}
                case "poppy":{
                    int r=Math.Max(1,Math.Min(4,radius));
                    surface.Disc(cx,cy,r,flower);
                    if(r>=3)surface.Disc(cx,cy,r-1,flower2);
                    // 四片薄瓣之间的褶，别糊成一个圆点
                    for(int i=0;i<4;i++){
                        double a=i*Math.PI/2+Math.PI/4;
                        surface.Set(cx+(int)Math.Round(Math.Cos(a)*r*.85),cy+(int)Math.Round(Math.Sin(a)*r*.85),Shade(flower2,.86));
                    }
                    surface.Set(cx,cy,art.core);
                    surface.Set(cx-r+1,cy-r+1,light);
                    surface.Set(cx+r-1,cy+r-1,Shade(flower2,.72));
                    break;}
                case "spike":{
                    int length=Math.Max(3,(int)Math.Round((3+art.size*2.2)*scale*bloom));
                    for(int i=0;i<=length;i++){
                        int yy=cy+i;
                        surface.Set(cx,yy,i%2==0?flower:flower2);
                        if(i%3!=1)surface.Set(cx-1,yy,flower2);
                        if(i%4==1)surface.Set(cx+1,yy,i>length*.6?flower:deep);
                        if(i%6==2)surface.Set(cx-2,yy,Mix(flower,flower2,.5));
                    }
                    surface.Set(cx,cy-1,light);
                    break;}
                default:{
                    int r=Math.Max(2,radius);
                    for(int i=0;i<8;i++){
                        double a=i*Math.PI/4;
                        int dx=(int)Math.Round(Math.Cos(a)),dy=(int)Math.Round(Math.Sin(a));
                        // 花瓣沿径向两格，读起来是花瓣而不是一圈散点
                        surface.Set(cx+dx*(r-1),cy+dy*(r-1),i%2==0?light:flower);
                        surface.Set(cx+dx*r,cy+dy*r,i%2==0?flower:flower2);
                        if(dx!=0&&dy!=0)surface.Set(cx+dx*r,cy+dy*(r-1),Mix(flower,flower2,.5));
                    }
                    surface.Disc(cx,cy,Math.Max(1,r-2),art.core);
                    surface.Set(cx,cy,deep);
                    break;}
            }
        }
        void DrawSowFeedback(GardenEngine engine){
            // 先画飞起来的土粒和落下的种子，再画光环与闪光：
            // 十字闪光压在最上面，保证"刚种下"的强调不会被一粒种子挡住。
            foreach(var spark in engine.Sparks){
                int x=(int)Math.Round(spark.x),y=(int)Math.Round(spark.y);
                double alpha=Math.Max(.2,Math.Min(1,spark.life/Math.Max(.001,spark.maxLife*.5)));
                if(spark.kind==2){
                    // 落下的种子：深色两格加一格反光，落进土里就消失。
                    surface.Blend(x,y,spark.color,alpha);
                    surface.Blend(x,y+1,Shade(spark.color,.8),alpha);
                    surface.Blend(x-1,y,Shift(spark.color,58),alpha*.7);
                    continue;
                }
                surface.Blend(x,y,spark.color,alpha);
                if(spark.kind==1){
                    // 光点做成十字闪光，比单个亮点更像"发芽了"。
                    surface.Blend(x,y-1,Shift(spark.color,62),alpha*.9);
                    surface.Blend(x,y+1,Shift(spark.color,38),alpha*.7);
                    surface.Blend(x-1,y,Shift(spark.color,46),alpha*.8);
                    surface.Blend(x+1,y,Shift(spark.color,46),alpha*.8);
                }else{
                    surface.Blend(x,y-1,Shift(spark.color,40),alpha*.6);
                }
            }
            // 撒下去的位置亮一圈、土被翻起来一点，再慢慢散掉。
            if(engine.SowGlow>0&&engine.SowSlot>=0&&engine.SowSlot<config.slots.Count){
                var spot=config.slots[engine.SowSlot];
                double t=1-engine.SowGlow;
                // 光环一开始就有 4 像素的直径，从小圈慢慢散到整个槽位那么大。
                int radius=4+(int)Math.Round(t*6);
                double alpha=engine.SowGlow;
                int ringColor=Mix(glow,soilLight,.45);
                // 光环画成扁椭圆：按行取左右两端点，顶底两行连成弧，
                // 半径小的时候也不会散成一把噪点。
                int ry=Math.Max(1,(int)Math.Round(radius*.42));
                for(int row=-ry;row<=ry;row++){
                    double span=1-(double)(row*row)/(ry*ry);
                    if(span<0)continue;
                    int dx=Math.Max(1,(int)Math.Round(radius*Math.Sqrt(span)));
                    int py=spot.y+1+row;
                    surface.Blend(spot.x-dx,py,ringColor,alpha*.8);
                    surface.Blend(spot.x+dx,py,ringColor,alpha*.8);
                    if(row==-ry||row==ry){
                        for(int x=-dx;x<=dx;x++)surface.Blend(spot.x+x,py,ringColor,alpha*.55);
                    }
                }
                // 新翻的一小撮土，用偏暖的土黄，深褐土上才看得出来是"刚动过"。
                // 中间一撮稍高、两边散开，别糊成一块实心方块。
                int fresh=Mix(soilLight,glow,.42);
                surface.Blend(spot.x-2,spot.y+1,Mix(soilLight,fresh,.5),.38*alpha);
                surface.Blend(spot.x-1,spot.y,Mix(soilLight,fresh,.7),.52*alpha);
                surface.Blend(spot.x,spot.y+2,Mix(soilLight,fresh,.95),.66*alpha);
                surface.Blend(spot.x+1,spot.y,Mix(soilLight,fresh,.65),.46*alpha);
                surface.Blend(spot.x+2,spot.y+1,Mix(soilLight,fresh,.45),.32*alpha);
                surface.Blend(spot.x,spot.y-1,Shift(glow,40),.5*alpha);
                // 一枚向上飘的白色十字闪光：整个动作里最容易看到的就是它
                int gx=spot.x,gy=spot.y-9-(int)Math.Round(t*3);
                double ga=alpha;
                int white=Color.White.ToArgb();
                surface.Blend(gx,gy,Mix(glow,white,.88),ga);
                surface.Blend(gx,gy-1,Mix(glow,white,.62),ga*.85);
                surface.Blend(gx-1,gy,Mix(glow,white,.62),ga*.8);
                surface.Blend(gx+1,gy,Mix(glow,white,.62),ga*.8);
                surface.Blend(gx,gy+1,Mix(glow,white,.45),ga*.7);
                surface.Blend(gx,gy-2,Mix(glow,white,.35),ga*.5);
            }
        }
        void DrawPetals(GardenEngine engine){
            foreach(var petal in engine.Petals){
                int x=(int)Math.Round(petal.x),y=(int)Math.Round(petal.y);
                double fade=Math.Min(1,petal.life/Math.Max(.001,petal.maxLife*.45));
                double alpha=Math.Max(.28,fade);
                int accent=(int)Math.Round(petal.phase*3)%3;
                surface.Blend(x,y,petal.color,alpha);
                if(accent==0){surface.Blend(x+1,y,Shift(petal.color,26),alpha*.85);surface.Blend(x,y+1,Shade(petal.color,.8),alpha*.6);}
                else if(accent==1){surface.Blend(x,y-1,Shift(petal.color,18),alpha*.8);surface.Blend(x+1,y,Shade(petal.color,.85),alpha*.65);}
                else{surface.Blend(x+1,y,Shift(petal.color,22),alpha*.8);surface.Blend(x,y-1,Shade(petal.color,.9),alpha*.55);}
            }
            foreach(var d in config.decorations)if(d.type=="butterfly")DrawButterfly(d.x,d.y,engine.Time);
        }
        void DrawButterfly(int baseX,int baseY,double time){
            int x=baseX+(int)Math.Round(Math.Sin(time*.42)*11);
            int y=baseY+(int)Math.Round(Math.Sin(time*.71+1.2)*4);
            bool open=((int)(time*5.5))%2==0;
            surface.Set(x,y-1,outline);surface.Set(x,y-2,outline);
            surface.Set(x-1,y-2,outline);surface.Set(x+1,y-2,outline);
            if(open){
                surface.Set(x-1,y-1,butterflyWing);surface.Set(x-2,y-2,butterflyWing);surface.Set(x-1,y-2,butterflyWing);
                surface.Set(x+1,y-1,butterflyWing);surface.Set(x+2,y-2,butterflyWing);surface.Set(x+1,y-2,butterflyWing);
                surface.Set(x-2,y-1,butterfly);surface.Set(x+2,y-1,butterfly);
            }else{
                surface.Set(x-1,y-2,butterflyWing);surface.Set(x+1,y-2,butterflyWing);
                surface.Set(x-1,y-1,butterfly);surface.Set(x+1,y-1,butterfly);
            }
        }
        void DrawWatering(GardenEngine engine){
            double wet=engine.Watering;
            if(wet<=0)return;
            double t=1-wet;
            for(int i=0;i<18;i++){
                int x=16+((i*29)%84);
                int y=config.bed.top-6+(int)((t*150+i*17)%40);
                surface.Blend(x,y,waterLight,.85);
                surface.Blend(x,y-1,water,.7);
            }
            int il=config.bed.left+config.bed.inset,ir=config.bed.right-config.bed.inset,ib=config.bed.bottom-config.bed.inset;
            for(int x=il;x<=ir;x+=2)surface.Blend(x,ib,waterLight,.35*Math.Min(1,t*3));
        }
        void DrawBadge(GardenEngine engine){
            if(!engine.NeedsWater)return;
            double pulse=engine.Critical?.55+.45*Math.Abs(Math.Sin(engine.Time*4.2)):.6+.4*Math.Sin(engine.Time*2.4);
            int bx=8,by=7+(int)Math.Round(Math.Sin(engine.Time*1.5));
            surface.Blend(bx,by-1,outline,pulse);
            surface.Blend(bx-1,by,outline,pulse);surface.Blend(bx+1,by,outline,pulse);
            surface.Blend(bx-2,by+1,outline,pulse);surface.Blend(bx+2,by+1,outline,pulse);
            surface.Blend(bx-2,by+2,outline,pulse);surface.Blend(bx+2,by+2,outline,pulse);
            surface.Blend(bx-2,by+3,outline,pulse);surface.Blend(bx+2,by+3,outline,pulse);
            surface.Blend(bx-1,by+4,outline,pulse);surface.Blend(bx+1,by+4,outline,pulse);
            surface.Blend(bx,by+5,outline,pulse);
            surface.Blend(bx,by,water,pulse);
            for(int dx=-1;dx<=1;dx++)for(int dy=1;dy<=3;dy++)surface.Blend(bx+dx,by+dy,water,pulse);
            surface.Blend(bx-1,by+4,water,pulse);surface.Blend(bx+1,by+4,water,pulse);
            surface.Blend(bx,by+5,water,pulse);
            surface.Blend(bx-1,by+1,waterLight,pulse);
            surface.Blend(bx-1,by+2,waterLight,pulse*.7);
            if(engine.Critical)surface.Blend(bx,by+7,waterLight,Math.Abs(Math.Sin(engine.Time*3)));
        }
    }
}
