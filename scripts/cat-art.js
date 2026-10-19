/* Mikan II: purpose-drawn silhouettes on the final 32px grid.
   Views, gait phases and action poses are drawings, never rotated/resized cats. */
const {Canvas,palette}=require('./pixel-canvas');
const sprites={}, canvases={};
function art(id,w,h,fn){let c=new Canvas(w,h);fn(c);canvases[id]=c;sprites[id]=c.data();return id;}
function stage(id,fn){return art(id,32,32,fn)}
function mirror(id,source){const c=canvases[source].mirror();canvases[id]=c;sprites[id]=c.data();return id;}
function eye(c,x,y,mood,wide=false){
  if(['closed','sleep','smile','groom'].includes(mood)){c.p(x-1,y,'d').p(x,y+1,'o').p(x+1,y+1,'o').p(x+2,y,'d');}
  else if(mood==='half'){c.r(x,y+1,2,1,'o').p(x-1,y,'s');}
  else {c.r(x,y,2,wide?3:2,'o').p(x,y,'w');if(wide)c.p(x+1,y+2,'d');}
}
for(const mood of ['open','half','closed','smile','scared','yawn','groom','flat'])art('face_front_'+mood,16,16,c=>{
  const flat=mood==='flat',yawn=mood==='yawn';
  c.poly(flat?[[0,4],[1,3],[5,5],[10,5],[14,3],[15,4],[14,8],[15,10],[13,12],[10,13],[5,13],[2,12],[0,10],[1,8]]:
    [[1,2],[2,1],[5,4],[10,4],[13,1],[14,2],[14,7],[15,8],[14,11],[12,12],[10,yawn?14:13],[5,yawn?14:13],[2,12],[0,10],[0,8],[1,6]],'f');
  c.inside(k=>{k.poly([[2,6],[6,5],[10,5],[13,6],[14,9],[11,10],[4,10],[1,9]],'l');k.r(5,5,1,2,'s').r(8,4,1,3,'s').r(11,5,1,2,'s');k.r(1,9,2,1,'s').r(13,9,2,1,'s');k.poly([[4,9],[6,9],[7,10],[9,9],[11,9],[12,11],[10,yawn?14:12],[5,yawn?14:12],[3,11]],'c');});
  if(!flat)c.p(2,3,'p').r(3,4,2,1,'r').p(13,3,'p').p(12,4,'r');
  c.outline();
  eye(c,4,8,mood,mood==='scared');eye(c,11,8,mood,mood==='scared');
  c.p(7,10,'r').p(8,10,'p');
  if(yawn)c.oval(6,10,4,5,'o').r(7,13,2,1,'p');
  else if(mood==='groom')c.p(7,11,'o').r(6,12,2,1,'p');
  else c.p(7,11,'d').p(6,12,'s').p(8,12,'s');
});
for(const mood of ['open','half','closed','curious','smile','flat'])art('face_quarter_'+mood,16,16,c=>{
  // Same round 16px skull, eye clusters, pink button nose and forehead M as
  // the reference front face; perspective shifts features, not identity.
  c.poly(mood==='flat'?[[1,4],[2,3],[6,5],[10,5],[14,3],[15,4],[14,8],[15,10],[13,12],[10,13],[5,13],[2,12],[0,10],[1,8]]:
    [[2,2],[3,1],[6,4],[10,4],[13,1],[14,2],[14,7],[15,8],[14,11],[12,12],[10,13],[5,13],[2,12],[0,10],[0,8],[1,6]],'f');
  c.inside(k=>{
    k.poly([[2,6],[6,5],[10,5],[13,6],[14,9],[11,10],[4,10],[1,9]],'l');
    k.r(5,5,1,2,'s').r(8,4,1,3,'s').r(11,5,1,2,'s').r(1,9,2,1,'s').r(13,9,2,1,'s');
    k.poly([[2,9],[5,9],[7,10],[10,10],[11,12],[8,13],[4,13],[1,11]],'c');
  });
  if(mood!=='flat')c.p(3,3,'p').p(4,4,'r').p(13,3,'p').p(12,4,'r');c.outline();
  eye(c,3,8,mood);eye(c,10,8,mood);
  c.p(5,10,'r').p(6,10,'p').p(5,11,'d').p(4,12,'s').p(6,12,'s');
  if(mood==='curious')c.p(3,6,'s');
});
art('face_tilt',16,16,c=>{
  c.poly([[2,2],[3,1],[6,5],[10,6],[14,4],[15,5],[14,9],[14,12],[11,14],[5,13],[2,11],[1,8],[2,6]],'f');
  c.inside(k=>{k.poly([[3,6],[7,6],[11,7],[13,9],[12,11],[7,11],[3,9]],'l');k.poly([[4,9],[7,10],[11,11],[12,13],[8,13],[4,12],[2,10]],'c');k.p(7,6,'s').p(7,7,'s').p(10,7,'s').p(10,8,'s').p(2,9,'s').p(13,11,'s');});
  c.p(3,3,'p').p(4,4,'r').p(13,6,'p').outline();eye(c,4,8,'open');eye(c,11,10,'open');c.p(7,11,'r').p(7,12,'d').p(6,12,'s').p(8,13,'s');
});
mirror('face_tilt_other','face_tilt');
function profileFace(c,mood){
  // 15px height fits even the compressed pounce at y=17, without shrinking
  // the eye or the cheek compared with the default seated kitten.
  c.poly([[3,6],[4,4],[5,1],[6,2],[8,5],[10,4],[13,1],[14,3],[14,7],[15,9],[14,11],[12,12],[10,13],[5,13],[2,12],[1,10],[2,8]],'f');
  c.inside(k=>{
    k.poly([[4,6],[7,5],[11,5],[13,7],[14,9],[11,10],[6,10],[2,9]],'l');
    k.r(7,5,1,2,'s').r(10,5,1,2,'s').r(12,8,3,1,'s').r(13,10,2,1,'s');
    k.poly([[2,9],[5,9],[7,10],[11,10],[11,12],[8,13],[4,13],[1,11]],'c');
  });
  c.p(5,3,'p').p(6,4,'r').p(13,3,'p').p(12,4,'r').outline();
  eye(c,5,8,mood,mood==='scared');c.p(2,10,'r').p(3,10,'p').p(4,11,'d').p(3,12,'s').p(5,12,'s');
  if(mood==='groom')c.r(3,13,2,1,'p');
}
for(const mood of ['open','half','closed','scared','groom'])art('face_side_'+mood,16,15,c=>profileFace(c,mood));
for(const mood of ['open','half','closed'])art('face_seated_side_'+mood,16,16,c=>profileFace(c,mood));
for(const tongue of [false,true])art(tongue?'face_down_tongue':'face_down',16,15,c=>{
  c.poly([[3,3],[4,1],[7,4],[11,3],[12,2],[14,5],[15,8],[14,11],[11,13],[6,14],[2,13],[1,11],[2,8],[2,5]],'f');
  c.inside(k=>{
    k.poly([[4,6],[8,5],[12,5],[14,8],[12,10],[8,11],[3,10]],'l');
    k.r(6,5,1,2,'s').r(9,5,1,2,'s').r(12,6,1,2,'s').r(13,10,2,1,'s');
    k.poly([[2,10],[5,10],[7,11],[10,11],[10,13],[6,14],[3,13],[1,11]],'c');
  });
  c.p(4,3,'p').p(5,4,'r').p(12,4,'p').outline();eye(c,4,9,'open');eye(c,10,8,'open');
  c.p(3,11,'r').p(4,11,'p').p(4,12,'d').p(3,13,'s').p(5,13,'s');if(tongue)c.r(3,14,2,1,'p');
});
art('face_back',16,15,c=>{
  c.poly([[1,2],[2,1],[5,4],[10,4],[13,1],[14,2],[14,7],[15,9],[13,12],[10,13],[5,13],[2,12],[0,9],[1,6]],'f');
  c.inside(k=>{k.poly([[2,5],[6,5],[8,4],[11,5],[13,7],[12,10],[9,12],[5,12],[2,10]],'l');k.r(4,4,1,3,'s').r(7,5,1,3,'s').r(10,4,1,3,'s').r(1,9,3,1,'s').r(12,9,3,1,'s');});c.outline();
});
art('face_back_quarter',15,15,c=>{
  c.poly([[2,3],[3,1],[6,4],[9,4],[11,1],[12,2],[13,7],[14,9],[12,12],[9,13],[4,12],[1,10],[1,6]],'f');
  c.inside(k=>{k.poly([[4,5],[8,5],[11,5],[12,8],[10,11],[5,12],[2,9]],'l');k.r(4,5,1,3,'s').r(8,5,1,3,'s').r(11,8,2,1,'s');});c.outline();
});
// Tail arcs are individually redrawn, with a cream tip and tabby bands.
for(let i=0;i<4;i++)stage('tail_sit_'+i,c=>{
  const pts=[[[21,26],[26,25],[28,22],[28,18],[27,16]],[[21,26],[26,25],[28,22],[29,19],[29,17]],[[21,26],[26,25],[29,23],[30,21],[30,19]],[[21,26],[26,25],[28,23],[29,20],[28,18]]][i];
  c.path(pts,'f',2).outline();c.p(25,25,'s').p(26,24,'s');const end=pts[pts.length-1];c.p(end[0],end[1],'c').p(end[0]-1,end[1],'c');
});
for(let breath=0;breath<2;breath++)stage('torso_sit_'+breath,c=>{
  c.poly([[13,17],[19,17],[21,20],[21,24],[22,26],[21,28],[11,28],[10,26],[12,22]],'f');
  c.inside(k=>{k.poly([[13,18],[18,18],[19,20],[18,23+breath],[16,25],[14,24],[12,21]],'c');k.r(11,22,2,1,'s').r(20,22,2,1,'s').r(11,25,1,1,'s');k.r(12,24,3,4,'c').r(18,24,3,4,'c');k.p(15,26,'b').p(17,26,'b').p(12,28,'b').p(19,28,'b');});c.outline();
});
for(let phase=0;phase<3;phase++)stage('torso_groom_'+phase,c=>{
  c.stamp(canvases.torso_sit_0);c.r(11,26,5,3,'.');
  const pts=[[[13,24],[12,21],[11,19]],[[13,24],[12,20],[10,17]],[[13,24],[13,21],[12,19]]][phase];
  let paw=new Canvas();paw.path(pts,'f',2).r(pts[2][0]-1,pts[2][1],3,2,'c').outline();c.join(paw,pts[0]);
});
for(let i=0;i<2;i++)stage('torso_quarter_'+i,c=>{
  c.poly([[13,16],[19,16],[21,19],[23,24],[22,27],[20,28],[11,28],[10,26],[12,23],[12,20]],'f');
  c.inside(k=>{k.poly([[13,18],[16,18],[17,20],[15,24],[13,25],[12,23]],'c');k.poly([[18,19],[21,20],[22,24],[21,26],[18,26]],'l');k.r(19,20,3,1,'s').r(21,23,2,1,'s');k.r(11,25,4,3,'c').r(17,26+i,3,2-i,'c').p(14,25,'b').p(18,24,'s');});c.outline();
});
stage('torso_back',c=>{
  c.poly([[12,16],[20,16],[23,21],[24,26],[22,28],[10,28],[9,26],[10,21]],'f');
  c.inside(k=>{k.poly([[13,17],[19,17],[21,21],[22,25],[19,27],[13,27],[11,24]],'l');k.r(14,18,1,3,'s').r(18,18,1,3,'s').r(10,23,3,1,'s').r(20,23,3,1,'s').r(11,27,3,1,'b').r(20,27,2,1,'b');});c.outline();
});
stage('torso_side_sit',c=>{
  c.poly([[13,17],[18,16],[21,18],[22,22],[22,26],[21,28],[11,28],[10,26],[12,24],[12,20]],'f');
  c.inside(k=>{k.poly([[13,18],[16,18],[17,21],[15,24],[13,25],[12,24]],'c');k.oval(18,21,4,6,'l').path([[19,20],[21,21],[21,23]],'s').r(11,25,3,3,'c').r(18,27,4,1,'c').p(14,25,'b').p(17,26,'s');}).outline();
});
stage('torso_back_quarter',c=>{
  c.poly([[12,16],[18,16],[22,19],[24,24],[22,28],[12,28],[9,25],[10,20]],'f');
  c.inside(k=>{k.poly([[12,18],[17,17],[21,20],[22,24],[19,27],[13,26],[11,23]],'l');k.r(14,18,1,3,'s').r(18,18,1,3,'s').r(21,22,2,1,'s').r(11,25,3,1,'s');k.r(11,27,3,1,'b').r(20,27,2,1,'b');});c.outline();
});
function leg(c,points,near=true,openRoot=false){
  let q=new Canvas();q.path(points,near?'f':'s',2);
  if(points[0][0]>=19&&points.length>2){let [x,y]=points[0],[xx,yy]=points[1];q.poly([[x-2,y-1],[x+1,y-1],[xx+1,yy],[xx-1,yy+1],[x-2,y+1]],near?'f':'s');if(near)q.p(x-1,y,'l');}
  let end=points[points.length-1];q.r(end[0]-1,end[1],3,1,near?'c':'b').outline(near?'o':'d');
  if(openRoot){
    // The underlying torso already supplies the shoulder. Leave its attachment
    // open in this foreground paw layer so it cannot reintroduce a black cuff.
    for(let y=0;y<32;y++)for(let x=0;x<32;x++){
      const distance=Math.hypot(x-points[0][0],y-points[0][1]);
      if(distance<=2||(distance<=4&&(q.a[y][x]==='o'||q.a[y][x]==='d')))q.p(x,y,'.');
    }
    c.stamp(q);
  }else if(near)c.join(q,points[0]);else c.stamp(q);
}
function motionTail(c,points){
  const tail=new Canvas(),root=points[0],bend=points[1],tip=points[points.length-1],band=points[points.length-2];
  tail.path(points,'f',2).line(...root,...bend,'f',3);
  tail.inside(k=>k.line(root[0]-1,root[1]-1,bend[0]-1,bend[1]-1,'l').r(band[0]-1,band[1],2,1,'s'));
  tail.outline().p(tip[0],tip[1],'c').p(tip[0]-1,tip[1],'c');c.stamp(tail);
}
const gait=[
 {spine:[[10,17],[13,15],[16,15],[19,13],[22,14],[25,17],[25,20],[23,22],[20,22],[17,21],[14,22],[10,20]],far:[[[14,21],[15,25],[17,26]],[[22,21],[20,24],[19,26]]],near:[[[12,20],[9,24],[7,26]],[[23,20],[25,23],[28,25]]],head:[2,10],tail:[[23,17],[26,16],[28,14],[28,12],[27,11]]},
 {spine:[[10,18],[14,16],[17,16],[20,14],[23,16],[25,19],[24,22],[22,23],[18,22],[14,23],[10,21]],far:[[[13,21],[12,25],[11,27]],[[23,22],[24,25],[26,26]]],near:[[[12,21],[12,25],[10,27]],[[23,21],[21,24],[23,27]]],head:[2,11],tail:[[24,18],[27,17],[29,15],[29,13],[28,12]]},
 {spine:[[10,17],[14,15],[17,15],[20,13],[23,14],[25,17],[24,20],[22,22],[18,21],[14,22],[10,20]],far:[[[12,21],[10,23],[11,25]],[[23,21],[25,22],[25,24]]],near:[[[13,20],[16,22],[14,25]],[[23,20],[20,22],[18,24]]],head:[2,10],tail:[[24,17],[27,15],[28,13],[28,11],[27,10]]},
 {spine:[[9,16],[14,14],[17,14],[20,13],[23,14],[25,17],[23,20],[20,21],[17,20],[13,21],[9,19]],far:[[[12,20],[10,23],[9,24]],[[22,20],[22,23],[21,25]]],near:[[[11,19],[7,20],[5,23]],[[23,19],[26,20],[26,23]]],head:[1,9],tail:[[24,16],[26,14],[26,11],[25,10],[24,11]]},
 {spine:[[9,16],[14,14],[18,13],[22,14],[25,16],[24,19],[21,21],[17,20],[12,21],[8,19]],far:[[[11,20],[7,21],[5,23]],[[22,19],[25,21],[28,23]]],near:[[[11,19],[6,20],[3,22]],[[23,19],[26,22],[29,24]]],head:[1,9],tail:[[24,16],[27,14],[29,13],[29,11],[28,10]]},
 {spine:[[10,17],[14,15],[17,15],[20,14],[23,15],[25,18],[24,21],[22,22],[18,21],[14,22],[10,20]],far:[[[13,21],[11,24],[10,26]],[[22,21],[22,24],[24,26]]],near:[[[12,20],[8,23],[6,26]],[[23,20],[26,23],[27,26]]],head:[2,10],tail:[[24,17],[27,16],[29,14],[29,12],[28,11]]}
];
function tailVariant(base,phase){return phase===0?base:base+'_tail_'+phase;}
function swayTail(points,phase){const amount=[0,1,0,-1][phase];return points.map(([x,y],j)=>j<2?[x,y]:[Math.max(1,Math.min(30,x+amount)),y+(phase===2?1:0)]);}
for(let i=0;i<gait.length;i++)for(let phase=0;phase<4;phase++)stage(tailVariant('gait_'+i,phase),c=>{
  let g=gait[i];motionTail(c,swayTail(g.tail,phase));
  g.far.forEach(p=>leg(c,p,false));let torso=new Canvas();torso.poly(g.spine,'f').inside(k=>{
    const top=Math.min(...g.spine.map(p=>p[1]));k.poly([[12,top+1],[18,top],[23,top+2],[23,top+4],[16,top+4],[11,top+3]],'l');
    k.poly([[10,top+4],[16,top+5],[21,top+5],[21,top+7],[14,top+8],[10,top+6]],'c');
    k.path([[17,top+1],[17,top+3]],'s').path([[21,top+2],[22,top+4]],'s');
  }).outline();c.stamp(torso);g.near.forEach(p=>leg(c,p,true));
});
// Pounce: anticipation, compression, push-off, flight, reach, impact, settle.
const pounces=[
 {body:[[10,23],[14,20],[21,19],[25,21],[25,25],[22,27],[12,27],[9,25]],legs:[[[11,25],[7,27]],[[23,25],[25,27]]],head:[2,15],tail:[[24,22],[27,19],[27,16]]},
 {body:[[10,24],[15,22],[22,21],[25,23],[24,27],[12,28],[9,26]],legs:[[[11,26],[6,28]],[[22,25],[24,28]]],head:[2,17],tail:[[24,23],[28,21],[29,18]]},
 {body:[[10,18],[15,16],[21,17],[25,19],[23,22],[15,23],[9,21]],legs:[[[11,20],[6,19],[4,20]],[[22,20],[26,24],[28,25]]],head:[1,11],tail:[[24,18],[28,17],[30,14]]},
 {body:[[10,14],[16,13],[22,14],[25,16],[23,19],[14,20],[9,18]],legs:[[[11,18],[7,20],[2,20]],[[22,17],[26,20],[29,21]]],head:[4,7],tail:[[24,15],[28,13],[30,11]]},
 {body:[[10,16],[16,15],[23,16],[25,19],[22,21],[13,21],[8,19]],legs:[[[10,20],[6,22],[2,23]],[[22,19],[26,22],[29,23]]],head:[4,9],tail:[[24,17],[28,15],[29,12]]},
 {body:[[10,21],[15,19],[22,19],[25,21],[24,25],[15,26],[9,24]],legs:[[[11,24],[6,26],[3,27]],[[23,23],[25,26],[27,27]]],head:[1,14],tail:[[24,21],[27,18],[27,15]]},
 {body:[[10,24],[15,22],[22,22],[25,24],[23,27],[12,28],[9,26]],legs:[[[11,26],[5,28]],[[23,25],[25,28]]],head:[2,17],tail:[[24,24],[28,21],[29,18]]},
 {body:[[11,22],[15,20],[22,20],[25,23],[23,27],[12,28],[10,26]],legs:[[[12,25],[8,28]],[[23,25],[25,28]]],head:[3,15],tail:[[24,23],[27,20],[28,17]]}
];
for(let i=0;i<pounces.length;i++)for(let phase=0;phase<4;phase++)stage(tailVariant('pounce_'+i,phase),c=>{
  const p=pounces[i],end=p.tail[p.tail.length-1];motionTail(c,swayTail(p.tail.concat([[end[0]-1,end[1]-1]]),phase));
  let torso=new Canvas();torso.poly(p.body,'f').inside(k=>{k.poly([[12,p.body[1][1]+1],[19,p.body[1][1]],[24,p.body[2][1]+2],[21,p.body[2][1]+4],[12,p.body[1][1]+4]],'l');k.r(18,p.body[1][1]+1,1,3,'s').r(22,p.body[2][1]+1,1,3,'s');}).outline();c.stamp(torso);p.legs.forEach(p=>leg(c,p));
});
for(let i=0;i<pounces.length;i++)stage('pounce_paws_'+i,c=>leg(c,pounces[i].legs[0],true,true));
stage('turn_front',c=>{
  c.poly([[12,18],[20,18],[22,21],[21,25],[18,27],[13,27],[10,24]],'f').inside(k=>k.poly([[13,19],[19,19],[19,23],[17,25],[13,24]],'c').r(11,22,2,1,'s').r(20,22,2,1,'s')).outline();
  leg(c,[[13,23],[10,25],[11,27]]);leg(c,[[19,23],[22,26],[22,28]]);
});
stage('turn_back',c=>{
  c.poly([[12,17],[20,17],[22,21],[22,25],[19,27],[13,27],[10,24]],'f').inside(k=>k.poly([[13,18],[19,18],[20,22],[19,25],[13,25],[11,22]],'l').r(14,19,1,3,'s').r(18,19,1,3,'s').r(10,23,3,1,'s').r(20,23,3,1,'s')).outline();
  leg(c,[[13,24],[11,26],[10,28]],false);leg(c,[[19,24],[21,27],[22,29]]);
});
for(let i=0;i<3;i++)for(let phase=0;phase<4;phase++)stage(tailVariant('arch_'+i,phase),c=>{
  const dx=[0,1,0,-1][phase],dy=phase===2?1:0;
  let tail=new Canvas();tail.poly([[24,20],[25,15],[24+dx,12+dy],[25+dx,9+dy],[27+dx,10+dy],[28,13],[28,18],[27,21]],'f').inside(k=>k.r(25+dx,10+dy,2,2,'c').r(26,15,2,1,'s')).outline();c.stamp(tail);
  leg(c,[[12,21],[10,26],[9,28]],false);leg(c,[[22,21],[24,26],[25,28]],false);
  let body=new Canvas();body.poly([[10,20],[12,16],[14,14-i],[16,14-i],[18,12-i],[20,13-i],[23,14],[25,18],[24,22],[19,24],[14,23]],'f');
  body.inside(k=>{k.poly([[13,17],[16,14-i],[20,14-i],[23,17],[21,20],[15,21]],'l');k.r(17,14-i,1,3,'s').r(21,15,1,3,'s').r(14,21,8,2,'c');}).outline();c.stamp(body);
  leg(c,[[12,21],[12,26],[10,28]]);leg(c,[[23,21],[23,26],[25,28]]);
});
for(let i=0;i<2;i++)for(let phase=0;phase<4;phase++)stage(tailVariant('loaf_'+i,phase),c=>{
  // A lifted chest and a rounded back over fully tucked feet. No exposed
  // socks or horizontal white belly stripe: those read as a lying cat.
  c.poly([[12,18],[17,17],[21,18],[24,21],[25,24],[24,27],[21,28],[12,28],[9,26],[9,22]],'f');
  c.inside(k=>{
    k.poly([[14,19],[18,18],[21,19],[23,22],[23,25],[20,26],[16,25],[12,23]],'l');
    k.poly([[10,21],[13,20],[16,22],[16,25],[14,27],[11,26],[10,24]],'c');
    k.path([[20,20],[22,21],[22,22]],'s').r(23,24,2,1,'s');
    k.p(11,26,'b').p(14,27,'b').p(18,27,'s');
  }).outline();
  const tip=[[19,27],[18,27],[18,26],[19,26]][phase];
  let tail=new Canvas();tail.path([[24,24],[24,26],[22,27],tip],'f',2).p(21,27,'s').p(...tip,'c').outline();c.join(tail,[24,24]);
  if(i)c.p(18,21,'h');
});
for(let i=0;i<2;i++)stage('sleep_curl_'+i,c=>{
  c.poly([[9,22],[10,18],[14,16],[21,16],[25,18],[27,22],[26,26],[23,28],[12,28],[8,26]],'f');
  c.inside(k=>{k.poly([[12,19],[17,17],[22,18],[25,21],[24,24],[20,25],[13,24],[10,22]],'l');k.r(17,17,1,3,'s').r(21,18,1,3,'s').path([[24,21],[25,23]],'s');k.oval(12,22,10,5,'c');}).outline();
  if(i)c.p(25,23,'l');
});
for(let phase=0;phase<4;phase++)stage(tailVariant('sleep_wrap',phase),c=>{
  const tip=[[12,27],[11,27],[11,26],[12,26]][phase];
  c.path([[25,23],[25,26],[22,28],[16,28],tip],'f',2).outline().r(tip[0],tip[1]-1,3,2,'c').r(19,27,2,2,'s').p(24,26,'s');
  // Keep the wrapping tip in front of the face, but merge the tail root into
  // the rump's fur instead of drawing a dark closed cuff over the body.
  const base=canvases.sleep_curl_0,merged=new Canvas().stamp(base).join(c,[25,23]);
  for(let y=0;y<32;y++)for(let x=0;x<32;x++)if(Math.hypot(x-25,y-23)<=4)c.a[y][x]=merged.a[y][x]===base.a[y][x]?'.':merged.a[y][x];
});
for(let i=0;i<2;i++)for(let phase=0;phase<4;phase++)stage(tailVariant('sleep_long_'+i,phase),c=>{
  c.poly([[11,23],[15,20],[22,20],[26,23],[27,26],[24,28],[10,28],[8,26]],'f');
  c.inside(k=>{k.poly([[13,22],[18,21],[23,22],[25,24],[24,26],[14,26],[10,25]],'l');k.r(17,21,1,3,'s').r(21,21,1,3,'s').r(10,25,7,2,'c');}).outline();leg(c,[[12,26],[6-i,28]]);leg(c,[[24,25],[28,28]]);
  const tip=[[29,21],[30,20],[29,19],[28,20]][phase];
  let tail=new Canvas();tail.path([[25,24],[28,23],tip],'f',2).outline().p(...tip,'c');c.join(tail,[25,24]);
});
for(let i=0;i<3;i++)for(let phase=0;phase<4;phase++)stage(tailVariant('eat_body_'+i,phase),c=>{
  c.poly([[12,20],[16,18],[22,18],[25,21],[25,26],[22,28],[11,28],[10,25]],'f');
  c.inside(k=>{k.poly([[14,20],[18,19],[22,20],[23,23],[21,26],[14,25],[12,23]],'l');k.r(17,19,1,3,'s').r(21,20,1,3,'s').r(11,25,3,3,'c').r(22,26,3,2,'c');}).outline();
  const tip=[[27,16],[28,16],[29,17],[28,17]][phase];
  let tail=new Canvas();tail.path([[24,23],[27,21],[28,18],tip],'f',2).outline().p(...tip,'c');c.join(tail,[24,23]);if(i===1)c.p(21,23,'h');
});
for(let i=0;i<4;i++)stage('play_body_'+i,c=>{
  c.poly([[13,18],[19,17],[22,19],[24,24],[23,27],[20,28],[12,28],[11,26],[12,22]],'f');
  c.inside(k=>{
    k.poly([[13,19],[16,19],[17,21],[16,24],[14,26],[12,25]],'c');
    k.poly([[19,19],[22,21],[23,24],[21,26],[18,25]],'l');
    k.r(20,20,2,1,'s').r(22,23,2,1,'s').p(20,27,'b');
  }).outline();
  leg(c,[[19,23],[18,25],[18,28]]);
  if(i===1)c.p(21,24,'h');
});
// A separate foreground foreleg keeps every white paw visible in front of
// the cheek and yarn. Draw the bend and tip anew for each phase of the swat.
const playPaws=[[[14,23],[13,25],[11,26]],[[14,23],[11,24],[8,25]],[[14,23],[11,26],[7,24]],[[14,23],[13,26],[11,28]]];
for(let i=0;i<playPaws.length;i++)stage('play_paw_'+i,c=>{
  const points=playPaws[i],tip=points[points.length-1];
  c.path(points,'f',2).r(tip[0]-1,tip[1]-1,3,2,'c').outline().p(tip[0],tip[1]+1,'b');
  // Open the shoulder into the chest without losing the foreleg silhouette.
  for(let y=21;y<=24;y++)for(let x=12;x<=16;x++)if(Math.hypot(x-14,y-23)<=2)c.p(x,y,'.');
});
for(let i=0;i<3;i++)art('yarn_'+i,8,8,c=>{
  c.oval(1,1,6,6,'g').inside(k=>{k.path(i===1?[[2,2],[5,5],[2,6]]:[[2,3],[4,2],[6,4]],'t');k.path(i===2?[[2,2],[3,5],[5,6]]:[[3,2],[2,5],[5,5]],'v');}).outline();c.p(7,7,'g');
});
for(const water of [false,true])for(let i=0;i<3;i++)art((water?'water_':'food_')+i,11,6,c=>{
  c.poly([[1,2],[9,2],[8,5],[2,5]],water?'n':'g').r(1,1,9,2,'o').r(2,1,7,1,water?'u':'s').r(3,3,5,1,water?'u':'t');
  if(water)c.r(3+i%2,1,3,1,'w');else c.p(3,1,'h').p(6+i%2,1,'b');c.outline();
});
for(let i=0;i<2;i++)stage('keyboard_cat_'+i,c=>{
  c.stamp(canvases.torso_quarter_0);leg(c,[[14,22],[12,25-i]]);leg(c,[[19,23],[17,26+i]]);
});
for(let i=0;i<2;i++)art('laptop_'+i,17,12,c=>{
  c.poly([[1,1],[14,1],[15,9],[16,10],[15,11],[0,11],[0,10],[2,9]],'o');c.r(2,2,12,6,'v').r(3,3,10,4,'g').r(4,4,3,1,'t').r(5,6,6,1,'t').p(11,5+i,'t').r(2,9,12,1,'b').r(5,10,5,1,'s');
});
for(let i=0;i<3;i++)art('screen_'+i,13,13,c=>{
  c.r(0,1,13,9,'o').r(1,2,11,7,'n').r(2,3,9,5,'u').r(6,10,2,2,'o').r(3,12,8,1,'o');c.poly([[5+i%2,4],[5+i%2,6],[8+i%2,5]],'c');
});
for(let i=0;i<3;i++)art('book_'+i,16,10,c=>{
  c.poly([[0,1],[6,0],[8,2],[10,0],[15,1],[15,9],[10,8],[8,9],[6,8],[0,9]],'o').poly([[1,2],[6,1],[7,3],[7,8],[6,7],[1,8]],'c').poly([[9,3],[10,1],[14,2],[14,8],[10,7],[9,8]],'b');
  c.r(2,3,4,1,'s').r(2,5,3,1,'s').r(10,3,3,1,'s').r(10,5,3,1,'s');if(i===1)c.path([[8,2],[11,1],[11,6],[8,8]],'c');if(i===2)c.path([[7,2],[5,1],[5,6],[7,8]],'h');
});
for(let i=0;i<3;i++)art('controller_'+i,14,9,c=>{
  c.poly([[3,1],[10,1],[12,3],[13,6],[12,8],[10,8],[8,6],[5,6],[3,8],[1,8],[0,6],[1,3]],'o').poly([[3,2],[10,2],[11,4],[12,6],[11,7],[9,5],[4,5],[2,7],[1,6],[2,4]],'g');c.r(3,3,1,3,'v').r(2,4,3,1,'v').p(9,3,i===1?'w':'p').p(11,4,i===2?'w':'y').p(6,3,'v');
});
for(let i=0;i<3;i++)art('hand_'+i,5,4,c=>{c.r(1,1,3,2,'c').p(0,2,'b').p(2,3,'b').outline();if(i===1)c.p(1,1,'h');});
stage('drag_body',c=>{
  c.poly([[12,16],[20,16],[21,20],[20,24],[21,28],[19,29],[17,27],[16,24],[15,28],[13,29],[11,28],[12,24],[10,20]],'f');c.inside(k=>k.r(13,18,6,5,'c').r(12,26,3,3,'c').r(18,27,3,2,'c').r(11,20,2,1,'s').r(19,20,2,1,'s')).outline();
});
art('question',6,9,c=>c.path([[1,1],[3,0],[5,1],[5,3],[3,4],[3,5]],'o').p(3,8,'o').p(2,1,'y'));
art('heart',7,7,c=>c.poly([[0,2],[1,1],[2,1],[3,2],[4,1],[5,1],[6,2],[6,4],[3,6],[0,4]],'r').poly([[1,2],[2,2],[3,3],[4,2],[5,2],[5,4],[3,5],[1,4]],'p').p(1,2,'c'));
art('sleep_z',7,9,c=>c.path([[2,0],[6,0],[2,4],[6,4]],'n').path([[0,6],[3,6],[0,8],[3,8]],'u'));
art('alert',3,8,c=>c.r(1,0,1,5,'r').p(1,7,'r'));
art('drop',3,5,c=>c.poly([[1,0],[2,3],[1,4],[0,3]],'u').p(1,2,'w'));
art('spark',5,5,c=>c.r(2,0,1,5,'y').r(0,2,5,1,'y'));
for(let i=0;i<4;i++)stage('tail_keyboard_'+i,c=>{
  const tip=[[25,16],[26,17],[27,18],[26,18]][i];motionTail(c,[[21,26],[24,24],[26,21],tip]);
});
for(let i=0;i<4;i++)stage('tail_drag_'+i,c=>{
  const tip=[[26,28],[27,27],[27,26],[25,27]][i];motionTail(c,[[20,22],[23,24],[24,27],tip]);
});
for(const source of ['face_side_open','face_side_half','face_side_closed','face_quarter_open','face_quarter_closed','face_back_quarter','torso_quarter_0','torso_back_quarter','gait_2',...Array.from({length:3},(_,i)=>'gait_2_tail_'+(i+1)),...Array.from({length:4},(_,i)=>'tail_sit_'+i)])mirror(source+'_right',source);
require('./cat-extra-art')({art,stage,canvases,leg});
require('./cat-daily-art')({art,stage,canvases,leg});
require('./cat-companion-art')({art,stage,canvases,leg});
require('./cat-scene-art')({art,stage,canvases,leg});
const tailCycles={};
for(const id of Object.keys(sprites))if(sprites[id+'_tail_1'])tailCycles[id]=[id,id+'_tail_1',id+'_tail_2',id+'_tail_3',id+'_tail_2',id+'_tail_1'];
tailCycles.gait_2_right=['gait_2_right',...Array.from({length:3},(_,i)=>'gait_2_tail_'+(i+1)+'_right'),'gait_2_tail_2_right','gait_2_tail_1_right'];
for(let i=0;i<4;i++){
  tailCycles['tail_sit_'+i]=[0,1,2,3].map(n=>'tail_sit_'+n);
  tailCycles['tail_sit_'+i+'_right']=[0,1,2,3].map(n=>'tail_sit_'+n+'_right');
  tailCycles['tail_keyboard_'+i]=[0,1,2,3,2,1].map(n=>'tail_keyboard_'+n);
  tailCycles['tail_drag_'+i]=[0,1,2,3].map(n=>'tail_drag_'+n);
}
module.exports={sprites,canvases,palette,gait,pounces,tailCycles};
