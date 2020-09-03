using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace idomainPlugin
{
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    public class Class1 :IExternalApplication
    {

        #region external application public methods

         Result IExternalApplication.OnStartup(UIControlledApplication application)
        {
            string tabName = "IDOM TOOLS";

            string panelAnnotationName = "Comandos AIN";

            application.CreateRibbonTab(tabName);

            var panelAnnotation = application.CreateRibbonPanel(tabName, panelAnnotationName);

            var TagWallLLayersBtnData = new PushButtonData("ConnectAINBttnData", "Connect\nAIN", Assembly.GetExecutingAssembly().Location, "idomainPlugin.Commands.ConnectAINCommand")
            {
                ToolTipImage = new BitmapImage(new Uri(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\idomainPlugin\\res\\Connect_320_320.png")),
                ToolTip = "Connect with AIN"

            };

            var TagWallLLayersBtn = panelAnnotation.AddItem(TagWallLLayersBtnData) as PushButton;
            TagWallLLayersBtn.LargeImage = new BitmapImage(new Uri(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\idomainPlugin\\res\\Connect_32_32.png"));

            var JSONFortmattingCommandData = new PushButtonData("JSONFortmattingCommand", "GET JSON\nfile", Assembly.GetExecutingAssembly().Location, "idomainPlugin.Commands.JSONFortmattingCommand")
            {
                ToolTipImage = new BitmapImage(new Uri(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\idomainPlugin\\res\\JSON_320_320.png")),
                ToolTip = "Download .json file with equipment data"

            };

            var JSONFortmattingCommandBtn = panelAnnotation.AddItem(JSONFortmattingCommandData) as PushButton;
            JSONFortmattingCommandBtn.LargeImage = new BitmapImage(new Uri(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\idomainPlugin\\res\\JSON_32_32.png"));


            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
        #endregion
    }
}
