using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.IO;
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
            String promptInfo = "A .json file will be downloaded from SAP AIN and put on your Desktop with FGC equipments information.\n";
            
            // Show the prompt message, and allow the user to close the dialog directly.
            TaskDialog taskDialog = new TaskDialog("Update Parameters");
            taskDialog.Id = "Customer DialogId";
            taskDialog.MainContent = promptInfo;
            TaskDialogCommonButtons buttons = TaskDialogCommonButtons.Ok |
                                                TaskDialogCommonButtons.Cancel;
            taskDialog.CommonButtons = buttons;
            TaskDialogResult result = taskDialog.Show();

            //Debajo se utiliza WriteAllText para sobreescribir desde 0 el fichero en caso de que exista y sino para crearlo
            System.IO.File.WriteAllText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Extract_AIN_Events.txt", "Initialization of JSON extraction process on " +
                DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + Environment.NewLine);

            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Selection selection = uidoc.Selection;
            Document doc = uidoc.Document;
            string name = doc.Title;
            string path = doc.PathName;

            //store element id's, aunque sólo queremos uno en principio

            //INICIO LLAMADA A TODOS LOS EQUIPOS
            //Esta llamada se hace para tener en una variable todos los equipments que están en AIN.
            JsonDeserializer deserial = new JsonDeserializer();
            var client = new RestClient("https://servicesiot.authentication.eu10.hana.ondemand.com/oauth/token");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddParameter("grant_type", "client_credentials");
            request.AddHeader("Authorization", "Basic YOUR TOKEN");
            string access_token = "";

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            var response = client.Execute(request);

            access_token = JObject.Parse(response.Content).SelectToken("access_token").ToString();
            //TaskDialog.Show("Access Token to AIN is", access_token);
            var clientEquipments = new RestClient("https://ain-live.cfapps.eu10.hana.ondemand.com/services/api/v1/equipment");
            var requestEquipments = new RestRequest(Method.GET);
            requestEquipments.AddParameter("Accept", "application/json");
            requestEquipments.AddParameter("Content-Type", "application/x-www-form-urlencoded");
            requestEquipments.AddHeader("authorization", "Bearer " + access_token);

            var responseEquipments = clientEquipments.Execute(requestEquipments);
            var respuestaFormateada = String.Concat("{'equipos':", responseEquipments.Content.ToString(), "}");
            //var respuestaFormateada = responseEquipments.Content.ToString().TrimStart('[').TrimEnd(']');
            JObject json = JObject.Parse(respuestaFormateada);
            //FIN LLAMADA A TODOS LOS EQUIPOS

            var totalEquipos = json["equipos"].LongCount();

            //Se hace un for para ir recorriendo los resultados que vienen de AIN.
            //Dentro del for hay que comprobar el parámetro ShortDescription en cada elemento para ver si contiene el string FGC_
            //En caso de que  tenga el string se hace la llamada a AIN con el equipmentId correspondiente y se pisan todos los parámetros.
            int e = 0;
            int a = 0;
            Boolean esFGC;
            Boolean notEnded = true;
            //Creación del fichero json que se requiere en la prueba
            /*var serializer = new JavaScriptSerializer();
            var serializedResult = serializer.Serialize(responseEquipment.Content);
            string json_pretty = JSON_PrettyPrinter.Process(serializedResult);*/
            while (notEnded)
            {
                var equimentId = json["equipos"][e]["equipmentId"].ToString().TrimStart('{').TrimEnd('}');
                esFGC = json["equipos"][e]["shortDescription"].ToString().TrimStart('{').TrimEnd('}').Contains("FGC_");
                var internalId = json["equipos"][e]["internalId"].ToString().TrimStart('{').TrimEnd('}');
                if (esFGC)
                {
                    //TaskDialog.Show("Detección de equipos", "Equipment " + json["equipos"][e]["shortDescription"] + "is FGC.");
                    //Una vez sabemos que un equipo es FGC hacemos la llamada correspondiente para recoger los valores de atributos del equipo
                    var clientEquipment = new RestClient("https://ain-live.cfapps.eu10.hana.ondemand.com/services/api/v1/equipment(" + equimentId + ")/values");
                    var requestEquipment = new RestRequest(Method.GET);
                    requestEquipment.AddHeader("authorization", "Bearer " + access_token);
                    var responseEquipment = clientEquipment.Execute(requestEquipment);
                    var jsonEquipment = JObject.Parse(responseEquipment.Content);
                    ///Escribir el parámetro en Revit
                    //Aqui hay que meter un for en funcion del número de atributos que devuelve /values.
                    //Depués, por cada parámetro hay que comprobar que existe y escribirlo en Revit.

                    int idInt = Convert.ToInt32(internalId);
                    ElementId id = new ElementId(idInt);
                    Element element = doc.GetElement(id);
                    if (!(element is null))
                    {

                        using (StreamWriter sw = File.AppendText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Extract_AIN_Events.txt"))
                        {
                            sw.WriteLine(json["equipos"][e]["shortDescription"] + " equipment's parameters were" +
                            "   downloaded and updated on the Revit model.");

                        }

                        //System.IO.File.WriteAllText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Extract_AIN_Events.json",   json["equipos"][e]["shortDescription"] + " equipment's parameters were" +
                        //    "   downloaded and updated on the Revit model.");
                    }

                    var atributosElemento = jsonEquipment["templates"][0]["attributeGroups"][0]["attributes"];
                    json["equipos"][e]["attributes"] = atributosElemento;
                    e++;
                    a++;
                }
                else
                {
                    a++;
                    //Se borra el nodo de la variable JSON en la posición actual
                    (json["equipos"] as JArray).RemoveAt(e);
                }
                if (a ==  totalEquipos)
                {
                    notEnded = false;
                }
                
            }
            //Escritura en fichero
            string jsonPaint = json.ToString();

            System.IO.File.WriteAllText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\FGC_Equipments.json", jsonPaint);

            TaskDialog.Show("Download correct", "JSON File is on your desktop with name FGC_Equipments.json");
            
            using (StreamWriter sw = File.AppendText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Extract_AIN_Events.txt"))
            {
                sw.WriteLine("A total of " + e + " equipments information has been updated correctly");
                sw.WriteLine("JSON file correctly generated and saved on Desktop on " +
                DateTime.Now.ToString("MM/dd/yyyy h:mm tt"));
            }

            //System.IO.File.WriteAllText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Extract_AIN_Events.json", "A total of " + e + " equipments information has been updated correctly");

            //System.IO.File.WriteAllText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Extract_AIN_Events.json", "JSON file correctly generated and saved on Desktop on " +
            //    DateTime.Now.ToString("MM/dd/yyyy h:mm tt"));

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

