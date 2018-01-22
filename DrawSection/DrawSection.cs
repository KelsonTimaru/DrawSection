using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.IO;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;

namespace DrawSection
{
    public class DrawSectionClass
    {
        [CommandMethod("DrawSection")]
        public static void DrawSection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            double dUTB = 31.68;
            double dDTB = 31.48;
            double dWaterDeepth = 1.73;
            double dRaceDeepth = 2.2;
            double dRoadWidth = 5.0;
            double dLenUTB = 10.0;
            //Bank的坡度
            double dGradeOfBank = 1.5;
            //Fence到路边的距离
            double dFenceGap = 0.3;

            //上下游过渡斜线的倾斜角度
            double dAngleOfSlopeInUpDown = 3;

            //Proposed Bank 颜色 = 92绿
            Color ProposedBankColor = Color.FromColorIndex(ColorMethod.ByAci, 92);


            //让用户在图上选取一个点
            PromptPointResult pPtRes;
            PromptPointOptions pPtOpts = new PromptPointOptions("");
            pPtOpts.Message = "\nEnter the start point of the line: ";
            pPtRes = doc.Editor.GetPoint(pPtOpts);

            //定义上游TB线的起点
            Point3d ptStart_UTB = pPtRes.Value;
            //定义上游TB线的终点(道路/桥的右边缘)
            Point3d ptEnd_UTB = new Point3d(ptStart_UTB.X + dRoadWidth + dLenUTB + dFenceGap * 2, ptStart_UTB.Y, ptStart_UTB.Z);


            // 如果用户按ESC键或取消命令，就退出
            if (pPtRes.Status == PromptStatus.Cancel) return;


            /////////
            //切换图层
            /////////

            //确定是否有Bank图层



            //绘制上游TB线  Start
            Line entUTB = new Line(ptStart_UTB, ptEnd_UTB);

            //设置线的颜色为
            entUTB.Color = ProposedBankColor;
            //设置线的宽度为0.13
            entUTB.LineWeight = LineWeight.LineWeight013;

            ObjectId LineUTB_Id = AppendEntity(entUTB);
            //绘制上游TB线  End


            //绘制上下游过渡斜线 Start
            //上下游高程的差
            double dDiffOfUpDownStream = Math.Abs(dUTB - dDTB);
            Point3d ptEndSlopeInUpDown = new Point3d(ptEnd_UTB.X + dDiffOfUpDownStream / Math.Tan(dAngleOfSlopeInUpDown * Math.PI / 180), ptStart_UTB.Y - dDiffOfUpDownStream, ptStart_UTB.Z);

            Line entSlopeInUpDownStream = new Line(ptEnd_UTB, ptEndSlopeInUpDown);

            //设置线的颜色为
            entSlopeInUpDownStream.Color = ProposedBankColor;
            //设置线的宽度为0.13
            entSlopeInUpDownStream.LineWeight = LineWeight.LineWeight013;

            ObjectId LineSlopeInUpDownStream_Id = AppendEntity(entSlopeInUpDownStream);
            //绘制上下游过渡斜线  End

            //绘制下游TB线 Start

            //确定下游TB线的终点，用上游TB线长度当作下游TB线长度
            //Point3d ptEnt_DTB = new Point3d()

            Line entDTB = new Line(ptEndSlopeInUpDown,new Point3d(ptEndSlopeInUpDown.X + dLenUTB,ptEndSlopeInUpDown.Y,0));

            //设置线的颜色为
            entDTB.Color = ProposedBankColor;
            //设置线的宽度为0.13
            entDTB.LineWeight = LineWeight.LineWeight013;

            ObjectId LineDTB_Id = AppendEntity(entDTB);

            //绘制下游TB线 End

            



        }

        //用于设定线型/颜色/线宽等的结构体
        public struct BankLineProp
        {
            public Color color;
            public double width;
            public LinetypeTable linetype;
        }

        //遍历并显示图层
        public static void DisplayLayerNames()
        {
            // 获取当前文档和数据库
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            // 启动事务
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // 以读模式打开图层表
                LayerTable acLyrTbl;
                acLyrTbl = acTrans.GetObject(acCurDb.LayerTableId,
                OpenMode.ForRead) as LayerTable;
                string sLayerNames = "";
                foreach (ObjectId acObjId in acLyrTbl)
                {
                    LayerTableRecord acLyrTblRec;
                    acLyrTblRec = acTrans.GetObject(acObjId, OpenMode.ForRead) as LayerTableRecord;
                    sLayerNames = sLayerNames + "\n" + acLyrTblRec.Name;
                }
                Application.ShowAlertDialog("The layers in this drawing are: " +
                sLayerNames);

                // 关闭事务
            }
        }


        public static ObjectId AppendEntity(Autodesk.AutoCAD.DatabaseServices.Entity ent)
        {
            Database acCurDb = HostApplicationServices.WorkingDatabase;
            ObjectId entId = new ObjectId();
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                //以只读方式打开块表
                BlockTable bt = (BlockTable)acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead);

                //以写方式打开模型空间块表记录
                BlockTableRecord btr = acTrans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                //将图形对象的信息添加到块表记录中，并返回ObjectId对象
                entId = btr.AppendEntity(ent);

                //把图形对象添加到事务处理中
                acTrans.AddNewlyCreatedDBObject(ent, true);

                //提交事务处理
                acTrans.Commit();

            }

            return entId;
        }

        //添加图层
        public static void AddMyLayer()
        {
            //获取当前文档和数据库，并启动事务； 
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                //返回当前数据库的图层表 
                LayerTable acLyrTbl;
                acLyrTbl = acTrans.GetObject(acCurDb.LayerTableId, OpenMode.ForRead) as LayerTable;
                //检查图层表里是否有图层MyLayer 
                if (acLyrTbl.Has("MyLayer") != true)
                {
                    //以写模式打开图层表 
                    acLyrTbl.UpgradeOpen();
                    //新创建一个图层表记录，并命名为”MyLayer” 
                    using (LayerTableRecord acLyrTblRec = new LayerTableRecord())
                    {
                        acLyrTblRec.Name = "MyLayer";
                        //添加新的图层表记录到图层表，添加事务 
                        acLyrTbl.Add(acLyrTblRec);
                        acTrans.AddNewlyCreatedDBObject(acLyrTblRec, true);
                        //释放DBObject对象 } 
                        //提交修改 
                        acTrans.Commit();
                    }
                    //关闭事务，回收内存； 
                }
                else
                {
                    Application.ShowAlertDialog("MyLayer already exists!");
                }
            }
        }

        //添加直线到当前模型空间
        [CommandMethod("AddLine")]
        public static void AddLine()
        {
            //获取当前文档及数据库
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            //启动事务
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                //以只读模式打开块表
                BlockTable acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                //以写模式打开Block表记录Model空间
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                //以5，5、12，3为起始点创建一条直线
                using (Line acLine = new Line(new Point3d(5, 5, 0), new Point3d(12, 3, 0)))
                {
                    //将新对象添加到块表记录和事务
                    acBlkTblRec.AppendEntity(acLine);
                    acTrans.AddNewlyCreatedDBObject(acLine, true);
                    //释放DBObject对象
                }

                //将新对象保存到数据库
                acTrans.Commit();
            }


        }
    }
}
