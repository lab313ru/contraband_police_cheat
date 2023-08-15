using System;
using System.IO;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace Doorstop
{
    public class Entrypoint
    {
        private static bool patched = false;

        public static void Start()
        {
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private static void DoPatch()
        {
            var harmony = new Harmony("contrapolice");
            harmony.PatchAll();
            patched = true;
        }

        private static void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (!patched)
            {
                DoPatch();
            }
        }
    }
}