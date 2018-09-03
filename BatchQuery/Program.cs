using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using Terrasoft.Core.Entities;
using Terrasoft.Nui.ServiceModel.DataContract;

namespace BatchExample
{
    // The helper class. Used to convert authentication response JSON string.
    class AuthResponseStatus
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public object Exception { get; set; }
        public object PasswordChangeUrl { get; set; }
        public object RedirectUrl { get; set; }
    }

    class Program
    {
        // Main bpm'online URL. Has to be changed to a custom one, for example, http://myapplication.bpmonline.com.
        private const string baseUri = "http://localhost/bpmonline7.12.3";

        // Container for cookie authentication in bpm'online. Must be used in subsequent requests.
        public static CookieContainer AuthCookie = new CookieContainer();

        // Query string to the Login method of the AuthService.svc service.
        private const string authServiceUri = baseUri + @"/ServiceModel/AuthService.svc/Login";

        // InsertQuery query path string.
        private const string queryUri = baseUri + @"/0/DataService/json/reply/BatchQuery";

        /// <summary>
        /// Performs user authentication request.
        /// </summary>
        /// <param name="userName">The name of the bpm'online user.</param>
        /// <param name="userPassword">The passord of the bpm'online user.</param>
        /// <returns></returns>
        public static bool TryLogin(string userName, string userPassword)
        {
            // Creating an instance of the authentication service request.
            var authRequest = HttpWebRequest.Create(authServiceUri) as HttpWebRequest;

            // Specifying the request HTTP method.
            authRequest.Method = "POST";

            // Defining the request's content type.
            authRequest.ContentType = "application/json";

            // Enabling the use of cookies in the request.
            authRequest.CookieContainer = AuthCookie;

            // Placing user credentials to the request.
            using (var requestStream = authRequest.GetRequestStream())
            {
                using (var writer = new StreamWriter(requestStream))
                {
                    writer.Write(@"{
                    ""UserName"":""" + userName + @""",
                    ""UserPassword"":""" + userPassword + @"""
                    }");
                }
            }

            // Auxiliary object where the HTTP reply data will be de-serialized.
            AuthResponseStatus status = null;

            // Getting a reply from the server. If the authentication is successful, cookie will be placed to the AuthCookie property.
            // These cookies must be used for subsequent requests.
            using (var response = (HttpWebResponse)authRequest.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    // De-serialization of the HTTP response to an auxiliary object.
                    string responseText = reader.ReadToEnd();
                    status = new JavaScriptSerializer().Deserialize<AuthResponseStatus>(responseText);
                }

            }

            // Checking authentication status.
            if (status != null)
            {
                // Authentication is successful.
                if (status.Code == 0)
                {
                    return true;
                }
                // Authentication is unsuccessful.
                Console.WriteLine(status.Message);
            }
            return false;
        }

        // Main method of the application.
        static void Main(string[] args)
        {
            // Calling authentication method. If the user is not authenticated, the application exits.
            if (!TryLogin("Supervisor", "Supervisor"))
            {
                Console.WriteLine("Authentication error!");
                return;
            }

            // InsertQuery class instance. See InsertExample project for more information.
            var insertQuery = new InsertQuery()
            {
                RootSchemaName = "Contact",
                ColumnValues = new ColumnValues()
                {
                    Items = new Dictionary<string, ColumnExpression>()
                    {
                        {
                            "Name",
                            new ColumnExpression()
                            {
                                ExpressionType = EntitySchemaQueryExpressionType.Parameter,
                                Parameter = new Parameter
                                {
                                    Value = "John Best",
                                    DataValueType = DataValueType.Text
                                }
                            }
                        }
                    }
                }
            };

            // InsertQuery class instance. See UpdateExample project for more information.
            var updateQuery = new UpdateQuery()
            {
                RootSchemaName = "Contact",
                ColumnValues = new ColumnValues()
                {
                    Items = new Dictionary<string, ColumnExpression>()
                    {
                        {
                            "Phone",
                            new ColumnExpression()
                            {
                                ExpressionType = EntitySchemaQueryExpressionType.Parameter,
                                Parameter = new Parameter()
                                {
                                    Value = "0123456789",
                                    DataValueType = DataValueType.Text
                                }
                            }
                        }
                    }
                },
                Filters = new Filters()
                {
                    FilterType = Terrasoft.Nui.ServiceModel.DataContract.FilterType.FilterGroup,
                    Items = new Dictionary<string, Filter>()
                    {
                        {
                            "FilterByName",
                            new Filter
                            {
                                FilterType = Terrasoft.Nui.ServiceModel.DataContract.FilterType.CompareFilter,
                                ComparisonType = FilterComparisonType.Equal,
                                LeftExpression = new BaseExpression()
                                {
                                    ExpressionType = EntitySchemaQueryExpressionType.SchemaColumn,
                                    ColumnPath = "Name"
                                },
                                RightExpression = new BaseExpression()
                                {
                                    ExpressionType = EntitySchemaQueryExpressionType.Parameter,
                                    Parameter = new Parameter()
                                    {
                                        DataValueType = DataValueType.Text,
                                        Value = "John Best"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // InsertQuery instance serialization to the JSON string.
            var jsonInsert = new JavaScriptSerializer().Serialize(insertQuery);
            // Adding query type to the JSON string.
            jsonInsert = jsonInsert.Insert(1, @"""__type"": ""Terrasoft.Nui.ServiceModel.DataContract.InsertQuery"",");
            
            var jsonUpdate = new JavaScriptSerializer().Serialize(updateQuery);
            jsonUpdate = jsonUpdate.Insert(1, @"""__type"": ""Terrasoft.Nui.ServiceModel.DataContract.UpdateQuery"",");
            // Формирование пакетного запроса.
            var json = @"{""items"": [" + jsonInsert + "," + jsonUpdate + "]}";

            Console.WriteLine(jsonInsert);
            Console.WriteLine();
            Console.WriteLine(jsonUpdate);
            Console.WriteLine();
            Console.WriteLine(json);

            
            // Converting JSON string to a byte array.
            byte[] jsonArray = Encoding.UTF8.GetBytes(json);
            // Creating HTTP request instance.
            var insertRequest = HttpWebRequest.Create(queryUri) as HttpWebRequest;
            // Defining HTTP method of the request.
            insertRequest.Method = "POST";
            // Defining request content type.
            insertRequest.ContentType = "application/json";
            // Adding the previously received authentication cookies.
            insertRequest.CookieContainer = AuthCookie;
            // Setting the request content length.
            insertRequest.ContentLength = jsonArray.Length;

            // Putting BPMCSRF token to the request header.
            CookieCollection cookieCollection = AuthCookie.GetCookies(new Uri(authServiceUri));
            string csrfToken = cookieCollection["BPMCSRF"].Value;
            insertRequest.Headers.Add("BPMCSRF", csrfToken);

            // Adding a JSON string to the request body.
            using (var requestStream = insertRequest.GetRequestStream())
            {
                requestStream.Write(jsonArray, 0, jsonArray.Length);
            }
            // Executing the HTTP request and receiving response from the server.
            using (var response = (HttpWebResponse)insertRequest.GetResponse())
            {
                // Displaying respose in console.
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    Console.WriteLine(reader.ReadToEnd());
                    // Response is just a JSON string.
                    // The main prooperties of such JSON are:
                    // ID - contains unique identifier of the inserted record.
                    // success - indicates whether the record was added successfully.
                    // You can convert this JSON to a plain old CLR object in same way as it is done in TryLogin() method above.
                    // But you have to define corresponding class before doing this.
                }
            }

            // Pause.
            Console.ReadLine();
        }
    }
}
