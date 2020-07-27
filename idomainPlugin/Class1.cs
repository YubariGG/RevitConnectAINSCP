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
            string tabName = "Plugin IDOM";

            string panelAnnotationName = "Comandos AIN";

            application.CreateRibbonTab(tabName);

            var panelAnnotation = application.CreateRibbonPanel(tabName, panelAnnotationName);

            var TagWallLLayersBtnData = new PushButtonData("TagWallLLayersBtnData", "Connect\nAIN", Assembly.GetExecutingAssembly().Location, "idomainPlugin.Commands.TagWallLayersCommand")
            {
                ToolTipImage = new BitmapImage(new Uri(@"C:\pluginIDOM\res\IDOM_320_320.png")),
                ToolTip = "IDOM"

            };

            var TagWallLLayersBtn = panelAnnotation.AddItem(TagWallLLayersBtnData) as PushButton;
            TagWallLLayersBtn.LargeImage = new BitmapImage(new Uri(@"C:\pluginIDOM\res\IDOM_32_32.png"));


            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
        #endregion
    }
}
