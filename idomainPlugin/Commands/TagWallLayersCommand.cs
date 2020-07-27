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

namespace idomainPlugin.Commands
{
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.DB;
    using System.Windows;
    using RestSharp.Serialization.Json;

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class TagWallLayersCommand : IExternalCommand
    {
        #region public methods
        [Obsolete]
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Format the prompt information string
            String promptInfo = "Element with ID 278826 parámetres will be updated with it's twin on AIN.\n";
            promptInfo += "If you don't wan`t this to happen, press Cancel.";

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

            //store element id's, aunque sólo queremos uno en principio

            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            foreach (var e in from ElementId id in selectedIds//for each selected element
                              let e = doc.GetElement(id)//get the element from the id
                              where e.Id.ToString() == "278826"//check each element to see if it is a receptacle
                              select e)
            {
                TaskDialog.Show("Informacion del sistema", "Se ha encontrado el elemento, se cambiará su parámetro Barcode a AG3-26234-IDOM");
                String idClient = "sb-ecf9d091-bb18-4525-9b13-0a7cc8a6b6b2!b6140|ain_broker_live!b1537";
                String secret = "wicnFoL0GmxaZhEb2V2UayFifp4=";
                JsonDeserializer deserial = new JsonDeserializer();
                var client = new RestClient("https://servicesiot.authentication.eu10.hana.ondemand.com/oauth/token");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddParameter("grant_type", "client_credentials");
                //request.AddParameter("Accept", "application/json");
                //request.AddParameter("Content-Type", "application/x-www-form-urlencoded");
                //client.Authenticator = new HttpBasicAuthenticator(idClient, secret);
                request.AddHeader("Authorization", "Basic c2ItZWNmOWQwOTEtYmIxOC00NTI1LTliMTMtMGE3Y2M4YTZiNmIyIWI2MTQwfGFpbl9icm9rZXJfbGl2ZSFiMTUzNzp3aWNuRm9MMEdteGFaaEViMlYyVWF5RmlmcDQ9");
                string resultado = "";
                var response = client.Execute(request);

                //client.ExecuteAsync(request, (response) =>
                  //{ 
                      resultado = response.Content;
                      TaskDialog.Show("Contenido Respuesta", resultado.ToString());
                      //Console.Write(resultado);
                      System.Diagnostics.Debug.WriteLine(resultado);
                      TaskDialog.Show("Contenido Respuesta", resultado.ToString());
                      
                      /*TaskDialog.Show("Contenido Respuesta", response.Content);
                      TaskDialog.Show("Contenido Respuesta", response.StatusCode.ToString());
                      TaskDialog.Show("Contenido Respuesta", response.ContentLength.ToString());

                      var jObject = JObject.Parse(response.Content);
                      TaskDialog.Show("Informacion del sistema", jObject.ToString());
                      var TestToken = jObject.SelectToken("access_token");
                      TaskDialog.Show("Informacion del sistema", TestToken.ToString());

                      var json = deserial.Deserialize<Dictionary<string, string>>(response);

                      if (json.ContainsKey("access_token"))
                      {
                          var token = json["access_token"];
                          TaskDialog.Show("Informacion del sistema", token.ToString());
                          client = new RestClient("https://ain-live.cfapps.eu10.hana.ondemand.com/services/api/v1/equipment(6F93AEFE21D341EA8AB3723419E66793)/values");
                          request = new RestRequest(Method.GET);
                          request.AddHeader("authorization", "Bearer " + token);
                          request.AddHeader("Content-Type", "application/json");
                          request.AddHeader("cache-control", "no-cache");
                          response = client.Execute(request);

                                ///Escribir el parámetro en Revit
                                Autodesk.Revit.DB.Transaction t = new Autodesk.Revit.DB.Transaction(doc, "Change Parameter");

                          t.Start();

                          Parameter myparam = e.LookupParameter("Barcode"); //lookup the value of the parameter named "Comment"

                                ///Sustituier lo de dentro del Set por lo que venga de la llamada..
                                myparam.Set("AG3 - 26234 - IDOM");

                          t.Commit();
                      }*/
                            //JObject jObject = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                            //var token = jObject.SelectToken("access_token");
                            //TaskDialog.Show("Informacion del sistema", token.ToString());
                            ///https://developers.onelogin.com/api-docs/1/samples/csharp/get-token-and-users
                  //});
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
