param([string]$ReleaseDirectory)
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
public static class DailyPreview {
 public static void Run(string root) {
  using(var content=Content.Load(root,"mikan")) {
   var current=new Animator(content.Skin);string[] ids={"fish_hug","warm_cup","pencil_nudge","break_sign"};
   string folder=Path.Combine(root,"assets","skins","mikan");
   using(var sheet=new Bitmap(1380,676))using(var g=Graphics.FromImage(sheet))using(var font=new Font("Microsoft YaHei UI",10)) {
    g.Clear(Color.FromArgb(246,244,236));g.DrawString("MIKAN / DAILY COMPANIONS / REFERENCE + AUTHORED KEYFRAMES",font,Brushes.DarkSlateGray,12,8);
    for(int row=0;row<ids.Length;row++) {
     var clip=content.Skin.clips[ids[row]];int y=40+row*158;g.DrawString(clip.label,font,Brushes.DarkSlateGray,12,y);
     using(var frame=current.RenderAt("sit",0,false))using(var scaled=Animator.Scale(frame,3))g.DrawImageUnscaled(scaled,8,y+26);
     for(int f=0;f<clip.frames.Count;f++)using(var frame=current.RenderAt(ids[row],Animator.FrameStartSeconds(clip,f)+.001,false))using(var scaled=Animator.Scale(frame,3)) {
      int x=116+f*104;g.DrawImageUnscaled(scaled,x,y+26);g.DrawString((f+1).ToString("00")+" / "+clip.frameDurations[f]+"ms",font,Brushes.DarkSlateGray,x+2,y+128);
     }
    }
    sheet.Save(Path.Combine(folder,"daily-companions-keyframes.png"),ImageFormat.Png);
   }
   var frames=new List<Bitmap>();var delays=new List<int>();
   try {
    string[] states={"sit","fish_hug","warm_cup","pencil_nudge","break_sign"};
    for(int i=0;i<120;i++)using(var composite=new Bitmap(160,32))using(var g=Graphics.FromImage(composite)) {
     for(int cell=0;cell<states.Length;cell++) {
      var clip=content.Skin.clips[states[cell]];double t=i*.05;if(clip.playback=="once")t%=clip.seconds+.8;
      using(var raw=current.RenderAt(states[cell],t,false))g.DrawImageUnscaled(raw,cell*32,0);
     }
     frames.Add(Animator.Scale(composite,4));delays.Add(5);
    }
    var colors=content.Skin.palette.Values.Select(v=>v.Length==9?Color.FromArgb((int)Convert.ToInt64(v.Substring(1),16)):ColorTranslator.FromHtml(v)).ToList();
    typeof(Animator).Assembly.GetType("Catcon.ArtExport").GetMethod("WriteGif",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Path.Combine(folder,"daily-companions-preview.gif"),frames,delays,colors});
   } finally {foreach(var frame in frames)frame.Dispose();}
  }
 }
}
'@
$artifacts = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$sourcePath = Join-Path $artifacts 'daily-preview.cs'
$assemblyPath = Join-Path $artifacts 'daily-preview.dll'
[IO.File]::WriteAllText($sourcePath,$source,(New-Object Text.UTF8Encoding($false)))
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $compiler /nologo /target:library /codepage:65001 "/out:$assemblyPath" "/reference:$exe" /reference:System.Drawing.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll $sourcePath
if ($LASTEXITCODE -ne 0) { throw 'Preview compilation failed' }
[void][Reflection.Assembly]::LoadFrom($assemblyPath)
[DailyPreview]::Run($root)
Write-Output 'Exported four daily companions, all authored keyframes and the unchanged reference cat.'
