
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
            
            foreach (Room item in GetRooms(app))
            {
                ApplyParameterToDevice(item, doc);
            }
           
            return Result.Succeeded;
        }

        public List<Room> GetRooms(Application app)
        {
            List<Room> roomList = new List<Room>();
            
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
                }
            }
            return roomList;
        }
       
        private List<FamilyInstance> GetCategoryDevices(Document doc, Room room, BuiltInCategory category )
        {
            BoundingBoxIntersectsFilter filter = Filter(room);
            List<FamilyInstance> lightingList = new List<FamilyInstance>();
            FilteredElementCollector roomCollector = new FilteredElementCollector(doc).
                OfClass(typeof(FamilyInstance)).
                OfCategory(category).
                WherePasses(filter);

            foreach (FamilyInstance e in roomCollector)
            {
                lightingList.Add(e);
            }

            return lightingList;
        }

        private void ApplyParameterToDevice(Room room, Document doc)
        {
            List<FamilyInstance> devicesList = new List<FamilyInstance>();
            BoundingBoxXYZ box = room.get_BoundingBox(null);
            if (box != null)
            {
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_ElectricalEquipment));
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_ElectricalFixtures));
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_LightingDevices));
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_LightingFixtures));
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_FireAlarmDevices));
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_CommunicationDevices));
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_TelephoneDevices));

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
                            param1.Set(room.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsString());
                            param2.Set(room.get_Parameter(BuiltInParameter.ROOM_NAME).AsString());
                        }                           
                        trans.Commit();
                    }
                }
            }
        }

        private BoundingBoxIntersectsFilter Filter(Room room)
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
    }
}
