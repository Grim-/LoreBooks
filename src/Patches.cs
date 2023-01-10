using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LoreBooks
{
    [HarmonyPatch(typeof(ItemDisplayOptionPanel))]
    public static class ItemDisplayOptionPanelPatches
    {
        [HarmonyPatch(nameof(ItemDisplayOptionPanel.GetActiveActions)), HarmonyPostfix]
        private static void EquipmentMenu_GetActiveActions_Postfix(ItemDisplayOptionPanel __instance, GameObject pointerPress, ref List<int> __result)
        {
            //skip adding options
            if (__instance.m_pendingItem == null || __instance.LocalCharacter.IsAI)
            {
                return;
            }

            foreach (var current in LoreBooksMod.Instance.StoredBooks)
            {
                if (__instance.m_pendingItem.ItemID == current.Key)
                {
                    //otherwise just add it
                    __result.Add(current.Key); 
                }
            }
        }



        [HarmonyPatch(nameof(ItemDisplayOptionPanel.ActionHasBeenPressed)), HarmonyPrefix]
        private static void EquipmentMenu_ActionHasBeenPressed_Prefix(ItemDisplayOptionPanel __instance, int _actionID)
        {
            Character owner = __instance.m_characterUI.TargetCharacter;
            UIBookPanel uIBookPanel = owner.CharacterUI.GetComponentInChildren<UIBookPanel>();
            Item CurrentItem = __instance.m_pendingItem;

            if (owner && CurrentItem && owner.Inventory.OwnsOrHasEquipped(CurrentItem.ItemID))
            {
                foreach (var StoredBook in LoreBooksMod.Instance.StoredBooks)
                {
                    //if the action ID, which for this is the itemsID is equal to 
                    if (_actionID == StoredBook.Key)
                    {                 
                        if (uIBookPanel)
                        {
                            uIBookPanel.ShowBook(StoredBook.Value);
                        }

                    }
                }
            }

        }


        [HarmonyPatch(nameof(ItemDisplayOptionPanel.GetActionText)), HarmonyPrefix]
        private static bool EquipmentMenu_GetActionText_Prefix(ItemDisplayOptionPanel __instance, int _actionID, ref string __result)
        {
            foreach (var CustomAction in LoreBooksMod.Instance.StoredBooks)
            {
                if (_actionID == CustomAction.Key)
                {
                    __result = $"Open Book ({CustomAction.Value.TitlePageContent})";
                    return false;
                }
            }
            return true;
        }


        [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
        public static class CharacterAwakePatch
        {
            static void Postfix(Character __instance)
            {
                LoreBooksMod.Instance.DelayDo(() =>
                {
                    if (__instance.IsLocalPlayer) LoreBooksMod.Instance.CreateBookUIForCharacter(__instance);
                }, 3f);
            }
        }
    }
}
