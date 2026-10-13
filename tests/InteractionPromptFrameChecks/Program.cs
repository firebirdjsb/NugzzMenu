using Il2CppScheduleOne.UI;
using NugzzMenu.Services;
using UnityEngine;

int passed = 0;
void Check(string name, Action action)
{
    SessionAuthorityService.Instance.FeaturesAllowed = true;
    GameplayStateGateService.Instance.Blocked = false;
    action();
    passed++;
    Console.WriteLine("PASS: " + name);
}
void Require(bool value, string message)
{
    if (!value) throw new InvalidOperationException(message);
}
void Show(InteractionCanvas canvas, Sprite icon = null, string hint = "LMB") =>
    InteractionPromptDisplayService.ShowForCurrentFrame(canvas, default, "Harvest plant",
        default, icon, default, hint, default);
bool Visible(InteractionCanvas canvas) => canvas.Container.gameObject.Active && canvas.Canvas.enabled;

Check("Previous display path reproduces invisible trimmer prompt", () =>
{
    var canvas = new InteractionCanvas();
    canvas.NativeUpdate(true);
    canvas.EnableInteractionDisplay(default, "Harvest plant", default, new Sprite(), default, "", 1f, default, true);
    Require(!Visible(canvas), "Setting content alone must not activate the container.");
    canvas.NativeUpdate(true);
    Require(!Visible(canvas), "The equipped-tool rule must hide the next-frame request too.");
});
Check("Trimmer prompt is visible in the current frame", () =>
{
    var canvas = new InteractionCanvas();
    canvas.NativeUpdate(true);
    Show(canvas, new Sprite(), "");
    Require(Visible(canvas) && canvas.Message == "Harvest plant", "Valid action did not become visible.");
});
Check("Aim-away removes the custom prompt on the next Update", () =>
{
    var canvas = new InteractionCanvas();
    canvas.NativeUpdate(true);
    Show(canvas);
    canvas.NativeUpdate(true);
    Require(!Visible(canvas), "Old action survived without a fresh target.");
});
Check("Unequipping cannot retain a stale custom display request", () =>
{
    var canvas = new InteractionCanvas();
    canvas.NativeUpdate(true);
    Show(canvas);
    canvas.NativeUpdate(false);
    Require(!Visible(canvas), "Custom intent leaked into ordinary interaction rendering.");
});
Check("Native display intent is preserved", () =>
{
    var canvas = new InteractionCanvas { _interactionDisplayEnabledThisFrame = true };
    Show(canvas);
    Require(canvas._interactionDisplayEnabledThisFrame, "Native request was lost.");
    canvas.NativeUpdate(false);
    Require(Visible(canvas), "Independent native prompt was hidden.");
});
Check("Native world labels stay enabled when the custom action expires", () =>
{
    var canvas = new InteractionCanvas();
    Show(canvas);
    canvas.NativeUpdate(true, 1);
    Require(!canvas.Container.gameObject.Active && canvas.Canvas.enabled, "World label canvas was hidden.");
});
Check("Menu and native input ownership suppress the custom prompt", () =>
{
    var canvas = new InteractionCanvas();
    canvas.NativeUpdate(true);
    GameplayStateGateService.Instance.Blocked = true;
    Show(canvas);
    Require(!Visible(canvas) && canvas.Message == null, "Blocked input still displayed an action.");
});
Check("Unapproved multiplayer access cannot display a custom action", () =>
{
    var canvas = new InteractionCanvas();
    SessionAuthorityService.Instance.FeaturesAllowed = false;
    Show(canvas);
    Require(!Visible(canvas) && canvas.Message == null, "Authority guard was bypassed.");
});
Check("Missing icon retains the input hint", () =>
{
    var canvas = new InteractionCanvas();
    Show(canvas);
    Require(Visible(canvas) && canvas.Message == "[LMB] Harvest plant", "Missing sprite hid the input hint.");
});
Check("Unavailable native UI returns without a display request", () =>
{
    var canvas = new InteractionCanvas { Canvas = null };
    Show(canvas);
    Require(!canvas._interactionDisplayEnabledThisFrame && canvas.Message == null, "Incomplete UI was touched.");
});
Check("Failed display restores native request state", () =>
{
    var canvas = new InteractionCanvas { FailDisplay = true };
    try { Show(canvas); }
    catch (InvalidOperationException) { }
    Require(!canvas._interactionDisplayEnabledThisFrame, "Failed render left a stale display request.");
});
Console.WriteLine(passed + " prompt frame checks passed using the production renderer and a native UI fixture. In-game visual QA remains required.");
