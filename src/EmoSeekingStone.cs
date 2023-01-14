using SideLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LoreBooks
{
    public class EmoSeekingStone : MonoBehaviour
    {
        #region Visual and Transforms
        private const string ItemPrefabName = "SeekingStone(Clone)";
        private const string CompassPivotTransformName = "Pivot";
        private const string CompassIndicatorTransformName = "Indicator";

        private Transform Pivot;
        private Transform Indicator;
        private Material _EmissionMaterial;
        #endregion
        ///timer
        private float Timer = 0;
        private float UpdateTick = 0.3f;
        //detection
        private float LocalDetectionRange = 30f;
        private float BaseIntensity = 10f;
        private float MinProximityIntensity = 0.125f;
        private float MaxProximityIntensity = 14f;
        private float FacingTargetIntensityBoost = 9f;
        private float IsFacingMaxAngle = 10f;
        private float CurrentProximityItensity = 0;
        private float DistanceThreshold = 2f;

        //options
        private bool BoostIntensityIfFacing = true;
        private bool FaceTarget = true;

        private int _TargetItemID = 5100060;
        private int _TrackedItemID;
        private Item _TrackedItem;

        private Equipment _ParentEquipment;
        private EmoProximityDetectionType DetectionType = EmoProximityDetectionType.GATHERABLE;
        private EmoSeekingStoneMode StoneMode = EmoSeekingStoneMode.PROXIMITY;

        #region Detection Type Predicates and Colors
        private Func<TreasureChest, bool> LootPredicate;
        private Func<CharacterAI, bool> AIPredicate;
        private Func<Gatherable, bool> GatherablePredicate;
        private Func<Item, bool> ItemPredicate;
        private Color GatherableColor = Color.green;
        private Color DangerColor = Color.red;
        private Color LootColor = Color.yellow;
        private Color GPSColor = Color.cyan;
        #endregion

        public Action<object, Vector3> OnThingDetected;

        private SeekingStoneRoute CurrentRoute = null;
        private int CurrentRouteIndex = 0;
        private List<SeekingStoneRoute> Route = new List<SeekingStoneRoute>();

        private bool IsPulseRunning = false;

        private GameObject TargetVFXPrefab = null;
        private GameObject TargetVFXInstance = null;

        public Equipment ParentEquipment
        {
            get
            {
                if (_ParentEquipment == null)
                {
                    _ParentEquipment = GetComponent<Equipment>();
                }

                if (_ParentEquipment == null)
                {
                    _ParentEquipment = GetComponentInParent<Equipment>();
                }

                return _ParentEquipment;
            }
        }

        public virtual void Awake()
        {

        }
        public virtual void Start()
        {
            InitMaterialReference();
            InitConditions();
            //InitTestRoute();
            InitTargetIndicator();
        }
        private void InitMaterialReference()
        {
            if (Pivot == null)
            {
                Pivot = ParentEquipment.LoadedVisual.gameObject.transform.Find($"{ItemPrefabName}/{CompassPivotTransformName}");
            }

            if (Indicator == null)
            {
                Indicator = ParentEquipment.LoadedVisual.gameObject.transform.Find($"{ItemPrefabName}/{CompassPivotTransformName}/{CompassIndicatorTransformName}");
            }

            if (_EmissionMaterial == null)
            {
                //try get material reference, this is specific to this particular prefab because of the extra layer for the compass indicator
                _EmissionMaterial = ParentEquipment.LoadedVisual.gameObject.transform.Find($"{ItemPrefabName}/{CompassPivotTransformName}/{CompassIndicatorTransformName}").GetComponent<MeshRenderer>().material;
            }
        }
        private void InitConditions()
        {
            LootPredicate = (container) =>
            {
                return container.HasAnyDrops && !container.IsEmpty;
            };

            AIPredicate = (ai) =>
            {
                return ai.Character.Stats.CurrentHealth > 0;
            };

            GatherablePredicate = (gatherable) =>
            {
                return gatherable.CanGather;
            };

            ItemPredicate = (item) =>
            {
                if (item is SelfFilledItemContainer)
                {
                    SelfFilledItemContainer itemContainer = item as SelfFilledItemContainer;

                    return !itemContainer.IsEmpty && itemContainer.IsPickable;
                }
                else
                {
                    return item.CanBePutInInventory;
                }
            };
        }
        private void InitTestRoute()
        {
            List<Vector3> Positions = new List<Vector3>();
            Positions.Add(new Vector3(1788.987f, 5.1868f, 1798.337f));
            //1788.987 5.1868 1798.337
            Positions.Add(new Vector3(1721.597f, 6.3845f, 1832.842f));
            //1721.597 6.3845 1832.842
            Positions.Add(new Vector3(1646.9f, 6.3845f, 1859.733f));
            //1646.9 6.3845 1859.733
            Positions.Add(new Vector3(1672.492f, 5.0721f, 1885.124f));
            //1672.492 5.0721 1885.124
            Positions.Add(new Vector3(1699.55f, 5.0721f, 1899.044f));
            //1699.55 5.0721 1899.044
            AddNewRoute("TutRoute", Positions);
            SetCurrentRouteByName("TutRoute");

            SetMode(EmoSeekingStoneMode.GPS);
        }
        private void InitTargetIndicator()
        {
            TargetVFXPrefab = OutwardHelpers.GetFromAssetBundle<GameObject>("lorebooks", "emoseekingstone", "RuneStoneTarget");
            if (TargetVFXInstance != null)
            {
                Destroy(TargetVFXInstance);
            }

            if (TargetVFXPrefab != null)
            {
                TargetVFXInstance = Instantiate(TargetVFXPrefab);
                HideTargetIndicator();
            }
        }

        #region Target Indicator
        public void SetTargetIndicatorPosition(Vector3 Position)
        {
            if (TargetVFXInstance != null)
            {
                TargetVFXInstance.transform.position = Position;
            }
        }
        public void ShowTargetIndicator()
        {
            if (TargetVFXInstance != null)
            {
                TargetVFXInstance.gameObject.SetActive(false);
            }
        }
        public void HideTargetIndicator()
        {
            if (TargetVFXInstance != null)
            {
                TargetVFXInstance.gameObject.SetActive(false);
            }
        }
        #endregion

        public virtual void Update()
        {
            Timer += Time.deltaTime;

            if (Timer > UpdateTick)
            {
                if (CanRun())
                {
                    Detect();
                }

                Timer = 0;
            }

            if (CanRun())
            {
                if (CustomKeybindings.GetKey(LoreBooksMod.EXTRA_ACTION_KEY_MOD) && CustomKeybindings.GetKeyDown(LoreBooksMod.EXTRA_ACTION_KEY) && StoneMode == EmoSeekingStoneMode.PROXIMITY)
                {
                    DetectionType = DetectionType.Next();

                    if (ParentEquipment.OwnerCharacter != null)
                    {
                        ParentEquipment.OwnerCharacter.CharacterUI.ShowInfoNotification($"Detection Mode [{DetectionType}]");
                    }
                    DisableEmission();
                }
            }
        }
        private void Detect()
        {
            switch (StoneMode)
            {
                case EmoSeekingStoneMode.PROXIMITY:
                    DoProximityModeDetection();
                    break;
                case EmoSeekingStoneMode.GPS:
                    DoGPSModeDetection();
                    break;
            }
        }
        public void SetMode(EmoSeekingStoneMode mode)
        {
            StoneMode = mode;

            switch (StoneMode)
            {
                case EmoSeekingStoneMode.PROXIMITY:
                    break;
                case EmoSeekingStoneMode.GPS:
                    SetEmissionColor(GPSColor, BaseIntensity);
                    break;
            }
        }

        #region Proximity Methods
        private void DoProximityModeDetection()
        {
            Collider[] colliders = Physics.OverlapSphere(ParentEquipment.OwnerCharacter.transform.position, DetectionType == EmoProximityDetectionType.AI ? LocalDetectionRange + 3f : LocalDetectionRange, LayerMask.GetMask("Characters", "WorldItem"));

            switch (DetectionType)
            {
                case EmoProximityDetectionType.NONE:

                    DisableEmission();

                    break;

                case EmoProximityDetectionType.GATHERABLE:

                    if (!DoDetectionType(colliders, GatherableColor, true, GatherablePredicate))
                    {
                        DisableEmission();
                    }

                    break;

                case EmoProximityDetectionType.AI:
                    if (!DoDetectionType(colliders, DangerColor, true, AIPredicate))
                    {
                        DisableEmission();
                    }

                    break;

                case EmoProximityDetectionType.LOOT:

                    if (!DoDetectionType(colliders, LootColor, true, LootPredicate) && !DoDetectionType<Item>(colliders, LootColor, true, ItemPredicate))
                    {
                        DisableEmission();
                    }

                    break;

                case EmoProximityDetectionType.ALL:

                    //priority order AI > Gatherables > Loot
                    //todo : dont like how it's done, should have inversed DoDetectionType

                    if (DoDetectionType<CharacterAI>(colliders, DangerColor, true, AIPredicate))
                    {

                    }
                    else if (DoDetectionType<Gatherable>(colliders, GatherableColor, true, GatherablePredicate))
                    {

                    }
                    else if (DoDetectionType<TreasureChest>(colliders, LootColor, true, LootPredicate) || DoDetectionType<Item>(colliders, LootColor, true, ItemPredicate))
                    {

                    }
                    else
                    {
                        DisableEmission();
                    }

                    break;

                    //case EmoProximityDetectionType.TARGETITEM:
                    //    if (DoDetectionType<Item>(colliders, Color.magenta, true, (item) =>
                    //    {
                    //        return item.ItemID == _TargetItemID;
                    //    }))
                    //    {
                    //        _TrackedItem = Item;
                    //    }
                    //    else
                    //    {
                    //        DisableEmission();
                    //    }
                    //    break;
            }
        }
        private bool DoDetectionType<T>(Collider[] colliders, Color DetectionColor, bool ScaleIntensityWithDistance, Func<T, bool> Condition) where T : Component
        {
            if (ColliderHasComponent<T>(colliders))
            {
                List<T> foundType = FindComponentsInColliders<T>(colliders, Condition);

                //sort the list by distance
                foundType.Sort((x, y) => { return (ParentEquipment.OwnerCharacter.transform.position - x.transform.position).sqrMagnitude.CompareTo((ParentEquipment.OwnerCharacter.transform.position - y.transform.position).sqrMagnitude); });

                if (foundType.Count > 0)
                {
                    float distance = Vector3.Distance(ParentEquipment.OwnerCharacter.transform.position, foundType[0].transform.position);
                    CurrentProximityItensity = (Mathf.Clamp(1.0f - distance / LocalDetectionRange, MinProximityIntensity, MaxProximityIntensity) * BaseIntensity);
                    if (ScaleIntensityWithDistance)
                    {

                        if (IsPlayerFacing(foundType[0].transform) && BoostIntensityIfFacing)
                        {
                            SetEmissionColor(DetectionColor, (CurrentProximityItensity + FacingTargetIntensityBoost));
                        }
                        else
                        {
                            SetEmissionColor(DetectionColor, CurrentProximityItensity);
                        }

                    }
                    else
                    {
                        SetEmissionColor(DetectionColor, BaseIntensity);
                    }

                    if (FaceTarget)
                    {
                        FaceIndicatorTo(foundType[0].transform);
                    }

                    OnThingDetected?.Invoke(foundType[0], foundType[0].transform.position);
                    return true;
                }
                else
                {
                    DisableEmission();
                    return false;
                }
            }

            return false;
        }
        /// <summary>
        /// Faces the Indicator towards the target Transform.
        /// </summary>
        /// <param name="target"></param>
        private void FaceIndicatorTo(Transform target)
        {
            //THANKS @DMGregory On Game Dev StackExchange this was totally beyond me lol
            var xToY = Quaternion.LookRotation(Vector3.forward, Vector3.left);
            var yToTarget = Quaternion.LookRotation(Pivot.transform.forward, target.position - Pivot.transform.position);
            Pivot.transform.rotation = yToTarget * xToY;
        }
        /// <summary>
        /// Faces the Indicator towards the target Position in World Space.
        /// </summary>
        /// <param name="target"></param>
        private void FaceIndicatorTo(Vector3 target)
        {
            //THANKS @DMGregory On Game Dev StackExchange this was totally beyond me lol
            var xToY = Quaternion.LookRotation(Vector3.forward, Vector3.left);
            var yToTarget = Quaternion.LookRotation(Pivot.transform.forward, target - Pivot.transform.position);
            Pivot.transform.rotation = yToTarget * xToY;
        }
        /// <summary>
        /// Returns true if the angle difference between player and target is less than IsFacingMaxAngle
        /// </summary>
        /// <param name="targetTransform"></param>
        /// <returns></returns>
        private bool IsPlayerFacing(Transform targetTransform)
        {
            return Vector3.Angle(ParentEquipment.OwnerCharacter.transform.forward, targetTransform.position - ParentEquipment.OwnerCharacter.transform.position) < IsFacingMaxAngle;
        }

        private List<T> FindComponentsInColliders<T>(Collider[] colliders, Func<T, bool> Condition = null) where T : Component
        {
            List<T> foundList = new List<T>();

            foreach (var col in colliders)
            {

                if (col.transform.root.name == ParentEquipment.OwnerCharacter.transform.root.name)
                {
                    continue;
                }

                if (col.gameObject == ParentEquipment.gameObject)
                {
                    continue;
                }

                T foundThing = col.GetComponentInChildren<T>();

                if (foundThing == null)
                {
                    foundThing = col.GetComponentInParent<T>();
                }

                if (foundThing != null)
                {
                    if (Condition == null)
                    {
                        foundList.Add(foundThing);
                    }
                    else
                    {
                        if (Condition.Invoke(foundThing))
                        {
                            foundList.Add(foundThing);
                        }
                    }

                }
            }

            return foundList;
        }
        private bool ColliderHasComponent<T>(Collider[] colliders)
        {
            foreach (var col in colliders)
            {
                //Skip the player and anything parented to the player
                if (col.transform.root.name == ParentEquipment.OwnerCharacter.transform.root.name)
                {
                    continue;
                }

                //skip the weapon itself
                if (col.gameObject == ParentEquipment.gameObject)
                {
                    continue;
                }

                if (col.GetComponentInChildren<T>() != null)
                {
                    return true;
                }
                else if (col.GetComponentInParent<T>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region GPS Methods
        private void DoGPSModeDetection()
        {
            Vector3 CurrentPosition = ParentEquipment.OwnerCharacter.transform.position;

            if (CurrentRoute != null)
            {
                Vector3 ClosestPointInRoute = CurrentRoute.Positions[CurrentRouteIndex];

                FaceIndicatorTo(ClosestPointInRoute);

                float distanceFromClosestPointInRoute = Vector3.Distance(ClosestPointInRoute, CurrentPosition);

                if (distanceFromClosestPointInRoute <= DistanceThreshold)
                {
                    CurrentRoute.Positions.RemoveAt(CurrentRouteIndex);

                    //LoreBooksMod.Log.LogMessage($"Seeking Stone : Close to {CurrentRouteIndex} Route Position");

                    DoEmissionPulse();

                    if (RouteHasNextPosition(CurrentRouteIndex, CurrentRoute))
                    {
                        CurrentRouteIndex++;
                        //LoreBooksMod.Log.LogMessage($"Seeking Stone : Moving to Next Way Point {CurrentRoute.RouteName}");
                    }
                    else
                    {
                        //LoreBooksMod.Log.LogMessage($"Seeking Stone : Reached end of {CurrentRoute.RouteName}");
                        CurrentRoute = null;
                        DisableEmission();
                    }
                }
            }
        }
        public void AddNewRoute(string routeName, List<Vector3> Positions)
        {
            SeekingStoneRoute emoGPSData = Route.Find(x => x.RouteName == routeName);
            if (emoGPSData == null)
            {
                Route.Add(new SeekingStoneRoute(routeName, Positions));
            }
            else
            {
                LoreBooksMod.Log.LogMessage($"Seeking Stone : A Route Already Exists with name {routeName}");
            }

        }
        public void RemoveRoute(string routeName)
        {
            SeekingStoneRoute emoGPSData = Route.Find(x => x.RouteName == routeName);
            if (emoGPSData != null)
            {
                Route.Remove(emoGPSData);
            }
        }
        public void ClearAllRouteData()
        {
            if (Route != null)
            {
                Route.Clear();
            }
        }
        public void SetCurrentRouteByName(string routeName, bool startAtNearestPoint = false)
        {
            SeekingStoneRoute emoGPSData = Route.Find(x => x.RouteName == routeName);

            if (emoGPSData != null)
            {
                CurrentRoute = emoGPSData;

                if (startAtNearestPoint)
                {
                    CurrentRouteIndex = FindNearestPointOnRoute(ParentEquipment.OwnerCharacter.transform.position, emoGPSData);
                }
                else
                {
                    CurrentRouteIndex = 0;
                }

                LoreBooksMod.Log.LogMessage($"Seeking Stone : Setting new Route {routeName} Index {CurrentRouteIndex}");
            }
        }
        public bool RouteHasNextPosition(int CurrentRouteIndex, SeekingStoneRoute CurrentRoute)
        {
            return CurrentRouteIndex + 1 <= CurrentRoute.Positions.Count;
        }
        private int FindNearestPointOnRoute(Vector3 CurrentPosition, SeekingStoneRoute emoGPSData)
        {
            int closestPointIndex = 0;

            float CurrentFurthestDistance = Mathf.Infinity;

            for (int i = 0; i < emoGPSData.Positions.Count; i++)
            {
                float distanceFromPointToPlayer = Vector3.Distance(emoGPSData.Positions[i], CurrentPosition);

                if (distanceFromPointToPlayer < CurrentFurthestDistance)
                {
                    closestPointIndex = i;
                    CurrentFurthestDistance = distanceFromPointToPlayer;
                }
            }

            return closestPointIndex;
        }

        #endregion

        #region Emission Methods
        /// <summary>
        /// Use RGB 0-1 and pass an intensity value to make brigher
        /// </summary>
        /// <param name="newColor"></param>
        /// <param name="intensity"></param>
        public void SetEmissionColor(Color newColor, float intensity = 1f)
        {
            if (_EmissionMaterial != null)
            {
                _EmissionMaterial.SetColor("_EmissionColor", newColor * intensity);
            }
        }

        /// <summary>
        /// Sets the Emission color to a clear color effectively disabling it.
        /// </summary>
        public void DisableEmission()
        {
            if (_EmissionMaterial != null)
            {
                _EmissionMaterial.SetColor("_EmissionColor", Color.clear);
            }
        }
        private void DoEmissionPulse()
        {
            if (!IsPulseRunning)
            {
                StartCoroutine(PulseIntensity(BaseIntensity, 50f, 0.25f, 1f));
            }
        }

        private IEnumerator PulseIntensity(float originalIntensity, float newIntensity, float lerpTime, float delay)
        {
            IsPulseRunning = true;
            yield return PulseEmissionTo(Color.cyan, newIntensity, lerpTime);
            yield return new WaitForSeconds(delay);
            yield return PulseEmissionTo(Color.cyan, originalIntensity, lerpTime);
            IsPulseRunning = false;
            yield break;
        }

        private IEnumerator PulseEmissionTo(Color newColor, float newIntensity, float time)
        {
            Color CurrentColor = _EmissionMaterial.GetColor("_EmissionColor");

            float timer = 0;
            while (timer < time)
            {
                _EmissionMaterial.SetColor("_EmissionColor", Color.Lerp(CurrentColor, newColor * newIntensity, Timer / time));
                timer += Time.deltaTime;
                yield return null;
            }

            yield break;
        }

        #endregion

        /// <summary>
        /// Returns true when the components ParentEquiment isn't null and that it is currently equipped by a local player.
        /// </summary>
        /// <returns></returns>
        private bool CanRun()
        {
            return ParentEquipment != null && ParentEquipment.OwnerCharacter != null && ParentEquipment.OwnerCharacter.IsLocalPlayer && ParentEquipment.IsEquipped;
        }

        public enum EmoProximityDetectionType
        {
            NONE = 0,
            AI = 1,
            GATHERABLE = 2,
            LOOT = 4,
            ALL = 8
            //TARGETITEM,
        }

        public enum EmoSeekingStoneMode
        {
            PROXIMITY,
            GPS
        }
    }

    [System.Serializable]
    public class SeekingStoneRoute
    {
        public string RouteName;
        public List<Vector3> Positions;

        public SeekingStoneRoute(string routeName, List<Vector3> positions)
        {
            this.RouteName = routeName;
            this.Positions = positions;
        }
    }
}
