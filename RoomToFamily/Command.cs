
#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;
using Application = Autodesk.Revit.ApplicationServices.Application;
using MessageBox = System.Windows.MessageBox;

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

            DocSelection dc = new DocSelection();
            dc.GetDocList(GetDocuments(app));

            Window docSelectionWindow = new Window();
            docSelectionWindow.ResizeMode = ResizeMode.NoResize;
            docSelectionWindow.Width = 500;
            docSelectionWindow.Height = 350;
            docSelectionWindow.Topmost = true;
            docSelectionWindow.Content = dc;
            docSelectionWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            docSelectionWindow.ShowDialog();

            List<Document> UserDefinedDocuments = new List<Document>();
            if (docSelectionWindow.DialogResult == true)
            {
                if (dc.Docs.Count > 0)
                {
                    UserDefinedDocuments = dc.Docs;
                }
                else
                {
                    MessageBox.Show("No projects selected");
                    return Result.Cancelled;
                }
            }

            LevelSelection ls = new LevelSelection();
            foreach (Document docItem in UserDefinedDocuments)
            {
                ls.GetLevels(GetLevels(docItem));
            }

            Window levelSelectionWindow = new Window();
            levelSelectionWindow.ResizeMode = ResizeMode.NoResize;
            levelSelectionWindow.Width = 320;
            levelSelectionWindow.Height = 350;
            levelSelectionWindow.Topmost = true;
            levelSelectionWindow.Content = ls;
            levelSelectionWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            levelSelectionWindow.ShowDialog();

            List<Level> UserDefinedLevels = new List<Level>();
            if (levelSelectionWindow.DialogResult == true)
            {
                if (ls.Levels.Count > 0)
                {
                    UserDefinedLevels = ls.Levels;
                }
                else
                {
                    MessageBox.Show("No levels selected");
                    return Result.Cancelled;
                }
            }

            //int counter = 0;
            //FilteredElementCollector luminaries = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
            //    .OfCategory(BuiltInCategory.OST_LightingFixtures);
            //FilteredElementCollector ElectricalEquipment = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
            //    .OfCategory(BuiltInCategory.OST_ElectricalEquipment);
            //FilteredElementCollector LightingDevices = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
            //    .OfCategory(BuiltInCategory.OST_LightingDevices);
            //FilteredElementCollector FireAlarmDevices = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
            //    .OfCategory(BuiltInCategory.OST_FireAlarmDevices);
            //FilteredElementCollector CommunicationDevices = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
            //    .OfCategory(BuiltInCategory.OST_CommunicationDevices);
            //FilteredElementCollector TelephoneDevices = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
            //    .OfCategory(BuiltInCategory.OST_TelephoneDevices);
            //FilteredElementCollector ElectricalFixtures = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
            //    .OfCategory(BuiltInCategory.OST_ElectricalFixtures);

            //List<Element> instancelist = new List<Element>();
            //instancelist.AddRange(luminaries.ToElements().ToList());
            //instancelist.AddRange(ElectricalEquipment.ToElements().ToList());
            //instancelist.AddRange(LightingDevices.ToElements().ToList());
            //instancelist.AddRange(CommunicationDevices.ToElements().ToList());
            //instancelist.AddRange(FireAlarmDevices.ToElements().ToList());
            //instancelist.AddRange(TelephoneDevices.ToElements().ToList());
            //instancelist.AddRange(ElectricalFixtures.ToElements().ToList());

            //foreach (FamilyInstance e in instancelist)
            //{
            //    {
            //        using (Transaction trans = new Transaction(doc, "Paramaters Adding"))
            //        {
            //            trans.Start();
            //            Parameter param1 = e.LookupParameter("RaumNummer");
            //            Parameter param2 = e.LookupParameter("RaumName");
            //            param1.Set("Clear Number");
            //            param2.Set("Clear Name");
            //            trans.Commit();
            //        }
            //    }
            //}

            foreach (Room item in GetRooms(UserDefinedDocuments, UserDefinedLevels))
            {
                ApplyParameterToDevice(item, doc);
            }
            TaskDialog.Show("Task Completed", "Task ended");
            return Result.Succeeded;
        }

        //private void BuildLline(Document doc, XYZ locationpoint)
        //{
        //    using (Transaction trans = new Transaction(doc, "Paramaters Adding"))
        //    {
        //        trans.Start();
        //        XYZ start = new XYZ(locationpoint.X, locationpoint.Y, 0);
        //        XYZ end = new XYZ(0, 0, 0);
        //        Line line = Line.CreateBound(start, end);
        //        Plane plane = new Plane();
        //        doc.Create.NewModelCurve(line, SketchPlane.Create(doc, plane));

        //        trans.Commit();
        //    }
        //}

        public List<Level> GetLevels(Document doc)
        {
            List<Level> levels = new List<Level>();
            FilteredElementCollector levelCollector = new FilteredElementCollector(doc).OfClass(typeof(Level));

            foreach (Element level in levelCollector)
            {
                levels.Add(level as Level);
            }
            return levels;
        }

        public List<Document> GetDocuments(Application app)
        {
            List<Document> docs = new List<Document>();
            foreach (Document d in app.Documents)
            {
                docs.Add(d);
            }
            return docs;
        }

        public List<Room> GetRooms(List<Document> docs, List<Level> levels)
        {
            List<Room> roomList = new List<Room>();
            string temp = string.Empty;
            foreach (Document d in docs)
            {
                FilteredElementCollector roomLinkedCollector = new FilteredElementCollector(d).OfCategory(BuiltInCategory.OST_Rooms);
                foreach (Room room in roomLinkedCollector)
                {
                    foreach (Level level in levels)
                    {
                        if (room.LevelId == level.Id)
                        {
                            roomList.Add(room);
                        }
                    }
                }
            }
            return roomList;
        }

        private List<FamilyInstance> GetCategoryDevices(Document doc, Room room, BuiltInCategory category)
        {
            BoundingBoxIntersectsFilter filter = Filter(room);
            List<FamilyInstance> instances = new List<FamilyInstance>();
            FilteredElementCollector roomCollector = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
                .OfCategory(category)
                .WherePasses(filter);

            foreach (FamilyInstance e in roomCollector)
            {
                if (FilterPosition(room, e))
                {
                    instances.Add(e);
                }
            }
           
            return instances;
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
                devicesList.AddRange(GetCategoryDevices(doc, room, BuiltInCategory.OST_DataDevices));

                {
                    using (Transaction trans = new Transaction(doc, "Paramaters Adding"))
                    {
                        trans.Start();
                        foreach (FamilyInstance instance in devicesList)
                        {
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

        private PointF GetLocation(FamilyInstance instance)
        {
            PointF location = new PointF();
            Location locationInstance = instance.Location;
            LocationPoint point = locationInstance as LocationPoint;

            location.X = (float)(point.Point.X);
            location.Y = (float)(point.Point.Y);
            return location;
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

        private bool FilterPosition(Room room, FamilyInstance instance)
        {
            XYZ locationPoint = new XYZ(GetLocation(instance).X, GetLocation(instance).Y, room.get_BoundingBox(null).Min.Z);
            if (room.IsPointInRoom(locationPoint))
            {
                return true;
            }
            else
                return false;
        }
    }
}
