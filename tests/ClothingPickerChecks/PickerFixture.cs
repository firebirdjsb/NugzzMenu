// Narrow rendering fixture: it does not load Unity, Harmony, or a game save.
namespace UnityEngine
{
    public struct Rect { public float x, y, width, height; public Rect(float x, float y, float w, float h) { this.x=x; this.y=y; width=w; height=h; } }
    public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a=1) { this.r=r;this.g=g;this.b=b;this.a=a; } }
    public class GUIStyle { }
    public class Texture2D { public static Texture2D whiteTexture = new(); }
    public enum TextAnchor { MiddleLeft }
    public enum FontStyle { Normal }
    public enum EventType { Repaint, MouseDown, Layout }
    public class Event { public EventType type; public static Event current = new() { type=EventType.Repaint }; }
    public static class GUI
    {
        public static Color color = new(1,1,1);
        public static bool enabled = true;
        public static int StrippedCalls;
        public static void DrawTexture(Rect rect, Texture2D texture)
        { StrippedCalls++; throw new NotSupportedException("Method unstripping failed"); }
    }
}
namespace NugzzMenu.Services
{
    using UnityEngine;
    public enum LabelCategory { Subtitle }
    public sealed class ItemService
    {
        public int Selected=-1;
        public int NativeSpawns;
        public int SwatchReads;
        public string GetClothingColorLabel() => Selected < 0 ? "Native / No color" : "Tint " + Selected;
        public int ClothingColorCount => 27;
        public bool IsClothingColorSelected(int index) => Selected==index;
        public string GetClothingColorName(int index) => "Tint " + index;
        public Color GetClothingColorSwatch(int index) { SwatchReads++; return new(0.5f,0.25f,0.75f); }
        public void SelectClothingColor(int index) => Selected=index;
        public void SetClothingColorKey(string key) => Selected=key=="Yellow" ? 13 : key=="LightGrey" ? 1 : 9;
        public bool TryFindItem(string name,out string id) { id=name; return true; }
        public void SpawnNativeClothing(string name,int quantity) => NativeSpawns++;
    }
    public sealed class GUISystemService
    {
        public static GUISystemService Instance { get; }=new();
        public Color GetColorForCategory(LabelCategory category) => new(1,1,1);
    }
    public sealed class TMPHybridService
    {
        public static TMPHybridService Instance { get; }=new();
        public void Label(float x,float y,float w,float h,string text,Color color,int size,TextAnchor anchor,FontStyle style,bool wrap) { }
    }
    public static class GUIFit
    {
        public static string Click;
        public static int Buttons;
        public static int Textures;
        public static bool ThrowTexture;
        public static void Panel(Rect rect,GUIStyle style) { }
        public static bool Button(Rect rect,string text,GUIStyle style)
        {
            Buttons++;
            if (GUI.enabled && Click!=null && text.Contains(Click,StringComparison.Ordinal)) { Click=null; return true; }
            return false;
        }
        public static void Texture(Rect rect,Texture2D texture)
        {
            Textures++;
            if (ThrowTexture) throw new InvalidOperationException("Fixture draw failure");
        }
    }
}
