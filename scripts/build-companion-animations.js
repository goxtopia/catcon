/* Five everyday companions with anticipation, contact and settling beats. */
module.exports=function({pose,clip,poses,head,layer}){
  const part=(id,name,properties)=>Object.assign(poses[id].find(l=>l.id===name),properties);

  pose('stretch','stretch_body_1','face_seated_side_open',5,12);
  part('stretch','tail',{sprite:'tail_stretch'});
  clip('stretch','伸个大懒腰 · 拉长啦','stretch',6,[
    {body:{sprite:'torso_side_sit'},head:head('face_seated_side_open',7,6),tail:{sprite:'tail_sit_0'}},
    {body:{sprite:'torso_side_sit'},head:head('face_seated_side_half',7,6),tail:{sprite:'tail_sit_1'}},
    {body:{sprite:'stretch_body_0'},head:head('face_seated_side_open',7,8),tail:{sprite:'tail_stretch_tail_1'}},
    {body:{sprite:'stretch_body_0'},head:head('face_seated_side_half',7,10),tail:{sprite:'tail_stretch'}},
    {body:{sprite:'stretch_body_1'},head:head('face_seated_side_open',6,12),tail:{sprite:'tail_stretch_tail_1'}},
    {body:{sprite:'stretch_body_2'},head:head('face_seated_side_closed',6,13),tail:{sprite:'tail_stretch_tail_2'}},
    {body:{sprite:'stretch_body_2'},head:head('face_seated_side_open',6,13),tail:{sprite:'tail_stretch_tail_1'}},
    {body:{sprite:'stretch_body_1'},head:head('face_seated_side_half',6,12),tail:{sprite:'tail_stretch_tail_3'}},
    {body:{sprite:'torso_side_sit'},head:head('face_seated_side_open',7,6),tail:{sprite:'tail_sit_0'}},
    {body:{sprite:'torso_side_sit'},head:head('face_seated_side_half',7,6),tail:{sprite:'tail_sit_1'}}
  ],[620,340,190,240,300,1150,170,330,700,850],{playback:'once',previewSeconds:1.2});

  pose('laser','torso_sit_0','face_front_open',8,6,{prop:'dot_0',px:25,py:8});
  part('laser','prop',{visible:false});
  part('laser','left',{sprite:'play_paw_0'});
  clip('laser','盯住红点 · 扑了个空','laser',4,[
    {prop:{visible:false},left:{visible:false}},
    {head:head('face_quarter_open_right',8,6),prop:{visible:true,sprite:'dot_0',x:25,y:9},left:{visible:false}},
    {head:head('face_quarter_open_right',8,5),prop:{visible:true,sprite:'dot_1',x:24,y:2},left:{visible:false}},
    {head:head('face_quarter_open',8,6),prop:{visible:true,sprite:'dot_0',x:2,y:9},left:{visible:false}},
    {head:head('face_quarter_open',6,7),prop:{visible:true,sprite:'dot_0',x:1,y:19},left:{visible:true,sprite:'play_paw_1'}},
    {head:head('face_quarter_open',6,7),prop:{visible:true,sprite:'dot_1',x:1,y:19},left:{visible:true,sprite:'play_paw_2'}},
    {head:head('face_front_closed',8,7),prop:{visible:false},left:{visible:true,sprite:'play_paw_3'}},
    {head:head('face_front_half',8,6),prop:{visible:false},left:{visible:false}},
    {head:head('face_tilt',8,5),prop:{visible:false},left:{visible:false}},
    {head:head('face_front_open',8,6),prop:{visible:false},left:{visible:false}}
  ],[600,300,260,320,180,120,220,240,700,600],{playback:'once',previewSeconds:1});

  pose('pat','torso_sit_0','face_quarter_open',8,6);
  part('pat','left',{sprite:'pat_paw_0'});
  clip('pat','伸爪拍拍 · 讨摸摸','pat',5,[
    {head:head('face_front_open',8,6)},
    {head:head('face_quarter_open',8,6),left:{visible:true,sprite:'pat_paw_0'}},
    {head:head('face_quarter_open',7,7),left:{visible:true,sprite:'pat_paw_1'}},
    {head:head('face_quarter_smile',7,7),left:{visible:true,sprite:'pat_paw_2'}},
    {head:head('face_quarter_open',7,6),left:{visible:true,sprite:'pat_paw_0'}},
    {head:head('face_quarter_smile',7,7),left:{visible:true,sprite:'pat_paw_1'}},
    {head:head('face_quarter_closed',7,6)},
    {head:head('face_front_smile',8,6)},
    {head:head('face_front_open',8,6)}
  ],[600,320,240,220,260,240,500,600,500],{playback:'once',previewSeconds:1});

  pose('blink','torso_sit_0','face_front_open',8,6);
  clip('blink','慢慢地眨个眼','blink',8,[
    {},
    {head:head('face_front_half',8,6)},
    {head:head('face_front_closed',8,6)},
    {head:head('face_front_closed',8,7)},
    {head:head('face_front_half',8,6)},
    {head:head('face_front_open',8,6)}
  ],[1400,220,900,760,220,1700],{previewSeconds:1.6});

  pose('sneeze','torso_sit_0','face_front_open',8,6);
  clip('sneeze','啊 —— 打了个喷嚏','sneeze',4,[
    {},
    {body:{sprite:'torso_sit_1'},head:head('face_front_closed',8,4)},
    {body:{sprite:'torso_sit_1'},head:head('face_front_yawn',8,2)},
    {body:{sprite:'torso_sit_1'},head:head('face_front_closed',8,9)},
    {body:{sprite:'torso_sit_1'},head:head('face_front_closed',8,8)},
    {body:{sprite:'torso_sit_0'},head:head('face_front_half',8,7)},
    {head:head('face_front_open',8,5)},
    {head:head('face_front_smile',8,6)}
  ],[550,320,380,120,180,160,240,700],{playback:'once',previewSeconds:1});
};
