using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace NugzzMenu.Services
{
    // Camera callbacks run after Animator evaluation and FinalIK's LateUpdate.
    // Keep delegates alive and detach them at shutdown; no execution-order guess.
    internal static class LocalAnimationRenderService
    {
        private static Camera.CameraCallback _builtIn;
        private static Il2CppSystem.Action<ScriptableRenderContext, Camera> _pipeline;
        internal static bool EnsureInitialized()
        {
            if (_pipeline != null) return true;
            try
            {
                _builtIn = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<Camera.CameraCallback>((Action<Camera>)(_ => Apply()));
                _pipeline = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<Il2CppSystem.Action<ScriptableRenderContext, Camera>>(
                    (Action<ScriptableRenderContext, Camera>)((_, _) => Apply()));
                Camera.onPreCull += _builtIn;
                RenderPipelineManager.beginCameraRendering += _pipeline;
                return true;
            }
            catch (Exception ex)
            {
                Shutdown();
                DebugLogService.Instance.VerboseWarning("Animation render callbacks unavailable: " + ex.Message);
                return false;
            }
        }
        internal static void Apply()
        {
            try { LocalBodyAnimationService.Instance.LateUpdate(); } catch { }
            try { LocalAnimationVisualService.Instance.LateUpdate(); } catch { }
            try { MotorcycleService.Instance.ApplyRiderPoses(); } catch { }
        }
        internal static void Shutdown()
        {
            try { if (_builtIn != null) Camera.onPreCull -= _builtIn; } catch { }
            try { if (_pipeline != null) RenderPipelineManager.beginCameraRendering -= _pipeline; } catch { }
            _builtIn = null; _pipeline = null;
        }
    }
}
