using BepInEx;
using UnityEngine;
using HarmonyLib;

namespace CarXDriftRacingMenuBindPlugin
{
    [BepInPlugin("org.bepinex.plugin.CarXDynostandKeyBind.DoubleBrackets", "DynostandKeyBind", "1.0.0")]
    public class DynostandKeyBindPlugin : BaseUnityPlugin
    {

        public void Start()
        {
            var instance = new Harmony("dynostandBindPatcher");
            instance.PatchAll(typeof(UIDynostandContextPatch));
            instance.PatchAll(typeof(GarageCar3dViewContextPatch));
        }

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
           if (Input.GetKeyDown(KeyCode.RightControl))
           {
               print("CURRENT STATE: " + States.current.GetType());
           }
           if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.M))
           {
                var InGameMenuState = FindObjectOfType<InGameMenuState>();
                if (InGameMenuState != null)
                {                    
                    var contextOwner = InGameMenuState.contextOwner;
                    if (contextOwner != null && contextOwner.topContext.GetType() != typeof(UIDynostandContext))
                    {
                        print("DynostandKeyBind: Switching to In Game Menu Dynostand");
                        contextOwner.ClearAll(); // he has "False" here
                        contextOwner.Add<GarageCar3dViewContext>();
                        contextOwner.Push<UIDynostandContext>();
                    }
                }   
                else
                {
                    print("DynostandKeyBind: Cannot find In Game Menu State");
                }
           }
        }
    }

    class UIDynostandContextPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIDynostandContext), "GotoGarage")]
        static bool GoToGaragePrefix(UIDynostandContext __instance)
        {
            if (States.current is not BaseRaceState && States.current.GetType() != typeof(InGameMenuState))
            {
                //Debug.Log("Original GoToGarage");
                return true;
            }
            Debug.Log("EXIT!");
            AnalyticsService.DynoTime(Time.time - __instance.m_entryTime);
            if (__instance.m_isDynoStateCurrent)
            {
                //MonoBehaviour.print("GoToGarage, m_isDynoStateCurrent is true");
                States.State<GarageGUIState>().GotoContext(__instance.m_dynoState.startContextOnExitToGarage);
                __instance.m_dynoState.startContextOnExitToGarage = null;
            }
            else
            {
                //Singleplayer wont save immediately without this line added
                __instance.ApplyChanges();
                //MonoBehaviour.print("GoToGarage, m_isDynoStateCurrent is false");
                __instance.Hide();
                //MonoBehaviour.print("Current state: " + States.current.GetType().ToString());
                //Modded, added surrounding state check
                if (!typeof(BaseRaceState).IsAssignableFrom(States.current.GetType()))
                {
                    Debug.Log("EXIT1");
                    //Modded, added state check and pop
                    if (States.current.GetType() == typeof(InGameMenuState))
                    {
                        Debug.Log("EXIT3");
                        //Remove top state
                        States.Pop();
                    }
                    //
                    else
                        __instance.owner.Push<UIGarageContext>();                  
                }
                else
                {
                    Debug.Log("EXIT2");
                    __instance.owner.ClearAll();
                }
                //
            }
            __instance.SetActiveCameraRotation(true);
            return false;
        }
    }

    class GarageCar3dViewContextPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GarageCar3dViewContext), "OnActivate")]
        static bool CarViewOnActivatePrefix(GarageCar3dViewContext __instance)
        {
            if (States.current.GetType() != typeof(InGameMenuState))
            {
                //Debug.Log("Original Garagecar OnActivate");
                return true;
            }
            //Replacement for base.OnActivate()
            __instance.ApplyHotkeys();
            __instance.m_gameManager.isUnityUIActive = true;
            __instance.boundsContext = __instance.CalculateBounds(__instance.transform as RectTransform, 1f, true);
            __instance.StartCoroutine(__instance.SetDefaultElement());
            __instance.raycaster = __instance.GetComponent<GraphicRaycaster>();
            //
            //base.OnActivate();
            foreach (RaceCar car in UnityEngine.Object.FindObjectsOfType<RaceCar>())
            {
                if (car.isLocalPlayer)
                {
                    //MonoBehaviour.print("Found local car :" + car.gameObject);
                    __instance.m_selectedRaceCar = car;
                    __instance.m_selectedCarData = car.metaInfo;
                    __instance.m_selectedCarView = car.gameObject;                       
                    return false;
                }
            }
            return false;           
        }

        [HarmonyPatch(typeof(GarageCar3dViewContext), "OnDeactivate")]
        [HarmonyPrefix]

        static bool OnDeactivatePrefix(GarageCar3dViewContext __instance)
        {
            if (States.current.GetType() != typeof(InGameMenuState))
            {
                //Debug.Log("Original GarageCar OnDeactivate");
                return true;
            }
            //Replacement for base.OnDeactivate()
            __instance.ReleaseHotkeys();
            __instance.m_gameManager.isUnityUIActive = false;
            //base.OnDeactivate();
            return false;
        }
    }
}
