/* Mikan II: native 32px drawings, independently timed animation frames. */
const fs=require('fs'),path=require('path');
const {sprites,palette,gait,pounces,tailCycles}=require('./cat-art');
const root=path.resolve(__dirname,'..');
const layer=(id,sprite,x=0,y=0,z=0,visible=true)=>({id,sprite,x,y,z,visible});
const poses={};
function pose(id,body,head,x,y,options={}){
  poses[id]=[
    layer('tail','tail_sit_0',0,0,0,options.tail!==false),layer('body',body,0,0,1),layer('head',head,x,y,3),
    layer('prop',options.prop||'food_0',options.px||0,options.py||0,4,!!options.prop),
    layer('left','hand_0',0,0,5,false),layer('right','hand_0',0,0,5,false),
    layer('effect',options.effect||'question',options.ex||0,options.ey||0,6,!!options.effect)
  ];
}
pose('sit','torso_sit_0','face_front_open',8,6);
pose('quarter','torso_quarter_0','face_quarter_open',7,7);
pose('side','torso_side_sit','face_seated_side_open',7,6);
pose('loaf','loaf_0','face_quarter_closed',7,10,{tail:false});
pose('sleep','sleep_curl_0','face_side_closed',6,17,{tail:false,effect:'sleep_z',ex:23,ey:8});
poses.sleep.push(layer('wrap','sleep_wrap',0,0,4));
pose('sleep_stretch','sleep_long_0','face_side_closed',3,16,{tail:false,effect:'sleep_z',ex:25,ey:11});
pose('run','gait_0','face_side_open',2,10,{tail:false});
pose('pounce','pounce_0','face_side_open',2,15,{tail:false});
pose('scared','arch_0','face_side_scared',3,10,{tail:false,effect:'alert',ex:26,ey:2});
pose('eat','eat_body_0','face_down',4,15,{tail:false,prop:'food_0',px:3,py:26});
pose('play','play_body_0','face_side_open',7,11,{prop:'yarn_0',px:2,py:23});
Object.assign(poses.play.find(l=>l.id==='left'),{sprite:'play_paw_0',visible:true});
pose('laptop','torso_quarter_0','face_quarter_open',7,5,{prop:'laptop_0',px:7,py:20});
Object.assign(poses.laptop.find(l=>l.id==='left'),{visible:true,x:9,y:23});Object.assign(poses.laptop.find(l=>l.id==='right'),{visible:true,x:17,y:23});
pose('watch','loaf_0','face_side_open_right',4,12,{tail:false,prop:'screen_0',px:19,py:18});
pose('read','torso_quarter_0','face_quarter_open',7,6,{prop:'book_0',px:7,py:22});
pose('game','torso_sit_0','face_front_open',8,5,{prop:'controller_0',px:9,py:22});
Object.assign(poses.game.find(l=>l.id==='left'),{visible:true,x:9,y:22});Object.assign(poses.game.find(l=>l.id==='right'),{visible:true,x:19,y:22});
pose('curious','torso_quarter_0','face_quarter_curious',7,7,{effect:'question',ex:25,ey:3});
pose('love','torso_sit_0','face_front_smile',8,6,{effect:'heart',ex:25,ey:4});
pose('drag','drag_body','face_front_flat',8,5);
const clips={};
function clip(id,label,base,seconds,frames,durations,extra={}){
  clips[id]={label,pose:base,seconds,frameMs:120,frames,frameDurations:durations,playback:'loop',deformers:[],...extra};
  if(frames.length!==durations.length)throw Error('Timing mismatch '+id);
}
const head=(sprite,x,y)=>({sprite,...(x===undefined?{}:{x,y})});
const frame=(body,face,x,y,other={})=>({...body&&{body:{sprite:body}},...face&&{head:head(face,x,y)},...other});
clip('sit','乖乖坐好','sit',12,[
  {},{body:{sprite:'torso_sit_1'},tail:{sprite:'tail_sit_1'}},{head:head('face_front_half')},{head:head('face_front_closed')},{head:head('face_front_half')},{tail:{sprite:'tail_sit_2'}}
],[2400,1200,80,100,80,1200]);
clip('sit_tilt','侧过脸看看','sit',8,[
  {},frame('torso_sit_0','face_tilt',8,5),frame('torso_quarter_0','face_quarter_open',8,6),frame('torso_sit_1','face_tilt_other',8,5),frame('torso_sit_0','face_front_half',8,6),{}
],[900,700,1000,1100,100,700]);
clip('sit_side','侧身安静坐着','side',12,[
  {},{tail:{sprite:'tail_sit_1'}},{head:head('face_seated_side_half',7,6)},{head:head('face_seated_side_closed',7,6)},{head:head('face_seated_side_open',7,6),tail:{sprite:'tail_sit_2'}}
],[1800,1400,100,120,1700]);
clip('sit_groom','舔爪洗洗脸','sit',9,[
  frame('torso_groom_0','face_front_smile',8,7),frame('torso_groom_1','face_front_closed',8,6),frame('torso_groom_2','face_front_groom',8,7),frame('torso_groom_0','face_front_groom',8,7),frame('torso_groom_1','face_front_closed',8,6),frame('torso_sit_0','face_front_smile',8,6)
],[200,180,200,160,200,900]);
clip('sleep','蜷成一团睡','sleep',25,[
  {},{body:{sprite:'sleep_curl_1'},effect:{y:7}},{body:{sprite:'sleep_curl_0'},effect:{y:6,visible:false}},{effect:{visible:true,y:8}}
],[1300,1100,1400,1200]);
clip('sleep_stretch','侧躺伸懒腿','sleep_stretch',24,[
  {},{body:{sprite:'sleep_long_1'}},{head:head('face_side_half',3,16),effect:{visible:false}},{head:head('face_side_closed',3,16)}
],[1400,900,120,1800]);
clip('loaf','鸡母蹲 · 冷静一下','loaf',14,[
  {},{body:{sprite:'loaf_1'},head:head('face_quarter_half',7,10)},{head:head('face_quarter_closed',7,10)},{head:head('face_quarter_smile',7,10)}
],[1800,120,1200,1000]);
clip('yarn','伸爪拨毛线球','play',12,[
  frame('play_body_0','face_side_open',7,11,{left:{sprite:'play_paw_0'},prop:{sprite:'yarn_0',x:2,y:24}}),
  frame('play_body_1','face_side_open',6,12,{left:{sprite:'play_paw_1'},prop:{sprite:'yarn_1',x:0,y:24}}),
  frame('play_body_2','face_side_half',7,11,{left:{sprite:'play_paw_2'},prop:{sprite:'yarn_2',x:0,y:24}}),
  frame('play_body_3','face_side_open',7,12,{left:{sprite:'play_paw_3'},prop:{sprite:'yarn_1',x:2,y:24}}),
  frame('play_body_0','face_side_closed',7,11,{left:{sprite:'play_paw_0'},prop:{sprite:'yarn_0',x:4,y:24}}),
  frame('play_body_0','face_side_open',7,11,{left:{sprite:'play_paw_0'},prop:{sprite:'yarn_2',x:3,y:24}})
],[300,100,220,180,220,650]);
clip('scared','弓背炸毛！','scared',1.8,[
  frame('pounce_0','face_side_open',2,15,{effect:{visible:false}}),
  frame('arch_0','face_side_scared',3,10),frame('arch_2','face_side_scared',3,9),frame('arch_1','face_side_scared',3,10),frame('arch_0','face_side_half',3,11,{effect:{visible:false}})
],[100,100,170,1080,350],{playback:'once'});
clip('eat','低头吃小鱼干','eat',12,[
  frame('eat_body_0','face_down',4,15),frame('eat_body_1','face_down',3,16),frame('eat_body_1','face_down_tongue',3,16,{prop:{sprite:'food_1'}}),frame('eat_body_0','face_side_groom',3,14,{prop:{sprite:'food_2'}}),frame('eat_body_2','face_side_closed',3,14),frame('eat_body_0','face_down',4,15)
],[450,170,130,230,170,400]);
clip('drink','低头舔水','eat',10,[
  frame('eat_body_0','face_down',4,15,{prop:{sprite:'water_0'}}),frame('eat_body_1','face_down_tongue',3,16,{prop:{sprite:'water_1'}}),frame('eat_body_1','face_down',3,16,{prop:{sprite:'water_2'}}),frame('eat_body_0','face_down_tongue',3,16,{prop:{sprite:'water_1'}}),frame('eat_body_0','face_side_closed',3,14,{prop:{sprite:'water_0'}})
],[350,100,120,100,650]);
clip('chase_tail','绕圈追自己的尾巴','run',8,[
  frame('gait_2','face_side_open',2,10),
  frame('torso_back_quarter','face_back_quarter',6,7,{tail:{visible:true,sprite:'tail_sit_1'}}),
  frame('turn_back','face_back',8,6,{tail:{visible:true,sprite:'tail_sit_2',z:4}}),
  frame('torso_back_quarter_right','face_back_quarter_right',11,7,{tail:{visible:true,sprite:'tail_sit_3'}}),
  frame('gait_2_right','face_side_open_right',16,10),
  frame('torso_quarter_0_right','face_quarter_open_right',9,8,{tail:{visible:true,sprite:'tail_sit_2'}}),
  frame('turn_front','face_front_open',8,8,{tail:{visible:true,sprite:'tail_sit_1'}}),
  frame('torso_quarter_1','face_quarter_open',7,8,{tail:{visible:true,sprite:'tail_sit_0'}})
],[130,110,120,110,130,110,120,110]);
const runFrames=gait.map((g,i)=>frame('gait_'+i,'face_side_open',...g.head));
clip('run','六拍轻快奔跑','run',4,runFrames,[90,80,80,80,100,90],{motion:'wander',speed:150});
clip('run_cursor','奔向鼠标','run',5,runFrames,[80,70,70,70,90,80],{motion:'cursor',speed:340,onArrival:'pounce'});
clip('pounce','蓄力 · 腾空 · 扑下','pounce',1.1,pounces.map((p,i)=>frame('pounce_'+i,i>=6?'face_side_half':'face_side_open',...p.head,{left:{visible:true,sprite:'pounce_paws_'+i,x:0,y:0}})),[130,100,80,100,150,90,160,290],{playback:'once'});
clips.pounce.contactAnchor=[3,27];
clips.pounce.motion='leap';
clips.pounce.leap={distance:26,height:7,takeoffFrame:2,landingFrame:5};
clip('retreat','挪开一点点','run',3,runFrames,[130,120,110,110,130,120]);
Object.assign(clips.retreat,{motion:'retreat',speed:140});
clip('code_watch','侧着脸监工','quarter',13,[
  {},{head:head('face_side_open',7,8)},{head:head('face_quarter_half',7,7)},{head:head('face_quarter_closed',7,7)},{head:head('face_quarter_open',7,7),tail:{sprite:'tail_sit_2'}}
],[1700,1200,80,100,1800]);
clip('code_laptop','认真敲两行','laptop',14,[
  {left:{y:22},right:{y:24}},{left:{y:24},right:{y:22},prop:{sprite:'laptop_1'}},{left:{y:23},right:{y:24},head:head('face_quarter_half',7,6)},{left:{y:24},right:{y:23},head:head('face_quarter_open',7,6),prop:{sprite:'laptop_1'}},{head:head('face_quarter_closed',7,5)}
],[170,150,160,170,300]);
clip('keyboard_walk','踩过整个键盘','laptop',12,[
  frame('keyboard_cat_0','face_quarter_open',4,5,{body:{sprite:'keyboard_cat_0',x:-3},left:{x:8,y:23},right:{x:13,y:24}}),
  frame('keyboard_cat_1','face_quarter_open',6,6,{body:{sprite:'keyboard_cat_1',x:-1},left:{x:10,y:24},right:{x:15,y:23}}),
  frame('keyboard_cat_0','face_quarter_open',8,5,{body:{sprite:'keyboard_cat_0',x:1},left:{x:12,y:23},right:{x:17,y:24}}),
  frame('keyboard_cat_1','face_quarter_open',10,6,{body:{sprite:'keyboard_cat_1',x:3},left:{x:14,y:24},right:{x:19,y:23}}),
  frame('keyboard_cat_0','face_quarter_half',8,5,{body:{sprite:'keyboard_cat_0',x:1},left:{x:12,y:23},right:{x:17,y:24}}),
  frame('keyboard_cat_1','face_quarter_open',6,6,{body:{sprite:'keyboard_cat_1',x:-1},left:{x:10,y:24},right:{x:15,y:23}})
],[230,230,230,350,230,230]);
clip('video_watch','侧身一起看','watch',16,[
  {},{prop:{sprite:'screen_1'}},{head:head('face_side_half_right',4,12),prop:{sprite:'screen_2'}},{head:head('face_side_closed_right',4,12)},{head:head('face_side_open_right',4,12),prop:{sprite:'screen_1'}}
],[1300,950,100,120,1400]);
clip('yawn','张嘴打个大哈欠','sit',4,[
  {head:head('face_front_half')},{head:head('face_front_closed')},{head:head('face_front_yawn',8,5)},{head:head('face_front_yawn',8,4),body:{sprite:'torso_sit_1'}},{head:head('face_front_yawn',8,5)},{head:head('face_front_closed')},{head:head('face_front_half')},{head:head('face_front_open')}
],[450,180,280,900,400,400,300,1090],{playback:'once'});
clip('office_read','低头翻一页资料','read',15,[
  {},{head:head('face_quarter_half',7,7),prop:{sprite:'book_1'}},{head:head('face_quarter_open',7,7),prop:{sprite:'book_2'}},{head:head('face_quarter_open',7,6)},{head:head('face_quarter_closed',7,6)}
],[1700,220,220,1600,110]);
clip('browse','陪你慢慢逛','read',12,[
  {},{head:head('face_side_open',7,7),prop:{sprite:'book_1'}},{head:head('face_quarter_open',7,6),prop:{sprite:'book_2'}},{head:head('face_quarter_half',7,6)},{head:head('face_quarter_closed',7,6)}
],[1600,1100,350,100,100]);
clip('chat_listen','耳朵竖起来偷听','quarter',10,[
  {head:head('face_quarter_flat',7,8)},{head:head('face_quarter_curious',7,7)},{head:head('face_side_open',7,8)},{head:head('face_quarter_open',7,7),tail:{sprite:'tail_sit_2'}},{head:head('face_quarter_closed',7,7)}
],[350,1100,900,1300,120]);
clip('chat_question','你在说什么呀？','curious',8,[
  {},{body:{sprite:'torso_sit_0'},head:head('face_tilt',8,5),effect:{y:2}},{body:{sprite:'torso_sit_1'},head:head('face_tilt_other',8,5),effect:{y:3}},{head:head('face_quarter_closed',7,7),effect:{visible:false}}
],[700,650,1000,140]);
clip('game','专心按小手柄','game',14,[
  {left:{y:21},right:{y:22},prop:{sprite:'controller_1'}},{left:{y:22},right:{y:21},prop:{sprite:'controller_2'}},{left:{y:21},right:{y:22},head:head('face_front_half',8,5)},{left:{y:22},right:{y:21},head:head('face_front_scared',8,4)},{head:head('face_front_open',8,5)}
],[220,200,230,350,650]);
clip('pet','眯眼呼噜呼噜','love',5,[
  {},{head:head('face_quarter_smile',7,7),body:{sprite:'torso_quarter_0'},effect:{y:3}},{head:head('face_front_smile',8,6),body:{sprite:'torso_sit_1'},tail:{sprite:'tail_sit_2'},effect:{y:2}},{head:head('face_front_smile',8,7),effect:{visible:false}}
],[650,750,750,500]);
clip('drag','四爪垂下来','drag',3,[
  {},{head:head('face_front_flat',8,6),tail:{sprite:'tail_sit_2'}},{head:head('face_front_half',8,5),tail:{sprite:'tail_sit_1'}},{head:head('face_front_flat',8,5),tail:{sprite:'tail_sit_3'}}
],[500,350,120,450]);
require('./build-extra-animations')({pose,clip,poses,frame,head,layer});
require('./build-daily-animations')({pose,clip,poses,frame,head,layer});
require('./build-companion-animations')({pose,clip,poses,frame,head,layer});
require('./build-scene-animations')({pose,clip,poses,head});
// Secondary sprite cycles keep tails alive during long held body drawings.
// Each embedded-tail variant retains that body's exact pose and attachment.
for(const layers of Object.values(poses))for(const l of layers)if(tailCycles[l.sprite])Object.assign(l,{spriteCycle:tailCycles[l.sprite],cycleMs:300});
for(const [id,c] of Object.entries(clips)){
  const ms=id.startsWith('sleep')?700:id==='loaf'||id==='video_watch'||id==='knead'?600:['run','run_cursor','retreat','chase_tail'].includes(id)?110:id==='pounce'||id==='scared'?100:id==='yarn'||id==='bug_watch'?160:id==='keyboard_walk'||id==='drag'?180:id==='pet'?200:400;
  for(const f of c.frames){
    const body=f.body||{};
    if(id==='keyboard_walk')f.tail={...(f.tail||{}),sprite:'tail_keyboard_0',visible:true};
    if(id==='drag')f.tail={...(f.tail||{}),sprite:'tail_drag_0',visible:true};
    for(const base of poses[c.pose]){
      let patch=f[base.id]||{},sprite=patch.sprite||base.sprite;
      if(base.id==='tail'&&(patch.visible===true||base.visible)){
        if(body.sprite&&body.sprite.endsWith('_right')&&!sprite.endsWith('_right'))sprite='tail_sit_0_right';
        if(patch.x===undefined)patch.x=body.x||0;
        if(patch.y===undefined)patch.y=body.y||0;
      }
      if(tailCycles[sprite]){patch.sprite=sprite;patch.spriteCycle=tailCycles[sprite];patch.cycleMs=ms;f[base.id]=patch;}
    }
  }
}
const skin={schemaVersion:1,id:'mikan',name:'蜜柑 · 橘白猫',author:'Catcon / Mikan II native pixel redraw',canvas:32,artRevision:6,palette,sprites,poses,clips};
fs.writeFileSync(path.join(root,'assets/skins/mikan/skin.json'),JSON.stringify(skin,null,2));
const count=Object.values(clips).reduce((n,c)=>n+c.frames.length,0);
console.log(`Mikan II: ${Object.keys(sprites).length} native pixel components, ${Object.keys(poses).length} poses, ${Object.keys(clips).length} clips, ${count} authored keyframes; no resampling, bitmap rotation or body-scale animation.`);
