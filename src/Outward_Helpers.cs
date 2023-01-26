using SideLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LoreBooks
{
    public static class OutwardHelpers
    {
        public static string EMISSION_VALUE = "_EmissionColor";

        public static Vector3 CharacterBodyCenterOffset = new Vector3(0, 0.7f, 0);

        public static void UpdateWeaponDamage(Weapon WeaponInstance, DamageList newDamageList)
        {
            WeaponInstance.Damage.Clear();
            //just fucken nuke everything 
            WeaponInstance.Stats.BaseDamage = newDamageList;
            WeaponInstance.m_baseDamage = WeaponInstance.Stats.BaseDamage.Clone();
            WeaponInstance.m_activeBaseDamage = WeaponInstance.Stats.BaseDamage.Clone();
            WeaponInstance.baseDamage = WeaponInstance.Stats.BaseDamage.Clone();
            WeaponInstance.Stats.Attacks = SL_WeaponStats.GetScaledAttackData(WeaponInstance);
            //ta-da updated weapon damage
        }

        public static Renderer TryGetWeaponRenderer(Weapon weaponGameObject)
        {
            return weaponGameObject.LoadedVisual.GetComponentInChildren<BoxCollider>().GetComponent<Renderer>();
        }

        public static T GetFromAssetBundle<T>(string SLPackName, string AssetBundle, string key) where T : UnityEngine.Object
        {
            if (!SL.PacksLoaded)
            {
                return default(T);
            }

            return SL.GetSLPack(SLPackName).AssetBundles[AssetBundle].LoadAsset<T>(key);
        }

        public static Vector3 GetPositionAroundCharacter(Character _affectedCharacter, Vector3 PositionOffset = default(Vector3))
        {
            return _affectedCharacter.transform.position + PositionOffset;
        }

        public static Vector3 GetPositionInRadiusAroundCharacter(Character _affectedCharacter, Vector3 PositionOffset = default(Vector3), float Radius = 1f)
        {
            return _affectedCharacter.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * Radius;
        }
        public static List<T> GetTypeFromColliders<T>(Collider[] colliders) where T : Component
        {
            List<T> list = new List<T>();
            foreach (var col in colliders)
            {
                T type = col.GetComponentInChildren<T>();
                if (type != null)
                {
                    list.Add(type);
                }
            }
            return list;
        }


        /// <summary>
        /// Adds a DamageType and Value to an existing DamageList
        /// </summary>
        /// <param name="Damage"></param>
        /// <param name="damageType"></param>
        /// <param name="value"></param>
        public static void AddDamageType(DamageList Damage, DamageType.Types damageType, float value)
        {
            if (!HasDamageType(Damage, damageType))
            {
                Damage.Add(damageType);
                Damage[damageType].Damage = value;
            }
        }
        public static void RemoveDamageType(DamageList Damage, DamageType.Types damageType)
        {
            if (HasDamageType(Damage, damageType))
            {
                Damage.Remove(damageType);
            }
        }
        public static DamageType GetDamageTypeDamage(DamageList Damage, DamageType.Types damageType)
        {
            return Damage[damageType];
        }

        public static void AddDamageToDamageType(DamageList Damage, DamageType.Types damageType, float value, bool addIfNotExist = true)
        {
            if (HasDamageType(Damage, damageType))
            {
                GetDamageTypeDamage(Damage, damageType).Damage += value;
            }
            else
            {
                if (addIfNotExist) AddDamageType(Damage, damageType, value);
            }
        }
        public static void RemoveDamageFromDamageType(DamageList Damage, DamageType.Types damageType, float value)
        {
            if (HasDamageType(Damage, damageType))
            {
                GetDamageTypeDamage(Damage, damageType).Damage -= value;

                if (Damage[damageType].Damage <= 0)
                {
                    RemoveDamageType(Damage, damageType);
                }
            }
        }

        public static bool HasDamageType(DamageList Damage, DamageType.Types damageType)
        {
            return Damage[damageType] != null;
        }

        public static Tag GetTagDefinition(string TagName)
        {
            foreach (var item in TagSourceManager.Instance.m_tags)
            {

                if (item.TagName == TagName)
                {
                    return item;
                }
            }

            return default(Tag);
        }
        public static T CheckOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T comp = gameObject.GetComponent<T>();

            if (comp == null)
            {
                return gameObject.AddComponent<T>();

            }

            return comp;
        }

        public static IEnumerator TeleportToArea(Character Character, UIBookPanel bookPanel, AreaManager.AreaEnum Area)
        {
            if (!Character.InLocomotion || !Character.NextIsLocomotion || Character.PreparingToSleep)
            {
                yield break;
            }

            if (Character && Character.IsLocalPlayer)
            {
                //yield return bookPanel.FadeEffect(0, 1, 1f);

                yield return new WaitForSeconds(1f);

                Area target = AreaManager.Instance.GetArea(Area);

                if (target != null)
                {
                    bookPanel.Hide();
                    //yield return bookPanel.FadeEffect(1, 0, 0.001f);

                    CharacterManager.Instance.RequestAreaSwitch(Character, target, 0, 0, 0, "");
                }
            }

            yield break;
        }

        public static OutwardRegions GetActiveRegionFromSceneName(string SceneName)
        {
            for (int i = 0; i < AreaManager.AreaFamilies.Length; i++)
            {
                for (int j = 0; j < AreaManager.AreaFamilies[i].FamilyKeywords.Length; j++)
                {
                    if (SceneName.Contains(AreaManager.AreaFamilies[i].FamilyKeywords[j]))
                    {
                        switch (AreaManager.AreaFamilies[i].FamilyName)
                        {
                            case "Cierzo":
                                return OutwardRegions.Cierzo;
                            case "Monsoon":
                                return OutwardRegions.Monsoon;
                            case "Levant":
                                return OutwardRegions.Levant;
                            case "Berg":
                                return OutwardRegions.Berg;
                            case "Harmattan":
                                return OutwardRegions.Harmattan;
                            case "Sirocco":
                                return OutwardRegions.Sirocco;
                        }
                    }
                }
            }

            return OutwardRegions.NONE;
        }

        public static void DelayDo(Action OnAfterDelay, float DelayTime)
        {
            LoreBooksMod.Instance.StartCoroutine(DoAfterDelay(OnAfterDelay, DelayTime));
        }

        public static IEnumerator DoAfterDelay(Action OnAfterDelay, float DelayTime)
        {
            yield return new WaitForSeconds(DelayTime);
            OnAfterDelay.Invoke();
            yield break;
        }


        public static Dictionary<string, Image> CreatedHighlightButtons = new Dictionary<string, Image>();
        /// <summary>
        /// Creates a ItemButton Highlight for the given itemUID and stores this in a dictionary for retrieval
        /// </summary>
        /// <param name="itemUID"></param>
        /// <param name="ItemDisplay"></param>
        /// <param name="TintColor"></param>
        /// <param name="Size"></param>
        /// <param name="Parent"></param>
        /// <param name="SetActive"></param>
        /// <param name="PushToBack"></param>
        /// <returns></returns>
        public static Image CreateButtonHighlight(string itemUID, Color TintColor, Vector2 Size, Transform Parent = null, bool SetActive = true, bool PushToBack = true)
        {
            if (GetButtonHighlight(itemUID))
            {
                DestroyButtonHighLight(itemUID);
            }

            Image NewImage = CreateImageHighlight(TintColor, Size);

            if (Parent != null)
            {
                NewImage.transform.SetParent(Parent, false);
            }

            NewImage.gameObject.SetActive(SetActive);

            if (PushToBack)
            {
                NewImage.transform.SetAsFirstSibling();
            }

            CreatedHighlightButtons.Add(itemUID, NewImage);
            return NewImage;
        }

        public static Image CreateImageHighlight(Color TintColor, Vector2 Size)
        {
            GameObject gameObject = new GameObject("Highlight");
            RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Size;

            Image NewImage = gameObject.AddComponent<Image>();
            NewImage.color = TintColor;
            return NewImage;
        }

        public static bool HasButtonHighLight(string itemUID)
        {
            return CreatedHighlightButtons.ContainsKey(itemUID) && CreatedHighlightButtons[itemUID] != null;
        }

        public static Image GetButtonHighlight(string itemUID)
        {
            if (CreatedHighlightButtons.ContainsKey(itemUID))
            {
                return CreatedHighlightButtons[itemUID];
            }

            return null;
        }

        public static void DestroyALLButtonHighLights(string itemUID)
        {
            foreach (var highlight in CreatedHighlightButtons)
            {
                DestroyButtonHighLight(highlight.Key);
            }

            CreatedHighlightButtons.Clear();
        }

        public static void DestroyButtonHighLight(string itemUID)
        {
            if (CreatedHighlightButtons.ContainsKey(itemUID))
            {
                GameObject.Destroy(CreatedHighlightButtons[itemUID].gameObject);
                CreatedHighlightButtons.Remove(itemUID);
            }
        }


        /// <summary>
        /// Helper function for Finding all the Characters around the Target, Predicate can be used to decide what is a valid target and what isnt, OnTargetFound can be used to do something to the CurrentFoundTarget, returns Target count.
        /// </summary>
        /// <param name="Target"></param>
        /// <param name="Radius"></param>
        /// <param name="TargetCap"></param>
        /// <param name="Predicate"></param>
        /// <param name="OnTargetFound"></param>
        /// <returns></returns>
        public static int GetCharactersAroundTarget(Character Target, float Radius, int TargetCap, Func<Character, bool> Predicate, Action<Character> OnTargetFound)
        {
            List<Character> FoundCharacters = new List<Character>();
            CharacterManager.Instance.FindCharactersInRange(Target.transform.position, Radius, ref FoundCharacters);

            int Targets = 0;

            foreach (var Char in FoundCharacters)
            {
                if (Targets > TargetCap)
                {
                    break;
                }

                if (Predicate(Char))
                {
                    OnTargetFound.Invoke(Char);
                    Targets++;
                }
            }

            return Targets;
        }

        public static GameObject ChangeParticleSystemColor(GameObject VFXGameObject, Color NewColor)
        {
            foreach (var item in VFXGameObject.GetComponentsInChildren<ParticleSystem>())
            {
                var ps = item.main;
                ps.startColor = NewColor;
            }

            return VFXGameObject;
        }

        public static Color GetDamageTypeColor(DamageType.Types Type)
        {
            switch (Type)
            {
                case DamageType.Types.Physical:
                    return Color.white;
                case DamageType.Types.Ethereal:
                    return Color.magenta;
                case DamageType.Types.Decay:
                    return Color.green;
                case DamageType.Types.Electric:
                    return Color.yellow;
                case DamageType.Types.Frost:
                    return Color.cyan;
                case DamageType.Types.Fire:
                    return Color.red;
                case DamageType.Types.Raw:
                    return Color.grey;
            }

            return Color.white;
        }

        public static void TryGenerateAndEquipItemByItemID(Character Character, int ItemID)
        {
            List<Item> OwnedItems = null;
            Item TargetItem = null;

            if (Character.Inventory != null)
            {
                OwnedItems = Character.Inventory.GetOwnedItems(ItemID);
            }

            if (OwnedItems != null && OwnedItems.Count > 0)
            {
                //exists use first found
                TargetItem = OwnedItems[0];
            }
            else
            {
                //doesnt exist create
                TargetItem = ItemManager.Instance.GenerateItemNetwork(ItemID);
            }

            //if all good and it is equipment
            if (TargetItem != null && TargetItem is Equipment)
            {
                TargetItem.ChangeParent(Character.Inventory.Pouch.transform);
                TargetItem.ForceUpdateParentChange();
                Character.Inventory.Equipment.EquipItem(TargetItem as Equipment);
            }
        }


        public static void ReduceRemainingCooldowns(Character Target, float reduceAmount)
        {
            foreach (var item in Target.Inventory.SkillKnowledge.m_learnedItems)
            {
                if (item is Skill skill)
                {
                    if (skill.m_remainingCooldownTime > 0)
                    {
                        skill.m_remainingCooldownTime -= reduceAmount;
                    }
                }
            }
        }

        public enum OutwardRegions
        {
            NONE,
            Cierzo,
            Monsoon,
            Levant,
            Berg,
            Harmattan,
            Sirocco
        }
    }
}
