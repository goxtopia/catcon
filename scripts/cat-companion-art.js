/* Third daily-companion set: stretching, rolling over, chin-on-paws, waving and a sneeze.
   Bodies, limbs and props are drawn on the final 32px grid; faces reuse the
   established Mikan library so identity stays identical. */
module.exports=function({art,stage,leg}){
  const tail=(id,pts,band,extra)=>stage(id,c=>{
    const tip=pts[pts.length-1];
    c.path(pts,'f',2).outline();
    if(band)c.p(band[0],band[1],'s').p(band[0]+1,band[1],'s');
    c.p(tip[0],tip[1],'c').p(tip[0]-1,tip[1],'c');
    if(extra)extra(c,tip);
  });

  /* Stretch: low chest, arched back, forelegs reaching forward and the tail
     standing straight up. Everything is drawn on the body layer so the head
     only hides the shoulder join. */
  for(let i=0;i<3;i++)stage('stretch_body_'+i,c=>{
    const full=[[10,24],[13,20],[17,17],[21,14],[24,13],[27,15],[27,22],[26,27],[14,28],[10,26]];
    const peak=[[10,24],[13,20],[17,16],[21,13],[24,12],[27,14],[27,21],[26,27],[14,28],[10,26]];
    const lean=[[10,24],[13,21],[17,18],[21,16],[25,16],[27,18],[27,24],[25,28],[14,28],[10,26]];
    c.poly(i===0?lean:i===1?full:peak,'f');
    c.inside(k=>{
      k.poly(i===0?[[11,23],[15,20],[19,18],[23,17],[26,19],[23,22],[18,24],[13,25]]
        :[[11,23],[15,19],[19,16],[23,15],[26,18],[22,21],[18,23],[13,25]],'l');
      k.poly([[10,25],[14,25],[16,27],[15,27],[10,27]],'c');
      k.r(19,i===0?19:16,1,3,'s').r(24,i===0?19:17,1,3,'s');
    });
    c.outline();
    leg(c,[[12,25],[7,27],[3,28]],false);
    leg(c,[[12,24],[7,26],[2,27]]);
    leg(c,[[24,23],[26,26],[27,28]]);
  });
  tail('tail_stretch',[[26,16],[28,12],[28,7],[27,4]],[27,14]);
  tail('tail_stretch_tail_1',[[26,16],[29,12],[29,7],[28,4]],[28,14]);
  tail('tail_stretch_tail_2',[[26,16],[28,13],[29,8],[28,5]],[27,15]);
  tail('tail_stretch_tail_3',[[26,16],[27,13],[28,8],[27,5]],[26,14]);
  /* A laser dot for the chasing action: a small warm red point with a halo. */
  for(let i=0;i<2;i++)art('dot_'+i,7,7,c=>{
    c.oval(1,1,5,5,'p').oval(2,2,3,3,'j').p(3,3,'w').p(2,2,'r');
    if(i){c.p(0,3,'p').p(6,3,'p').p(3,0,'p').p(3,6,'p');}
  });
  /* A paw reaches out low and left, where nothing else occupies the canvas. */
  const pats=[[9,22],[9,24],[9,23]];
  pats.forEach(([x,y],i)=>stage('pat_paw_'+i,c=>{
    c.r(9,y+1,3,2,'f');
    c.r(x,y,4,3,'c');c.r(x,y,4,1,'w');
    c.p(x+1,y+2,'b').p(x+3,y+2,'b');
    c.outline();
  }));
};
