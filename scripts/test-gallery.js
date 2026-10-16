/* Exercise the offline player with a tiny in-memory DOM, no browser or network. */
const fs=require('fs'),path=require('path'),vm=require('vm'),assert=require('assert');
const root=path.resolve(__dirname,'..'),cards=[],controls={pause:{},bg:{value:'#e9ebde'},speed:{value:'1'},grid:{appendChild(a){cards.push(a)}}};
let next,draws=0;
class LocalImage{
  set src(value){const p=path.resolve(root,'docs',value),data=fs.readFileSync(p);this.naturalWidth=data.readUInt32BE(16);this.complete=true;}
}
const document={
  getElementById(id){return controls[id]},querySelectorAll(){return cards},
  createElement(){return {style:{},querySelector(){return {getContext(){return {clearRect(){},drawImage(image,sx,sy,sw,sh){assert(sx>=0&&sx+sw<=image.naturalWidth);assert.strictEqual(sh,32);draws++}}}}}}}
};
const html=fs.readFileSync(path.join(root,'docs/gallery.html'),'utf8');
const script=html.match(/<script>([\s\S]*?)<\/script>/)[1];
vm.runInNewContext(script,{document,Image:LocalImage,requestAnimationFrame(fn){next=fn}}, {timeout:1000});
const expected=Object.keys(JSON.parse(fs.readFileSync(path.join(root,'assets/skins/mikan/skin.json'),'utf8')).clips).length;
assert.strictEqual(cards.length,expected);
for(let tick=0;tick<240;tick++)next(tick*50);
assert.strictEqual(draws,240*expected);
const button={};controls.pause.onclick({target:button});assert.strictEqual(button.textContent,'继续播放');
next(12100);controls.pause.onclick({target:button});assert.strictEqual(button.textContent,'暂停动画');
controls.speed.value='.5';for(let tick=0;tick<100;tick++)next(12200+tick*50);
controls.bg.onchange({target:{value:'#30403b'}});assert(cards.every(c=>c.style.background==='#30403b'));
console.log(`Offline player verified: ${expected} clips, authored timing at normal/half speed, pause/resume, background switching, no frame outside its PNG strip.`);
