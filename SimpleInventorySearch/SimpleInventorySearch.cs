using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using FrooxEngine;
using Elements.Core;
using FrooxEngine.UIX;
using Elements.Assets;
using System.Runtime.CompilerServices;

namespace SimpleInventorySearch;

public class SimpleInventorySearch : ResoniteMod
{
    public const string VERSION = "1.0.0";
    public override string Name => "SimpleInventorySearch";
    public override string Author => "art0007i";
    public override string Version => VERSION;
    public override string Link => "https://github.com/art0007i/SimpleInventorySearch/";
    public override void OnEngineInit()
    {
        Harmony harmony = new Harmony("me.art0007i.SimpleInventorySearch");
        harmony.PatchAll();
    }

    public static ConditionalWeakTable<BrowserDialog, string> filterDict = new();

    public static void DoFilter(Slot item, string filterStr, bool isfolder)
    {
        if (
            (isfolder && !filterStr.StartsWith("@")) ||
            string.IsNullOrEmpty(filterStr) ||
            !item.Components.Any(x => x is BrowserItem))
        {
            item.ActiveSelf = true;
            return;
        }
        if (filterStr.StartsWith("@"))
            filterStr = filterStr.Substring(1);

        item.ActiveSelf = item.Name.ToLower().Contains(filterStr) || new StringRenderTree(item.Name).GetRawString().ToLower().Contains(filterStr);
    }
    [HarmonyPatch(typeof(BrowserDialog), "OnAttach")]
    class SimpleInventorySearchPatch
    {

        public static int DEFAULT_ITEM_SIZE = BrowserDialog.DEFAULT_ITEM_SIZE;
        public static bool Prefix(BrowserDialog __instance, SyncRef<Text> ____selectedText, SyncRef<Slot> ____pathRoot, SyncRef<Slot> ____buttonsRoot, SyncRef<SlideSwapRegion> ____swapper)
        {
            UIBuilder uIBuilder = new UIBuilder(__instance.Slot);
            uIBuilder.HorizontalHeader(DEFAULT_ITEM_SIZE * 0.4f, out var header, out var content);
            uIBuilder = new UIBuilder(content);
            uIBuilder.HorizontalHeader(DEFAULT_ITEM_SIZE * 1.2f, out var header2, out content);
            uIBuilder = new UIBuilder(content);
            uIBuilder.HorizontalHeader(DEFAULT_ITEM_SIZE * 0.5f, out var header3, out var content2);
            uIBuilder = new UIBuilder(header);
            UIBuilder uIBuilder2 = uIBuilder;
            LocaleString text = "---";
            ____selectedText.Target = uIBuilder2.Text(in text);
            ____selectedText.Target.Color.Value = RadiantUI_Constants.TEXT_COLOR;

            // TODO: ineject this using transpiler instead of copying the entire source code
            var uib = new UIBuilder(header3);
            uib.VerticalHeader(DEFAULT_ITEM_SIZE * 2, out var left, out var right);
            var uib2 = new UIBuilder(left);
            RadiantUI_Constants.SetupDefaultStyle(uib2);
            var field = uib2.TextField(null);
            // TODO: add 4px bottom padding to match folder history
            field.Text.NullContent.Value = "Filter...";
            field.Editor.Target.FinishHandling.Value = TextEditor.FinishAction.NullOnWhitespace;
            field.Editor.Target.LocalEditingChanged += (e) =>
            {
                var filterStr = e.TargetString.Trim().ToLower();
                filterDict.Remove(__instance);
                filterDict.Add(__instance, filterStr);

                var grid1 = Traverse.Create(__instance).Field("_folderGrid").GetValue<SyncRef<GridLayout>>().Target?.Slot;
                var grid2 = Traverse.Create(__instance).Field("_itemGrid").GetValue<SyncRef<GridLayout>>().Target?.Slot;

                if (grid1 != null)
                    foreach (var item in grid1.Children)
                        DoFilter(item, filterStr, true);

                if (grid2 != null)
                    foreach (var item in grid2.Children)
                        DoFilter(item, filterStr, false);

            };
            //
            ____pathRoot.Target = right.Slot;//header3.Slot;

            uIBuilder = new UIBuilder(header2);
            uIBuilder.VerticalLayout(0f, 4f, Alignment.MiddleRight);
            ____buttonsRoot.Target = uIBuilder.Root;
            ____swapper.Target = content2.Slot.AttachComponent<SlideSwapRegion>();

            return false;
        }

    }

    [HarmonyPatch(typeof(BrowserDialog), "GenerateContent")]
    class NewContentPatch
    {
        public static void Postfix(BrowserDialog __instance, SyncRef<GridLayout> ____folderGrid, SyncRef<GridLayout> ____itemGrid)
        {
            if (____folderGrid.Target != null)
                ____folderGrid.Target.Slot.ChildAdded += (_, s) =>
                {
                    s.RunInUpdates(0, () =>
                    {
                        DoFilter(s, filterDict.GetOrCreateValue(__instance), true);
                    });
                };

            if (____itemGrid.Target != null)
                ____itemGrid.Target.Slot.ChildAdded += (_, s) =>
                {
                    s.RunInUpdates(0, () =>
                    {
                        DoFilter(s, filterDict.GetOrCreateValue(__instance), false);
                    });
                };
        }
    }
}
