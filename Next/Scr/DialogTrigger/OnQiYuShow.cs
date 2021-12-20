﻿using HarmonyLib;

namespace SkySwordKill.Next.DialogTrigger
{
    [HarmonyPatch(typeof(QiYu.QiYuUIMag),"Show")]
    public class OnQiYuShow
    {
        public static int lastOption = 0;
        
        public static bool Prefix(QiYu.QiYuUIMag __instance,int id)
        {
            Main.LogInfo($"打开奇遇 ID : [{id}]");
            lastOption = 0;
            var env = new DialogEnvironment()
            {
                qiyuID = id
            };
            if (DialogAnalysis.TryTrigger("奇遇触发", env))
            {
                __instance.Close();
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}