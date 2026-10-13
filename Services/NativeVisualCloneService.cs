using System;
using System.Collections.Generic;
using UnityEngine;

namespace NugzzMenu.Services
{
    // Render-only copies: never instantiate a native NPC/prop's scripts or physics.
    internal static class NativeVisualCloneService
    {
        internal static Transform Copy(Transform source, Transform parent, Dictionary<int, Transform> transforms)
        {
            var skins = new List<KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRenderer>>();
            Transform model = new GameObject(source.name).transform;
            model.SetParent(parent, false);
            CopyHierarchy(source, model, transforms, skins);
            model.localScale = source.lossyScale;
            foreach (var skin in skins)
            {
                var bones = skin.Key.bones;
                var copies = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Transform>(bones.Length);
                for (int i = 0; i < bones.Length; i++)
                    if (bones[i] != null && transforms.TryGetValue(bones[i].GetInstanceID(), out Transform bone)) copies[i] = bone;
                    else throw new InvalidOperationException("Visual has a bone outside its hierarchy");
                skin.Value.bones = copies;
                if (skin.Key.rootBone != null && transforms.TryGetValue(skin.Key.rootBone.GetInstanceID(), out Transform root)) skin.Value.rootBone = root;
            }
            if (model.GetComponentsInChildren<Renderer>(true).Length == 0)
                throw new InvalidOperationException("Visual has no renderers");
            return model;
        }
        private static void CopyHierarchy(Transform source, Transform destination,
            Dictionary<int, Transform> transforms, List<KeyValuePair<SkinnedMeshRenderer, SkinnedMeshRenderer>> skins)
        {
            transforms[source.GetInstanceID()] = destination;
            MeshFilter mesh = source.GetComponent<MeshFilter>();
            MeshRenderer renderer = source.GetComponent<MeshRenderer>();
            if (mesh != null && mesh.sharedMesh != null && renderer != null)
            {
                destination.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                destination.gameObject.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            }
            SkinnedMeshRenderer skin = source.GetComponent<SkinnedMeshRenderer>();
            if (skin != null && skin.sharedMesh != null)
            {
                var copy = destination.gameObject.AddComponent<SkinnedMeshRenderer>();
                copy.sharedMesh = skin.sharedMesh; copy.sharedMaterials = skin.sharedMaterials;
                copy.localBounds = skin.localBounds; skins.Add(new(skin, copy));
            }
            for (int i = 0; i < source.childCount; i++)
            {
                Transform child = source.GetChild(i);
                if (child.name.EndsWith("LOD1", StringComparison.OrdinalIgnoreCase) || child.name.EndsWith("LOD2", StringComparison.OrdinalIgnoreCase)) continue;
                Transform copy = new GameObject(child.name).transform;
                copy.SetParent(destination, false); copy.localPosition = child.localPosition;
                copy.localRotation = child.localRotation; copy.localScale = child.localScale;
                CopyHierarchy(child, copy, transforms, skins);
            }
        }
    }
}
