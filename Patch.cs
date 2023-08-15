using HarmonyLib;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using static GameMessage;

[HarmonyPatch]
public static class Patch
{
    public static void GetUnfoundContrabandHidespots(VehicleInteraction io, List<string> hidespots)
    {
        var contrabandIOList = AccessTools.FieldRefAccess<VehicleInteraction, List<ContrabandIO>>(io, "contrabandIOList");
        var hiddenObjectsController = AccessTools.FieldRefAccess<VehicleInteraction, HiddenObjectsController>(io, "hiddenObjectsController");
        
        if (contrabandIOList != null)
        {
            for (int i = 0; i < contrabandIOList.Count; i++)
            {
                ContrabandIO contrabandIO = contrabandIOList[i];
                if (!(contrabandIO == null) && !contrabandIO.isDestroyed && contrabandIO.HasContraband())
                {
                    hidespots.Add(ContrabandDetailedPlacement.Translate(contrabandIO.detailedPlacement));
                }
            }
        }

        List<HiddenObjectsController.ExposedContraband> exposedContraband = hiddenObjectsController.GetExposedContraband();
        if (exposedContraband == null || exposedContraband.Count <= 0)
        {
            return;
        }

        for (int j = 0; j < exposedContraband.Count; j++)
        {
            HiddenObjectsController.ExposedContraband exposedContraband2 = exposedContraband[j];
            if (exposedContraband2 != null && !(exposedContraband2.item == null) && !(exposedContraband2.marker == null))
            {
                Marker_EC_HideSpot marker = exposedContraband2.marker;
                if (!(marker == null))
                {
                    hidespots.Add(ContrabandDetailedPlacement.Translate(marker.detailedPlacement));
                }
            }
        }
    }

    private static void GetMismatched(DocumentsData documents, Vehicle2 veh, Dictionary<string, List<string>> wrongs)
    {
        if (veh == null)
        {
            return;
        }

        Human human = veh.GetAIOwner();
        if (human == null)
        {
            human = veh.aiDriverArrested;
        }

        if (human == null)
        {
            return;
        }

        wrongs.Clear();

        List<string> docs = new List<string>();
        if (documents.IsFieldMismatched("NameSurname"))
        {
            docs.Add(PaperMistakeEntry.GetTitle(PaperMistakeEntry.Type.NameSurname));
        }

        if (documents.IsFieldMismatched("DateOfExpiration"))
        {
            docs.Add(PaperMistakeEntry.GetTitle(PaperMistakeEntry.Type.ExpirationDate));
        }

        if (documents.IsFieldMismatched("PassportNumber"))
        {
            docs.Add(PaperMistakeEntry.GetTitle(PaperMistakeEntry.Type.PassportNumber));
        }

        if (documents.IsFieldMismatched("Stamp"))
        {
            docs.Add(PaperMistakeEntry.GetTitle(PaperMistakeEntry.Type.Stamp));
        }

        if (documents.IsFieldMismatched("PaperColors"))
        {
            docs.Add(PaperMistakeEntry.GetTitle(PaperMistakeEntry.Type.PaperColor));
        }

        wrongs.Add("Docs", docs);

        List<string> v = new List<string>();

        if (documents.IsFieldMismatched("VehicleType"))
        {
            v.Add(PaperMistakeEntry.GetTitle(PaperMistakeEntry.Type.VehicleType));
        }

        if (veh.vcd.overweight)
        {
            v.Add(PaperMistakeEntry.GetTitle(PaperMistakeEntry.Type.VehicleWeight));
        }

        if (documents.IsFieldMismatched("Photo"))
        {
            v.Add(PaperMistakeEntry.GetTitle(PaperMistakeEntry.Type.Photo));
        }

        if (documents.IsFieldMismatched("VehicleID"))
        {
            v.Add(PaperMistakeEntry.GetTitle(PaperMistakeEntry.Type.VehicleID));
        }

        if (!Refs.inspectionZone.IsVehicleCargoValid(veh))
        {
            v.Add(PaperMistakeEntry.GetTitle(PaperMistakeEntry.Type.Cargo));
        }

        wrongs.Add("Vehicle", v);

        List<string> crime = new List<string>();
        if (veh.io.HasContraband())
        {
            List<string> hidespots = new List<string>();
            GetUnfoundContrabandHidespots(veh.io, hidespots);

            crime.AddRange(hidespots);
        }

        wrongs.Add("Contraband", crime);

        var fp = veh.io.faultyParts;

        if (fp.Count > 0)
        {
            List<string> parts = new List<string>();

            var tp1 = FaultyPart.Type.Wheel;
            if (fp.ContainsKey(tp1))
            {
                parts.Add(string.Format("{0} = {1}", FaultyPart.GetDescription(tp1), fp[tp1]));
            }

            tp1 = FaultyPart.Type.Window;
            if (fp.ContainsKey(tp1))
            {
                parts.Add(string.Format("{0} = {1}", FaultyPart.GetDescription(tp1), fp[tp1]));
            }

            tp1 = FaultyPart.Type.Light;
            if (fp.ContainsKey(tp1))
            {
                parts.Add(string.Format("{0} = {1}", FaultyPart.GetDescription(tp1), fp[tp1]));
            }

            tp1 = FaultyPart.Type.Mirror;
            if (fp.ContainsKey(tp1))
            {
                parts.Add(string.Format("{0} = {1}", FaultyPart.GetDescription(tp1), fp[tp1]));
            }

            tp1 = FaultyPart.Type.Bumper;
            if (fp.ContainsKey(tp1))
            {
                parts.Add(string.Format("{0} = {1}", FaultyPart.GetDescription(tp1), fp[tp1]));
            }

            tp1 = FaultyPart.Type.Bodywork;
            if (fp.ContainsKey(tp1))
            {
                parts.Add(string.Format("{0} = {1}", FaultyPart.GetDescription(tp1), fp[tp1]));
            }

            wrongs.Add("Parts", parts);
        }

        if (human.documents.dcd.forbidden)
        {
            List<string> forbidden = new List<string>();

            forbidden.Add(PaperMistakeEntry.GetTitle(PaperMistakeEntry.Type.DailyRestrictions));
            wrongs.Add("Restrictions", forbidden);
        }

        if (veh.aiDriver.driverIsPanic)
        {
            List<string> panic = new List<string>();
            panic.Add("PANIC");

            wrongs.Add("Driver", panic);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Barrier), "OnVehicleEntered")]
    private static void RegisterUpdatePatch(Barrier __instance, Vehicle2 vehicle)
    {
        DocumentsData documents = vehicle.GetAIOwner().documents;

        if (documents == null)
        {
            return;
        }

        Dictionary<string, List<string>> wrongs = new Dictionary<string, List<string>>();
        GetMismatched(documents, vehicle, wrongs);

        var dialog = DialogueController.GetDefaultInput();
        var texts = new List<string>();

        foreach (var w in wrongs)
        {
            if (w.Value.Count > 0)
            {
                texts.Add(string.Format("{0}:\n{1}", w.Key, string.Join("\n", w.Value)));
            }
        }

        var text = string.Join("\n", texts);

        Subtitles panel = Manager.ui.ShowPanel("Subtitles", 0.2f, null, 0f, false, false).GetComponent<Subtitles>();
        panel.Set(false, Subtitles.ActorType.Dispatcher, text, vehicle.GetAIOwner().GetTalkController());
        panel.Show(true, 10f);
        //File.WriteAllLines(@"c:\games\Contraband Police\current.txt", wrongs);
    }
}