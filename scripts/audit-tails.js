/* Guard against a tail that floats: composite every authored frame the way the
   runtime does and require the tail's visible pixels to touch the body. A tail
   rooted in mid-air, or with its root hidden behind a prop, shows up here. */
const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'..');
const skin=JSON.parse(fs.readFileSync(path.join(root,'assets/skins/mikan/skin.json'),'utf8'));
const rows=name=>Array.isArray(skin.sprites[name])?skin.sprites[name]:skin.sprites[name].rows;
/* Frames where the tail is deliberately out of sight or swapped at runtime. */
const known={'box_peek':[6],'keyboard_walk':[0,1,2,3,4,5,6,7,8,9,10,11,12,13]};
let checked=0;const floating=[];
for(const [id,clip] of Object.entries(skin.clips)){
  clip.frames.forEach((patch,index)=>{
    checked++;
    const layers=skin.poses[clip.pose].map(l=>{
      const override=patch[l.id]||{};
      const layer=Object.assign({},l);
      for(const key of ['sprite','x','y','visible'])if(key in override)layer[key]=override[key];
      return layer;
    }).sort((a,b)=>a.z-b.z);
    const owner=[];
    for(const layer of layers){
      if(!layer.visible||!skin.sprites[layer.sprite])continue;
      rows(layer.sprite).forEach((row,y)=>{
        for(let x=0;x<row.length;x++){
          if(row[x]==='.')continue;
          const gx=x+(layer.x||0),gy=y+(layer.y||0);
          if(gx<0||gx>31||gy<0||gy>31)continue;
          owner[gy*32+gx]=layer.id;
        }
      });
    }
    const tail=[],body=new Set();
    for(let i=0;i<32*32;i++){
      if(owner[i]==='tail')tail.push([i%32,Math.floor(i/32)]);
      else if(owner[i]==='body')body.add(i);
    }
    if(!tail.length||!body.size)return;
    const touches=tail.some(([x,y])=>[[1,0],[-1,0],[0,1],[0,-1]].some(([dx,dy])=>{
      const nx=x+dx,ny=y+dy;
      return nx>=0&&nx<32&&ny>=0&&ny<32&&body.has(ny*32+nx);
    }));
    if(!touches&&!(known[id]||[]).includes(index))floating.push(id+' frame '+index);
  });
}
if(floating.length){
  console.error('Floating tail: '+floating.join(', '));
  process.exit(1);
}
console.log(`Tail attachment verified: ${checked} frames, every visible tail touches the body.`);
