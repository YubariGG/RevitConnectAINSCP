using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI.Selection;
using System.Windows.Forms;
using Newtonsoft.Json;
using RestSharp.Authenticators;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.Script.Serialization;

namespace idomainPlugin.Commands
{
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.DB;
    using System.Windows;
    using RestSharp.Serialization.Json;
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class JSONFortmattingCommand : IExternalCommand
    {
        #region public methods
        [Obsolete]
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Format the prompt information string
            String promptInfo = "A .json file will be downloaded from SAP AIN and put on your Desktop qith element's ID on its name.\n";

            // Show the prompt message, and allow the user to close the dialog directly.
            TaskDialog taskDialog = new TaskDialog("Update Parameters");
            taskDialog.Id = "Customer DialogId";
            taskDialog.MainContent = promptInfo;
            TaskDialogCommonButtons buttons = TaskDialogCommonButtons.Ok |
                                                TaskDialogCommonButtons.Cancel;
            taskDialog.CommonButtons = buttons;
            //TaskDialogResult result = taskDialog.Show();

            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Selection selection = uidoc.Selection;
            Document doc = uidoc.Document;
            string name = doc.Title;
            string path = doc.PathName;

            //store element id's, aunque sólo queremos uno en principio

            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            foreach (var e in from ElementId id in selectedIds//for each selected element
                              let e = doc.GetElement(id)//get the element from the id
                              where e.Id.ToString() == "278826"//check each element to see if it is a receptacle
                              select e)
            {
                //TaskDialog.Show("Informacion del sistema", "Se ha encontrado el elemento, se cambiará su parámetro Barcode a AG3-26234-IDOM");
                /*String idClient = "sb-ecf9d091-bb18-4525-9b13-0a7cc8a6b6b2!b6140|ain_broker_live!b1537";
                String secret = "wicnFoL0GmxaZhEb2V2UayFifp4=";*/
                JsonDeserializer deserial = new JsonDeserializer();
                var client = new RestClient("https://servicesiot.authentication.eu10.hana.ondemand.com/oauth/token");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddParameter("grant_type", "client_credentials");
                //request.AddParameter("Accept", "application/json");
                //request.AddParameter("Content-Type", "application/x-www-form-urlencoded");
                //client.Authenticator = new HttpBasicAuthenticator(idClient, secret);
                request.AddHeader("Authorization", "Basic c2ItZWNmOWQwOTEtYmIxOC00NTI1LTliMTMtMGE3Y2M4YTZiNmIyIWI2MTQwfGFpbl9icm9rZXJfbGl2ZSFiMTUzNzp3aWNuRm9MMEdteGFaaEViMlYyVWF5RmlmcDQ9");
                string access_token = "";

                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                var response = client.Execute(request);

                access_token = JObject.Parse(response.Content).SelectToken("access_token").ToString();
                //TaskDialog.Show("Access Token to AIN is", access_token);
                var clientEquipment = new RestClient("https://ain-live.cfapps.eu10.hana.ondemand.com/services/api/v1/equipment(6F93AEFE21D341EA8AB3723419E66793)/values");
                var requestEquipment = new RestRequest(Method.GET);
                requestEquipment.AddHeader("authorization", "Bearer " + access_token);
                /* request.AddHeader("Content-Type", "application/json");
                 request.AddHeader("cache-control", "no-cache");*/
                var responseEquipment = clientEquipment.Execute(requestEquipment);

                //Creación del fichero json que se requiere en la prueba
                var serializer = new JavaScriptSerializer();
                var serializedResult = serializer.Serialize(responseEquipment.Content);
                string json_pretty = JSON_PrettyPrinter.Process(serializedResult);
                //Escritura en fichero
                System.IO.File.WriteAllText(@"C:\Users\jaime.hernandez\Desktop\jsonAIN" + e.Id.ToString() + ".json", json_pretty);

                TaskDialog.Show("Download correct", "JSON File is on your desktop with name " + e.Id.ToString() + ".json");
            }

            return Result.Succeeded;
        }

        class JSON_PrettyPrinter
        {
            public static string Process(string inputText)
            {
                bool escaped = false;
                bool inquotes = false;
                int column = 0;
                int indentation = 0;
                Stack<int> indentations = new Stack<int>();
                int TABBING = 8;
                StringBuilder sb = new StringBuilder();
                foreach (char x in inputText)
                {
                    sb.Append(x);
                    column++;
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else
                    {
                        if (x == '\\')
                        {
                            escaped = true;
                        }
                        else if (x == '\"')
                        {
                            inquotes = !inquotes;
                        }
                        else if (!inquotes)
                        {
                            if (x == ',')
                            {
                                // if we see a comma, go to next line, and indent to the same depth
                                sb.Append("\r\n");
                                column = 0;
                                for (int i = 0; i < indentation; i++)
                                {
                                    sb.Append(" ");
                                    column++;
                                }
                            }
                            else if (x == '[' || x == '{')
                            {
                                // if we open a bracket or brace, indent further (push on stack)
                                indentations.Push(indentation);
                                indentation = column;
                            }
                            else if (x == ']' || x == '}')
                            {
                                // if we close a bracket or brace, undo one level of indent (pop)
                                indentation = indentations.Pop();
                            }
                            else if (x == ':')
                            {
                                // if we see a colon, add spaces until we get to the next
                                // tab stop, but without using tab characters!
                                while ((column % TABBING) != 0)
                                {
                                    sb.Append(' ');
                                    column++;
                                }
                            }
                        }
                    }
                }
                return sb.ToString();
            }

        }
        #endregion
    }
}
