param(
    [Parameter(Mandatory=$true)][string]$Clips,
    [string]$Name = 'pack',
    [int]$Scale = 4,
    [int]$From = 0,
    [int]$Count = 0,
    [string]$ReleaseDirectory,
    [switch]$NoGif,
    [switch]$Publish
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $ReleaseDirectory) {
    $ReleaseDirectory = Get-ChildItem -LiteralPath (Join-Path $root 'dist') -Directory |
        Where-Object { $_.Name -match '^Catcon-\d+\.\d+\.\d+$' } |
        Sort-Object { [version]($_.Name.Substring(7)) } -Descending | Select-Object -First 1 -ExpandProperty FullName
}
$exe = Join-Path $ReleaseDirectory 'Catcon.exe'
[void][Reflection.Assembly]::LoadFrom($exe)
$source = @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Catcon;
public static class PackPreview {
 public static void Run(string root,string name,string[] ids,int scale,bool gif,int from,int count,bool publish) {
  using(var content=Content.Load(root,"mikan")) {
   var anim=new Animator(content.Skin);
   string folder=publish?Path.Combine(root,"assets","skins","mikan"):Path.Combine(root,"artifacts","preview");Directory.CreateDirectory(folder);
   int cell=32*scale,colW=cell+16,rowH=cell+42;
   int maxFrames=ids.Max(id=>Math.Min(content.Skin.clips[id].frames.Count-from,count>0?count:999));
   int width=116+maxFrames*colW+16, height=44+ids.Length*rowH;
   using(var sheet=new Bitmap(width,height))using(var g=Graphics.FromImage(sheet))using(var font=new Font("Microsoft YaHei UI",9))using(var small=new Font("Consolas",8))using(var title=new Font("Microsoft YaHei UI",13,FontStyle.Bold))using(var brush=new SolidBrush(Color.FromArgb(60,72,60))) {
    g.Clear(Color.FromArgb(246,244,236));g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAlias;
    g.DrawString(name=="companions"?"MIKAN / DAILY COMPANIONS II / REFERENCE + AUTHORED KEYFRAMES":name=="scenes"?"MIKAN / CONTEXT-SPECIFIC ACTIONS / SIX SCENES":"MIKAN / PACK PREVIEW / "+name,title,brush,12,10);
    for(int row=0;row<ids.Length;row++) {
     var clip=content.Skin.clips[ids[row]];int y=44+row*rowH;
     using(var bg=new SolidBrush(row%2==0?Color.FromArgb(233,235,222):Color.FromArgb(240,237,226)))g.FillRectangle(bg,4,y-2,width-8,rowH-4);
     g.DrawString(clip.label,font,brush,10,y+4);g.DrawString(ids[row]+" / "+clip.playback,small,brush,10,y+24);
     using(var frame=anim.RenderAt("sit",0,false))using(var scaled=Animator.Scale(frame,scale))g.DrawImageUnscaled(scaled,4,y+42);
     g.DrawString("ref",small,brush,8,y+42+cell+2);
     int last=Math.Min(clip.frames.Count,from+(count>0?count:999));
     for(int f=from;f<last;f++) {
      int x=104+(f-from)*colW;double t=Animator.FrameStartSeconds(clip,f)+.001;
      using(var frame=anim.RenderAt(ids[row],t,false))using(var scaled=Animator.Scale(frame,scale))g.DrawImageUnscaled(scaled,x,y+42);
      g.DrawString((f+1).ToString("00"),small,brush,x+2,y+42+cell+2);
      g.DrawString(clip.frameDurations[f]+"ms",small,brush,x+2,y+42+cell+14);
     }
    }
    sheet.Save(Path.Combine(folder,name+"-keyframes.png"),ImageFormat.Png);
   }
   if(!gif)return;
   var frames=new List<Bitmap>();var delays=new List<int>();
   try {
    for(int i=0;i<120;i++)using(var composite=new Bitmap(32*ids.Length,32))using(var g=Graphics.FromImage(composite)) {
     for(int c2=0;c2<ids.Length;c2++) {
      var clip=content.Skin.clips[ids[c2]];double t=i*.05;
      if(clip.playback=="once"){double total=clip.seconds;t%=total+.9;}else t%=clip.seconds;
      using(var raw=anim.RenderAt(ids[c2],t,false))g.DrawImageUnscaled(raw,c2*32,0);
     }
     frames.Add(Animator.Scale(composite,4));delays.Add(5);
    }
    var colors=content.Skin.palette.Values.Select(v=>v.Length==9?Color.FromArgb((int)Convert.ToInt64(v.Substring(1),16)):ColorTranslator.FromHtml(v)).ToList();
    typeof(Animator).Assembly.GetType("Catcon.ArtExport").GetMethod("WriteGif",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Path.Combine(folder,name+"-preview.gif"),frames,delays,colors});
   } finally {foreach(var frame in frames)frame.Dispose();}
  }
 }
}
'@
$artifacts = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$sourcePath = Join-Path $artifacts 'pack-preview.cs'
$assemblyPath = Join-Path $artifacts 'pack-preview.dll'
[IO.File]::WriteAllText($sourcePath,$source,(New-Object Text.UTF8Encoding($false)))
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $compiler /nologo /target:library /codepage:65001 "/out:$assemblyPath" "/reference:$exe" /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll $sourcePath
if ($LASTEXITCODE -ne 0) { throw 'Preview compilation failed' }
[void][Reflection.Assembly]::LoadFrom($assemblyPath)
$list = $Clips.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ }
[PackPreview]::Run($root,$Name,$list,$Scale,(-not $NoGif),$From,$Count,[bool]$Publish)
if ($Publish) { Write-Output ('Preview written to assets/skins/mikan/' + $Name + '-keyframes.png') } else { Write-Output ('Preview written to artifacts/preview/' + $Name + '-keyframes.png') }
