<<<<<<< HEAD
﻿using Newtonsoft.Json.Linq;
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
    public class ConnectAINCommand : IExternalCommand
    {
        #region public methods
        [Obsolete]
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Format the prompt information string
            String promptInfo = "Revit elements parameters will be actualized with the parameters of their twins on AIN.\n";
            promptInfo += "If you don't wan`t this to happen, press Cancel. Otherwise, press Accept";

            //Debajo se utiliza WriteAllText para sobreescribir desde 0 el fichero en caso de que exista y sino para crearlo
            string pathLog = @"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Sync_AIN_Events.txt";
            System.IO.File.WriteAllText(pathLog, "Initialization of AIN - Revit syncronization process at " +  DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + Environment.NewLine  );

            // Show the prompt message, and allow the user to close the dialog directly.
            TaskDialog taskDialog = new TaskDialog("Update Parameters");
            taskDialog.Id = "Customer DialogId";
            taskDialog.MainContent = promptInfo;
            TaskDialogCommonButtons buttons = TaskDialogCommonButtons.Ok |
                                                TaskDialogCommonButtons.Cancel;
            taskDialog.CommonButtons = buttons;
            TaskDialogResult result = taskDialog.Show();

            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Selection selection = uidoc.Selection;
            Document doc = uidoc.Document;
            string name = doc.Title;
            string path = doc.PathName;

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

            Boolean esFGC;

            for (var e = 0; e < totalEquipos; e++)
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
                    
                    var atributosElemento = jsonEquipment["templates"][0]["attributeGroups"][0]["attributes"].LongCount();

                    if (!(element is null))
                    {

                        for (var at = 0; at < atributosElemento; at++)
                        {
                            var attributeName = jsonEquipment["templates"][0]["attributeGroups"][0]["attributes"][at]["name"].ToString().TrimStart('{').TrimEnd('}');
                            var attributeValue = jsonEquipment["templates"][0]["attributeGroups"][0]["attributes"][at]["value1"].ToString().TrimStart('{').TrimEnd('}');
                            Autodesk.Revit.DB.Transaction t = new Autodesk.Revit.DB.Transaction(doc, "Change Parameter");
                            t.Start();
                            
                            Parameter myparam = element.LookupParameter(attributeName.Substring(1)); 

                            if (!(myparam is null))
                            {

                                ///Sustituir lo de dentro del Set por lo que venga de la llamada..
                                myparam.Set(attributeValue);
                                //TaskDialog.Show("Parameter correctly updated", "Parameter " + attributeName + " del equipo " + element.Id.ToString() +
                                //        " se ha cambiado correctamente a " + attributeValue);
                                //Debajo utilizamos sw.WriteLine para no sobreescribir el contenido que ya tenga el 
                                using (StreamWriter sw = File.AppendText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Sync_AIN_Events.txt"))
                                {
                                    sw.WriteLine("Parameter " + attributeName + " del equipo " + element.Id.ToString() +
                                        " se ha cambiado correctamente a " + attributeValue);

                                }
                                //System.IO.File.WriteAllText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Sync_AIN_Events.json", "Parameter " + attributeName + " del equipo " + element.Id.ToString() +
                                //        " se ha cambiado correctamente a " + attributeValue);
                            }
                            t.Commit();
                        }
                    }
                }
            }

            //System.IO.File.WriteAllText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Sync_AIN_Events.json", "Synchronization ended at " +  DateTime.Now.ToString("MM/dd/yyyy h:mm tt")  + " correctly.");
            using (StreamWriter sw = File.AppendText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Sync_AIN_Events.txt"))
            {
                sw.WriteLine("Synchronization ended at " + DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + " correctly.");

            }
            return Result.Succeeded;
        }

        internal class Token
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }
        }
        #endregion
    }
}
=======
﻿using Newtonsoft.Json.Linq;
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
    public class ConnectAINCommand : IExternalCommand
    {
        #region public methods
        [Obsolete]
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Format the prompt information string
            String promptInfo = "Revit elements parameters will be actualized with the parameters of their twins on AIN.\n";
            promptInfo += "If you don't wan`t this to happen, press Cancel. Otherwise, press Accept";

            //Debajo se utiliza WriteAllText para sobreescribir desde 0 el fichero en caso de que exista y sino para crearlo
            string pathLog = @"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Sync_AIN_Events.txt";
            System.IO.File.WriteAllText(pathLog, "Initialization of AIN - Revit syncronization process at " +  DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + Environment.NewLine  );

            // Show the prompt message, and allow the user to close the dialog directly.
            TaskDialog taskDialog = new TaskDialog("Update Parameters");
            taskDialog.Id = "Customer DialogId";
            taskDialog.MainContent = promptInfo;
            TaskDialogCommonButtons buttons = TaskDialogCommonButtons.Ok |
                                                TaskDialogCommonButtons.Cancel;
            taskDialog.CommonButtons = buttons;
            TaskDialogResult result = taskDialog.Show();

            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Selection selection = uidoc.Selection;
            Document doc = uidoc.Document;
            string name = doc.Title;
            string path = doc.PathName;

            //INICIO LLAMADA A TODOS LOS EQUIPOS
            //Esta llamada se hace para tener en una variable todos los equipments que están en AIN.
            JsonDeserializer deserial = new JsonDeserializer();
            var client = new RestClient("https://servicesiot.authentication.eu10.hana.ondemand.com/oauth/token");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddParameter("grant_type", "client_credentials");
            request.AddHeader("Authorization", "Basic c2ItZWNmOWQwOTEtYmIxOC00NTI1LTliMTMtMGE3Y2M4YTZiNmIyIWI2MTQwfGFpbl9icm9rZXJfbGl2ZSFiMTUzNzp3aWNuRm9MMEdteGFaaEViMlYyVWF5RmlmcDQ9");
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

            Boolean esFGC;

            for (var e = 0; e < totalEquipos; e++)
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
                    
                    var atributosElemento = jsonEquipment["templates"][0]["attributeGroups"][0]["attributes"].LongCount();

                    if (!(element is null))
                    {

                        for (var at = 0; at < atributosElemento; at++)
                        {
                            var attributeName = jsonEquipment["templates"][0]["attributeGroups"][0]["attributes"][at]["name"].ToString().TrimStart('{').TrimEnd('}');
                            var attributeValue = jsonEquipment["templates"][0]["attributeGroups"][0]["attributes"][at]["value1"].ToString().TrimStart('{').TrimEnd('}');
                            Autodesk.Revit.DB.Transaction t = new Autodesk.Revit.DB.Transaction(doc, "Change Parameter");
                            t.Start();
                            
                            Parameter myparam = element.LookupParameter(attributeName.Substring(1)); 

                            if (!(myparam is null))
                            {

                                ///Sustituir lo de dentro del Set por lo que venga de la llamada..
                                myparam.Set(attributeValue);
                                //TaskDialog.Show("Parameter correctly updated", "Parameter " + attributeName + " del equipo " + element.Id.ToString() +
                                //        " se ha cambiado correctamente a " + attributeValue);
                                //Debajo utilizamos sw.WriteLine para no sobreescribir el contenido que ya tenga el 
                                using (StreamWriter sw = File.AppendText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Sync_AIN_Events.txt"))
                                {
                                    sw.WriteLine("Parameter " + attributeName + " del equipo " + element.Id.ToString() +
                                        " se ha cambiado correctamente a " + attributeValue);

                                }
                                //System.IO.File.WriteAllText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Sync_AIN_Events.json", "Parameter " + attributeName + " del equipo " + element.Id.ToString() +
                                //        " se ha cambiado correctamente a " + attributeValue);
                            }
                            t.Commit();
                        }
                    }
                }
            }

            //System.IO.File.WriteAllText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Sync_AIN_Events.json", "Synchronization ended at " +  DateTime.Now.ToString("MM/dd/yyyy h:mm tt")  + " correctly.");
            using (StreamWriter sw = File.AppendText(@"C:\Users\" + Environment.UserName.ToString() + "\\Desktop\\Sync_AIN_Events.txt"))
            {
                sw.WriteLine("Synchronization ended at " + DateTime.Now.ToString("MM/dd/yyyy h:mm tt") + " correctly.");

            }
            return Result.Succeeded;
        }

        internal class Token
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }
        }
        #endregion
    }
}
>>>>>>> 30b16236226490e907684998de9f4d79d064a6d0
