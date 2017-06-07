
#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;

#endregion

namespace RoomToFamily
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            
            foreach (Room item in GetRooms(doc, app))
            {
                GetDevices(item, doc);
            }
           
            return Result.Succeeded;
        }

        public List<Room> GetRooms(Document doc, Application app)
        {
            List<Room> roomList = new List<Room>();
            List<BoundingBoxXYZ> boxList = new List<BoundingBoxXYZ>();
           
            foreach (Document d in app.Documents)
            {
                if (d.IsLinked)
                {
                    FilteredElementCollector roomLinkedCollector = new FilteredElementCollector(d).OfCategory(BuiltInCategory.OST_Rooms);

                    string tempLinkedRoom = string.Empty;
                    foreach (Room e in roomLinkedCollector)
                    {
                        roomList.Add(e);
                        tempLinkedRoom += e.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsString() + " " + e.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() + "\n";
                    }
                    //TaskDialog.Show("Linked Room", tempLinkedRoom);
                }
            }
            return roomList;
        }

        public List<FamilyInstance> GetLighting(Document doc, Room room)
        {
            BoundingBoxIntersectsFilter filter = Filter(room);
            List<FamilyInstance> lightingList = new List<FamilyInstance>();
            FilteredElementCollector roomCollector = new FilteredElementCollector(doc).
                                                        OfClass(typeof(FamilyInstance)).
                                                        OfCategory(BuiltInCategory.OST_LightingDevices).
                                                        WherePasses(filter);
            string temp = string.Empty;
            foreach (FamilyInstance e in roomCollector)
            {

                lightingList.Add(e);
                temp += e.Id + " " + e.Name + "\n";
            }
            //if (temp != string.Empty)
                //TaskDialog.Show("LightingDevices", temp);
            return lightingList;
        }

        public List<FamilyInstance> GetLightingFixtures(Document doc, Room room)
        {
            BoundingBoxIntersectsFilter filter = Filter(room);
            List<FamilyInstance> lightingFixturesList = new List<FamilyInstance>();
            FilteredElementCollector roomCollector = new FilteredElementCollector(doc).
                                                        OfClass(typeof(FamilyInstance)).
                                                        OfCategory(BuiltInCategory.OST_LightingFixtures).
                                                        WherePasses(filter);
            string temp = string.Empty;
            foreach (FamilyInstance e in roomCollector)
            {
                lightingFixturesList.Add(e);
                Debug.Print(e.Name);
                temp += e.Id + " " + e.Name + "\n";
            }
            //if (temp != string.Empty)
                //TaskDialog.Show("LightingFixtures", temp);
            return lightingFixturesList;
        }

        public List<FamilyInstance> GetElectricalEquipment(Document doc, Room room)
        {
            BoundingBoxIntersectsFilter filter = Filter(room);
            List<FamilyInstance> list = new List<FamilyInstance>();
            FilteredElementCollector collector = new FilteredElementCollector(doc).
                                                     OfClass(typeof(FamilyInstance)).
                                                     OfCategory(BuiltInCategory.OST_ElectricalEquipment).
                                                     WherePasses(filter);

            string temp = string.Empty;
            foreach (FamilyInstance e in collector)
            {

                list.Add(e);
                Debug.Print(e.Name);
                temp += e.Id + " " + e.Name + "\n";
            }
            //if (temp != string.Empty)
                //TaskDialog.Show("ElectricalEquipment", temp);
            return list;
        }

        public List<FamilyInstance> GetElectricalFixtures(Document doc, Room room)
        {
            BoundingBoxIntersectsFilter filter = Filter(room);
            List<FamilyInstance> list = new List<FamilyInstance>();
            FilteredElementCollector collector = new FilteredElementCollector(doc).
                                                    OfClass(typeof(FamilyInstance)).
                                                    OfCategory(BuiltInCategory.OST_ElectricalFixtures).
                                                    WherePasses(filter);
            string temp = string.Empty;
            foreach (FamilyInstance e in collector)
            {

                list.Add(e);
                temp += e.Id + " " + e.Name + "\n";
            }
            //if (temp != string.Empty)
                //TaskDialog.Show("ElectricalFixtures", temp);
            return list;
        }

        public List<FamilyInstance> GetFireAlarmDevices(Document doc, Room room)
        {
            BoundingBoxIntersectsFilter filter = Filter(room);
            List<FamilyInstance> list = new List<FamilyInstance>();
            FilteredElementCollector collector = new FilteredElementCollector(doc).
                                                    OfClass(typeof(FamilyInstance)).
                                                    OfCategory(BuiltInCategory.OST_FireAlarmDevices).
                                                    WherePasses(filter);

            string temp = string.Empty;
            foreach (FamilyInstance e in collector)
            {
                list.Add(e);
                temp += e.Id + " " + e.Name + "\n";
            }
            //if (temp != string.Empty)
                //TaskDialog.Show("FireAlarmDevices", temp);
            return list;
        }

        public void GetDevices(Room room, Document doc)
        {
            List<FamilyInstance> devicesList = new List<FamilyInstance>();
            BoundingBoxXYZ box = room.get_BoundingBox(null);
            if (box != null)
            {
                devicesList.AddRange(GetElectricalEquipment(doc, room));
                devicesList.AddRange(GetElectricalFixtures(doc, room));
                devicesList.AddRange(GetLighting(doc, room));
                devicesList.AddRange(GetLightingFixtures(doc, room));
                devicesList.AddRange(GetFireAlarmDevices(doc, room));

                string temp = string.Empty;               
                {                       
                    using (Transaction trans = new Transaction(doc, "Paramaters Adding"))
                    {
                        trans.Start();                        
                        foreach (FamilyInstance instance in devicesList)
                        {
                            temp += instance.Name + "\n";
                            Parameter param1 = instance.LookupParameter("RaumNummer");
                            Parameter param2 = instance.LookupParameter("RaumName");
                            param1.Set(room.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsDouble());
                            param2.Set(room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString());
                        }                           
                        trans.Commit();
                    }
                }

                //if (temp!=string.Empty)
                //TaskDialog.Show("Room Devices", temp +"\n" + room.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsValueString());
            }
        }

        public BoundingBoxIntersectsFilter Filter(Room room)
        {
            BoundingBoxXYZ box = room.get_BoundingBox(null);
            if (box != null)
            {
                Outline outline = new Outline(box.Min, box.Max);
                BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(outline);
                return filter;
            }
            return null;
        }

        public void SetDeviceParameter(List<Room> rooms, Document doc)
        {
            foreach (var item in rooms)
            {
                GetDevices(item, doc);
            }
        }
    }
}
