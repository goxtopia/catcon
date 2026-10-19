/* Six context-exclusive behaviours: one per recognised desktop context, so a
   coding, video, office, browsing, chat or gaming session each gets a beat that
   never appears anywhere else. */
module.exports=function({pose,clip,poses,head}){
  const part=(id,name,properties)=>Object.assign(poses[id].find(l=>l.id===name),properties);

  /* coding: the build fails right under the cat's paws. */
  pose('compile','torso_quarter_0','face_quarter_open',7,6,{prop:'laptop_err',px:7,py:20});
  part('compile','left',{visible:true,x:9,y:23});
  part('compile','right',{visible:true,x:17,y:23});
  /* The resting tail has to match the ones used mid-clip, otherwise the last
     frame snaps back to the generic tail_sit and the tail jumps. */
  part('compile','tail',{sprite:'tail_sit_0'});
  clip('compile_error','编译挂了 · 缩成一团','compile',6,[
    {prop:{sprite:'laptop_0'},left:{y:22},right:{y:24},tail:{sprite:'tail_sit_0'}},
    {prop:{sprite:'laptop_1'},left:{y:24},right:{y:22},tail:{sprite:'tail_sit_1'}},
    {head:head('face_quarter_open',7,5),tail:{sprite:'tail_sit_2'}},
    {head:head('face_front_scared',8,4),left:{y:21},right:{y:21},tail:{sprite:'tail_qpuff_0'},effect:{visible:true,sprite:'alert',x:4,y:3}},
    {body:{sprite:'torso_quarter_1'},head:head('face_front_scared',8,3),left:{visible:false},right:{visible:false},tail:{sprite:'tail_qpuff_1'},effect:{visible:true,sprite:'alert',x:4,y:4}},
    {body:{sprite:'torso_quarter_0'},head:head('face_front_scared',8,4),left:{visible:false},right:{visible:false},tail:{sprite:'tail_qpuff_0'},effect:{visible:true,sprite:'alert',x:4,y:3}},
    {head:head('face_quarter_open',7,7),tail:{sprite:'tail_sit_1'}},
    {head:head('face_quarter_half',7,6),tail:{sprite:'tail_sit_0'}},
    {head:head('face_quarter_closed',7,6)}
  ],[380,320,220,160,260,420,700,800,700],{playback:'once',previewSeconds:1.2});

  /* video: a bird crosses the screen and gets swatted. */
  pose('video','loaf_0','face_side_open_right',4,12,{tail:false,prop:'screen_0',px:19,py:18});
  part('video','left',{visible:false,sprite:'reach_right_0'});
  clip('video_paw','屏幕里飞过一只鸟','video',6,[
    {},
    {prop:{sprite:'screen_bird_0'}},
    {prop:{sprite:'screen_bird_1'},head:head('face_side_open_right',4,11)},
    {prop:{sprite:'screen_bird_0'},left:{visible:true,sprite:'reach_right_0'}},
    {prop:{sprite:'screen_bird_1'},left:{visible:true,sprite:'reach_right_1'},head:head('face_side_open_right',4,10)},
    {prop:{sprite:'screen_1'},left:{visible:true,sprite:'reach_right_1'}},
    {prop:{sprite:'screen_0'},left:{visible:false},head:head('face_side_half_right',4,12)},
    {head:head('face_side_closed_right',4,12)}
  ],[900,420,520,300,260,600,900,900],{playback:'once',previewSeconds:1.4});

  /* office: sitting on the papers so the work has to stop. */
  pose('paper','paper_body_0','face_quarter_open',7,7);
  part('paper','left',{visible:false,sprite:'pat_paw_0'});
  clip('paper_sit','一屁股坐在资料上','paper',5,[
    {},
    {head:head('face_quarter_closed',7,8)},
    {head:head('face_quarter_half',7,7),left:{visible:true,sprite:'pat_paw_0'}},
    {body:{sprite:'paper_body_1'},head:head('face_quarter_closed',7,8),left:{visible:true,sprite:'pat_paw_1'}},
    {head:head('face_quarter_smile',7,7),left:{visible:true,sprite:'pat_paw_2'}},
    {head:head('face_quarter_open',7,7),left:{visible:false}}
  ],[1200,900,280,520,700,1000],{previewSeconds:1.5});

  /* browsing: the page keeps scrolling, the head keeps dropping. */
  pose('nod','torso_quarter_0','face_quarter_open',7,6,{prop:'book_0',px:7,py:22});
  clip('browse_nod','看着看着就点头了','nod',6,[
    {},
    {head:head('face_quarter_half',7,8)},
    {head:head('face_quarter_closed',7,10)},
    {head:head('face_quarter_closed',7,11)},
    {head:head('face_quarter_open',7,6)},
    {head:head('face_quarter_open',7,5)},
    {head:head('face_quarter_half',7,7)},
    {head:head('face_quarter_closed',7,9)},
    {}
  ],[900,700,600,420,150,320,700,600,900],{previewSeconds:1.6});

  /* chat: caught mid-yawn, so it pretends to be grooming. */
  pose('chat_yawn','torso_sit_0','face_front_open',8,6);
  clip('caught_yawn','打哈欠被抓 · 装没事','chat_yawn',6,[
    {},
    {head:head('face_front_half',8,6)},
    {head:head('face_front_yawn',8,5)},
    {body:{sprite:'torso_sit_1'},head:head('face_front_yawn',8,4)},
    {head:head('face_front_open',8,6)},
    {head:head('face_quarter_open',7,7)},
    {body:{sprite:'torso_sit_1'},head:head('face_front_groom',8,6)},
    {body:{sprite:'torso_sit_0'},head:head('face_front_groom',8,7)},
    {body:{sprite:'torso_sit_0'},head:head('face_front_smile',8,6)}
  ],[500,220,320,820,140,300,720,480,820],{playback:'once',previewSeconds:1.2});

  /* gaming: the run ends, the controller goes down and the screen loses. */
  pose('lose','torso_sit_0','face_front_open',8,5,{prop:'controller_0',px:9,py:23});
  part('lose','left',{visible:true,x:9,y:22});
  part('lose','right',{visible:true,x:19,y:22});
  clip('game_lose','输了 · 扭头不看了','lose',6,[
    {left:{y:21},right:{y:23},prop:{sprite:'controller_1',y:22}},
    {left:{y:23},right:{y:21},prop:{sprite:'controller_2',y:22}},
    {head:head('face_front_scared',8,4),prop:{sprite:'controller_0',y:22}},
    {head:head('face_front_closed',8,5),left:{y:20},right:{y:20},prop:{sprite:'controller_0',y:23}},
    {head:head('face_front_closed',8,5),left:{visible:false},right:{visible:false}},
    {head:head('face_tilt',10,5)},
    {head:head('face_tilt_other',6,5)},
    {head:head('face_front_closed',8,6)},
    {head:head('face_quarter_open',7,6)},
    {head:head('face_front_open',8,5)}
  ],[240,220,340,520,220,420,620,600,700,700],{playback:'once',previewSeconds:1.2});
};
