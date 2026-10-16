/* Small anticipation, contact, reaction and settling beats, all drawn at 32px. */
module.exports=function({pose,clip,poses,head,frame}){
  const part=(id,name,properties)=>Object.assign(poses[id].find(l=>l.id===name),properties);
  pose('fish_hug','hug_body_0','face_front_open',8,6,{prop:'plush_fish_0',px:10,py:23});
  part('fish_hug','left',{sprite:'hug_paws_0',visible:true});
  clip('fish_hug','抱着小鱼玩偶犯困','fish_hug',8,[
    {},{head:head('face_quarter_half',8,7)},
    {head:head('face_front_smile',8,6),left:{sprite:'hug_paws_1'},prop:{y:21}},
    frame('hug_body_1','face_front_closed',8,7,{left:{sprite:'hug_paws_2'},prop:{sprite:'plush_fish_1',y:21}}),
    frame('hug_body_1','face_front_closed',8,8,{left:{sprite:'hug_paws_2'},prop:{sprite:'plush_fish_1',y:21}}),
    {head:head('face_front_half',8,7),left:{sprite:'hug_paws_1'},prop:{y:21}},
    {head:head('face_front_open',8,6),left:{sprite:'hug_paws_1'},prop:{y:21}},
    {head:head('face_front_smile',8,6)},{}
  ],[650,350,300,1000,1100,180,500,800,500],{previewSeconds:1.4});

  pose('warm_cup','torso_sit_0','face_quarter_open',7,6,{prop:'cozy_mug',px:5,py:23,effect:'mug_steam_0',ex:5,ey:16});
  part('warm_cup','effect',{z:2,spriteCycle:['mug_steam_0','mug_steam_1','mug_steam_2'],cycleMs:220});
  part('warm_cup','left',{sprite:'mug_paws_0'});
  clip('warm_cup','捧着暖杯闻一闻','warm_cup',7,[
    {},{head:head('face_tilt',7,6)},
    {head:head('face_quarter_open',6,7),left:{visible:true}},
    {head:head('face_quarter_half',6,8),left:{visible:true,sprite:'mug_paws_1'},prop:{x:6,y:22},effect:{x:6,y:15}},
    {head:head('face_quarter_closed',6,8),left:{visible:true,sprite:'mug_paws_1'},prop:{x:6,y:22},effect:{x:6,y:15}},
    {head:head('face_quarter_half',6,7),left:{visible:true,sprite:'mug_paws_1'},prop:{x:6,y:22},effect:{x:6,y:15}},
    {head:head('face_front_smile',8,6),left:{visible:true,sprite:'mug_paws_2'},prop:{x:6,y:23}},
    {head:head('face_quarter_closed',7,6),left:{visible:true}},{}
  ],[650,400,300,220,1000,180,700,800,500],{previewSeconds:2});

  pose('pencil_nudge','torso_sit_0','face_quarter_open',8,6,{prop:'pencil_roll_0',px:2,py:28});
  part('pencil_nudge','left',{sprite:'pencil_paw_0'});
  clip('pencil_nudge','悄悄推铅笔 · 装无辜','pencil_nudge',6,[
    {},{head:head('face_quarter_half',7,7)},
    {head:head('face_quarter_open',7,7),left:{visible:true}},
    {head:head('face_quarter_open',7,8),left:{visible:true,sprite:'pencil_paw_1'}},
    {head:head('face_quarter_open',7,8),left:{visible:true,sprite:'pencil_paw_2'},prop:{sprite:'pencil_roll_1',x:6}},
    {head:head('face_quarter_open_right',8,7),left:{visible:true,sprite:'pencil_paw_3'},prop:{sprite:'pencil_roll_2',x:10}},
    {head:head('face_quarter_open_right',8,6),prop:{sprite:'pencil_roll_1',x:13}},
    {head:head('face_front_open',8,6),prop:{sprite:'pencil_roll_0',x:15}},
    {head:head('face_front_half',8,6),prop:{x:15}},
    {head:head('face_front_open',8,6),prop:{x:15}}
  ],[600,450,280,220,110,120,240,850,150,1000],{playback:'once',previewSeconds:1.8});

  pose('break_sign','torso_sit_0','face_front_open',8,6,{prop:'rest_sign_0',px:23,py:17});
  part('break_sign','right',{sprite:'sign_paw_0',visible:true});
  clip('break_sign','举个小牌子 · 休息一下','break_sign',6,[
    {},{head:head('face_quarter_open_right',8,6)},
    {right:{sprite:'sign_paw_1'},prop:{y:13}},
    {head:head('face_front_smile',8,6),right:{sprite:'sign_paw_2'},prop:{y:10}},
    {head:head('face_tilt_other',8,5),right:{sprite:'sign_paw_3'},prop:{sprite:'rest_sign_1',y:11}},
    {head:head('face_front_smile',8,6),right:{sprite:'sign_paw_2'},prop:{y:10}},
    {head:head('face_front_open',8,6),right:{sprite:'sign_paw_2'},prop:{y:10}},
    {head:head('face_front_half',8,6),right:{sprite:'sign_paw_1'},prop:{y:13}},
    {head:head('face_front_smile',8,6)},
    {right:{visible:false},prop:{visible:false}}
  ],[450,300,180,500,600,600,700,200,450,600],{playback:'once',previewSeconds:1.5});
};
