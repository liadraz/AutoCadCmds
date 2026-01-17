using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsSystem;
using Autodesk.AutoCAD.Runtime;

namespace AutoCadCmds
{
    public class BasicCmndExapmles
    {
        [CommandMethod("HelloWorld")]
        public void HelloWorld()
        {
            // Get the current document and editor
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            ed.WriteMessage("Hello, AutoCAD World!");
        }

        [CommandMethod("SelectOne")]
        public void SelectOne()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            
            PromptEntityOptions peo = new PromptEntityOptions("\nSelect an entity: ");
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status == PromptStatus.OK)
            {
                ed.WriteMessage($"\nYou selected an entity of type: {per.ObjectId.ObjectClass.Name}");
            }
            else
            {
                ed.WriteMessage("\nNo entity selected.");
            }
        }

        [CommandMethod("SelectMany")]
        public void SelectMany()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            PromptEntityOptions pso = new PromptEntityOptions("\nSelect Entities: ");
            PromptSelectionResult psr = ed.GetSelection();

            if (psr.Status == PromptStatus.OK)
            {
                SelectionSet ss = psr.Value;
                ed.WriteMessage($"\nYou selected {ss.Count} entities.");
            }
        }

        [CommandMethod("CreateLine")]
        public void CreateLine()
        {
            // Get the current database
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Open the Block table for read. 
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write. 
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Create a line that starts at (0,0,0) and ends at (100,100,0)
                Point3d startPt = new Point3d(0, 0, 0);
                Point3d endPt = new Point3d(100, 100, 0);
                Line line = new Line(startPt, endPt);

                // Add the new line to the block table record and the transaction
                btr.AppendEntity(line);
                tr.AddNewlyCreatedDBObject(line, true);

                tr.Commit();
            }
        }

        //       BlockTable                     Each dwg has only one BlockTable. BT is the catalog-registry consits only ObjectId references
        //├── BlockTableRecord(ModelSpace)      BTR represents one block consists entities in that block
        //│    ├── Line
        //│    ├── Polyline
        //│    └── Hatch
        //├── BlockTableRecord(PaperSpace)
        //└── BlockTableRecord("MyBlock")       Block that was made using the Block command and used as a template 
        //     ├── Circle
        //     └── Line
        //                                      When inserting a block, a BlockReference is created in the current space (Model or Paper) that references the BlockTableRecord.

        // Database vs Document
        // Document represents the DWG file opened in AutoCAD application including the UI grpahic layouts. Each Document has its own Database.
        //      To interact with the user use Document and Editor.
        // Database represents the actual data structure of the DWG file (the graphics itself) including all entities, layers, styles, etc.
        //      To create objects use Database.

        // Transaction represents a unit of work that allows you to group multiple operations into a single atomic action.
        //     Transactions are essential when working with the AutoCAD database to ensure that changes are made safely and consistently.
        //      When changes are made within a transaction, they can be committed (saved) or aborted (discarded) as a single unit.

        [CommandMethod("CretaeCircle")]
        public void CretaeCircle()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Prompt the user for the radius of the circle.
            //      These are the tools used to "talk" to the user and receive a decimal number (double) via the command line.
            //      The PromptDoubleOptions object acts as a filter between the user's keyboard and your code.
            //      When you set AllowNegative = false, the validation happens inside AutoCAD's internal engine
            PromptDoubleOptions pdo = new PromptDoubleOptions("\nEnter radius of the circle: ");
            pdo.AllowNegative = false;
            pdo.AllowZero = false;

            // Is a blocking method that waits for the user to input a value and press Enter.
            //      AutoCAD Engine does the validation based on the options set in pdo.
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nInvalid radius.");
                return;
            }

            double radius = pdr.Value;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                Point3d center = new Point3d(0, 0, 0);
                //      Vector3d.ZAxis - When creating a circle, AutoCAD needs to know its "Normal" (which way it faces). Usually, this is the Z-axis (0,0,1)
                Circle circle = new Circle(center, Vector3d.ZAxis, radius);
                
                btr.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);

                tr.Commit();
            }
        }

        [CommandMethod("MoveObject")]
        public void MoveObject()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // select entity and validate selection
            PromptEntityOptions peo = new PromptEntityOptions("\nSelect an entity to move: ");
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                //  open the selected entity for write
                Entity ent = tr.GetObject(per.ObjectId, OpenMode.ForWrite) as Entity;

                //  define the move vector
                Vector3d moveVec3d = new Vector3d(50.0, 50.0, 0);
                
                //  create the transformation matrix
                Matrix3d moveMatrix = Matrix3d.Displacement(moveVec3d);

                ent.TransformBy(moveMatrix);

                tr.Commit();
            }
        }

        // Matrix3d is the mathematical way AutoCAD handles moving, rotating, and scaling.
        //      TransformBy(Matrix3d) is better
        //      It works on any object, It is much faster for the database to process, It handles complex math (like rotating and moving at the same time) automatically.

        // Distinction between Creating and Modifying Objects
        //      In Creation: You need the BlockTableRecord because you are adding a new member to the family.
        //      You need to tell the "container" (Model Space) to accept a new child.
        //      In Modification: The object is already "inside" the Model Space.
        //      You already have its "address" (ObjectId).
        //      Since you aren't adding or removing anything from the Model Space you only need to open the object itself.

        [CommandMethod("CloneAndRed")]
        public void CloneAndRed()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptEntityOptions peo = new PromptEntityOptions("\nSelect an object to clone: ");
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity originalEnt = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;

                // Clone the entity
                // Note: Cloning creates a copy of the entity in memory. To add it to the drawing, you need to append it to a BlockTableRecord.
                Entity cloneEnt = originalEnt.Clone() as Entity;

                // Set color to red (1 is red in AutoCAD color index)
                cloneEnt.ColorIndex = 1;

                Matrix3d displacement = Matrix3d.Displacement(new Vector3d(20.0, 0, 0));
                cloneEnt.TransformBy(displacement);

                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                btr.AppendEntity(cloneEnt);
                tr.AddNewlyCreatedDBObject(cloneEnt, true);

                tr.Commit();
            }

            ed.WriteMessage("\nCloned entity created in red color.");
        }   
    }
} 