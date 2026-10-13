using Mono.Cecil;
using NugzzMenu.Services;
using NugzzMenu.UI;
using UnityEngine;

if (args.Length!=3) { Console.Error.WriteLine("Usage: ClothingPickerChecks <menu.dll> <interop-directory> <before-fix-menu.dll>"); return 1; }
var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetFullPath(args[1]));
resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0])));
using var menu = ModuleDefinition.ReadModule(args[0],new ReaderParameters { AssemblyResolver=resolver });
using var baseline = ModuleDefinition.ReadModule(args[2],new ReaderParameters { AssemblyResolver=resolver });
int passed=0;
void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
void Check(string name,Action check) { check();passed++;Console.WriteLine("PASS: "+name); }

bool ReachesStrippedGui(MethodDefinition method,HashSet<string> visited)
{
    if (!visited.Add(method.FullName) || !method.HasBody) return false;
    if (method.Body.Instructions.Any(i=>i.Operand is string text && text=="Method unstripping failed")) return true;
    foreach(var instruction in method.Body.Instructions)
        if(instruction.Operand is MethodReference call && call.DeclaringType.FullName=="UnityEngine.GUI" &&
            ReachesStrippedGui(call.Resolve(),visited)) return true;
    return false;
}
MethodDefinition PickerDraw(ModuleDefinition module) => module.Types.Single(t=>t.FullName=="NugzzMenu.UI.ClothingColorPicker").Methods.Single(m=>m.Name=="Draw");
Check("Previous picker reaches the real stripped Unity DrawTexture overload",()=>
    Require(ReachesStrippedGui(PickerDraw(baseline),new()),"Baseline no longer reproduces the supplied error"));
Check("Replacement picker and shared texture helper avoid stripped GUI overloads",()=>
{
    Require(!ReachesStrippedGui(PickerDraw(menu),new()),"Picker reaches a throwing generated method");
    var texture=menu.Types.Single(t=>t.FullName=="NugzzMenu.Services.GUIFit").Methods.Single(m=>m.Name=="Texture");
    Require(!ReachesStrippedGui(texture,new()),"Shared helper reaches a throwing generated method");
});
var item=new ItemService();
var style=new GUIStyle();
void Draw() { float y=0; ClothingColorPicker.Draw(ref y,700,style,style,item,1); Require(y>0,"Layout did not advance"); }
Check("Expanded picker completes all controls over repeated redraws",()=>
{
    GUIFit.Click="Clothing:";
    for(int i=0;i<120;i++) Draw();
    Require(GUI.StrippedCalls==0 && GUIFit.Textures==27*120,"Stripped call or incomplete swatch grid");
    Require(GUIFit.Buttons==37*120,"Controls after the palette were not rendered");
});
Check("Palette drawing restores global GUI tint on failure",()=>
{
    GUI.color=new Color(0.9f,0.8f,0.7f,0.6f);
    GUIFit.ThrowTexture=true;
    try { Draw(); throw new Exception("Expected fixture error"); }
    catch(InvalidOperationException ex) when(ex.Message=="Fixture draw failure") { }
    finally { GUIFit.ThrowTexture=false; }
    Require(GUI.color.r==0.9f && GUI.color.g==0.8f && GUI.color.b==0.7f && GUI.color.a==0.6f,"Global tint leaked");
});
Check("Non-Repaint input events process selection without drawing swatches",()=>
{
    int reads=item.SwatchReads,textures=GUIFit.Textures;
    Event.current.type=EventType.MouseDown;
    GUIFit.Click="Tint 13"; Draw();
    Require(item.Selected==13 && item.SwatchReads==reads && GUIFit.Textures==textures,"Input event drew or skipped selection");
    Event.current.type=EventType.Repaint;
});
Check("Native selection restores defaults without spawning clothing",()=>
{
    GUIFit.Click="Native / No color"; Draw();
    Require(item.Selected==-1 && item.NativeSpawns==0,"Native option changed inventory");
});
Console.WriteLine(passed+" picker checks passed. Interop inspection and fixtures do not establish in-game visual QA.");
return 0;
