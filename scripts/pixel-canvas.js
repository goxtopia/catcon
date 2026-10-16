/* Native-resolution pixel tools. No resampling or rotated bitmap artwork. */
class Canvas {
  constructor(w=32,h=32){this.w=w;this.h=h;this.a=Array.from({length:h},()=>Array(w).fill('.'));}
  p(x,y,c){if(x>=0&&x<this.w&&y>=0&&y<this.h)this.a[y][x]=c;return this;}
  r(x,y,w,h,c){for(let j=y;j<y+h;j++)for(let i=x;i<x+w;i++)this.p(i,j,c);return this;}
  line(x,y,xx,yy,c,width=1){let dx=Math.abs(xx-x),sx=x<xx?1:-1,dy=-Math.abs(yy-y),sy=y<yy?1:-1,e=dx+dy;for(;;){this.r(x-Math.floor(width/2),y-Math.floor(width/2),width,width,c);if(x===xx&&y===yy)break;let ee=2*e;if(ee>=dy){e+=dy;x+=sx}if(ee<=dx){e+=dx;y+=sy}}return this;}
  path(points,c,width=1){for(let i=1;i<points.length;i++)this.line(...points[i-1],...points[i],c,width);return this;}
  poly(points,c){
    for(let y=0;y<this.h;y++)for(let x=0;x<this.w;x++){let inside=false;for(let i=0,j=points.length-1;i<points.length;j=i++){let [a,b]=points[i],[d,e]=points[j];if((b>y)!=(e>y)&&x<(d-a)*(y-b)/(e-b)+a)inside=!inside;}if(inside)this.p(x,y,c);}
    return this.path(points.concat([points[0]]),c);
  }
  oval(x,y,w,h,c){for(let j=y;j<y+h;j++)for(let i=x;i<x+w;i++)if(((i-x+.5)*2/w-1)**2+((j-y+.5)*2/h-1)**2<=1)this.p(i,j,c);return this;}
  outline(c='o'){let old=this.a.map(r=>r.slice());for(let y=0;y<this.h;y++)for(let x=0;x<this.w;x++)if(old[y][x]==='.'&&[[0,1],[0,-1],[1,0],[-1,0]].some(([dx,dy])=>old[y+dy]&&old[y+dy][x+dx]&&old[y+dy][x+dx]!=='.'))this.p(x,y,c);return this;}
  inside(fn){let old=this.a.map(r=>r.slice());fn(this);for(let y=0;y<this.h;y++)for(let x=0;x<this.w;x++)if(old[y][x]==='.')this.a[y][x]='.';return this;}
  stamp(source,x=0,y=0){for(let j=0;j<source.h;j++)for(let i=0;i<source.w;i++)if(source.a[j][i]!=='.')this.p(x+i,y+j,source.a[j][i]);return this;}
  // Attach a limb as part of the same fur surface. Preserve the outer contour,
  // but let the chest/hip colours flow through an overlapping root instead of
  // stamping a closed dark outline on top of the torso.
  join(source,root){
    const old=this.a.map(r=>r.slice()),fur=c=>c&&c!=='.'&&c!=='o'&&c!=='d';
    for(let y=0;y<this.h;y++)for(let x=0;x<this.w;x++){
      const incoming=source.a[y]?.[x];if(!incoming||incoming==='.')continue;
      const under=old[y][x],distance=Math.hypot(x-root[0],y-root[1]);
      if((incoming==='o'||incoming==='d')&&fur(under)){
        if(distance>4)this.p(x,y,under==='c'||under==='b'?'b':'s');
        continue;
      }
      if(fur(incoming)&&fur(under)&&distance<=4)continue;
      if(fur(incoming)&&(under==='o'||under==='d')&&distance<=3){
        const neighbours=[[0,-1],[1,-1],[-1,-1],[1,0],[-1,0]].map(([dx,dy])=>old[y+dy]?.[x+dx]).filter(fur);
        this.p(x,y,neighbours[0]||incoming);continue;
      }
      this.p(x,y,incoming);
    }
    const merged=this.a.map(r=>r.slice());
    for(let y=0;y<this.h;y++)for(let x=0;x<this.w;x++)if((merged[y][x]==='o'||merged[y][x]==='d')&&Math.hypot(x-root[0],y-root[1])<=4){
      const neighbours=[[0,-1],[0,1],[-1,0],[1,0]].map(([dx,dy])=>merged[y+dy]?.[x+dx]);
      if(neighbours.filter(fur).length>=3)this.p(x,y,neighbours.find(c=>c==='c'||c==='b')||neighbours.find(fur));
    }
    return this;
  }
  mirror(){let c=new Canvas(this.w,this.h);c.a=this.a.map(r=>r.slice().reverse());return c;}
  data(){return {width:this.w,height:this.h,rows:this.a.map(r=>r.join(''))};}
}
const palette={'.':'#00000000',o:'#49372f',d:'#785038',s:'#b87540',f:'#e6a05b',l:'#ffc57b',h:'#ffda98',c:'#fff0d1',b:'#e8cea6',p:'#e9988d',r:'#b96963',w:'#fffaf0',g:'#89a88e',t:'#d1dfb6',v:'#526d60',u:'#9fc4cf',n:'#587b91',y:'#edd084'};
module.exports={Canvas,palette};
