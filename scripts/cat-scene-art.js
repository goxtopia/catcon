/* Scene-exclusive props: a red build-error banner, a puffed tail for fright,
   a bird on the video screen, a sheet of paper the cat sits on, a mouse and
   the paw that rests on it. All drawn on the final 32px grid. */
module.exports=function({art,stage,leg,canvases}){
  /* The laptop screen flashes red when the build fails. Same shell as
     laptop_0/1, only the panel content changes. */
  art('laptop_err',17,12,c=>{
    c.poly([[1,1],[14,1],[15,9],[16,10],[15,11],[0,11],[0,10],[2,9]],'o');
    c.r(2,2,12,6,'v').r(3,3,10,4,'j');
    c.r(4,4,3,1,'w').r(8,4,4,1,'r').r(4,6,7,1,'r').p(12,3,'y').p(12,5,'y');
    c.r(2,9,12,1,'b').r(5,10,5,1,'s');
  });

  /* Something dark crosses the video screen. A light bird disappears against
     the pale panel, so it is a silhouette with spread wings. */
  for(let i=0;i<2;i++)art('screen_bird_'+i,13,13,c=>{
    c.stamp(canvases.screen_0);
    c.r(2,3,9,5,'u');
    const x=3+i*2,y=5;
    c.r(x,y,5,1,'o');
    c.p(x,y-1,'o').p(x+4,y-1,'o');
    c.p(x+1,y+1,'o').p(x+3,y+1,'o').p(x+2,y+2,'o');
  });

  /* Sitting straight down on the papers: the sheet corners poke out on both
     sides with two grey text lines, so the joke is readable without a prop. */
  for(let i=0;i<2;i++)stage('paper_body_'+i,c=>{
    c.poly([[2,26],[29,23],[29,30],[2,30]],'w');
    c.outline();
    c.inside(k=>{
      k.r(4,27,6,1,'u').r(21,26,6,1,'u');
      k.r(5,29,9,1,'u').r(18,28,8,1,'u');
    });
    c.stamp(canvases['torso_sit_'+(i?1:0)]);
  });

  /* The laptop hides the floor on the right, so tail_stretch (rooted in mid-air
     for the stretch pose) reads as a hook with a gap. These frightened tails
     copy the silhouette of tail_sit - wide base at the bottom, thin tip - and
     only extend it upward, so the length matches every other tail in the set. */
  const taper=(c,pts,w)=>{
    for(let i=1;i<pts.length;i++)c.line(...pts[i-1],...pts[i],'f',w[i-1]);
    c.outline();
  };
  const qPuff=[[[19,26],[25,24],[27,19],[26,15],[27,13]],
               [[19,26],[26,24],[28,19],[27,15],[28,13]]];
  qPuff.forEach((pts,i)=>{
    const tip=pts[pts.length-1];
    stage('tail_qpuff_'+i,c=>{
      taper(c,pts,[4,3,2,2]);
      c.p(pts[2][0],pts[2][1],'s').p(pts[2][0]+1,pts[2][1],'s');
      c.p(tip[0],tip[1],'c');
    });
  });

  /* A thin forearm with a small paw pad, resting on the lower corner of the
     screen. Wide paws at this scale read as a striped bar, so keep it slim. */
  const reaches=[[[[20,30],[23,29],[25,27]],23,25],[[[20,29],[23,28],[26,25]],24,23]];
  reaches.forEach(([p,px,py],i)=>stage('reach_right_'+i,c=>{
    c.path(p,'f',2);
    c.r(px,py,3,2,'c');
    c.outline();
    c.p(px+1,py+1,'b');
  }));
};
