﻿using HarmonyLib;
using JSONClass;
using KBEngine;
using UnityEngine;
using YSGame.Fight;

namespace SkySwordKill.Next.Patch;

/// <summary>
/// 战斗界面武器图标补充Patch
/// </summary>
[HarmonyPatch(typeof(UIFightWeaponItem),"SetWeapon")]
public class UIFightWeaponItemPatch
{
    [HarmonyPostfix]
    public static void Postfix(UIFightWeaponItem __instance,GUIPackage.Skill skill, ITEM_INFO weapon)
    {
        int id = weapon.itemId;
        if (id == -1) return;
        if (_ItemJsonData.DataDict.ContainsKey(id))
        {
            _ItemJsonData itemJsonData = _ItemJsonData.DataDict[id];
            var path = $"Assets/Item Icon/{ItemUIPatch.GetItemIconByKey(itemJsonData)}.png";

            if (!Main.Res.HaveAsset(path))
                path = $"Assets/Item Icon/1.png";
                
            if (Main.Res.TryGetAsset(path, asset =>
                {
                    if (asset is Texture2D texture)
                    {
                        __instance.IconImage.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0, 0));
                    }
                }))
            {
                Main.LogInfo($"物品 [{id}] 图标加载成功");
            }
        }
    }
}