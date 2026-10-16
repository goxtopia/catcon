using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace Catcon {
    static class ArtExport {
        public static void Run(string root,string destination){
            using(var content=Content.Load(root,"mikan")){
                Skin skin=content.Skin;var anim=new Animator(skin);int size=skin.canvas;
                string folder=Path.Combine(destination,"assets","skins","mikan");Directory.CreateDirectory(Path.Combine(folder,"frames"));Directory.CreateDirectory(Path.Combine(destination,"docs"));
                using(var bitmap=anim.RenderAt("sit",0,false))using(var icon=Native.CreateIcon(bitmap))using(var iconFile=File.Create(Path.Combine(destination,"assets","catcon.ico")))icon.Save(iconFile);
                var records=new List<object>();
                foreach(var pair in skin.clips){
                    var times=Animator.ExportTimeline(skin,pair.Value);int count=times.Count-1;
                    var durations=Enumerable.Range(0,count).Select(i=>times[i+1]-times[i]).ToList();
                    using(var strip=new Bitmap(size*count,size,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(strip)){
                        for(int i=0;i<count;i++)using(var frame=anim.RenderAt(pair.Key,times[i]/1000.0+.000001,false))g.DrawImageUnscaled(frame,i*size,0);
                        strip.Save(Path.Combine(folder,"frames",pair.Key+".png"),ImageFormat.Png);
                    }
                    records.Add(new{id=pair.Key,label=pair.Value.label,frames=count,frameMs=pair.Value.frameMs,durations=durations,playback=pair.Value.playback??"loop"});
                }
                Json.Write(Path.Combine(folder,"frames","manifest.json"),new{schemaVersion=2,canvas=size,clips=records});
                int cols=4,cellW=200,cellH=180,rows=(skin.clips.Count+cols-1)/cols;
                using(var sheet=new Bitmap(cols*cellW,rows*cellH+96))using(var g=Graphics.FromImage(sheet))using(var font=new Font("Microsoft YaHei UI",10))using(var title=new Font("Microsoft YaHei UI",24,FontStyle.Bold))using(var labelBrush=new SolidBrush(Color.FromArgb(70,81,65))) {
                    g.Clear(Color.FromArgb(246,244,236));g.DrawString("蜜柑 II · 逐帧动作图鉴",title,labelBrush,24,16);g.DrawString("CATCON  /  NATIVE 32 × 32  /  "+skin.clips.Count+" ANIMATIONS",font,labelBrush,27,61);
                    int i=0;foreach(var pair in skin.clips){int x=i%cols*cellW,y=i/cols*cellH+96;
                        using(var bg=new SolidBrush(i%2==0?Color.FromArgb(233,235,222):Color.FromArgb(239,236,224)))g.FillRectangle(bg,x+8,y+4,cellW-16,cellH-8);
                        double sample=pair.Key=="yawn"?1.2:pair.Key=="sit_tilt"?1.0:.4;
                        using(var frame=anim.RenderAt(pair.Key,sample,false))using(var scaled=Animator.Scale(frame,4))g.DrawImageUnscaled(scaled,x+36,y+1);
                        var format=new StringFormat{Alignment=StringAlignment.Center};g.DrawString(pair.Value.label,font,labelBrush,new RectangleF(x+5,y+132,cellW-10,25),format);format.Dispose();
                        using(var small=new Font("Consolas",8))g.DrawString(pair.Key,small,labelBrush,x+18,y+155);i++;
                    }
                    sheet.Save(Path.Combine(folder,"contact-sheet.png"),ImageFormat.Png);
                }
                var frames=new List<Bitmap>();var delays=new List<int>();
                var palette=skin.palette.Values.Select(v=>v.Length==9?Color.FromArgb((int)Convert.ToInt64(v.Substring(1),16)):ColorTranslator.FromHtml(v)).ToList();
                try{foreach(string id in new[]{"run","pounce","chase_tail","yarn","sit_groom","sleep"}){
                        Clip clip=skin.clips[id];int loops=id=="run"?3:id=="chase_tail"?2:1;
                        var times=Animator.ExportTimeline(skin,clip);
                        for(int loop=0;loop<loops;loop++)for(int i=0;i<times.Count-1;i++)using(var frame=anim.RenderAt(id,times[i]/1000.0+.000001,false)){
                            frames.Add(Animator.Scale(frame,4));delays.Add(Math.Max(1,(times[i+1]-times[i])/10));
                        }
                    }
                    WriteGif(Path.Combine(folder,"preview.gif"),frames,delays,palette);
                }finally{foreach(var b in frames)b.Dispose();}
                ExportMotionStudy(folder,skin,anim,palette);
                WriteGallery(Path.Combine(destination,"docs","gallery.html"),skin,records);
            }
        }
        static void WriteGallery(string path,Skin skin,List<object> records){
            string data=new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(records);
            string html=@"<!doctype html><html lang='zh-CN'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>蜜柑 · 动作图鉴</title><style>
:root{color-scheme:light}*{box-sizing:border-box}body{background:#f6f4ec;color:#35433b;margin:0;font:15px 'Microsoft YaHei',system-ui,sans-serif}main{max-width:1120px;margin:64px auto;padding:0 28px}header{display:flex;align-items:end;justify-content:space-between;gap:24px;margin-bottom:36px}.eyebrow{font:11px monospace;letter-spacing:3px;color:#74816d}h1{font-size:40px;letter-spacing:-1px;margin:12px 0}p{color:#74816d;line-height:1.8}button,select{border:1px solid #cbd5c2;border-radius:8px;background:#e1e9da;color:#35433b;padding:10px 16px;font:inherit;cursor:pointer}.toolbar{display:flex;gap:12px;flex-wrap:wrap;margin:28px 0}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(210px,1fr));gap:16px}article{background:#e9ebde;border:1px solid #dfe4d5;border-radius:14px;overflow:hidden;padding:18px 20px 20px;position:relative}article:hover{border-color:#9bb38e}canvas{width:160px;height:160px;display:block;image-rendering:pixelated;margin:0 auto 4px}h2{font-size:15px;font-weight:600;margin:12px 0 8px}code{font:11px monospace;color:#74816d}.number{position:absolute;top:16px;left:18px;color:#9aa68f;font:11px monospace}.footer{border-top:1px solid #d9dfd0;margin-top:40px;padding:22px 0;font-size:13px}@media(max-width:600px){main{margin:32px auto}header{display:block}h1{font-size:32px}.grid{grid-template-columns:repeat(2,minmax(0,1fr));gap:10px}article{padding:12px}canvas{width:128px;height:128px}h2{font-size:13px}}</style><main><header><div><div class='eyebrow'>CATCON / MIKAN / VOL. 01</div><h1>每一种小动作，都有点心情。</h1><p>蜜柑 · 橘白猫的第一套动作包<br>原生 32 × 32，独立部件、关键帧与变形器共同组成。</p></div><div class='eyebrow'>27 ANIMATIONS<br><br>ONE LITTLE COMPANION</div></header><div class='toolbar'><button id='pause'>暂停动画</button><select id='bg' aria-label='预览背景'><option value='#e9ebde'>鼠尾草背景</option><option value='#fff9ed'>奶油背景</option><option value='#30403b'>深色背景</option></select><select id='speed' aria-label='播放速度'><option value='1'>正常速度</option><option value='.5'>半速观察</option><option value='2'>两倍速</option></select></div><div class='grid' id='grid'></div><p class='footer'>这里播放的是导出的透明像素序列。桌面版还会叠加拖拽、位置移动、鼠标反应与前台情境。修改 skin.json 或行为配置后，在小窝中选择「应用 / 重新加载」。</p></main><script>
const clips=__DATA__;let paused=false,t=0,last=0;const items=clips.map((c,i)=>{const a=document.createElement('article');a.innerHTML=`<span class='number'>${String(i+1).padStart(2,'0')}</span><canvas width='32' height='32'></canvas><h2>${c.label}</h2><code>${c.id}</code>`;document.getElementById('grid').appendChild(a);const image=new Image();image.src='../assets/skins/mikan/frames/'+c.id+'.png';return{...c,image,ctx:a.querySelector('canvas').getContext('2d')};});document.getElementById('pause').onclick=e=>{paused=!paused;e.target.textContent=paused?'继续播放':'暂停动画'};document.getElementById('bg').onchange=e=>document.querySelectorAll('article').forEach(a=>a.style.background=e.target.value);function draw(now){if(!paused)t+=Math.min(100,now-(last||now))*Number(document.getElementById('speed').value);last=now;items.forEach(c=>{if(c.image.complete&&c.image.naturalWidth){let f=Math.floor(t/100)%c.frames;c.ctx.clearRect(0,0,32,32);c.ctx.drawImage(c.image,f*32,0,32,32,0,0,32,32)}});requestAnimationFrame(draw)}requestAnimationFrame(draw);
</script></html>";
            html=html.Replace("let f=Math.floor(t/100)%c.frames;","const total=c.durations.reduce((a,b)=>a+b,0);let ms=t%(total+(c.playback==='once'?800:0));ms=Math.min(total-1,ms);let f=0;while(f<c.frames-1&&ms>=c.durations[f])ms-=c.durations[f++];");
            html=html.Replace("VOL. 01","MIKAN II").Replace("第一套动作包","逐帧重绘动作包").Replace("独立部件、关键帧与变形器共同组成","独立视角与运动姿态，逐帧绘制、逐帧定时").Replace("27 ANIMATIONS",skin.clips.Count+" ANIMATIONS");
            File.WriteAllText(path,html.Replace("__DATA__",data.Replace("<","\\u003c").Replace(">","\\u003e")),new UTF8Encoding(false));
        }
        static void ExportMotionStudy(string folder,Skin skin,Animator anim,List<Color> palette){
            string[] ids={"run","pounce","chase_tail"};string[] names={"奔跑 / 接地、压缩、收腿、腾空、伸展、落地","扑击 / 蓄力、蹬地、飞行、伸爪、落地缓冲","追尾 / 侧面、后侧、背面、另一侧、前侧、正面"};
            foreach(string id in ids){
                var drawings=new List<Bitmap>();var timing=new List<int>();var clip=skin.clips[id];
                try{
                    var times=Animator.ExportTimeline(skin,clip);
                    for(int i=0;i<times.Count-1;i++)using(var frame=anim.RenderAt(id,times[i]/1000.0+.000001,false)){drawings.Add(Animator.Scale(frame,4));timing.Add(Math.Max(1,(times[i+1]-times[i])/10));}
                    if(clip.playback=="once")timing[timing.Count-1]+=70;
                    WriteGif(Path.Combine(folder,id+"-preview.gif"),drawings,timing,palette);
                }finally{foreach(var image in drawings)image.Dispose();}
            }
            using(var sheet=new Bitmap(896,540))using(var g=Graphics.FromImage(sheet))using(var font=new Font("Microsoft YaHei UI",10))using(var title=new Font("Microsoft YaHei UI",19,FontStyle.Bold))using(var brush=new SolidBrush(Color.FromArgb(62,76,61))){
                g.Clear(Color.FromArgb(246,244,236));g.DrawString("蜜柑 II · 独立姿态与运动原画",title,brush,18,12);
                for(int row=0;row<ids.Length;row++){
                    var clip=skin.clips[ids[row]];int top=60+row*158;g.DrawString(names[row],font,brush,16,top);
                    for(int i=0;i<clip.frames.Count;i++){
                        int x=i*112;using(var fill=new SolidBrush(i%2==0?Color.FromArgb(231,235,220):Color.FromArgb(238,235,224)))g.FillRectangle(fill,x+5,top+27,104,126);
                        using(var frame=anim.RenderAt(ids[row],Animator.FrameStartSeconds(clip,i)+.001,false))using(var scaled=Animator.Scale(frame,3))g.DrawImageUnscaled(scaled,x+8,top+28);
                        g.DrawString((i+1).ToString("00")+" / "+clip.frameDurations[i]+"ms",font,brush,x+9,top+127);
                    }
                }
                sheet.Save(Path.Combine(folder,"motion-keyframes.png"),ImageFormat.Png);
            }
            var frames=new List<Bitmap>();var delays=new List<int>();
            try{
                for(int i=0;i<100;i++)using(var composite=new Bitmap(96,32,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(composite)){
                    for(int col=0;col<ids.Length;col++){
                        var clip=skin.clips[ids[col]];double seconds=i*.05;
                        if(clip.playback=="once")seconds%=clip.seconds+.7;
                        using(var frame=anim.RenderAt(ids[col],seconds,false))g.DrawImageUnscaled(frame,col*32,0);
                    }
                    frames.Add(Animator.Scale(composite,4));delays.Add(5);
                }
                WriteGif(Path.Combine(folder,"locomotion-preview.gif"),frames,delays,palette);
            }finally{foreach(var frame in frames)frame.Dispose();}
        }
        // GIF89a with a fixed palette and frequent clear codes. The deliberately
        // simple literal LZW stream keeps this exporter dependency free.
        static void WriteGif(string path,List<Bitmap> frames,List<int> delays,List<Color> colors){
            colors=colors.Distinct().ToList();while(colors.Count<32)colors.Add(Color.Black);
            using(var writer=new BinaryWriter(File.Create(path))){
                writer.Write(Encoding.ASCII.GetBytes("GIF89a"));writer.Write((ushort)frames[0].Width);writer.Write((ushort)frames[0].Height);writer.Write((byte)0xF4);writer.Write((byte)0);writer.Write((byte)0);
                for(int i=0;i<32;i++){writer.Write(colors[i].R);writer.Write(colors[i].G);writer.Write(colors[i].B);}
                writer.Write(new byte[]{0x21,0xFF,11});writer.Write(Encoding.ASCII.GetBytes("NETSCAPE2.0"));writer.Write(new byte[]{3,1,0,0,0});
                for(int frameNumber=0;frameNumber<frames.Count;frameNumber++){
                    var frame=frames[frameNumber];writer.Write(new byte[]{0x21,0xF9,4,9});writer.Write((ushort)delays[frameNumber]);writer.Write((byte)0);writer.Write((byte)0);
                    writer.Write((byte)0x2C);writer.Write((ushort)0);writer.Write((ushort)0);writer.Write((ushort)frame.Width);writer.Write((ushort)frame.Height);writer.Write((byte)0);writer.Write((byte)5);
                    var bytes=new List<byte>();int buffer=0,bits=0,literals=0;
                    Action<int> code=n=>{buffer|=n<<bits;bits+=6;while(bits>=8){bytes.Add((byte)(buffer&255));buffer>>=8;bits-=8;}};code(32);
                    for(int y=0;y<frame.Height;y++)for(int x=0;x<frame.Width;x++){
                        if(literals==16){code(32);literals=0;}
                        var pixel=frame.GetPixel(x,y);int index=0;if(pixel.A>0){index=colors.FindIndex(1,c=>c.ToArgb()==pixel.ToArgb());if(index<0)index=1;}
                        code(index);literals++;
                    }
                    code(33);if(bits>0)bytes.Add((byte)buffer);
                    for(int offset=0;offset<bytes.Count;offset+=255){int length=Math.Min(255,bytes.Count-offset);writer.Write((byte)length);writer.Write(bytes.GetRange(offset,length).ToArray());}writer.Write((byte)0);
                }
                writer.Write((byte)0x3B);
            }
        }
    }
}
