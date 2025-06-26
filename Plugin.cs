using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Zorro.Core;

namespace PassportPagination
{
    [BepInPlugin("radsi.pagination", "PassportPagination", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        static PassportManager passportManager;
        static Texture2D arrowImage;
        static int currentPage = 0;
        static GameObject buttonRight, buttonLeft;

        private class Patcher
        {
            static void ConfigureModButton(GameObject button, string name, Action onClick)
            {
                button.name = name;
                var uiButton = button.GetComponent<Button>();
                uiButton.onClick.RemoveAllListeners();
                uiButton.onClick.AddListener(() => onClick());
            }

            [HarmonyPatch(typeof(PassportManager), "Awake")]
            [HarmonyPostfix]
            static void PassportManagerAwakePostfix(PassportManager __instance)
            {
                passportManager = __instance;

                var originalButton = GameObject.Find("GAME/PassportManager/PassportUI/Canvas/Panel/Panel/BG/Options/Grid/UI_PassportGridButton");
                var parent = originalButton.transform.parent.parent.parent;

                buttonRight = UnityEngine.Object.Instantiate(originalButton);
                buttonLeft = UnityEngine.Object.Instantiate(buttonRight);

                buttonRight.transform.SetParent(parent, false);
                buttonLeft.transform.SetParent(parent, false);

                buttonRight.transform.localScale = new Vector3(1f, 0.4f, 1f);
                buttonLeft.transform.localScale = new Vector3(-1f, 0.4f, 1f);

                buttonRight.transform.GetChild(1).GetChild(0).GetComponent<RawImage>().texture = arrowImage;
                buttonLeft.transform.GetChild(1).GetChild(0).GetComponent<RawImage>().texture = arrowImage;

                buttonRight.transform.localPosition = new Vector3(273f, -182f, 0f);
                buttonLeft.transform.localPosition = new Vector3(-53f, -182f, 0f);

                UnityEngine.Object.Destroy(buttonRight.GetComponent<PassportButton>());
                UnityEngine.Object.Destroy(buttonLeft.GetComponent<PassportButton>());

                buttonRight.SetActive(false);
                buttonLeft.SetActive(false);

                ConfigureModButton(buttonRight, "right_mod", () =>
                {
                    currentPage = Mathf.Min(currentPage + 1, GetMaxPage());
                    UpdatePage();
                });

                ConfigureModButton(buttonLeft, "left_mod", () =>
                {
                    currentPage = Mathf.Max(currentPage - 1, 0);
                    UpdatePage();
                });
            }

            [HarmonyPatch(typeof(PassportManager), "OpenTab")]
            [HarmonyPostfix]
            static void PassportManagerOpenTabPostfix()
            {
                currentPage = 0;
                UpdatePage();
            }

            static void UpdatePage()
            {
                var list = Singleton<Customization>.Instance.GetList(passportManager.activeType);
                int start = currentPage * 18;

                for (int i = 0; i < passportManager.buttons.Length; i++)
                {
                    int index = start + i;
                    if (index < list.Length)
                        passportManager.buttons[i].SetButton(list[index], index);
                    else
                        passportManager.buttons[i].SetButton(null, -1);
                }

                var setActiveButton = typeof(PassportManager).GetMethod("SetActiveButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                setActiveButton?.Invoke(passportManager, null);

                int maxPage = GetMaxPage();
                buttonLeft.SetActive(currentPage > 0);
                buttonRight.SetActive(currentPage < maxPage);
            }

            static int GetMaxPage()
            {
                int total = Singleton<Customization>.Instance.GetList(passportManager.activeType).Length;
                return Mathf.Max((total - 1) / 18, 0);
            }
        }

        void Awake()
        {
            arrowImage = new Texture2D(2, 2);
            arrowImage.LoadImage(Resource1.arrow);

            new Harmony("radsi.pagination").PatchAll(typeof(Patcher));
        }
    }
}